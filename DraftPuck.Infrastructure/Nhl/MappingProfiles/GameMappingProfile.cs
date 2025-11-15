using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<NhlFullGame, GameDto>()
            .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.StartTimeUTC))
            .ForMember(dest => dest.GameState, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameState(src.GameState)))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => NhlMappingHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType)))
            .ForMember(dest => dest.MinutesRemainingInPeriod, opt => opt.MapFrom(src => src.Clock.InIntermission ? 0 : NhlMappingHelpers.MapMinutesRemaining(src.Clock.TimeRemaining)))
            .ForMember(dest => dest.SecondsRemainingInPeriod, opt => opt.MapFrom(src => src.Clock.InIntermission ? 0 : NhlMappingHelpers.MapSecondsRemaining(src.Clock.TimeRemaining)))
            .ForMember(dest => dest.GoalsByPeriod, opt => opt.MapFrom(src => src.Summary.Linescore.ByPeriod))
            .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameType(src.GameType)))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.PlayerSummaries, opt => opt.MapFrom(src => src.RosterSpots))
            .AfterMap((src, dest) =>
            {
                dest.HomeTeam.Strength = NhlMappingHelpers.MapStrength(src.Situation, true);
                dest.HomeTeam.Situations = NhlMappingHelpers.MapSituations(src.Situation, true);
                dest.AwayTeam.Strength = NhlMappingHelpers.MapStrength(src.Situation, false);
                dest.AwayTeam.Situations = NhlMappingHelpers.MapSituations(src.Situation, false);
            });
    }
}