using DriveX.Exceptions;

namespace DriveX.Services;

// A car can only be rented by a customer in the city where the rental company is
// (a customer in Khulna renting a car in Barisal is useless). The React page checks this too,
// but the server must check it as well, because the browser can be bypassed.
public static class CityRule
{
    public static void EnsureCustomerIsInCarCity(string? customerCity, string carCity)
    {
        if (string.IsNullOrWhiteSpace(customerCity))
        {
            throw new AppException("Please select your city");
        }

        if (!string.Equals(customerCity.Trim(), carCity.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException($"This car is only available in {carCity}");
        }
    }
}
