using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Services;

namespace SchoolEduERP.Controllers;

[Authorize]
public class ReportDownloadController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfReportService _pdfService;
    private readonly IGpaCalculatorService _gpaService;

    public ReportDownloadController(ApplicationDbContext context, IPdfReportService pdfService, IGpaCalculatorService gpaService)
    {
        _context = context;
        _pdfService = pdfService;
        _gpaService = gpaService;
    }

    [HttpGet]
    public async Task<IActionResult> StudentReportCard(int studentId)
    {
        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear == null) return NotFound("No active academic year set.");

        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.ClassSection)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.AcademicYearId == activeYear.Id && e.IsActive);

        if (enrollment == null) return NotFound("Student enrollment not found.");

        var marks = await _context.MarkEntries
            .Include(m => m.Exam)
            .Include(m => m.Course)
            .Where(m => m.StudentId == studentId
                && m.Exam.ExamDate >= activeYear.StartDate
                && m.Exam.ExamDate <= activeYear.EndDate)
            .ToListAsync();

        var totalDays = await _context.AttendanceRecords.CountAsync(a => a.StudentId == studentId);
        var presentDays = await _context.AttendanceRecords.CountAsync(a => a.StudentId == studentId && a.IsPresent);
        var attendancePct = totalDays > 0 ? Math.Round((decimal)presentDays / totalDays * 100, 1) : 100;

        var markItems = marks.Select(m =>
        {
            var (grade, gp) = _gpaService.GetGrade(m.MarksObtained, m.Exam.TotalMarks);
            return new MarkReportItem
            {
                SubjectName = m.Course.Name,
                MarksObtained = m.MarksObtained,
                TotalMarks = m.Exam.TotalMarks,
                LetterGrade = m.LetterGrade ?? grade,
                GradePoint = m.GradePoint ?? gp
            };
        }).ToList();

        var overallGpa = markItems.Any() ? Math.Round(markItems.Average(m => m.GradePoint), 2) : 0;

        var data = new StudentReportData
        {
            StudentName = $"{enrollment.Student.FirstName} {enrollment.Student.LastName}",
            AdmissionNumber = enrollment.Student.AdmissionNumber,
            ClassName = $"{enrollment.ClassSection.ClassName}-{enrollment.ClassSection.Section}",
            RollNumber = enrollment.RollNumber,
            DateOfBirth = enrollment.Student.DateOfBirth,
            AcademicYear = activeYear?.Name ?? "N/A",
            OverallGpa = overallGpa,
            AttendancePercentage = attendancePct,
            Marks = markItems
        };

        var pdf = _pdfService.GenerateStudentReportCard(data);
        return File(pdf, "application/pdf", $"ReportCard_{enrollment.Student.AdmissionNumber}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> FeeReceipt(int paymentId)
    {
        var payment = await _context.FeePayments
            .Include(p => p.Student)
            .Include(p => p.FeeHead)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null) return NotFound();

        var enrollment = await _context.Enrollments
            .Include(e => e.ClassSection)
            .FirstOrDefaultAsync(e => e.StudentId == payment.StudentId && e.IsActive);

        var data = new FeeReceiptData
        {
            ReceiptNumber = $"RCP-{payment.Id:D6}",
            StudentName = $"{payment.Student.FirstName} {payment.Student.LastName}",
            AdmissionNumber = payment.Student.AdmissionNumber,
            ClassName = enrollment != null ? $"{enrollment.ClassSection.ClassName}-{enrollment.ClassSection.Section}" : "N/A",
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            TransactionId = payment.TransactionId,
            Items = new List<FeeReceiptItem>
            {
                new FeeReceiptItem
                {
                    FeeName = payment.FeeHead.Name,
                    AmountDue = payment.FeeHead.Amount,
                    AmountPaid = payment.AmountPaid
                }
            }
        };

        var pdf = _pdfService.GenerateFeeReceipt(data);
        return File(pdf, "application/pdf", $"FeeReceipt_{payment.Id}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> AttendanceReport(int classSectionId, int month, int year)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
            return BadRequest("Invalid month or year value.");

        var classSection = await _context.ClassSections.FindAsync(classSectionId);
        if (classSection == null) return NotFound();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var records = await _context.AttendanceRecords
            .Include(a => a.Student)
            .Where(a => a.ClassSectionId == classSectionId && a.Date >= startDate && a.Date < endDate)
            .ToListAsync();

        var enrollments = await _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.ClassSectionId == classSectionId && e.IsActive)
            .OrderBy(e => e.RollNumber)
            .ToListAsync();

        var workingDays = records.Select(r => r.Date.Date).Distinct().Count();
        if (workingDays == 0) workingDays = 1;

        var students = enrollments.Select(e =>
        {
            var studentRecords = records.Where(r => r.StudentId == e.StudentId).ToList();
            var present = studentRecords.Count(r => r.IsPresent);
            var absent = studentRecords.Count(r => !r.IsPresent);
            return new StudentAttendanceReportItem
            {
                RollNumber = e.RollNumber,
                StudentName = $"{e.Student.FirstName} {e.Student.LastName}",
                PresentDays = present,
                AbsentDays = absent
            };
        }).ToList();

        var avgAtt = students.Any() ? (decimal)students.Average(s => s.PresentDays) / workingDays * 100 : 0;

        var data = new AttendanceReportData
        {
            ClassName = $"{classSection.ClassName}-{classSection.Section}",
            Month = startDate.ToString("MMMM"),
            Year = year,
            TotalWorkingDays = workingDays,
            AverageAttendance = Math.Round(avgAtt, 1),
            Students = students
        };

        var pdf = _pdfService.GenerateAttendanceReport(data);
        return File(pdf, "application/pdf", $"Attendance_{classSection.ClassName}{classSection.Section}_{startDate:MMyyyy}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ClassResult(int examId, int courseId)
    {
        var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);

        var marks = await _context.MarkEntries
            .Include(m => m.Student)
            .Where(m => m.ExamId == examId && m.CourseId == courseId)
            .ToListAsync();

        // Batch load enrollments for all students in marks
        var studentIds = marks.Select(m => m.StudentId).Distinct().ToList();
        var enrollments = await _context.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.IsActive)
            .ToDictionaryAsync(e => e.StudentId, e => e.RollNumber);

        var results = new List<StudentResultItem>();
        foreach (var m in marks)
        {
            var (grade, gp) = _gpaService.GetGrade(m.MarksObtained, exam.TotalMarks);
            results.Add(new StudentResultItem
            {
                RollNumber = enrollments.GetValueOrDefault(m.StudentId, 0),
                StudentName = $"{m.Student.FirstName} {m.Student.LastName}",
                MarksObtained = m.MarksObtained,
                TotalMarks = exam.TotalMarks,
                LetterGrade = m.LetterGrade ?? grade,
                GradePoint = m.GradePoint ?? gp
            });
        }

        var marksValues = results.Select(r => r.MarksObtained).ToList();
        var data = new ClassResultData
        {
            ClassName = exam.Course?.Name ?? "N/A",
            ExamName = exam.Name,
            AcademicYear = activeYear?.Name ?? "N/A",
            ClassAverage = marksValues.Any() ? Math.Round(marksValues.Average(), 1) : 0,
            Highest = marksValues.Any() ? marksValues.Max() : 0,
            Lowest = marksValues.Any() ? marksValues.Min() : 0,
            PassRate = results.Any() ? Math.Round((decimal)results.Count(r => r.GradePoint >= 1.5m) / results.Count * 100, 1) : 0,
            AverageGpa = results.Any() ? Math.Round(results.Average(r => r.GradePoint), 2) : 0,
            Results = results
        };

        var pdf = _pdfService.GenerateClassResultReport(data);
        return File(pdf, "application/pdf", $"ClassResult_{exam.Name.Replace(" ", "_")}.pdf");
    }
}
