using System.Security.Claims;
using ITG.Constants;
using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ITG.Controllers;

// Serves uploaded certificate files after checking who is asking
[Authorize]
public class CertificateFilesController : Controller
{
    private readonly ICertificateService _certificateService;

    public CertificateFilesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var file = await _certificateService.GetFileAsync(id, userId, User.IsInRole(AppRoles.Admin));

        if (file == null)
        {
            return NotFound();
        }

        // "inline" opens the PDF/image in the browser instead of downloading it
        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(file.FileName);
        Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();

        return File(file.Content, file.ContentType);
    }
}
