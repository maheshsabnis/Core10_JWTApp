namespace Core10_JWTApp.Models;

/// <summary>
/// Represents a hospital patient record exposed via the secured Patients API.
/// Used as the data shape for both the in-memory sample data (<see cref="Core10_JWTApp.Patients.PatientStore"/>)
/// and the JSON responses returned by <see cref="Core10_JWTApp.Patients.PatientEndpoints"/>.
/// </summary>
public sealed class Patient
{
    /// <summary>Unique identifier of the patient, used to look up a single record via <c>GET /api/patients/{id}</c>.</summary>
    public int Id { get; set; }

    /// <summary>Full name of the patient.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Patient's age in years.</summary>
    public int Age { get; set; }

    /// <summary>Patient's gender (e.g. "Male", "Female").</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>Medical diagnosis or condition the patient was admitted for.</summary>
    public string Diagnosis { get; set; } = string.Empty;

    /// <summary>Patient's contact phone number.</summary>
    public string ContactNumber { get; set; } = string.Empty;

    /// <summary>Date the patient was admitted.</summary>
    public DateOnly AdmissionDate { get; set; }
}
