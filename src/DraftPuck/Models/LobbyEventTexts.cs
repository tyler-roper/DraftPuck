namespace DraftPuck.Models
{
    public static class LobbyEventTexts
    {
        private readonly static Random _random = new();

        public static string GetTitle(LobbyEventType eventType)
        {
            if (eventType == LobbyEventType.LobbyCreated) return LobbyCreated.Title;
            if (eventType == LobbyEventType.UserJoined) return UserJoined.Title;
            if (eventType == LobbyEventType.NewPick) return NewPick.Title;
            if (eventType == LobbyEventType.DrinkAwarded) return DrinkAwarded.Title;
            if (eventType == LobbyEventType.DrinkAssigned) return DrinkAssigned.Title;
            if (eventType == LobbyEventType.DrinkInvalidated) return DrinkInvalidated.Title;
            if (eventType == LobbyEventType.DrinkRevoked) return DrinkRevoked.Title;   
            if (eventType == LobbyEventType.GoalChanged) return GoalChanged.Title;
            if (eventType == LobbyEventType.GoalRemoved) return GoalRemoved.Title;
            return "No title";
        }

        public static string GetText(LobbyEventType eventType)
        {
            if (eventType == LobbyEventType.LobbyCreated) return Random(LobbyCreated.Texts);
            if (eventType == LobbyEventType.UserJoined) return Random(UserJoined.Texts);
            if (eventType == LobbyEventType.NewPick) return Random(NewPick.Texts);
            if (eventType == LobbyEventType.DrinkAwarded) return Random(DrinkAwarded.Texts);
            if (eventType == LobbyEventType.DrinkAssigned) return Random(DrinkAssigned.Texts);
            if (eventType == LobbyEventType.DrinkInvalidated) return Random(DrinkInvalidated.Texts);
            if (eventType == LobbyEventType.DrinkRevoked) return Random(DrinkRevoked.Texts);
            if (eventType == LobbyEventType.GoalChanged) return Random(GoalChanged.Texts);
            if (eventType == LobbyEventType.GoalRemoved) return Random(GoalRemoved.Texts);
            return "No text generated";
        }

        private static string Random(List<string> strings) => strings[_random.Next(strings.Count)];

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
                "Welcome, {{name}}!"
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
                "Someone boutta hop up on {{name}}'s dick and do a full split. {{playerBadge}}",
                "{{name}} gettin' lubed up to do some rectal damage. {{playerBadge}}",
                "Everyone be on alert, {{name}} is armed and dangerous. {{playerBadge}}",
                "{{name}} with the chance of a lifetime! {{playerBadge}}",
                "Hey {{name}}. Pick your target, take a deep breath, and give out a drink. {{playerBadge}}",
                "That's the sound of buttholes puckering across the world, courtesy of {{name}}. {{playerBadge}}"
            };
        }

        private static class DrinkAssigned
        {
            public static string Title = "Drink Assigned";
            public static List<string> Texts = new()
            {
                "\"Hey {{recipientName}}, go fuck a goat!\" - {{senderName}}",
                "{{senderName}} is making {{recipientName}} CHUG! CHUG! CHUG!",
                "{{senderName}} gives {{recipientName}} a bird bath!",
                "How nice! {{senderName}} has decided to quench {{recipientName}}'s thirst!",
                "Better bring a bib {{recipientName}}, 'cause {{senderName}} is gettin' you MESSY.",
                "Hey {{recipientName}}, chug a beer you emaciated prick! (Good one {{senderName}}!).",
                "Oof, {{senderName}} is gonna make {{recipientName}} toss some cookies.",
                "That's right {{recipientName}}, show {{senderName}} your chug face.",
                "One beer chug for {{recipientName}}, courtesy of {{senderName}}.",
                "Hope you got your funnel ready {{recipientName}}, 'cause {{senderName}}'s calling you out.",
                "{{senderName}}: *turns hat backwards* {{recipientName}}, I CHOOSE YOU!!!"
            };
        }

        private static class DrinkInvalidated
        {
            public static string Title = "Drink Update";
            public static List<string> Texts = new()
            {
                "{{recipientName}} chugged a beer for {{senderName}} even though {{player}} didn't score."
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
}
