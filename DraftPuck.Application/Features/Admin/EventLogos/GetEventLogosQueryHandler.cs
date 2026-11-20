namespace DraftPuck.Application.Features.Admin.EventLogos;

public class GetEventLogosQueryHandler(IDbContext dbContext) : IRequestHandler<GetEventLogosQuery, List<EventLogoAdminDto>>
{
    public async Task<List<EventLogoAdminDto>> Handle(GetEventLogosQuery request, CancellationToken ct)
    {
        var logos = await dbContext.EventLogos.AsNoTracking().OrderByDescending(e => e.StartsUtc).ToListAsync(ct);
        return [.. logos.Select(EventLogoAdminDto.From)];
    }
}
