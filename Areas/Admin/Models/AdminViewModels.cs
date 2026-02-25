using System.ComponentModel.DataAnnotations;

namespace SchoolEduERP.Areas.Admin.Models;

public class FeeStructureViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Fee Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Display(Name = "Applicable Class")]
    public string? ApplicableClass { get; set; }

    [Required]
    [Display(Name = "Academic Year")]
    public int AcademicYearId { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(1);

    public bool IsActive { get; set; } = true;

    // Display
    public string? AcademicYearName { get; set; }
    public List<AcademicYearDropdown> AcademicYears { get; set; } = new();
}

public class FeeStructureListViewModel
{
    public List<FeeStructureViewModel> FeeHeads { get; set; } = new();
    public int ActiveAcademicYearId { get; set; }
    public string ActiveAcademicYearName { get; set; } = string.Empty;
    public decimal TotalFeeAmount { get; set; }
}

public class SalaryViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Basic Salary")]
    public decimal BasicSalary { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Allowances { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Deductions { get; set; }

    public decimal NetSalary => BasicSalary + Allowances - Deductions;

    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required]
    public string Month { get; set; } = DateTime.Today.ToString("MMMM yyyy");

    public string Status { get; set; } = "Pending";

    // Display
    public string? TeacherName { get; set; }
    public string? EmployeeId { get; set; }
    public List<TeacherDropdown> Teachers { get; set; } = new();
}

public class ReportViewModel
{
    // Fee Summary
    public decimal TotalFeeExpected { get; set; }
    public decimal TotalFeeCollected { get; set; }
    public decimal TotalFeeOverdue { get; set; }
    public decimal CollectionRate { get; set; }

    // Attendance
    public decimal OverallAttendanceRate { get; set; }
    public int TotalPresentToday { get; set; }
    public int TotalAbsentToday { get; set; }

    // GPA
    public decimal AverageGpa { get; set; }
    public List<GpaDistributionItem> GpaDistribution { get; set; } = new();

    public string ActiveAcademicYear { get; set; } = string.Empty;

    // ── Tab 1: Student Performance ────────────────
    public int TotalStudents { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal HighestGpa { get; set; }
    public decimal LowestGpa { get; set; }
    public List<TopStudentItem> TopStudents { get; set; } = new();

    // ── Tab 2: Fee Analytics ──────────────────────
    public List<FeeHeadBreakdownItem> FeeBreakdown { get; set; } = new();
    public int TotalPayments { get; set; }
    public int PaidStudentCount { get; set; }
    public int UnpaidStudentCount { get; set; }

    // ── Tab 3: Attendance Trends ──────────────────
    public List<ClassAttendanceItem> ClassAttendance { get; set; } = new();
    public int TotalWorkingDays { get; set; }

    // ── Tab 4: Exam Results ──────────────────────
    public List<ExamResultSummaryItem> ExamResults { get; set; } = new();
}

// Helper dropdowns and items
public class AcademicYearDropdown { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class TeacherDropdown { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class GpaDistributionItem { public string Range { get; set; } = string.Empty; public int Count { get; set; } }

public class TopStudentItem
{
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int RollNumber { get; set; }
    public decimal Gpa { get; set; }
    public string Grade { get; set; } = string.Empty;
}

public class FeeHeadBreakdownItem
{
    public string FeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Collected { get; set; }
    public decimal Pending { get; set; }
    public decimal CollectionRate { get; set; }
    public string DueDate { get; set; } = string.Empty;
}

public class ClassAttendanceItem
{
    public string ClassName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class ExamResultSummaryItem
{
    public string ExamName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public decimal Average { get; set; }
    public decimal Highest { get; set; }
    public decimal Lowest { get; set; }
    public decimal PassRate { get; set; }
    public int TotalMarks { get; set; }
}

// ── Teacher Management ────────────────────────────────────────────────
public class TeacherViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Employee ID")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Specialization { get; set; }

    [MaxLength(100)]
    public string? Qualification { get; set; }

    [Display(Name = "Joining Date")]
    [DataType(DataType.Date)]
    public DateTime JoiningDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    // ── Login Credentials (required on create, ignored on edit) ──
    [Display(Name = "Login Password")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string? Password { get; set; }

    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }

    // Display only
    public string? UserId { get; set; }
    public bool HasLoginAccount => !string.IsNullOrEmpty(UserId);
}

// Academic Year Management
public class AcademicYearViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int StudentCount { get; set; }
}

public class AcademicYearListViewModel
{
    public List<AcademicYearViewModel> Years { get; set; } = new();
    public AcademicYearViewModel? ActiveYear { get; set; }
}

// Student Profile
public class StudentProfileViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public DateTime AdmissionDate { get; set; }

    // Academic
    public string ClassName { get; set; } = string.Empty;
    public int RollNumber { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal Gpa { get; set; }
    public decimal AttendancePercentage { get; set; }

    // Marks
    public List<MarkReportLine> Marks { get; set; } = new();

    // Fee
    public decimal TotalFeeDue { get; set; }
    public decimal TotalFeePaid { get; set; }
}

public class MarkReportLine
{
    public string SubjectName { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public decimal MarksObtained { get; set; }
    public int TotalMarks { get; set; }
    public string Grade { get; set; } = string.Empty;
    public decimal GradePoint { get; set; }
}
