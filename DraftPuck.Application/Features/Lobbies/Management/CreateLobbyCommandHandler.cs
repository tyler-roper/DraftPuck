using DraftPuck.Application.Features.Lobbies.Members;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class CreateLobbyCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<CreateLobbyCommand, LobbyDto>
{
    private static readonly Random _random = new();

    public async Task<LobbyDto> Handle(CreateLobbyCommand request, CancellationToken ct)
    {
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
        dbContext.LobbyMembers.Add(hostMember);

        var botNotifications = new List<LobbyMemberEventPayload>();

        foreach (var botRequest in request.Bots)
        {
            var botName = LobbyMemberNames.Normalize(botRequest.Nickname);
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

        await mediator.Publish(new LobbyCreatedNotification(new(lobby)), ct);
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
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        } while (await dbContext.Lobbies.AnyAsync(l => l.JoinCode == result));

        return result;
    }
}