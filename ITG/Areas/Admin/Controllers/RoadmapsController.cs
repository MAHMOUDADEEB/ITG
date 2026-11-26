using ITG.Data;
using ITG.Models.Entities;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

// Decide which certificates (and in which order) are required to reach each position
public class RoadmapsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public RoadmapsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? positionId)
    {
        var positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.CareerPath.Name).ThenBy(p => p.Level)
            .Select(p => new LookupItem(p.Id, p.Title + " · " + p.CareerPath.Name))
            .ToListAsync();

        // Show the first position that needs a roadmap if none is chosen
        positionId ??= await _context.Positions
            .AsNoTracking()
            .Where(p => p.Level > 1 || p.CareerPath.ParentCareerPathId != null)
            .OrderBy(p => p.CareerPath.Name).ThenBy(p => p.Level)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        var items = new List<RoadmapRow>();
        var available = new List<LookupItem>();

        if (positionId.HasValue)
        {
            items = await _context.PositionCertificates
                .AsNoTracking()
                .Where(pc => pc.PositionId == positionId)
                .OrderBy(pc => pc.SortOrder)
                .Select(pc => new RoadmapRow(pc.CertificateId, pc.Certificate.Title, pc.Certificate.Category,
                    pc.Certificate.IsActive, pc.SortOrder))
                .ToListAsync();

            var usedIds = items.Select(i => i.CertificateId).ToList();
            available = await _context.Certificates
                .AsNoTracking()
                .Where(c => c.IsActive && !usedIds.Contains(c.Id))
                .OrderBy(c => c.Title)
                .Select(c => new LookupItem(c.Id, c.Title))
                .ToListAsync();
        }

        return View(new RoadmapManageViewModel(positionId, positions, items, available));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(RoadmapItemForm form)
    {
        var exists = await _context.PositionCertificates
            .AnyAsync(pc => pc.PositionId == form.PositionId && pc.CertificateId == form.CertificateId);
        var valid = await _context.Positions.AnyAsync(p => p.Id == form.PositionId)
                    && await _context.Certificates.AnyAsync(c => c.Id == form.CertificateId);

        if (exists || !valid)
        {
            ShowError("Please choose a certificate that is not already in this roadmap.");
            return RedirectToAction(nameof(Index), new { positionId = form.PositionId });
        }

        var nextOrder = await _context.PositionCertificates
            .Where(pc => pc.PositionId == form.PositionId)
            .Select(pc => (int?)pc.SortOrder)
            .MaxAsync() ?? 0;

        _context.PositionCertificates.Add(new PositionCertificate
        {
            PositionId = form.PositionId,
            CertificateId = form.CertificateId,
            SortOrder = nextOrder + 1
        });

        await _context.SaveChangesAsync();
        ShowSuccess("The certificate was added to the roadmap.");
        return RedirectToAction(nameof(Index), new { positionId = form.PositionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(RoadmapItemForm form)
    {
        var item = await _context.PositionCertificates
            .FirstOrDefaultAsync(pc => pc.PositionId == form.PositionId && pc.CertificateId == form.CertificateId);

        if (item != null)
        {
            _context.PositionCertificates.Remove(item);
            await _context.SaveChangesAsync();
            await RenumberAsync(form.PositionId);
            ShowSuccess("The certificate was removed from the roadmap.");
        }

        return RedirectToAction(nameof(Index), new { positionId = form.PositionId });
    }

    // Moves a certificate one step up (-1) or down (+1)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(RoadmapItemForm form, int direction)
    {
        var items = await _context.PositionCertificates
            .Where(pc => pc.PositionId == form.PositionId)
            .OrderBy(pc => pc.SortOrder)
            .ToListAsync();

        var index = items.FindIndex(pc => pc.CertificateId == form.CertificateId);
        var swapWith = index + Math.Sign(direction);

        if (index >= 0 && swapWith >= 0 && swapWith < items.Count)
        {
            (items[index], items[swapWith]) = (items[swapWith], items[index]);
            for (var i = 0; i < items.Count; i++)
            {
                items[i].SortOrder = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { positionId = form.PositionId });
    }

    private async Task RenumberAsync(int positionId)
    {
        var items = await _context.PositionCertificates
            .Where(pc => pc.PositionId == positionId)
            .OrderBy(pc => pc.SortOrder)
            .ToListAsync();

        for (var i = 0; i < items.Count; i++)
        {
            items[i].SortOrder = i + 1;
        }

        await _context.SaveChangesAsync();
    }
}
