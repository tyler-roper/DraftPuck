namespace DraftPuck.Infrastructure.AzureStorage;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    public string? ConnectionString { get; set; }
    public bool UseDevelopmentStorage { get; set; } = false;
    public string AchievementQueueName { get; set; } = "achievement-check-queue";
    public string NhlQueueName { get; set; } = "nhl-queue";
    public string AvatarStorageContainer { get; set; } = "user-avatars";
}