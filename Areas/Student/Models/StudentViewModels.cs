using System.ComponentModel.DataAnnotations;

namespace SchoolEduERP.Areas.Student.Models;

public class AdmissionViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Admission Number")]
    public string AdmissionNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    public string? Gender { get; set; }
    public string? Address { get; set; }

    [Display(Name = "Guardian Name")]
    public string? GuardianName { get; set; }

    [Phone]
    [Display(Name = "Guardian Phone")]
    public string? GuardianPhone { get; set; }

    [Display(Name = "Admission Date")]
    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    // Class & Enrollment (for create/edit)
    [Display(Name = "Class & Section")]
    public int ClassSectionId { get; set; }

    [Display(Name = "Academic Year")]
    public int AcademicYearId { get; set; }

    [Display(Name = "Roll Number")]
    public int RollNumber { get; set; }

    // Display
    public string? ClassName { get; set; }

    // Dropdown lists
    public List<ClassSectionListItem> ClassSections { get; set; } = new();
    public List<AcademicYearListItem> AcademicYears { get; set; } = new();
}

public class EnrollmentViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Student")]
    public int StudentId { get; set; }

    [Required]
    [Display(Name = "Class & Section")]
    public int ClassSectionId { get; set; }

    [Required]
    [Display(Name = "Academic Year")]
    public int AcademicYearId { get; set; }

    public int? CourseId { get; set; }

    [Display(Name = "Roll Number")]
    public int RollNumber { get; set; }

    // Display helpers
    public string? StudentName { get; set; }
    public string? ClassName { get; set; }
    public string? AcademicYearName { get; set; }
    public string? CourseName { get; set; }

    // Lists for dropdowns
    public List<StudentListItem> Students { get; set; } = new();
    public List<ClassSectionListItem> ClassSections { get; set; } = new();
    public List<AcademicYearListItem> AcademicYears { get; set; } = new();
    public List<CourseListItem> Courses { get; set; } = new();
}

public class FeeViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public List<FeeScheduleItem> FeeSchedule { get; set; } = new();
    public List<FeePaymentItem> RecentPayments { get; set; } = new();
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}

public class FeePaymentFormViewModel
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public int FeeHeadId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal AmountPaid { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Cash";

    public string? TransactionId { get; set; }
}

// Helper classes for dropdowns
public class StudentListItem { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class ClassSectionListItem { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class AcademicYearListItem { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class CourseListItem { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class FeeScheduleItem
{
    public int FeeHeadId { get; set; }
    public string FeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pending"; // Paid, Pending, Overdue
    public decimal AmountPaid { get; set; }
}

public class FeePaymentItem
{
    public int Id { get; set; }
    public string FeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
