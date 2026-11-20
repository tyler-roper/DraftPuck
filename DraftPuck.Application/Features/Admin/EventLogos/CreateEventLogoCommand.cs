namespace DraftPuck.Application.Features.Admin.EventLogos;

/// <summary>Schedules a logo for an event. See <see cref="EventLogoEntity"/> for what each field does.</summary>
public record CreateEventLogoCommand(string Name, DateTime StartsUtc, DateTime EndsUtc, string LogoUrl, string WideLogoUrl)
    : IRequest<EventLogoAdminDto>;
