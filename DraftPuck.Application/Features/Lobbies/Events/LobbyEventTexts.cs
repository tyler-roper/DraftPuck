namespace DraftPuck.Application.Features.Lobbies.Events;

public static class LobbyEventTexts
{
    private record EventText(string Title, params string[] Texts);

    private static readonly Dictionary<LobbyEventType, EventText> _eventMap = new()
    {
        { LobbyEventType.LobbyCreated, new("Lobby Created",
            "{{name}} created the lobby.",
            "{{name}} opened the bar.",
            "{{name}} dropped the puck on this lobby.",
            "{{name}} is hosting. Behave.",
            "{{name}} built it. Now they'll come.",
            "{{name}} booked the ice.",
            "The commissioner, {{name}}, has entered.")
        },
        { LobbyEventType.UserJoined, new("New Challenger",
            "Welcome, {{name}}!",
            "Nice of you to join us, {{name}}!",
            "{{name}}'s finally here!",
            "Who gave {{name}} the code...?",
            "{{name}} enters the chat.",
            "Why is {{name}} here?",
            "Welcome to the team, {{name}}.",
            "Who is {{name}}?",
            "{{name}} has entered the building.",
            "{{name}} is here. Lock the doors.",
            "{{name}} hopped over the boards.",
            "Fresh legs! {{name}} is on.",
            "{{name}} has been called up from the minors.",
            "Somebody get {{name}} a jersey.",
            "{{name}} signed a one-day contract.",
            "{{name}} just walked in with a 12-pack. Hopefully.",
            "The crowd goes mild for {{name}}.",
            "{{name}} is warming up in the tunnel.",
            "New phone, who's this? Oh, it's {{name}}.",
            "{{name}} cleared waivers.",
            "{{name}} found the lobby. Took a while.",
            "{{name}} is here to drink beer and pick players. And they're all out of beer.",
            "Pull up a stool, {{name}}.")
        },
        { LobbyEventType.UserRejoined, new("User Rejoined",
            "Welcome back, {{name}}!",
            "{{name}} is back from the penalty box.",
            "Look who came crawling back. Hi, {{name}}.",
            "{{name}} is back on the ice.",
            "{{name}} found their phone.",
            "{{name}} returns. The prophecy is fulfilled.",
            "{{name}} is off injured reserve.",
            "{{name}} just couldn't stay away.",
            "Welcome back, {{name}}. Your drinks missed you.")
        },
        { LobbyEventType.UserRemoved, new("User Removed",
            "{{name}} was removed from the lobby.",
            "{{name}} was escorted out by security.",
            "{{name}} got the boot.",
            "{{name}} was traded for a bag of pucks.",
            "{{name}} was sent down to the minors.",
            "{{name}} was ejected from the game.")
        },
        { LobbyEventType.UserPromoted, new("User Promoted",
            "{{name}} was promoted to lobby admin.",
            "{{name}} got the C stitched on.",
            "{{name}} is wearing the A now.",
            "{{name}} was named captain. Power corrupts.",
            "{{name}} has the keys now.",
            "All hail {{name}}, lobby admin.")
        },
        { LobbyEventType.UserLeft, new("User Left",
            "{{name}} left the lobby.",
            "{{name}} hit the showers.",
            "{{name}} has left the building.",
            "{{name}} took an early exit.",
            "{{name}} called a cab.",
            "{{name}} went to the dressing room. Permanently.",
            "{{name}} has retired. Hang the jersey.",
            "{{name}} tapped out.",
            "{{name}} rage-quit. Probably.")
        },
        { LobbyEventType.NewPick, new("New Pick",
            "{{name}} picks {{playerBadge}}",
            "{{name}} is riding with {{playerBadge}}",
            "{{name}} drafts {{playerBadge}}",
            "{{name}} bets the farm on {{playerBadge}}",
            "{{name}} likes {{playerBadge}} tonight.",
            "{{name}} calls their shot: {{playerBadge}}",
            "With the next pick, {{name}} selects {{playerBadge}}",
            "{{name}} put all their chips on {{playerBadge}}",
            "{{name}} trusts {{playerBadge}}. Bold.",
            "{{playerBadge}} is {{name}}'s guy.",
            "{{name}} goes with {{playerBadge}}. Interesting choice.",
            "{{name}} has a feeling about {{playerBadge}}",
            "{{name}} rolls the dice on {{playerBadge}}")
        },
        { LobbyEventType.PickRemoved, new("Pick Removed",
            "{{name}} has un-picked {{playerBadge}}",
            "{{name}} got cold feet on {{playerBadge}}",
            "{{name}} traded away {{playerBadge}}",
            "{{name}} waived {{playerBadge}}",
            "{{name}} changed their mind about {{playerBadge}}",
            "{{playerBadge}} has been benched by {{name}}",
            "{{name}} is having second thoughts. Bye, {{playerBadge}}")
        },
        { LobbyEventType.PickScratched, new("Player Scratched",
            "{{playerBadge}} was scratched, so {{name}}'s pick was removed.",
            "{{playerBadge}} is a healthy scratch, so {{name}}'s pick is gone.",
            "{{playerBadge}} isn't dressing tonight. Sorry, {{name}}.",
            "Bad news, {{name}}: {{playerBadge}} is in the press box.",
            "{{playerBadge}} won't play, so {{name}} gets a do-over.")
        },
        { LobbyEventType.DrinkAwarded, new("Drink Awarded",
            "That's a bingo for {{name}}! {{playerBadge}}",
            "And then suddenly, {{name}} held all the cards. {{playerBadge}}",
            "{{name}} has that look in their eye like they're about to drown someone. {{playerBadge}}",
            "Hope you've been nice to {{name}} lately. {{playerBadge}}",
            "You did it {{name}}! You really did it! {{playerBadge}}",
            "Beginner's luck or pure skill? Doesn't matter to {{name}}! {{playerBadge}}",
            "\"Eeny, meeny, miny, moe...\" - {{name}} {{playerBadge}}",
            "Everyone be on alert, {{name}} is armed and dangerous. {{playerBadge}}",
            "{{name}} with the chance of a lifetime! {{playerBadge}}",
            "Hey {{name}}. Pick your target, take a deep breath, and give out a drink. {{playerBadge}}",
            "That's the sound of buttholes puckering across the world, courtesy of {{name}}. {{playerBadge}}",
            "{{name}} is locked and loaded! {{playerBadge}}",
            "Time to start making enemies, {{name}}. {{playerBadge}}",
            "Who ya got, {{name}}? {{playerBadge}}",
            "He shoots... he scores! {{name}} has been awarded a drink. {{playerBadge}}",
            "Hey {{name}} - give out a drink, bud! {{playerBadge}}",
            "Someone tell {{name}} that their pick just scored. {{playerBadge}}",
            "Light the lamp! {{name}} is buying. Well, giving. {{playerBadge}}",
            "{{name}}'s pick found the back of the net. {{playerBadge}}",
            "Top shelf, where mama keeps the cookies. {{name}} has a drink to give. {{playerBadge}}",
            "{{name}} has a drink burning a hole in their pocket. {{playerBadge}}",
            "Everybody look busy. {{name}} has a drink to give. {{playerBadge}}",
            "Goal! Somebody's about to have a bad time, thanks to {{name}}. {{playerBadge}}",
            "{{name}} called it. {{playerBadge}}",
            "Celly time for {{name}}. {{playerBadge}}",
            "Snipe, celly, sip: {{name}} is up. {{playerBadge}}",
            "The goal horn is for {{name}}. {{playerBadge}}",
            "{{name}} is reloading. {{playerBadge}}",
            "Choose wisely, {{name}}. {{playerBadge}}",
            "Don't make eye contact with {{name}}. {{playerBadge}}",
            "{{name}} cashes in. {{playerBadge}}",
            "That one's in the books for {{name}}. {{playerBadge}}")
        },
        { LobbyEventType.DrinkAssigned, new("Drink Assigned",
            "{{senderName}} is making {{recipientName}} CHUG! CHUG! CHUG!",
            "{{senderName}} gives {{recipientName}} a bird bath!",
            "How nice! {{senderName}} has decided to quench {{recipientName}}'s thirst!",
            "Better bring a bib {{recipientName}}, 'cause {{senderName}} is gettin' you MESSY.",
            "Oof, {{senderName}} is gonna make {{recipientName}} toss some cookies.",
            "That's right {{recipientName}}, show {{senderName}} your chug face.",
            "One beer chug for {{recipientName}}, courtesy of {{senderName}}.",
            "Hope you got your funnel ready {{recipientName}}, 'cause {{senderName}}'s calling you out.",
            "CUT {{recipientName}}'s LIFE INTO PIECES, THIS IS {{senderName}}'s LAST RESORT!",
            "{{senderName}} draws first blood with a vicious attack on {{recipientName}}. But this is far from over...",
            "A right hook by {{senderName}}, DOWN GOES {{recipientName}}!",
            "\"Gluglglguglglgugl\" - {{recipientName}}, chugging a beer from {{senderName}}",
            "{{senderName}} just sunk your battleship, {{recipientName}}.",
            "{{senderName}} is taking {{recipientName}} down a peg.",
            "{{senderName}} absolutely bullying {{recipientName}}. Enjoy the beer, dork!",
            "{{senderName}} sends {{recipientName}} to the sin bin. Drink up.",
            "{{recipientName}}, you've been served. Love, {{senderName}}.",
            "{{senderName}} drops the gloves with {{recipientName}}.",
            "{{senderName}} with a big hit on {{recipientName}}!",
            "{{recipientName}} takes one for the team, courtesy of {{senderName}}.",
            "Bottoms up, {{recipientName}}. {{senderName}} insists.",
            "{{senderName}} just put {{recipientName}} on the injury report.",
            "Last call for {{recipientName}}, says {{senderName}}.",
            "{{senderName}} would like to buy {{recipientName}} a drink. And then watch them drink it.",
            "{{recipientName}} got caught with their head down. {{senderName}} lays the hit.",
            "{{senderName}} → {{recipientName}}. Nothing personal. Okay, a little personal.",
            "Two minutes for drinking: {{recipientName}}. Called by {{senderName}}.",
            "{{recipientName}}, {{senderName}} says hydrate. With beer.",
            "{{senderName}} slides one down the bar to {{recipientName}}.",
            "{{recipientName}} has been chosen by {{senderName}}. It's an honor. Drink.")
        },
        { LobbyEventType.DrinkInvalidated, new("Drink Update",
            "Hope you didn't chug yet, {{recipientName}}, because {{senderName}}'s drink just got revoked.",
            "Put it down, {{recipientName}}! {{senderName}}'s drink was taken back.",
            "Plot twist, {{recipientName}}: {{senderName}}'s drink no longer counts.",
            "Spit it out, {{recipientName}}. The goal behind {{senderName}}'s drink was changed.")
        },
        { LobbyEventType.DrinkRevoked, new("Drink Revoked",
            "{{name}} has had a drink revoked due to a scoring change.",
            "Not so fast, {{name}}. A scoring change took your drink.",
            "{{name}}'s drink got waved off.",
            "The refs giveth, the refs taketh away, {{name}}.")
        },
        { LobbyEventType.GoalChanged, new("Goal Change",
            "{{oldScorer}}'s goal has been changed to {{newScorer}}",
            "After review, that's {{newScorer}}'s goal, not {{oldScorer}}'s.",
            "The scorer's been changed: {{oldScorer}} → {{newScorer}}.",
            "{{newScorer}} gets the credit; {{oldScorer}} gets a \"nice try\".")
        },
        // {{duplicateName}} is the removed spot's name, filled in on the server (see LobbyEventHandler).
        { LobbyEventType.MembersMerged, new("Spot Reclaimed",
            "{{duplicateName}} was {{name}} all along. Back in their old spot.",
            "{{duplicateName}} is really {{name}}, on a new device.",
            "Plot twist: {{duplicateName}} was {{name}}. One spot, one person.",
            "{{name}} lost their phone and came back as {{duplicateName}}. Back to {{name}} it is.")
        },
        { LobbyEventType.GoalRemoved, new("Goal Removed",
            "{{player}}'s goal has been called back.",
            "No goal! {{player}}'s goal came off the board.",
            "The refs waved off {{player}}'s goal.",
            "After review: no goal for {{player}}.",
            "{{player}}'s goal has been taken back. Coach's challenge?")
        }
    };
    /// <summary>Every text an event type can have (one is picked at random), or none if it has no list.</summary>
    public static IReadOnlyList<string> Texts(LobbyEventType eventType) =>
        _eventMap.TryGetValue(eventType, out var eventText) ? eventText.Texts : [];

    public static string GetTitle(LobbyEventType eventType)
    {
        return _eventMap.TryGetValue(eventType, out var eventText) ? eventText.Title : "No title";
    }

    public static string GetText(LobbyEventType eventType)
    {
        return _eventMap.TryGetValue(eventType, out var eventText) ? Random(eventText.Texts) : "No text generated";
    }

    private static string Random(string[] strings)
    {
        return strings == null || strings.Length == 0 ? string.Empty : strings[global::System.Random.Shared.Next(strings.Length)];
    }
}