using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Users;

public class UpdateFcmRegistrationTokenCommandHandler(IDbContext dbContext, IUserRepository userRepository, IMapper mapper) : IRequestHandler<UpdateFcmRegistrationTokenCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateFcmRegistrationTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken) ?? throw new NotFoundException($"User not found with ID {request.UserId}");
        user.FcmRegistrationToken = request.Token;
        await dbContext.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}