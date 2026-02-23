using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Areas.Admin.Models;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Services;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class FeeStructureController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicYearService _academicYearService;

    public FeeStructureController(ApplicationDbContext context, IAcademicYearService academicYearService)
    {
        _context = context;
        _academicYearService = academicYearService;
    }

    public async Task<IActionResult> Index()
    {
        var activeYear = await _academicYearService.GetActiveYearAsync();
        var feeHeads = await _context.FeeHeads
            .Include(f => f.AcademicYear)
            .Where(f => (activeYear == null || f.AcademicYearId == activeYear.Id) && f.IsActive)
            .OrderBy(f => f.ApplicableClass)
            .ThenBy(f => f.Name)
            .ToListAsync();

        var model = new FeeStructureListViewModel
        {
            ActiveAcademicYearId = activeYear?.Id ?? 0,
            ActiveAcademicYearName = activeYear?.Name ?? "N/A",
            TotalFeeAmount = feeHeads.Sum(f => f.Amount),
            FeeHeads = feeHeads.Select(f => new FeeStructureViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Amount = f.Amount,
                ApplicableClass = f.ApplicableClass,
                AcademicYearId = f.AcademicYearId,
                AcademicYearName = f.AcademicYear.Name,
                DueDate = f.DueDate,
                IsActive = f.IsActive
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new FeeStructureViewModel
        {
            AcademicYears = await _context.AcademicYears
                .Select(a => new AcademicYearDropdown { Id = a.Id, Name = a.Name }).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeStructureViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AcademicYears = await _context.AcademicYears
                .Select(a => new AcademicYearDropdown { Id = a.Id, Name = a.Name }).ToListAsync();
            return View(model);
        }

        var feeHead = new FeeHead
        {
            Name = model.Name,
            Amount = model.Amount,
            ApplicableClass = model.ApplicableClass,
            AcademicYearId = model.AcademicYearId,
            DueDate = model.DueDate,
            IsActive = true
        };

        _context.FeeHeads.Add(feeHead);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Fee structure created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var fee = await _context.FeeHeads.FindAsync(id);
        if (fee == null) return NotFound();

        var model = new FeeStructureViewModel
        {
            Id = fee.Id,
            Name = fee.Name,
            Amount = fee.Amount,
            ApplicableClass = fee.ApplicableClass,
            AcademicYearId = fee.AcademicYearId,
            DueDate = fee.DueDate,
            IsActive = fee.IsActive,
            AcademicYears = await _context.AcademicYears
                .Select(a => new AcademicYearDropdown { Id = a.Id, Name = a.Name }).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FeeStructureViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AcademicYears = await _context.AcademicYears
                .Select(a => new AcademicYearDropdown { Id = a.Id, Name = a.Name }).ToListAsync();
            return View(model);
        }

        var fee = await _context.FeeHeads.FindAsync(model.Id);
        if (fee == null) return NotFound();

        fee.Name = model.Name;
        fee.Amount = model.Amount;
        fee.ApplicableClass = model.ApplicableClass;
        fee.AcademicYearId = model.AcademicYearId;
        fee.DueDate = model.DueDate;
        fee.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Fee structure updated!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var fee = await _context.FeeHeads.FindAsync(id);
        if (fee == null)
        {
            TempData["Error"] = "Fee head not found.";
            return RedirectToAction(nameof(Index));
        }

        // Check if any payments reference this fee head
        var hasPayments = await _context.FeePayments.AnyAsync(p => p.FeeHeadId == id);
        if (hasPayments)
        {
            // Soft-delete to preserve payment history
            fee.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{fee.Name}' has been removed from the fee structure (payment records preserved).";
        }
        else
        {
            _context.FeeHeads.Remove(fee);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{fee.Name}' deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
