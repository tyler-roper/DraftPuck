using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Users;
public class GetUserByIdQueryHandler(IMapper mapper, IUserRepository userRepository) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var userEntity = await userRepository.GetById(request.Id, ct);
        return userEntity == null ? throw new NotFoundException("User not found.") : mapper.Map<UserDto>(userEntity);
    }
}
