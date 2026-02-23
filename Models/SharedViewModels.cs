using System.ComponentModel.DataAnnotations;

namespace SchoolEduERP.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? Role { get; set; } = "Student";

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Student";
}

public class DashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public decimal AttendancePercentage { get; set; }
    public decimal FeeCollected { get; set; }
    public decimal FeeOverdue { get; set; }
    public int TodayClasses { get; set; }
    public int TotalClasses { get; set; }
    public string ActiveAcademicYear { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
