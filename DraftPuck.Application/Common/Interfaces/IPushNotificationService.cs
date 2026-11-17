using DraftPuck.Domain.Achievements;

namespace DraftPuck.Application.Common.Interfaces;

/// <summary>
/// Sends push notifications to all of a user's devices (their tokens). Each returns how many devices received it;
/// devices Firebase reports as gone (app uninstalled, permission revoked) are removed.
/// </summary>
public interface IPushNotificationService
{
    Task<int> SendAchievementNotification(IReadOnlyCollection<string> tokens, AchievementEntity achievement, CancellationToken ct);
    /// <summary>The day-after push for a lobby; tapping it opens the lobby's post-game page.</summary>
    Task<int> SendRecapNotification(string lobbyCode, string title, string message, IReadOnlyCollection<string> tokens, CancellationToken ct = default);
    Task<int> SendLobbyEventNotification(string lobbyCode, string title, string message, IReadOnlyCollection<string> tokens, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
