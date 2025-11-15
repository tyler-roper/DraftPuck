namespace DraftPuck.Application.Features.Lobbies;

public static class LobbyEventTexts
{
    private static readonly Random _random = new();
    private record EventText(string Title, params string[] Texts);

    private static readonly Dictionary<LobbyEventType, EventText> _eventMap = new()
    {
        { LobbyEventType.LobbyCreated, new("Lobby Created",
            "{{name}} created the lobby.")
        },
        { LobbyEventType.UserJoined, new("New Challenger",
            "Welcome, {{name}}!",
            "Nice of you to join us, {{name}}!",
            "{{name}}'s finally here!",
            "Who gave {{name}} the code...?",
            "{{name}} enters the chat.",
            "Why is {{name}} here?",
            "Welcome to the team, {{name}}.",
            "Who is {{name}}?")
        },
        { LobbyEventType.UserRejoined, new("User Rejoined",
            "Welcome back, {{name}}!")
        },
        { LobbyEventType.UserRemoved, new("User Removed",
            "{{name}} was removed from the lobby.")
        },
        { LobbyEventType.UserPromoted, new("User Promoted",
            "{{name}} was promoted to lobby admin.")
        },
        { LobbyEventType.UserLeft, new("User Left",
            "{{name}} left the lobby.")
        },
        { LobbyEventType.NewPick, new("New Pick",
            "{{name}} picks {{playerBadge}}")
        },
        { LobbyEventType.PickRemoved, new("Pick Removed",
            "{{name}} has un-picked {{playerBadge}}")
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
            "Someone tell {{name}} that their pick just scored.")
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
            "{{senderName}} absolutely bullying {{recipientName}}. Enjoy the beer, dork!")
        },
        { LobbyEventType.DrinkInvalidated, new("Drink Update",
            "Hope you didn't chug yet, {{recipientName}}, because {{senderName}}'s drink just got revoked.")
        },
        { LobbyEventType.DrinkRevoked, new("Drink Revoked",
            "{{name}} has had a drink revoked due to a scoring change.")
        },
        { LobbyEventType.GoalChanged, new("Goal Change",
            "{{oldScorer}}'s goal has been changed to {{newScorer}}")
        },
        { LobbyEventType.GoalRemoved, new("Goal Removed",
            "{{player}}'s goal has been called back.")
        }
    };
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
        return strings == null || strings.Length == 0 ? string.Empty : strings[_random.Next(strings.Length)];
    }
}