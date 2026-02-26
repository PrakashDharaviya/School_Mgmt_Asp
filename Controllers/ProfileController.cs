using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Models.ViewModels;

namespace SchoolEduERP.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        var model = new ProfilePageViewModel();
        model.Profile.Email = user.Email ?? string.Empty;
        model.Profile.FullName = user.FullName;
        model.Profile.Role = role;

        if (role == "Teacher")
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher != null)
            {
                model.Profile.FullName = $"{teacher.FirstName} {teacher.LastName}".Trim();
                model.Profile.Phone = teacher.Phone ?? string.Empty;
                model.Profile.Address = string.Empty; // Teacher model doesn't have Address
                model.Profile.EmployeeIdOrAdmission = teacher.EmployeeId;
                model.Profile.JoiningDate = teacher.JoiningDate;
            }
        }
        else if (role == "Student")
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            if (student != null)
            {
                model.Profile.FullName = $"{student.FirstName} {student.LastName}".Trim();
                model.Profile.Phone = student.Phone ?? string.Empty;
                model.Profile.Address = student.Address ?? string.Empty;
                model.Profile.EmployeeIdOrAdmission = student.AdmissionNumber;
                model.Profile.JoiningDate = student.AdmissionDate;
            }
        }

        // Check for profile photo
        var profilePhotosDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
        var photoPath = Directory.Exists(profilePhotosDir)
            ? Directory.GetFiles(profilePhotosDir, $"{user.Id}.*").FirstOrDefault()
            : null;

        if (photoPath != null)
        {
            model.Profile.ProfilePhotoPath = "/uploads/profiles/" + Path.GetFileName(photoPath);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfilePageViewModel model, IFormFile? profilePhoto)
    {
        // Only validate Profile part
        ModelState.Remove("ChangePassword.OldPassword");
        ModelState.Remove("ChangePassword.NewPassword");
        ModelState.Remove("ChangePassword.ConfirmPassword");

        if (!ModelState.IsValid)
        {
            // Reload role info
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                model.Profile.Role = userRoles.FirstOrDefault() ?? "User";
            }
            TempData["Error"] = "Please fix the validation errors.";
            return View("Index", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        try
        {
            // Update ApplicationUser
            user.FullName = model.Profile.FullName;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Update role-specific table
            if (role == "Teacher")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher != null)
                {
                    var names = model.Profile.FullName.Split(' ', 2);
                    teacher.FirstName = names[0];
                    teacher.LastName = names.Length > 1 ? names[1] : string.Empty;
                    teacher.Phone = model.Profile.Phone;
                    teacher.Email = model.Profile.Email;
                    teacher.UpdatedAt = DateTime.UtcNow;
                    _context.Teachers.Update(teacher);
                }
            }
            else if (role == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
                if (student != null)
                {
                    var names = model.Profile.FullName.Split(' ', 2);
                    student.FirstName = names[0];
                    student.LastName = names.Length > 1 ? names[1] : string.Empty;
                    student.Phone = model.Profile.Phone;
                    student.Email = model.Profile.Email;
                    student.Address = model.Profile.Address;
                    student.UpdatedAt = DateTime.UtcNow;
                    _context.Students.Update(student);
                }
            }

            // Handle profile photo upload
            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Only image files (jpg, jpeg, png, gif, webp) are allowed.";
                    model.Profile.Role = role;
                    return View("Index", model);
                }

                if (profilePhoto.Length > 2 * 1024 * 1024) // 2MB limit
                {
                    TempData["Error"] = "Profile photo must be less than 2MB.";
                    model.Profile.Role = role;
                    return View("Index", model);
                }

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsDir);

                // Delete old photos
                foreach (var oldFile in Directory.GetFiles(uploadsDir, $"{user.Id}.*"))
                {
                    System.IO.File.Delete(oldFile);
                }

                var fileName = $"{user.Id}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while updating your profile. Please try again.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfilePageViewModel model)
    {
        // Only validate ChangePassword part
        ModelState.Remove("Profile.FullName");
        ModelState.Remove("Profile.Email");
        ModelState.Remove("Profile.Phone");
        ModelState.Remove("Profile.Address");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            // Reload profile data for the view
            var reloadModel = await BuildProfilePageModel(user);
            reloadModel.ChangePassword = model.ChangePassword;
            TempData["Error"] = "Please fix the validation errors.";
            return View("Index", reloadModel);
        }

        try
        {
            // Verify old password
            var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, model.ChangePassword.OldPassword);
            if (!isOldPasswordValid)
            {
                var reloadModel = await BuildProfilePageModel(user);
                reloadModel.ChangePassword = model.ChangePassword;
                TempData["Error"] = "Old password is incorrect.";
                return View("Index", reloadModel);
            }

            // Change password (Identity handles hashing automatically)
            var result = await _userManager.ChangePasswordAsync(user, model.ChangePassword.OldPassword, model.ChangePassword.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Password changed successfully!";
                _logger.LogInformation("User {UserId} changed their password.", user.Id);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to change password: {errors}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while changing your password. Please try again.";
        }

        return RedirectToAction("Index");
    }

    private async Task<ProfilePageViewModel> BuildProfilePageModel(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        var model = new ProfilePageViewModel();
        model.Profile.Email = user.Email ?? string.Empty;
        model.Profile.FullName = user.FullName;
        model.Profile.Role = role;

        if (role == "Teacher")
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher != null)
            {
                model.Profile.FullName = $"{teacher.FirstName} {teacher.LastName}".Trim();
                model.Profile.Phone = teacher.Phone ?? string.Empty;
                model.Profile.Address = string.Empty;
                model.Profile.EmployeeIdOrAdmission = teacher.EmployeeId;
                model.Profile.JoiningDate = teacher.JoiningDate;
            }
        }
        else if (role == "Student")
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            if (student != null)
            {
                model.Profile.FullName = $"{student.FirstName} {student.LastName}".Trim();
                model.Profile.Phone = student.Phone ?? string.Empty;
                model.Profile.Address = student.Address ?? string.Empty;
                model.Profile.EmployeeIdOrAdmission = student.AdmissionNumber;
                model.Profile.JoiningDate = student.AdmissionDate;
            }
        }

        // Check for profile photo
        var profilePhotosDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
        var photoPath = Directory.Exists(profilePhotosDir)
            ? Directory.GetFiles(profilePhotosDir, $"{user.Id}.*").FirstOrDefault()
            : null;

        if (photoPath != null)
        {
            model.Profile.ProfilePhotoPath = "/uploads/profiles/" + Path.GetFileName(photoPath);
        }

        return model;
    }
}
