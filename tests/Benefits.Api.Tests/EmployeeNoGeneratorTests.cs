using Benefits.Application.Employees;
using Xunit;

namespace Benefits.Api.Tests;

public class EmployeeNoGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSixDigitInteger()
    {
        var generator = new EmployeeNoGenerator();

        var employeeNo = generator.Generate();

        Assert.InRange(employeeNo, 0, 999_999);
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
