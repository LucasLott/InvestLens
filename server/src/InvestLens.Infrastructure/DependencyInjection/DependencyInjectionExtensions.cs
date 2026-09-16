using InvestLens.Application.Interfaces.Security;
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
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            
            return services;
        }
    }
}