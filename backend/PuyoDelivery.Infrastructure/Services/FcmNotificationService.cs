using System.Net.Http.Json;

namespace PuyoDelivery.Infrastructure.Services;

public class FcmNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _serverKey;

    public FcmNotificationService(HttpClient httpClient, string serverKey)
    {
        _httpClient = httpClient;
        _serverKey = serverKey;
    }

    public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(fcmToken)) return false;

        try
        {
            var payload = new
            {
                to = fcmToken,
                notification = new { title, body, sound = "default" },
                priority = "high"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.Add("Authorization", $"key={_serverKey}");
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
