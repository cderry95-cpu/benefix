namespace Benefits.Application.Employees;

/// <summary>
/// Generates random employee numbers: six zero-padded digits (e.g. "048291"),
/// tested with a seedable <see cref="Random"/>.
/// </summary>
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
        return $"{digits:D6}";
    }
}
