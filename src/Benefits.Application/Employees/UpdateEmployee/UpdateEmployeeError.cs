namespace Benefits.Application.Employees;

/// <summary>Why an update-employee request was rejected.</summary>
public enum UpdateEmployeeError
{
    None,
    NotFound,
    MissingRequiredFields,
    InvalidEmail,
    InvalidGender,
    FutureDateOfBirth,
    DuplicateEmail,
}
