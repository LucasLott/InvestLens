using InvestLens.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using InvestLens.Api.Authentication;
using InvestLens.Api.Authorization;
using InvestLens.Application.Authentication;
using InvestLens.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace InvestLens.Api.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<JwtSettings>().Bind(configuration.GetSection(JwtSettings.SectionName)).ValidateOnStart();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddSingleton<IAuthorizationHandler, SameUserAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.SameUser, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new SameUserRequirement());
                });
            });

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>, SymmetricSecurityKey>((options, settings, key) =>
                {
                    options.MapInboundClaims = false;
                    options.SaveToken = false;
                    options.IncludeErrorDetails = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidIssuer = settings.Value.Issuer,
                        ValidAudience = settings.Value.Audience,
                        IssuerSigningKey = key,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = JwtClaimNames.Nome,
                        LogTokenId = false,
                        IncludeTokenOnFailedValidation = false
                    };
                });
                
            return services;
        }
    }
}
