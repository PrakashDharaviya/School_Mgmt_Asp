using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Data.Repositories;

public class StudentRepository : Repository<Student>, IStudentRepository
{
    public StudentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Student?> GetByAdmissionNumberAsync(string admissionNumber)
        => await _dbSet.FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber);

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
        => await _dbSet.Where(s => s.IsActive).ToListAsync();

    public async Task<IEnumerable<Student>> GetStudentsByClassSectionAsync(int classSectionId, int academicYearId)
        => await _context.Enrollments
            .Where(e => e.ClassSectionId == classSectionId && e.AcademicYearId == academicYearId && e.IsActive)
            .Include(e => e.Student)
            .Select(e => e.Student)
            .ToListAsync();
}

public class TeacherRepository : Repository<Teacher>, ITeacherRepository
{
    public TeacherRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Teacher?> GetByEmployeeIdAsync(string employeeId)
        => await _dbSet.FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

    public async Task<IEnumerable<Teacher>> GetActiveTeachersAsync()
        => await _dbSet.Where(t => t.IsActive).ToListAsync();
}

public class FeeHeadRepository : Repository<FeeHead>, IFeeHeadRepository
{
    public FeeHeadRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<FeeHead>> GetByAcademicYearAsync(int academicYearId)
        => await _dbSet.Where(f => f.AcademicYearId == academicYearId && f.IsActive).ToListAsync();

    public async Task<IEnumerable<FeeHead>> GetOverdueFeeHeadsAsync()
        => await _dbSet.Where(f => f.DueDate < DateTime.UtcNow && f.IsActive).ToListAsync();

    public async Task<IEnumerable<FeeHead>> GetUpcomingFeeHeadsAsync(int daysAhead)
    {
        var cutoff = DateTime.UtcNow.AddDays(daysAhead);
        return await _dbSet.Where(f => f.DueDate <= cutoff && f.DueDate >= DateTime.UtcNow && f.IsActive).ToListAsync();
    }
}
