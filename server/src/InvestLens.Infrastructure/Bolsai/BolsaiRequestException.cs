using System.Net;

namespace InvestLens.Infrastructure.Bolsai
{
    public enum BolsaiFailureKind
    {
        Unauthorized,
        Forbidden,
        NotFound,
        RateLimited,
        ExternalServiceFailure,
        Timeout,
        NetworkUnavailable,
        InvalidResponse
    }

    public sealed class BolsaiRequestException(BolsaiFailureKind kind, string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public BolsaiFailureKind Kind { get; } = kind;
        public HttpStatusCode? StatusCode { get; } = statusCode;
    }
}
