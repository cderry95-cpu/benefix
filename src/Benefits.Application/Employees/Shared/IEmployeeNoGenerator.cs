namespace Benefits.Application.Employees;

/// <summary>
/// Generates employee numbers. The format is documented as a team decision
/// in AGENTS.md: six random zero-padded digits (e.g. "048291") — readable,
/// fixed-width, yet deliberately non-sequential so the number leaks nothing
/// about headcount growth.
/// </summary>
public interface IEmployeeNoGenerator
{
    string Generate();
}
