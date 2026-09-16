using FluentValidation;
using InvestLens.Application.Interfaces.Services.Auth;
using InvestLens.Application.Interfaces.Services.Usuario;
using InvestLens.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InvestLens.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
