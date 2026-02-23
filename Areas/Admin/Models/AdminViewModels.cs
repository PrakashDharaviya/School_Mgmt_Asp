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
}

// Helper dropdowns and items
public class AcademicYearDropdown { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class TeacherDropdown { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class GpaDistributionItem { public string Range { get; set; } = string.Empty; public int Count { get; set; } }

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
