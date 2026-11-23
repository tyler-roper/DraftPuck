using System.Text.RegularExpressions;
using DraftPuck.Application.Features.Lobbies.Events;

namespace DraftPuck.Tests.Application.Lobbies;

public class LobbyEventTextsTests
{
    // The tokens each event's texts may use (the client fills them from the event), and the ones each must use.
    public static TheoryData<LobbyEventType, string[], string[]> Events => new()
    {
        { LobbyEventType.LobbyCreated, ["name"], ["name"] },
        { LobbyEventType.UserJoined, ["name"], ["name"] },
        { LobbyEventType.UserRejoined, ["name"], ["name"] },
        { LobbyEventType.UserRemoved, ["name"], ["name"] },
        { LobbyEventType.UserPromoted, ["name"], ["name"] },
        { LobbyEventType.UserLeft, ["name"], ["name"] },
        { LobbyEventType.MembersMerged, ["name", "duplicateName"], ["name", "duplicateName"] },
        { LobbyEventType.NewPick, ["name", "playerBadge"], ["name", "playerBadge"] },
        { LobbyEventType.PickRemoved, ["name", "playerBadge"], ["name", "playerBadge"] },
        { LobbyEventType.PickScratched, ["name", "playerBadge"], ["name", "playerBadge"] },
        { LobbyEventType.DrinkAwarded, ["name", "playerBadge"], ["name", "playerBadge"] },
        { LobbyEventType.DrinkAssigned, ["senderName", "recipientName"], ["senderName", "recipientName"] },
        { LobbyEventType.DrinkInvalidated, ["senderName", "recipientName"], ["senderName", "recipientName"] },
        { LobbyEventType.DrinkRevoked, ["name"], ["name"] },
        { LobbyEventType.GoalChanged, ["oldScorer", "newScorer"], ["oldScorer", "newScorer"] },
        { LobbyEventType.GoalRemoved, ["player"], ["player"] }
    };

    [Theory]
    [MemberData(nameof(Events))]
    public void EveryTextUsesOnlyItsEventsTokensAndAllTheOnesItNeeds(LobbyEventType type, string[] allowed, string[] required)
    {
        var texts = LobbyEventTexts.Texts(type);
        Assert.NotEmpty(texts);

        foreach (var text in texts)
        {
            var tokens = Regex.Matches(text, @"\{\{(\w+)\}\}").Select(m => m.Groups[1].Value).ToList();
            Assert.All(tokens, token => Assert.Contains(token, allowed));
            Assert.All(required, token => Assert.Contains(token, tokens));
            // A brace that isn't part of a token would show up in the feed as-is.
            Assert.DoesNotMatch(@"(?<!\{)\{(?!\{)|(?<!\})\}(?!\})", text);
        }
    }

    [Fact]
    public void NoTextIsListedTwice()
    {
        var all = Enum.GetValues<LobbyEventType>().SelectMany(t => LobbyEventTexts.Texts(t).Select(text => (t, text))).ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
    }
}
