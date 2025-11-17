namespace DraftPuck.Application.Features.Achievements;

public class AchievementMappingProfile : Profile
{
    public AchievementMappingProfile()
    {
        CreateMap<AchievementEntity, AchievementDto>();
    }
}