using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Lobbies;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Management;
using DraftPuck.Application.Features.Lobbies.Members;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>Creating, joining, leaving and renaming in lobbies, and removing members.</summary>
[Collection(DatabaseCollection.Name)]
public class LobbyMembershipTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private async Task<LobbyDto> CreateAsync(Guid creatorId, string name = "Host", params BotDto[] bots)
    {
        await using var db = fixture.CreateContext();
        return await new CreateLobbyCommandHandler(db, TestServices.Mapper(), _mediator).Handle(new CreateLobbyCommand
        {
            CreatorUserId = creatorId,
            Name = name,
            PicksPerTeam = 2,
            GameIds = [Seed.NewGameId()],
            Bots = [.. bots]
        }, default);
    }

    private async Task JoinAsync(string code, Guid? userId, string name = "Joiner", bool isBot = false, BotPickStyle? style = null)
    {
        await using var db = fixture.CreateContext();
        await new JoinLobbyCommandHandler(db, TestServices.Mapper(), _mediator).Handle(
            new JoinLobbyCommand { Code = code, UserId = userId, Name = name, IsBot = isBot, BotPickStyle = style }, default);
    }

    private async Task LeaveAsync(string code, Guid userId)
    {
        await using var db = fixture.CreateContext();
        await new LeaveLobbyCommandHandler(db, _mediator).Handle(new LeaveLobbyCommand { Code = code, UserId = userId }, default);
    }

    private async Task RemoveAsync(string code, Guid requesterUserId, Guid memberId)
    {
        await using var db = fixture.CreateContext();
        await new RemoveLobbyMemberCommandHandler(db, _mediator).Handle(
            new RemoveLobbyMemberCommand { Code = code, RequesterUserId = requesterUserId, LobbyMemberId = memberId }, default);
    }

    private async Task RenameAsync(string code, Guid userId, string newName)
    {
        await using var db = fixture.CreateContext();
        await new ChangeNameCommandHandler(db, _mediator).Handle(new ChangeNameCommand { Code = code, UserId = userId, NewName = newName }, default);
    }

    private async Task<UserEntity> UserAsync()
    {
        await using var db = fixture.CreateContext();
        var user = new Seed(db).User();
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<List<LobbyMemberEntity>> MembersAsync(string code)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyMembers.Include(m => m.LobbyMemberPicks).Where(m => m.Lobby.JoinCode == code).ToListAsync();
    }

    // --- Names ---

    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("Tab\there")]
    [InlineData("   ")]
    [InlineData("This name is far too long to fit in fifty characters!")]
    public void RejectsUnsafeOrInvalidNames(string name)
    {
        Assert.Throws<ValidationException>(() => LobbyMemberNames.Normalize(name));
    }

    [Fact]
    public void TrimsNames()
    {
        Assert.Equal("Sam", LobbyMemberNames.Normalize("  Sam  "));
    }

    // --- Create ---

    [Fact]
    public async Task CreatingALobbyStartsItsFeedWithWhoCreatedIt()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Host");
        var created = (LobbyCreatedNotification)_mediator.ReceivedCalls()
            .Select(c => c.GetArguments()[0]).Single(a => a is LobbyCreatedNotification)!;

        await using (var db = fixture.CreateContext())
        {
            await new LobbyEventHandler(db, new LobbyEventWriter(db, Substitute.For<IMediator>())).Handle(created, default);
        }

        await using var check = fixture.CreateContext();
        var hostMember = (await MembersAsync(lobby.JoinCode)).Single(m => m.UserId == host.Id);
        var feedEvent = await check.LobbyEvents.SingleAsync(e => e.LobbyId == lobby.Id && e.LobbyEventType == LobbyEventType.LobbyCreated);
        Assert.Equal((hostMember.Id, "Lobby Created"), (feedEvent.LobbyMemberId!.Value, feedEvent.Title));
        Assert.Contains("{{name}}", feedEvent.Text);
    }

    [Fact]
    public async Task CreatingALobbyAddsTheHostAndBots()
    {
        var host = await UserAsync();

        var lobby = await CreateAsync(host.Id, "Host", new BotDto { Nickname = "Robo", PickStyle = BotPickStyle.Good });

        Assert.Matches("^[A-Z]{4}$", lobby.JoinCode);
        var members = await MembersAsync(lobby.JoinCode);
        Assert.Contains(members, m => m.UserId == host.Id && m.Name == "Host" && !m.IsBot);
        var bot = Assert.Single(members, m => m.IsBot);
        Assert.Equal(BotPickStyle.Good, bot.BotPickStyle);
        Assert.NotEqual(host.Id, bot.UserId);
    }

    [Fact]
    public async Task CreatingALobbyRejectsMarkupInBotNames()
    {
        var host = await UserAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateAsync(host.Id, "Host", new BotDto { Nickname = "<b>bot</b>", PickStyle = BotPickStyle.Best }));
    }

    // --- Join ---

    [Fact]
    public async Task JoiningAddsAMember()
    {
        var lobby = await CreateAsync((await UserAsync()).Id);
        var joiner = await UserAsync();

        await JoinAsync(lobby.JoinCode, joiner.Id, "Sam");

        Assert.Contains(await MembersAsync(lobby.JoinCode), m => m.UserId == joiner.Id && m.Name == "Sam");
    }

    [Fact]
    public async Task JoiningRequiresAUserAndAnExistingLobby()
    {
        var lobby = await CreateAsync((await UserAsync()).Id);

        await Assert.ThrowsAsync<UnauthorizedException>(() => JoinAsync(lobby.JoinCode, userId: null));
        await Assert.ThrowsAsync<NotFoundException>(() => JoinAsync("ZZZZ", Guid.NewGuid()));
    }

    [Fact]
    public async Task JoiningAgainUpdatesTheNameInsteadOfAddingAMember()
    {
        var lobby = await CreateAsync((await UserAsync()).Id);
        var joiner = await UserAsync();

        await JoinAsync(lobby.JoinCode, joiner.Id, "Sam");
        await JoinAsync(lobby.JoinCode, joiner.Id, "Samantha");

        Assert.Equal("Samantha", Assert.Single(await MembersAsync(lobby.JoinCode), m => m.UserId == joiner.Id).Name);
    }

    [Fact]
    public async Task OnlyTheHostCanAddBots()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => JoinAsync(lobby.JoinCode, member.Id, "Bot", isBot: true, BotPickStyle.Best));
        await Assert.ThrowsAsync<ValidationException>(() => JoinAsync(lobby.JoinCode, host.Id, "Bot", isBot: true, style: null));

        await JoinAsync(lobby.JoinCode, host.Id, "Bot", isBot: true, BotPickStyle.Worst);
        var bot = Assert.Single(await MembersAsync(lobby.JoinCode), m => m.IsBot);
        Assert.NotEqual(host.Id, bot.UserId); // the bot gets its own user; it doesn't replace the host's membership
        Assert.Contains(await MembersAsync(lobby.JoinCode), m => m.UserId == host.Id && !m.IsBot);
    }

    [Fact]
    public async Task RejoiningRestoresPicksNobodyElseTookSince()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var gameId = lobby.GameIds.Single();
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id);

        Guid keptPickId, takenPickId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var memberEntity = await db.LobbyMembers.SingleAsync(m => m.Lobby.JoinCode == lobby.JoinCode && m.UserId == member.Id);
            keptPickId = seed.Pick(memberEntity, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0)).Id;
            takenPickId = seed.Pick(memberEntity, gameId, TestGames.AwayTeamId, TestGames.AwayPlayer(0)).Id;
            await db.SaveChangesAsync();
        }

        await LeaveAsync(lobby.JoinCode, member.Id);
        await using (var db = fixture.CreateContext())
        {
            var hostMember = await db.LobbyMembers.SingleAsync(m => m.Lobby.JoinCode == lobby.JoinCode && m.UserId == host.Id);
            new Seed(db).Pick(hostMember, gameId, TestGames.AwayTeamId, TestGames.AwayPlayer(0)); // the host takes the player meanwhile
            await db.SaveChangesAsync();
        }
        await JoinAsync(lobby.JoinCode, member.Id, "Back again");

        await using var check = fixture.CreateContext();
        Assert.False((await check.LobbyMembers.SingleAsync(m => m.Lobby.JoinCode == lobby.JoinCode && m.UserId == member.Id)).IsRemoved);
        Assert.True((await check.LobbyMemberPicks.SingleAsync(p => p.Id == keptPickId)).IsActive);
        Assert.False((await check.LobbyMemberPicks.SingleAsync(p => p.Id == takenPickId)).IsActive);
    }

    // --- Leave ---

    [Fact]
    public async Task LeavingRemovesTheMemberAndTheirPicks()
    {
        var lobby = await CreateAsync((await UserAsync()).Id);
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id);
        await using (var db = fixture.CreateContext())
        {
            var memberEntity = await db.LobbyMembers.SingleAsync(m => m.Lobby.JoinCode == lobby.JoinCode && m.UserId == member.Id);
            new Seed(db).Pick(memberEntity, lobby.GameIds.Single(), TestGames.HomeTeamId, TestGames.HomePlayer(0));
            await db.SaveChangesAsync();
        }

        await LeaveAsync(lobby.JoinCode, member.Id);

        var left = Assert.Single(await MembersAsync(lobby.JoinCode), m => m.UserId == member.Id);
        Assert.True(left.IsRemoved);
        Assert.All(left.LobbyMemberPicks, p => Assert.False(p.IsActive));
    }

    [Fact]
    public async Task WhenTheHostLeavesTheLongestServingHumanTakesOver()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Host", new BotDto { Nickname = "Robo", PickStyle = BotPickStyle.Best });
        var first = await UserAsync();
        var second = await UserAsync();
        await JoinAsync(lobby.JoinCode, first.Id, "First");
        await JoinAsync(lobby.JoinCode, second.Id, "Second");
        await using (var db = fixture.CreateContext())
        {
            // Make the join order unambiguous.
            await db.LobbyMembers.Where(m => m.UserId == first.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Joined, DateTime.UtcNow.AddMinutes(-10)));
        }

        await LeaveAsync(lobby.JoinCode, host.Id);

        await using var check = fixture.CreateContext();
        Assert.Equal(first.Id, (await check.Lobbies.SingleAsync(l => l.JoinCode == lobby.JoinCode)).CreatedBy);
    }

    [Fact]
    public async Task WhenTheLastHumanLeavesTheLobbyIsDeleted()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Host", new BotDto { Nickname = "Robo", PickStyle = BotPickStyle.Best });

        await LeaveAsync(lobby.JoinCode, host.Id);

        await using var check = fixture.CreateContext();
        Assert.False(await check.Lobbies.AnyAsync(l => l.JoinCode == lobby.JoinCode));
    }

    // --- Remove member ---

    [Fact]
    public async Task OnlyTheHostCanRemoveMembersAndTheHostCannotBeRemoved()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id);
        var members = await MembersAsync(lobby.JoinCode);
        var hostMember = members.Single(m => m.UserId == host.Id);
        var memberEntity = members.Single(m => m.UserId == member.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => RemoveAsync(lobby.JoinCode, member.Id, hostMember.Id));
        await Assert.ThrowsAsync<BadRequestException>(() => RemoveAsync(lobby.JoinCode, host.Id, hostMember.Id));

        await RemoveAsync(lobby.JoinCode, host.Id, memberEntity.Id);
        Assert.True((await MembersAsync(lobby.JoinCode)).Single(m => m.Id == memberEntity.Id).IsRemoved);
    }

    // --- Merge members ---

    private async Task MergeAsync(string code, Guid requesterUserId, Guid originalMemberId, Guid duplicateMemberId)
    {
        await using var db = fixture.CreateContext();
        await new MergeLobbyMembersCommandHandler(db, _mediator).Handle(new MergeLobbyMembersCommand
        {
            Code = code,
            RequesterUserId = requesterUserId,
            OriginalMemberId = originalMemberId,
            DuplicateMemberId = duplicateMemberId
        }, default);
    }

    /// <summary>A lobby where one guest joined as "Sam" (and picked), lost their session, and joined again as "Sam 2" (and picked).</summary>
    private async Task<(LobbyDto Lobby, UserEntity Host, UserEntity OldLogin, UserEntity NewLogin, Guid OriginalId, Guid DuplicateId)> DuplicatedGuestAsync()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var oldLogin = await UserAsync();
        var newLogin = await UserAsync();
        await JoinAsync(lobby.JoinCode, oldLogin.Id, "Sam");
        await JoinAsync(lobby.JoinCode, newLogin.Id, "Sam 2");

        await using var db = fixture.CreateContext();
        var members = await db.LobbyMembers.Where(m => m.Lobby.JoinCode == lobby.JoinCode).ToListAsync();
        var original = members.Single(m => m.UserId == oldLogin.Id);
        var duplicate = members.Single(m => m.UserId == newLogin.Id);
        var seed = new Seed(db);
        seed.Pick(original, lobby.GameIds.Single(), TestGames.HomeTeamId, TestGames.HomePlayer(0));
        seed.Pick(duplicate, lobby.GameIds.Single(), TestGames.AwayTeamId, TestGames.AwayPlayer(0));
        await db.SaveChangesAsync();

        return (lobby, host, oldLogin, newLogin, original.Id, duplicate.Id);
    }

    [Fact]
    public async Task MergingGivesTheOriginalSpotToTheNewLoginAndRemovesTheDuplicate()
    {
        var (lobby, host, oldLogin, newLogin, originalId, duplicateId) = await DuplicatedGuestAsync();

        await MergeAsync(lobby.JoinCode, host.Id, originalId, duplicateId);

        var members = await MembersAsync(lobby.JoinCode);
        var original = members.Single(m => m.Id == originalId);
        Assert.Equal((newLogin.Id, "Sam", false), (original.UserId, original.Name, original.IsRemoved));
        Assert.All(original.LobbyMemberPicks, p => Assert.True(p.IsActive));

        // The duplicate, and anything it did, is dropped; it's left with the login the guest lost.
        var duplicate = members.Single(m => m.Id == duplicateId);
        Assert.Equal((oldLogin.Id, true), (duplicate.UserId, duplicate.IsRemoved));
        Assert.All(duplicate.LobbyMemberPicks, p => Assert.False(p.IsActive));

        var merged = _mediator.ReceivedCalls().Select(c => c.GetArguments()[0]).OfType<MembersMergedNotification>().Single();
        Assert.Equal((originalId, duplicateId), (merged.Data.Original.Id, merged.Data.Duplicate.Id));
    }

    [Fact]
    public async Task AfterAMergeTheNewLoginIsTheOriginalSpot()
    {
        var (lobby, host, _, newLogin, originalId, duplicateId) = await DuplicatedGuestAsync();
        await MergeAsync(lobby.JoinCode, host.Id, originalId, duplicateId);

        // The new device opening the lobby again (it joins under the name it has) lands in the original spot.
        await JoinAsync(lobby.JoinCode, newLogin.Id, "Sam");

        var active = (await MembersAsync(lobby.JoinCode)).Where(m => m.UserId == newLogin.Id && !m.IsRemoved);
        Assert.Equal(originalId, Assert.Single(active).Id);
    }

    [Fact]
    public async Task OnlyTheHostMergesAndOnlyTwoDifferentCurrentHumanSpots()
    {
        var (lobby, host, _, newLogin, originalId, duplicateId) = await DuplicatedGuestAsync();
        Guid hostMemberId, botId, removedId;
        await using (var db = fixture.CreateContext())
        {
            var entity = await db.Lobbies.SingleAsync(l => l.JoinCode == lobby.JoinCode);
            hostMemberId = (await db.LobbyMembers.SingleAsync(m => m.LobbyId == entity.Id && m.UserId == host.Id)).Id;
            var seed = new Seed(db);
            var bot = seed.Member(entity, seed.User(isBot: true));
            bot.IsBot = true;
            var removed = seed.Member(entity);
            removed.Name = "Gone";
            removed.IsRemoved = true;
            await db.SaveChangesAsync();
            (botId, removedId) = (bot.Id, removed.Id);
        }

        await Assert.ThrowsAsync<ForbiddenException>(() => MergeAsync(lobby.JoinCode, newLogin.Id, originalId, duplicateId));
        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, originalId, originalId));
        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, botId, duplicateId));
        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, originalId, botId));
        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, hostMemberId, duplicateId));
        await Assert.ThrowsAsync<NotFoundException>(() => MergeAsync(lobby.JoinCode, host.Id, removedId, duplicateId));
        await Assert.ThrowsAsync<NotFoundException>(() => MergeAsync(lobby.JoinCode, host.Id, originalId, Guid.NewGuid()));

        Assert.All(await MembersAsync(lobby.JoinCode), m => Assert.Equal(m.Id == removedId, m.IsRemoved));
    }

    [Fact]
    public async Task ASignedInPlayersSpotNeverMoves()
    {
        var (lobby, host, oldLogin, _, originalId, duplicateId) = await DuplicatedGuestAsync();
        await using (var db = fixture.CreateContext())
        {
            await db.Users.Where(u => u.Id == oldLogin.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsGuest, false));
        }

        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, originalId, duplicateId));

        Assert.Equal(oldLogin.Id, (await MembersAsync(lobby.JoinCode)).Single(m => m.Id == originalId).UserId);
    }

    [Fact]
    public async Task AnEndedLobbyCannotBeMerged()
    {
        var (lobby, host, _, _, originalId, duplicateId) = await DuplicatedGuestAsync();
        await using (var db = fixture.CreateContext())
        {
            await db.Lobbies.Where(l => l.JoinCode == lobby.JoinCode).ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false));
        }

        await Assert.ThrowsAsync<BadRequestException>(() => MergeAsync(lobby.JoinCode, host.Id, originalId, duplicateId));
    }

    [Fact]
    public async Task TheFeedSaysWhoTheDuplicateReallyWas()
    {
        var (lobby, host, _, _, originalId, duplicateId) = await DuplicatedGuestAsync();
        await MergeAsync(lobby.JoinCode, host.Id, originalId, duplicateId);
        var merged = _mediator.ReceivedCalls().Select(c => c.GetArguments()[0]).OfType<MembersMergedNotification>().Single();

        await using (var db = fixture.CreateContext())
        {
            await new LobbyEventHandler(db, new LobbyEventWriter(db, Substitute.For<IMediator>())).Handle(merged, default);
        }

        await using var check = fixture.CreateContext();
        var feedEvent = await check.LobbyEvents.SingleAsync(e => e.LobbyId == lobby.Id && e.LobbyEventType == LobbyEventType.MembersMerged);
        Assert.Equal((originalId, duplicateId), (feedEvent.LobbyMemberId!.Value, feedEvent.LobbyMember2Id!.Value));
        // The original spot's name is filled in by the client; the removed duplicate's is written in.
        Assert.Contains("{{name}}", feedEvent.Text);
        Assert.Contains("<strong>Sam 2</strong>", feedEvent.Text);
        Assert.DoesNotContain("{{duplicateName}}", feedEvent.Text);
    }

    // --- Names on join ---

    [Fact]
    public async Task JoiningRejectsANameAnotherMemberUses()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Taken");
        var joiner = await UserAsync();

        await Assert.ThrowsAsync<ValidationException>(() => JoinAsync(lobby.JoinCode, joiner.Id, "TAKEN"));
        Assert.DoesNotContain(await MembersAsync(lobby.JoinCode), m => m.UserId == joiner.Id);
    }

    [Fact]
    public async Task JoiningAgainUnderYourOwnNameIsFine()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var joiner = await UserAsync();

        await JoinAsync(lobby.JoinCode, joiner.Id, "Sam");
        await JoinAsync(lobby.JoinCode, joiner.Id, "sam");

        Assert.Single(await MembersAsync(lobby.JoinCode), m => m.UserId == joiner.Id);
    }

    [Fact]
    public async Task ARemovedMembersNameIsFreeAgain()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id);
        var leaver = await UserAsync();
        await JoinAsync(lobby.JoinCode, leaver.Id, "Sam");
        await LeaveAsync(lobby.JoinCode, leaver.Id);

        var newcomer = await UserAsync();
        await JoinAsync(lobby.JoinCode, newcomer.Id, "Sam");

        Assert.Contains(await MembersAsync(lobby.JoinCode), m => m.UserId == newcomer.Id && m.Name == "Sam");
    }

    [Fact]
    public async Task BotsNeedAnUnusedNameAndARealPickStyle()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Host");

        await Assert.ThrowsAsync<ValidationException>(() => JoinAsync(lobby.JoinCode, host.Id, "host", isBot: true, style: BotPickStyle.Best));
        await Assert.ThrowsAsync<ValidationException>(() => JoinAsync(lobby.JoinCode, host.Id, "Robo", isBot: true, style: null));
        await Assert.ThrowsAsync<ValidationException>(() => JoinAsync(lobby.JoinCode, host.Id, "Robo", isBot: true, style: (BotPickStyle)99));

        await JoinAsync(lobby.JoinCode, host.Id, "Robo", isBot: true, style: BotPickStyle.Random);
        Assert.Contains(await MembersAsync(lobby.JoinCode), m => m.IsBot && m.Name == "Robo");
    }

    [Fact]
    public async Task NewLobbiesNeedDistinctNamesAndRealPickStyles()
    {
        var host = await UserAsync();

        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(host.Id, "Host", new BotDto { Nickname = "HOST", PickStyle = BotPickStyle.Best }));
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(host.Id, "Host",
            new BotDto { Nickname = "Robo", PickStyle = BotPickStyle.Best }, new BotDto { Nickname = "robo", PickStyle = BotPickStyle.Bad }));
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(host.Id, "Host", new BotDto { Nickname = "Robo", PickStyle = (BotPickStyle)99 }));
    }

    // --- Rename ---

    [Fact]
    public async Task RenamingRejectsANameAnotherMemberUses()
    {
        var host = await UserAsync();
        var lobby = await CreateAsync(host.Id, "Taken");
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id, "Sam");

        await Assert.ThrowsAsync<ValidationException>(() => RenameAsync(lobby.JoinCode, member.Id, "TAKEN"));
        await Assert.ThrowsAsync<ValidationException>(() => RenameAsync(lobby.JoinCode, member.Id, "<script>"));

        await RenameAsync(lobby.JoinCode, member.Id, "Samwise");
        Assert.Contains(await MembersAsync(lobby.JoinCode), m => m.UserId == member.Id && m.Name == "Samwise");
    }

    [Fact]
    public async Task RemovedMembersCannotRename()
    {
        var lobby = await CreateAsync((await UserAsync()).Id);
        var member = await UserAsync();
        await JoinAsync(lobby.JoinCode, member.Id, "Sam");
        await LeaveAsync(lobby.JoinCode, member.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => RenameAsync(lobby.JoinCode, member.Id, "Ghost"));
    }
}
