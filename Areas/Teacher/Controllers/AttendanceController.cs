using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Teacher.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Policy = "TeacherAccess")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? classSectionId, DateTime? date)
    {
        var selectedDate = date ?? DateTime.Today;
        var classSections = await _context.ClassSections
            .Select(c => new ClassSectionOption { Id = c.Id, Name = c.ClassName + "-" + c.Section })
            .ToListAsync();

        var selectedClassId = classSectionId ?? classSections.FirstOrDefault()?.Id ?? 0;

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        var yearId = activeYear?.Id ?? 0;

        var enrolledStudents = await _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.ClassSectionId == selectedClassId && e.AcademicYearId == yearId && e.IsActive)
            .OrderBy(e => e.RollNumber)
            .ToListAsync();

        var existingRecords = await _context.AttendanceRecords
            .Where(a => a.ClassSectionId == selectedClassId && a.Date.Date == selectedDate.Date)
            .ToListAsync();

        var students = enrolledStudents.Select(e => new StudentAttendanceItem
        {
            StudentId = e.StudentId,
            RollNumber = e.RollNumber,
            StudentName = e.Student.FirstName + " " + e.Student.LastName,
            IsPresent = existingRecords.FirstOrDefault(r => r.StudentId == e.StudentId)?.IsPresent ?? true
        }).ToList();

        var model = new AttendanceViewModel
        {
            ClassSectionId = selectedClassId,
            ClassName = classSections.FirstOrDefault(c => c.Id == selectedClassId)?.Name ?? "",
            Date = selectedDate,
            Students = students,
            TotalStudents = students.Count,
            PresentCount = students.Count(s => s.IsPresent),
            AbsentCount = students.Count(s => !s.IsPresent),
            ClassSections = classSections
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceViewModel model)
    {
        // Remove existing records for this class/date
        var existing = await _context.AttendanceRecords
            .Where(a => a.ClassSectionId == model.ClassSectionId && a.Date.Date == model.Date.Date)
            .ToListAsync();
        _context.AttendanceRecords.RemoveRange(existing);

        // Add new records
        foreach (var student in model.Students)
        {
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                StudentId = student.StudentId,
                ClassSectionId = model.ClassSectionId,
                Date = model.Date,
                IsPresent = student.IsPresent
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Attendance saved successfully!";
        return RedirectToAction(nameof(Index), new { classSectionId = model.ClassSectionId, date = model.Date.ToString("yyyy-MM-dd") });
    }
}
