using Benefits.Application.Employees;
using Microsoft.AspNetCore.Mvc;

namespace Benefits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IAddStarterService addStarterService, ILogger<EmployeesController> logger)
    : ControllerBase
{
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
            _ => Problem("An unexpected error occurred while saving the starter."),
        };
    }
}
