namespace DraftPuck.Application.Features.Lobbies.Recap;

public record RecapMember(Guid Id, string Name, bool IsBot = false);

/// <summary>
/// An active pick. <paramref name="SeasonGoals"/> is the player's goals before tonight (from the roster, when known);
/// <paramref name="GameStart"/> is puck drop.
/// </summary>
public record RecapPick(Guid Id, Guid MemberId, int GameId, int PlayerId, int Drinks, DateTime Created,
    int? SeasonGoals = null, DateTime? GameStart = null);

/// <summary>A drink earned by a pick of <paramref name="FromMemberId"/>'s, handed to <paramref name="ToMemberId"/> (if it was).</summary>
public record RecapDrink(Guid FromMemberId, Guid? ToMemberId, DateTime Created, DateTime? Assigned);

/// <summary>An achievement a member earned that night; <paramref name="Rarity"/> is the share of players who have it.</summary>
public record RecapAchievement(Guid MemberId, string Name, string Description, double Rarity);

public record RecapInput(
    IReadOnlyCollection<RecapMember> Members,
    IReadOnlyCollection<RecapPick> Picks,
    IReadOnlyCollection<RecapDrink> Drinks,
    IReadOnlyDictionary<Guid, int> MessageCounts,
    IReadOnlyCollection<RecapAchievement> Achievements);

/// <summary>
/// A candidate award. <see cref="Strength"/> says how remarkable it was (about 0 to 1; <see cref="LobbyAwards.Notable"/>
/// and up always make the recap). The optional fields carry what the award's text needs.
/// </summary>
public record LobbyAward(string Key, Guid MemberId, double Strength, int Value = 0, RecapPick? Pick = null,
    Guid? OtherMemberId = null, int OtherValue = 0, RecapAchievement? Achievement = null);

/// <summary>
/// The end-of-night awards. Each rule finds its best candidate, if the award applies at all; <see cref="Select"/> then
/// picks between <see cref="MinAwards"/> and <see cref="MaxAwards"/> of them.
/// </summary>
public static class LobbyAwards
{
    public const string BestPick = "best_pick";
    public const string Sleeper = "sleeper";
    public const string HatTrick = "hat_trick";
    public const string PerfectNight = "perfect_night";
    public const string BuzzerBeater = "buzzer_beater";
    public const string IceCold = "ice_cold";
    public const string Bully = "bully";
    public const string Rivalry = "rivalry";
    public const string SpreadingTheLove = "spreading_the_love";
    public const string Thirstiest = "thirstiest";
    public const string DesignatedDriver = "designated_driver";
    public const string Hoarder = "hoarder";
    public const string Chatterbox = "chatterbox";
    public const string SilentAssassin = "silent_assassin";
    public const string BeatenByARobot = "beaten_by_a_robot";
    public const string OverAchiever = "over_achiever";

    /// <summary>Bump when the rules or wording change, so cached recaps are recomputed.</summary>
    public const int Version = 1;

    public const int MinAwards = 3;
    public const int MaxAwards = 7;
    public const int MaxAwardsPerMember = 2;
    /// <summary>Awards this strong always make the recap (up to <see cref="MaxAwards"/>).</summary>
    public const double Notable = 0.5;
    /// <summary>How much the lobby's seed can reorder close calls.</summary>
    private const double Jitter = 0.1;

    /// <summary>The awards for the night: the notable ones, topped up to the minimum, spread across members.</summary>
    public static List<LobbyAward> Compute(RecapInput input, int seed) => Select(Candidates(input), seed);

