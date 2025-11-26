using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Admin.Lobbies;

public class GetAllLobbiesQuery : IRequest<IEnumerable<LobbySummaryDto>>
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool ActiveOnly { get; set; }
    public Guid? UserId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool IncludeRemovedUsers { get; set; }
}