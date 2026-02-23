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

        // AttendanceRecord - unique per student+date
        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(a => new { a.StudentId, a.Date }).IsUnique();
        });

        // AcademicYear
        builder.Entity<AcademicYear>(e =>
        {
            e.HasIndex(a => a.Name).IsUnique();
        });

        // Subject
        builder.Entity<Subject>(e =>
        {
            e.HasIndex(s => new { s.Standard, s.Name }).IsUnique();
            e.HasOne(s => s.Teacher).WithMany(t => t.Subjects).HasForeignKey(s => s.TeacherId).OnDelete(DeleteBehavior.SetNull);
        });

        // Seed default academic year
        builder.Entity<AcademicYear>().HasData(new AcademicYear
        {
            Id = 1,
            Name = "2024-25",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 1)
        });

        // Seed ClassSections (Indian numeric standards)
        builder.Entity<ClassSection>().HasData(
            new ClassSection { Id = 1, ClassName = "10", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 2, ClassName = "10", Section = "B", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 3, ClassName = "9", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 4, ClassName = "9", Section = "B", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 5, ClassName = "11", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 6, ClassName = "1", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 7, ClassName = "2", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 8, ClassName = "3", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 9, ClassName = "4", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 10, ClassName = "5", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 11, ClassName = "6", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 12, ClassName = "7", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 13, ClassName = "8", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) },
            new ClassSection { Id = 14, ClassName = "12", Section = "A", Capacity = 40, CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2024, 1, 1) }
        );

        // Seed Subjects (common Indian curriculum examples)
        builder.Entity<Subject>().HasData(
            // Primary (1-5)
            new Subject { Id = 1, Standard = 1, Name = "English", Code = "ENG-1", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 2, Standard = 1, Name = "Mathematics", Code = "MATH-1", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 3, Standard = 1, Name = "Environmental Studies", Code = "EVS-1", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 4, Standard = 1, Name = "Hindi", Code = "HIN-1", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 5, Standard = 2, Name = "English", Code = "ENG-2", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 6, Standard = 2, Name = "Mathematics", Code = "MATH-2", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 7, Standard = 2, Name = "Environmental Studies", Code = "EVS-2", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 8, Standard = 2, Name = "Hindi", Code = "HIN-2", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 9, Standard = 3, Name = "English", Code = "ENG-3", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 10, Standard = 3, Name = "Mathematics", Code = "MATH-3", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 11, Standard = 3, Name = "Environmental Studies", Code = "EVS-3", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 12, Standard = 3, Name = "Hindi", Code = "HIN-3", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 13, Standard = 4, Name = "English", Code = "ENG-4", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 14, Standard = 4, Name = "Mathematics", Code = "MATH-4", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 15, Standard = 4, Name = "Environmental Studies", Code = "EVS-4", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 16, Standard = 4, Name = "Hindi", Code = "HIN-4", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 17, Standard = 5, Name = "English", Code = "ENG-5", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 18, Standard = 5, Name = "Mathematics", Code = "MATH-5", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 19, Standard = 5, Name = "Environmental Studies", Code = "EVS-5", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 20, Standard = 5, Name = "Hindi", Code = "HIN-5", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            // Middle (6-8)
            new Subject { Id = 21, Standard = 6, Name = "English", Code = "ENG-6", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 22, Standard = 6, Name = "Mathematics", Code = "MATH-6", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 23, Standard = 6, Name = "Science", Code = "SCI-6", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 24, Standard = 6, Name = "Social Studies", Code = "SOC-6", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 25, Standard = 6, Name = "Hindi", Code = "HIN-6", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 26, Standard = 7, Name = "English", Code = "ENG-7", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 27, Standard = 7, Name = "Mathematics", Code = "MATH-7", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 28, Standard = 7, Name = "Science", Code = "SCI-7", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 29, Standard = 7, Name = "Social Studies", Code = "SOC-7", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 30, Standard = 7, Name = "Hindi", Code = "HIN-7", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 31, Standard = 8, Name = "English", Code = "ENG-8", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 32, Standard = 8, Name = "Mathematics", Code = "MATH-8", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 33, Standard = 8, Name = "Science", Code = "SCI-8", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 34, Standard = 8, Name = "Social Studies", Code = "SOC-8", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 35, Standard = 8, Name = "Hindi", Code = "HIN-8", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            // Secondary (9-10)
            new Subject { Id = 36, Standard = 9, Name = "English", Code = "ENG-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 37, Standard = 9, Name = "Mathematics", Code = "MATH-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 38, Standard = 9, Name = "Physics", Code = "PHY-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 39, Standard = 9, Name = "Chemistry", Code = "CHEM-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 40, Standard = 9, Name = "Biology", Code = "BIO-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 41, Standard = 9, Name = "History", Code = "HIS-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 42, Standard = 9, Name = "Geography", Code = "GEO-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 43, Standard = 9, Name = "Hindi", Code = "HIN-9", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 44, Standard = 10, Name = "English", Code = "ENG-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 45, Standard = 10, Name = "Mathematics", Code = "MATH-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 46, Standard = 10, Name = "Physics", Code = "PHY-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 47, Standard = 10, Name = "Chemistry", Code = "CHEM-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 48, Standard = 10, Name = "Biology", Code = "BIO-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 49, Standard = 10, Name = "History", Code = "HIS-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 50, Standard = 10, Name = "Geography", Code = "GEO-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 51, Standard = 10, Name = "Hindi", Code = "HIN-10", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            // Higher Secondary (11-12)
            new Subject { Id = 52, Standard = 11, Name = "English", Code = "ENG-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 53, Standard = 11, Name = "Physics", Code = "PHY-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 54, Standard = 11, Name = "Chemistry", Code = "CHEM-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 55, Standard = 11, Name = "Biology", Code = "BIO-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 56, Standard = 11, Name = "Mathematics", Code = "MATH-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 57, Standard = 11, Name = "Economics", Code = "ECO-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 58, Standard = 11, Name = "Accountancy", Code = "ACC-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 59, Standard = 11, Name = "Business Studies", Code = "BUS-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 60, Standard = 11, Name = "Computer Science", Code = "CS-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 61, Standard = 11, Name = "Hindi", Code = "HIN-11", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },

            new Subject { Id = 62, Standard = 12, Name = "English", Code = "ENG-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 63, Standard = 12, Name = "Physics", Code = "PHY-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 64, Standard = 12, Name = "Chemistry", Code = "CHEM-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 65, Standard = 12, Name = "Biology", Code = "BIO-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 66, Standard = 12, Name = "Mathematics", Code = "MATH-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 67, Standard = 12, Name = "Economics", Code = "ECO-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 68, Standard = 12, Name = "Accountancy", Code = "ACC-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 69, Standard = 12, Name = "Business Studies", Code = "BUS-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 70, Standard = 12, Name = "Computer Science", Code = "CS-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) },
            new Subject { Id = 71, Standard = 12, Name = "Hindi", Code = "HIN-12", CreatedAt = new DateTime(2024,1,1), UpdatedAt = new DateTime(2024,1,1) }
        );
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
