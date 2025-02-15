using DraftPuck.Infrastructure.Nhl.Models;
using DraftPuck.Shared.Models;
using System.Globalization;

namespace DraftPuck.Core.AutoMapper;

public class LobbyProfile : Profile
{
    public LobbyProfile()
    {
        CreateMap<Lobby, LobbyResponse>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.LobbyMembers));
    }
}

public class LobbyMemberProfile : Profile
{
    public LobbyMemberProfile()
    {
        CreateMap<LobbyMember, LobbyMemberResponse>()
            .ForMember(dest => dest.Picks, opt => opt.MapFrom(src => src.LobbyMemberPicks));
    }
}

public class LobbyMemberPickProfile : Profile
{
    public LobbyMemberPickProfile()
    {
        CreateMap<LobbyMemberPick, LobbyMemberPickResponse>();
    }
}

public class DrinkProfile : Profile
{
    public DrinkProfile()
    {
        CreateMap<Drink, DrinkResponse>();
    }
}

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        CreateMap<MessageEntity, MessageModel>();
    }
}

public class GameProfile : Profile
{
    public GameProfile()
    {
        CreateMap<NhlFullGame, Game>()
            .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.StartTimeUTC))
            .ForMember(dest => dest.GameState, opt => opt.MapFrom(src => MapperHelpers.MapGameState(src.GameState)))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => MapperHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType)))
            .ForMember(dest => dest.MinutesRemainingInPeriod, opt => opt.MapFrom(src => src.Clock.InIntermission ? 0 : MapperHelpers.MapMinutesRemaining(src.Clock.TimeRemaining)))
            .ForMember(dest => dest.SecondsRemainingInPeriod, opt => opt.MapFrom(src => src.Clock.InIntermission ? 0 : MapperHelpers.MapSecondsRemaining(src.Clock.TimeRemaining)))
            .ForMember(dest => dest.GoalsByPeriod, opt => opt.MapFrom(src => src.Summary.Linescore.ByPeriod))
            .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => MapperHelpers.MapGameType(src.GameType)))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.PlayerSummaries, opt => opt.MapFrom(src => src.RosterSpots))
            .AfterMap((src, dest) =>
            {
                dest.HomeTeam.Strength = MapperHelpers.MapStrength(src.Situation, true);
                dest.HomeTeam.Situations = MapperHelpers.MapSituations(src.Situation, true);
                dest.AwayTeam.Strength = MapperHelpers.MapStrength(src.Situation, false);
                dest.AwayTeam.Situations = MapperHelpers.MapSituations(src.Situation, false);
            });
    }
}

public class TeamProfile : Profile
{
    public TeamProfile()
    {
        CreateMap<NhlTeamSummary, Team>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CommonName.Default))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => MapperHelpers.MapLocation(src.PlaceName)))
            .ForMember(dest => dest.Abbreviation, opt => opt.MapFrom(src => src.Abbrev))
            .Include<NhlTeamSummary, GameTeam>();
    }
}

public class GameTeamProfile : Profile
{
    public GameTeamProfile()
    {
        CreateMap<NhlTeamSummary, GameTeam>();
    }
}

public class PlayerProfile : Profile
{
    public PlayerProfile()
    {
        CreateMap<NhlPlayer, Player>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Default))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Default))
            .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.CurrentTeamId))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.SweaterNumber))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PlayerId))
            .ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(src => src.FeaturedStats != null ? src.FeaturedStats.RegularSeason.SubSeason.GamesPlayed : 0))
            .ForMember(dest => dest.Goals, opt => opt.MapFrom(src => src.FeaturedStats != null ? src.FeaturedStats.RegularSeason.SubSeason.Goals : 0))
            .ForMember(dest => dest.Assists, opt => opt.MapFrom(src => src.FeaturedStats != null ? src.FeaturedStats.RegularSeason.SubSeason.Assists : 0))
            .ForMember(dest => dest.Points, opt => opt.MapFrom(src => src.FeaturedStats != null ? src.FeaturedStats.RegularSeason.SubSeason.Points : 0));
    }
}

public class PlayerSummaryProfile : Profile
{
    public PlayerSummaryProfile()
    {
        CreateMap<NhlPlayerSummary, PlayerSummary>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Default))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Default))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.SweaterNumber))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PlayerId));
    }
}

public class PlayProfile : Profile
{
    public PlayProfile()
    {
        CreateMap<NhlPlay, Play>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.EventId))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => MapperHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType)))
            .ForMember(dest => dest.TimeRemainingInPeriod, opt => opt.MapFrom(src => src.TimeRemaining))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => MapperHelpers.MapPlayType(src.TypeDescKey)))
            .ForMember(dest => dest.PrimaryPlayerId, opt => opt.MapFrom(src => MapperHelpers.MapPrimaryPlayerId(src)))
            .ForMember(dest => dest.PrimaryTeamId, opt => opt.MapFrom(src => src.Details.EventOwnerTeamId))
            .ForMember(dest => dest.HomeScore, opt => opt.MapFrom(src => src.Details.HomeScore))
            .ForMember(dest => dest.AwayScore, opt => opt.MapFrom(src => src.Details.AwayScore))
            .ForMember(dest => dest.Penalty, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Details.DescKey) ? null : MapperHelpers.KebabToCamelCase(src.Details.DescKey)));
    }
}

public class PeriodSummaryProfile : Profile
{
    public PeriodSummaryProfile()
    {
        CreateMap<NhlLinescorePeriod, PeriodSummary>()
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.HomeGoals, opt => opt.MapFrom(src => src.Home))
            .ForMember(dest => dest.AwayGoals, opt => opt.MapFrom(src => src.Away));
    }
}

public class GameSummaryProfile : Profile
{
    public GameSummaryProfile()
    {
        CreateMap<NhlScheduleGame, GameSummary>()
            .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.StartTimeUTC))
            .ForMember(dest => dest.GameState, opt => opt.MapFrom(src => MapperHelpers.MapGameState(src.GameState)))
            .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => MapperHelpers.MapGameType(src.GameType)));

        CreateMap<Game, GameSummary>();
    }
}

public class ScheduleProfile : Profile
{
    public ScheduleProfile()
    {
        CreateMap<NhlSchedule, Schedule>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateTime.ParseExact(src.GameWeek.First().Date, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(dest => dest.Games, opt => opt.MapFrom(src => src.GameWeek.First().Games));
    }
}