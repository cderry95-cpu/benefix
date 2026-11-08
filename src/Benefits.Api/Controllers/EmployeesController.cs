using Benefits.Application.Employees;
using Microsoft.AspNetCore.Mvc;

namespace Benefits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(
    IAddStarterService addStarterService,
    IListEmployeesService listEmployeesService,
    IGetEmployeeService getEmployeeService,
    IDeleteEmployeeService deleteEmployeeService,
    IUpdateEmployeeService updateEmployeeService,
    ILogger<EmployeesController> logger)
    : ControllerBase
{
    /// <summary>
    /// Lists employees, ordered alphabetically by surname, paginated.
    /// Query params: page (default 1), pageSize (default 10, max 100).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await listEmployeesService.ListEmployeesAsync(new ListEmployeesRequest(page, pageSize), cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.EmployeePage);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Invalid pagination",
            Detail = result.Message,
            Status = StatusCodes.Status400BadRequest,
        });
    }

    /// <summary>
    /// Fetches a single employee by their employee number.
    /// </summary>
    [HttpGet("{employeeNo:int}")]
    public async Task<IActionResult> GetByEmployeeNo(int employeeNo, CancellationToken cancellationToken)
    {
        var result = await getEmployeeService.GetEmployeeAsync(employeeNo, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Employee);
        }

        return NotFound(new ProblemDetails
        {
            Title = "Employee not found",
            Detail = $"No employee with employee number {employeeNo} was found.",
            Status = StatusCodes.Status404NotFound,
        });
    }

    /// <summary>
    /// Permanently deletes an employee by their employee number.
    /// </summary>
    [HttpDelete("{employeeNo:int}")]
    public async Task<IActionResult> DeleteByEmployeeNo(int employeeNo, CancellationToken cancellationToken)
    {
        var result = await deleteEmployeeService.DeleteEmployeeAsync(employeeNo, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return NotFound(new ProblemDetails
        {
            Title = "Employee not found",
            Detail = $"No employee with employee number {employeeNo} was found.",
            Status = StatusCodes.Status404NotFound,
        });
    }

    /// <summary>
    /// Updates an existing employee's details by their employee number.
    /// The employee number cannot be changed.
    /// </summary>
    [HttpPut("{employeeNo:int}")]
    public async Task<IActionResult> Update(int employeeNo, UpdateEmployeeRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            logger.LogWarning("Employee update rejected by the binding layer: a JSON request body is required.");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "A JSON request body is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var result = await updateEmployeeService.UpdateEmployeeAsync(employeeNo, request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Employee);
        }

        return result.Error switch
        {
            UpdateEmployeeError.NotFound => NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"No employee with employee number {employeeNo} was found.",
                Status = StatusCodes.Status404NotFound,
            }),
            UpdateEmployeeError.DuplicateEmail => Conflict(new ProblemDetails
            {
                Title = "Duplicate email",
                Detail = result.Message,
                Status = StatusCodes.Status409Conflict,
            }),
            UpdateEmployeeError.FutureDateOfBirth => BadRequest(new ProblemDetails
            {
                Title = "Invalid date of birth",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            UpdateEmployeeError.InvalidGender => BadRequest(new ProblemDetails
            {
                Title = "Invalid gender",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            UpdateEmployeeError.InvalidEmail => BadRequest(new ProblemDetails
            {
                Title = "Invalid email",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            UpdateEmployeeError.MissingRequiredFields => BadRequest(new ProblemDetails
            {
                Title = "Missing required fields",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            _ => Problem("An unexpected error occurred while updating the employee."),
        };
    }

    /// <summary>Adds a new starter and returns the saved employee.</summary>
    [HttpPost]
    public async Task<IActionResult> AddStarter(AddStarterRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            logger.LogWarning("Starter request rejected by the binding layer: a JSON request body is required.");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "A JSON request body is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var result = await addStarterService.AddStarterAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Employee);
        }

        return result.Error switch
        {
            AddStarterError.DuplicateEmail => Conflict(new ProblemDetails
            {
                Title = "Duplicate email",
                Detail = result.Message,
                Status = StatusCodes.Status409Conflict,
            }),
            AddStarterError.InvalidGender => BadRequest(new ProblemDetails
            {
                Title = "Invalid gender",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            AddStarterError.FutureDateOfBirth => BadRequest(new ProblemDetails
            {
                Title = "Invalid date of birth",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            AddStarterError.InvalidEmail => BadRequest(new ProblemDetails
            {
                Title = "Invalid email",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            AddStarterError.MissingRequiredFields => BadRequest(new ProblemDetails
            {
                Title = "Missing required fields",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest,
            }),
            AddStarterError.EmployeeNoGenerationFailed => Problem(
                "Could not generate a unique employee number. Please retry.",
                statusCode: StatusCodes.Status500InternalServerError),
            _ => Problem("An unexpected error occurred while saving the starter."),
        };
    }
}
