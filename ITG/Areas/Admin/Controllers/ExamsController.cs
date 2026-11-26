using ITG.Data;
using ITG.Models.Entities;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

// Manage the AI self-assessment exams
public class ExamsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public ExamsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var exams = await _context.Exams
            .AsNoTracking()
            .OrderBy(e => e.Category).ThenBy(e => e.Title)
            .Select(e => new ExamListItem(
                e.Id, e.Title, e.Category, e.Description, e.DurationMinutes, e.QuestionCount,
                e.ExamPositions.Select(ep => ep.PositionId).ToList(),
                e.ExamPositions.OrderBy(ep => ep.Position.Level).Select(ep => ep.Position.Title).ToList(),
                e.IsActive,
                e.Attempts.Count))
            .ToListAsync();

        ViewBag.Categories = await _context.GetCategoryOptionsAsync();
        ViewBag.Positions = await _context.Positions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CareerPath.Name).ThenBy(p => p.Level)
            .Select(p => new PositionLookupItem(p.Id, p.Title, p.CareerPath.Name))
            .ToListAsync();

        return View(exams);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ExamForm form)
    {
        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var title = form.Title.Trim();
        if (await _context.Exams.AnyAsync(e => e.Title == title && e.Id != form.Id))
        {
            ShowError("An exam with this title already exists.");
            return RedirectToAction(nameof(Index));
        }

        // Keep only real positions; an exam with no position is never shown to anyone
        var requested = form.PositionIds.Distinct().ToList();
        var positionIds = await _context.Positions
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        if (positionIds.Count == 0)
        {
            ShowError("Choose at least one position.");
            return RedirectToAction(nameof(Index));
        }

        var exam = form.Id == 0
            ? _context.Exams.Add(new Exam()).Entity
            : await _context.Exams.Include(e => e.ExamPositions).FirstOrDefaultAsync(e => e.Id == form.Id);

        if (exam == null)
        {
            return NotFound();
        }

        exam.Title = title;
        exam.Category = form.Category.Trim();
        exam.Description = form.Description?.Trim();
        exam.DurationMinutes = form.DurationMinutes;
        exam.QuestionCount = form.QuestionCount;
        exam.IsActive = form.IsActive;

        // Sync the linked positions: remove the unticked ones, add the new ones
        foreach (var link in exam.ExamPositions.Where(ep => !positionIds.Contains(ep.PositionId)).ToList())
        {
            exam.ExamPositions.Remove(link);
        }
        foreach (var positionId in positionIds.Where(id => exam.ExamPositions.All(ep => ep.PositionId != id)))
        {
            exam.ExamPositions.Add(new ExamPosition { PositionId = positionId });
        }

        await _context.SaveChangesAsync();
        ShowSuccess(form.Id == 0 ? "The exam was added." : "The exam was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null)
        {
            return NotFound();
        }

        if (await _context.ExamAttempts.AnyAsync(a => a.ExamId == id))
        {
            ShowError("Employees already took this exam. Deactivate it instead of deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();

        ShowSuccess("The exam was deleted.");
        return RedirectToAction(nameof(Index));
    }
}
