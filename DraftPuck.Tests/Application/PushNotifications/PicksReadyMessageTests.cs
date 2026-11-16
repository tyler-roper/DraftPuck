using DraftPuck.Application.Features.PushNotifications;

namespace DraftPuck.Tests.Application.PushNotifications;

public class PicksReadyMessageTests
{
    private static readonly DateTime Start = new(2026, 11, 16, 1, 0, 0, DateTimeKind.Utc);

    private static GameDto Game(string away, string home, int minutesAfterStart = 0)
    {
        var game = TestGames.Create(Seed.NewGameId(), Start.AddMinutes(minutesAfterStart));
        game.AwayTeam.Abbreviation = away;
        game.HomeTeam.Abbreviation = home;
        return game;
    }

    [Fact]
    public void NamesOneGame() =>
        Assert.Equal("Picks are open for EDM @ PHI.", PicksReadyMessage.Body([Game("EDM", "PHI")]));

    [Fact]
    public void NamesTwoGames() =>
        Assert.Equal("Picks are open for EDM @ PHI and FLA @ DAL.", PicksReadyMessage.Body([Game("FLA", "DAL"), Game("EDM", "PHI")]));

    [Fact]
    public void NamesThreeGamesInStartOrder() =>
        Assert.Equal("Picks are open for VAN @ VGK, CGY @ ANA and FLA @ DAL.",
            PicksReadyMessage.Body([Game("FLA", "DAL", 10), Game("CGY", "ANA", 5), Game("VAN", "VGK")]));

    [Fact]
    public void CountsTheRestPastThree() =>
        Assert.Equal("Picks are open for CGY @ ANA, EDM @ PHI and 3 more.",
            PicksReadyMessage.Body([Game("EDM", "PHI"), Game("CGY", "ANA"), Game("FLA", "DAL"), Game("NSH", "SJS"), Game("VAN", "VGK")]));

    [Fact]
    public void NamesAGameOnceEvenIfListedTwice()
    {
        var game = Game("EDM", "PHI");
        Assert.Equal("Picks are open for EDM @ PHI.", PicksReadyMessage.Body([game, game]));
    }
}
