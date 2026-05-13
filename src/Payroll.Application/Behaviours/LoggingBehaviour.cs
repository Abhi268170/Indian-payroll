using MediatR;
using Microsoft.Extensions.Logging;

namespace Payroll.Application.Behaviours;

internal sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);
        TResponse response = await next();
        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
