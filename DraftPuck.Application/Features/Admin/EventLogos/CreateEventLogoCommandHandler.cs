using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Admin.EventLogos;

public class CreateEventLogoCommandHandler(IDbContext dbContext) : IRequestHandler<CreateEventLogoCommand, EventLogoAdminDto>
{
    // The column sizes (DraftPuckContext).
    private const int MaxNameLength = 100;
    private const int MaxUrlLength = 1000;

    public async Task<EventLogoAdminDto> Handle(CreateEventLogoCommand request, CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? "";
        if (name.Length == 0) throw new BadRequestException("Give the event a name.");
        if (name.Length > MaxNameLength) throw new BadRequestException($"Keep the name to {MaxNameLength} characters.");
        var (starts, ends) = (AsUtc(request.StartsUtc), AsUtc(request.EndsUtc));
        if (ends <= starts) throw new BadRequestException("The event has to end after it starts.");

        var logo = new EventLogoEntity
        {
            Name = name,
            StartsUtc = starts,
            EndsUtc = ends,
            LogoUrl = ValidLogoUrl(request.LogoUrl, "logo"),
            WideLogoUrl = ValidLogoUrl(request.WideLogoUrl, "wide logo"),
        };

        dbContext.EventLogos.Add(logo);
        await dbContext.SaveChangesAsync(ct);
        return EventLogoAdminDto.From(logo);
    }

    /// <summary>Times without a zone are taken as UTC, not as the server's local time.</summary>
    private static DateTime AsUtc(DateTime time)
        => time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : DateTime.SpecifyKind(time, DateTimeKind.Utc);

    /// <summary>A path on this site ("/img/logo-4nations.png") or a full http(s) URL.</summary>
    private static string ValidLogoUrl(string? url, string which)
    {
        url = url?.Trim() ?? "";
        var isSitePath = url.StartsWith('/') && !url.StartsWith("//");
        var isWebUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        if (!isSitePath && !isWebUrl) throw new BadRequestException($"The {which} must be a path on this site (like /img/logo-event.png) or a full URL.");
        if (url.Length > MaxUrlLength) throw new BadRequestException($"Keep the {which} to {MaxUrlLength} characters.");
        return url;
    }
}
