using Azure.Storage.Blobs;
using Clients.Application.Interfaces;
using Clients.Domain.Interfaces;
using Clients.Infrastructure.Cache;
using Clients.Infrastructure.Email;
using Clients.Infrastructure.Messaging;
using Clients.Infrastructure.Persistence;
using Clients.Infrastructure.Persistence.Repositories;
using Clients.Infrastructure.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;

namespace Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- SQL Server ---
        services.AddDbContext<ClientsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ClientsDb"),
                b => b.MigrationsAssembly(typeof(ClientsDbContext).Assembly.FullName)));

        services.AddScoped<IClientRepository, ClientRepository>();

        // --- Redis Cache ---
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis:ConnectionString not configured.");
            options.InstanceName = "clients:";
        });

        services.AddScoped<IClientCacheService, ClientCacheService>();

        // --- Azure Blob Storage ---
        services.AddSingleton(new BlobServiceClient(
            configuration["BlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException("BlobStorage:ConnectionString not configured.")));

        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // --- SendGrid Email ---
        services.AddSingleton<ISendGridClient>(
            new SendGridClient(
                configuration["SendGrid:ApiKey"]
                    ?? throw new InvalidOperationException("SendGrid:ApiKey not configured.")));

        services.AddScoped<IEmailService, SendGridEmailService>();

        // --- MassTransit / Azure Service Bus ---
        services.AddMassTransit(x =>
        {
            x.UsingAzureServiceBus((_, cfg) =>
            {
                cfg.Host(configuration["ServiceBus:ConnectionString"]);
            });
        });

        services.AddScoped<IClientEventPublisher, ClientEventPublisher>();

        return services;
    }
}
