using DraftPuck.Shared.Discord;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Discord;

public class BeginLinkDiscordUserCommandHandler(IOAuthCache cache, IOptions<DiscordOptions> discordConfig) : IRequestHandler<BeginLinkDiscordUserCommand, string>
{
    private readonly DiscordOptions _discordConfig = discordConfig.Value;

    public async Task<string> Handle(BeginLinkDiscordUserCommand request, CancellationToken cancellationToken)
    {
        var state = Guid.NewGuid();
        await cache.AddStateAsync(state, request.DraftPuckUserId);

        var clientId = _discordConfig.ClientId;
        var redirectUri = _discordConfig.RedirectUri;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            throw new Exception("Discord clientId or redirectUri are missing from configuration.");

        var discordAuthUrl =
            $"https://discord.com/oauth2/authorize" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope=identify%20guilds.join" +
            $"&state={state}";

        return discordAuthUrl;
    }
}