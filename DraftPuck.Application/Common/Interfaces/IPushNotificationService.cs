﻿using DraftPuck.Application.Features.Achievements;
﻿using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Domain.Achievements;

namespace DraftPuck.Application.Common.Interfaces;
public interface IPushNotificationService
{
    Task<bool> SendAchievementNotification(string token, AchievementEntity achievement, CancellationToken ct);
    Task<bool> SendLobbyEventNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
