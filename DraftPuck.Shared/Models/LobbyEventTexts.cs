namespace DraftPuck.Shared.Models;

public static class LobbyEventTexts
{
    private static readonly Random _random = new();

    public static string GetTitle(LobbyEventType eventType)
    {
        return eventType == LobbyEventType.LobbyCreated
            ? LobbyCreated.Title
            : eventType == LobbyEventType.UserJoined
            ? UserJoined.Title
            : eventType == LobbyEventType.NewPick
            ? NewPick.Title
            : eventType == LobbyEventType.PickRemoved
            ? PickRemoved.Title
            : eventType == LobbyEventType.DrinkAwarded
            ? DrinkAwarded.Title
            : eventType == LobbyEventType.DrinkAssigned
            ? DrinkAssigned.Title
            : eventType == LobbyEventType.DrinkInvalidated
            ? DrinkInvalidated.Title
            : eventType == LobbyEventType.DrinkRevoked
            ? DrinkRevoked.Title
            : eventType == LobbyEventType.GoalChanged
            ? GoalChanged.Title
            : eventType == LobbyEventType.GoalRemoved
            ? GoalRemoved.Title
            : eventType == LobbyEventType.UserRejoined
            ? UserRejoined.Title
            : eventType == LobbyEventType.UserRemoved ? UserRemoved.Title : "No title";
    }

    public static string GetText(LobbyEventType eventType)
    {
        return eventType == LobbyEventType.LobbyCreated
            ? Random(LobbyCreated.Texts)
            : eventType == LobbyEventType.UserJoined
            ? Random(UserJoined.Texts)
            : eventType == LobbyEventType.NewPick
            ? Random(NewPick.Texts)
            : eventType == LobbyEventType.PickRemoved
            ? Random(PickRemoved.Texts)
            : eventType == LobbyEventType.DrinkAwarded
            ? Random(DrinkAwarded.Texts)
            : eventType == LobbyEventType.DrinkAssigned
            ? Random(DrinkAssigned.Texts)
            : eventType == LobbyEventType.DrinkInvalidated
            ? Random(DrinkInvalidated.Texts)
            : eventType == LobbyEventType.DrinkRevoked
            ? Random(DrinkRevoked.Texts)
            : eventType == LobbyEventType.GoalChanged
            ? Random(GoalChanged.Texts)
            : eventType == LobbyEventType.GoalRemoved
            ? Random(GoalRemoved.Texts)
            : eventType == LobbyEventType.UserRejoined
            ? Random(UserRejoined.Texts)
            : eventType == LobbyEventType.UserRemoved ? Random(UserRemoved.Texts) : "No text generated";
    }

    private static string Random(List<string> strings)
    {
        return strings[_random.Next(strings.Count)];
    }

    private static class LobbyCreated
    {
        public static string Title = "Lobby Created";
        public static List<string> Texts = new()
        {
            "{{name}} created the lobby."
        };
    }

    private static class UserJoined
    {
        public static string Title = "New Challenger";
        public static List<string> Texts = new()
        {
            "Welcome, {{name}}!",
            "Nice of you to join us, {{name}}!",
            "{{name}}'s finally here!",
            "Who gave {{name}} the code...?",
            "{{name}} enters the chat.",
            "Why is {{name}} here?",
            "Welcome to the team, {{name}}.",
            "Who is {{name}}?"
        };
    }

    private static class UserRejoined
    {
        public static string Title = "User Rejoined";
        public static List<string> Texts = new()
        {
            "Welcome back, {{name}}!"
        };
    }

    private static class UserRemoved
    {
        public static string Title = "User Removed";
        public static List<string> Texts = new()
        {
            "{{name}} was removed from the lobby."
        };
    }

    private static class NewPick
    {
        public static string Title = "New Pick";
        public static List<string> Texts = new()
        {
            "{{name}} picks {{playerBadge}}"
        };
    }

    private static class PickRemoved
    {
        public static string Title = "Pick Removed";
        public static List<string> Texts = new()
        {
            "{{name}} has un-picked {{playerBadge}}"
        };
    }

