using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace RevenueMonsterLibrary.Client;

/// <summary>
///     Registers <see cref="RevenueMonsterClient" /> with dependency injection.
/// </summary>
/// <remarks>
///     The client is registered as a typed client from IHttpClientFactory, so it is transient. Inject it into
///     transient or scoped services; a singleton would keep one HttpClient for the app's lifetime and stop the factory
///     from rotating its handler.
/// </remarks>
public static class RevenueMonsterServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <see cref="RevenueMonsterClient" /> with options bound from <paramref name="configuration" />, for
    ///     example <c>builder.Configuration.GetSection("RevenueMonster")</c>.
    /// </summary>
    /// <returns>The client's <see cref="IHttpClientBuilder" />, to add message handlers or change its settings.</returns>
    public static IHttpClientBuilder AddRevenueMonsterClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RevenueMonsterOptions>().Bind(configuration);
        return AddClient(services);
    }

    /// <summary>
    ///     Registers <see cref="RevenueMonsterClient" /> with options set by <paramref name="configure" />.
    /// </summary>
    /// <returns>The client's <see cref="IHttpClientBuilder" />, to add message handlers or change its settings.</returns>
    public static IHttpClientBuilder AddRevenueMonsterClient(this IServiceCollection services,
        Action<RevenueMonsterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<RevenueMonsterOptions>().Configure(configure);
        return AddClient(services);
    }

    private static IHttpClientBuilder AddClient(IServiceCollection services)
    {
        // Options are read each time a client is created, so reloaded configuration applies to later clients
        return services.AddHttpClient<RevenueMonsterClient, RevenueMonsterClient>((httpClient, serviceProvider) =>
            new RevenueMonsterClient(httpClient,
                serviceProvider.GetRequiredService<IOptionsMonitor<RevenueMonsterOptions>>().CurrentValue,
                serviceProvider.GetService<TimeProvider>()));
    }
}