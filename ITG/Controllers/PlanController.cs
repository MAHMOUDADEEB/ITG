using System.Globalization;
using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

// "My Plan": personal 4-week development plan written by the AI
public class PlanController : EmployeeBaseController
{
    private readonly IDevelopmentPlanService _planService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PlanController(IDevelopmentPlanService planService, IStringLocalizer<SharedResource> localizer)
    {
        _planService = planService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index() =>
        View(await _planService.GetCurrentAsync(CurrentUserId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate()
    {
        var result = await _planService.GenerateAsync(CurrentUserId, CultureInfo.CurrentUICulture.Name);

        if (result.Succeeded)
        {
            ShowSuccess(_localizer["Your development plan is ready."]);
        }
        else
        {
            ShowError(_localizer[result.ErrorKey ?? "Something went wrong. Please try again."]);
        }

        return RedirectToAction(nameof(Index));
    }

    // Called by the page (fetch) when a task is ticked or unticked
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTask(int planId, int week, int task, bool done)
    {
        var result = await _planService.SetTaskDoneAsync(CurrentUserId, planId, week, task, done);
        return result == null
            ? NotFound()
            : Json(new { done = result.DoneTasks, total = result.TotalTasks, percent = result.Percent });
    }
}
