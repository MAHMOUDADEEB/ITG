namespace ITG.ViewModels.Ai;

// Sent by the chat page as JSON: the whole visible conversation
public class ChatRequest
{
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatMessageDto
{
    // "user" or "assistant"
    public string Role { get; set; } = "user";
    public string Text { get; set; } = string.Empty;
}