    /// <summary>Every award that applies, with its strength.</summary>
    public static List<LobbyAward> Candidates(RecapInput input)
    {
        var members = input.Members.ToDictionary(m => m.Id);
        string NameOf(Guid id) => members.TryGetValue(id, out var m) ? m.Name : "";
        bool IsHuman(Guid id) => members.TryGetValue(id, out var m) && !m.IsBot;

        var picks = input.Picks.Where(p => members.ContainsKey(p.MemberId)).ToList();
        // Drinks handed between members still in the lobby: an award shouldn't go to (or name) someone who left.
        var handed = input.Drinks
            .Where(d => d.ToMemberId is { } to && members.ContainsKey(to) && members.ContainsKey(d.FromMemberId))
            .Select(d => (From: d.FromMemberId, To: d.ToMemberId!.Value, d.Created, Assigned: d.Assigned ?? d.Created))
            .ToList();
        var awards = new List<LobbyAward?>();

        // --- Picks ---

        var bestPick = picks.Where(p => p.Drinks > 0).OrderByDescending(p => p.Drinks).ThenBy(p => p.Created).FirstOrDefault();
        if (bestPick != null)
            awards.Add(new LobbyAward(BestPick, bestPick.MemberId, Scale(bestPick.Drinks, 1, 3), bestPick.Drinks, bestPick));

        // Another pick (not the best one) whose player scored three or more.
        var hatTrick = picks.Where(p => p.Drinks >= 3 && p.Id != bestPick?.Id).OrderByDescending(p => p.Drinks).ThenBy(p => p.Created).FirstOrDefault();
        if (hatTrick != null)
            awards.Add(new LobbyAward(HatTrick, hatTrick.MemberId, 0.9, hatTrick.Drinks, hatTrick));

        // The scorer nobody expected: the fewest goals on the season before tonight.
        var sleeper = picks.Where(p => p.Drinks > 0 && p.SeasonGoals != null && p.Id != bestPick?.Id)
            .OrderBy(p => p.SeasonGoals).ThenByDescending(p => p.Drinks).ThenBy(p => p.Created).FirstOrDefault();
        if (sleeper != null)
            awards.Add(new LobbyAward(Sleeper, sleeper.MemberId, 1 - Scale(sleeper.SeasonGoals!.Value, 0, 10), sleeper.SeasonGoals.Value, sleeper));

        // Every pick (at least two) scored.
        awards.Add(picks.GroupBy(p => p.MemberId)
            .Where(g => g.Count() >= 2 && g.All(p => p.Drinks > 0))
            .Select(g => new LobbyAward(PerfectNight, g.Key, Math.Min(1, 0.5 + 0.15 * (g.Count() - 2)), g.Count()))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());

        // The latest pick, relative to puck drop, that went on to score: 15 minutes before or any time after.
        var buzzer = picks.Where(p => p.Drinks > 0 && p.GameStart != null && p.Created >= p.GameStart - TimeSpan.FromMinutes(15))
            .OrderByDescending(p => p.Created - p.GameStart).FirstOrDefault();
        if (buzzer != null)
        {
            var minutesAfterStart = (int)Math.Floor((buzzer.Created - buzzer.GameStart!.Value).TotalMinutes);
            awards.Add(new LobbyAward(BuzzerBeater, buzzer.MemberId, minutesAfterStart >= 0 ? 0.75 : 0.4, minutesAfterStart, buzzer));
        }

        // The most picks without one of them scoring.
        awards.Add(picks.GroupBy(p => p.MemberId)
            .Where(g => g.Count() >= 2 && g.All(p => p.Drinks == 0))
            .Select(g => new LobbyAward(IceCold, g.Key, Scale(g.Count(), 1, 4), g.Count()))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());

        // --- Drinks ---

        var pairs = handed.Where(d => d.From != d.To).GroupBy(d => (d.From, d.To)).ToDictionary(g => g.Key, g => g.Count());

        // The most drinks one member gave another.
        awards.Add(pairs.Where(p => p.Value >= 2)
            .Select(p => new LobbyAward(Bully, p.Key.From, Scale(p.Value, 1, 5), p.Value, OtherMemberId: p.Key.To))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).ThenBy(a => NameOf(a.OtherMemberId!.Value)).FirstOrDefault());

        // Two members trading drinks: scored on the smaller side, so it has to go both ways.
        awards.Add(pairs.Where(p => p.Key.From.CompareTo(p.Key.To) < 0 || !pairs.ContainsKey((p.Key.To, p.Key.From))) // each pair once
            .Select(p => (p.Key.From, p.Key.To, There: p.Value, Back: pairs.GetValueOrDefault((p.Key.To, p.Key.From))))
            .Where(x => Math.Min(x.There, x.Back) >= 2)
            .Select(x => x.There >= x.Back
                ? new LobbyAward(Rivalry, x.From, Scale(Math.Min(x.There, x.Back), 1, 4), x.There, OtherMemberId: x.To, OtherValue: x.Back)
                : new LobbyAward(Rivalry, x.To, Scale(Math.Min(x.There, x.Back), 1, 4), x.Back, OtherMemberId: x.From, OtherValue: x.There))
            .OrderByDescending(a => Math.Min(a.Value, a.OtherValue)).ThenByDescending(a => a.Value + a.OtherValue).ThenBy(a => NameOf(a.MemberId))
            .FirstOrDefault());

        // Gave drinks to the most different people.
        awards.Add(handed.Where(d => d.From != d.To).GroupBy(d => d.From)
            .Select(g => (MemberId: g.Key, People: g.Select(d => d.To).Distinct().Count()))
            .Where(x => x.People >= 3)
            .Select(x => new LobbyAward(SpreadingTheLove, x.MemberId, Scale(x.People, 2, 5), x.People))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());

        // Took the most drinks.
        awards.Add(handed.GroupBy(d => d.To)
            .Select(g => new LobbyAward(Thirstiest, g.Key, Scale(g.Count(), 1, 6), g.Count()))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());

        // Took none on a night with plenty going around (the one who gave the most, if several).
        if (handed.Count >= 5 && members.Count >= 3)
        {
            var given = handed.GroupBy(d => d.From).ToDictionary(g => g.Key, g => g.Count());
            var taken = handed.Select(d => d.To).ToHashSet();
            awards.Add(members.Values.Where(m => !m.IsBot && !taken.Contains(m.Id))
                .Select(m => new LobbyAward(DesignatedDriver, m.Id, Scale(handed.Count, 3, 12), handed.Count))
                .OrderByDescending(a => given.GetValueOrDefault(a.MemberId)).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());
        }

