using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Members;

/// <summary>The caller's own notification settings in one lobby.</summary>
public class LobbyNotificationPreferencesDto
{
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }

    public static LobbyNotificationPreferencesDto From(LobbyMemberEntity member) => new()
    {
        DrinkReceivedNotificationPreference = member.DrinkReceivedNotificationPreference,
        DrinkAwardedNotificationPreference = member.DrinkAwardedNotificationPreference,
        ChatNotificationPreference = member.ChatNotificationPreference,
        PickingStartedNotificationPreference = member.PickingStartedNotificationPreference
    };
}

public class GetLobbyNotificationPreferencesQuery : IRequest<LobbyNotificationPreferencesDto>
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}

public class UpdateLobbyNotificationPreferencesCommand : IRequest<LobbyNotificationPreferencesDto>
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
}

public class LobbyNotificationPreferencesHandler(IDbContext dbContext)
    : IRequestHandler<GetLobbyNotificationPreferencesQuery, LobbyNotificationPreferencesDto>,
      IRequestHandler<UpdateLobbyNotificationPreferencesCommand, LobbyNotificationPreferencesDto>
{
    public async Task<LobbyNotificationPreferencesDto> Handle(GetLobbyNotificationPreferencesQuery request, CancellationToken ct)
        => LobbyNotificationPreferencesDto.From(await GetMemberAsync(request.Code, request.UserId, ct));

    public async Task<LobbyNotificationPreferencesDto> Handle(UpdateLobbyNotificationPreferencesCommand request, CancellationToken ct)
    {
        MemberNotificationPreferences.Validate(
            request.DrinkReceivedNotificationPreference,
            request.DrinkAwardedNotificationPreference,
            request.ChatNotificationPreference,
            request.PickingStartedNotificationPreference);

        var member = await GetMemberAsync(request.Code, request.UserId, ct);
        member.DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference;
        member.DrinkAwardedNotificationPreference = request.DrinkAwardedNotificationPreference;
        member.ChatNotificationPreference = request.ChatNotificationPreference;
        member.PickingStartedNotificationPreference = request.PickingStartedNotificationPreference;
        await dbContext.SaveChangesAsync(ct);

        return LobbyNotificationPreferencesDto.From(member);
    }

    private async Task<LobbyMemberEntity> GetMemberAsync(string code, Guid userId, CancellationToken ct)
        => await dbContext.LobbyMembers
            .FirstOrDefaultAsync(lm => lm.Lobby.JoinCode == code && lm.UserId == userId && !lm.IsRemoved && !lm.IsBot, ct)
            ?? throw new NotFoundException("You're not a member of this lobby.");
}
