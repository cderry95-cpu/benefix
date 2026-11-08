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
