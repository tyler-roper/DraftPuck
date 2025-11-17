namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlPlayer
{
    public int PlayerId { get; set; }
    public bool IsActive { get; set; }
    public int CurrentTeamId { get; set; }
    public string CurrentTeamAbbrev { get; set; } = null!;
    public NhlDefaultString FullTeamName { get; set; } = null!;
    public NhlDefaultString FirstName { get; set; } = null!;
    public NhlDefaultString LastName { get; set; } = null!;
    public string TeamLogo { get; set; } = null!;
    public int SweaterNumber { get; set; }
    public string Position { get; set; } = null!;
    public string Headshot { get; set; } = null!;
    public string HeroImage { get; set; } = null!;
    public FeaturedStats FeaturedStats { get; set; } = null!;
}

public class FeaturedStats
{
    public int Season { get; set; }
    public Season RegularSeason { get; set; } = null!;
}

public class Season
{
    public SubSeason SubSeason { get; set; } = null!;
}

public class SubSeason
{
    public int GamesPlayed { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Points { get; set; }
    public int PlusMinus { get; set; }
    public int Pim { get; set; }
    public int GameWinningGoals { get; set; }
    public int OtGoals { get; set; }
    public int Shots { get; set; }
    public decimal ShootingPctg { get; set; }
}
