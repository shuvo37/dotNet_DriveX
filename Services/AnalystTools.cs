using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DriveX.Data;
using DriveX.Enums;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

// The read-only "tools" the AI analyst may call. Each one reads the live database and returns
// business data only: ids, car models, amounts, hours. Never names, emails, phones or addresses of customers.
public class AnalystTools
{
    private const int MaxRows = 100;      // rows sent back to the AI per tool call
    private const int MaxFetch = 5000;    // rows read from the database per tool call

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DriveXDbContext _db;

    public AnalystTools(DriveXDbContext db)
    {
        _db = db;
    }

    // What the AI is told about the tools (Gemini "function declarations").
    public const string DeclarationsJson = """
    [
      {
        "functionDeclarations": [
          {
            "name": "get_fleet_status",
            "description": "Lists every car with its company, city, hourly price, seats, fuel, rental status (AVAILABLE, RENTED, MAINTENANCE) and whether it is active. Also counts active cars per status."
          },
          {
            "name": "get_bookings",
            "description": "Lists bookings made in a date range (by booking time), newest first, at most 100. Each row has bookingId, car model, status, booking time, rent start, rent end, total hours and total price. Also returns the total number of matching bookings.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "status": { "type": "STRING", "enum": ["NOT_DELIVERED", "ONGOING", "SUBMITTED"], "description": "Only bookings with this status. Leave out for all statuses." },
                "from_date": { "type": "STRING", "description": "Start date, format yyyy-MM-dd. Default: 30 days ago." },
                "to_date": { "type": "STRING", "description": "End date (included), format yyyy-MM-dd. Default: today." },
                "car_model": { "type": "STRING", "description": "Only bookings of cars whose model name contains this text." }
              }
            }
          },
          {
            "name": "get_late_returns",
            "description": "Looks at all ONGOING rentals and shows which are late right now: the time the car had to be back, whole hours late, and the fine owed under the DriveX late-fine rule. Also shows how much time is left for rentals that are not late."
          },
          {
            "name": "get_revenue",
            "description": "Money received in a date range (by payment time): total from rental payments, total from fines, number of payments, and a per-car-model breakdown sorted by total.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "from_date": { "type": "STRING", "description": "Start date, format yyyy-MM-dd. Default: 30 days ago." },
                "to_date": { "type": "STRING", "description": "End date (included), format yyyy-MM-dd. Default: today." }
              }
            }
          },
          {
            "name": "get_car_performance",
            "description": "For every active car: number of bookings, total hours booked and booking revenue in a date range (by booking time). Cars with zero bookings are included, so it can show the best and the least rented cars.",
            "parameters": {
              "type": "OBJECT",
              "properties": {
                "from_date": { "type": "STRING", "description": "Start date, format yyyy-MM-dd. Default: 30 days ago." },
                "to_date": { "type": "STRING", "description": "End date (included), format yyyy-MM-dd. Default: today." }
              }
            }
          }
        ]
      }
    ]
    """;

    public async Task<JsonNode> RunAsync(string name, JsonObject? args)
    {
        object result = name switch
        {
            "get_fleet_status" => await FleetStatusAsync(),
            "get_bookings" => await BookingsAsync(args),
            "get_late_returns" => await LateReturnsAsync(),
            "get_revenue" => await RevenueAsync(args),
            "get_car_performance" => await CarPerformanceAsync(args),
            _ => new { error = $"Unknown tool: {name}" }
        };

        return JsonSerializer.SerializeToNode(result, JsonOptions)!;
    }

    // ---------- tools ----------

