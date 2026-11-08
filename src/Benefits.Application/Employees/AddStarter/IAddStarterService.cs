namespace Benefits.Application.Employees;

/// <summary>Use case: add a new starter.</summary>
public interface IAddStarterService
{
    Task<AddStarterResult> AddStarterAsync(AddStarterRequest request, CancellationToken cancellationToken = default);
}
