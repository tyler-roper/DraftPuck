using AutoMapper;
using DraftPuck.Application.Features.Users;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Infrastructure.Nhl.MappingProfiles;

namespace DraftPuck.Tests.Support;

/// <summary>Real implementations of the services handlers depend on, configured for tests.</summary>
public static class TestServices
{
    public static readonly AuthOptions AuthOptions = new()
    {
        JwtKey = "test-signing-key-that-is-long-enough-for-hmac-sha256",
        Issuer = "draftpuck-tests",
        Audience = "draftpuck-tests"
    };

    public static TokenService TokenService() => new(AuthOptions);

    /// <summary>The app's AutoMapper profiles, as registered in Program.</summary>
    public static readonly MapperConfiguration MapperConfiguration = new(
        cfg => cfg.AddMaps(typeof(UserMappingProfile).Assembly, typeof(GameMappingProfile).Assembly),
        NullLoggerFactory.Instance);

    public static IMapper Mapper() => MapperConfiguration.CreateMapper();
}
