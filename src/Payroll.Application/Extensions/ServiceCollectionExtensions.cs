using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Application.Behaviours;

namespace Payroll.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            // Pipeline order: Logging → Validation → Performance → Handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(ApplicationAssemblyMarker).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
