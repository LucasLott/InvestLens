using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace InvestLens.Api.ExceptionHandling
{
    public sealed class HttpErrorLogger(ILogger<HttpErrorLogger> logger)
    {
        private static readonly object LoggedKey = new();
        
        public static string TraceId(HttpContext context) =>
            Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        public void Log(HttpContext context, int status, Exception? exception = null)
        {
            if (context.Items.ContainsKey(LoggedKey) ||
                !(status is 400 or 401 or 403 or 404 or 409 || status >= 500)) return;
            context.Items[LoggedKey] = true;

            // Templates evitam expor segmentos e query strings com dados sensíveis.
            var endpoint = context.GetEndpoint() ?? context.Features.Get<IExceptionHandlerFeature>()?.Endpoint;

            var path = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? "[unmatched]";

            var method = context.Request.Method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS"
                ? context.Request.Method : "[other]";

            // Message/Data/InnerException podem conter credenciais. Registrar tipo e métodos seguros.
            var frames = exception is not null && status >= 500
                ? string.Join(" <- ", new StackTrace(exception, false).GetFrames()
                    .Select(frame => frame.GetMethod())
                    .Select(info => $"{info?.DeclaringType?.FullName}.{info?.Name}"))
                : null;

            logger.Log(status >= 500 ? LogLevel.Error : LogLevel.Warning,
                "Erro HTTP {StatusCode} {Method} {Path} TraceId={TraceId} Exception={ExceptionType} StackTrace={StackTrace}",
                status, method, path, TraceId(context), exception?.GetType().FullName, frames);
        }
    }
}