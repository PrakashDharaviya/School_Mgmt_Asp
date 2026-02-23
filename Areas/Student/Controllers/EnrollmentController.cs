using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Student.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "TeacherAccess")]
public class EnrollmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public EnrollmentController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? standard, string? section)
    {
        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);

        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.ClassSection)
            .Include(e => e.AcademicYear)
            .Include(e => e.Course)
            .Where(e => e.IsActive);

        if (activeYear != null)
            query = query.Where(e => e.AcademicYearId == activeYear.Id);

        if (standard.HasValue)
        {
            var stdStr = standard.Value.ToString();
            query = query.Where(e => e.ClassSection.ClassName == stdStr);
        }

        if (!string.IsNullOrEmpty(section))
            query = query.Where(e => e.ClassSection.Section == section);

        var rawEnrollments = await query
            .Select(e => new EnrollmentViewModel
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                ClassSectionId = e.ClassSectionId,
                ClassName = e.ClassSection.ClassName + "-" + e.ClassSection.Section,
                AcademicYearId = e.AcademicYearId,
                AcademicYearName = e.AcademicYear.Name,
                RollNumber = e.RollNumber,
                CourseName = e.Course != null ? e.Course.Name : null
            })
            .ToListAsync();

        // Sort in memory after materialization
        var enrollments = rawEnrollments
            .OrderBy(e => int.TryParse(e.ClassName?.Split('-')[0], out var n) ? n : 99)
            .ThenBy(e => e.ClassName)
            .ThenBy(e => e.RollNumber)
            .ToList();

        // Distinct standards/sections for filter
        var classSections = await _context.ClassSections.ToListAsync();
        ViewBag.Standards = classSections.Select(c => c.ClassName).Distinct()
            .OrderBy(c => int.TryParse(c, out var n) ? n : 99).ToList();
        ViewBag.Sections = classSections.Select(c => c.Section).Distinct().OrderBy(s => s).ToList();
        ViewBag.SelectedStandard = standard;
        ViewBag.SelectedSection = section;

        return View(enrollments);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EnrollmentViewModel();
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnrollmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // Check duplicate
        var exists = await _context.Enrollments
            .AnyAsync(e => e.StudentId == model.StudentId
                        && e.ClassSectionId == model.ClassSectionId
                        && e.AcademicYearId == model.AcademicYearId
                        && e.IsActive);
        if (exists)
        {
            ModelState.AddModelError("", "Student is already enrolled in this class for the selected year.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // Auto roll number if not set
        if (model.RollNumber <= 0)
        {
            var maxRoll = await _context.Enrollments
                .Where(e => e.ClassSectionId == model.ClassSectionId && e.AcademicYearId == model.AcademicYearId)
                .MaxAsync(e => (int?)e.RollNumber) ?? 0;
            model.RollNumber = maxRoll + 1;
        }

        var enrollment = new Enrollment
        {
            StudentId = model.StudentId,
            ClassSectionId = model.ClassSectionId,
            AcademicYearId = model.AcademicYearId,
            CourseId = model.CourseId,
            RollNumber = model.RollNumber,
            IsActive = true
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Student enrolled successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null) return NotFound();
        enrollment.IsActive = false;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Enrollment removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(EnrollmentViewModel model)
    {
        model.Students = await _context.Students.Where(s => s.IsActive)
            .OrderBy(s => s.FirstName)
            .Select(s => new StudentListItem { Id = s.Id, Name = s.FirstName + " " + s.LastName }).ToListAsync();

        var allSections = await _context.ClassSections.ToListAsync();
        model.ClassSections = allSections
            .OrderBy(c => int.TryParse(c.ClassName, out var n) ? n : 99)
            .ThenBy(c => c.Section)
            .Select(c => new ClassSectionListItem { Id = c.Id, Name = "Std " + c.ClassName + " - " + c.Section })
            .ToList();

        model.AcademicYears = await _context.AcademicYears
            .OrderByDescending(a => a.IsActive)
            .Select(a => new AcademicYearListItem { Id = a.Id, Name = a.Name }).ToListAsync();
        model.Courses = await _context.Courses
            .Select(c => new CourseListItem { Id = c.Id, Name = c.Name }).ToListAsync();
    }
}
