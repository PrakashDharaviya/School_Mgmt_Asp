using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Services;

public interface IAcademicYearService
{
    Task<AcademicYear?> GetActiveYearAsync();
    Task<IEnumerable<AcademicYear>> GetAllYearsAsync();
    Task SetActiveYearAsync(int yearId);
    Task<AcademicYear> CreateYearAsync(string name, DateTime start, DateTime end);
}

public class AcademicYearService : IAcademicYearService
{
    private readonly ApplicationDbContext _context;

    public AcademicYearService(ApplicationDbContext context) => _context = context;

    public async Task<AcademicYear?> GetActiveYearAsync()
        => await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);

    public async Task<IEnumerable<AcademicYear>> GetAllYearsAsync()
        => await _context.AcademicYears.OrderByDescending(a => a.StartDate).ToListAsync();

    public async Task SetActiveYearAsync(int yearId)
    {
        var allYears = await _context.AcademicYears.ToListAsync();
        var target = allYears.FirstOrDefault(a => a.Id == yearId);
        if (target == null)
            throw new InvalidOperationException($"Academic year with Id {yearId} not found.");

        foreach (var y in allYears) y.IsActive = false;
        target.IsActive = true;

        await _context.SaveChangesAsync();
    }

    public async Task<AcademicYear> CreateYearAsync(string name, DateTime start, DateTime end)
    {
        var year = new AcademicYear { Name = name, StartDate = start, EndDate = end, IsActive = false };
        _context.AcademicYears.Add(year);
        await _context.SaveChangesAsync();
        return year;
    }
}
