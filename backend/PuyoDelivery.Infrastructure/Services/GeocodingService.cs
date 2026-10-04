using System.Net.Http.Json;
using System.Text.Json;

namespace PuyoDelivery.Infrastructure.Services;

public class GeocodingService
{
    private readonly HttpClient _httpClient;

    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(double Lat, double Lng)?> GeocodeAsync(string address)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(address)}&limit=1";
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PuyoDelivery/1.0");
            var response = await _httpClient.GetFromJsonAsync<List<GeocodingResult>>(url);

            if (response is { Count: > 0 })
            {
                var result = response[0];
                if (double.TryParse(result.lat, out var lat) && double.TryParse(result.lon, out var lng))
                    return (lat, lng);
            }
        }
        catch { }
        return null;
    }

    private record GeocodingResult(string lat, string lon, string display_name);
}
