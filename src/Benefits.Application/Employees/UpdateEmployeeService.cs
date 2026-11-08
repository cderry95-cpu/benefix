using Benefits.Application.Helpers;
using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Use case: update an existing employee's editable fields (everything except
/// <see cref="Domain.Employee.EmployeeNo"/>, which identifies the resource and
/// cannot be changed). Validation rules match adding a starter via the shared
/// request validation, and email uniqueness applies but never to the
/// employee's own number.
/// </summary>
public interface IUpdateEmployeeService
{
    Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        string employeeNo,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateEmployeeService(IEmployeeRepository repository, ILogger<UpdateEmployeeService> logger)
    : IUpdateEmployeeService
{
    public async Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        string employeeNo,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalised = employeeNo.Trim();

        if (string.IsNullOrWhiteSpace(normalised))
        {
            logger.LogWarning("Employee update failed: employee number is empty.");
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.NotFound);
        }

        var existing = await repository.GetByEmployeeNoAsync(normalised, cancellationToken);

        if (existing is null)
        {
            logger.LogWarning("Employee update failed: no employee with employee number '{EmployeeNo}' exists.", normalised);
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.NotFound);
        }

        var (fields, failure, message) = EmployeeRequestValidation.Validate(
            request.Email, request.Firstname, request.Surname,
            request.Gender, request.Dob, request.Title, request.Address,
            logContext: $"Employee '{normalised}' update", logger: logger);

        if (failure is not EmployeeValidationFailure.None)
        {
            return UpdateEmployeeResult.Failure(MapFailure(failure), message);
        }

        var otherWithEmail = await repository.GetByEmailAsync(fields!.Email, cancellationToken);

        if (otherWithEmail is not null && otherWithEmail.EmployeeNo != normalised)
        {
            var duplicateMessage = $"An employee with email '{fields.Email}' already exists.";
            logger.LogWarning("Employee '{EmployeeNo}' update rejected: {Message}", normalised, duplicateMessage);
            return UpdateEmployeeResult.Failure(UpdateEmployeeError.DuplicateEmail, duplicateMessage);
        }

        var updated = fields.ToEmployee(normalised);

        if (!await repository.UpdateAsync(updated, cancellationToken))
        {
            logger.LogWarning(
                "Employee '{EmployeeNo}' update rejected: the employee no longer exists.",
                normalised);
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
