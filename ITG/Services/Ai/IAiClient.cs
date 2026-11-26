namespace ITG.Services.Ai;

public interface IAiClient
{
    bool IsConfigured { get; }

    // Sends a conversation to the AI and returns its text answer.
    // When json = true, the AI is asked to answer with JSON only.
    Task<string> GenerateAsync(string systemInstruction, IReadOnlyList<AiMessage> messages,
        bool json = false, CancellationToken cancellationToken = default);
}
