using System.Security.Claims;
using ITG.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Controllers;

// Every employee page inherits this, so role protection is applied in one place
[Authorize(Roles = AppRoles.Employee)]
public abstract class EmployeeBaseController : Controller
{
    // Id of the signed-in employee
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Messages shown at the top of the next page (after a redirect)
    protected void ShowSuccess(string message) => TempData["StatusSuccess"] = message;
    protected void ShowError(string message) => TempData["StatusError"] = message;

    // Reads the chosen option of each question (fields q0, q1, q2 ...).
    // A question without an answer gives null, so every answer keeps its position.
    protected List<int?> ReadAnswers(int maxQuestions = 50) =>
        Enumerable.Range(0, maxQuestions)
            .Select(i => int.TryParse(Request.Form[$"q{i}"].ToString(), out var option) ? option : (int?)null)
            .ToList();
}
