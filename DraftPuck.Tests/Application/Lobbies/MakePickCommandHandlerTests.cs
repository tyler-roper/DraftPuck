using System.ComponentModel.DataAnnotations;
using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Picks;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Lobbies;

[Collection(DatabaseCollection.Name)]
public class MakePickCommandHandlerTests(DatabaseFixture fixture)
{
    private readonly IGameCache _gameCache = Substitute.For<IGameCache>();

    private sealed record Setup(int GameId, string Code, LobbyMemberEntity Host, LobbyMemberEntity Member, LobbyMemberEntity Bot);

    /// <summary>A lobby (1 pick per team) with a host, a member and a bot, for a game in the cache starting at the given offset.</summary>
    private async Task<Setup> SetupAsync(TimeSpan startsIn)
    {
        var gameId = Seed.NewGameId();
        _gameCache.GetGameByIdAsync(gameId).Returns(TestGames.Create(gameId, DateTime.UtcNow.Add(startsIn)));

        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var (lobby, host) = seed.Lobby([gameId]);
        var member = seed.Member(lobby);
        var bot = seed.Bot(lobby);
        await db.SaveChangesAsync();
        return new Setup(gameId, lobby.JoinCode, host, member, bot);
    }

    private async Task PickAsync(Setup s, LobbyMemberEntity requester, int playerId, int teamId = TestGames.HomeTeamId,
        Guid? forMemberId = null, int? gameId = null)
    {
        await using var db = fixture.CreateContext();
        var handler = new MakePickCommandHandler(db, Substitute.For<AutoMapper.IMapper>(), Substitute.For<IMediator>(), _gameCache,
            Options.Create(new ApplicationOptions()));

        await handler.Handle(new MakePickCommand
        {
            Code = s.Code,
            UserId = requester.UserId,
            LobbyMemberId = forMemberId,
            GameId = gameId ?? s.GameId,
            TeamId = teamId,
            PlayerId = playerId
        }, default);
    }

    private async Task<int> PickCountAsync(Guid memberId)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyMemberPicks.CountAsync(p => p.LobbyMemberId == memberId && p.IsActive);
    }

    private static readonly TimeSpan WindowOpen = TimeSpan.FromMinutes(10);

    [Fact]
    public async Task MemberCanPickWhileTheWindowIsOpen()
    {
        var s = await SetupAsync(WindowOpen);

        await PickAsync(s, s.Member, TestGames.HomePlayer(0));

        Assert.Equal(1, await PickCountAsync(s.Member.Id));
    }

    [Fact]
    public async Task PickingStaysOpenAfterPuckDrop()
    {
        var s = await SetupAsync(TimeSpan.FromMinutes(-45));

        await PickAsync(s, s.Member, TestGames.HomePlayer(0));

        Assert.Equal(1, await PickCountAsync(s.Member.Id));
    }

    [Fact]
    public async Task RejectsPicksBeforeTheWindowOpens()
    {
        var s = await SetupAsync(TimeSpan.FromMinutes(31));

        await Assert.ThrowsAsync<BadRequestException>(() => PickAsync(s, s.Member, TestGames.HomePlayer(0)));
        Assert.Equal(0, await PickCountAsync(s.Member.Id));
    }

    [Fact]
    public async Task RejectsGamesOutsideTheLobby()
    {
        var s = await SetupAsync(WindowOpen);
        var otherGameId = Seed.NewGameId();
        _gameCache.GetGameByIdAsync(otherGameId).Returns(TestGames.Create(otherGameId, DateTime.UtcNow.Add(WindowOpen)));

        await Assert.ThrowsAsync<BadRequestException>(() => PickAsync(s, s.Member, TestGames.HomePlayer(0), gameId: otherGameId));
    }

    [Fact]
    public async Task RejectsPlayersNotOnTheTeam()
    {
        var s = await SetupAsync(WindowOpen);

        await Assert.ThrowsAsync<BadRequestException>(() => PickAsync(s, s.Member, TestGames.AwayPlayer(0), teamId: TestGames.HomeTeamId));
    }

    [Fact]
    public async Task RejectsPicksBeyondTheLimitPerTeam()
    {
        var s = await SetupAsync(WindowOpen);
        await PickAsync(s, s.Member, TestGames.HomePlayer(0));

        await Assert.ThrowsAsync<BadRequestException>(() => PickAsync(s, s.Member, TestGames.HomePlayer(1)));

        // The limit is per team: an away pick is still allowed.
        await PickAsync(s, s.Member, TestGames.AwayPlayer(0), teamId: TestGames.AwayTeamId);
        Assert.Equal(2, await PickCountAsync(s.Member.Id));
    }

    [Fact]
    public async Task RejectsAPlayerAlreadyPickedInTheLobby()
    {
        var s = await SetupAsync(WindowOpen);
        await PickAsync(s, s.Host, TestGames.HomePlayer(0));

        await Assert.ThrowsAsync<ValidationException>(() => PickAsync(s, s.Member, TestGames.HomePlayer(0)));
    }

    [Fact]
    public async Task RejectsUsersOutsideTheLobby()
    {
        var s = await SetupAsync(WindowOpen);
        var outsider = new LobbyMemberEntity { UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ForbiddenException>(() => PickAsync(s, outsider, TestGames.HomePlayer(0)));
    }

    [Fact]
    public async Task HostCanPickForABot()
    {
        var s = await SetupAsync(WindowOpen);

        await PickAsync(s, s.Host, TestGames.HomePlayer(0), forMemberId: s.Bot.Id);

        Assert.Equal(1, await PickCountAsync(s.Bot.Id));
        Assert.Equal(0, await PickCountAsync(s.Host.Id));
    }

    [Fact]
    public async Task OnlyTheHostCanPickForABot()
    {
        var s = await SetupAsync(WindowOpen);

        await Assert.ThrowsAsync<ForbiddenException>(() => PickAsync(s, s.Member, TestGames.HomePlayer(0), forMemberId: s.Bot.Id));
    }

    [Fact]
    public async Task HostCannotPickForAnotherHuman()
    {
        var s = await SetupAsync(WindowOpen);

        await Assert.ThrowsAsync<ForbiddenException>(() => PickAsync(s, s.Host, TestGames.HomePlayer(0), forMemberId: s.Member.Id));
    }

    [Fact]
    public async Task ConcurrentPicksOfTheSamePlayerLeaveExactlyOne()
    {
        var s = await SetupAsync(WindowOpen);

        var results = await Task.WhenAll(
            Capture(PickAsync(s, s.Host, TestGames.HomePlayer(0))),
            Capture(PickAsync(s, s.Member, TestGames.HomePlayer(0))));

        Assert.Equal(1, await PickCountAsync(s.Host.Id) + await PickCountAsync(s.Member.Id));
        Assert.All(results, ex => Assert.True(ex is null or ValidationException, ex?.ToString()));
    }

    private static async Task<Exception?> Capture(Task task)
    {
        try { await task; return null; }
        catch (Exception ex) { return ex; }
    }
}
