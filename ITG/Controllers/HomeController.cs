using System.Diagnostics;
using System.Security.Claims;
using ITG.Constants;
using ITG.Models;
using ITG.Services.Interfaces;
using ITG.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ICareerService _careerService;
    private readonly ISkillAssessmentService _assessmentService;
    private readonly IReadinessService _readinessService;
    private readonly IDevelopmentPlanService _planService;

    public HomeController(ICareerService careerService, ISkillAssessmentService assessmentService,
        IReadinessService readinessService, IDevelopmentPlanService planService)
    {
        _careerService = careerService;
        _assessmentService = assessmentService;
        _readinessService = readinessService;
        _planService = planService;
    }

    public async Task<IActionResult> Index()
    {
        // Admins have their own dashboard
        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var model = new HomeViewModel
        {
            Progress = await _careerService.GetProgressAsync(userId),
            LatestAssessment = await _assessmentService.GetLatestAsync(userId),
            PreviousAssessment = await _assessmentService.GetPreviousAsync(userId),
            CanTakeAssessment = await _assessmentService.CanTakeAsync(userId),
            Readiness = await _readinessService.GetAsync(userId),
            Plan = await _planService.GetCurrentAsync(userId)
        };

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
