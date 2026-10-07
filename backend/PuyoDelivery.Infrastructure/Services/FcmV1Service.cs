using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace PuyoDelivery.Infrastructure.Services;

/// <summary>
/// Envío de notificaciones push vía Firebase Cloud Messaging (API V1).
/// Si no hay cuenta de servicio configurada, todos los envíos son no-op.
/// </summary>
public class FcmV1Service
{
    private readonly bool _enabled;

    public FcmV1Service(string? serviceAccountPath, string? serviceAccountJson)
    {
        try
        {
            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                credential = GoogleCredential.FromJson(serviceAccountJson);
            }
            else if (!string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath))
            {
                credential = GoogleCredential.FromFile(serviceAccountPath);
            }
            else
            {
                _enabled = false;
                return;
            }

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions { Credential = credential });
            }
            _enabled = true;
        }
        catch
        {
            _enabled = false;
        }
    }

    public bool IsEnabled => _enabled;

    public async Task<bool> SendToTokenAsync(string? fcmToken, string title, string body,
        Dictionary<string, string>? data = null)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(fcmToken)) return false;

        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification { Title = title, Body = body },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "puyo_rider_channel",
                        Sound = "default"
                    }
                },
                Data = data ?? new Dictionary<string, string>()
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
