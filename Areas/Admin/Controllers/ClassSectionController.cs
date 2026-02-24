using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class ClassSectionController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClassSectionController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var sections = await _context.ClassSections.ToListAsync();
        sections = sections
            .OrderBy(c => int.TryParse(c.ClassName, out var n) ? n : 99)
            .ThenBy(c => c.Section)
            .ToList();

        // Get active year (fall back to the latest year if none is active)
        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive)
                      ?? await _context.AcademicYears.OrderByDescending(a => a.Id).FirstOrDefaultAsync();

        // Count enrolled students per class section
        var enrollmentCounts = new Dictionary<int, int>();
        if (activeYear != null)
        {
            enrollmentCounts = await _context.Enrollments
                .Where(e => e.IsActive && e.AcademicYearId == activeYear.Id)
                .GroupBy(e => e.ClassSectionId)
                .Select(g => new { ClassSectionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ClassSectionId, g => g.Count);
        }

        ViewBag.EnrollmentCounts = enrollmentCounts;
        return View(sections);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ClassSection());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassSection model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exists = await _context.ClassSections
            .AnyAsync(c => c.ClassName == model.ClassName && c.Section == model.Section);
        if (exists)
        {
            ModelState.AddModelError("", $"Class {model.ClassName}-{model.Section} already exists.");
            return View(model);
        }

        _context.ClassSections.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Class {model.ClassName}-{model.Section} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var section = await _context.ClassSections.FindAsync(id);
        if (section == null) return NotFound();
        return View(section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClassSection model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existing = await _context.ClassSections.FindAsync(model.Id);
        if (existing == null) return NotFound();

        // Check for duplicate ClassName+Section (exclude self)
        var duplicate = await _context.ClassSections
            .AnyAsync(c => c.ClassName == model.ClassName && c.Section == model.Section && c.Id != model.Id);
        if (duplicate)
        {
            ModelState.AddModelError("", $"Class {model.ClassName}-{model.Section} already exists.");
            return View(model);
        }

        // Update only changed fields to preserve CreatedAt and other audit fields
        existing.ClassName = model.ClassName;
        existing.Section = model.Section;
        existing.Capacity = model.Capacity;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Class section updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _context.ClassSections.FindAsync(id);
        if (section == null) return NotFound();

        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.ClassSectionId == id && e.IsActive);
        if (hasEnrollments)
        {
            TempData["Error"] = "Cannot delete - active enrollments exist for this class.";
            return RedirectToAction(nameof(Index));
        }

        _context.ClassSections.Remove(section);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Class section deleted.";
        return RedirectToAction(nameof(Index));
    }
}
