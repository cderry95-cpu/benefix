using Benefits.Application.Employees;
using Benefits.Domain;
using Microsoft.Extensions.Logging;

namespace Benefits.Application.Helpers;

/// <summary>Field-level failure shared by the write use cases.</summary>
public enum EmployeeValidationFailure
{
    None,
    MissingRequiredFields,
    InvalidEmail,
    InvalidGender,
    FutureDateOfBirth,
}

/// <summary>
/// Request fields after validation: trimmed, with the gender parsed to the
/// domain enum. <see cref="ToEmployee"/> attaches them to an employee identity
/// so add and update share one construction path.
/// </summary>
public sealed record EmployeeFields(
    string Email,
    string Firstname,
    string Surname,
    DateOnly? Dob,
    Gender? Gender,
    string? Title,
    string? Address)
{
    public Domain.Employee ToEmployee(string employeeNo) => new()
    {
        EmployeeNo = employeeNo,
        Email = Email,
        Firstname = Firstname,
        Surname = Surname,
        Dob = Dob,
        Gender = Gender,
        Title = Title,
        Address = Address,
    };
}

/// <summary>
/// Shared validation for employee write requests (add/update): required
/// fields, email format, allowed gender strings and future-DOB sanity.
/// Both use cases validate identically here, then handle their unique
/// concerns (uniqueness locality, generation, persistence) separately.
/// </summary>
public static class EmployeeRequestValidation
{
    public static (EmployeeFields? Fields, EmployeeValidationFailure Failure, string? Message) Validate(
        string? email,
        string? firstname,
        string? surname,
        string? gender,
        DateOnly? dob,
        string? title,
        string? address,
        string logContext,
        ILogger logger)
    {
        var missing = GetMissingRequiredFields(email, firstname, surname);

        if (missing.Count is not 0)
        {
            var message = $"The following required fields are missing or empty: {string.Join(", ", missing)}.";
            logger.LogWarning("{LogContext} rejected: {Message}", logContext, message);
            return (null, EmployeeValidationFailure.MissingRequiredFields, message);
        }

        var trimmedEmail = email!.Trim();

        if (!IsValidEmail(trimmedEmail))
        {
            var message = $"'{trimmedEmail}' is not a valid email address.";
            logger.LogWarning("{LogContext} rejected: {Message}", logContext, message);
            return (null, EmployeeValidationFailure.InvalidEmail, message);
        }

        Gender? parsedGender = null;

        if (!string.IsNullOrWhiteSpace(gender))
        {
            if (!GenderParser.TryParse(gender, out var genderValue))
            {
                var allowed = string.Join(", ", GenderParser.AllowedValues);
                var message = $"'{gender.Trim()}' is not a valid gender. Allowed values are: {allowed}.";
                logger.LogWarning("{LogContext} rejected: {Message}", logContext, message);
                return (null, EmployeeValidationFailure.InvalidGender, message);
            }

            parsedGender = genderValue;
        }

        if (dob is { } dobValue && dobValue > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            var message = "Date of birth cannot be in the future.";
            logger.LogWarning("{LogContext} rejected: {Message} (DOB={Dob}).", logContext, message, dobValue);
            return (null, EmployeeValidationFailure.FutureDateOfBirth, message);
        }

        return (new EmployeeFields(
            Email: trimmedEmail,
            Firstname: firstname!.Trim(),
            Surname: surname!.Trim(),
            Dob: dob,
            Gender: parsedGender,
            Title: title,
            Address: address), EmployeeValidationFailure.None, null);
    }

    private static List<string> GetMissingRequiredFields(string? email, string? firstname, string? surname)
    {
        var missing = new List<string>(3);

        if (string.IsNullOrWhiteSpace(email))
        {
            missing.Add("Email");
        }

        if (string.IsNullOrWhiteSpace(firstname))
        {
            missing.Add("Firstname");
        }

        if (string.IsNullOrWhiteSpace(surname))
        {
            missing.Add("Surname");
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
}
