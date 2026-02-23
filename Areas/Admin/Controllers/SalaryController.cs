using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class SalaryController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalaryController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var salaries = await _context.Salaries
            .Include(s => s.Teacher)
            .OrderByDescending(s => s.PaymentDate)
            .Select(s => new SalaryViewModel
            {
                Id = s.Id,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher.FirstName + " " + s.Teacher.LastName,
                EmployeeId = s.Teacher.EmployeeId,
                BasicSalary = s.BasicSalary,
                Allowances = s.Allowances,
                Deductions = s.Deductions,
                PaymentDate = s.PaymentDate,
                Month = s.Month,
                Status = s.Status
            })
            .ToListAsync();

        return View(salaries);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SalaryViewModel
        {
            Teachers = await _context.Teachers.Where(t => t.IsActive)
                .Select(t => new TeacherDropdown { Id = t.Id, Name = t.FirstName + " " + t.LastName }).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalaryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Teachers = await _context.Teachers.Where(t => t.IsActive)
                .Select(t => new TeacherDropdown { Id = t.Id, Name = t.FirstName + " " + t.LastName }).ToListAsync();
            return View(model);
        }

        var salary = new Salary
        {
            TeacherId = model.TeacherId,
            BasicSalary = model.BasicSalary,
            Allowances = model.Allowances,
            Deductions = model.Deductions,
            NetSalary = model.BasicSalary + model.Allowances - model.Deductions,
            PaymentDate = model.PaymentDate,
            Month = model.Month,
            Status = model.Status
        };

        _context.Salaries.Add(salary);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Salary record created!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var salary = await _context.Salaries.FindAsync(id);
        if (salary == null) return NotFound();

        var model = new SalaryViewModel
        {
            Id = salary.Id,
            TeacherId = salary.TeacherId,
            BasicSalary = salary.BasicSalary,
            Allowances = salary.Allowances,
            Deductions = salary.Deductions,
            PaymentDate = salary.PaymentDate,
            Month = salary.Month,
            Status = salary.Status,
            Teachers = await _context.Teachers.Where(t => t.IsActive)
                .Select(t => new TeacherDropdown { Id = t.Id, Name = t.FirstName + " " + t.LastName }).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SalaryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Teachers = await _context.Teachers.Where(t => t.IsActive)
                .Select(t => new TeacherDropdown { Id = t.Id, Name = t.FirstName + " " + t.LastName }).ToListAsync();
            return View(model);
        }

        var salary = await _context.Salaries.FindAsync(model.Id);
        if (salary == null) return NotFound();

        salary.TeacherId = model.TeacherId;
        salary.BasicSalary = model.BasicSalary;
        salary.Allowances = model.Allowances;
        salary.Deductions = model.Deductions;
        salary.NetSalary = model.BasicSalary + model.Allowances - model.Deductions;
        salary.PaymentDate = model.PaymentDate;
        salary.Month = model.Month;
        salary.Status = model.Status;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Salary record updated!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var salary = await _context.Salaries.FindAsync(id);
        if (salary != null)
        {
            _context.Salaries.Remove(salary);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Salary record deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var salary = await _context.Salaries.FindAsync(id);
        if (salary != null)
        {
            salary.Status = "Paid";
            salary.PaymentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Salary marked as paid!";
        }
        return RedirectToAction(nameof(Index));
    }
}
