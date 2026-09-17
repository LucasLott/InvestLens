using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Infrastructure.Repositories;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.Infrastructure.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
            services.AddSingleton<SymmetricSecurityKey>(provider => provider.GetRequiredService<IOptions<JwtSettings>>().Value.CreateSigningKey());
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            
            return services;
        }
    }
}
