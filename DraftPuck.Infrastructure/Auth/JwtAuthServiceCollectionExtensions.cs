using DraftPuck.Shared.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Auth
{
    public static class JwtAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddTokenService(this IServiceCollection services, IConfiguration configuration) =>
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName))
                .AddTransient<ITokenService>(serviceProvider =>
                    CreateTokenService(serviceProvider.GetRequiredService<IOptions<AuthOptions>>().Value)
                );

        private static TokenService CreateTokenService(AuthOptions authOptions)
        {
            if (string.IsNullOrEmpty(authOptions.JwtKey))
                throw new InvalidOperationException("JWT Key is missing in AuthOptions configuration.");

            if (string.IsNullOrEmpty(authOptions.Issuer))
                throw new InvalidOperationException("JWT Issuer is missing in AuthOptions configuration.");

            if (string.IsNullOrEmpty(authOptions.Audience))
                throw new InvalidOperationException("JWT Audience is missing in AuthOptions configuration.");

            return new TokenService(authOptions);
        }
    }
}
