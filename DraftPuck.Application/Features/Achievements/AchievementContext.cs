namespace DraftPuck.Application.Features.Achievements;

public class AchievementContext
{
    public required UserEntity User { get; set; }
    public required IDbContext DbContext { get; set; }
}