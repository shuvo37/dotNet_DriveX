using System.Globalization;
using System.Text;
using System.Text.Json;
using DriveX.DTOs.Ai;
using DriveX.DTOs.Cars;
using DriveX.Exceptions;
using DriveX.Options;
using Microsoft.Extensions.Options;

namespace DriveX.Services;

public class GeminiAssistantService : IAiAssistantService
{
    private readonly HttpClient _http;
    private readonly ICarService _carService;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAssistantService> _logger;

    public GeminiAssistantService(
        HttpClient http,
        ICarService carService,
        IOptions<GeminiSettings> options,
        ILogger<GeminiAssistantService> logger)
    {
        _http = http;
        _carService = carService;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> AskAsync(string question, IEnumerable<ChatMessage>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new AppException("I'm not ready to answer just yet. Please try again in a little while.");
        }

        // The same public data as GET /api/car: active cars of active companies. Nothing about users.
        var cars = await _carService.GetActiveCarsAsync();

        // Up to the last 10 earlier messages (5 questions + 5 answers), then the new question.
        var contents = new List<object>();
        foreach (var (role, text) in ChatHistory.Clean(history, maxMessages: 10, maxChars: 1000))
        {
            contents.Add(new { role, parts = new[] { new { text } } });
        }

        contents.Add(new { role = "user", parts = new[] { new { text = question.Trim() } } });

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = BuildInstructions(cars) } } },
            contents,
            generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
        };

        var json = await GeminiCaller.GenerateAsync(_http, _settings, JsonSerializer.Serialize(body), _logger);

        var answer = ReadAnswer(json);
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new AppException("I couldn't find an answer to that. Try asking in a different way. I can help with cars, prices and booking.");
        }

        return answer.Trim();
    }

    // Gemini answers with candidates[0].content.parts[*].text; join the text parts.
    private static string ReadAnswer(string json)
    {
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0
            || !candidates[0].TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
            {
                text.Append(t.GetString());
            }
        }

        return text.ToString();
    }

    private static string BuildInstructions(List<CarResponse> cars)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are the DriveX assistant, a friendly helper on a car rental website.");
        sb.AppendLine("Answer the customer's question using ONLY the DriveX information and the car list below.");
        sb.AppendLine("Rules:");
        sb.AppendLine("- Never invent cars, prices, seats, fuel types, cities or availability. If the list does not contain the answer, say you do not have that information.");
        sb.AppendLine("- A car with status AVAILABLE can be rented now. A car with status RENTED is taken at the moment. A car with status MAINTENANCE cannot be rented now.");
        sb.AppendLine("- Prices are per hour, in US dollars.");
        sb.AppendLine("- You cannot book cars yourself. To rent a car, the customer picks it on the Cars page and pays there.");
        sb.AppendLine("- Only talk about renting cars on DriveX. Politely refuse anything else.");
        sb.AppendLine("- Keep answers short and in plain text, with no markdown symbols.");
        sb.AppendLine("- Earlier messages of this conversation are included. Use them to understand follow-ups such as \"it\", \"that car\" or \"and the cheaper one?\".");
        sb.AppendLine();
        sb.AppendLine("How DriveX works (use this for questions about booking, paying, returning and fines):");
        sb.AppendLine("1. A customer logs in, opens the Cars page and presses Book Now on an available car.");
        sb.AppendLine("2. They choose how long to rent: days (0 to 30) plus hours (0 to 23), at least 1 hour in total. Total hours = days x 24 + hours.");
        sb.AppendLine("3. They enter their address and an 11-digit phone number starting with 01.");
        sb.AppendLine("4. The price is the car's hourly rate x total hours.");
        sb.AppendLine("5. They pay with bKash or Nagad. After the payment and booking succeed, the car is marked RENTED.");
        sb.AppendLine("6. Staff hand the car over and start the rental. The customer gets an email, and the rental time counts from that moment.");
        sb.AppendLine("7. The customer must return the car when the rented hours are over (start time + total hours). Staff receive it and submit the return.");
        sb.AppendLine("8. Late fine: only complete late hours count, parts of an hour do not. Fine = complete late hours x (hourly rate + extra). The extra is half the hourly rate rounded down, and at least $1.");
        sb.AppendLine("   Example: a car at $3 per hour has an extra of $1, so each late hour costs $4. Being 45 minutes late costs nothing, 2 hours 40 minutes late costs 2 hours = $8.");
        sb.AppendLine("9. Customers can see their bookings on their Profile page.");
        sb.AppendLine("If a customer asks about something not described here (cancellations, refunds, deposits, documents, insurance), say you do not have that information and suggest contacting the car company.");
        sb.AppendLine("You cannot see any customer's own bookings or payments, so for those questions tell them to open their Profile page.");
        sb.AppendLine();
        sb.AppendLine("Car list (model | company | city | address | price per hour | seats | fuel | status | tag):");

        if (cars.Count == 0)
        {
            sb.AppendLine("(no cars are listed right now)");
        }

        foreach (var c in cars)
        {
            sb.AppendLine(string.Join(" | ",
                c.ModelName,
                c.Company.CompanyName,
                c.Company.CompanyCity,
                c.Company.CompanyAddress,
                "$" + c.PricePerHour.ToString("0.##", CultureInfo.InvariantCulture),
                c.Seats + " seats",
                c.Fuel,
                c.RentalStatus,
                string.IsNullOrWhiteSpace(c.Tag) ? "-" : c.Tag));
        }

        return sb.ToString();
    }
}
