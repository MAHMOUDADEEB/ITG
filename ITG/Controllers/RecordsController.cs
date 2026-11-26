using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Controllers;

// Records: passed exams, verified certificates and the skills score
public class RecordsController : EmployeeBaseController
{
    private readonly IRecordsService _recordsService;

    public RecordsController(IRecordsService recordsService)
    {
        _recordsService = recordsService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _recordsService.GetAsync(CurrentUserId);
        return View(model);
    }
}
