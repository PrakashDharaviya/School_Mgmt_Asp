using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Services;

namespace SchoolEduERP.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentAccess")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IGpaCalculatorService _gpaService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, IGpaCalculatorService gpaService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _gpaService = gpaService;
        _userManager = userManager;
    }

    /// <summary>
    /// Auto-detect the logged-in student and show their profile.
    /// </summary>
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
        if (student == null)
        {
            TempData["Error"] = "No student record linked to your account.";
            return RedirectToAction("Index", "Dashboard", new { area = "" });
        }

        return await BuildProfileView(student.Id);
    }

    /// <summary>
    /// Show profile by student id (for Admin/Teacher).
    /// </summary>
    public async Task<IActionResult> Index(int id)
    {
        // Students can only view their own profile
        if (User.IsInRole("Student"))
        {
            var user = await _userManager.GetUserAsync(User);
            var myStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            if (myStudent == null || myStudent.Id != id) return Forbid();
        }

        return await BuildProfileView(id);
    }

    /// <summary>
    /// Student sees their own attendance read-only.
    /// </summary>
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyAttendance()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
        if (student == null)
        {
            TempData["Error"] = "No student record linked to your account.";
            return RedirectToAction("Index", "Dashboard", new { area = "" });
        }

        var records = await _context.AttendanceRecords
            .Where(a => a.StudentId == student.Id)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        var totalDays = records.Count;
        var presentDays = records.Count(r => r.IsPresent);
        var absentDays = totalDays - presentDays;
        var pct = totalDays > 0 ? Math.Round((decimal)presentDays / totalDays * 100, 1) : 100;

        ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
        ViewBag.TotalDays = totalDays;
        ViewBag.PresentDays = presentDays;
        ViewBag.AbsentDays = absentDays;
        ViewBag.Percentage = pct;
        ViewBag.Records = records.Take(30).Select(r => new { r.Date, r.IsPresent }).ToList();

        return View();
    }

    private async Task<IActionResult> BuildProfileView(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return NotFound();

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        var enrollment = await _context.Enrollments
            .Include(e => e.ClassSection)
            .FirstOrDefaultAsync(e => e.StudentId == id && e.AcademicYearId == (activeYear != null ? activeYear.Id : 1) && e.IsActive);

        var marks = await _context.MarkEntries
            .Include(m => m.Exam)
            .Include(m => m.Course)
            .Where(m => m.StudentId == id)
            .ToListAsync();

        var totalAttendance = await _context.AttendanceRecords.CountAsync(a => a.StudentId == id);
        var presentCount = await _context.AttendanceRecords.CountAsync(a => a.StudentId == id && a.IsPresent);
        var attendancePct = totalAttendance > 0 ? Math.Round((decimal)presentCount / totalAttendance * 100, 1) : 100;

        var markLines = marks.Select(m =>
        {
            var (grade, gp) = _gpaService.GetGrade(m.MarksObtained, m.Exam.TotalMarks);
            return new MarkReportLine
            {
                SubjectName = m.Course.Name,
                ExamName = m.Exam.Name,
                MarksObtained = m.MarksObtained,
                TotalMarks = m.Exam.TotalMarks,
                Grade = m.LetterGrade ?? grade,
                GradePoint = m.GradePoint ?? gp
            };
        }).ToList();

        var gpa = markLines.Any() ? Math.Round(markLines.Average(m => m.GradePoint), 2) : 0;

        var totalDue = await _context.FeeHeads.Where(f => f.IsActive && f.AcademicYearId == (activeYear != null ? activeYear.Id : 1)).SumAsync(f => f.Amount);
        var totalPaid = await _context.FeePayments.Where(p => p.StudentId == id && p.Status == "Completed").SumAsync(p => p.AmountPaid);

        var model = new StudentProfileViewModel
        {
            Id = student.Id,
            FullName = $"{student.FirstName} {student.LastName}",
            AdmissionNumber = student.AdmissionNumber,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Address = student.Address,
            GuardianName = student.GuardianName,
            GuardianPhone = student.GuardianPhone,
            AdmissionDate = student.AdmissionDate,
            ClassName = enrollment != null ? $"{enrollment.ClassSection.ClassName}-{enrollment.ClassSection.Section}" : "N/A",
            RollNumber = enrollment?.RollNumber ?? 0,
            AcademicYear = activeYear?.Name ?? "N/A",
            Gpa = gpa,
            AttendancePercentage = attendancePct,
            Marks = markLines,
            TotalFeeDue = totalDue,
            TotalFeePaid = totalPaid
        };

        return View("Index", model);
    }
}
