using ITG.Data;
using ITG.Models.Entities;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

public class DepartmentsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentListItem(d.Id, d.Name, d.IsActive, d.Employees.Count))
            .ToListAsync();

        return View(departments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DepartmentForm form)
    {
        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var name = form.Name.Trim();
        if (await _context.Departments.AnyAsync(d => d.Name == name && d.Id != form.Id))
        {
            ShowError("A department with this name already exists.");
            return RedirectToAction(nameof(Index));
        }

        var department = form.Id == 0
            ? _context.Departments.Add(new Department()).Entity
            : await _context.Departments.FindAsync(form.Id);

        if (department == null)
        {
            return NotFound();
        }

        department.Name = name;
        department.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        ShowSuccess(form.Id == 0 ? "The department was added." : "The department was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        if (await _context.Employees.AnyAsync(e => e.DepartmentId == id))
        {
            ShowError("This department has employees. Deactivate it instead of deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        ShowSuccess("The department was deleted.");
        return RedirectToAction(nameof(Index));
    }
}
