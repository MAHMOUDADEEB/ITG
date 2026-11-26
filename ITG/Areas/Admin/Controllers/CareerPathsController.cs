using ITG.Data;
using ITG.Models.Entities;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

// Manage career paths and the positions inside each path
public class CareerPathsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public CareerPathsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var paths = await _context.CareerPaths
            .AsNoTracking()
            .OrderBy(c => c.ParentCareerPathId != null).ThenBy(c => c.Name)
            .Select(c => new CareerPathListItem(
                c.Id, c.Name, c.Description, c.IsActive, c.ParentCareerPathId,
                c.ParentCareerPath != null ? c.ParentCareerPath.Name : null,
                c.Positions
                    .OrderBy(p => p.Level)
                    .Select(p => new PositionListItem(p.Id, p.Title, p.Level, p.Description, p.IsFork, p.IsActive,
                        p.CareerPathId, p.Employees.Count, p.PositionCertificates.Count))
                    .ToList()))
            .ToListAsync();

        return View(paths);
    }

    // ----- Paths -----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePath(CareerPathForm form)
    {
        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var name = form.Name.Trim();
        if (await _context.CareerPaths.AnyAsync(c => c.Name == name && c.Id != form.Id))
        {
            ShowError("A career path with this name already exists.");
            return RedirectToAction(nameof(Index));
        }

        if (form.ParentCareerPathId == form.Id && form.Id != 0)
        {
            ShowError("A career path cannot be its own parent.");
            return RedirectToAction(nameof(Index));
        }

        var path = form.Id == 0
            ? _context.CareerPaths.Add(new CareerPath()).Entity
            : await _context.CareerPaths.FindAsync(form.Id);

        if (path == null)
        {
            return NotFound();
        }

        path.Name = name;
        path.Description = form.Description?.Trim();
        path.ParentCareerPathId = form.ParentCareerPathId;
        path.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        ShowSuccess(form.Id == 0 ? "The career path was added." : "The career path was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePath(int id)
    {
        var path = await _context.CareerPaths.FindAsync(id);
        if (path == null)
        {
            return NotFound();
        }

        var inUse = await _context.Positions.AnyAsync(p => p.CareerPathId == id)
                    || await _context.CareerPaths.AnyAsync(c => c.ParentCareerPathId == id)
                    || await _context.Employees.AnyAsync(e => e.SelectedCareerPathId == id)
                    || await _context.EmployeeCareerPathOptions.AnyAsync(o => o.CareerPathId == id);
        if (inUse)
        {
            ShowError("This path still has positions or branches. Deactivate it instead of deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _context.CareerPaths.Remove(path);
        await _context.SaveChangesAsync();

        ShowSuccess("The career path was deleted.");
        return RedirectToAction(nameof(Index));
    }

    // ----- Positions -----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePosition(PositionForm form)
    {
        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var levelTaken = await _context.Positions.AnyAsync(p =>
            p.CareerPathId == form.CareerPathId && p.Level == form.Level && p.Id != form.Id);
        if (levelTaken)
        {
            ShowError("This level is already used in the selected path.");
            return RedirectToAction(nameof(Index));
        }

        var position = form.Id == 0
            ? _context.Positions.Add(new Position()).Entity
            : await _context.Positions.FindAsync(form.Id);

        if (position == null)
        {
            return NotFound();
        }

        position.Title = form.Title.Trim();
        position.Level = form.Level;
        position.CareerPathId = form.CareerPathId;
        position.Description = form.Description?.Trim();
        position.IsFork = form.IsFork;
        position.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        ShowSuccess(form.Id == 0 ? "The position was added." : "The position was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position == null)
        {
            return NotFound();
        }

        var inUse = await _context.Employees.AnyAsync(e => e.CurrentPositionId == id)
                    || await _context.Promotions.AnyAsync(p => p.FromPositionId == id || p.ToPositionId == id);
        if (inUse)
        {
            ShowError("Employees are linked to this position. Deactivate it instead of deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();

        ShowSuccess("The position was deleted.");
        return RedirectToAction(nameof(Index));
    }
}
