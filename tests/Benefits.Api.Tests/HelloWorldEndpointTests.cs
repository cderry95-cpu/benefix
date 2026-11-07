using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Benefits.Api.Tests;

public class HelloWorldEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_HelloWorld_ReturnsHelloWorld()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/helloworld");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello, World!", await response.Content.ReadAsStringAsync());
    }
}
