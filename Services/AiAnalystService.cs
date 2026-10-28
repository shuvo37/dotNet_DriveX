using System.Text;
using System.Text.Json.Nodes;
using DriveX.DTOs.Ai;
using DriveX.Exceptions;
using DriveX.Options;
using Microsoft.Extensions.Options;

namespace DriveX.Services;

// The AI analyst. It talks to Gemini in a loop:
//   1. send the admin's question + the list of tools
//   2. if Gemini asks for tools, run them on the live database and send the results back
//   3. repeat until Gemini writes its final analysis (at most MaxRounds times)
public class AiAnalystService : IAiAnalystService
{
    private const int MaxRounds = 6;

    private readonly HttpClient _http;
    private readonly AnalystTools _tools;
    private readonly GeminiSettings _settings;
    private readonly ILogger<AiAnalystService> _logger;

    public AiAnalystService(
        HttpClient http,
        AnalystTools tools,
        IOptions<GeminiSettings> options,
        ILogger<AiAnalystService> logger)
    {
        _http = http;
        _tools = tools;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<AnalystResponse> AskAsync(string question, IEnumerable<ChatMessage>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AppException("The AI assistant is not set up yet");
        }

        // The conversation so far: up to the last 6 earlier messages (3 questions + 3 answers) as plain text,
        // then the new question. Later, (tool request, tool result) pairs are added to it.
        var contents = new JsonArray();

        foreach (var (role, text) in ChatHistory.Clean(history, maxMessages: 6, maxChars: 4000))
        {
            contents.Add(new JsonObject
            {
                ["role"] = role,
                ["parts"] = new JsonArray(new JsonObject { ["text"] = text })
            });
        }

        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray(new JsonObject { ["text"] = question.Trim() })
        });

        var toolsUsed = new List<string>();

        for (var round = 0; round < MaxRounds; round++)
        {
            var reply = await CallGeminiAsync(contents);

            var content = reply["candidates"]?[0]?["content"];
            var parts = content?["parts"] as JsonArray;

            if (content is null || parts is null || parts.Count == 0)
            {
                throw new AppException("The analyst could not answer that. Try asking in a different way.");
            }

            var calls = parts.Where(p => p?["functionCall"] is not null).ToList();

            // No tool requested: this is the final analysis.
            if (calls.Count == 0)
            {
                var answer = JoinText(parts);
                if (string.IsNullOrWhiteSpace(answer))
                {
                    throw new AppException("The analyst could not answer that. Try asking in a different way.");
                }

                return new AnalystResponse(answer.Trim(), toolsUsed.Distinct().ToList());
            }

            // Keep Gemini's turn exactly as received (newer models need their hidden "thought signature" back).
            contents.Add(content.DeepClone());

            var results = new JsonArray();
            foreach (var part in calls)
            {
                var call = part!["functionCall"]!;
                var name = call["name"]?.ToString() ?? string.Empty;
                var args = call["args"] as JsonObject;

                toolsUsed.Add(name);

                JsonNode result;
                try
                {
                    result = await _tools.RunAsync(name, args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Analyst tool {Tool} failed", name);
                    result = new JsonObject { ["error"] = "This tool failed. Continue without it and say so." };
                }

                var functionResponse = new JsonObject
                {
                    ["name"] = name,
                    ["response"] = new JsonObject { ["result"] = result }
                };

                if (call["id"] is not null)
                {
                    functionResponse["id"] = call["id"]!.DeepClone();
                }

                results.Add(new JsonObject { ["functionResponse"] = functionResponse });
            }

            contents.Add(new JsonObject { ["role"] = "user", ["parts"] = results });
        }

        throw new AppException("The analysis needed too many steps. Try a narrower question.");
    }

    private async Task<JsonNode> CallGeminiAsync(JsonArray contents)
    {
        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = BuildInstructions() })
            },
            ["contents"] = contents.DeepClone(),
            ["tools"] = JsonNode.Parse(AnalystTools.DeclarationsJson),
            ["generationConfig"] = new JsonObject { ["temperature"] = 0.2, ["maxOutputTokens"] = 4096 }
        };

        var json = await GeminiCaller.GenerateAsync(_http, _settings, body.ToJsonString(), _logger);

        return JsonNode.Parse(json)
               ?? throw new AppException("The assistant is not available right now. Please try again later.");
    }

    private static string JoinText(JsonArray parts)
    {
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            // Skip Gemini's internal "thought" parts; keep only the real answer text.
            if (part?["thought"]?.GetValue<bool>() == true)
            {
                continue;
            }

            var t = part?["text"]?.ToString();
            if (!string.IsNullOrEmpty(t))
            {
                text.Append(t);
            }
        }

        return text.ToString();
    }

    private static string BuildInstructions()
    {
        var now = DateTime.Now;

        var sb = new StringBuilder();
        sb.AppendLine("You are the business analyst of DriveX, a car rental company. You are talking to the ADMIN who runs it.");
        sb.AppendLine($"Current date and time: {now:yyyy-MM-dd HH:mm} (local time). Money is in US dollars. Car prices are per hour.");
        sb.AppendLine();
        sb.AppendLine("How you work:");
        sb.AppendLine("- You can read the live DriveX data ONLY through the tools you are given. Call the tools you need, and call several if the question needs it.");
        sb.AppendLine("- Never guess or invent numbers. Every figure in your answer must come from a tool result. If the data is missing or a tool says it was truncated, say so.");
        sb.AppendLine("- Analyse, do not just list. Compare, rank, spot problems and patterns, and explain your reasoning with the actual numbers.");
        sb.AppendLine("- End with a clear recommendation or decision when the question asks for one. Say how sure you are and what data would change your mind.");
        sb.AppendLine("- You cannot change anything. You only recommend. Tell the admin which action to take and where (for example the admin pages to update a car or its status).");
        sb.AppendLine("- You have no customer names, emails or phone numbers, and you must not ask for them.");
        sb.AppendLine("- Earlier messages of this conversation are included as plain text, so follow-ups like \"and last month?\" make sense. They do not contain the data tables, so call the tools again whenever you need numbers.");
        sb.AppendLine("- Only talk about running DriveX. Politely refuse anything else.");
        sb.AppendLine("- Write plain text. Use short paragraphs and simple numbered lines. Do not use markdown symbols such as ** or #.");
        sb.AppendLine();
        sb.AppendLine("How DriveX works:");
        sb.AppendLine("- Booking status: NOT_DELIVERED (paid, car not handed over yet), ONGOING (customer has the car), SUBMITTED (car returned).");
        sb.AppendLine("- Car status: AVAILABLE, RENTED, MAINTENANCE. A car that is not active is hidden from customers.");
        sb.AppendLine("- Customers pay with bKash or Nagad. A booking price is the car's hourly rate x total hours.");
        sb.AppendLine("- The rental clock starts when staff hand the car over. The car must be back at start time + total hours.");
        sb.AppendLine("- Late fine: only complete late hours count. Fine = complete late hours x (hourly rate + extra), where extra = half the hourly rate rounded down, at least $1.");
        sb.AppendLine("- Income has two kinds: RENTED_PRICE payments (rentals) and FINE payments (late fines).");

        return sb.ToString();
    }
}
