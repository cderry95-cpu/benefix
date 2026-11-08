using Benefits.Application.Helpers;
using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Validation rules match adding a starter via the shared request validation,
/// and email uniqueness applies but never to the employee's own number. The
/// employee number is captured from the route, never from the request, so it
/// is the one field an update cannot change.
/// </summary>
public sealed class UpdateEmployeeService(IEmployeeRepository repository, ILogger<UpdateEmployeeService> logger)
    : IUpdateEmployeeService
{
    public async Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        int employeeNo,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByEmployeeNoAsync(employeeNo, cancellationToken);

        if (existing is null)
        {
            logger.LogWarning("Employee update failed: no employee with employee number {EmployeeNo} exists.", employeeNo);
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.NotFound);
        }

        var (fields, failure, message) = EmployeeRequestValidation.Validate(
            request.Email, request.Firstname, request.Surname,
            request.Gender, request.Dob, request.Title, request.Address,
            logContext: $"Employee {employeeNo} update", logger: logger);

        if (failure is not EmployeeValidationFailure.None)
        {
            return UpdateEmployeeResult.Failure(MapFailure(failure), message);
        }

        var otherWithEmail = await repository.GetByEmailAsync(fields!.Email, cancellationToken);

        if (otherWithEmail is not null && otherWithEmail.EmployeeNo != employeeNo)
        {
            var duplicateMessage = $"An employee with email '{fields.Email}' already exists.";
            logger.LogWarning("Employee {EmployeeNo} update rejected: {Message}", employeeNo, duplicateMessage);
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.DuplicateEmail, duplicateMessage);
        }

        var updated = fields.ToEmployee(employeeNo);

        if (!await repository.UpdateAsync(updated, cancellationToken))
        {
            logger.LogWarning(
                "Employee {EmployeeNo} update rejected: the employee no longer exists.",
                employeeNo);
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.NotFound);
        }

        logger.LogInformation(
            "Employee updated. EmployeeNo={EmployeeNo}, Email={Email}, Name={Firstname} {Surname}.",
            updated.EmployeeNo, updated.Email, updated.Firstname, updated.Surname);

        return UpdateEmployeeResult.Success(EmployeeDto.FromDomain(updated));
    }

    private static UpdateEmployeeError MapFailure(EmployeeValidationFailure failure) => failure switch
    {
        EmployeeValidationFailure.MissingRequiredFields => UpdateEmployeeError.MissingRequiredFields,
        EmployeeValidationFailure.InvalidEmail => UpdateEmployeeError.InvalidEmail,
        EmployeeValidationFailure.InvalidGender => UpdateEmployeeError.InvalidGender,
        EmployeeValidationFailure.FutureDateOfBirth => UpdateEmployeeError.FutureDateOfBirth,
        _ => UpdateEmployeeError.NotFound,
    };
}
