using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Student.Models;
using SchoolEduERP.Data;
using DomainModels = SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "TeacherAccess")]
public class AdmissionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdmissionController> _logger;

    public AdmissionController(ApplicationDbContext context, ILogger<AdmissionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? standard, string? section)
    {
        // Get all class sections for the filter dropdown
        var classSections = await _context.ClassSections.ToListAsync();
        classSections = classSections
            .OrderBy(c => int.TryParse(c.ClassName, out var n) ? n : 99)
            .ThenBy(c => c.Section)
            .ToList();

        var distinctStandards = classSections
            .Select(c => c.ClassName)
            .Distinct()
            .OrderBy(c => int.TryParse(c, out var n) ? n : 99)
            .ToList();

        // Get active academic year
        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);

        // Build student query — join with enrollment to know their class
        var query = _context.Students
            .Where(s => s.IsActive)
            .AsQueryable();

        // If filtering by standard/section, join via enrollment
        List<AdmissionViewModel> students;

        if (standard.HasValue || !string.IsNullOrEmpty(section))
        {
            var stdStr = standard?.ToString();
            var rawList = await (from s in query
                           join e in _context.Enrollments.Include(e => e.ClassSection)
                               on s.Id equals e.StudentId
                           where e.IsActive
                                 && (activeYear == null || e.AcademicYearId == activeYear.Id)
                                 && (stdStr == null || e.ClassSection.ClassName == stdStr)
                                 && (string.IsNullOrEmpty(section) || e.ClassSection.Section == section)
                           select new AdmissionViewModel
                           {
                               Id = s.Id,
                               AdmissionNumber = s.AdmissionNumber,
                               FirstName = s.FirstName,
                               LastName = s.LastName,
                               Email = s.Email,
                               Phone = s.Phone,
                               DateOfBirth = s.DateOfBirth,
                               Gender = s.Gender,
                               AdmissionDate = s.AdmissionDate,
                               ClassName = e.ClassSection.ClassName + "-" + e.ClassSection.Section,
                               RollNumber = e.RollNumber
                           }).ToListAsync();

            students = rawList
                .OrderBy(s => int.TryParse(s.ClassName?.Split('-')[0], out var n) ? n : 99)
                .ThenBy(s => s.ClassName)
                .ThenBy(s => s.RollNumber)
                .ToList();
        }
        else
        {
            // Show all students, with their class info if enrolled
            var rawList = await (from s in query
                           join e in _context.Enrollments.Include(e => e.ClassSection)
                               .Where(e => e.IsActive && (activeYear == null || e.AcademicYearId == activeYear.Id))
                               on s.Id equals e.StudentId into enrollments
                           from e in enrollments.DefaultIfEmpty()
                           select new AdmissionViewModel
                           {
                               Id = s.Id,
                               AdmissionNumber = s.AdmissionNumber,
                               FirstName = s.FirstName,
                               LastName = s.LastName,
                               Email = s.Email,
                               Phone = s.Phone,
                               DateOfBirth = s.DateOfBirth,
                               Gender = s.Gender,
                               AdmissionDate = s.AdmissionDate,
                               ClassName = e != null ? e.ClassSection.ClassName + "-" + e.ClassSection.Section : null,
                               RollNumber = e != null ? e.RollNumber : 0
                           }).ToListAsync();

            students = rawList
                .OrderBy(s => s.ClassName == null ? 1 : 0)
                .ThenBy(s => int.TryParse(s.ClassName?.Split('-')[0], out var n) ? n : 99)
                .ThenBy(s => s.ClassName)
                .ThenBy(s => s.RollNumber)
                .ToList();
        }

        ViewBag.Standards = distinctStandards;
        ViewBag.Sections = classSections.Select(c => c.Section).Distinct().OrderBy(s => s).ToList();
        ViewBag.SelectedStandard = standard;
        ViewBag.SelectedSection = section;

        // Group students by class for the view
        ViewBag.GroupedStudents = students
            .GroupBy(s => s.ClassName ?? "Not Enrolled")
            .OrderBy(g => g.Key == "Not Enrolled" ? 1 : 0)
            .ThenBy(g => int.TryParse(g.Key.Split('-')[0], out var n) ? n : 99)
            .ThenBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        return View(students);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AdmissionViewModel { AdmissionDate = DateTime.Today };
        await PopulateClassDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateClassDropdowns(model);
            return View(model);
        }

        var exists = await _context.Students.AnyAsync(s => s.AdmissionNumber == model.AdmissionNumber);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.AdmissionNumber), "Admission number already exists.");
            await PopulateClassDropdowns(model);
            return View(model);
        }

        var student = new DomainModels.Student
        {
            AdmissionNumber = model.AdmissionNumber,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Address = model.Address,
            GuardianName = model.GuardianName,
            GuardianPhone = model.GuardianPhone,
            AdmissionDate = model.AdmissionDate,
            IsActive = true
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Auto-enroll if class section and academic year selected
        if (model.ClassSectionId > 0 && model.AcademicYearId > 0)
        {
            // Auto-assign roll number
            var maxRoll = await _context.Enrollments
                .Where(e => e.ClassSectionId == model.ClassSectionId && e.AcademicYearId == model.AcademicYearId)
                .MaxAsync(e => (int?)e.RollNumber) ?? 0;

            var enrollment = new DomainModels.Enrollment
            {
                StudentId = student.Id,
                ClassSectionId = model.ClassSectionId,
                AcademicYearId = model.AcademicYearId,
                RollNumber = maxRoll + 1,
                IsActive = true
            };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = $"Student {student.FirstName} {student.LastName} admitted successfully!";
        _logger.LogInformation("Student admitted: {AdmissionNumber}", student.AdmissionNumber);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return NotFound();

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == id && e.IsActive
                && (activeYear == null || e.AcademicYearId == activeYear.Id));

        var model = new AdmissionViewModel
        {
            Id = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            GuardianName = student.GuardianName,
            GuardianPhone = student.GuardianPhone,
            AdmissionDate = student.AdmissionDate,
            ClassSectionId = enrollment?.ClassSectionId ?? 0,
            AcademicYearId = enrollment?.AcademicYearId ?? (activeYear?.Id ?? 0),
            RollNumber = enrollment?.RollNumber ?? 0
        };

        await PopulateClassDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateClassDropdowns(model);
            return View(model);
        }

        var student = await _context.Students.FindAsync(model.Id);
        if (student == null) return NotFound();

        student.FirstName = model.FirstName;
        student.LastName = model.LastName;
        student.Email = model.Email;
        student.Phone = model.Phone;
        student.DateOfBirth = model.DateOfBirth;
        student.Gender = model.Gender;
        student.Address = model.Address;
        student.GuardianName = model.GuardianName;
        student.GuardianPhone = model.GuardianPhone;

        await _context.SaveChangesAsync();

        // Update/create enrollment if class section selected
        if (model.ClassSectionId > 0 && model.AcademicYearId > 0)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == model.Id && e.AcademicYearId == model.AcademicYearId && e.IsActive);

            if (enrollment != null)
            {
                enrollment.ClassSectionId = model.ClassSectionId;
                if (model.RollNumber > 0) enrollment.RollNumber = model.RollNumber;
            }
            else
            {
                var maxRoll = await _context.Enrollments
                    .Where(e => e.ClassSectionId == model.ClassSectionId && e.AcademicYearId == model.AcademicYearId)
                    .MaxAsync(e => (int?)e.RollNumber) ?? 0;

                _context.Enrollments.Add(new DomainModels.Enrollment
                {
                    StudentId = model.Id,
                    ClassSectionId = model.ClassSectionId,
                    AcademicYearId = model.AcademicYearId,
                    RollNumber = model.RollNumber > 0 ? model.RollNumber : maxRoll + 1,
                    IsActive = true
                });
            }
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Student updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return NotFound();

        student.IsActive = false;

        // Deactivate enrollments too
        var enrollments = await _context.Enrollments.Where(e => e.StudentId == id && e.IsActive).ToListAsync();
        foreach (var e in enrollments) e.IsActive = false;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Student {student.FirstName} {student.LastName} removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateClassDropdowns(AdmissionViewModel model)
    {
        var allSections = await _context.ClassSections.ToListAsync();
        model.ClassSections = allSections
            .OrderBy(c => int.TryParse(c.ClassName, out var n) ? n : 99)
            .ThenBy(c => c.Section)
            .Select(c => new ClassSectionListItem { Id = c.Id, Name = "Std " + c.ClassName + " - " + c.Section })
            .ToList();

        model.AcademicYears = await _context.AcademicYears
            .OrderByDescending(a => a.IsActive)
            .Select(a => new AcademicYearListItem { Id = a.Id, Name = a.Name })
            .ToListAsync();
    }
}
