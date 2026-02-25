using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Teacher.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Services;
using DomainModels = SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Policy = "TeacherAccess")]
public class MarksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IGpaCalculatorService _gpaService;

    public MarksController(ApplicationDbContext context, IGpaCalculatorService gpaService)
    {
        _context = context;
        _gpaService = gpaService;
    }

    private record EnrolledStudentDto(DomainModels.Student Student, int RollNumber);

    public async Task<IActionResult> Index(int? examId, int? courseId)
    {
        var exams = await _context.Exams.Select(e => new ExamOption { Id = e.Id, Name = e.Name }).ToListAsync();
        var courses = await _context.Courses.Select(c => new CourseOption { Id = c.Id, Name = c.Name }).ToListAsync();
        var classSections = await _context.ClassSections
            .Select(c => new ClassSectionOption { Id = c.Id, Name = c.ClassName + "-" + c.Section }).ToListAsync();

        var selectedExamId = examId ?? exams.FirstOrDefault()?.Id ?? 0;
        var selectedCourseId = courseId ?? courses.FirstOrDefault()?.Id ?? 0;

        var exam = await _context.Exams.FindAsync(selectedExamId);

        var existingMarks = await _context.MarkEntries
            .Include(m => m.Student)
            .Where(m => m.ExamId == selectedExamId && m.CourseId == selectedCourseId)
            .ToListAsync();

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        List<EnrolledStudentDto> enrolledStudents;
        if (activeYear != null)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.AcademicYearId == activeYear.Id && e.IsActive && e.CourseId == selectedCourseId)
                .ToListAsync();
            enrolledStudents = enrollments
                .GroupBy(e => e.StudentId)
                .Select(g => new EnrolledStudentDto(g.First().Student, g.First().RollNumber))
                .ToList();
        }
        else
        {
            enrolledStudents = new List<EnrolledStudentDto>();
        }

        var students = new List<StudentMarkItem>();
        foreach (var enrolled in enrolledStudents)
        {
            var existingMark = existingMarks.FirstOrDefault(m => m.StudentId == enrolled.Student.Id);
            var marks = existingMark?.MarksObtained ?? 0;
            var (letterGrade, gradePoint) = _gpaService.GetGrade(marks, exam?.TotalMarks ?? 100);

            students.Add(new StudentMarkItem
            {
                StudentId = enrolled.Student.Id,
                RollNumber = enrolled.RollNumber,
                StudentName = enrolled.Student.FirstName + " " + enrolled.Student.LastName,
                MarksObtained = marks,
                GradePoint = existingMark?.GradePoint ?? gradePoint,
                LetterGrade = existingMark?.LetterGrade ?? letterGrade,
                Status = existingMark != null ? "Saved" : "Pending"
            });
        }

        var marksValues = students.Where(s => s.MarksObtained > 0).Select(s => s.MarksObtained).ToList();

        var model = new MarksViewModel
        {
            ExamId = selectedExamId,
            ExamName = exam?.Name ?? "N/A",
            CourseId = selectedCourseId,
            CourseName = courses.FirstOrDefault(c => c.Id == selectedCourseId)?.Name ?? "",
            TotalMarks = exam?.TotalMarks ?? 100,
            Students = students.OrderBy(s => s.RollNumber).ToList(),
            Exams = exams,
            Courses = courses,
            ClassSections = classSections,
            ClassAverage = marksValues.Any() ? Math.Round(marksValues.Average(), 1) : 0,
            HighestMarks = marksValues.Any() ? marksValues.Max() : 0,
            LowestMarks = marksValues.Any() ? marksValues.Min() : 0
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MarksViewModel model)
    {
        var exam = await _context.Exams.FindAsync(model.ExamId);
        var totalMarks = exam?.TotalMarks ?? 100;

        foreach (var student in model.Students)
        {
            var (letterGrade, gradePoint) = _gpaService.GetGrade(student.MarksObtained, totalMarks);

            var existing = await _context.MarkEntries
                .FirstOrDefaultAsync(m => m.StudentId == student.StudentId && m.ExamId == model.ExamId && m.CourseId == model.CourseId);

            if (existing != null)
            {
                existing.MarksObtained = student.MarksObtained;
                existing.GradePoint = gradePoint;
                existing.LetterGrade = letterGrade;
                _context.MarkEntries.Update(existing);
            }
            else
            {
                _context.MarkEntries.Add(new DomainModels.MarkEntry
                {
                    StudentId = student.StudentId,
                    ExamId = model.ExamId,
                    CourseId = model.CourseId,
                    MarksObtained = student.MarksObtained,
                    GradePoint = gradePoint,
                    LetterGrade = letterGrade,
                    IsPublished = false
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Marks saved successfully!";
        return RedirectToAction(nameof(Index), new { examId = model.ExamId, courseId = model.CourseId });
    }
}
