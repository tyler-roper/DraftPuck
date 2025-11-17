namespace DraftPuck.Infrastructure.Redis;
public class RedisOptions
{
    public const string SectionName = "Redis";
    public string? ConnectionString { get; set; }
}