    private static class DrinkAwarded
    {
        public static string Title = "Drink Awarded";
        public static List<string> Texts = new()
        {
            "That's a bingo for {{name}}! {{playerBadge}}",
            "And then suddenly, {{name}} held all the cards. {{playerBadge}}",
            "{{name}} has that look in their eye like they're about to drown someone. {{playerBadge}}",
            "Hope you've been nice to {{name}} lately. {{playerBadge}}",
            "You did it {{name}}! You really did it! {{playerBadge}}",
            "Beginner's luck or pure skill? Doesn't matter to {{name}}! {{playerBadge}}",
            "\"Eeny, meeny, miny, moe...\" - {{name}} {{playerBadge}}",
            //"Someone boutta hop up on {{name}}'s dick and do a full split. {{playerBadge}}",
            "Everyone be on alert, {{name}} is armed and dangerous. {{playerBadge}}",
            "{{name}} with the chance of a lifetime! {{playerBadge}}",
            "Hey {{name}}. Pick your target, take a deep breath, and give out a drink. {{playerBadge}}",
            "That's the sound of buttholes puckering across the world, courtesy of {{name}}. {{playerBadge}}",
            "{{name}} is locked and loaded! {{playerBadge}}",
            "Time to start making enemies, {{name}}. {{playerBadge}}",
            "Who ya got, {{name}}? {{playerBadge}}",
            "He shoots... he scores! {{name}} has been awarded a drink. {{playerBadge}}",
            "Hey {{name}} - give out a drink, bud! {{playerBadge}}",
            "Someone tell {{name}} that their pick just scored."
        };
    }

    private static class DrinkAssigned
    {
        public static string Title = "Drink Assigned";
        public static List<string> Texts = new()
        {
            //"\"Hey {{recipientName}}, go fuck a goat!\" - {{senderName}}",
            "{{senderName}} is making {{recipientName}} CHUG! CHUG! CHUG!",
            "{{senderName}} gives {{recipientName}} a bird bath!",
            "How nice! {{senderName}} has decided to quench {{recipientName}}'s thirst!",
            "Better bring a bib {{recipientName}}, 'cause {{senderName}} is gettin' you MESSY.",
            //"Hey {{recipientName}}, chug a beer you emaciated prick! (Good one {{senderName}}!).",
            "Oof, {{senderName}} is gonna make {{recipientName}} toss some cookies.",
            "That's right {{recipientName}}, show {{senderName}} your chug face.",
            "One beer chug for {{recipientName}}, courtesy of {{senderName}}.",
            "Hope you got your funnel ready {{recipientName}}, 'cause {{senderName}}'s calling you out.",
            "CUT {{recipientName}}'s LIFE INTO PIECES, THIS IS {{senderName}}'s LAST RESORT!",
            //"Is that Zdeno Chara's dick? Oh nevermind, it's just {{recipientName}} chugging a beer from {{senderName}}.",
            "{{senderName}} draws first blood with a vicious attack on {{recipientName}}. But this is far from over...",
            //"{{senderName}} just turned {{recipientName}}'s whole life into a managerie of assplay.",
            "A right hook by {{senderName}}, DOWN GOES {{recipientName}}!",
            "\"Gluglglguglglgugl\" - {{recipientName}}, chugging a beer from {{senderName}}",
            //"{{recipientName}} just got shit-slapped by {{senderName}}!",
            "{{senderName}} just sunk your battleship, {{recipientName}}.",
            "{{senderName}} is taking {{recipientName}} down a peg.",
            //"Dr. {{senderName}} performs a spontaneous colonoscopy on {{recipientName}}!",
            "{{senderName}} absolutely bullying {{recipientName}}. Enjoy the beer, dork!"
        };
    }

    private static class DrinkInvalidated
    {
        public static string Title = "Drink Update";
        public static List<string> Texts = new()
        {
            "Hope you didn't chug yet, {{recipientName}}, because {{senderName}}'s drink just got revoked."
        };
    }

    private static class DrinkRevoked
    {
        public static string Title = "Drink Revoked";
        public static List<string> Texts = new()
        {
            "{{name}} has had a drink revoked due to a scoring change."
        };
    }

    private static class GoalChanged
    {
        public static string Title = "Goal Change";
        public static List<string> Texts = new()
        {
            "{{oldScorer}}'s goal has been changed to {{newScorer}}"
        };
    }

    private static class GoalRemoved
    {
        public static string Title = "Goal Removed";
        public static List<string> Texts = new()
        {
            "{{player}}'s goal has been called back."
        };
    }
}
