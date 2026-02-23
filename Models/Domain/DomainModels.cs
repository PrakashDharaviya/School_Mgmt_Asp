using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolEduERP.Models.Domain;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(256)]
    public string? CreatedBy { get; set; }
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
}

public class AcademicYear : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Name { get; set; } = string.Empty; // e.g. "2024-25"

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<FeeHead> FeeHeads { get; set; } = new List<FeeHead>();
}

public class Student : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string AdmissionNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime DateOfBirth { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(200)]
    public string? GuardianName { get; set; }

    [MaxLength(20)]
    public string? GuardianPhone { get; set; }

    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<MarkEntry> MarkEntries { get; set; } = new List<MarkEntry>();
    public ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}

public class Teacher : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Specialization { get; set; }

    [MaxLength(100)]
    public string? Qualification { get; set; }

    public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Salary> Salaries { get; set; } = new List<Salary>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

public class Course : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    public int Credits { get; set; } = 1;

    public int? TeacherId { get; set; }
    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<MarkEntry> MarkEntries { get; set; } = new List<MarkEntry>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

public class ClassSection : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string ClassName { get; set; } = string.Empty; // e.g. "X"

    [Required, MaxLength(10)]
    public string Section { get; set; } = string.Empty; // e.g. "A"

    public int Capacity { get; set; } = 40;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Enrollment : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    public int ClassSectionId { get; set; }
    [ForeignKey(nameof(ClassSectionId))]
    public ClassSection ClassSection { get; set; } = null!;

    public int AcademicYearId { get; set; }
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear AcademicYear { get; set; } = null!;

    public int? CourseId { get; set; }
    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    public int RollNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FeeHead : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Tuition, Lab, Library

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? ApplicableClass { get; set; }

    public int AcademicYearId { get; set; }
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear AcademicYear { get; set; } = null!;

    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}

public class FeePayment : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    public int FeeHeadId { get; set; }
    [ForeignKey(nameof(FeeHeadId))]
    public FeeHead FeeHead { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Online, Cheque

    [MaxLength(100)]
    public string? TransactionId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Completed, Pending, Failed
}

public class AttendanceRecord : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    public int? ClassSectionId { get; set; }
    [ForeignKey(nameof(ClassSectionId))]
    public ClassSection? ClassSection { get; set; }

    public DateTime Date { get; set; }

    public bool IsPresent { get; set; }

    [MaxLength(200)]
    public string? Remarks { get; set; }
}

public class Exam : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty; // Unit Test 1, Mid-Term

    public int CourseId { get; set; }
    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public DateTime ExamDate { get; set; }

    public int TotalMarks { get; set; } = 100;

    [MaxLength(50)]
    public string? Room { get; set; }

    public ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();
    public ICollection<MarkEntry> MarkEntries { get; set; } = new List<MarkEntry>();
}

public class ExamSchedule : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int ExamId { get; set; }
    [ForeignKey(nameof(ExamId))]
    public Exam Exam { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [MaxLength(100)]
    public string? Room { get; set; }

    [MaxLength(200)]
    public string? Invigilator { get; set; }
}

public class MarkEntry : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    public int ExamId { get; set; }
    [ForeignKey(nameof(ExamId))]
    public Exam Exam { get; set; } = null!;

    public int CourseId { get; set; }
    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal MarksObtained { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal? GradePoint { get; set; }

    [MaxLength(5)]
    public string? LetterGrade { get; set; }

    public bool IsPublished { get; set; }
}

public class Salary : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int TeacherId { get; set; }
    [ForeignKey(nameof(TeacherId))]
    public Teacher Teacher { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasicSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowances { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetSalary { get; set; }

    public DateTime PaymentDate { get; set; }

    [MaxLength(20)]
    public string Month { get; set; } = string.Empty; // "January 2024"

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Paid, Pending
}

public class ReminderLog : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    public int? StudentId { get; set; }
    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [Required, MaxLength(50)]
    public string ReminderType { get; set; } = string.Empty; // Fee, Attendance

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
}
