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
        Assert.Matches(@"^\d{6}$", employee!.EmployeeNo);
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

    [Fact]
    public async Task Get_List_ReturnsValidPageShape()
    {
        var response = await _client.GetAsync("/api/employees?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EmployeePage>(ApiJsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page!.Page);
        Assert.Equal(5, page.PageSize);
        Assert.True(page.Items.Count <= 5);
        Assert.True(page.TotalCount >= page.Items.Count);
        AssertSortedAlphabetically(page.Items);
    }

    [Fact]
    public async Task Get_List_PostedEmployeeIsOrderedAlphabetically()
    {
        // A surname deliberately sorting after every other record (including
        // seeded ones) makes this test independent of seed data and order.
        var request = ValidRequest() with
        {
            Firstname = "Zoe",
            Surname = "Zzyzx",
        };
        var postResponse = await _client.PostAsJsonAsync("/api/employees", request);
        var posted = await ReadEmployeeAsync(postResponse);

        var response = await _client.GetAsync("/api/employees?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EmployeePage>(ApiJsonOptions);
        AssertSortedAlphabetically(page!.Items);
        Assert.Equal(posted!.EmployeeNo, page.Items[^1].EmployeeNo);
    }

    [Fact]
    public async Task Get_List_PastLastPage_ReturnsEmptyItems()
    {
        var response = await _client.GetAsync("/api/employees?page=9999&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EmployeePage>(ApiJsonOptions);
        Assert.Empty(page!.Items);
        Assert.Equal(10, page.PageSize);
    }

    [Theory]
    [InlineData("page=0&pageSize=10")]
    [InlineData("page=1&pageSize=0")]
    [InlineData("page=1&pageSize=101")]
    public async Task Get_List_InvalidPaginationParams_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"/api/employees?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Invalid pagination", problem!.Title);
        Assert.NotNull(problem.Detail);
    }

    private static void AssertSortedAlphabetically(IReadOnlyList<EmployeeDto> items)
    {
        for (var i = 1; i < items.Count; i++)
        {
            var previous = (items[i - 1].Surname, items[i - 1].Firstname, items[i - 1].Email);
            Assert.True(
                string.CompareOrdinal(previous.Item1 ?? string.Empty, items[i].Surname ?? string.Empty) <= 0);
        }
    }

    [Fact]
    public async Task Get_ByEmployeeNo_ReturnsSavedEmployee()
    {
        var request = ValidRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/employees", request);
        var posted = await ReadEmployeeAsync(postResponse);

        var response = await _client.GetAsync($"/api/employees/{posted!.EmployeeNo}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await ReadEmployeeAsync(response);
        Assert.NotNull(employee);
        Assert.Equal(posted.EmployeeNo, employee!.EmployeeNo);
        Assert.Equal(request.Email, employee.Email);
        Assert.Equal("Freddie", employee.Firstname);
        Assert.Equal(request.Dob, employee.Dob);
        Assert.Equal(Gender.PreferNotToSay, employee.Gender);
    }

    [Fact]
    public async Task Get_ByEmployeeNo_UnknownNumber_ReturnsNotFound()
    {
        // Pick a number guaranteed absent by checking the known population:
        // fetch everything (max page size 100 covers this suite's row count)
        // and try random candidates until one is missing.
        var existing = (await _client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=100", ApiJsonOptions))!
            .Items.Select(e => e.EmployeeNo).ToHashSet();

        string unknown;
        do
        {
            unknown = $"{Random.Shared.Next(0, 1_000_000):D6}";
        }
        while (existing.Contains(unknown));

        var response = await _client.GetAsync($"/api/employees/{unknown}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Employee not found", problem!.Title);
        Assert.Contains(unknown, problem.Detail);
    }

    /// <summary>
    /// Posts a new starter and returns its (employee number, request) pair so
    /// update/delete tests have a known subject.
    /// </summary>
    private async Task<(EmployeeDto Posted, AddStarterRequest Request)> PostOneAsync(string? email = null)
    {
        var request = ValidRequest() with { Email = email ?? $"{Guid.NewGuid():N}@example.test" };
        var postResponse = await _client.PostAsJsonAsync("/api/employees", request);
        var posted = await ReadEmployeeAsync(postResponse);
        Assert.NotNull(posted);
        return (posted!, request);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_UpdatesFieldsKeepsEmployeeNo()
    {
        var (posted, _) = await PostOneAsync();

        var changedRequest = new
        {
            title = "Mrs",
            firstname = "Farrokh",
            surname = "Bulsara",
            dob = "1989-01-01",
            gender = "Prefer not to say",
            email = $"updated.{Guid.NewGuid():N}@example.test",
            address = "9 New Road, London",
        };
        var response = await _client.PutAsJsonAsync($"/api/employees/{posted.EmployeeNo}", changedRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await ReadEmployeeAsync(response);
        Assert.NotNull(employee);
        Assert.Equal(posted.EmployeeNo, employee!.EmployeeNo);
        Assert.Equal(changedRequest.firstname, employee.Firstname);
        Assert.Equal(changedRequest.surname, employee.Surname);
        Assert.Equal(changedRequest.email, employee.Email);
        Assert.Equal(DateOnly.Parse(changedRequest.dob), employee.Dob);
        Assert.Equal(Gender.PreferNotToSay, employee.Gender);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_PersistsChange()
    {
        var (posted, _) = await PostOneAsync();

        var changedEmail = $"persistent.{Guid.NewGuid():N}@example.test";
        await _client.PutAsJsonAsync($"/api/employees/{posted.EmployeeNo}", new
        {
            firstname = "Persist",
            surname = "Test",
            email = changedEmail,
        });

        var getResponse = await _client.GetAsync($"/api/employees/{posted.EmployeeNo}");
        var persisted = await ReadEmployeeAsync(getResponse);
        Assert.Equal(changedEmail, persisted!.Email);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_UnknownNumber_ReturnsNotFound()
    {
        var existing = (await _client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=100", ApiJsonOptions))!
            .Items.Select(e => e.EmployeeNo).ToHashSet();

        string unknown;
        do
        {
            unknown = $"{Random.Shared.Next(0, 1_000_000):D6}";
        }
        while (existing.Contains(unknown));

        var response = await _client.PutAsJsonAsync($"/api/employees/{unknown}", new
        {
            firstname = "Ghost",
            surname = "Starter",
            email = $"{Guid.NewGuid():N}@example.test",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Employee not found", problem!.Title);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_DuplicateEmail_ReturnsConflict()
    {
        var (first, _) = await PostOneAsync();
        var (second, _) = await PostOneAsync();

        var response = await _client.PutAsJsonAsync($"/api/employees/{first.EmployeeNo}", new
        {
            firstname = "Clash",
            surname = "Starter",
            email = second.Email,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Duplicate email", problem!.Title);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_MissingRequiredFields_ReturnsBadRequest()
    {
        var (posted, _) = await PostOneAsync();

        var response = await _client.PutAsJsonAsync($"/api/employees/{posted.EmployeeNo}", new
        {
            firstname = "NoSurname",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Missing required fields", problem!.Title);
        Assert.Contains("Surname", problem.Detail);
    }

    [Fact]
    public async Task Put_ByEmployeeNo_EmployeeNoNotInBody_CannotChangeNumber()
    {
        // The request DTO has no employee number property; update only ever
        // targets the number in the route. This test pins the shape contract:
        // a body containing an employeeNo field is ignored by the DTO.
        var (posted, _) = await PostOneAsync();

        var response = await _client.PutAsJsonAsync($"/api/employees/{posted.EmployeeNo}", new
        {
            employeeNo = "999999",
            firstname = "Shape",
            surname = "Test",
            email = $"{Guid.NewGuid():N}@example.test",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await ReadEmployeeAsync(response);
        Assert.Equal(posted.EmployeeNo, employee!.EmployeeNo);
    }

    [Fact]
    public async Task Delete_ByEmployeeNo_RemovesEmployee()
    {
        var request = ValidRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/employees", request);
        var posted = await ReadEmployeeAsync(postResponse);

        var response = await _client.DeleteAsync($"/api/employees/{posted!.EmployeeNo}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/employees/{posted.EmployeeNo}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ByEmployeeNo_DeletionPersists()
    {
        var before = (await _client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=100", ApiJsonOptions))!.TotalCount;

        var postResponse = await _client.PostAsJsonAsync("/api/employees", ValidRequest());
        var posted = await ReadEmployeeAsync(postResponse);

        await _client.DeleteAsync($"/api/employees/{posted!.EmployeeNo}");

        var after = (await _client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=100", ApiJsonOptions))!.TotalCount;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Delete_ByEmployeeNo_AlreadyDeleted_ReturnsNotFound()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/employees", ValidRequest());
        var posted = await ReadEmployeeAsync(postResponse);

        var first = await _client.DeleteAsync($"/api/employees/{posted!.EmployeeNo}");
        var second = await _client.DeleteAsync($"/api/employees/{posted.EmployeeNo}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task Delete_ByEmployeeNo_UnknownNumber_ReturnsNotFound()
    {
        var existing = (await _client.GetFromJsonAsync<EmployeePage>("/api/employees?pageSize=100", ApiJsonOptions))!
            .Items.Select(e => e.EmployeeNo).ToHashSet();

        string unknown;
        do
        {
            unknown = $"{Random.Shared.Next(0, 1_000_000):D6}";
        }
        while (existing.Contains(unknown));

        var response = await _client.DeleteAsync($"/api/employees/{unknown}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJsonOptions);
        Assert.Equal("Employee not found", problem!.Title);
    }
}
