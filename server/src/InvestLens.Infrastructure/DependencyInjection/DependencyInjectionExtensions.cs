using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Infrastructure.Repositories;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace InvestLens.Infrastructure.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            
            return services;
        }
    }
}
