namespace DraftPuck.Application.Features.Titles;
public class GetAllTitlesQueryHandler(IMapper mapper, IDbContext dbContext) : IRequestHandler<GetAllTitlesQuery, List<TitleDto>>
{
    public async Task<List<TitleDto>> Handle(GetAllTitlesQuery request, CancellationToken ct)
    {
        var titles = await dbContext.Titles.ToListAsync(cancellationToken: ct);
        return mapper.Map<List<TitleDto>>(titles);
    }
}
