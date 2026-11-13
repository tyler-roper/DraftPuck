namespace DraftPuck.Tests.Support;

/// <summary>
/// Adds test data to a context. Call <c>SaveChangesAsync</c> on the context afterwards.
/// Join codes and game IDs are unique per test run, so tests sharing the database don't see each other's data.
/// </summary>
public class Seed(DraftPuckContext db)
{
    private static int _joinCodeCounter;
    private static int _gameIdCounter = 2_000_000;

    public static int NewGameId() => Interlocked.Increment(ref _gameIdCounter);

    public static string NewJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var n = Interlocked.Increment(ref _joinCodeCounter);
        var code = new char[4];
        for (var i = 3; i >= 0; i--)
        {
            code[i] = chars[n % chars.Length];
            n /= chars.Length;
        }
        return new string(code);
    }

    public UserEntity User(bool isBot = false)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), IsBot = isBot, Nickname = isBot ? "Bot" : "Human" };
        db.Users.Add(user);
        return user;
    }

    /// <summary>
    /// A lobby for the given games, with its host as the first member.
    /// </summary>
    public (LobbyEntity Lobby, LobbyMemberEntity Host) Lobby(int[] gameIds, int picksPerTeam = 1, bool isActive = true, bool botAutoPicking = true)
    {
        var hostUser = User();
        var lobby = new LobbyEntity
        {
            Id = Guid.NewGuid(),
            JoinCode = NewJoinCode(),
            CreatedBy = hostUser.Id,
            PicksPerTeam = picksPerTeam,
            GameIds = [.. gameIds],
            IsActive = isActive,
            IsBotAutoPickingEnabled = botAutoPicking
        };
        db.Lobbies.Add(lobby);
        return (lobby, Member(lobby, hostUser));
    }

    public LobbyMemberEntity Member(LobbyEntity lobby, UserEntity? user = null)
    {
        user ??= User();
        var member = new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = user.Id, Name = "Member" };
        db.LobbyMembers.Add(member);
        return member;
    }

    public LobbyMemberEntity Bot(LobbyEntity lobby, BotPickStyle? style = BotPickStyle.Best)
    {
        var user = User(isBot: true);
        var bot = new LobbyMemberEntity
        {
            Id = Guid.NewGuid(),
            LobbyId = lobby.Id,
            UserId = user.Id,
            Name = "Bot",
            IsBot = true,
            BotPickStyle = style
        };
        db.LobbyMembers.Add(bot);
        return bot;
    }

    public LobbyMemberPickEntity Pick(LobbyMemberEntity member, int gameId, int teamId, int playerId)
    {
        var pick = new LobbyMemberPickEntity
        {
            Id = Guid.NewGuid(),
            LobbyMemberId = member.Id,
            GameId = gameId,
            TeamId = teamId,
            PlayerId = playerId
        };
        db.LobbyMemberPicks.Add(pick);
        return pick;
    }
}