    private async Task<object> FleetStatusAsync()
    {
        var cars = await _db.Cars.AsNoTracking()
            .OrderBy(c => c.CarId)
            .Select(c => new
            {
                c.CarId,
                c.ModelName,
                Company = c.Company.CompanyName,
                City = c.Company.CompanyCity,
                c.PricePerHour,
                c.Seats,
                c.Fuel,
                c.RentalStatus,
                c.IsActive
            })
            .ToListAsync();

        var rows = cars.Select(c => new
        {
            c.CarId,
            c.ModelName,
            c.Company,
            c.City,
            c.PricePerHour,
            c.Seats,
            Fuel = c.Fuel.ToString(),
            RentalStatus = c.RentalStatus.ToString(),
            c.IsActive
        }).ToList();

        var activeByStatus = cars
            .Where(c => c.IsActive)
            .GroupBy(c => c.RentalStatus.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new
        {
            totalCars = cars.Count,
            activeCarsByStatus = activeByStatus,
            cars = rows.Take(MaxRows),
            truncated = rows.Count > MaxRows
        };
    }

    private async Task<object> BookingsAsync(JsonObject? args)
    {
        var (start, endExclusive) = DateRange(args);
        var statusText = Arg(args, "status");
        var carModel = Arg(args, "car_model");

        var query = _db.Bookings.AsNoTracking()
            .Where(b => b.BookingTime >= start && b.BookingTime < endExclusive);

        if (!string.IsNullOrWhiteSpace(statusText))
        {
            if (!Enum.TryParse<BookingStatus>(statusText, ignoreCase: true, out var status))
            {
                return new { error = "status must be NOT_DELIVERED, ONGOING or SUBMITTED" };
            }

            query = query.Where(b => b.BookingStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(carModel))
        {
            query = query.Where(b => b.Car.ModelName.Contains(carModel));
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(b => b.BookingTime)
            .Take(MaxRows)
            .Select(b => new
            {
                b.BookingId,
                CarModel = b.Car.ModelName,
                b.BookingStatus,
                b.BookingTime,
                b.RentStart,
                b.RentEnd,
                b.TotalHours,
                b.TotalPrice
            })
            .ToListAsync();

        return new
        {
            fromDate = start.ToString("yyyy-MM-dd"),
            toDate = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            totalMatching = total,
            truncated = total > MaxRows,
            bookings = rows.Select(b => new
            {
                b.BookingId,
                b.CarModel,
                Status = b.BookingStatus?.ToString(),
                b.BookingTime,
                b.RentStart,
                b.RentEnd,
                b.TotalHours,
                b.TotalPrice
            })
        };
    }

    private async Task<object> LateReturnsAsync()
    {
        var now = DateTime.Now;

        var ongoing = await _db.Bookings.AsNoTracking()
            .Where(b => b.BookingStatus == BookingStatus.ONGOING && b.RentStart != null)
            .OrderBy(b => b.BookingId)
            .Take(MaxFetch)
            .Select(b => new
            {
                b.BookingId,
                CarModel = b.Car.ModelName,
                Rate = b.Car.PricePerHour,
                RentStart = b.RentStart!.Value,
                b.TotalHours
            })
            .ToListAsync();

        var rows = ongoing.Select(b =>
        {
            var mustReturnBy = b.RentStart.AddHours(b.TotalHours);
            var minutesLate = Math.Max(0, (int)Math.Floor((now - mustReturnBy).TotalMinutes));

            // Same rule as the staff page (CarSubmission.jsx): only complete late hours count,
            // and each late hour costs the hourly rate + extra, where extra = max(1, floor(rate / 2)).
            var wholeHoursLate = minutesLate / 60;
            var extra = Math.Max(1, Math.Floor(b.Rate / 2));
            var fine = wholeHoursLate > 0 ? Math.Round(wholeHoursLate * (b.Rate + extra), 2) : 0;

            return new
            {
                b.BookingId,
                b.CarModel,
                b.RentStart,
                MustReturnBy = mustReturnBy,
                IsLate = minutesLate > 0,
                MinutesLate = minutesLate,
                WholeHoursLate = wholeHoursLate,
                FineOwedNow = fine,
                MinutesLeft = minutesLate > 0 ? 0 : (int)Math.Ceiling((mustReturnBy - now).TotalMinutes)
            };
        }).ToList();

        return new
        {
            now,
            ongoingRentals = rows.Count,
            lateRentals = rows.Count(r => r.IsLate),
            totalFineOwedNow = Math.Round(rows.Sum(r => r.FineOwedNow), 2),
            rentals = rows.OrderByDescending(r => r.MinutesLate).Take(MaxRows)
        };
    }

    private async Task<object> RevenueAsync(JsonObject? args)
    {
        var (start, endExclusive) = DateRange(args);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.PaymentTime >= start && p.PaymentTime < endExclusive)
            .OrderBy(p => p.PaymentId)
            .Take(MaxFetch)
            .Select(p => new { p.PaymentType, p.TotalAmount, CarModel = p.Car.ModelName })
            .ToListAsync();

        var rental = payments.Where(p => p.PaymentType == PaymentType.RENTED_PRICE).Sum(p => p.TotalAmount);
        var fines = payments.Where(p => p.PaymentType == PaymentType.FINE).Sum(p => p.TotalAmount);

        var perCar = payments
            .GroupBy(p => p.CarModel)
            .Select(g => new
            {
                CarModel = g.Key,
                Payments = g.Count(),
                RentalIncome = Math.Round(g.Where(p => p.PaymentType == PaymentType.RENTED_PRICE).Sum(p => p.TotalAmount), 2),
                FineIncome = Math.Round(g.Where(p => p.PaymentType == PaymentType.FINE).Sum(p => p.TotalAmount), 2),
                Total = Math.Round(g.Sum(p => p.TotalAmount), 2)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return new
        {
            fromDate = start.ToString("yyyy-MM-dd"),
            toDate = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            paymentsCount = payments.Count,
            truncated = payments.Count >= MaxFetch,
            rentalIncome = Math.Round(rental, 2),
            fineIncome = Math.Round(fines, 2),
            totalIncome = Math.Round(rental + fines, 2),
            perCarModel = perCar.Take(MaxRows)
        };
    }

    private async Task<object> CarPerformanceAsync(JsonObject? args)
    {
        var (start, endExclusive) = DateRange(args);

        var cars = await _db.Cars.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CarId)
            .Select(c => new { c.ModelName, c.PricePerHour, c.RentalStatus })
            .ToListAsync();

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => b.BookingTime >= start && b.BookingTime < endExclusive)
            .OrderBy(b => b.BookingId)
            .Take(MaxFetch)
            .Select(b => new { CarModel = b.Car.ModelName, b.TotalHours, b.TotalPrice })
            .ToListAsync();

        var byModel = bookings.GroupBy(b => b.CarModel).ToDictionary(g => g.Key, g => g.ToList());

        var rows = cars.Select(c =>
        {
            byModel.TryGetValue(c.ModelName, out var list);

            return new
            {
                c.ModelName,
                c.PricePerHour,
                RentalStatus = c.RentalStatus.ToString(),
                Bookings = list?.Count ?? 0,
                HoursBooked = list?.Sum(x => x.TotalHours) ?? 0,
                BookingRevenue = Math.Round(list?.Sum(x => x.TotalPrice) ?? 0, 2)
            };
        })
        .OrderByDescending(r => r.BookingRevenue)
        .ToList();

        return new
        {
            fromDate = start.ToString("yyyy-MM-dd"),
            toDate = endExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            truncated = bookings.Count >= MaxFetch,
            cars = rows.Take(MaxRows)
        };
    }

    // ---------- helpers ----------

    private static string? Arg(JsonObject? args, string key) => args?[key]?.ToString();

    // from_date / to_date (yyyy-MM-dd). Defaults: the last 30 days up to and including today.
    // Returns [from, toExclusive) so the whole "to" day is included.
    private static (DateTime Start, DateTime EndExclusive) DateRange(JsonObject? args)
    {
        var today = DateTime.Now.Date;

        var start = TryDate(Arg(args, "from_date")) ?? today.AddDays(-30);
        var end = TryDate(Arg(args, "to_date")) ?? today;

        if (end < start)
        {
            (start, end) = (end, start);
        }

        return (start, end.AddDays(1));
    }

    private static DateTime? TryDate(string? text)
    {
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.Date
            : null;
    }
}
