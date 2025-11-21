using DraftPuck.Shared.Discord;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Http = System.Net.Http;

namespace DraftPuck.Application.Features.Discord;

public class CompleteLinkDiscordUserCommandHandler(IOAuthCache cache, HttpClient httpClient, IDbContext dbContext, IOptions<DiscordOptions> discordConfig) : IRequestHandler<CompleteLinkDiscordUserCommand, bool>
{
    private readonly DiscordOptions _discordConfig = discordConfig.Value;
    private const string DiscordApiBaseUrl = "https://discord.com/api/v10";

    private static readonly JsonSerializerOptions DiscordSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<bool> Handle(CompleteLinkDiscordUserCommand request, CancellationToken cancellationToken)
    {
        var draftPuckUserId = await cache.GetUserIdAndDeleteByState(request.State);
        if (draftPuckUserId == null) return false;

        var tokenResponse = await ExchangeCodeForToken(request.Code);
        if (tokenResponse == null) return false;

        var discordUser = await GetDiscordIdentity(tokenResponse.AccessToken);
        if (discordUser == null) return false;

        var userEntity = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == draftPuckUserId, cancellationToken);
        if (userEntity == null) return false;

        userEntity.DiscordUserId = discordUser.Id;
        userEntity.DiscordUserLinkedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<DiscordTokenResponseDto?> ExchangeCodeForToken(string code)
    {
        if (string.IsNullOrEmpty(_discordConfig.ClientSecret))
            throw new Exception("Discord client secret not configured.");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"client_id", _discordConfig.ClientId!},
            {"client_secret", _discordConfig.ClientSecret!},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", _discordConfig.RedirectUri!}
        });

        var response = await httpClient.PostAsync($"{DiscordApiBaseUrl}/oauth2/token", content);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<DiscordTokenResponseDto>(json, DiscordSerializerOptions);
    }

    private async Task<DiscordIdentityResponseDto?> GetDiscordIdentity(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{DiscordApiBaseUrl}/users/@me");
        request.Headers.Authorization = new Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscordIdentityResponseDto>(json, DiscordSerializerOptions);
    }
}