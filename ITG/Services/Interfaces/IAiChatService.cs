using ITG.Services.Ai;

namespace ITG.Services.Interfaces;

public interface IAiChatService
{
    // Sends the conversation (latest message last) and returns the assistant's answer
    Task<string> ReplyAsync(string employeeId, IReadOnlyList<AiMessage> conversation, string cultureName);
}
