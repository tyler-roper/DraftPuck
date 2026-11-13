using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class GameSummaryMappingProfile : Profile
{
    public GameSummaryMappingProfile()
    {
        CreateMap<NhlScheduleGame, GameSummaryDto>()
            .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.StartTimeUTC))
            .ForMember(dest => dest.GameState, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameState(src.GameState)))
            .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameType(src.GameType)))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.PeriodDescriptor != null ? src.PeriodDescriptor.Number : 0))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => src.PeriodDescriptor != null ? NhlMappingHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType) : PeriodType.Regulation))
            // The schedule has no game clock; the full game feed provides it.
            .ForMember(dest => dest.MinutesRemainingInPeriod, opt => opt.Ignore())
            .ForMember(dest => dest.SecondsRemainingInPeriod, opt => opt.Ignore());

        CreateMap<GameDto, GameSummaryDto>();
    }
}