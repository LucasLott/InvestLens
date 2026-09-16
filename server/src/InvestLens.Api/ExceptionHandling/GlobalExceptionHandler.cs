using FluentValidation;
using InvestLens.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace InvestLens.Api.ExceptionHandling
{
    public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, HttpErrorLogger logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            var status = exception switch
            {
                ValidationException or BusinessException => 400,
                BadHttpRequestException request when request.StatusCode is 400 or 401 or 403 or 404 or 409 => request.StatusCode,
                _ => 500
            };
            
            context.Response.StatusCode = status;

            logger.Log(context, status, exception);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "Erro interno do servidor." : ReasonPhrases.GetReasonPhrase(status)
            };

            problem.Extensions["traceId"] = HttpErrorLogger.TraceId(context);

            if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem }))
                await context.Response.WriteAsJsonAsync(problem, options: null,
                    contentType: "application/problem+json", cancellationToken: cancellationToken);

            return true;
        }
    }
}