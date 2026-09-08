# ASP.NET Core 10 Identity API

This project uses ASP.NET Core Identity's **built-in opaque bearer tokens**, not JWTs. Tokens are protected with ASP.NET Core Data Protection and are intended for this application's own clients. They cannot be decoded as JWTs or validated with JwtBearer. The original weather endpoint remains public.

## Run locally

1. Install the .NET 10 SDK and SQL Server Express LocalDB (available through the Visual Studio installer).
2. Select the `https` launch profile in Visual Studio, or run `dotnet run --project Core10_JWTApp --launch-profile https` from the solution directory. Trust the development HTTPS certificate if prompted (`dotnet dev-certs https --trust`).
3. Development startup creates `Core10_JWTApp_Identity` in `(localdb)\mssqllocaldb` if it does not exist. The database is persistent across restarts. This requires permission to create a database.
4. Open `Core10_JWTApp/Identity.http` and run the registration request. Use a unique email for each new user.
5. Copy the **complete confirmation URL** from the application console / Visual Studio debug output into `confirmationUrl` and execute it. Email is not actually sent; the Development logger also logs password-reset codes. Keep those logs private.
6. Log in with `useCookies=false`. Copy `accessToken` and `refreshToken` from the response into the HTTP file variables. Do not commit real tokens.
7. Call the protected profile and account-management endpoints. Run the email/password-change examples independently, updating your credentials afterward.

Override `ConnectionStrings:IdentityDatabase` with user secrets or the `ConnectionStrings__IdentityDatabase` environment variable for another SQL Server instance. Only Development has a default LocalDB connection. Keep production passwords out of committed settings.

## Endpoints

All routes below start with `/auth`.

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/register` | Register with `email` and `password`; produces a confirmation email |
| POST | `/login?useCookies=false` | Return `tokenType`, `accessToken`, `expiresIn`, and `refreshToken` after email confirmation |
| POST | `/refresh` | Exchange `{ "refreshToken": "..." }` for a new token pair |
| GET | `/confirmEmail` | Follow the generated link with `userId`, `code`, and optional `changedEmail` |
| POST | `/resendConfirmationEmail` | Resend confirmation using `email` |
| POST | `/forgotPassword` | Request a password reset using `email` |
| POST | `/resetPassword` | Reset using `email`, `resetCode`, and `newPassword` |
| GET | `/manage/info` | Read email and confirmation state (bearer required) |
| POST | `/manage/info` | Change `newEmail` or `newPassword` plus `oldPassword` (bearer required) |
| POST | `/manage/2fa` | Built-in authenticator and recovery-code management (bearer required) |
| GET | `/profile` | Read ID, email, confirmation state, and display name (bearer required) |
| PUT | `/profile` | Set `displayName`, at most 100 characters; null/whitespace clears it (bearer required) |

Built-in Identity handles password hashing, account lockout, confirmation, password reset, and account management. Login requires a confirmed email. Five failed password attempts trigger a 15-minute lockout. Profile endpoints always derive the user ID from the authenticated principal; callers cannot select another user's profile.

## Refresh-token behavior

- `IdentityTokens:AccessTokenMinutes` defaults to **15**; `RefreshTokenDays` defaults to **7**. Settings must be positive and refresh lifetime must exceed access lifetime.
- Send access tokens as `Authorization: Bearer <accessToken>` over HTTPS. Do not send refresh tokens to ordinary API endpoints.
- Before access expiry (using the response's `expiresIn`), or after one authentication failure, call `/auth/refresh`. No current access token is required.
- On success, atomically replace **both** stored tokens. Serialize concurrent refresh requests in the client. Retry a failed API call at most once; if refresh returns 401, discard tokens and sign in again.
- Expired, malformed, or security-stamp-invalidated refresh tokens are rejected by Identity. Password changes/resets invalidate previously issued refresh tokens through the security stamp. Existing access tokens can remain valid until their expiry.
- A successful refresh gives a fresh refresh-token lifetime. **The previous refresh token is not consumed.** Built-in Identity does not provide single-use rotation, replay detection, a token revocation database, or an absolute session-duration limit. Do not rely on it for those guarantees.
- There is no bearer logout/revocation endpoint in this implementation. Client logout discards local tokens; stolen copies are not thereby invalidated.
- Use platform-protected storage for native clients. For browser applications, consider an HttpOnly-cookie/BFF design instead of storing long-lived bearer credentials in JavaScript-accessible storage. This implementation authenticates protected routes with the bearer scheme; cookie login is not its supported client flow.

## Files

- `Identity/ApplicationUser.cs`: Identity user with a display name.
- `Identity/ApplicationDbContext.cs`: Identity EF Core SQL Server persistence.
- `Identity/IdentityServiceExtensions.cs`: Identity, bearer lifetimes, lockout, and service registration.
- `Identity/DevelopmentEmailSender.cs`: environment-guarded confirmation/reset logging.
- `Identity/ProfileEndpoints.cs`: protected, validated profile endpoints.
- `Identity.http`: manual workflow and negative-case requests.

## Before production

- Configure the SQL Server connection and replace the Development email-sender registration **and its fail-fast environment check** in `IdentityServiceExtensions` with a real `IEmailSender<ApplicationUser>`. Non-Development startup is deliberately rejected until that is done. Never bypass this by running production as Development.
- Development uses `EnsureCreatedAsync`, not migrations. It creates a fresh schema but does not upgrade existing databases. Establish reviewed EF Core migrations/deployment scripts before production; an `EnsureCreated` database cannot simply switch to migrations without a migration/baseline strategy. Production must not use automatic schema creation. Do not delete an existing database containing real data to resolve schema mismatches.
- Persist and protect the Data Protection key ring, and share it with a stable application name across replicas. Without the right keys, issued access/refresh tokens and confirmation links cannot be read after deployment or by another instance.
- Use HTTPS, trusted proxy/forwarded-header configuration, a trusted public host for email links, an explicit CORS policy where needed, and rate limiting for authentication endpoints. Do not log passwords or tokens in production.
- If interoperable JWTs, single-use refresh-token rotation, revocation, or federated sign-in are required, use a suitable OAuth/OIDC identity provider or a separately designed token service instead of these built-in opaque tokens.
