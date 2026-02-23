using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models;
using SchoolEduERP.Models.Domain;
using Microsoft.AspNetCore.Authorization;

namespace SchoolEduERP.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Block Student login if not enrolled in the active academic year
                if (roles.Contains("Student"))
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);

                    if (student == null)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty,
                            "No student record found for your account. Please contact the school administrator.");
                        return View(model);
                    }

                    var activeYear = await _context.AcademicYears
                        .FirstOrDefaultAsync(a => a.IsActive);

                    var isEnrolled = activeYear != null && await _context.Enrollments
                        .AnyAsync(e => e.StudentId == student.Id
                                    && e.AcademicYearId == activeYear.Id
                                    && e.IsActive);

                    if (!isEnrolled)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty,
                            "Your enrollment is not yet complete. Please contact the school administrator to complete your enrollment.");
                        return View(model);
                    }
                }
            }

            _logger.LogInformation("User {Email} logged in.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "AdminAccess")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminAccess")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            RoleType = model.Role,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);

            // If role is Student, create a Student domain record
            if (string.Equals(model.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                var studentExists = await _context.Students.AnyAsync(s => s.Email == model.Email);
                if (!studentExists)
                {
                    var names = (model.FullName ?? "").Split(' ', 2);
                    var first = names.Length > 0 ? names[0] : model.FullName;
                    var last = names.Length > 1 ? names[1] : string.Empty;
                    var admissionNumber = $"ADM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

                    var studentEntity = new Student
                    {
                        AdmissionNumber = admissionNumber,
                        FirstName = first ?? string.Empty,
                        LastName = last ?? string.Empty,
                        Email = model.Email,
                        DateOfBirth = DateTime.UtcNow,
                        Gender = "N/A",
                        AdmissionDate = DateTime.UtcNow,
                        IsActive = true,
                        UserId = user.Id
                    };

                    _context.Students.Add(studentEntity);
                    await _context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("User {Email} registered as {Role}.", model.Email, model.Role);
            TempData["Success"] = "Registration successful!";
            // After admin creates user, keep admin on role management or redirect to role list
            return RedirectToAction("Index", "Role", new { area = "Shared" });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }
}
