using DraftPuck.DiscordBot.Interfaces;
using DraftPuck.Shared.Discord;
using System.Net.Http.Json;

namespace DraftPuck.DiscordBot.Services;

public class InternalApiClient(HttpClient httpClient) : IInternalApiClient
{
    public async Task SendDiscordServerJoinedNotification(string userId, CancellationToken ct)
    {
        var request = new DiscordServerJoinedRequestDto { DiscordUserId = userId };

        using var response = await httpClient.PostAsJsonAsync(
            "api/internal/discord-server-joined",
            request,
            ct);

        response.EnsureSuccessStatusCode();
    }
}
