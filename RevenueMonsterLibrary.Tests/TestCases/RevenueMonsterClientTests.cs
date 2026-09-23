using System.Net;
using RevenueMonsterLibrary.Client;
using RevenueMonsterLibrary.Model;
using RevenueMonsterLibrary.Tests.Fakes;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class RevenueMonsterClientTests
{
    private const string FirstTokenResponse =
        """{"accessToken":"access-1","expiresIn":7200,"refreshToken":"refresh-1","tokenType":"Bearer"}""";

    private const string SecondTokenResponse =
        """{"accessToken":"access-2","expiresIn":7200,"refreshToken":"refresh-2","tokenType":"Bearer"}""";

    private static readonly DateTimeOffset StartTime = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task GetAccessTokenAsync_FirstCall_RequestsTokenWithClientCredentials()
    {
        // Arrange
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => Ok(FirstTokenResponse));

        // Act
        var accessToken = await client.GetAccessTokenAsync();

        // Assert
        var request = handler.Requests.Single();
        Assert.AreEqual("access-1", accessToken);
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://sb-oauth.revenuemonster.my/v1/token", request.Uri.AbsoluteUri);
        Assert.AreEqual($"Basic {Encode.Base64Encode($"{options.ClientId}:secret")}", request.Headers["Authorization"]);
        Assert.AreEqual("""{"grantType":"client_credentials"}""", request.Body);
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_SecondClientWithSameCredentials_ReusesCachedToken()
    {
        // Arrange
        var options = CreateOptions();
        var time = new ManualTimeProvider(StartTime);
        var (first, handler) = CreateClient(options, _ => Ok(FirstTokenResponse), time);
        var second = new RevenueMonsterClient(new HttpClient(handler), options, time);

        // Act
        await first.GetAccessTokenAsync();
        time.Advance(TimeSpan.FromHours(1));
        var accessToken = await second.GetAccessTokenAsync();

        // Assert
        Assert.AreEqual("access-1", accessToken);
        Assert.HasCount(1, handler.TokenRequests);
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_NearExpiry_RenewsWithRefreshToken()
    {
        // Arrange
        var time = new ManualTimeProvider(StartTime);
        var responses = new Queue<string>([FirstTokenResponse, SecondTokenResponse]);
        var (client, handler) = CreateClient(CreateOptions(), _ => Ok(responses.Dequeue()), time);
        await client.GetAccessTokenAsync();

        // Act - 60 seconds before expiry the token is renewed
        time.Advance(TimeSpan.FromSeconds(7200 - 60));
        var accessToken = await client.GetAccessTokenAsync();

        // Assert
        Assert.AreEqual("access-2", accessToken);
        Assert.AreEqual("""{"grantType":"refresh_token","refreshToken":"refresh-1"}""", handler.Requests[1].Body);
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_RefreshTokenRejected_FallsBackToClientCredentials()
    {
        // Arrange
        var time = new ManualTimeProvider(StartTime);
        var responses = new Queue<HttpResponseMessage>([
            Ok(FirstTokenResponse),
            FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest,
                """{"error":{"code":"INVALID_GRANT","message":"refresh token expired"}}"""),
            Ok(SecondTokenResponse)
        ]);
        var (client, handler) = CreateClient(CreateOptions(), _ => responses.Dequeue(), time);
        await client.GetAccessTokenAsync();
        time.Advance(TimeSpan.FromHours(2));

        // Act
        var accessToken = await client.GetAccessTokenAsync();

        // Assert
        Assert.AreEqual("access-2", accessToken);
        Assert.AreEqual("""{"grantType":"client_credentials"}""", handler.Requests[2].Body);
    }

    [TestMethod]
    public async Task CreateOnlineCheckoutAsync_SendsSignedRequestThatVerifies()
    {
        // Arrange
        var time = new ManualTimeProvider(StartTime);
        var (client, handler) = CreateClient(CreateOptions(), request => request.IsTokenRequest
            ? Ok(FirstTokenResponse)
            : Ok("""{"item":{"checkoutId":"c1","url":"https://pay.example/c1"},"code":"SUCCESS"}"""), time);
        var payment = new WebPayment
        {
            storeId = "1",
            type = "WEB_PAYMENT",
            order = new Order { id = "o1", title = "A&B", amount = 100, currencyType = "MYR" }
        };

        // Act
        var response = await client.CreateOnlineCheckoutAsync(payment);

        // Assert - request line, headers and body
        var request = handler.ApiRequests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://sb-open.revenuemonster.my/v3/payment/online", request.Uri.AbsoluteUri);
        Assert.AreEqual("Bearer access-1", request.Headers["Authorization"]);
        Assert.AreEqual(StartTime.ToUnixTimeSeconds().ToString(), request.Headers["X-Timestamp"]);
        Assert.AreEqual(32, request.Headers["X-Nonce-Str"].Length);
        Assert.AreEqual(SignatureHelper.GenerateCompactJson(payment), request.Body);

        // Assert - the signature matches what was sent
        Assert.IsTrue(SignatureHelper.VerifyWebhook(request.Body, "POST", request.Uri.AbsoluteUri,
            request.Headers["X-Nonce-Str"], request.Headers["X-Timestamp"], request.Headers["X-Signature"],
            TestKeys.PublicKey));

        // Assert - the response is mapped
        Assert.AreEqual("c1", response.item.checkoutId);
    }

    [TestMethod]
    public async Task GetTransactionByOrderIdAsync_EscapesOrderIdAndKeepsDatesAsSent()
    {
        // Arrange
        var (client, handler) = CreateClient(CreateOptions(), request => request.IsTokenRequest
            ? Ok(FirstTokenResponse)
            : Ok("""{"item":{"transactionId":"t1","createdAt":"2023-01-01T00:00:00.000Z"},"code":"SUCCESS"}"""));

        // Act
        var response = await client.GetTransactionByOrderIdAsync("A/B 1");

        // Assert
        var request = handler.ApiRequests.Single();
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual("https://sb-open.revenuemonster.my/v3/payment/transaction/order/A%2FB%201",
            request.Uri.AbsoluteUri);
        Assert.IsNull(request.Body);
        Assert.IsTrue(SignatureHelper.VerifyWebhook(null, "GET", request.Uri.AbsoluteUri,
            request.Headers["X-Nonce-Str"], request.Headers["X-Timestamp"], request.Headers["X-Signature"],
            TestKeys.PublicKey));
        Assert.AreEqual("t1", response.item.transactionId);
        Assert.AreEqual("2023-01-01T00:00:00.000Z", response.item.createdAt);
    }

    [TestMethod]
    public async Task ApiRequest_ErrorResponse_ThrowsRevenueMonsterException()
    {
        // Arrange
        var (client, _) = CreateClient(CreateOptions(), request => request.IsTokenRequest
            ? Ok(FirstTokenResponse)
            : FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest,
                """{"error":{"code":"INVALID_REQUEST","message":"Invalid order","debug":"order.amount"}}"""));

        // Act
        var exception = await Assert.ThrowsExactlyAsync<RevenueMonsterException>(() =>
            client.RefundAsync(new RefundRequest { transactionId = "t1" }));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.AreEqual("INVALID_REQUEST", exception.ErrorCode);
        Assert.AreEqual("Invalid order", exception.Error.message);
        Assert.AreEqual("order.amount", exception.Error.debug);
    }

    [TestMethod]
    public async Task ApiRequest_NonJsonResponse_ThrowsWithStatusCodeAndBody()
    {
        // Arrange
        var (client, _) = CreateClient(CreateOptions(), request => request.IsTokenRequest
            ? Ok(FirstTokenResponse)
            : new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>502</html>") });

        // Act
        var exception = await Assert.ThrowsExactlyAsync<RevenueMonsterException>(() =>
            client.GetTransactionByIdAsync("t1"));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.IsNull(exception.Error);
        Assert.AreEqual("<html>502</html>", exception.ResponseBody);
    }

    [TestMethod]
    public async Task ApiRequest_Unauthorized_DropsCachedTokenForNextRequest()
    {
        // Arrange - the first API call is rejected, the second succeeds
        var apiResponses = new Queue<HttpResponseMessage>([
            FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized,
                """{"error":{"code":"UNAUTHORIZED","message":"token revoked"}}"""),
            Ok("""{"item":{"transactionId":"t1"},"code":"SUCCESS"}""")
        ]);
        var tokenResponses = new Queue<string>([FirstTokenResponse, SecondTokenResponse]);
        var (client, handler) = CreateClient(CreateOptions(), request => request.IsTokenRequest
            ? Ok(tokenResponses.Dequeue())
            : apiResponses.Dequeue());

        // Act
        await Assert.ThrowsExactlyAsync<RevenueMonsterException>(() => client.GetTransactionByIdAsync("t1"));
        await client.GetTransactionByIdAsync("t1");

        // Assert
        Assert.HasCount(2, handler.TokenRequests);
        Assert.AreEqual("Bearer access-2", handler.ApiRequests[1].Headers["Authorization"]);
    }

    [TestMethod]
    public async Task ProductionEnvironment_UsesProductionUrls()
    {
        // Arrange
        var options = CreateOptions();
        options.Environment = RevenueMonsterEnvironment.Production;
        var (client, handler) = CreateClient(options, request => request.IsTokenRequest
            ? Ok(FirstTokenResponse)
            : Ok("""{"item":{},"code":"SUCCESS"}"""));

        // Act
        await client.GetFpxBanksAsync();

        // Assert
        Assert.AreEqual("https://oauth.revenuemonster.my/v1/token", handler.TokenRequests.Single().Uri.AbsoluteUri);
        Assert.AreEqual("https://open.revenuemonster.my/v3/payment/fpx-bank",
            handler.ApiRequests.Single().Uri.AbsoluteUri);
    }

    [TestMethod]
    public void Constructor_MissingPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateOptions();
        options.PrivateKey = "";

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() => new RevenueMonsterClient(new HttpClient(), options));
    }

    private static RevenueMonsterOptions CreateOptions()
    {
        return new RevenueMonsterOptions
        {
            // A unique client ID per test keeps the shared token cache separate between tests
            ClientId = Guid.NewGuid().ToString("N"),
            ClientSecret = "secret",
            PrivateKey = TestKeys.PrivateKey
        };
    }

    private static (RevenueMonsterClient Client, FakeHttpMessageHandler Handler) CreateClient(
        RevenueMonsterOptions options, Func<RecordedRequest, HttpResponseMessage> responder,
        TimeProvider? timeProvider = null)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var client = new RevenueMonsterClient(new HttpClient(handler), options,
            timeProvider ?? new ManualTimeProvider(StartTime));
        return (client, handler);
    }

    private static HttpResponseMessage Ok(string json)
    {
        return FakeHttpMessageHandler.Json(HttpStatusCode.OK, json);
    }
}
