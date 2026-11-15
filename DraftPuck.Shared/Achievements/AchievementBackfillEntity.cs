namespace DraftPuck.Shared.Achievements;

/// <summary>
/// One admin-triggered achievement backfill. The worker adds to the counts as it checks each queued user, so the admin
/// page can show progress; it's finished when every queued user has been checked.
/// </summary>
public class AchievementBackfillEntity
{
    public Guid Id { get; set; }
    public DateTime StartedUtc { get; set; }
    public Guid StartedBy { get; set; }
    public int UsersQueued { get; set; }
    public int UsersChecked { get; set; }
    public int AchievementsAwarded { get; set; }
}
