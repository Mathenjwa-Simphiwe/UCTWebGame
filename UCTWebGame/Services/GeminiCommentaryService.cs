using System.Net.Http.Json;
using System.Text.Json;

namespace UCTWebGame.Services;

// Free alternative to the Claude version: uses the Google Gemini API free tier (no billing needed).
// The game engine decides everything; the LLM only describes it. If anything goes wrong
// (no key, rate limit, timeout, blocked reply), the caller's fallback text is returned.
public class GeminiCommentaryService : ICommentaryService
{
    private const string SystemPrompt =
        "You are an energetic war-game commentator. Write 1-2 punchy sentences (max 30 words) " +
        "about the battle round described. Use only the facts given. Never invent numbers, " +
        "countries or outcomes. No emojis, no hashtags.";

    // The free tier only allows a few requests per minute. Instead of waiting or hitting HTTP 429,
    // rounds that come too soon after the last call simply use the template fallback.
    private static long _lastCallTicks;

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly TimeSpan _minGap;
    private readonly ILogger<GeminiCommentaryService> _log;

    public GeminiCommentaryService(HttpClient http, IConfiguration config, ILogger<GeminiCommentaryService> log)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        _model = config["Gemini:Model"] ?? "gemini-2.5-flash-lite";
        _minGap = TimeSpan.FromSeconds(config.GetValue("Gemini:MinSecondsBetweenCalls", 6));
        _log = log;
    }

    public async Task<string> GenerateAsync(string facts, string fallback, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || !TryTakeSlot())
            return fallback;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = facts } } } },
                generationConfig = new { maxOutputTokens = 200, temperature = 0.9 }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{_model}:generateContent")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            using var response = await _http.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Gemini commentary call failed; using fallback text.");
            return fallback;
        }
    }

    // True if enough time has passed since the last LLM call (thread-safe).
    private bool TryTakeSlot()
    {
        var now = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastCallTicks);
        if (now - last < _minGap.Ticks) return false;
        return Interlocked.CompareExchange(ref _lastCallTicks, now, last) == last;
    }
}
