using ITG.Services.Interfaces;
using ITG.ViewModels.Roadmap;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

public class RoadmapController : EmployeeBaseController
{
    private readonly ICareerService _careerService;
    private readonly ICertificateService _certificateService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RoadmapController(
        ICareerService careerService,
        ICertificateService certificateService,
        IStringLocalizer<SharedResource> localizer)
    {
        _careerService = careerService;
        _certificateService = certificateService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var model = new RoadmapViewModel
        {
            Progress = await _careerService.GetProgressAsync(CurrentUserId)
        };

        return View(model);
    }

    // Employee chooses a path at a fork (from Home or Roadmap)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPath(int careerPathId, string? returnUrl)
    {
        var saved = await _careerService.SelectPathAsync(CurrentUserId, careerPathId);

        if (saved)
        {
            ShowSuccess(_localizer["Your career path has been updated."]);
        }
        else
        {
            ShowError(_localizer["This path cannot be selected."]);
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadCertificateViewModel model)
    {
        var result = await _certificateService.UploadAsync(
            CurrentUserId, model.CertificateId, model.File, model.IssuedOn);

        if (result.Succeeded)
        {
            ShowSuccess(_localizer["Your certificate was uploaded and is waiting for HR review."]);
        }
        else
        {
            ShowError(_localizer[result.ErrorKey ?? "Something went wrong. Please try again."]);
        }

        return RedirectToAction(nameof(Index));
    }
}
