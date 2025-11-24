using DraftPuck.Shared.Achievements;

namespace DraftPuck.Shared.Firebase;
public interface IPushNotificationService
{
    Task SendAchievementNotification(string token, AchievementEntity achievement, CancellationToken ct);
    Task SendLobbyEventNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null);
}
