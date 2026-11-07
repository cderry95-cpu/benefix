using Benefits.Application;
using Microsoft.AspNetCore.Mvc;

namespace Benefits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloWorldController(IGreetingService greetingService) : ControllerBase
{
    [HttpGet]
    public string Get() => greetingService.GetGreeting();
}
