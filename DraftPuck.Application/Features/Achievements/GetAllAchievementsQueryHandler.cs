namespace DraftPuck.Application.Features.Achievements;
public class GetAllAchievementsQueryHandler(IMapper mapper, IDbContext dbContext) : IRequestHandler<GetAllAchievementsQuery, List<AchievementDto>>
{
    public async Task<List<AchievementDto>> Handle(GetAllAchievementsQuery request, CancellationToken ct)
    {
        var achievements = await dbContext.Achievements.ToListAsync(cancellationToken: ct);
        return mapper.Map<List<AchievementDto>>(achievements);
    }
}
