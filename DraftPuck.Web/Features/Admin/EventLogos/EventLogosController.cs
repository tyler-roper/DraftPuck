using DraftPuck.Application.Features.Admin.EventLogos;

namespace DraftPuck.Web.Features.Admin.EventLogos;

/// <summary>The logos shown instead of the usual one during events (holidays, tournaments, opening night).</summary>
[Route("api/admin/event-logos")]
public class EventLogosController(IMediator mediator) : AdminBaseController
{
    [HttpGet]
    public async Task<ActionResult<List<EventLogoAdminDto>>> GetAll()
        => Ok(await mediator.Send(new GetEventLogosQuery()));

    [HttpPost]
    public async Task<ActionResult<EventLogoAdminDto>> Create(CreateEventLogoCommand command)
        => Ok(await mediator.Send(command));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteEventLogoCommand(id));
        return NoContent();
    }
}
