using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Users;

public class RegisterDeviceCommandHandler(IDbContext dbContext, IOptions<ApplicationOptions> appOptions) : IRequestHandler<RegisterDeviceCommand>
{
    public async Task Handle(RegisterDeviceCommand request, CancellationToken ct)
    {
        var token = DeviceTokens.Validate(request.Token);
        var now = appOptions.Value.CurrentTimeUtc;
        var description = DeviceDescriptions.Describe(request.UserAgent, request.IsInstalledApp);

        if (!await dbContext.Users.AnyAsync(u => u.Id == request.UserId, ct))
            throw new ValidationException("Unknown user.");

        // A token is one browser or app. If it's registered to someone else, that person signed out and this user
        // signed in on the same device, so it moves rather than notifying both.
        var device = await dbContext.UserDevices.FirstOrDefaultAsync(d => d.Token == token, ct);
        if (device == null)
            dbContext.UserDevices.Add(new UserDeviceEntity { UserId = request.UserId, Token = token, Description = description, LastSeenUtc = now });
        else
        {
            device.UserId = request.UserId;
            device.LastSeenUtc = now;
            device.Description = description;
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
