using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using InvestLens.Infrastructure.Repositories;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using InvestLens.Infrastructure.Bolsai;
using InvestLens.Application.Interfaces.Providers;
using InvestLens.Application.Interfaces.Repositories;

namespace InvestLens.Infrastructure.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IFinancialImportRepository, FinancialImportRepository>();
            services.AddScoped<IFinancialUpdateRepository, FinancialUpdateRepository>();
            services.AddScoped<IPlanoRepository, PlanoRepository>();
            services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
            services.AddSingleton<SymmetricSecurityKey>(provider => provider.GetRequiredService<IOptions<JwtSettings>>().Value.CreateSigningKey());
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddOptions<BolsaiSettings>()
                .Bind(configuration.GetSection(BolsaiSettings.SectionName))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<BolsaiSettings>, BolsaiSettingsValidator>();
            services.AddHttpClient<BolsaiClient>((provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<BolsaiSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });
            services.AddScoped<ICompanyMarketDataProvider>(provider => provider.GetRequiredService<BolsaiClient>());
            services.AddScoped<IApiRequestBudget, BolsaiRequestBudget>();
            
            return services;
        }
    }
}
