using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class GameTeamMappingProfile : Profile
{
    public GameTeamMappingProfile()
    {
        CreateMap<NhlTeamSummary, GameTeamDto>();
    }
}