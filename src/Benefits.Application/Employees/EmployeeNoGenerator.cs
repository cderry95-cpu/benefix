namespace Benefits.Application.Employees;

/// <summary>
/// Generates employee numbers. The format is documented as a team decision
/// in AGENTS.md: "EMP-" plus six zero-padded random digits (e.g. "EMP-048291")
/// — readable and fixed-width, yet deliberately non-sequential so the number
/// leaks nothing about headcount growth.
/// </summary>
public interface IEmployeeNoGenerator
{
    string Generate();
}

public sealed class EmployeeNoGenerator : IEmployeeNoGenerator
{
    private readonly Random _random;

    /// <param name="random">Defaults to the process-wide <see cref="Random.Shared"/>;
    /// tests may inject a seeded instance for deterministic output.</param>
    public EmployeeNoGenerator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public string Generate()
    {
        var digits = _random.Next(0, 1_000_000);
        return $"EMP-{digits:D6}";
    }
}
