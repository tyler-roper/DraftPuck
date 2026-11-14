using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Management;

/// <summary>
/// Limits on a lobby's settings. The forms enforce these too, but the API is the authority: nothing here may rely on
/// the client having checked first.
/// </summary>
public static class LobbyRules
{
    public const int MinPicksPerTeam = 1;
    public const int MaxPicksPerTeam = 9;
    public const int MaxBots = 10;
    // More than any NHL day has, and few enough to always fit the GameIds column.
    public const int MaxGames = 32;

    public static void ValidateNewLobby(int picksPerTeam, IReadOnlyCollection<int> gameIds, int botCount)
    {
        if (picksPerTeam is < MinPicksPerTeam or > MaxPicksPerTeam)
            throw new ValidationException($"Picks per team must be between {MinPicksPerTeam} and {MaxPicksPerTeam}.");

        if (gameIds.Count == 0)
            throw new ValidationException("Choose at least one game.");

        if (gameIds.Count > MaxGames || gameIds.Distinct().Count() != gameIds.Count)
            throw new ValidationException("The list of games is invalid.");

        ValidateBotCount(botCount);
    }

    public static void ValidateBotCount(int botCount)
    {
        if (botCount > MaxBots)
            throw new ValidationException($"A lobby can have at most {MaxBots} bots.");
    }
}
