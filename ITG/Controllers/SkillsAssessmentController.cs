using System.Globalization;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

// AI soft-skills assessment: taken once, re-opened only by an admin
public class SkillsAssessmentController : EmployeeBaseController
{
    private readonly ISkillAssessmentService _assessmentService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SkillsAssessmentController(ISkillAssessmentService assessmentService, IStringLocalizer<SharedResource> localizer)
    {
        _assessmentService = assessmentService;
        _localizer = localizer;
    }

    // Introduction page
    public async Task<IActionResult> Index()
    {
        if (!await _assessmentService.CanTakeAsync(CurrentUserId))
        {
            ShowError(_localizer["You have already completed the skills assessment."]);
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // The questions (written by the AI the first time this page opens)
    public async Task<IActionResult> Questions()
    {
        if (!await _assessmentService.CanTakeAsync(CurrentUserId))
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var questions = await _assessmentService.StartAsync(CurrentUserId, CultureInfo.CurrentUICulture.Name);
            return View(questions);
        }
        catch (AiUnavailableException ex)
        {
            ShowError(_localizer[ex.MessageKey]);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit()
    {
        var result = await _assessmentService.SubmitAsync(
            CurrentUserId, ReadAnswers(), CultureInfo.CurrentUICulture.Name);

        if (!result.Succeeded)
        {
            ShowError(_localizer[result.ErrorKey!]);
            return RedirectToAction(nameof(Questions));
        }

        ShowSuccess(_localizer["Your skills assessment is complete. Here are your results."]);
        return RedirectToAction("Index", "Home");
    }
}
