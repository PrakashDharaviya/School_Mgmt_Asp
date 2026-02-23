using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEduERP.Data;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Areas.Admin.Models;

namespace SchoolEduERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminAccess")]
public class SubjectController : Controller
{
    private readonly ApplicationDbContext _context;

    public SubjectController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var subjects = await _context.Subjects.Include(s => s.Teacher).OrderBy(s => s.Standard).ThenBy(s => s.Name).ToListAsync();
        return View(subjects);
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

        _context.Subjects.Update(model);
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
