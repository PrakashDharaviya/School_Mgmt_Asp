using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class CourseController : Controller
{
    private readonly ApplicationDbContext _context;

    public CourseController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .Include(c => c.Teacher)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Count enrollments per course
        var enrollmentCounts = await _context.Enrollments
            .Where(e => e.IsActive && e.CourseId != null)
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CourseId!.Value, g => g.Count);

        ViewBag.EnrollmentCounts = enrollmentCounts;
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
        return View(new Course());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
            return View(model);
        }

        // Check for duplicates
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var codeExists = await _context.Courses.AnyAsync(c => c.Code == model.Code);
            if (codeExists)
            {
                ModelState.AddModelError(nameof(model.Code), "A course with this code already exists.");
                ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
                return View(model);
            }
        }

        _context.Courses.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stream/Course \"{model.Name}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
            return View(model);
        }

        var existing = await _context.Courses.FindAsync(model.Id);
        if (existing == null) return NotFound();

        // Check for duplicate code (exclude self)
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var codeExists = await _context.Courses.AnyAsync(c => c.Code == model.Code && c.Id != model.Id);
            if (codeExists)
            {
                ModelState.AddModelError(nameof(model.Code), "A course with this code already exists.");
                ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).OrderBy(t => t.FirstName).ToListAsync();
                return View(model);
            }
        }

        existing.Name = model.Name;
        existing.Code = model.Code;
        existing.Credits = model.Credits;
        existing.TeacherId = model.TeacherId;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stream/Course \"{existing.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.CourseId == id && e.IsActive);
        if (hasEnrollments)
        {
            TempData["Error"] = "Cannot delete - active enrollments exist for this stream/course.";
            return RedirectToAction(nameof(Index));
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stream/Course \"{course.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
