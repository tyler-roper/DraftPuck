using System.ComponentModel.DataAnnotations;
using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Drinks;
using DraftPuck.Application.Features.Lobbies.Management;
using DraftPuck.Application.Features.Lobbies.Messages;
using DraftPuck.Application.Features.Lobbies.Picks;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>Removing picks, assigning drinks, chat, and deactivating stale lobbies.</summary>
[Collection(DatabaseCollection.Name)]
public class LobbyActivityTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IGameCache _gameCache = Substitute.For<IGameCache>();

    private sealed record Setup(int GameId, string Code, LobbyMemberEntity Host, LobbyMemberEntity Member, LobbyMemberEntity Other, LobbyMemberPickEntity MemberPick);

    /// <summary>A lobby with a host, a member who has one pick, and another member.</summary>
    private async Task<Setup> SetupAsync(GameState gameState = GameState.Upcoming)
    {
        var gameId = Seed.NewGameId();
        _gameCache.GetGameByIdAsync(gameId).Returns(TestGames.Create(gameId, DateTime.UtcNow, gameState));

        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var (lobby, host) = seed.Lobby([gameId]);
        var member = seed.Member(lobby);
        var other = seed.Member(lobby);
        var pick = seed.Pick(member, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
        await db.SaveChangesAsync();
        return new Setup(gameId, lobby.JoinCode, host, member, other, pick);
    }

    private async Task RemovePickAsync(Setup s, Guid userId)
    {
        await using var db = fixture.CreateContext();
        await new RemovePickCommandHandler(db, _mediator, _gameCache).Handle(new RemovePickCommand { Code = s.Code, PickId = s.MemberPick.Id, UserId = userId }, default);
    }

    private async Task<bool> PickIsActiveAsync(Guid pickId)
    {
        await using var db = fixture.CreateContext();
        return (await db.LobbyMemberPicks.SingleAsync(p => p.Id == pickId)).IsActive;
    }

    // --- Remove pick ---

    [Fact]
    public async Task MembersCanRemoveTheirOwnPickBeforePuckDrop()
    {
        var s = await SetupAsync(GameState.Upcoming);

        await RemovePickAsync(s, s.Member.UserId);

        Assert.False(await PickIsActiveAsync(s.MemberPick.Id));
        await _mediator.Received(1).Publish(Arg.Any<PickRemovedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MembersCannotRemoveTheirPickOnceTheGameStarts()
    {
        var s = await SetupAsync(GameState.Live);

        await Assert.ThrowsAsync<ForbiddenException>(() => RemovePickAsync(s, s.Member.UserId));
        Assert.True(await PickIsActiveAsync(s.MemberPick.Id));
    }

    [Fact]
    public async Task MembersCannotRemoveSomeoneElsesPick()
    {
        var s = await SetupAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => RemovePickAsync(s, s.Other.UserId));
        Assert.True(await PickIsActiveAsync(s.MemberPick.Id));
    }

    [Fact]
    public async Task TheHostCanRemoveAnyPickEvenDuringTheGame()
    {
        var s = await SetupAsync(GameState.Live);

        await RemovePickAsync(s, s.Host.UserId);

        Assert.False(await PickIsActiveAsync(s.MemberPick.Id));
    }

    [Fact]
    public async Task SiteAdminsCanRemoveAnyPick()
    {
        var s = await SetupAsync(GameState.Live);
        UserEntity admin;
        await using (var db = fixture.CreateContext())
        {
            admin = await SeedAccounts.AccountAsync(db, isAdmin: true);
        }

        await RemovePickAsync(s, admin.Id);

        Assert.False(await PickIsActiveAsync(s.MemberPick.Id));
    }

    // --- Assign drink ---

    private async Task<Guid> AwardDrinkAsync(LobbyMemberPickEntity pick)
    {
        await using var db = fixture.CreateContext();
        var drink = new DrinkEntity { Id = Guid.NewGuid(), LobbyMemberPickId = pick.Id, EventId = Random.Shared.Next() };
        db.Drinks.Add(drink);
        await db.SaveChangesAsync();
        return drink.Id;
    }

    private async Task AssignAsync(Setup s, Guid assignerUserId, Guid drinkId, Guid recipientMemberId)
    {
        await using var db = fixture.CreateContext();
        await new AssignDrinkCommandHandler(db, TestServices.Mapper(), _mediator).Handle(new AssignDrinkCommand
        {
            Code = s.Code,
            AssignerUserId = assignerUserId,
            DrinkId = drinkId,
            RecipientLobbyMemberId = recipientMemberId
        }, default);
    }

    [Fact]
    public async Task MembersHandOutDrinksTheirPicksEarned()
    {
        var s = await SetupAsync();
        var drinkId = await AwardDrinkAsync(s.MemberPick);

        await AssignAsync(s, s.Member.UserId, drinkId, s.Other.Id);

        await using var check = fixture.CreateContext();
        var drink = await check.Drinks.SingleAsync(d => d.Id == drinkId);
        Assert.Equal(s.Other.Id, drink.RecipientLobbyMemberId);
        Assert.NotNull(drink.Assigned);
    }

    [Fact]
    public async Task NobodyElseCanHandOutYourDrink()
    {
        var s = await SetupAsync();
        var drinkId = await AwardDrinkAsync(s.MemberPick);

        await Assert.ThrowsAsync<NotFoundException>(() => AssignAsync(s, s.Other.UserId, drinkId, s.Other.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => AssignAsync(s, s.Host.UserId, drinkId, s.Other.Id));
    }

    [Fact]
    public async Task ADrinkCanOnlyBeHandedOutOnce()
    {
        var s = await SetupAsync();
        var drinkId = await AwardDrinkAsync(s.MemberPick);
        await AssignAsync(s, s.Member.UserId, drinkId, s.Other.Id);

        await Assert.ThrowsAsync<ValidationException>(() => AssignAsync(s, s.Member.UserId, drinkId, s.Host.Id));
    }

    [Fact]
    public async Task DrinksCanOnlyGoToCurrentMembersOfTheLobby()
    {
        var s = await SetupAsync();
        var drinkId = await AwardDrinkAsync(s.MemberPick);
        LobbyMemberEntity outsider;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (otherLobby, _) = seed.Lobby([s.GameId]);
            outsider = seed.Member(otherLobby);
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<NotFoundException>(() => AssignAsync(s, s.Member.UserId, drinkId, outsider.Id));
    }

    // --- Chat ---

    [Fact]
    public async Task MembersCanSendMessagesButOutsidersCannot()
    {
        var s = await SetupAsync();

        async Task Send(Guid userId, string message)
        {
            await using var db = fixture.CreateContext();
            await new SendLobbyMessageCommandHandler(db, _mediator).Handle(new SendLobbyMessageCommand { Code = s.Code, UserId = userId, Message = message }, default);
        }

        await Send(s.Member.UserId, "Go team");
        await Assert.ThrowsAsync<NotFoundException>(() => Send(Guid.NewGuid(), "Let me in"));

        await using var check = fixture.CreateContext();
        Assert.Equal("Go team", Assert.Single(await check.Messages.Where(m => m.LobbyMember.Lobby.JoinCode == s.Code).ToListAsync()).Message);
    }

    // --- Stale lobbies ---

    [Fact]
    public async Task DeactivatesLobbiesFromBeforeTodaysCutoff()
    {
        var s = await SetupAsync();
        string oldCode;
        await using (var db = fixture.CreateContext())
        {
            var (old, _) = new Seed(db).Lobby([Seed.NewGameId()]);
            oldCode = old.JoinCode;
            await db.SaveChangesAsync();
            await db.Lobbies.Where(l => l.Id == old.Id).ExecuteUpdateAsync(x => x.SetProperty(l => l.Created, DateTime.UtcNow.AddDays(-2)));
        }

        await using (var db = fixture.CreateContext())
        {
            await new DeactivateStaleLobbiesCommandHandler(db, Options.Create(new ApplicationOptions()), NullLogger<DeactivateStaleLobbiesCommandHandler>.Instance)
                .Handle(new DeactivateStaleLobbiesCommand(), default);
        }

        await using var check = fixture.CreateContext();
        Assert.False((await check.Lobbies.SingleAsync(l => l.JoinCode == oldCode)).IsActive);
        Assert.True((await check.Lobbies.SingleAsync(l => l.JoinCode == s.Code)).IsActive);
    }
}
