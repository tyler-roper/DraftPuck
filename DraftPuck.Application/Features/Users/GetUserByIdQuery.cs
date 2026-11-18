namespace DraftPuck.Application.Features.Users;

/// <summary>
/// A user's full account (email, settings, everything they own). Only for the user themselves: other players' profiles
/// come from <see cref="GetUserByNameQuery"/>, which leaves out what's private.
/// </summary>
public class GetUserByIdQuery : IRequest<UserDto>
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
}
