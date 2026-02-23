using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Services;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class AcademicYearController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicYearService _yearService;

    public AcademicYearController(ApplicationDbContext context, IAcademicYearService yearService)
    {
        _context = context;
        _yearService = yearService;
    }

    public async Task<IActionResult> Index()
    {
        var years = await _context.AcademicYears.OrderByDescending(y => y.StartDate).ToListAsync();
        var activeYear = years.FirstOrDefault(y => y.IsActive);

        var model = new AcademicYearListViewModel
        {
            Years = years.Select(y => new AcademicYearViewModel
            {
                Id = y.Id,
                Name = y.Name,
                StartDate = y.StartDate,
                EndDate = y.EndDate,
                IsActive = y.IsActive,
                StudentCount = _context.Enrollments.Count(e => e.AcademicYearId == y.Id && e.IsActive)
            }).ToList(),
            ActiveYear = activeYear != null ? new AcademicYearViewModel
            {
                Id = activeYear.Id,
                Name = activeYear.Name,
                StartDate = activeYear.StartDate,
                EndDate = activeYear.EndDate,
                IsActive = true,
                StudentCount = await _context.Enrollments.CountAsync(e => e.AcademicYearId == activeYear.Id && e.IsActive)
            } : null
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        if (startDate >= endDate)
        {
            TempData["Error"] = "Start date must be before end date.";
            return RedirectToAction(nameof(Index));
        }

        var exists = await _context.AcademicYears.AnyAsync(a => a.Name == name);
        if (exists)
        {
            TempData["Error"] = "Academic year with this name already exists.";
            return RedirectToAction(nameof(Index));
        }

        await _yearService.CreateYearAsync(name, startDate, endDate);
        TempData["Success"] = $"Academic year '{name}' created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, DateTime startDate, DateTime endDate)
    {
        var year = await _context.AcademicYears.FindAsync(id);
        if (year == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var duplicate = await _context.AcademicYears.AnyAsync(a => a.Name == name && a.Id != id);
        if (duplicate)
        {
            TempData["Error"] = "Another academic year with this name already exists.";
            return RedirectToAction(nameof(Index));
        }

        year.Name = name;
        year.StartDate = startDate;
        year.EndDate = endDate;
        year.UpdatedAt = DateTime.UtcNow;

        if (startDate >= endDate)
        {
            TempData["Error"] = "Start date must be before end date.";
            return RedirectToAction(nameof(Index));
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Academic year '{name}' updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var year = await _context.AcademicYears.FindAsync(id);
        if (year == null) return NotFound();

        if (year.IsActive)
        {
            TempData["Error"] = "Cannot delete the active academic year. Switch to another year first.";
            return RedirectToAction(nameof(Index));
        }

        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.AcademicYearId == id);
        if (hasEnrollments)
        {
            TempData["Error"] = "Cannot delete this year because it has student enrollments.";
            return RedirectToAction(nameof(Index));
        }

        var hasFeeHeads = await _context.FeeHeads.AnyAsync(f => f.AcademicYearId == id);
        if (hasFeeHeads)
        {
            TempData["Error"] = "Cannot delete this year because it has fee structures linked to it.";
            return RedirectToAction(nameof(Index));
        }

        _context.AcademicYears.Remove(year);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Academic year '{year.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int yearId)
    {
        await _yearService.SetActiveYearAsync(yearId);
        TempData["Success"] = "Active academic year updated!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rollover(int fromYearId, int toYearId)
    {
        var toYear = await _context.AcademicYears.FindAsync(toYearId);
        if (toYear == null)
        {
            TempData["Error"] = "Target academic year not found.";
            return RedirectToAction(nameof(Index));
        }

        var enrollments = await _context.Enrollments
            .Where(e => e.AcademicYearId == fromYearId && e.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var e in enrollments)
        {
            var exists = await _context.Enrollments.AnyAsync(x =>
                x.StudentId == e.StudentId && x.ClassSectionId == e.ClassSectionId && x.AcademicYearId == toYearId);
            if (!exists)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    StudentId = e.StudentId,
                    ClassSectionId = e.ClassSectionId,
                    AcademicYearId = toYearId,
                    CourseId = e.CourseId,
                    RollNumber = e.RollNumber,
                    IsActive = true
                });
                count++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Rolled over {count} students to new academic year!";
        return RedirectToAction(nameof(Index));
    }
}
