using Core10_JWTApp.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Core10_JWTApp.Patients;

/// <summary>
/// Defines the secured Patients API. All routes require an authenticated user (bearer token
/// issued by <c>/auth/login</c>) and are mounted under <c>/api/patients</c> by <see cref="MapPatientEndpoints"/>.
/// </summary>
public static class PatientEndpoints
{
    /// <summary>
    /// Registers the <c>/patients</c> route group (secured with <c>RequireAuthorization()</c>) onto the
    /// supplied group, exposing the list and get-by-id endpoints.
    /// </summary>
    public static RouteGroupBuilder MapPatientEndpoints(this RouteGroupBuilder group)
    {
        var patients = group.MapGroup("/patients").RequireAuthorization();
        patients.MapGet("", GetPatients).WithName("GetPatients");
        patients.MapGet("/{id:int}", GetPatientById).WithName("GetPatientById");
        return group;
    }

    /// <summary>Handles <c>GET /api/patients</c>, returning the full sample patient list.</summary>
    private static Ok<IReadOnlyList<Patient>> GetPatients() =>
        TypedResults.Ok(PatientStore.SamplePatients);

    /// <summary>Handles <c>GET /api/patients/{id}</c>, returning a single patient or 404 if none match.</summary>
    private static Results<Ok<Patient>, NotFound> GetPatientById(int id)
    {
        var patient = PatientStore.SamplePatients.FirstOrDefault(p => p.Id == id);
        return patient is null ? TypedResults.NotFound() : TypedResults.Ok(patient);
    }
}
