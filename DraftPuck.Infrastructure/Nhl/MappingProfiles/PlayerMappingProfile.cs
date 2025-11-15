using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class PlayerMappingProfile : Profile
{
    public PlayerMappingProfile()
    {
        CreateMap<NhlPlayer, PlayerDto>()
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