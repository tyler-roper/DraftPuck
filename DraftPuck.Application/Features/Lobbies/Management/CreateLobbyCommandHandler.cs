using DraftPuck.Application.Features.Lobbies.Members;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class CreateLobbyCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<CreateLobbyCommand, LobbyDto>
{
    public async Task<LobbyDto> Handle(CreateLobbyCommand request, CancellationToken ct)
    {
        LobbyRules.ValidateNewLobby(request.PicksPerTeam, request.GameIds, request.Bots.Count);
        var hostName = LobbyMemberNames.Normalize(request.Name);

        var newLobbyId = Guid.NewGuid();
        var lobby = new LobbyEntity
        {
            Id = newLobbyId,
            JoinCode = await RandomString(4),
            CreatedBy = request.CreatorUserId,
            PicksPerTeam = request.PicksPerTeam,
            IsBotAutoPickingEnabled = request.IsBotAutoPickingEnabled,
            GameIds = request.GameIds
        };
        dbContext.Lobbies.Add(lobby);

        var hostMember = new LobbyMemberEntity
        {
            LobbyId = newLobbyId,
            UserId = request.CreatorUserId,
            Name = hostName
        };
        if (await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.CreatorUserId, ct) is { } host)
            MemberNotificationPreferences.StampDefaults(hostMember, host);
        dbContext.LobbyMembers.Add(hostMember);

        var botNotifications = new List<LobbyMemberEventPayload>();

        var takenNames = new HashSet<string>([hostName], StringComparer.OrdinalIgnoreCase);
        foreach (var botRequest in request.Bots)
        {
            var botName = LobbyMemberNames.Normalize(botRequest.Nickname);
            if (!takenNames.Add(botName))
                throw new ValidationException($"The name '{botName}' is already taken in this lobby.");
            LobbyRules.ValidateBotPickStyle(botRequest.PickStyle);
            var botUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                IsBot = true,
                Nickname = botName
            };
            dbContext.Users.Add(botUser);

            var botMember = new LobbyMemberEntity
            {
                LobbyId = newLobbyId,
                UserId = botUser.Id,
                Name = botName,
                IsBot = true,
                BotPickStyle = botRequest.PickStyle
            };
            dbContext.LobbyMembers.Add(botMember);

            botNotifications.Add(new LobbyMemberEventPayload(lobby, botMember));
        }

        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new LobbyCreatedNotification(new(lobby, hostMember)), ct);
        await mediator.Publish(new UserJoinedLobbyNotification(new(lobby, hostMember)), ct);

        foreach (var payload in botNotifications)
            await mediator.Publish(new UserJoinedLobbyNotification(payload), ct);

        return mapper.Map<LobbyDto>(lobby);
    }

    private async Task<string> RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string result;
        do
        {
            result = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        } while (await dbContext.Lobbies.AnyAsync(l => l.JoinCode == result));

        return result;
    }
}