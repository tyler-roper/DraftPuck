using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Admin.EventLogos;

public class DeleteEventLogoCommandHandler(IDbContext dbContext) : IRequestHandler<DeleteEventLogoCommand>
{
    public async Task Handle(DeleteEventLogoCommand request, CancellationToken ct)
    {
        var logo = await dbContext.EventLogos.FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException("Event logo not found.");

        dbContext.EventLogos.Remove(logo);
        await dbContext.SaveChangesAsync(ct);
    }
}
