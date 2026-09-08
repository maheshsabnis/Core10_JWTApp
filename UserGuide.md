# User Guide: Testing the Identity API

This guide explains how to start the application and invoke its endpoints using Visual Studio's HTTP editor, Postman, or PowerShell.

> The application uses ASP.NET Core 10 Identity's **built-in opaque bearer tokens**, not JWTs. Use the returned access token directly in the Authorization header; do not try to decode it as a JWT.

## 1. Prerequisites and current configuration

- Visual Studio 2026 with ASP.NET/web development support, or the .NET 10 SDK.
- A running **default SQL Server instance** on `localhost` (not LocalDB or `SQLEXPRESS`).
- Windows Authentication access to SQL Server for the Windows account running the application.
- Permission to create the database if it does not exist, and to create/read/write its Identity tables.

The current `Core10_JWTApp/appsettings.json` connection string is:

```text
Server=localhost;Database=aspnet10jwtdb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

`appsettings.Development.json` does not override it. Environment variables or user secrets can still override configuration. Older LocalDB instructions in README.md do not describe this current connection.

There are two different kinds of authentication here:

- **SQL Server Windows Authentication:** the application's Windows process identity connects to the database. No SQL username/password is required in the connection string.
- **API bearer authentication:** a registered application user logs in with email/password and receives tokens. Your Windows login does not automatically authenticate API requests.

Development startup calls `EnsureCreatedAsync()` to create a fresh database/schema. It does **not** apply EF migrations or upgrade an existing schema. Installing EF Design/Tools does not change this behavior. Do not run `Update-Database` against an `EnsureCreated` database without a planned migration/baseline strategy, and do not delete a database containing data to fix schema errors.

## 2. Start the application

### Visual Studio

1. Open `Core10_JWTApp.slnx`.
2. Set `Core10_JWTApp` as the startup project if necessary.
3. Select the **https** launch profile.
4. Start with **F5** or **Ctrl+F5**.
5. Keep the application running while sending requests.

The launch profile sets the environment to `Development`. No browser opens automatically.

### PowerShell terminal

Run these commands from the solution directory:

```powershell
Set-Location 'D:\maheshapps\net10\Core10_JWTApp'
dotnet dev-certs https --trust
dotnet build
dotnet run --project .\Core10_JWTApp\Core10_JWTApp.csproj --launch-profile https
```

Accept the development-certificate trust prompt. Use a second terminal for test requests; the first terminal hosts the application and displays email-confirmation logs.

### Addresses

| Address | Purpose |
| --- | --- |
| `https://localhost:7045` | Base URL for the requests in this guide |
| `https://localhost:7045/weatherforecast` | Public endpoint for a quick connectivity check |
| `https://localhost:7045/openapi/v1.json` | OpenAPI document, available only in Development |

Opening `/` or `/swagger` may return **404**: this is an API application with no home page or Swagger UI configured. OpenAPI JSON is not an interactive Swagger UI. Use HTTPS directly instead of relying on HTTP redirects.

## 3. Choose a request client

### Option A: Visual Studio HTTP editor (quickest)

Open [Core10_JWTApp/Identity.http](Core10_JWTApp/Identity.http). The file already contains most of the requests below.

1. Set `@host` to `https://localhost:7045`.
2. Choose a unique test `@email` and a strong test `@password`.
3. Click **Send Request** above the request you want to execute.
4. Copy the logged confirmation URL into `@confirmationUrl` when requested.
5. After login, paste the response values into `@accessToken` and `@refreshToken`. Paste only the token strings, without surrounding quotes or the `Bearer` prefix.

Run requests in the order described in this guide, not blindly from top to bottom. In the existing HTTP file, login appears before confirmation so you can test the expected pre-confirmation failure. Run login again after confirming. Optional email/password changes affect later requests.

For additional requests not already in the HTTP file, use Postman or a scratch `.http` file. Separate HTTP requests with `###`.

### Option B: Postman

