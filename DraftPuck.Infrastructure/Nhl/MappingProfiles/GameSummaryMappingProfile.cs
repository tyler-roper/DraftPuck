using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class GameSummaryMappingProfile : Profile
{
    public GameSummaryMappingProfile()
    {
        CreateMap<NhlScheduleGame, GameSummaryDto>()
            .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.StartTimeUTC))
            .ForMember(dest => dest.GameState, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameState(src.GameState)))
            .ForMember(dest => dest.GameType, opt => opt.MapFrom(src => NhlMappingHelpers.MapGameType(src.GameType)));

        CreateMap<GameDto, GameSummaryDto>();
    }
}