using DraftPuck.Application.Features.Lobbies.Events;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>The feed items for bots' picks: one per game, naming the bots unless all of them picked.</summary>
public class BotPickEventTextsTests
{
    private static readonly GameDto Game = TestGames.Create(1, DateTime.UtcNow); // HOM vs AWY

    private static BotGamePicks Picks(string name, int count = 2, bool isReplacement = false)
        => new(new LobbyMemberEntity { Id = Guid.NewGuid(), Name = name, IsBot = true }, count, isReplacement);

    private static List<string> Texts(int botCount, params BotGamePicks[] picks)
        => [.. BotPickEventTexts.Describe(new BotPicksMadePayload(new LobbyEntity(), Game, picks, botCount)).Select(t => t.Text)];

    [Fact]
    public void AllBotsPickingForAGameIsOneItem() =>
        Assert.Equal(["All bots have made their picks for HOM vs AWY."], Texts(3, Picks("A"), Picks("B"), Picks("C")));

    [Fact]
    public void ALobbysOnlyBotIsNamed() =>
        Assert.Equal(["<strong>Botty</strong> made its picks for HOM vs AWY."], Texts(1, Picks("Botty")));

    // E.g. bots added after the others had picked.
    [Fact]
    public void SomeBotsPickingAreNamed()
    {
        Assert.Equal(["<strong>New</strong> made its picks for HOM vs AWY."], Texts(3, Picks("New")));
        Assert.Equal(["<strong>A</strong> and <strong>B</strong> made their picks for HOM vs AWY."], Texts(3, Picks("A"), Picks("B")));
    }

    [Fact]
    public void ReplacementsSayHowMany()
    {
        Assert.Equal(["<strong>Botty</strong> picked a replacement for HOM vs AWY."], Texts(2, Picks("Botty", 1, isReplacement: true)));
        Assert.Equal(["<strong>Botty</strong> picked replacements for HOM vs AWY."], Texts(2, Picks("Botty", 2, isReplacement: true)));
        Assert.Equal(
            ["<strong>A</strong>, <strong>B</strong> and <strong>C</strong> picked replacements for HOM vs AWY."],
            Texts(4, Picks("A", 1, true), Picks("B", 1, true), Picks("C", 1, true)));
    }

    [Fact]
    public void FirstPicksAndReplacementsInTheSameRunAreSeparateItems() =>
        Assert.Equal(
            ["<strong>New</strong> made its picks for HOM vs AWY.", "<strong>Old</strong> picked a replacement for HOM vs AWY."],
            Texts(2, Picks("New"), Picks("Old", 1, isReplacement: true)));

    // Event text is rendered as HTML by the client.
    [Fact]
    public void BotNamesAreEncoded() =>
        Assert.Equal(["<strong>&lt;b&gt;</strong> made its picks for HOM vs AWY."], Texts(1, Picks("<b>")));
}
