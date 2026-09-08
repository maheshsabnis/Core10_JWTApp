using Core10_JWTApp.Models;

namespace Core10_JWTApp.Patients;

/// <summary>
/// Provides an in-memory, hard-coded set of sample <see cref="Patient"/> records so the
/// Patients API has data to return without requiring a database. Used by
/// <see cref="PatientEndpoints"/> to serve <c>GET /api/patients</c> and <c>GET /api/patients/{id}</c>.
/// </summary>
public static class PatientStore
{
    /// <summary>Read-only collection of sample patients (Indian names) used as the API's default dataset.</summary>
    public static readonly IReadOnlyList<Patient> SamplePatients = new List<Patient>
    {
        new()
        {
            Id = 1,
            Name = "Aarav Sharma",
            Age = 34,
            Gender = "Male",
            Diagnosis = "Hypertension",
            ContactNumber = "9876543210",
            AdmissionDate = new DateOnly(2025, 1, 12)
        },
        new()
        {
            Id = 2,
            Name = "Priya Patel",
            Age = 28,
            Gender = "Female",
            Diagnosis = "Type 2 Diabetes",
            ContactNumber = "9123456780",
            AdmissionDate = new DateOnly(2025, 2, 5)
        },
        new()
        {
            Id = 3,
            Name = "Rohan Mehta",
            Age = 45,
            Gender = "Male",
            Diagnosis = "Fracture - Left Arm",
            ContactNumber = "9988776655",
            AdmissionDate = new DateOnly(2025, 3, 20)
        },
        new()
        {
            Id = 4,
            Name = "Sneha Iyer",
            Age = 31,
            Gender = "Female",
            Diagnosis = "Migraine",
            ContactNumber = "9871234560",
            AdmissionDate = new DateOnly(2025, 4, 2)
        },
        new()
        {
            Id = 5,
            Name = "Vikram Singh",
            Age = 52,
            Gender = "Male",
            Diagnosis = "Coronary Artery Disease",
            ContactNumber = "9012345678",
            AdmissionDate = new DateOnly(2025, 5, 15)
        },
        new()
        {
            Id = 6,
            Name = "Ananya Reddy",
            Age = 22,
            Gender = "Female",
            Diagnosis = "Asthma",
            ContactNumber = "9765432109",
            AdmissionDate = new DateOnly(2025, 6, 8)
        }
    };
}
