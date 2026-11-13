using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Tests.Infrastructure;

public class NhlMappingProfileTests
{
    private readonly AutoMapper.IMapper _mapper = TestServices.Mapper();

    [Theory]
    [InlineData(3, "REG", PeriodType.Regulation)]
    [InlineData(4, "OT", PeriodType.Overtime)]
    [InlineData(5, "SO", PeriodType.Shootout)]
    public void LinescorePeriodsKeepTheirType(int number, string nhlType, PeriodType expected)
    {
        // The client labels box-score columns by type; a 4th "regulation" period has no label.
        var period = _mapper.Map<PeriodSummaryDto>(new NhlLinescorePeriod
        {
            PeriodDescriptor = new NhlPeriodDescriptor { Number = number, PeriodType = nhlType },
            Home = 1,
            Away = 2
        });

        Assert.Equal(expected, period.PeriodType);
        Assert.Equal(number, period.Number);
        Assert.Equal((1, 2), (period.HomeGoals, period.AwayGoals));
    }

    [Fact]
    public void PlayerSummariesIncludeTheirPosition()
    {
        var player = _mapper.Map<PlayerSummaryDto>(new NhlPlayerSummary
        {
            PlayerId = 97,
            TeamId = 22,
            FirstName = new NhlDefaultString { Default = "Connor" },
            LastName = new NhlDefaultString { Default = "McDavid" },
            SweaterNumber = 97,
            PositionCode = "C",
            Headshot = ""
        });

        Assert.Equal("C", player.Position);
        Assert.Equal(("Connor", "McDavid", 97), (player.FirstName, player.LastName, player.Number));
    }

    [Fact]
    public void ScheduledGamesIncludeTheCurrentPeriod()
    {
        var summary = _mapper.Map<GameSummaryDto>(new NhlScheduleGame
        {
            Id = 1,
            GameState = "LIVE",
            GameScheduleState = "OK",
            StartTimeUTC = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc),
            HomeTeam = new NhlTeamSummary(),
            AwayTeam = new NhlTeamSummary(),
            PeriodDescriptor = new NhlPeriodDescriptor { Number = 4, PeriodType = "OT" }
        });

        Assert.Equal(4, summary.Period);
        Assert.Equal(PeriodType.Overtime, summary.PeriodType);
    }

    [Fact]
    public void ScheduledGamesWithoutAPeriodYetStartAtZero()
    {
        var summary = _mapper.Map<GameSummaryDto>(new NhlScheduleGame
        {
            Id = 1,
            GameState = "FUT",
            GameScheduleState = "OK",
            HomeTeam = new NhlTeamSummary(),
            AwayTeam = new NhlTeamSummary(),
            PeriodDescriptor = null!
        });

        Assert.Equal(0, summary.Period);
    }
}