1. Create a request and choose its method (`GET`, `POST`, or `PUT`).
2. Enter the full URL, for example `https://localhost:7045/auth/register`.
3. For requests with a JSON body, choose **Body > raw > JSON** and paste the body shown below. This sets `Content-Type: application/json`.
4. For protected endpoints, choose **Authorization > Bearer Token** and paste the access token only. Postman adds `Authorization: Bearer ...`.
5. For registration, login, confirmation, and refresh, choose **No Auth**. Refresh uses the refresh token in the JSON body, not in an Authorization header.
6. Click **Send** and inspect the status and response body.

The HTTP examples below also show headers explicitly. In Postman, paste only the JSON part into the body, not the HTTP method or headers.

### Option C: PowerShell

Section 7 provides a copy-and-paste workflow using `Invoke-RestMethod`.

## 4. Core test: register, confirm, log in, and manage a profile

Use a disposable account such as `identity-demo@example.test`. Example passwords below are for local testing only. If that email is already registered, choose a new email or log in with the existing account.

### Step 1: Register

```http
POST https://localhost:7045/auth/register
Content-Type: application/json

{
  "email": "identity-demo@example.test",
  "password": "Development-Only!234"
}
```

**Expected:** `200 OK`, usually with an empty body. Registration does not return tokens or log you in. Invalid email, weak password, or duplicate registration returns `400 Bad Request` with validation details.

### Step 2: Confirm the email address

No real email is sent. Look in the application's terminal or **View > Output > Debug** when launched under the Visual Studio debugger for:

```text
Development email confirmation for identity-demo@example.test: https://localhost:7045/auth/confirmEmail?userId=...&code=...
```

Copy the **complete URL after the email address**, retaining the query string exactly as logged. Open it in a browser or send a GET request to that URL. Do not use the `...` placeholder above as an actual URL or decode/re-encode the code manually.

**Expected:** `200 OK` with confirmation text. Logging in before this step returns `401 Unauthorized`.

If the link is unavailable, request another:

```http
POST https://localhost:7045/auth/resendConfirmationEmail
Content-Type: application/json

{
  "email": "identity-demo@example.test"
}
```

**Expected:** `200 OK`. For a registered account, look for a new link in the logs. A successful resend response alone does not prove an account exists.

### Step 3: Log in using bearer tokens

```http
POST https://localhost:7045/auth/login?useCookies=false
Content-Type: application/json

{
  "email": "identity-demo@example.test",
  "password": "Development-Only!234"
}
```

**Expected:** `200 OK` with a response shaped like:

```json
{
  "tokenType": "Bearer",
  "accessToken": "<opaque-access-token>",
  "expiresIn": 900,
  "refreshToken": "<opaque-refresh-token>"
}
```

Save both tokens privately. `expiresIn` is in seconds; the current access-token lifetime is 15 minutes. The refresh-token lifetime is 7 days. These values come from `IdentityTokens` in `appsettings.json`.

Use `useCookies=false`. Although the underlying built-in login endpoint supports cookie options, this application's protected routes are configured to authenticate bearer tokens.

### Step 4: Read your profile

Replace `<access-token>` with the login response's access token:

```http
GET https://localhost:7045/auth/profile
Authorization: Bearer <access-token>
```

**Expected:** `200 OK`, for example:

```json
{
  "id": "<generated-user-id>",
  "email": "identity-demo@example.test",
  "emailConfirmed": true,
  "displayName": null
}
```

Without a valid access token, expect `401 Unauthorized`. The endpoint identifies the user from the token; there is no user-ID parameter for reading another account.

### Step 5: Update your display name

```http
PUT https://localhost:7045/auth/profile
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "displayName": "Demo User"
}
```

**Expected:** `200 OK` containing the updated profile. Repeat GET `/auth/profile` to verify persistence.

- Display names can contain at most **100 characters**, validated before trimming.
- Leading/trailing whitespace is trimmed after validation.
- `{"displayName": null}` or a whitespace-only value clears the name. An omitted displayName also clears it; this is not a partial PATCH endpoint.
- More than 100 characters returns `400 Bad Request` without saving the invalid update.
- This endpoint updates only the display name. Email/password changes use `/auth/manage/info`.

