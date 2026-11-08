using Benefits.Domain;
using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Use case: add a new starter. Generates a unique <see cref="Employee.EmployeeNo"/>,
/// validates the request, enforces email uniqueness (checked explicitly and backed
/// by a unique database index) and persists the employee.
/// </summary>
public interface IAddStarterService
{
    Task<AddStarterResult> AddStarterAsync(AddStarterRequest request, CancellationToken cancellationToken = default);
}

public sealed class AddStarterService(
    IEmployeeRepository repository,
    IEmployeeNoGenerator employeeNoGenerator,
    ILogger<AddStarterService> logger)
    : IAddStarterService
{
    private const int MaxEmployeeNoAttempts = 3;

    public async Task<AddStarterResult> AddStarterAsync(AddStarterRequest request, CancellationToken cancellationToken = default)
    {
        var missingFields = GetMissingRequiredFields(request);

        if (missingFields.Count is not 0)
        {
            var missing = string.Join(", ", missingFields);
            var message = $"The following required fields are missing or empty: {missing}.";
            logger.LogWarning("Starter not saved: {Message}", message);
            return AddStarterResult.Failure(AddStarterError.MissingRequiredFields, message);
        }

        var email = request.Email!.Trim();

        if (!IsValidEmail(email))
        {
            logger.LogWarning("Starter not saved: '{Email}' is not a valid email address.", email);
            return AddStarterResult.Failure(
                AddStarterError.InvalidEmail, $"'{email}' is not a valid email address.");
        }

        if (request.Dob is { } dob && dob > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            const string message = "Date of birth cannot be in the future.";
            logger.LogWarning("Starter not saved: {Message} (DOB={Dob}).", message, dob);
            return AddStarterResult.Failure(AddStarterError.FutureDateOfBirth, message);
        }

        Gender? gender = null;

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (!GenderParser.TryParse(request.Gender, out var parsedGender))
            {
                var allowed = string.Join(", ", GenderParser.AllowedValues);
                var message =
                    $"'{request.Gender.Trim()}' is not a valid gender. Allowed values are: {allowed}.";
                logger.LogWarning("Starter not saved: {Message}", message);
                return AddStarterResult.Failure(AddStarterError.InvalidGender, message);
            }

            gender = parsedGender;
        }

        var employeeNo = await ReserveEmployeeNoAsync(cancellationToken);

        if (employeeNo is null)
        {
            const string message = "Could not generate a unique employee number.";
            logger.LogError("Starter not saved: {Message}", message);
            return AddStarterResult.Failure(AddStarterError.EmployeeNoGenerationFailed, message);
        }

        if (await repository.EmailExistsAsync(email, cancellationToken))
        {
            logger.LogWarning("Starter not saved: an employee with email '{Email}' already exists.", email);
            return AddStarterResult.Failure(
                AddStarterError.DuplicateEmail, $"An employee with email '{email}' already exists.");
        }

        var employee = new Employee
        {
            EmployeeNo = employeeNo,
            Email = email,
            Firstname = request.Firstname!.Trim(),
            Surname = request.Surname!.Trim(),
            Title = request.Title,
            Dob = request.Dob,
            Gender = gender,
            Address = request.Address,
        };

        if (!await repository.AddAsync(employee, cancellationToken))
        {
            logger.LogWarning(
                "Starter not saved: the database rejected the insert because email '{Email}' is already in use.",
                email);
            return AddStarterResult.Failure(
                AddStarterError.DuplicateEmail, $"An employee with email '{email}' already exists.");
        }

        logger.LogInformation(
            "Starter saved successfully. EmployeeNo={EmployeeNo}, Email={Email}, Name={Firstname} {Surname}.",
            employee.EmployeeNo, employee.Email, employee.Firstname, employee.Surname);

        return AddStarterResult.Success(ToDto(employee));
    }

    /// <summary>
    /// Reserves a random employee number, retrying on the rare collision
    /// (expected roughly once per million inserts). Returns null when the
    /// retry budget is exhausted.
    /// </summary>
    private async Task<string?> ReserveEmployeeNoAsync(CancellationToken cancellationToken)
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

    private static List<string> GetMissingRequiredFields(AddStarterRequest request)
    {
        var missing = new List<string>(3);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            missing.Add(nameof(AddStarterRequest.Email));
        }

        if (string.IsNullOrWhiteSpace(request.Firstname))
        {
            missing.Add(nameof(AddStarterRequest.Firstname));
        }

        if (string.IsNullOrWhiteSpace(request.Surname))
        {
            missing.Add(nameof(AddStarterRequest.Surname));
        }

        return missing;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new System.Net.Mail.MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static EmployeeDto ToDto(Employee employee) => new()
    {
        EmployeeNo = employee.EmployeeNo,
        Email = employee.Email,
        Title = employee.Title,
        Firstname = employee.Firstname,
        Surname = employee.Surname,
        Dob = employee.Dob,
        Gender = employee.Gender,
        Address = employee.Address,
    };
}
