using InvestLens.Api.ExceptionHandling;
using InvestLens.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NLog.Web;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InvestLens.Api.Extensions
{
    public static class ApiExtensions
    {
        public static void AddApiLogging(this WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();

            // As falhas HTTP são registradas pelo HttpErrorLogger, sem detalhes do token.
            builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.None);
            
            builder.Host.UseNLog();
        }

        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                // Model binding pode incluir valores de entrada nos erros.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ProblemDetails { Status = 400, Title = "Requisição inválida." };
                    problem.Extensions["traceId"] = HttpErrorLogger.TraceId(context.HttpContext);
                    var result = new BadRequestObjectResult(problem);
                    result.ContentTypes.Add("application/problem+json");
                    return result;
                };
            });
            
            services.AddHealthChecks();
            services.AddSingleton<HttpErrorLogger>();
            services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = HttpErrorLogger.TraceId(context.HttpContext));
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.Configure<ExceptionHandlerOptions>(options => options.SuppressDiagnosticsCallback = _ => true);
            services.AddOptions<SwaggerSettings>().Bind(configuration.GetSection(SwaggerSettings.SectionName));
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Informe o AccessToken retornado pelo login."
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
            services.AddOptions<SwaggerGenOptions>().Configure<IOptions<SwaggerSettings>>((options, settings) =>
            {
                var swagger = settings.Value;
                options.SwaggerDoc(swagger.Version, new OpenApiInfo
                {
                    Title = swagger.Title,
                    Version = swagger.Version,
                    Description = swagger.Description
                });
            });

            return services;
        }

        public static void UseApiErrors(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<HttpErrorLogger>();
            
            // Cobre também respostas sem exception, como 404 e erros de model binding.
            app.Use(async (context, next) =>
            {
                await next(context);
                logger.Log(context, context.Response.StatusCode);
            });
            app.UseExceptionHandler();
            app.UseStatusCodePages();
        }

        public static void UseApiDocumentation(this WebApplication app)
        {
            var swagger = app.Services.GetRequiredService<IOptions<SwaggerSettings>>().Value;

            if (!swagger.EnableSwaggerProd) return;

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.DocumentTitle = swagger.Title;
                options.SwaggerEndpoint($"{swagger.Version}/swagger.json", $"{swagger.Title} {swagger.Version}");
            });
        }
    }
}
