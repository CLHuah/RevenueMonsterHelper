using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevenueMonsterLibrary.Client;
using RevenueMonsterLibrary.Tests.Fakes;
using System.Net;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class RevenueMonsterServiceCollectionExtensionsTests
{
    private const string TokenResponse =
        """{"accessToken":"access-1","expiresIn":7200,"refreshToken":"refresh-1","tokenType":"Bearer"}""";

    private static readonly DateTimeOffset StartTime = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task AddRevenueMonsterClient_ClientsFromSameProvider_ShareAccessToken()
    {
        // Arrange
        var handler = CreateHandler();
        var services = new ServiceCollection();
        var clientId = Guid.NewGuid().ToString("N");
        services.AddRevenueMonsterClient(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = "secret";
                options.PrivateKey = TestKeys.PrivateKey;
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using var serviceProvider = services.BuildServiceProvider();

        // Act - every resolve creates a new client, as a typed client is transient
        await serviceProvider.GetRequiredService<RevenueMonsterClient>().GetTransactionByIdAsync("t1");
        await serviceProvider.GetRequiredService<RevenueMonsterClient>().GetTransactionByIdAsync("t2");

        // Assert
        Assert.HasCount(1, handler.TokenRequests);
        Assert.HasCount(2, handler.ApiRequests);
    }

    [TestMethod]
    public void AddRevenueMonsterClient_MissingClientSecret_ThrowsWhenResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRevenueMonsterClient(options =>
        {
            options.ClientId = Guid.NewGuid().ToString("N");
            options.PrivateKey = TestKeys.PrivateKey;
        });
        using var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => serviceProvider.GetRequiredService<RevenueMonsterClient>());
    }

    [TestMethod]
    public async Task AddRevenueMonsterClient_WithConfiguration_BindsOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                // A unique client ID per test keeps the shared token cache separate between tests
                ["RevenueMonster:ClientId"] = Guid.NewGuid().ToString("N"),
                ["RevenueMonster:ClientSecret"] = "secret",
                ["RevenueMonster:PrivateKey"] = TestKeys.PrivateKey,
                ["RevenueMonster:Environment"] = "Production"
            })
            .Build();
        var handler = CreateHandler();
        var services = new ServiceCollection();
        services.AddRevenueMonsterClient(configuration.GetSection("RevenueMonster"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var serviceProvider = services.BuildServiceProvider();

        // Act
        await serviceProvider.GetRequiredService<RevenueMonsterClient>().GetTransactionByIdAsync("t1");

        // Assert
        Assert.AreEqual("https://oauth.revenuemonster.my/v1/token", handler.TokenRequests.Single().Uri.AbsoluteUri);
        Assert.AreEqual("https://open.revenuemonster.my/v3/payment/transaction/t1",
            handler.ApiRequests.Single().Uri.AbsoluteUri);
    }

    [TestMethod]
    public async Task AddRevenueMonsterClient_WithConfigure_UsesOptionsAndRegisteredTimeProvider()
    {
        // Arrange
        var handler = CreateHandler();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new ManualTimeProvider(StartTime));
        services.AddRevenueMonsterClient(options =>
            {
                options.ClientId = Guid.NewGuid().ToString("N");
                options.ClientSecret = "secret";
                options.PrivateKey = TestKeys.PrivateKey;
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var serviceProvider = services.BuildServiceProvider();

        // Act
        await serviceProvider.GetRequiredService<RevenueMonsterClient>().GetTransactionByIdAsync("t1");

        // Assert
        var request = handler.ApiRequests.Single();
        Assert.AreEqual("https://sb-open.revenuemonster.my/v3/payment/transaction/t1", request.Uri.AbsoluteUri);
        Assert.AreEqual(StartTime.ToUnixTimeSeconds().ToString(), request.Headers["X-Timestamp"]);
    }

    private static FakeHttpMessageHandler CreateHandler()
    {
        return new FakeHttpMessageHandler(request => FakeHttpMessageHandler.Json(HttpStatusCode.OK,
            request.IsTokenRequest ? TokenResponse : """{"item":{"transactionId":"t1"},"code":"SUCCESS"}"""));
    }
}