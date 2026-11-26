using System.Security.Claims;
using ITG.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Areas.Admin.Controllers;

// Every admin controller inherits from this, so the
// area and role protection are applied in one place
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminBaseController : Controller
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Translations for messages created in controllers
    protected IStringLocalizer<SharedResource> Localizer =>
        HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    // Messages shown at the top of the next page (after a redirect)
    protected void ShowSuccess(string key) => TempData["StatusSuccess"] = Localizer[key].Value;
    protected void ShowError(string key) => TempData["StatusError"] = Localizer[key].Value;

    // First validation error of a posted form (already translated by DataAnnotations)
    protected void ShowFirstModelError()
    {
        var message = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        TempData["StatusError"] = message ?? Localizer["Please check the form and try again."].Value;
    }

    // Go back to the page the request came from (only inside this site)
    protected IActionResult RedirectToLocal(string? returnUrl, string fallbackAction, string? fallbackController = null) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction, fallbackController);
}
