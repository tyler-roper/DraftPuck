using DraftPuck.Application.Common;
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

    public static void ValidatePassword(string? password, bool allowNull = false)
    {
        if (allowNull && password == null) return;
        if (password == null || password.Trim().Length < 8 || password.Length > 25)
            throw new ValidationException("Invalid password.");
    }

    // Only other users count: the user being updated keeps their own nickname and email. (Checking just the first
    // match let another account's email or nickname through whenever the user's own row matched first.)
    private static async Task ValidateUniqueness(IDbContext dbContext, string? nickname, string? email, CancellationToken cancellationToken, Guid? userId = null)
    {
        if (nickname == null && email == null) return;

        var others = dbContext.Users.Where(u => userId == null || u.Id != userId);

        if (nickname != null && await others.AnyAsync(u => u.Nickname == nickname, cancellationToken))
            throw new ValidationException("Nickname is already taken.");

        if (email != null && await others.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ValidationException("Email is already taken.");
    }
}
