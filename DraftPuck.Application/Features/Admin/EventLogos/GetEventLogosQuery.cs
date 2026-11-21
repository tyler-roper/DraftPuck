namespace DraftPuck.Application.Features.Admin.EventLogos;

/// <summary>Every event logo, latest-starting first.</summary>
public record GetEventLogosQuery : IRequest<List<EventLogoAdminDto>>;
