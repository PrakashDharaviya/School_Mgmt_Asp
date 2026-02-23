using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Helpers;
using SchoolEduERP.Models.Domain;
using TeacherEntity = SchoolEduERP.Models.Domain.Teacher;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TeacherController> _logger;
    private const int PageSize = 10;

    public TeacherController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TeacherController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Admin/Teacher
    public async Task<IActionResult> Index(string? search, bool? active, int page = 1)
    {
        var query = _context.Teachers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(t =>
                t.FirstName.ToLower().Contains(q) ||
                t.LastName.ToLower().Contains(q) ||
                t.EmployeeId.ToLower().Contains(q) ||
                (t.Email != null && t.Email.ToLower().Contains(q)) ||
                (t.Specialization != null && t.Specialization.ToLower().Contains(q)));
        }

        if (active.HasValue)
            query = query.Where(t => t.IsActive == active.Value);

        query = query.OrderBy(t => t.FirstName).ThenBy(t => t.LastName);

        var projected = query.Select(t => new TeacherViewModel
        {
            Id             = t.Id,
            EmployeeId     = t.EmployeeId,
            FirstName      = t.FirstName,
            LastName       = t.LastName,
            Email          = t.Email ?? string.Empty,
            Phone          = t.Phone,
            Specialization = t.Specialization,
            Qualification  = t.Qualification,
            JoiningDate    = t.JoiningDate,
            IsActive       = t.IsActive,
            UserId         = t.UserId
        });

        var paged = await PaginatedList<TeacherViewModel>.CreateAsync(projected, page, PageSize);

        ViewBag.SearchTerm   = search;
        ViewBag.ActiveFilter = active;
        ViewData["RouteValues"] = new Microsoft.AspNetCore.Routing.RouteValueDictionary(
            new { search, active });

        return View(paged);
    }

    // GET: /Admin/Teacher/Create
    [HttpGet]
    public IActionResult Create() => View(new TeacherViewModel { JoiningDate = DateTime.Today });

    // POST: /Admin/Teacher/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherViewModel model)
    {
        // Password required on create when email is given
        if (!string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password is required to create a login account.");

        if (!ModelState.IsValid)
            return View(model);

        // Check duplicate Employee ID
        if (await _context.Teachers.AnyAsync(t => t.EmployeeId == model.EmployeeId))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Employee ID already exists.");
            return View(model);
        }

        // Check duplicate email in Teachers table
        if (!string.IsNullOrWhiteSpace(model.Email) &&
            await _context.Teachers.AnyAsync(t => t.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "A teacher with this email already exists.");
            return View(model);
        }

        // ── STEP 1: Save Teacher domain record first (safe — no Identity dependency) ──
        var teacher = new TeacherEntity
        {
            EmployeeId     = model.EmployeeId,
            FirstName      = model.FirstName,
            LastName       = model.LastName,
            Email          = model.Email,
            Phone          = model.Phone,
            Specialization = model.Specialization,
            Qualification  = model.Qualification,
            JoiningDate    = model.JoiningDate,
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync(); // Teacher record is now persisted with an Id

        // ── STEP 2: Create Identity login account (after teacher is safely saved) ──
        if (!string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.Password))
        {
            // Reuse orphaned Identity account if one already exists (e.g., from a previous failed attempt)
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // Link existing account to this teacher
                teacher.UserId    = existingUser.Id;
                teacher.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Teacher {teacher.FirstName} {teacher.LastName} added and linked to existing login account.";
            }
            else
            {
                var appUser = new ApplicationUser
                {
                    UserName       = model.Email,
                    Email          = model.Email,
                    FullName       = model.FirstName + " " + model.LastName,
                    RoleType       = "Teacher",
                    EmailConfirmed = true,
                    CreatedAt      = DateTime.UtcNow,
                    UpdatedAt      = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(appUser, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(appUser, "Teacher");
                    teacher.UserId    = appUser.Id;
                    teacher.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Teacher {teacher.FirstName} {teacher.LastName} added successfully with login account!";
                }
                else
                {
                    // Teacher was saved but login creation failed — warn the admin
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Teacher saved but login account could not be created: {errors}";
                }
            }
        }
        else
        {
            TempData["Success"] = $"Teacher {teacher.FirstName} {teacher.LastName} added successfully (no login account).";
        }

        _logger.LogInformation("Teacher created: {EmployeeId} | Login: {HasLogin}", teacher.EmployeeId, teacher.UserId != null);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Teacher/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var t = await _context.Teachers.FindAsync(id);
        if (t == null) return NotFound();

        return View(new TeacherViewModel
        {
            Id             = t.Id,
            EmployeeId     = t.EmployeeId,
            FirstName      = t.FirstName,
            LastName       = t.LastName,
            Email          = t.Email ?? string.Empty,
            Phone          = t.Phone,
            Specialization = t.Specialization,
            Qualification  = t.Qualification,
            JoiningDate    = t.JoiningDate,
            IsActive       = t.IsActive,
            UserId         = t.UserId
        });
    }

    // POST: /Admin/Teacher/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TeacherViewModel model)
    {
        // Clear password validation for edit — password not changed here
        ModelState.Remove(nameof(model.Password));
        ModelState.Remove(nameof(model.ConfirmPassword));

        if (!ModelState.IsValid)
            return View(model);

        var teacher = await _context.Teachers.FindAsync(model.Id);
        if (teacher == null) return NotFound();

        // Check duplicate Employee ID (excluding self)
        if (await _context.Teachers.AnyAsync(t => t.EmployeeId == model.EmployeeId && t.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Employee ID already used by another teacher.");
            return View(model);
        }

        teacher.EmployeeId     = model.EmployeeId;
        teacher.FirstName      = model.FirstName;
        teacher.LastName       = model.LastName;
        teacher.Email          = model.Email;
        teacher.Phone          = model.Phone;
        teacher.Specialization = model.Specialization;
        teacher.Qualification  = model.Qualification;
        teacher.JoiningDate    = model.JoiningDate;
        teacher.IsActive       = model.IsActive;
        teacher.UpdatedAt      = DateTime.UtcNow;

        // Sync Identity user name/email if linked
        if (!string.IsNullOrEmpty(teacher.UserId))
        {
            var appUser = await _userManager.FindByIdAsync(teacher.UserId);
            if (appUser != null)
            {
                appUser.FullName = model.FirstName + " " + model.LastName;
                appUser.Email    = model.Email;
                appUser.UserName = model.Email;
                await _userManager.UpdateAsync(appUser);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Teacher updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Teacher/ResetPassword/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher == null) return NotFound();

        if (string.IsNullOrEmpty(teacher.UserId))
        {
            TempData["Error"] = "This teacher has no login account.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var user = await _userManager.FindByIdAsync(teacher.UserId);
        if (user == null)
        {
            TempData["Error"] = "Login account not found.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            TempData["Success"] = "Password reset successfully!";
        }
        else
        {
            TempData["Error"] = "Reset failed: " + string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST: /Admin/Teacher/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher == null) return NotFound();

        teacher.IsActive  = false;
        teacher.UpdatedAt = DateTime.UtcNow;

        // Disable Identity account
        if (!string.IsNullOrEmpty(teacher.UserId))
        {
            var user = await _userManager.FindByIdAsync(teacher.UserId);
            if (user != null)
            {
                user.LockoutEnabled  = true;
                user.LockoutEnd      = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Teacher {teacher.FirstName} {teacher.LastName} deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
