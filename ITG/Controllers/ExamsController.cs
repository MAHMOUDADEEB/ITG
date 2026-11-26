using System.Globalization;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.ViewModels.Exams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

// AI self-assessment exams (practice only, they do not affect promotion)
public class ExamsController : EmployeeBaseController
{
    private readonly IExamService _examService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ExamsController(IExamService examService, IStringLocalizer<SharedResource> localizer)
    {
        _examService = examService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? filter)
    {
        var model = new ExamsIndexViewModel
        {
            Overview = await _examService.GetOverviewAsync(CurrentUserId),
            Filter = filter is "available" or "completed" ? filter : "all"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            var attemptId = await _examService.StartAttemptAsync(CurrentUserId, id, CultureInfo.CurrentUICulture.Name);
            return RedirectToAction(nameof(Take), new { id = attemptId });
        }
        catch (AiUnavailableException ex)
        {
            ShowError(_localizer[ex.MessageKey]);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Take(int id)
    {
        var model = await _examService.GetAttemptToTakeAsync(id, CurrentUserId);

        // Finished or time is up: show the result instead
        return model == null
            ? RedirectToAction(nameof(Result), new { id })
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        await _examService.SubmitAsync(id, CurrentUserId, ReadAnswers());
        return RedirectToAction(nameof(Result), new { id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var model = await _examService.GetResultAsync(id, CurrentUserId);
        return model == null ? NotFound() : View(model);
    }
}
