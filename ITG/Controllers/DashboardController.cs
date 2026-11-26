using ITG.Services.Interfaces;
using ITG.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Controllers;

public class DashboardController : EmployeeBaseController
{
    private readonly ICareerService _careerService;
    private readonly IExamService _examService;

    public DashboardController(ICareerService careerService, IExamService examService)
    {
        _careerService = careerService;
        _examService = examService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            Progress = await _careerService.GetProgressAsync(CurrentUserId),
            LastExam = await _examService.GetLastCompletedAsync(CurrentUserId)
        };

        return View(model);
    }
}
