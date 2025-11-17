namespace DraftPuck.Web.Features.Users;

public class CreateUserRequestDto
{
    public string Nickname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
