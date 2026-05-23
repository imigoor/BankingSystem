using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Transactions.Application.Interfaces;
using Transactions.Domain.Interfaces;
using Transactions.Infrastructure.HttpHandlers;
using Transactions.Infrastructure.Messaging;
using Transactions.Infrastructure.Messaging.Consumers;
using Transactions.Infrastructure.Persistence;
using Transactions.Infrastructure.Persistence.Repositories;
using Transactions.Infrastructure.Services;

namespace Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Database ---
        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("TransactionsDb"),
                b => b.MigrationsAssembly(typeof(TransactionsDbContext).Assembly.FullName)));

        services.AddScoped<ITransferRepository, TransferRepository>();


        services.AddScoped<ITransferEventPublisher, TransferEventPublisher>();

        services.AddTransient<TokenForwardingHandler>();

        services.AddHttpClient<IClientServiceClient, ClientServiceClient>(client =>
        {
            var baseUrl = configuration["Services:ClientsService:BaseUrl"]
                ?? throw new InvalidOperationException("ClientsService BaseUrl not configured.");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<TokenForwardingHandler>()
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());

        // --- Notification service (abstract boundary) ---
        services.AddScoped<INotificationService, NotificationService>();

        // --- MassTransit / Azure Service Bus ---
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ClientBankingDataUpdatedConsumer>();

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(configuration["AzureServiceBus:ConnectionString"]);

                cfg.SubscriptionEndpoint<ClientBankingDataUpdatedConsumer>(
                    "transactions-banking-updates",
                    e => e.ConfigureConsumer<ClientBankingDataUpdatedConsumer>(ctx));
            });
        });

        return services;
    }

    // -------------------------------------------------------------------------
    // Polly Policies
    // -------------------------------------------------------------------------

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, _) =>
                {
                    Console.WriteLine(
                        $"[Polly Retry] Attempt {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (_, duration) =>
                    Console.WriteLine($"[Polly CircuitBreaker] Circuit OPEN for {duration.TotalSeconds}s"),
                onReset: () =>
                    Console.WriteLine("[Polly CircuitBreaker] Circuit CLOSED — service recovered"),
                onHalfOpen: () =>
                    Console.WriteLine("[Polly CircuitBreaker] Circuit HALF-OPEN — testing recovery"));

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        => Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(5),
            onTimeoutAsync: (_, timeout, _, _) =>
            {
                Console.WriteLine($"[Polly Timeout] Request timed out after {timeout.TotalSeconds}s");
                return Task.CompletedTask;
            });
}
