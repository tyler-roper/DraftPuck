using DraftPuck.Domain.Achievements;

namespace DraftPuck.Application.Common.Interfaces;

/// <summary>
/// Sends push notifications to all of a user's devices (their tokens). Each returns how many devices received it;
/// devices Firebase reports as gone (app uninstalled, permission revoked) are removed.
/// </summary>
public interface IPushNotificationService
{
    Task<int> SendAchievementNotification(IReadOnlyCollection<string> tokens, AchievementEntity achievement, CancellationToken ct);
    Task<int> SendLobbyEventNotification(string lobbyCode, string title, string message, IReadOnlyCollection<string> tokens, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
