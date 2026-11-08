using Benefits.Application.Employees;
using Benefits.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Benefits.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory SQLite database (single open connection) and
    /// the employee repositories. The database schema is created when the
    /// singleton <see cref="EmployeeDatabase"/> is resolved.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<EmployeeDatabase>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        return services;
    }
}
