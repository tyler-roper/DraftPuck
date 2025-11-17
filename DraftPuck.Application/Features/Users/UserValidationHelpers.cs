using DraftPuck.Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Users;
public static class UserValidationHelpers
{
    public static async Task ValidateCreateUserRequest(IDbContext dbContext, CreateUserCommand request, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email);
        ValidateNickname(request.Nickname);
        ValidatePassword(request.Password);
        await ValidateUniqueness(dbContext, request.Nickname, request.Email, cancellationToken);
    }

    public static async Task ValidateUpdateUserRequest(IDbContext dbContext, UpdateUserCommand request, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email, true);
        ValidateNickname(request.Nickname, true);
        ValidatePassword(request.Password, true);
        await ValidateUniqueness(dbContext, request.Nickname, request.Email, cancellationToken, request.TargetUserId);
    }

    private static void ValidateEmail(string? email, bool allowNull = false)
    {
        if (allowNull && email == null) return;
        if (!RegexUtilities.IsValidEmail(email))
            throw new ValidationException("Invalid email.");
    }

    private static void ValidateNickname(string? nickname, bool allowNull = false)
    {
        if (allowNull && nickname == null) return;
        if (!RegexUtilities.IsValidNickname(nickname))
            throw new ValidationException("Invalid nickname.");
    }

    private static void ValidatePassword(string? password, bool allowNull = false)
    {
        if (allowNull && password == null) return;
        if (password == null || password.Trim().Length < 8 || password.Length > 25)
            throw new ValidationException("Invalid password.");
    }

    private static async Task ValidateUniqueness(IDbContext dbContext, string? nickname, string? email, CancellationToken cancellationToken, Guid? userId = null)
    {
        if (nickname == null && email == null) return;

        var conflictingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Nickname == nickname || u.Email == email, cancellationToken);
        if (conflictingUser == null || (userId != null && userId == conflictingUser.Id)) return;

        if (nickname != null && conflictingUser.Nickname == nickname) throw new ValidationException("Nickname is already taken.");
        if (email != null && conflictingUser.Email == email) throw new ValidationException("Email is already taken.");
    }
}
