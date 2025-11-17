using DraftPuck.Application.Features.Banners;

namespace DraftPuck.Web.Features.Banners;

public class BannersController(IMediator mediator) : BaseController()
{
    [HttpGet]
    public async Task<ActionResult<List<BannerDto>>> GetAllBanners()
    {
        var query = new GetAllBannersQuery();
        var bannerDtos = await mediator.Send(query);
        return Ok(bannerDtos);
    }
}