        // Sat on their drinks longest before handing them out (on average, over at least two).
        awards.Add(handed.Where(d => IsHuman(d.From)).GroupBy(d => d.From)
            .Where(g => g.Count() >= 2)
            .Select(g => (MemberId: g.Key, Minutes: (int)g.Average(d => (d.Assigned - d.Created).TotalMinutes)))
            .Where(x => x.Minutes >= 5)
            .Select(x => new LobbyAward(Hoarder, x.MemberId, Scale(x.Minutes, 0, 20), x.Minutes))
            .OrderByDescending(a => a.Value).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault());

        // --- Chat ---

        var messages = input.MessageCounts.Where(m => IsHuman(m.Key) && m.Value > 0).OrderByDescending(m => m.Value).ThenBy(m => NameOf(m.Key)).ToList();
        if (messages.Count > 0)
        {
            // The most messages, well ahead of everyone else.
            var (top, count) = (messages[0].Key, messages[0].Value);
            var runnerUp = messages.Count > 1 ? messages[1].Value : 0;
            if (count >= 10 && count >= runnerUp * 1.5)
                awards.Add(new LobbyAward(Chatterbox, top, Scale(count, 5, 30), count));

            // Never said a word but gave the most drinks, in a lobby that was chatting.
            if (messages.Sum(m => m.Value) >= 5)
            {
                var topGiver = handed.Where(d => d.From != d.To).GroupBy(d => d.From)
                    .Select(g => (MemberId: g.Key, Count: g.Count())).OrderByDescending(x => x.Count).ThenBy(x => NameOf(x.MemberId)).FirstOrDefault();
                if (topGiver.Count >= 3 && IsHuman(topGiver.MemberId) && input.MessageCounts.GetValueOrDefault(topGiver.MemberId) == 0)
                    awards.Add(new LobbyAward(SilentAssassin, topGiver.MemberId, 0.6, topGiver.Count));
            }
        }

        // --- Bots ---

        // A bot gave more drinks than every human.
        var givenBy = handed.Where(d => d.From != d.To).GroupBy(d => d.From).ToDictionary(g => g.Key, g => g.Count());
        var topBot = members.Values.Where(m => m.IsBot).Select(m => (m.Id, Given: givenBy.GetValueOrDefault(m.Id))).OrderByDescending(x => x.Given).FirstOrDefault();
        var topHuman = members.Values.Where(m => !m.IsBot).Select(m => givenBy.GetValueOrDefault(m.Id)).DefaultIfEmpty(0).Max();
        if (topBot.Given > 0 && topBot.Given > topHuman)
            awards.Add(new LobbyAward(BeatenByARobot, topBot.Id, 0.8, topBot.Given));

        // --- Achievements ---

        // The rarest achievement earned that night.
        var rarest = input.Achievements.Where(a => IsHuman(a.MemberId)).OrderBy(a => a.Rarity).ThenBy(a => NameOf(a.MemberId)).FirstOrDefault();
        if (rarest != null)
            awards.Add(new LobbyAward(OverAchiever, rarest.MemberId, Math.Clamp(1 - rarest.Rarity * 2, 0.2, 1), Achievement: rarest));

        return [.. awards.OfType<LobbyAward>()];
    }

    /// <summary>
    /// Every notable award, topped up to <see cref="MinAwards"/> with the strongest of the rest, at most
    /// <see cref="MaxAwards"/>, first giving each member one award before anyone gets a second. The seed (the lobby's)
    /// reorders close calls, so lobbies vary but a lobby's recap is the same on every load.
    /// </summary>
    public static List<LobbyAward> Select(IReadOnlyCollection<LobbyAward> candidates, int seed)
    {
        var random = new Random(seed);
        var ranked = candidates
            .OrderBy(a => a.Key, StringComparer.Ordinal) // a fixed order for the seeded jitter
            .Select(a => (Award: a, Score: a.Strength + (random.NextDouble() * 2 - 1) * Jitter))
            .OrderByDescending(x => x.Score)
            .Select(x => x.Award)
            .ToList();

        var chosen = new List<LobbyAward>();
        foreach (var perMember in new[] { 1, MaxAwardsPerMember })
        {
            foreach (var award in ranked)
            {
                if (chosen.Count >= MaxAwards) break;
                if (chosen.Contains(award) || chosen.Count(a => a.MemberId == award.MemberId) >= perMember) continue;
                if (award.Strength < Notable && chosen.Count >= MinAwards) continue;
                chosen.Add(award);
            }
        }

        // Show them strongest first.
        return [.. chosen.OrderByDescending(a => a.Strength).ThenBy(a => a.Key, StringComparer.Ordinal)];
    }

    /// <summary>0 at <paramref name="low"/>, 1 at <paramref name="high"/>, straight in between.</summary>
    private static double Scale(int value, int low, int high) => Math.Clamp((double)(value - low) / (high - low), 0, 1);
}
