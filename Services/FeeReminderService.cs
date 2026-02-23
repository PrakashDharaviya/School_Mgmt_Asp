using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Services;

public interface IFeeReminderService
{
    Task<IEnumerable<ReminderLog>> GetPendingRemindersAsync();
    Task<IEnumerable<FeeHead>> GetUpcomingDuesAsync(int daysAhead = 7);
    Task<IEnumerable<FeeHead>> GetOverdueFeeHeadsAsync();
    Task GenerateRemindersAsync();
}

public class FeeReminderService : IFeeReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeeReminderService> _logger;

    public FeeReminderService(ApplicationDbContext context, ILogger<FeeReminderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ReminderLog>> GetPendingRemindersAsync()
        => await _context.ReminderLogs.Where(r => !r.IsSent).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IEnumerable<FeeHead>> GetUpcomingDuesAsync(int daysAhead = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.FeeHeads
            .Where(f => f.DueDate <= cutoff && f.DueDate >= DateTime.UtcNow && f.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<FeeHead>> GetOverdueFeeHeadsAsync()
        => await _context.FeeHeads.Where(f => f.DueDate < DateTime.UtcNow && f.IsActive).ToListAsync();

    public async Task GenerateRemindersAsync()
    {
        var overdueFees = await GetOverdueFeeHeadsAsync();
        var upcomingFees = await GetUpcomingDuesAsync();

        // Get students who haven't paid overdue fees
        foreach (var fee in overdueFees)
        {
            var paidStudentIds = await _context.FeePayments
                .Where(p => p.FeeHeadId == fee.Id && p.Status == "Completed")
                .Select(p => p.StudentId)
                .ToListAsync();

            // Filter students by applicable class if specified
            IQueryable<Student> studentQuery = _context.Students.Where(s => s.IsActive && !paidStudentIds.Contains(s.Id));
            if (!string.IsNullOrWhiteSpace(fee.ApplicableClass))
            {
                var applicableStudentIds = await _context.Enrollments
                    .Include(e => e.ClassSection)
                    .Where(e => e.IsActive && e.ClassSection.ClassName == fee.ApplicableClass)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();
                studentQuery = studentQuery.Where(s => applicableStudentIds.Contains(s.Id));
            }

            var unpaidStudents = await studentQuery.ToListAsync();

            foreach (var student in unpaidStudents)
            {
                var existing = await _context.ReminderLogs
                    .AnyAsync(r => r.StudentId == student.Id && r.ReminderType == "FeeOverdue" && r.CreatedAt.Date == DateTime.UtcNow.Date);

                if (!existing)
                {
                    _context.ReminderLogs.Add(new ReminderLog
                    {
                        StudentId = student.Id,
                        ReminderType = "FeeOverdue",
                        Message = $"Fee '{fee.Name}' of ₹{fee.Amount} is overdue (due: {fee.DueDate:dd-MMM-yyyy})",
                        IsSent = false
                    });
                }
            }
        }

        // Upcoming fee reminders
        foreach (var fee in upcomingFees)
        {
            var paidStudentIds = await _context.FeePayments
                .Where(p => p.FeeHeadId == fee.Id && p.Status == "Completed")
                .Select(p => p.StudentId)
                .ToListAsync();

            // Filter students by applicable class if specified for upcoming fees
            IQueryable<Student> upcomingStudentQuery = _context.Students.Where(s => s.IsActive && !paidStudentIds.Contains(s.Id));
            if (!string.IsNullOrWhiteSpace(fee.ApplicableClass))
            {
                var applicableStudentIds = await _context.Enrollments
                    .Include(e => e.ClassSection)
                    .Where(e => e.IsActive && e.ClassSection.ClassName == fee.ApplicableClass)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();
                upcomingStudentQuery = upcomingStudentQuery.Where(s => applicableStudentIds.Contains(s.Id));
            }

            var unpaidStudents = await upcomingStudentQuery.ToListAsync();

            foreach (var student in unpaidStudents)
            {
                var existing = await _context.ReminderLogs
                    .AnyAsync(r => r.StudentId == student.Id && r.ReminderType == "FeeUpcoming" && r.CreatedAt.Date == DateTime.UtcNow.Date);

                if (!existing)
                {
                    _context.ReminderLogs.Add(new ReminderLog
                    {
                        StudentId = student.Id,
                        ReminderType = "FeeUpcoming",
                        Message = $"Fee '{fee.Name}' of ₹{fee.Amount} is due on {fee.DueDate:dd-MMM-yyyy}",
                        IsSent = false
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Fee reminders generated at {Time}", DateTime.UtcNow);
    }
}
