using System.Globalization;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.ViewModels.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

// The "AI" page: a simple text chat with the learning assistant
public class AiAssistantController : EmployeeBaseController
{
    private readonly IAiChatService _chatService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AiAssistantController(IAiChatService chatService, IStringLocalizer<SharedResource> localizer)
    {
        _chatService = chatService;
        _localizer = localizer;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Called by the page with fetch(); returns JSON { reply } or { error }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] ChatRequest request)
    {
        var conversation = request.Messages
            .Select(m => new AiMessage(m.Role != "assistant", m.Text ?? string.Empty))
            .ToList();

        try
        {
            var reply = await _chatService.ReplyAsync(CurrentUserId, conversation, CultureInfo.CurrentUICulture.Name);
            return Json(new { reply });
        }
        catch (AiUnavailableException ex)
        {
            return Json(new { error = _localizer[ex.MessageKey].Value });
        }
    }
}