### Step 6: Refresh your tokens

You can test refresh immediately; there is no need to wait for access-token expiry.

```http
POST https://localhost:7045/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refresh-token-from-login>"
}
```

**Expected:** `200 OK` with a new `accessToken`, `refreshToken`, `tokenType`, and `expiresIn`. No Authorization header is required.

Replace **both** saved tokens, then call `/auth/profile` with the new access token. Invalid or expired refresh tokens return `401 Unauthorized`; discard unusable tokens and sign in again.

Important behavior:

- Built-in refresh **does not consume the previous refresh token**. It may still work until expiry or security-stamp invalidation. This is not single-use rotation or replay detection.
- Each successful refresh issues a fresh refresh-token lifetime; this implementation has no absolute session-duration limit.
- Password changes/resets invalidate previous refresh tokens through the security stamp. Existing access tokens can remain valid until expiry.
- In a client, serialize refresh requests and atomically replace both tokens. Retry an API call at most once after refresh; do not create an infinite 401/refresh loop.
- There is no bearer logout/revocation endpoint. Local logout discards tokens but does not invalidate stolen copies.

## 5. Optional account-management tests

Run these after the core flow. They change account state. Update your saved email, password, and tokens as appropriate before subsequent tests.

### Read account information

```http
GET https://localhost:7045/auth/manage/info
Authorization: Bearer <access-token>
```

**Expected:** `200 OK` with `email` and `isEmailConfirmed`. Note that the built-in response uses `isEmailConfirmed`, while the custom profile response uses `emailConfirmed`.

### Change email

```http
POST https://localhost:7045/auth/manage/info
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "newEmail": "identity-demo-updated@example.test"
}
```

**Expected:** `200 OK` with account information and a new confirmation link in the Development logs. The old email remains in use until the new address is confirmed.

Follow the complete new link, including its `changedEmail` parameter. Then log in using the new email and your current password, save the new tokens, and verify `/auth/profile`. Update the email in your request client for all later tests.

### Change password while signed in

```http
POST https://localhost:7045/auth/manage/info
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "oldPassword": "Development-Only!234",
  "newPassword": "Development-Only!567"
}
```

**Expected:** `200 OK`; an incorrect old password or invalid new password returns `400 Bad Request`. Sign in again with your current email and new password, then replace both tokens.

To test security-stamp invalidation, retain a pre-change refresh token privately and submit it to `/auth/refresh` after the successful password change. Expect `401 Unauthorized`.

### Reset a forgotten password

Use the account's **current confirmed email**, substituting it below if you changed it:

```http
POST https://localhost:7045/auth/forgotPassword
Content-Type: application/json

{
  "email": "identity-demo@example.test"
}
```

**Expected:** `200 OK`. For a confirmed existing account, the Development logs contain `Development password reset for ...: <reset-code>`. A 200 response does not guarantee an email/code was generated; the endpoint avoids disclosing account existence.

Copy the code exactly, then send:

```http
POST https://localhost:7045/auth/resetPassword
Content-Type: application/json

{
  "email": "identity-demo@example.test",
  "resetCode": "<complete-code-from-logs>",
  "newPassword": "Development-Only!890"
}
```

**Expected:** `200 OK`. Invalid/expired codes or a weak new password return `400 Bad Request`. Log in with the new password and save a new token pair afterward.

### Optional: authenticator-based two-factor authentication

Use a disposable test account and keep recovery codes private so you do not lose access.

1. Send **POST** `/auth/manage/2fa` with a valid bearer token, `Content-Type: application/json`, and body `{}`. Expect `200 OK` with fields including `sharedKey`, `isTwoFactorEnabled`, and `recoveryCodesLeft`.
2. Manually add the `sharedKey` as a time-based account in an authenticator app. This API does not provide an enrollment UI or QR-code page.
3. Send **POST** `/auth/manage/2fa` with the same headers and body `{"enable": true, "twoFactorCode": "<current-authenticator-code>"}`. On success, save any returned recovery codes securely.
4. For later logins, include `twoFactorCode` alongside `email` and `password`. Alternatively, use `twoFactorRecoveryCode` with an unused recovery code; recovery codes are single-use. A password-only login now fails.
5. To disable 2FA for this test account, authenticate and send **POST** `/auth/manage/2fa` with body `{"enable": false}`.

