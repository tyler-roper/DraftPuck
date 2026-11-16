namespace DraftPuck.Application.Features.Users;

public class ForgetDeviceCommandHandler(IDbContext dbContext) : IRequestHandler<ForgetDeviceCommand>
{
    public async Task Handle(ForgetDeviceCommand request, CancellationToken ct)
    {
        var token = DeviceTokens.Validate(request.Token);

        // Only the user's own device: a token someone else has registered since isn't theirs to remove.
        var device = await dbContext.UserDevices.FirstOrDefaultAsync(d => d.Token == token && d.UserId == request.UserId, ct);
        if (device == null) return;

        dbContext.UserDevices.Remove(device);
        await dbContext.SaveChangesAsync(ct);
    }
}
