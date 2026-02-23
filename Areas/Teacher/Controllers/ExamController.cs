using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Teacher.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Policy = "TeacherAccess")]
public class ExamController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExamController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var exams = await _context.Exams
            .Include(e => e.Course)
            .Include(e => e.ExamSchedules)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        var viewModels = exams.Select(e => new ExamViewModel
        {
            Id = e.Id,
            Name = e.Name,
            CourseId = e.CourseId,
            CourseName = e.Course.Name,
            ExamDate = e.ExamDate,
            TotalMarks = e.TotalMarks,
            Room = e.Room,
            Schedules = e.ExamSchedules.Select(s => new ExamScheduleItem
            {
                Id = s.Id,
                ExamName = e.Name,
                CourseName = e.Course.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Room = s.Room,
                Invigilator = s.Invigilator
            }).ToList()
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ExamViewModel
        {
            Courses = await _context.Courses.Select(c => new CourseOption { Id = c.Id, Name = c.Name }).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Courses = await _context.Courses.Select(c => new CourseOption { Id = c.Id, Name = c.Name }).ToListAsync();
            return View(model);
        }

        var exam = new Exam
        {
            Name = model.Name,
            CourseId = model.CourseId,
            ExamDate = model.ExamDate,
            TotalMarks = model.TotalMarks,
            Room = model.Room
        };

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        // Add schedule if provided
        if (model.StartTime.HasValue && model.EndTime.HasValue)
        {
            _context.ExamSchedules.Add(new ExamSchedule
            {
                ExamId = exam.Id,
                StartTime = model.StartTime.Value,
                EndTime = model.EndTime.Value,
                Room = model.Room,
                Invigilator = model.Invigilator
            });
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Exam created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();

        var model = new ExamViewModel
        {
            Id = exam.Id,
            Name = exam.Name,
            CourseId = exam.CourseId,
            ExamDate = exam.ExamDate,
            TotalMarks = exam.TotalMarks,
            Room = exam.Room,
            Courses = await _context.Courses.Select(c => new CourseOption { Id = c.Id, Name = c.Name }).ToListAsync()
        };

        // Populate schedule times from first schedule if exists
        var schedule = await _context.ExamSchedules.FirstOrDefaultAsync(s => s.ExamId == id);
        if (schedule != null)
        {
            model.StartTime = schedule.StartTime;
            model.EndTime = schedule.EndTime;
            model.Invigilator = schedule.Invigilator;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExamViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Courses = await _context.Courses.Select(c => new CourseOption { Id = c.Id, Name = c.Name }).ToListAsync();
            return View(model);
        }

        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();

        exam.Name = model.Name;
        exam.CourseId = model.CourseId;
        exam.ExamDate = model.ExamDate;
        exam.TotalMarks = model.TotalMarks;
        exam.Room = model.Room;

        // Update or add schedule
        var schedule = await _context.ExamSchedules.FirstOrDefaultAsync(s => s.ExamId == id);
        if (model.StartTime.HasValue && model.EndTime.HasValue)
        {
            if (schedule != null)
            {
                schedule.StartTime = model.StartTime.Value;
                schedule.EndTime = model.EndTime.Value;
                schedule.Room = model.Room;
                schedule.Invigilator = model.Invigilator;
            }
            else
            {
                _context.ExamSchedules.Add(new ExamSchedule
                {
                    ExamId = exam.Id,
                    StartTime = model.StartTime.Value,
                    EndTime = model.EndTime.Value,
                    Room = model.Room,
                    Invigilator = model.Invigilator
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Exam updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam != null)
        {
            // Remove related schedules and mark entries first
            var schedules = _context.ExamSchedules.Where(s => s.ExamId == id);
            _context.ExamSchedules.RemoveRange(schedules);
            var marks = _context.MarkEntries.Where(m => m.ExamId == id);
            _context.MarkEntries.RemoveRange(marks);
            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
