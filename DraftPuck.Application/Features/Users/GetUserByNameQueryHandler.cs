using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Users;
public class GetUserByNameQueryHandler(IMapper mapper, IUserRepository userRepository) : IRequestHandler<GetUserByNameQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserByNameQuery request, CancellationToken ct)
    {
        var userEntity = await userRepository.GetByName(request.Name, ct);
        return userEntity == null ? throw new NotFoundException("User not found.") : mapper.Map<UserProfileDto>(userEntity);
    }
}
