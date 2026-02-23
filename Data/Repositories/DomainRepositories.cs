using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Data.Repositories;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByAdmissionNumberAsync(string admissionNumber);
    Task<IEnumerable<Student>> GetActiveStudentsAsync();
    Task<IEnumerable<Student>> GetStudentsByClassSectionAsync(int classSectionId, int academicYearId);
}

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher?> GetByEmployeeIdAsync(string employeeId);
    Task<IEnumerable<Teacher>> GetActiveTeachersAsync();
}

public interface IFeeHeadRepository : IRepository<FeeHead>
{
    Task<IEnumerable<FeeHead>> GetByAcademicYearAsync(int academicYearId);
    Task<IEnumerable<FeeHead>> GetOverdueFeeHeadsAsync();
    Task<IEnumerable<FeeHead>> GetUpcomingFeeHeadsAsync(int daysAhead);
}
