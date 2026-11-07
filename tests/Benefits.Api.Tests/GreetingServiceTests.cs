using Benefits.Application;
using Xunit;

namespace Benefits.Api.Tests;

public class GreetingServiceTests
{
    [Fact]
    public void GetGreeting_ReturnsHelloWorld()
    {
        var service = new GreetingService();

        var result = service.GetGreeting();

        Assert.Equal("Hello, World!", result);
    }
}
