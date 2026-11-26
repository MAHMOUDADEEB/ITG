using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ITG.Services.Ai;

// Calls the Google Gemini REST API (generateContent) with HttpClient.
// No extra NuGet package is needed.
public class GeminiClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> GenerateAsync(string systemInstruction, IReadOnlyList<AiMessage> messages,
        bool json = false, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new AiUnavailableException(AiFailure.NotConfigured);
        }

        object generationConfig = json
            ? new { responseMimeType = "application/json", temperature = 0.7 }
            : new { temperature = 0.7 };

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = messages.Select(m => new
            {
                role = m.FromUser ? "user" : "model",
                parts = new[] { new { text = m.Text } }
            }),
            generationConfig
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent";

        // Google sometimes answers "busy" (500/503) for a moment, so we try up to 3 times
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("x-goog-api-key", _options.ApiKey);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Gemini request failed (attempt {Attempt}), trying again", attempt);
                await Task.Delay(RetryDelay(attempt), cancellationToken);
                continue;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Gemini request failed: {Message}", ex.Message);
                throw new AiUnavailableException(AiFailure.Unavailable, inner: ex);
            }

            using (response)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (IsTemporary(response.StatusCode) && attempt < MaxAttempts)
                {
                    _logger.LogWarning("Gemini is busy ({Status}, attempt {Attempt}), trying again", (int)response.StatusCode, attempt);
                    await Task.Delay(RetryDelay(attempt), cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini returned {Status}: {Body}", (int)response.StatusCode, content);
                    throw new AiUnavailableException(MapFailure(response.StatusCode, content), content);
                }

                var text = ExtractText(content);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Gemini returned no text: {Body}", content);
                    throw new AiUnavailableException(AiFailure.BadResponse, content);
                }

                return text;
            }
        }
    }

    private const int MaxAttempts = 3;

    private static TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(attempt * 2);

    private static bool IsTemporary(HttpStatusCode status) =>
        status is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static AiFailure MapFailure(HttpStatusCode status, string content) => status switch
    {
        HttpStatusCode.TooManyRequests => AiFailure.QuotaExceeded,
        HttpStatusCode.NotFound => AiFailure.ModelNotFound,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AiFailure.InvalidKey,
        HttpStatusCode.BadRequest when content.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) => AiFailure.InvalidKey,
        _ => AiFailure.Unavailable
    };

    // Joins the text parts of the first candidate (thinking parts are skipped)
    private static string ExtractText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0 ||
            !candidates[0].TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            var isThought = part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True;
            if (!isThought && part.TryGetProperty("text", out var text))
            {
                builder.Append(text.GetString());
            }
        }

        return builder.ToString().Trim();
    }
}
