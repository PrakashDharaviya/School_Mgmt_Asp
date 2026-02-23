using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Models;

namespace SchoolEduERP.Areas.Shared.Controllers;

[Area("Shared")]
[Authorize(Policy = "AdminAccess")]
public class RoleController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public RoleController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var roles = await _roleManager.Roles.ToListAsync();

        var userRoles = new List<UserRoleViewModel>();
        foreach (var user in users)
        {
            var userRoleNames = await _userManager.GetRolesAsync(user);
            userRoles.Add(new UserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                Roles = userRoleNames.ToList(),
                RoleType = user.RoleType ?? "Student"
            });
        }

        ViewBag.Roles = roles;
        ViewBag.AdminCount = userRoles.Count(u => u.Roles.Contains("Admin"));
        ViewBag.TeacherCount = userRoles.Count(u => u.Roles.Contains("Teacher"));
        ViewBag.StudentCount = userRoles.Count(u => u.Roles.Contains("Student"));

        return View(userRoles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        // Use RegisterViewModel to render the create form
        var model = new RegisterViewModel { Role = "Student" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Prevent duplicate user by email
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            RoleType = model.Role,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        // If role is Student, create a Student domain record and link if not exists
        if (string.Equals(model.Role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var studentExists = await _context.Students.AnyAsync(s => s.Email == model.Email);
            if (!studentExists)
            {
                var names = (model.FullName ?? "").Split(' ', 2);
                var first = names.Length > 0 ? names[0] : model.FullName;
                var last = names.Length > 1 ? names[1] : string.Empty;
                var admissionNumber = $"ADM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

                var studentEntity = new SchoolEduERP.Models.Domain.Student
                {
                    AdmissionNumber = admissionNumber,
                    FirstName = first ?? string.Empty,
                    LastName = last ?? string.Empty,
                    Email = model.Email,
                    Phone = string.Empty,
                    DateOfBirth = DateTime.UtcNow, // placeholder, admin can edit later
                    Gender = "N/A",
                    Address = string.Empty,
                    GuardianName = string.Empty,
                    GuardianPhone = string.Empty,
                    AdmissionDate = DateTime.UtcNow,
                    IsActive = true,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Students.Add(studentEntity);
                await _context.SaveChangesAsync();
            }
        }

        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        user.RoleType = role;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"Role updated to {role} for {user.FullName}.";
        return RedirectToAction(nameof(Index));
    }
}

public class UserRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string RoleType { get; set; } = string.Empty;
}
