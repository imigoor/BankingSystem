using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Transactions.Application.Common;

namespace Transactions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
