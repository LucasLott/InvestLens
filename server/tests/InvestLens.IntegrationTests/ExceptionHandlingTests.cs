using System.Text.Json;
using InvestLens.Api.ExceptionHandling;
using InvestLens.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestLens.IntegrationTests
{

    public class ExceptionHandlingTests
    {
        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(403)]
        [InlineData(404)]
        [InlineData(409)]
        [InlineData(500)]
        public async Task HandlerReturnsSafeProblemAndLogsOnlyOnce(int status)
        {
            var capture = new CaptureLogger();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApiServices(new ConfigurationBuilder().Build());
            services.AddSingleton<ILogger<HttpErrorLogger>>(capture);
            await using var provider = services.BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "test-trace" };
            context.Request.Method = "GET";
            context.Request.Path = "/sensitive-marker";
            context.Request.QueryString = new QueryString("?token=sensitive-marker");
            context.Request.Headers.Accept = "text/html"; // Exercita o fallback JSON seguro.
            context.Response.Body = new MemoryStream();
            Exception exception;
            try
            {
                throw status == 500 ? new InvalidOperationException("sensitive-marker")
                    : new BadHttpRequestException("sensitive-marker", status);
            }
            catch (Exception caught) { exception = caught; }
            var handler = provider.GetServices<IExceptionHandler>().Single();
            Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));
            provider.GetRequiredService<HttpErrorLogger>().Log(context, status);
            Assert.Equal(status, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Assert.DoesNotContain("sensitive-marker", body);
            using var json = JsonDocument.Parse(body);
            Assert.Equal("test-trace", json.RootElement.GetProperty("traceId").GetString());
            var entry = Assert.Single(capture.Entries);
            Assert.Equal(status == 500 ? LogLevel.Error : LogLevel.Warning, entry.Level);
            Assert.Contains("test-trace", entry.Message);
            Assert.DoesNotContain("sensitive-marker", entry.Message);
            if (status == 500) Assert.Contains(nameof(HandlerReturnsSafeProblemAndLogsOnlyOnce), entry.Message);
        }

        private sealed class CaptureLogger : ILogger<HttpErrorLogger>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = [];
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => Entries.Add((level, formatter(state, exception)));
        }
    }
}