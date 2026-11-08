using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Benefits.Api.Json;
using Benefits.Application.Employees;
using Benefits.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Benefits.Api.Tests;

public class EmployeesEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>Mirrors the API's JSON options so test-side deserialization
    /// accepts the same enum wire names the API produces.</summary>
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(namingPolicy: new TitleCaseEnumNamingPolicy()) },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static AddStarterRequest ValidRequest() => new()
    {
        Title = "Mr",
        Firstname = "Freddie",
        Surname = "Mercury",
        Dob = new DateOnly(1988, 9, 5),
        Gender = "Prefer not to say",
        Email = $"{Guid.NewGuid():N}@example.test",
        Address = "1 Wembley Way, London",
    };

    private async Task<HttpResponseMessage> PostJsonAsync(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/api/employees", content);
    }

    private async Task<EmployeeDto?> ReadEmployeeAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<EmployeeDto>(ApiJsonOptions);
    }

    [Fact]
    public async Task Post_ValidStarter_ReturnsSavedEmployee()
    {
        var request = ValidRequest();

        var response = await _client.PostAsJsonAsync("/api/employees", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await ReadEmployeeAsync(response);
        Assert.NotNull(employee);
        Assert.True(Guid.TryParse(employee!.EmployeeNo, out _));
        Assert.Equal(request.Email, employee.Email);
        Assert.Equal("Freddie", employee.Firstname);
        Assert.Equal(request.Dob, employee.Dob);
        Assert.Equal(Gender.PreferNotToSay, employee.Gender);
    }

    [Fact]
    public async Task Post_DuplicateEmail_ReturnsConflict()
    {
        var request = ValidRequest();
        await _client.PostAsJsonAsync("/api/employees", request);

        var response = await _client.PostAsJsonAsync("/api/employees", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsBadRequest()
    {
        var request = ValidRequest() with { Email = "not-an-email" };

        var response = await _client.PostAsJsonAsync("/api/employees", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingEmail_ReturnsBadRequest()
    {
        var response = await PostJsonAsync("""{ "title": "Mr" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingFirstname_ReturnsBadRequest()
    {
        var response = await PostJsonAsync(
            $$"""{ "email": "{{Guid.NewGuid():N}}@example.test", "surname": "Mercury" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingSurname_ReturnsBadRequest()
    {
        var response = await PostJsonAsync(
            $$"""{ "email": "{{Guid.NewGuid():N}}@example.test", "firstname": "Freddie" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_FutureDob_ReturnsBadRequest()
    {
        var futureDob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var response = await PostJsonAsync(
            $$"""{ "email": "{{Guid.NewGuid():N}}@example.test", "firstname": "Freddie", "surname": "Mercury", "dob": "{{futureDob:O}}" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Invalid date of birth", problem!.Title);
        Assert.Contains("future", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_GenderAsSpacedString_RoundTrips()
    {
        var response = await PostJsonAsync(
            $$"""{ "email": "{{Guid.NewGuid():N}}@example.test", "firstname": "Freddie", "surname": "Mercury", "gender": "Prefer not to say" }""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await ReadEmployeeAsync(response);
        Assert.Equal(Gender.PreferNotToSay, employee!.Gender);
    }

    [Fact]
    public async Task Post_InvalidGender_ReturnsBadRequestListingAllowedValues()
    {
        var response = await PostJsonAsync(
            $$"""{ "email": "{{Guid.NewGuid():N}}@example.test", "firstname": "Freddie", "surname": "Mercury", "gender": "Banana" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Invalid gender", problem!.Title);
        Assert.Contains("Banana", problem.Detail);
        Assert.Contains("Male, Female, Other, Prefer not to say", problem.Detail);
    }
}
