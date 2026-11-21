namespace DraftPuck.Application.Features.Admin.EventLogos;

/// <summary>An event logo, as the admin page lists it.</summary>
public class EventLogoAdminDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartsUtc { get; set; }
    public DateTime EndsUtc { get; set; }
    public string LogoUrl { get; set; } = null!;
    public string WideLogoUrl { get; set; } = null!;

    public static EventLogoAdminDto From(EventLogoEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        StartsUtc = DateTime.SpecifyKind(e.StartsUtc, DateTimeKind.Utc),
        EndsUtc = DateTime.SpecifyKind(e.EndsUtc, DateTimeKind.Utc),
        LogoUrl = e.LogoUrl,
        WideLogoUrl = e.WideLogoUrl,
    };
}
