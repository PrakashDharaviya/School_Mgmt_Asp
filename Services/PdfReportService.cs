using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Services;

public interface IPdfReportService
{
    byte[] GenerateStudentReportCard(StudentReportData data);
    byte[] GenerateFeeReceipt(FeeReceiptData data);
    byte[] GenerateAttendanceReport(AttendanceReportData data);
    byte[] GenerateClassResultReport(ClassResultData data);
}

public class PdfReportService : IPdfReportService
{
    public byte[] GenerateStudentReportCard(StudentReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("School ERP System").Bold().FontSize(20).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("Student Report Card").FontSize(14).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"Academic Year: {data.AcademicYear}").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Date: {DateTime.Now:dd-MMM-yyyy}").FontSize(9);
                            c.Item().Text($"Admission No: {data.AdmissionNumber}").FontSize(9).Bold();
                        });
                    });
                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // Student Info
                    col.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Student Name: {data.StudentName}").Bold().FontSize(12);
                            c.Item().Text($"Class: {data.ClassName} | Roll No: {data.RollNumber}");
                            c.Item().Text($"Date of Birth: {data.DateOfBirth:dd-MMM-yyyy}");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Overall GPA: {data.OverallGpa:F2}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            c.Item().Text($"Attendance: {data.AttendancePercentage}%").FontSize(11);
                        });
                    });

                    // Marks Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Subject").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Marks Obtained").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Total Marks").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Grade").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("GPA").FontColor(Colors.White).Bold();
                        });

                        // Rows
                        var alternate = false;
                        foreach (var mark in data.Marks)
                        {
                            var bg = alternate ? Colors.Grey.Lighten4 : Colors.White;
                            table.Cell().Background(bg).Padding(6).Text(mark.SubjectName);
                            table.Cell().Background(bg).Padding(6).Text($"{mark.MarksObtained:F1}");
                            table.Cell().Background(bg).Padding(6).Text($"{mark.TotalMarks}");
                            table.Cell().Background(bg).Padding(6).Text(mark.LetterGrade).Bold();
                            table.Cell().Background(bg).Padding(6).Text($"{mark.GradePoint:F1}");
                            alternate = !alternate;
                        }
                    });

                    // Summary
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(15).Column(c =>
                        {
                            c.Item().Text("Summary").Bold().FontSize(12);
                            c.Item().PaddingTop(5).Text($"Total Marks: {data.Marks.Sum(m => m.MarksObtained):F1} / {data.Marks.Sum(m => m.TotalMarks)}");
                            c.Item().Text($"Percentage: {(data.Marks.Sum(m => m.TotalMarks) > 0 ? data.Marks.Sum(m => m.MarksObtained) / data.Marks.Sum(m => m.TotalMarks) * 100 : 0):F1}%");
                            c.Item().Text($"Cumulative GPA: {data.OverallGpa:F2}").Bold();
                            c.Item().Text($"Result: {(data.OverallGpa >= 1.5m ? "PASS" : "FAIL")}").Bold()
                                .FontColor(data.OverallGpa >= 1.5m ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("School ERP © 2025 | Generated on ");
                    t.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).Bold();
                    t.Span(" | Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateFeeReceipt(FeeReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("School ERP System").Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("FEE RECEIPT").Bold().FontSize(12).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(140).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Receipt No: {data.ReceiptNumber}").FontSize(9).Bold();
                            c.Item().Text($"Date: {data.PaymentDate:dd-MMM-yyyy}").FontSize(9);
                        });
                    });
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Student: {data.StudentName}").Bold();
                            c.Item().Text($"Admission No: {data.AdmissionNumber}");
                            c.Item().Text($"Class: {data.ClassName}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Fee Head").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Amount Due").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Amount Paid").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Padding(6).Text(item.FeeName);
                            table.Cell().Padding(6).Text($"₹{item.AmountDue:N2}");
                            table.Cell().Padding(6).Text($"₹{item.AmountPaid:N2}");
                        }

                        // Total row
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Total").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text($"₹{data.Items.Sum(i => i.AmountDue):N2}").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text($"₹{data.Items.Sum(i => i.AmountPaid):N2}").Bold();
                    });

                    col.Item().PaddingTop(10).Text($"Payment Method: {data.PaymentMethod}").FontSize(9);
                    if (!string.IsNullOrEmpty(data.TransactionId))
                        col.Item().Text($"Transaction ID: {data.TransactionId}").FontSize(9);

                    col.Item().PaddingTop(20).AlignCenter().Text("This is a computer-generated receipt.").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateAttendanceReport(AttendanceReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("School ERP System - Attendance Report").Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"{data.ClassName} | {data.Month} {data.Year}").FontSize(12);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Green.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Total Students: {data.Students.Count}").Bold();
                            c.Item().Text($"Working Days: {data.TotalWorkingDays}");
                            c.Item().Text($"Average Attendance: {data.AverageAttendance:F1}%");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Roll").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Student Name").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Present").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Absent").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Attendance %").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        var alt = false;
                        foreach (var s in data.Students)
                        {
                            var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                            var pct = data.TotalWorkingDays > 0 ? (double)s.PresentDays / data.TotalWorkingDays * 100 : 0;
                            table.Cell().Background(bg).Padding(5).Text(s.RollNumber.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(s.StudentName).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(s.PresentDays.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(s.AbsentDays.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text($"{pct:F1}%").FontSize(9)
                                .FontColor(pct < 75 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            alt = !alt;
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ").FontSize(8);
                    t.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).Bold().FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateClassResultReport(ClassResultData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("School ERP System - Class Result Report").Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"{data.ClassName} | {data.ExamName} | Academic Year: {data.AcademicYear}").FontSize(11);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Total Students: {data.Results.Count}").Bold();
                            c.Item().Text($"Class Average: {data.ClassAverage:F1}%");
                            c.Item().Text($"Pass Rate: {data.PassRate:F1}%");
                        });
                        row.RelativeItem().Background(Colors.Green.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Highest: {data.Highest:F1}").Bold();
                            c.Item().Text($"Lowest: {data.Lowest:F1}");
                            c.Item().Text($"Average GPA: {data.AverageGpa:F2}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Roll").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Student Name").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Marks").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Grade").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("GPA").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Result").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        var alt = false;
                        foreach (var r in data.Results.OrderByDescending(r => r.MarksObtained))
                        {
                            var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                            var passed = r.GradePoint >= 1.5m;
                            table.Cell().Background(bg).Padding(5).Text(r.RollNumber.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(r.StudentName).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text($"{r.MarksObtained:F1}/{r.TotalMarks}").FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(r.LetterGrade).Bold().FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text($"{r.GradePoint:F1}").FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(passed ? "PASS" : "FAIL").FontSize(9).Bold()
                                .FontColor(passed ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            alt = !alt;
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ").FontSize(8);
                    t.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).Bold().FontSize(8);
                    t.Span(" | Page ");
                    t.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }
}

// Data transfer objects for PDF generation
public class StudentReportData
{
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int RollNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal OverallGpa { get; set; }
    public decimal AttendancePercentage { get; set; }
    public List<MarkReportItem> Marks { get; set; } = new();
}

public class MarkReportItem
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal MarksObtained { get; set; }
    public int TotalMarks { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public decimal GradePoint { get; set; }
}

public class FeeReceiptData
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public List<FeeReceiptItem> Items { get; set; } = new();
}

public class FeeReceiptItem
{
    public string FeeName { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
}

public class AttendanceReportData
{
    public string ClassName { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TotalWorkingDays { get; set; }
    public decimal AverageAttendance { get; set; }
    public List<StudentAttendanceReportItem> Students { get; set; } = new();
}

public class StudentAttendanceReportItem
{
    public int RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
}

public class ClassResultData
{
    public string ClassName { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public decimal ClassAverage { get; set; }
    public decimal Highest { get; set; }
    public decimal Lowest { get; set; }
    public decimal PassRate { get; set; }
    public decimal AverageGpa { get; set; }
    public List<StudentResultItem> Results { get; set; } = new();
}

public class StudentResultItem
{
    public int RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal MarksObtained { get; set; }
    public int TotalMarks { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public decimal GradePoint { get; set; }
}
