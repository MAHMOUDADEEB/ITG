namespace ITG.Services.Ai;

// Settings read from appsettings.json / user secrets, section "Gemini"
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Keep the key out of appsettings.json: use "Manage User Secrets" in Visual Studio
    public string ApiKey { get; set; } = string.Empty;

    // Free-tier friendly model. Can be changed without touching the code.
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    public int TimeoutSeconds { get; set; } = 60;
}
