using Benefits.Application.Employees;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benefits.Api.Tests;

public class ListEmployeesServiceTests
{
    private readonly FakeEmployeeRepository _repository = new();

    private ListEmployeesService CreateService() =>
        new(_repository, NullLogger<ListEmployeesService>.Instance);

    private static AddStarterRequest ValidRequest(string firstname = "Freddie", string surname = "Mercury") => new()
    {
        Title = "Mr",
        Firstname = firstname,
        Surname = surname,
        Dob = new DateOnly(1988, 9, 5),
        Gender = "Male",
        Email = $"{Guid.NewGuid():N}@example.test",
        Address = "1 Wembley Way, London",
    };

    private async Task<int> SeedEmployeesAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var result = await new AddStarterService(_repository, new EmployeeNoGenerator(), NullLogger<AddStarterService>.Instance)
                .AddStarterAsync(ValidRequest($"First{i:00}", $"Last{i:00}"));

            Assert.True(result.IsSuccess);
        }

        return count;
    }

    [Fact]
    public async Task List_WithoutParams_UsesDefaultPageAndPageSize()
    {
        await SeedEmployeesAsync(3);
        var service = CreateService();

        var result = await service.ListEmployeesAsync(new ListEmployeesRequest(null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.EmployeePage!.Page);
        Assert.Equal(ListEmployeesService.DefaultPageSize, result.EmployeePage.PageSize);
        Assert.Equal(3, result.EmployeePage.TotalCount);
        Assert.Equal(3, result.EmployeePage.Items.Count);
    }

    [Fact]
    public async Task List_PaginationMath_MapsCountAndTotal()
    {
        await SeedEmployeesAsync(25);
        var service = CreateService();

        var page3 = await service.ListEmployeesAsync(new ListEmployeesRequest(3, 5));

        Assert.True(page3.IsSuccess);
        Assert.Equal(5, page3.EmployeePage!.Items.Count);
        Assert.Equal(3, page3.EmployeePage.Page);
        Assert.Equal(5, page3.EmployeePage.PageSize);
        Assert.Equal(25, page3.EmployeePage.TotalCount);

        // Ordinal surname sort ("Last00".."Last24") means ten rows precede
        // page 3, so the first item is the eleventh employee.
        Assert.Equal("First10", page3.EmployeePage.Items[0].Firstname);
    }

    [Fact]
    public async Task List_PastLastPage_ReturnsEmptyPageWithMetadata()
    {
        await SeedEmployeesAsync(5);
        var service = CreateService();

        var result = await service.ListEmployeesAsync(new ListEmployeesRequest(2, 5));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.EmployeePage!.Items);
        Assert.Equal(5, result.EmployeePage.TotalCount);
    }

    [Fact]
    public async Task List_PageZero_ReturnsInvalidPagination()
    {
        await SeedEmployeesAsync(2);
        var service = CreateService();

        var result = await service.ListEmployeesAsync(new ListEmployeesRequest(0, 10));

        Assert.False(result.IsSuccess);
        Assert.Equal(ListEmployeesError.InvalidPagination, result.Error);
        Assert.Contains("page", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, _repository.Saved.Count);
    }

    [Fact]
    public async Task List_PageSizeZero_ReturnsInvalidPagination()
    {
        await SeedEmployeesAsync(2);
        var service = CreateService();

        var result = await service.ListEmployeesAsync(new ListEmployeesRequest(1, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(ListEmployeesError.InvalidPagination, result.Error);
    }

    [Fact]
    public async Task List_PageSizeAboveMax_ReturnsInvalidPagination()
    {
        await SeedEmployeesAsync(2);
        var service = CreateService();

        var result = await service.ListEmployeesAsync(new ListEmployeesRequest(1, ListEmployeesService.MaxPageSize + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ListEmployeesError.InvalidPagination, result.Error);
        Assert.Contains(ListEmployeesService.MaxPageSize.ToString(), result.Message);
    }
}
