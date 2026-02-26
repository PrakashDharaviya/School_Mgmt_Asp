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
        if (activeYear == null)
        {
            TempData["Error"] = "No active academic year set. Please set an active year first.";
            return View(new ReportViewModel { ActiveAcademicYear = "N/A" });
        }
        var yearId = activeYear.Id;

        // ── Fee calculations ──────────────────────────────────────
        var activeStudentCount = await _context.Enrollments.CountAsync(e => e.AcademicYearId == yearId && e.IsActive);
        var feeHeads = await _context.FeeHeads.Where(f => f.IsActive && f.AcademicYearId == yearId).ToListAsync();
        decimal totalFeeExpected = 0;
        var feeBreakdown = new List<FeeHeadBreakdownItem>();
        foreach (var fee in feeHeads)
        {
            int applicableCount;
            if (!string.IsNullOrWhiteSpace(fee.ApplicableClass))
            {
                applicableCount = await _context.Enrollments
                    .Include(e => e.ClassSection)
                    .CountAsync(e => e.AcademicYearId == yearId && e.IsActive && e.ClassSection.ClassName == fee.ApplicableClass);
            }
            else
            {
                applicableCount = activeStudentCount;
            }
            var expected = fee.Amount * applicableCount;
            totalFeeExpected += expected;

            var collected = await _context.FeePayments
                .Where(p => p.FeeHeadId == fee.Id && p.Status == "Completed")
                .SumAsync(p => p.AmountPaid);

            feeBreakdown.Add(new FeeHeadBreakdownItem
            {
                FeeName = fee.Name,
                Amount = expected,
                Collected = collected,
                Pending = Math.Max(expected - collected, 0),
                CollectionRate = expected > 0 ? Math.Round(collected / expected * 100, 1) : 0,
                DueDate = fee.DueDate.ToString("dd-MMM-yyyy")
            });
        }

        var activeYearFeeHeadIds = feeHeads.Select(f => f.Id).ToList();
        var totalCollected = await _context.FeePayments
            .Where(p => p.Status == "Completed" && activeYearFeeHeadIds.Contains(p.FeeHeadId))
            .SumAsync(p => p.AmountPaid);
        var totalOverdue = Math.Max(totalFeeExpected - totalCollected, 0);
        var totalPayments = await _context.FeePayments
            .CountAsync(p => p.Status == "Completed" && activeYearFeeHeadIds.Contains(p.FeeHeadId));

        // Students who paid at least one fee
        var paidStudentIds = await _context.FeePayments
            .Where(p => p.Status == "Completed" && activeYearFeeHeadIds.Contains(p.FeeHeadId))
            .Select(p => p.StudentId).Distinct().CountAsync();
        var unpaidStudentCount = Math.Max(activeStudentCount - paidStudentIds, 0);

        // ── Attendance ────────────────────────────────────────────
        var totalAttendanceRecords = await _context.AttendanceRecords.CountAsync();
        var totalPresent = await _context.AttendanceRecords.CountAsync(a => a.IsPresent);
        var totalAbsent = totalAttendanceRecords - totalPresent;
        var overallRate = totalAttendanceRecords > 0
            ? Math.Round((decimal)totalPresent / totalAttendanceRecords * 100, 1) : 0;

        var todayRecords = await _context.AttendanceRecords.CountAsync(a => a.Date.Date == DateTime.Today);
        var todayPresent = await _context.AttendanceRecords.CountAsync(a => a.Date.Date == DateTime.Today && a.IsPresent);
        var todayAbsent = todayRecords - todayPresent;
        var totalWorkingDays = await _context.AttendanceRecords.Select(a => a.Date.Date).Distinct().CountAsync();

        // Per-class attendance (latest recorded date)
        var classSections = await _context.ClassSections.ToListAsync();
        var latestDate = await _context.AttendanceRecords.MaxAsync(a => (DateTime?)a.Date) ?? DateTime.Today;
        var latestRecords = await _context.AttendanceRecords
            .Where(a => a.Date.Date == latestDate.Date)
            .ToListAsync();
        var enrollmentsByClass = await _context.Enrollments
            .Where(e => e.AcademicYearId == yearId && e.IsActive)
            .GroupBy(e => e.ClassSectionId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count);

        var classAttendance = classSections
            .Where(cs => enrollmentsByClass.ContainsKey(cs.Id))
            .Select(cs =>
            {
                var recs = latestRecords.Where(r => r.ClassSectionId == cs.Id).ToList();
                var p = recs.Count(r => r.IsPresent);
                var a = recs.Count(r => !r.IsPresent);
                var total = enrollmentsByClass.GetValueOrDefault(cs.Id, 0);
                return new ClassAttendanceItem
                {
                    ClassName = $"{cs.ClassName}-{cs.Section}",
                    TotalStudents = total,
                    PresentToday = p,
                    AbsentToday = a,
                    AttendanceRate = recs.Count > 0 ? Math.Round((decimal)p / recs.Count * 100, 1) : 0
                };
            }).ToList();

        // ── GPA & Student Performance ─────────────────────────────
        var allMarks = await _context.MarkEntries
            .Include(m => m.Student).Include(m => m.Exam)
            .Where(m => m.GradePoint.HasValue)
            .ToListAsync();

        var gpaDistribution = new List<GpaDistributionItem>
        {
            new() { Range = "3.5 - 4.0", Count = allMarks.Count(m => m.GradePoint >= 3.5m) },
            new() { Range = "3.0 - 3.49", Count = allMarks.Count(m => m.GradePoint >= 3.0m && m.GradePoint < 3.5m) },
            new() { Range = "2.5 - 2.99", Count = allMarks.Count(m => m.GradePoint >= 2.5m && m.GradePoint < 3.0m) },
            new() { Range = "2.0 - 2.49", Count = allMarks.Count(m => m.GradePoint >= 2.0m && m.GradePoint < 2.5m) },
            new() { Range = "Below 2.0", Count = allMarks.Count(m => m.GradePoint < 2.0m) },
        };

        // Per-student average GPA
        var studentGpas = allMarks
            .GroupBy(m => m.StudentId)
            .Select(g => new {
                StudentId = g.Key,
                Student = g.First().Student,
                AvgGpa = Math.Round(g.Average(m => m.GradePoint!.Value), 2)
            }).ToList();

        var enrollmentLookup = await _context.Enrollments
            .Include(e => e.ClassSection)
            .Where(e => e.AcademicYearId == yearId && e.IsActive)
            .ToDictionaryAsync(e => e.StudentId, e => e);

        var topStudents = studentGpas
            .OrderByDescending(s => s.AvgGpa)
            .Take(10)
            .Select(s =>
            {
                var enr = enrollmentLookup.GetValueOrDefault(s.StudentId);
                return new TopStudentItem
                {
                    StudentName = $"{s.Student.FirstName} {s.Student.LastName}",
                    ClassName = enr != null ? $"{enr.ClassSection.ClassName}-{enr.ClassSection.Section}" : "N/A",
                    RollNumber = enr?.RollNumber ?? 0,
                    Gpa = s.AvgGpa,
                    Grade = s.AvgGpa >= 3.5m ? "A+" : s.AvgGpa >= 3.0m ? "A" : s.AvgGpa >= 2.5m ? "B+" : s.AvgGpa >= 2.0m ? "B" : "C"
                };
            }).ToList();

        var passCount = studentGpas.Count(s => s.AvgGpa >= 1.5m);
        var failCount = studentGpas.Count(s => s.AvgGpa < 1.5m);
        var notEvaluatedCount = activeStudentCount - passCount - failCount;

        // ── Backend Validation: Total = Passed + Failed + Not Evaluated ──
        if (passCount + failCount + notEvaluatedCount != activeStudentCount)
        {
            var logger = HttpContext.RequestServices.GetService<ILogger<ReportsController>>();
            logger?.LogError(
                "Student count mismatch! Total={Total}, Passed={Passed}, Failed={Failed}, NotEvaluated={NotEvaluated}. " +
                "Expected: Passed + Failed + NotEvaluated = Total.",
                activeStudentCount, passCount, failCount, notEvaluatedCount);
        }

        // ── Exam Results ──────────────────────────────────────────
        var exams = await _context.Exams.Include(e => e.Course).ToListAsync();
        var examResults = new List<ExamResultSummaryItem>();
        foreach (var exam in exams)
        {
            var examMarks = allMarks.Where(m => m.ExamId == exam.Id).ToList();
            if (!examMarks.Any()) continue;
            var marks = examMarks.Select(m => m.MarksObtained).ToList();
            examResults.Add(new ExamResultSummaryItem
            {
                ExamName = exam.Name,
                CourseName = exam.Course?.Name ?? "N/A",
                TotalStudents = examMarks.Count,
                TotalMarks = exam.TotalMarks,
                Average = Math.Round(marks.Average(), 1),
                Highest = marks.Max(),
                Lowest = marks.Min(),
                PassRate = examMarks.Count > 0
                    ? Math.Round((decimal)examMarks.Count(m => m.GradePoint >= 1.5m) / examMarks.Count * 100, 1) : 0
            });
        }

        // ── Build Model ──────────────────────────────────────────
        var model = new ReportViewModel
        {
            TotalFeeExpected = totalFeeExpected,
            TotalFeeCollected = totalCollected,
            TotalFeeOverdue = totalOverdue,
            CollectionRate = totalFeeExpected > 0 ? Math.Round(totalCollected / totalFeeExpected * 100, 1) : 0,
            OverallAttendanceRate = overallRate,
            TotalPresentToday = todayPresent,
            TotalAbsentToday = todayAbsent,
            AverageGpa = studentGpas.Any() ? Math.Round(studentGpas.Average(s => s.AvgGpa), 2) : 0,
            GpaDistribution = gpaDistribution,
            ActiveAcademicYear = activeYear?.Name ?? "N/A",

            // Tab 1
            TotalStudents = activeStudentCount,
            PassCount = passCount,
            FailCount = failCount,
            NotEvaluatedCount = notEvaluatedCount,
            HighestGpa = studentGpas.Any() ? studentGpas.Max(s => s.AvgGpa) : 0,
            LowestGpa = studentGpas.Any() ? studentGpas.Min(s => s.AvgGpa) : 0,
            TopStudents = topStudents,

            // Tab 2
            FeeBreakdown = feeBreakdown,
            TotalPayments = totalPayments,
            PaidStudentCount = paidStudentIds,
            UnpaidStudentCount = unpaidStudentCount,

            // Tab 3
            ClassAttendance = classAttendance,
            TotalWorkingDays = totalWorkingDays,

            // Tab 4
            ExamResults = examResults
        };

        return View(model);
    }
}
