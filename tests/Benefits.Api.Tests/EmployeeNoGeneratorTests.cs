using Benefits.Application.Employees;
using Xunit;

namespace Benefits.Api.Tests;

public class EmployeeNoGeneratorTests
{
    [Fact]
    public void Generate_MatchesEmpFormat()
    {
        var generator = new EmployeeNoGenerator();

        var employeeNo = generator.Generate();

        Assert.Matches(@"^EMP-\d{6}$", employeeNo);
    }

    [Fact]
    public void Generate_TwoSeededValues_AreUnique()
    {
        var generator = new EmployeeNoGenerator(new Random(42));

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first, second);
    }
}
