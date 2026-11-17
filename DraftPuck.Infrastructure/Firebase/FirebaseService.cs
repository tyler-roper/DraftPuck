using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Infrastructure.Persistence;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;

public class FirebaseService(
    IOptions<ApplicationOptions> appConfig,
    FirebaseApp firebaseApp,
    IServiceScopeFactory scopeFactory,
    ILogger<FirebaseService> logger) : IPushNotificationService
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    private static class PushNotificationTypes
    {
        public static readonly string LOBBY_EVENT = "LobbyEvent";
        public static readonly string ACHIEVEMENT = "Achievement";
    }

    public Task<int> SendAchievementNotification(IReadOnlyCollection<string> tokens, AchievementEntity achievement, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            { "type", PushNotificationTypes.ACHIEVEMENT },
            { "title", $"Achievement earned: {achievement.FriendlyName}" },
            { "body", achievement.Description ?? "" },
            { "icon", _appConfig.GetAbsoluteUrl("img/icons/icon-192.png") },
            { "badge", _appConfig.GetAbsoluteUrl("img/icons/badge.png") }
        };

        return Send(tokens, data, ct);
    }

    public Task<int> SendRecapNotification(string lobbyCode, string title, string message, IReadOnlyCollection<string> tokens, CancellationToken ct = default) =>
        SendLobbyEventNotification(lobbyCode, title, message, tokens, new Dictionary<string, string> { { "link", _appConfig.GetAbsoluteUrl($"lobby/{lobbyCode}/review") } }, ct);

    public Task<int> SendLobbyEventNotification(string lobbyCode, string title, string message, IReadOnlyCollection<string> tokens, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var messageData = new Dictionary<string, string>
        {
            { "type", PushNotificationTypes.LOBBY_EVENT },
            { "title", title ?? "" },
            { "body", message ?? "" },
            { "icon", _appConfig.GetAbsoluteUrl("img/icons/icon-192.png") },
            { "badge", _appConfig.GetAbsoluteUrl("img/icons/badge.png") },
            { "link", _appConfig.GetAbsoluteUrl($"lobby/{lobbyCode}") }
        };

        if (data != null)
        {
            foreach (var kvp in data)
                messageData[kvp.Key] = kvp.Value ?? "";
        }

        return Send(tokens, messageData, ct);
    }

    /// <summary>
    /// Whether Firebase's error means the token will never work again (the app was uninstalled, notifications were
    /// turned off, or the token belongs to another project), as opposed to a failure worth retrying next time.
    /// </summary>
    public static bool IsDeadToken(MessagingErrorCode? errorCode) =>
        errorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch;

    private async Task<int> Send(IReadOnlyCollection<string> tokens, Dictionary<string, string> data, CancellationToken ct)
    {
        if (tokens.Count == 0) return 0;

        // Data-only message: the service worker builds and shows the notification itself.
        // High urgency so delivery isn't deferred while the device is idle (e.g. Android doze).
        var message = new MulticastMessage
        {
            Tokens = [.. tokens],
            Data = data,
            Webpush = new WebpushConfig { Headers = new Dictionary<string, string> { { "Urgency", "high" } } }
        };

        BatchResponse batch;
        try
        {
            batch = await FirebaseMessaging.GetMessaging(firebaseApp).SendEachForMulticastAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {type} push notification to {count} devices.", data.GetValueOrDefault("type"), tokens.Count);
            return 0;
        }

        var tokenList = message.Tokens.ToList();
        var dead = new List<string>();
        for (var i = 0; i < batch.Responses.Count; i++)
        {
            var response = batch.Responses[i];
            if (response.IsSuccess) continue;

            if (IsDeadToken(response.Exception?.MessagingErrorCode))
                dead.Add(tokenList[i]);
            else
                logger.LogWarning(response.Exception, "A device didn't get a {type} push notification.", data.GetValueOrDefault("type"));
        }

        if (dead.Count > 0)
            await RemoveDevicesAsync(dead, ct);

        return batch.SuccessCount;
    }

    // In a scope of its own: senders call this from parallel loops, where their own DbContext can't be shared.
    private async Task RemoveDevicesAsync(List<string> tokens, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DraftPuckContext>();
            var removed = await db.UserDevices.Where(d => tokens.Contains(d.Token)).ExecuteDeleteAsync(ct);
            logger.LogInformation("Removed {count} devices that no longer accept push notifications.", removed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't remove {count} devices that no longer accept push notifications.", tokens.Count);
        }
    }
}
