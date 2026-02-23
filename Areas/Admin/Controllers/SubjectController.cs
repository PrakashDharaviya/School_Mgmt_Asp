using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Helpers;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Areas.Admin.Models;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class SubjectController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public SubjectController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? standard, string? search, int page = 1)
    {
        var query = _context.Subjects.Include(s => s.Teacher).AsQueryable();

        if (standard.HasValue)
            query = query.Where(s => s.Standard == standard.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) || (s.Code != null && s.Code.Contains(search)));

        var ordered = query.OrderBy(s => s.Standard).ThenBy(s => s.Name);
        var paged   = await PaginatedList<Subject>.CreateAsync(ordered, page, PageSize);

        var allStandards = await _context.Subjects
            .Select(s => s.Standard).Distinct().OrderBy(s => s).ToListAsync();

        ViewBag.Standards       = allStandards;
        ViewBag.SelectedStandard = standard;
        ViewBag.SearchTerm      = search;

        return View(paged);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).ToListAsync();
        return View(new Subject());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subject model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).ToListAsync();
            return View(model);
        }

        _context.Subjects.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Subject added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var subj = await _context.Subjects.FindAsync(id);
        if (subj == null) return NotFound();
        ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).ToListAsync();
        return View(subj);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Subject model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teachers = await _context.Teachers.Where(t => t.IsActive).ToListAsync();
            return View(model);
        }

        var existing = await _context.Subjects.FindAsync(model.Id);
        if (existing == null) return NotFound();

        // Update only changed fields to preserve CreatedAt and other audit fields
        existing.Standard = model.Standard;
        existing.Name = model.Name;
        existing.Code = model.Code;
        existing.TeacherId = model.TeacherId;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subj = await _context.Subjects.FindAsync(id);
        if (subj == null) return NotFound();
        _context.Subjects.Remove(subj);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Subject deleted.";
        return RedirectToAction(nameof(Index));
    }
}
