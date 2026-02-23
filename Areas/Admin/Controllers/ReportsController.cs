using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Services;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicYearService _academicYearService;
    private readonly IGpaCalculatorService _gpaService;

    public ReportsController(
        ApplicationDbContext context,
        IAcademicYearService academicYearService,
        IGpaCalculatorService gpaService)
    {
        _context = context;
        _academicYearService = academicYearService;
        _gpaService = gpaService;
    }

    public async Task<IActionResult> Index()
    {
        var activeYear = await _academicYearService.GetActiveYearAsync();
        var yearId = activeYear?.Id ?? 1;

        // Fee calculations - per student
        var activeStudentCount = await _context.Enrollments.CountAsync(e => e.AcademicYearId == yearId && e.IsActive);
        var feePerStudent = await _context.FeeHeads.Where(f => f.IsActive && f.AcademicYearId == yearId).SumAsync(f => f.Amount);
        var totalFeeExpected = feePerStudent * activeStudentCount;
        var totalCollected = await _context.FeePayments.Where(p => p.Status == "Completed").SumAsync(p => p.AmountPaid);
        var totalOverdue = totalFeeExpected - totalCollected;
        if (totalOverdue < 0) totalOverdue = 0;

        // Overall attendance (all records, not just today - seed data has historical records)
        var totalAttendanceRecords = await _context.AttendanceRecords.CountAsync();
        var totalPresent = await _context.AttendanceRecords.CountAsync(a => a.IsPresent);
        var totalAbsent = totalAttendanceRecords - totalPresent;
        var overallRate = totalAttendanceRecords > 0
            ? Math.Round((decimal)totalPresent / totalAttendanceRecords * 100, 1)
            : 0;

        // GPA distribution
        var allMarks = await _context.MarkEntries.Where(m => m.GradePoint.HasValue).ToListAsync();
        var gpaDistribution = new List<GpaDistributionItem>
        {
            new() { Range = "3.5 - 4.0", Count = allMarks.Count(m => m.GradePoint >= 3.5m) },
            new() { Range = "3.0 - 3.49", Count = allMarks.Count(m => m.GradePoint >= 3.0m && m.GradePoint < 3.5m) },
            new() { Range = "2.5 - 2.99", Count = allMarks.Count(m => m.GradePoint >= 2.5m && m.GradePoint < 3.0m) },
            new() { Range = "2.0 - 2.49", Count = allMarks.Count(m => m.GradePoint >= 2.0m && m.GradePoint < 2.5m) },
            new() { Range = "Below 2.0", Count = allMarks.Count(m => m.GradePoint < 2.0m) },
        };

        var model = new ReportViewModel
        {
            TotalFeeExpected = totalFeeExpected,
            TotalFeeCollected = totalCollected,
            TotalFeeOverdue = totalOverdue,
            CollectionRate = totalFeeExpected > 0 ? Math.Round(totalCollected / totalFeeExpected * 100, 1) : 0,
            OverallAttendanceRate = overallRate,
            TotalPresentToday = totalPresent,
            TotalAbsentToday = totalAbsent,
            AverageGpa = allMarks.Any() ? Math.Round(allMarks.Where(m => m.GradePoint.HasValue).Average(m => m.GradePoint!.Value), 2) : 0,
            GpaDistribution = gpaDistribution,
            ActiveAcademicYear = activeYear?.Name ?? "N/A"
        };

        return View(model);
    }
}