Do not use the placeholder strings as real codes. An invalid enablement code returns a validation error.

## 6. Endpoint reference

All auth paths are relative to `https://localhost:7045`.

| Method | Path | Bearer required? | Request body / query |
| --- | --- | --- | --- |
| POST | `/auth/register` | No | `email`, `password` |
| POST | `/auth/login?useCookies=false` | No | `email`, `password`; 2FA code if enabled |
| POST | `/auth/refresh` | No | `refreshToken` |
| GET | `/auth/confirmEmail` | No | Use the full generated URL with its query parameters |
| POST | `/auth/resendConfirmationEmail` | No | `email` |
| POST | `/auth/forgotPassword` | No | `email` |
| POST | `/auth/resetPassword` | No | `email`, `resetCode`, `newPassword` |
| GET | `/auth/manage/info` | Yes | None |
| POST | `/auth/manage/info` | Yes | `newEmail` or `oldPassword` + `newPassword` |
| POST | `/auth/manage/2fa` | Yes | `{}` to read state; options described above to change it |
| GET | `/auth/profile` | Yes | None |
| PUT | `/auth/profile` | Yes | `displayName` |
| GET | `/weatherforecast` | No | None |
| GET | `/openapi/v1.json` | No | None; Development only |

Use JSON bodies and `Content-Type: application/json` for POST/PUT requests. A browser address bar sends only GET requests; it cannot register or log in a user by opening those URLs.

## 7. PowerShell core workflow

Keep the application running in another terminal. Run the following blocks in the **same PowerShell session** so variables are retained. Use a new test email if you already registered the earlier example account. These requests rely on the trusted development HTTPS certificate; do not disable certificate validation globally.

### Register

```powershell
$baseUrl = 'https://localhost:7045'
$email = "powershell-$([Guid]::NewGuid().ToString('N'))@example.test"
$password = 'Development-Only!234'
$credentials = @{ email = $email; password = $password } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUrl/auth/register" -ContentType 'application/json' -Body $credentials
Write-Host "Registered test email: $email"
```

### Confirm, then log in

Copy the full confirmation URL for this email from the server logs when prompted:

```powershell
$confirmationUrl = Read-Host 'Paste the complete confirmation URL from the application logs'
Invoke-RestMethod -Method Get -Uri $confirmationUrl
$tokens = Invoke-RestMethod -Method Post -Uri "$baseUrl/auth/login?useCookies=false" -ContentType 'application/json' -Body $credentials
$headers = @{ Authorization = "Bearer $($tokens.accessToken)" }
```

### Read and update the profile

```powershell
Invoke-RestMethod -Method Get -Uri "$baseUrl/auth/profile" -Headers $headers
$profileBody = @{ displayName = 'PowerShell Test User' } | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "$baseUrl/auth/profile" -Headers $headers -ContentType 'application/json' -Body $profileBody
Invoke-RestMethod -Method Get -Uri "$baseUrl/auth/profile" -Headers $headers
```

### Refresh and use the new access token

```powershell
$refreshBody = @{ refreshToken = $tokens.refreshToken } | ConvertTo-Json
$tokens = Invoke-RestMethod -Method Post -Uri "$baseUrl/auth/refresh" -ContentType 'application/json' -Body $refreshBody
$headers = @{ Authorization = "Bearer $($tokens.accessToken)" }
Invoke-RestMethod -Method Get -Uri "$baseUrl/auth/profile" -Headers $headers
```

