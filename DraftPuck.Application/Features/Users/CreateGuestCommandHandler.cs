using DraftPuck.Application.Features.Auth;
using DraftPuck.Shared.Auth;

namespace DraftPuck.Application.Features.Users;

public class CreateGuestCommandHandler(IDbContext dbContext, IMapper mapper, ITokenService tokenService, IUserRepository userRepository) : IRequestHandler<CreateGuestCommand, AuthenticationResultDto>
{
    public async Task<AuthenticationResultDto> Handle(CreateGuestCommand request, CancellationToken cancellationToken)
    {
        var newId = Guid.NewGuid();
        UserEntity guest = new() { Id = newId };
        dbContext.Users.Add(guest);

        var refreshToken = tokenService.GenerateRefreshToken(request.IpAddress);
        var jwtToken = tokenService.GenerateGuestJwtToken(guest.Id);
        guest.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var userWithDetails = await userRepository.GetById(guest.Id, cancellationToken);
        var userDto = mapper.Map<UserDto>(userWithDetails);

        return new AuthenticationResultDto
        {
            User = userDto,
            JwtToken = jwtToken,
            RefreshToken = refreshToken.Token,
            AntiCsrfToken = refreshToken.AntiCsrfToken
        };
    }
}
