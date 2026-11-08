namespace Benefits.Application.Employees;

/// <summary>Why an add-starter request was rejected.</summary>
public enum AddStarterError
{
    None,
    MissingRequiredFields,
    InvalidEmail,
    InvalidGender,
    FutureDateOfBirth,
    DuplicateEmail,
    EmployeeNoGenerationFailed,
}
