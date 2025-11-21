namespace DraftPuck.Application.Features.Banners;
public class GetAllBannersQueryHandler(IMapper mapper, IDbContext dbContext) : IRequestHandler<GetAllBannersQuery, List<BannerDto>>
{
    public async Task<List<BannerDto>> Handle(GetAllBannersQuery request, CancellationToken ct)
    {
        var banners = await dbContext.Banners.ToListAsync(cancellationToken: ct);
        return mapper.Map<List<BannerDto>>(banners);
    }
}
