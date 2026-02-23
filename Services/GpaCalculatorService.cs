namespace SchoolEduERP.Services;

public interface IGpaCalculatorService
{
    decimal CalculateGpa(IEnumerable<(decimal marks, int credits)> entries, int totalMarks = 100);
    (string letterGrade, decimal gradePoint) GetGrade(decimal marks, int totalMarks = 100);
    decimal CalculateWeightedGpa(IEnumerable<(decimal gradePoint, int credits)> entries);
}

public class GpaCalculatorService : IGpaCalculatorService
{
    // Grade boundaries: Letter → (minPercentage, gradePoint)
    private static readonly (string letter, decimal minPct, decimal gp)[] GradeTable =
    {
        ("A+", 90, 4.0m),
        ("A",  80, 3.7m),
        ("B+", 70, 3.3m),
        ("B",  60, 3.0m),
        ("C+", 50, 2.5m),
        ("C",  40, 2.0m),
        ("D",  33, 1.5m),
        ("F",   0, 0.0m),
    };

    public (string letterGrade, decimal gradePoint) GetGrade(decimal marks, int totalMarks = 100)
    {
        var pct = totalMarks > 0 ? (marks / totalMarks) * 100 : 0;
        foreach (var (letter, minPct, gp) in GradeTable)
        {
            if (pct >= minPct)
                return (letter, gp);
        }
        return ("F", 0.0m);
    }

    public decimal CalculateGpa(IEnumerable<(decimal marks, int credits)> entries, int totalMarks = 100)
    {
        decimal totalWeighted = 0;
        int totalCredits = 0;

        foreach (var (marks, credits) in entries)
        {
            var (_, gp) = GetGrade(marks, totalMarks);
            totalWeighted += gp * credits;
            totalCredits += credits;
        }

        return totalCredits > 0 ? Math.Round(totalWeighted / totalCredits, 2) : 0;
    }

    public decimal CalculateWeightedGpa(IEnumerable<(decimal gradePoint, int credits)> entries)
    {
        decimal totalWeighted = 0;
        int totalCredits = 0;

        foreach (var (gp, credits) in entries)
        {
            totalWeighted += gp * credits;
            totalCredits += credits;
        }

        return totalCredits > 0 ? Math.Round(totalWeighted / totalCredits, 2) : 0;
    }
}
