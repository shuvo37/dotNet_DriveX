using System.Text;
using DriveX.Exceptions;
using DriveX.Options;

namespace DriveX.Services;

// One place that sends a request to Gemini and survives its bad moments:
//   1. try the main model; if Google says "overloaded" (503) or the network hiccups, wait 2 seconds and try once more
//   2. if it still fails (or the quota is used up, 429, or the model is gone, 404), try the fallback model once
//   3. if everything fails, throw a friendly message for the user
public static class GeminiCaller
{
    private const string BusyMessage = "I'm getting a lot of questions right now, but I can still help. Please send your question again in a moment.";
    private const string UnavailableMessage = "I couldn't get an answer just now, but I can help with cars, prices and booking. Please try again in a moment.";

    // Returns the response JSON text of the first successful call.
    public static async Task<string> GenerateAsync(HttpClient http, GeminiSettings settings, string bodyJson, ILogger logger)
    {
        var models = new List<string> { settings.Model };
        if (!string.IsNullOrWhiteSpace(settings.FallbackModel)
            && !string.Equals(settings.FallbackModel, settings.Model, StringComparison.OrdinalIgnoreCase))
        {
            models.Add(settings.FallbackModel);
        }

        var busy = false;

        for (var m = 0; m < models.Count; m++)
        {
            var attempts = m == 0 ? 2 : 1;   // the main model gets one retry, the fallback model one try

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                var (status, body) = await SendOnceAsync(http, settings.ApiKey, models[m], bodyJson, logger);

                if (status >= 200 && status < 300)
                {
                    if (m > 0)
                    {
                        logger.LogWarning("Answered by the fallback model {Model}", models[m]);
                    }

                    return body;
                }

                if (status is 429 or 503)
                {
                    busy = true;
                }

                // 0 = network error or timeout. These and 5xx are usually short-lived, so retry once.
                var temporary = status is 0 or 500 or 502 or 503 or 504;
                if (temporary && attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    continue;
                }

                break;
            }

            logger.LogWarning("Model {Model} failed", models[m]);
        }

        throw new AppException(busy ? BusyMessage : UnavailableMessage);
    }

    // One HTTP call. Returns (HTTP status, response text); status 0 means the call itself failed.
    private static async Task<(int Status, string Body)> SendOnceAsync(
        HttpClient http, string apiKey, string model, string bodyJson, ILogger logger)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        try
        {
            using var response = await http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Gemini ({Model}) returned {Status}: {Body}", model, (int)response.StatusCode, text);
            }

            return ((int)response.StatusCode, text);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not reach Gemini ({Model})", model);
            return (0, string.Empty);
        }
    }
}
