using System.ComponentModel.DataAnnotations;

namespace SchoolEduERP.Models.ViewModels;

public class ProfileViewModel
{
    // Personal Information
    [Required(ErrorMessage = "Full Name is required")]
    [MaxLength(200, ErrorMessage = "Full Name cannot exceed 200 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Profile Photo")]
    public string? ProfilePhotoPath { get; set; }

    // Read-only info
    public string Role { get; set; } = string.Empty;
    public string? EmployeeIdOrAdmission { get; set; }
    public DateTime? JoiningDate { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Old password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Old Password")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfilePageViewModel
{
    public ProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel ChangePassword { get; set; } = new();
}
