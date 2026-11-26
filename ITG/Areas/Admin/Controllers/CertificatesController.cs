using ITG.Data;
using ITG.Models.Entities;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

// Manage the list of certificates the company recognises
public class CertificatesController : AdminBaseController
{
    private readonly AppDbContext _context;

    public CertificatesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var certificates = await _context.Certificates
            .AsNoTracking()
            .OrderBy(c => c.Category).ThenBy(c => c.Title)
            .Select(c => new CertificateListItem(
                c.Id, c.Title, c.Category, c.Description, c.IsActive,
                c.PositionCertificates.Count,
                c.Submissions.Count))
            .ToListAsync();

        ViewBag.Categories = await _context.GetCategoryOptionsAsync();
        return View(certificates);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CertificateForm form)
    {
        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var title = form.Title.Trim();
        if (await _context.Certificates.AnyAsync(c => c.Title == title && c.Id != form.Id))
        {
            ShowError("A certificate with this title already exists.");
            return RedirectToAction(nameof(Index));
        }

        var certificate = form.Id == 0
            ? _context.Certificates.Add(new Certificate()).Entity
            : await _context.Certificates.FindAsync(form.Id);

        if (certificate == null)
        {
            return NotFound();
        }

        certificate.Title = title;
        certificate.Category = form.Category.Trim();
        certificate.Description = form.Description?.Trim();
        certificate.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        ShowSuccess(form.Id == 0 ? "The certificate was added." : "The certificate was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var certificate = await _context.Certificates
            .Include(c => c.PositionCertificates)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null)
        {
            return NotFound();
        }

        // Certificates with uploads keep their history: deactivate them instead
        if (await _context.EmployeeCertificates.AnyAsync(s => s.CertificateId == id))
        {
            ShowError("This certificate has employee uploads. Deactivate it instead of deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _context.Certificates.Remove(certificate);
        await _context.SaveChangesAsync();

        ShowSuccess("The certificate was deleted.");
        return RedirectToAction(nameof(Index));
    }
}
