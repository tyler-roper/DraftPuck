using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Domain.Auth;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Auth.PasswordReset;

public class RequestPasswordResetCommandHandler(IDbContext dbContext, IEmailSender emailSender, IOptions<ApplicationOptions> appOptions)
    : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 255)
            throw new ValidationException("Enter your email.");

        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsGuest && !u.IsBot, ct);

        // The caller gets the same response either way; there's simply nothing to send.
        if (user == null)
            return;

        // One link at a time: a new request replaces any earlier link.
        await dbContext.PasswordResetTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(ct);

        var token = PasswordResetTokens.Create();
        dbContext.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            UserId = user.Id,
            TokenHash = PasswordResetTokens.Hash(token),
            Expires = DateTime.UtcNow.Add(PasswordResetTokens.Lifetime)
        });
        await dbContext.SaveChangesAsync(ct);

        var app = appOptions.Value;
        var link = app.GetAbsoluteUrl($"reset-password?token={token}");
        await emailSender.SendAsync(PasswordResetEmail.Create(user.Email!, user.Nickname ?? "there", link, app), ct);
    }
}
