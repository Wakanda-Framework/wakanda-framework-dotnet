using System;
using org.wakanda.framework.Cache;
using org.wakanda.framework.Cache.Interface;
using org.wakanda.framework.MessageBroker;
using org.wakanda.framework.MessageBroker.Interface;
using org.wakanda.framework.Repositories;
using org.wakanda.framework.Repositories.Interface;
using org.wakanda.framework.Services;
using org.wakanda.framework.Services.Interface;
using org.wakanda.framework.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using StackExchange.Redis;


namespace org.wakanda.framework.Extensions
{

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services to the service collection
    /// </summary>
    public static IServiceCollection AddWakandaFrameworkCore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped(
            typeof(IBaseRepository<,,,>),
            typeof(BaseRepository<,,,>)
        );
        services.AddScoped(
            typeof(IBaseService<,>),
            typeof(BaseService<,,,>)
        );

        return services;
    }

    /// <summary>
    /// Adds caching support
    /// </summary>
    public static IServiceCollection AddWakandaFrameworkCaching(this IServiceCollection services, IConfiguration config)
    {
        // Bind settings
        services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
        var cacheConfig = config.GetSection("CacheSettings").Get<CacheSettings>();

        // Choose provider
        if (cacheConfig.Enabled && cacheConfig.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            // Requires a "Redis" connection string
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, NullCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Adds message broker support. Registers the default NullBrokerService.
    /// </summary>
    public static IServiceCollection AddWakandaFrameworkMessageBroker(
        this IServiceCollection services, IConfiguration config)
    {
        // Bind settings
        services.Configure<MessageBrokerSettings>(
            config.GetSection("MessageBrokerSettings"));

        // Default to no-op broker
        services.TryAddSingleton<IBrokerService, NullBrokerService>();

        return services;
    }

    /// <summary>
    /// Registers Kafka as the message broker implementation. Should be called after AddWakandaFrameworkMessageBroker.
    /// </summary>
    public static IServiceCollection AddWakandaFrameworkKafkaBroker(
        this IServiceCollection services)
    {
        services.AddSingleton<IBrokerService, KafkaBrokerService>();
        return services;
    }
}

}
