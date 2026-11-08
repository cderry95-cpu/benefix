namespace Benefits.Application.Employees;

/// <summary>
/// Generates employee numbers. The format is documented as a team decision
/// in AGENTS.md: an integer of six random digits (0–999,999) — readable, yet
/// deliberately non-sequential so the number leaks nothing about headcount
/// growth.
/// </summary>
public interface IEmployeeNoGenerator
{
    int Generate();
}
