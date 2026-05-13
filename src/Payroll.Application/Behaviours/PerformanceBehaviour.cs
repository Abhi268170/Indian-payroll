using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Payroll.Application.Behaviours;

internal sealed class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        TResponse response = await next();
        timer.Stop();

        if (timer.ElapsedMilliseconds > SlowRequestThresholdMs)
            logger.LogWarning("Slow request: {RequestName} took {ElapsedMs}ms",
                typeof(TRequest).Name, timer.ElapsedMilliseconds);

        return response;
    }
}
