using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Services;

namespace SchoolEduERP.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAcademicYearService _academicYearService;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAcademicYearService academicYearService)
    {
        _context = context;
        _userManager = userManager;
        _academicYearService = academicYearService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        var role = roles.FirstOrDefault() ?? "Student";
        var activeYear = await _academicYearService.GetActiveYearAsync();

        if (role == "Student")
        {
            return await BuildStudentDashboard(user!, activeYear);
        }

        var model = new DashboardViewModel
        {
            TotalStudents = await _context.Students.CountAsync(s => s.IsActive),
            TotalTeachers = await _context.Teachers.CountAsync(t => t.IsActive),
            AttendancePercentage = await GetOverallAttendancePercentageAsync(),
            FeeCollected = await _context.FeePayments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.AmountPaid),
            FeeOverdue = await GetOverdueFeeAmountAsync(),
            TodayClasses = 28,
            TotalClasses = 32,
            ActiveAcademicYear = activeYear?.Name ?? "N/A",
            UserRole = role,
            UserName = user?.FullName ?? "User"
        };

        return role == "Admin" ? View("AdminDashboard", model) : View("TeacherDashboard", model);
    }

    private async Task<IActionResult> BuildStudentDashboard(ApplicationUser user, AcademicYear? activeYear)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);

        decimal attendancePct = 0;
        decimal feePaid = 0;
        int studentId = 0;

        if (student != null)
        {
            studentId = student.Id;
            var totalAtt = await _context.AttendanceRecords.CountAsync(a => a.StudentId == student.Id);
            var presentAtt = await _context.AttendanceRecords.CountAsync(a => a.StudentId == student.Id && a.IsPresent);
            attendancePct = totalAtt > 0 ? Math.Round((decimal)presentAtt / totalAtt * 100, 1) : 100;
            feePaid = await _context.FeePayments.Where(p => p.StudentId == student.Id && p.Status == "Completed").SumAsync(p => p.AmountPaid);
        }

        var model = new DashboardViewModel
        {
            TotalStudents = 0,
            TotalTeachers = await _context.Teachers.CountAsync(t => t.IsActive),
            AttendancePercentage = attendancePct,
            FeeCollected = feePaid,
            FeeOverdue = 0,
            TodayClasses = 0,
            TotalClasses = 0,
            ActiveAcademicYear = activeYear?.Name ?? "N/A",
            UserRole = "Student",
            UserName = user.FullName
        };

        ViewBag.StudentId = studentId;
        return View("StudentDashboard", model);
    }

    private async Task<decimal> GetOverallAttendancePercentageAsync()
    {
        var total = await _context.AttendanceRecords.CountAsync();
        var present = await _context.AttendanceRecords.CountAsync(a => a.IsPresent);
        return total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0;
    }

    private async Task<decimal> GetOverdueFeeAmountAsync()
    {
        var overdueFees = await _context.FeeHeads
            .Where(f => f.DueDate < DateTime.UtcNow && f.IsActive)
            .SumAsync(f => f.Amount);
        return overdueFees;
    }
}
