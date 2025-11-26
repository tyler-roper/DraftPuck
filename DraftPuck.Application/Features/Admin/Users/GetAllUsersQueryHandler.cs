using DraftPuck.Application.Features.Users;

namespace DraftPuck.Application.Features.Admin.Users;

public class GetAllUsersQueryHandler(IDbContext dbContext, IMapper mapper) : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var pageSize = Math.Max(request.PageSize, 50);

        var userQuery = dbContext.Users.AsNoTracking();

        if (request.Nickname != null)
            userQuery = userQuery.Where(u => EF.Functions.Like(u.Nickname, $"%{request.Nickname}%"));

        userQuery = userQuery.Where(u => !u.IsGuest || request.IncludeGuests);
        userQuery = userQuery.Where(u => u.IsActive || !request.ActiveOnly);

        var users = await userQuery
            .OrderBy(l => l.Nickname)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return mapper.Map<IEnumerable<UserDto>>(users);
    }
}