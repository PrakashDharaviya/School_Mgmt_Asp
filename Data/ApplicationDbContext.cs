using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ClassSection> ClassSections => Set<ClassSection>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<MarkEntry> MarkEntries => Set<MarkEntry>();
    public DbSet<Salary> Salaries => Set<Salary>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();
    public DbSet<Subject> Subjects => Set<Subject>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Student
        builder.Entity<Student>(e =>
        {
            e.HasIndex(s => s.AdmissionNumber).IsUnique();
            e.HasIndex(s => s.Email);
        });

        // Teacher
        builder.Entity<Teacher>(e =>
        {
            e.HasIndex(t => t.EmployeeId).IsUnique();
        });

        // Course
        builder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasOne(c => c.Teacher).WithMany(t => t.Courses).HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.SetNull);
        });

        // Enrollment - unique per student+class+year
        builder.Entity<Enrollment>(e =>
        {
            e.HasIndex(en => new { en.StudentId, en.ClassSectionId, en.AcademicYearId }).IsUnique();
            e.HasOne(en => en.Course).WithMany(c => c.Enrollments).HasForeignKey(en => en.CourseId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(en => en.AcademicYear).WithMany(a => a.Enrollments).HasForeignKey(en => en.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        // MarkEntry - unique per student+exam+course (restrict to avoid cascade cycles)
        builder.Entity<MarkEntry>(e =>
        {
            e.HasIndex(m => new { m.StudentId, m.ExamId, m.CourseId }).IsUnique();
            e.HasOne(m => m.Student).WithMany(s => s.MarkEntries).HasForeignKey(m => m.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Exam).WithMany(ex => ex.MarkEntries).HasForeignKey(m => m.ExamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Course).WithMany(c => c.MarkEntries).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        // Exam - restrict cascade on Course FK
        builder.Entity<Exam>(e =>
        {
            e.HasOne(ex => ex.Course).WithMany(c => c.Exams).HasForeignKey(ex => ex.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        // FeePayment - restrict cascade on FeeHead FK
        builder.Entity<FeePayment>(e =>
        {
            e.HasOne(fp => fp.FeeHead).WithMany(fh => fh.FeePayments).HasForeignKey(fp => fp.FeeHeadId).OnDelete(DeleteBehavior.Restrict);
        });

        // FeeHead - restrict cascade on AcademicYear FK
        builder.Entity<FeeHead>(e =>
        {
            e.HasOne(fh => fh.AcademicYear).WithMany(a => a.FeeHeads).HasForeignKey(fh => fh.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        // AttendanceRecord - unique per student+classSection+date (allows attendance in different classes)
        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(a => new { a.StudentId, a.ClassSectionId, a.Date }).IsUnique();
            e.HasOne(a => a.ClassSection).WithMany().HasForeignKey(a => a.ClassSectionId).OnDelete(DeleteBehavior.SetNull);
        });

        // AcademicYear
        builder.Entity<AcademicYear>(e =>
        {
            e.HasIndex(a => a.Name).IsUnique();
        });

        // ClassSection - unique per ClassName+Section
        builder.Entity<ClassSection>(e =>
        {
            e.HasIndex(c => new { c.ClassName, c.Section }).IsUnique();
        });

        // Subject
        builder.Entity<Subject>(e =>
        {
            e.HasIndex(s => new { s.Standard, s.Name }).IsUnique();
            e.HasOne(s => s.Teacher).WithMany(t => t.Subjects).HasForeignKey(s => s.TeacherId).OnDelete(DeleteBehavior.SetNull);
        });

    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
