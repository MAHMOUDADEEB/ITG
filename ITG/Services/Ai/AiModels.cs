namespace ITG.Services.Ai;

// One message in a conversation sent to the AI
public record AiMessage(bool FromUser, string Text);

// Why an AI request failed (each value has a friendly message for the user)
public enum AiFailure
{
    NotConfigured,
    InvalidKey,
    QuotaExceeded,
    ModelNotFound,
    BadResponse,
    Unavailable
}

public class AiUnavailableException : Exception
{
    public AiFailure Failure { get; }

    public AiUnavailableException(AiFailure failure, string? details = null, Exception? inner = null)
        : base(details ?? failure.ToString(), inner)
    {
        Failure = failure;
    }

    // Translation key shown to the user
    public string MessageKey => Failure switch
    {
        AiFailure.NotConfigured => "The AI service is not configured yet. Please ask the administrator to add the API key.",
        AiFailure.InvalidKey => "The AI API key is not valid. Please ask the administrator to check it.",
        AiFailure.QuotaExceeded => "The AI service reached its free usage limit. Please try again later.",
        AiFailure.ModelNotFound => "The AI model name in the settings is not available. Please ask the administrator to check it.",
        _ => "The AI service is not available right now. Please try again in a moment."
    };
}
