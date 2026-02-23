using System.ComponentModel.DataAnnotations;

namespace SchoolEduERP.Areas.Teacher.Models;

public class AttendanceViewModel
{
    public int ClassSectionId { get; set; }
    public string ClassName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public string? Subject { get; set; }
    public List<StudentAttendanceItem> Students { get; set; } = new();
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }

    public List<ClassSectionOption> ClassSections { get; set; } = new();
}

public class StudentAttendanceItem
{
    public int StudentId { get; set; }
    public int RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public bool IsPresent { get; set; } = true;
}

public class ClassSectionOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MarksViewModel
{
    public int ClassSectionId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TotalMarks { get; set; } = 100;
    public bool AutoGpa { get; set; } = true;

    public List<StudentMarkItem> Students { get; set; } = new();
    public List<ClassSectionOption> ClassSections { get; set; } = new();
    public List<ExamOption> Exams { get; set; } = new();
    public List<CourseOption> Courses { get; set; } = new();

    // Summary
    public decimal ClassAverage { get; set; }
    public decimal HighestMarks { get; set; }
    public decimal LowestMarks { get; set; }
}

public class StudentMarkItem
{
    public int StudentId { get; set; }
    public int RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal MarksObtained { get; set; }

    public decimal GradePoint { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Saved, Pending
}

public class ExamViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Exam Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required]
    [Display(Name = "Exam Date")]
    [DataType(DataType.Date)]
    public DateTime ExamDate { get; set; } = DateTime.Today.AddDays(7);

    [Display(Name = "Total Marks")]
    public int TotalMarks { get; set; } = 100;

    public string? Room { get; set; }

    // For schedule
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Invigilator { get; set; }

    // Display helpers
    public string? CourseName { get; set; }
    public List<CourseOption> Courses { get; set; } = new();
    public List<ExamScheduleItem> Schedules { get; set; } = new();
}

public class ExamScheduleItem
{
    public int Id { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Room { get; set; }
    public string? Invigilator { get; set; }
}

public class ExamOption { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class CourseOption { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
