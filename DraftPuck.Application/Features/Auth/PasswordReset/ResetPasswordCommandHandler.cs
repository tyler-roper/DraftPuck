using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Users;

namespace DraftPuck.Application.Features.Auth.PasswordReset;

public class ResetPasswordCommandHandler(IDbContext dbContext) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // Check the password first, so a typo doesn't use up the link.
        UserValidationHelpers.ValidatePassword(request.Password);

        var now = DateTime.UtcNow;
        var tokenHash = PasswordResetTokens.Hash(request.Token ?? "");
        var resetToken = await dbContext.PasswordResetTokens
            .Include(t => t.User)
                .ThenInclude(u => u.RefreshTokens)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.Expires > now && t.User.IsActive, ct)
            ?? throw new BadRequestException("This reset link is invalid or has expired. Request a new one.");

        var user = resetToken.User;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Signed-in sessions elsewhere end, in case someone else had the old password.
        foreach (var refreshToken in user.RefreshTokens.Where(t => t.IsValid))
        {
            refreshToken.Revoked = now;
            refreshToken.ReasonRevoked = "Password reset";
        }

        dbContext.PasswordResetTokens.Remove(resetToken);
        await dbContext.SaveChangesAsync(ct);
    }
}
