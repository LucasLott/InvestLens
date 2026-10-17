using FluentValidation;
using InvestLens.Application.Interfaces.Services.Auth;
using InvestLens.Application.Interfaces.Services.Usuario;
using InvestLens.Application.Interfaces.Services.Configuracao;
using InvestLens.Application.Interfaces.Services;
using InvestLens.Application.Authorization;
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
            services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<RefreshTokenService>();
            services.AddScoped<IFinancialImportService, FinancialImportService>();
            services.AddScoped<IFinancialUpdateService, FinancialUpdateService>();
            services.AddSingleton<IFinancialFeatureAccess, ConservativeFinancialFeatureAccess>();
            services.AddScoped<IPlanoService, PlanoService>();
            services.AddScoped<IAssinaturaService, AssinaturaService>();
            services.AddScoped<ISolicitacaoService, SolicitacaoService>();

            return services;
        }
    }
}
