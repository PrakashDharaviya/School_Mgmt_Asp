using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Student.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Services;

namespace SchoolEduERP.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentAccess")]
public class FeePaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFeeReminderService _reminderService;
    private readonly UserManager<SchoolEduERP.Models.Domain.ApplicationUser> _userManager;

    public FeePaymentController(ApplicationDbContext context, IFeeReminderService reminderService, UserManager<SchoolEduERP.Models.Domain.ApplicationUser> userManager)
    {
        _context = context;
        _reminderService = reminderService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? studentId)
    {
        var user = await _userManager.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");

        SchoolEduERP.Models.Domain.Student? selectedStudent = null;

        if (isAdmin || isTeacher)
        {
            // Admin/Teacher can browse all students
            var students = await _context.Students.Where(s => s.IsActive).ToListAsync();
            selectedStudent = studentId.HasValue
                ? students.FirstOrDefault(s => s.Id == studentId.Value)
                : students.FirstOrDefault();
            ViewBag.Students = students.Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName });
            ViewBag.CanBrowse = true;
        }
        else
        {
            // Student sees only their own fees
            selectedStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user!.Id && s.IsActive);
            ViewBag.CanBrowse = false;
        }

        if (selectedStudent == null)
            return View(new FeeViewModel { StudentName = "No student record linked" });

        var activeYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear == null)
        {
            TempData["Error"] = "No active academic year set.";
            return View(new FeeViewModel { StudentName = selectedStudent.FirstName + " " + selectedStudent.LastName });
        }
        var yearId = activeYear.Id;

        var feeHeads = await _context.FeeHeads.Where(f => f.IsActive && f.AcademicYearId == yearId).ToListAsync();
        var payments = await _context.FeePayments
            .Include(p => p.FeeHead)
            .Where(p => p.StudentId == selectedStudent.Id)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        // Get student's class from enrollment
        var studentEnrollment = await _context.Enrollments
            .Include(e => e.ClassSection)
            .FirstOrDefaultAsync(e => e.StudentId == selectedStudent.Id && e.AcademicYearId == yearId && e.IsActive);
        var className = studentEnrollment != null
            ? $"{studentEnrollment.ClassSection.ClassName}-{studentEnrollment.ClassSection.Section}"
            : "N/A";

        var totalPaid = payments.Where(p => p.Status == "Completed").Sum(p => p.AmountPaid);
        var totalDue = feeHeads.Sum(f => f.Amount);

        var model = new FeeViewModel
        {
            StudentId = selectedStudent.Id,
            StudentName = $"{selectedStudent.FirstName} {selectedStudent.LastName}",
            ClassName = className,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            Balance = totalDue - totalPaid,
            FeeSchedule = feeHeads.Select(f =>
            {
                var paidForHead = payments.Where(p => p.FeeHeadId == f.Id && p.Status == "Completed").Sum(p => p.AmountPaid);
                return new FeeScheduleItem
                {
                    FeeHeadId = f.Id,
                    FeeName = f.Name,
                    Amount = f.Amount,
                    DueDate = f.DueDate,
                    AmountPaid = paidForHead,
                    Status = paidForHead >= f.Amount ? "Paid"
                        : f.DueDate < DateTime.UtcNow ? "Overdue" : "Pending"
                };
            }).ToList(),
            RecentPayments = payments.Take(10).Select(p => new FeePaymentItem
            {
                Id = p.Id,
                FeeName = p.FeeHead.Name,
                Amount = p.AmountPaid,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int studentId, int feeHeadId)
    {
        var feeHead = await _context.FeeHeads.FindAsync(feeHeadId);
        if (feeHead == null) return NotFound();

        // Verify access: students can only pay their own fees
        if (User.IsInRole("Student"))
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            if (student == null || student.Id != studentId) return Forbid();
        }

        var model = new FeePaymentFormViewModel
        {
            StudentId = studentId,
            FeeHeadId = feeHeadId,
            AmountPaid = feeHead.Amount
        };

        ViewBag.FeeName = feeHead.Name;
        ViewBag.FeeAmount = feeHead.Amount;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(FeePaymentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var feeHead = await _context.FeeHeads.FindAsync(model.FeeHeadId);
            ViewBag.FeeName = feeHead?.Name;
            ViewBag.FeeAmount = feeHead?.Amount;
            return View(model);
        }

        var payment = new SchoolEduERP.Models.Domain.FeePayment
        {
            StudentId = model.StudentId,
            FeeHeadId = model.FeeHeadId,
            AmountPaid = model.AmountPaid,
            PaymentMethod = model.PaymentMethod,
            TransactionId = model.TransactionId ?? $"TXN-{DateTime.UtcNow.Ticks}",
            PaymentDate = DateTime.UtcNow,
            Status = "Completed"
        };

        _context.FeePayments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment recorded successfully!";
        return RedirectToAction(nameof(Index), new { studentId = model.StudentId });
    }
}
