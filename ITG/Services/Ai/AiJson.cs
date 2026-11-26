using System.Text.Json;

namespace ITG.Services.Ai;

// Reads JSON written by the AI, even if it wrapped it in ```json ... ``` fences
public static class AiJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Parse<T>(string aiText)
    {
        var start = aiText.IndexOf('{');
        var end = aiText.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new AiUnavailableException(AiFailure.BadResponse, aiText);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(aiText[start..(end + 1)], Options)
                   ?? throw new AiUnavailableException(AiFailure.BadResponse, aiText);
        }
        catch (JsonException ex)
        {
            throw new AiUnavailableException(AiFailure.BadResponse, aiText, ex);
        }
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);

    // "Arabic" or "English", used inside prompts
    public static string LanguageName(string cultureName) =>
        cultureName.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "Arabic" : "English";
}