`Invoke-RestMethod` throws on non-success HTTP responses. Use the Visual Studio HTTP editor or Postman to inspect the expected error responses in the next section. These scripts create a persistent test account; they do not delete database data. Avoid saving tokens, confirmation URLs, reset codes, or actual account passwords in committed files or shared transcripts.

## 8. Negative tests and expected results

Use a disposable test account. Do lockout testing last so it does not interrupt the successful workflow.

| Test | Expected result |
| --- | --- |
| GET `/auth/profile` without Authorization | `401 Unauthorized` |
| GET `/auth/profile` with `Authorization: Bearer invalid-token` | `401 Unauthorized` |
| Login with valid credentials before confirming email | `401 Unauthorized` |
| Login with an incorrect password | `401 Unauthorized` |
| Register a duplicate email, invalid email, or weak password | `400 Bad Request` with validation details |
| PUT `/auth/profile` with a 101-character display name and valid token | `400 Bad Request`; previous name remains unchanged |
| PUT `/auth/profile` with a 100-character display name and valid token | `200 OK` |
| Refresh using `{"refreshToken":"invalid-token"}` | `401 Unauthorized` |
| Use an expired access token on `/auth/profile` | `401 Unauthorized`; refresh with a still-valid refresh token |
| Use a refresh token issued before a successful password change/reset | `401 Unauthorized` |
| Reuse a valid refresh token after a successful refresh | May succeed; built-in tokens are not single-use |
| Make five incorrect-password attempts for a confirmed account | Account is locked out for 15 minutes; subsequent login returns 401 during lockout |

For an expiry test, wait until the access token's `expiresIn` duration has elapsed. Do not change the system clock. Keep the refresh token and avoid changing the password during this test.

## 9. Troubleshooting

| Symptom | Check / action |
| --- | --- |
| Connection refused | Confirm the app is running with the `https` profile; check its listening address and startup output. |
| SQL Server connection failure | Confirm the default SQL Server service is running on `localhost`. This configuration does not target LocalDB or a named instance. |
| SQL login or database-creation permission error | In SSMS, connect to `localhost` using Windows Authentication with the app's Windows account; have the required database permissions granted. |
| Missing tables or schema mismatch | `EnsureCreatedAsync` does not migrate an existing database. Resolve the schema with an appropriate deployment/migration plan; do not delete real data. |
| HTTPS certificate error | Run `dotnet dev-certs https --trust`, accept the prompt, and restart the client if needed. The SQL connection's `TrustServerCertificate=True` does not configure browser/API HTTPS trust. |
| No confirmation message in an inbox | Expected: the Development sender logs links rather than sending email. Check the hosting terminal or Visual Studio Debug output. |
| 401 immediately after registration | Confirm the email first, then log in. |
| 401 on a protected request | Check token expiry and the exact `Authorization: Bearer <access-token>` header. Do not use the refresh token as an access token. |
| 401 after enabling 2FA | Include the current authenticator code or an unused recovery code when logging in. |
| 400 validation error | Read the response's `errors` object; check JSON fields, password policy, duplicate email, or display-name length. |
| 405 Method Not Allowed | Use the method in the endpoint table, not a browser GET for a POST/PUT endpoint. |
| 415 Unsupported Media Type | Send the body as JSON with `Content-Type: application/json`. |
| `/swagger` or `/` returns 404 | No Swagger UI or home page is configured; use `Identity.http`, Postman, or the OpenAPI JSON document. |
| Production email-sender startup exception | This implementation deliberately permits only Development until a real email sender is configured. Do not run production as Development to bypass the check. |
| Tokens stop working after deployment | Check that the application's Data Protection keys are persisted, protected, and shared correctly across instances. |

## 10. Local testing versus production

The logged email links/codes, trusted local SQL certificate setting, and automatic fresh-database creation are development conveniences. Before production, configure real email delivery, trusted certificates, reviewed schema deployment, persistent Data Protection keys, rate limiting, and appropriate client token storage. Keep test passwords and tokens out of source control.

This guide documents how to exercise the current application. It does not add a UI, JWT issuance, single-use refresh-token rotation, a logout/revocation endpoint, or a production email provider.
