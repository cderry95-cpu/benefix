using Benefits.Application.Helpers;
using Benefits.Domain;
using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Generates a unique <see cref="Employee.EmployeeNo"/>, validates the request
/// via shared employee validation, enforces email uniqueness (checked
/// explicitly and backed by a unique database index) and persists the employee.
/// </summary>
public sealed class AddStarterService(
    IEmployeeRepository repository,
    IEmployeeNoGenerator employeeNoGenerator,
    ILogger<AddStarterService> logger)
    : IAddStarterService
{
    private const int MaxEmployeeNoAttempts = 3;

    public async Task<AddStarterResult> AddStarterAsync(AddStarterRequest request, CancellationToken cancellationToken = default)
    {
        var (fields, failure, message) = EmployeeRequestValidation.Validate(
            request.Email, request.Firstname, request.Surname,
            request.Gender, request.Dob, request.Title, request.Address,
            logContext: "Starter", logger: logger);

        if (failure is not EmployeeValidationFailure.None)
        {
            return AddStarterResult.Failure(MapFailure(failure), message);
        }

        var employeeNo = await ReserveEmployeeNoAsync(cancellationToken);

        if (employeeNo is null)
        {
            var generationMessage = "Could not generate a unique employee number.";
            logger.LogError("Starter not saved: {Message}", generationMessage);
            return AddStarterResult.Failure(AddStarterError.EmployeeNoGenerationFailed, generationMessage);
        }

        // The reserved number has never been persisted, so any existing email
        // owner is necessarily a different employee.
        var otherWithEmail = await repository.GetByEmailAsync(fields!.Email, cancellationToken);

        if (otherWithEmail is not null)
        {
            var duplicateMessage = $"An employee with email '{fields.Email}' already exists.";
            logger.LogWarning("Starter not saved: {Message}", duplicateMessage);
            return AddStarterResult.Failure(AddStarterError.DuplicateEmail, duplicateMessage);
        }

        var employee = fields.ToEmployee(employeeNo!.Value);

        if (!await repository.AddAsync(employee, cancellationToken))
        {
            logger.LogWarning(
                "Starter not saved: the database rejected the insert because email '{Email}' is already in use.",
                employee.Email);
            return AddStarterResult.Failure(
                AddStarterError.DuplicateEmail, $"An employee with email '{employee.Email}' already exists.");
        }

        logger.LogInformation(
            "Starter saved successfully. EmployeeNo={EmployeeNo}, Email={Email}, Name={Firstname} {Surname}.",
            employee.EmployeeNo, employee.Email, employee.Firstname, employee.Surname);

        return AddStarterResult.Success(EmployeeDto.FromDomain(employee));
    }

    /// <summary>
    /// Reserves a random employee number, retrying on the rare collision
    /// (expected roughly once per million inserts). Returns null when the
    /// retry budget is exhausted.
    /// </summary>
    private async Task<int?> ReserveEmployeeNoAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxEmployeeNoAttempts; attempt++)
        {
            var candidate = employeeNoGenerator.Generate();

            if (!await repository.EmployeeNoExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }

            logger.LogWarning(
                "Employee number {EmployeeNo} already exists (generation attempt {Attempt} of {MaxAttempts}).",
                candidate, attempt, MaxEmployeeNoAttempts);
        }

        return null;
    }

    private static AddStarterError MapFailure(EmployeeValidationFailure failure) => failure switch
    {
        EmployeeValidationFailure.MissingRequiredFields => AddStarterError.MissingRequiredFields,
        EmployeeValidationFailure.InvalidEmail => AddStarterError.InvalidEmail,
        EmployeeValidationFailure.InvalidGender => AddStarterError.InvalidGender,
        EmployeeValidationFailure.FutureDateOfBirth => AddStarterError.FutureDateOfBirth,
        _ => AddStarterError.EmployeeNoGenerationFailed,
    };
}
