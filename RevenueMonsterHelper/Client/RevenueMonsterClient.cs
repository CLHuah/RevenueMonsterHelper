using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevenueMonsterLibrary.Helper;
using RevenueMonsterLibrary.Model;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RevenueMonsterLibrary.Client;

/// <summary>
///     Calls the Revenue Monster Open API. It obtains and renews access tokens, signs every request and turns error
///     responses into <see cref="RevenueMonsterException" />s.
/// </summary>
/// <remarks>
///     Thread-safe. The options are read once, when the client is created. Access tokens are shared by every client
///     with the same credentials and environment, so creating a client per request (for example as a typed client
///     from IHttpClientFactory) does not request a new token each time.
/// </remarks>
public sealed class RevenueMonsterClient
{
    // Length of the nonce sent with every API request
    private const int NonceLength = 32;

    // Access tokens shared by every client with the same OAuth URL and credentials
    private static readonly ConcurrentDictionary<string, TokenCache> TokenCaches = new();

    private readonly string _apiBaseUrl;
    private readonly string _basicCredentials;
    private readonly HttpClient _httpClient;
    private readonly string _oauthBaseUrl;
    private readonly string _privateKey;
    private readonly TimeProvider _timeProvider;
    private readonly TokenCache _tokenCache;
    private readonly TimeSpan _tokenRenewalMargin;

    /// <summary>
    ///     Creates a client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send requests.</param>
    /// <param name="options">Credentials, signing key and environment.</param>
    /// <param name="timeProvider">
    ///     Source of the current time, used for request timestamps and token expiry. Defaults to the system clock.
    /// </param>
    /// <remarks>
    ///     With dependency injection, register the client with <c>services.AddRevenueMonsterClient(...)</c> so the
    ///     HttpClient comes from IHttpClientFactory. Without it, create one client and reuse it: creating an
    ///     HttpClient per request can exhaust sockets under load.
    /// </remarks>
    public RevenueMonsterClient(HttpClient httpClient, RevenueMonsterOptions options, TimeProvider timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ClientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ClientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PrivateKey);

        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _apiBaseUrl = options.ApiBaseUrl;
        _oauthBaseUrl = options.OAuthBaseUrl;
        _privateKey = options.PrivateKey;
        _tokenRenewalMargin = options.TokenRenewalMargin;
        _basicCredentials = Encode.Base64Encode($"{options.ClientId}:{options.ClientSecret}");
        _tokenCache = TokenCaches.GetOrAdd($"{_oauthBaseUrl}\n{options.ClientId}\n{options.ClientSecret}",
            _ => new TokenCache());
    }

    /// <summary>
    ///     An access token, its refresh token and when to renew it.
    /// </summary>
    private sealed record CachedToken(string AccessToken, string RefreshToken, DateTimeOffset RenewAt);

    /// <summary>
    ///     The access token for one set of credentials, shared by every client using them.
    /// </summary>
    private sealed class TokenCache
    {
        public readonly SemaphoreSlim Lock = new(1, 1);
        private volatile CachedToken _token;

        public CachedToken Token
        {
            get => _token;
            set => _token = value;
        }

        /// <summary>
        ///     Drops the cached token if it is still <paramref name="accessToken" />.
        /// </summary>
        public void Invalidate(string accessToken)
        {
            var current = _token;
            if (current?.AccessToken == accessToken) Interlocked.CompareExchange(ref _token, null, current);
        }
    }

    #region Access tokens

    /// <summary>
    ///     Returns a valid access token, requesting or renewing one when needed.
    /// </summary>
    /// <remarks>
    ///     A token is renewed <see cref="RevenueMonsterOptions.TokenRenewalMargin" /> before it expires, with its
    ///     refresh token when possible and with the client credentials otherwise.
    /// </remarks>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Use the cached token while it is still valid
        var cached = _tokenCache.Token;
        if (cached is not null && _timeProvider.GetUtcNow() < cached.RenewAt) return cached.AccessToken;

        await _tokenCache.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Another caller may have renewed the token while this one waited
            cached = _tokenCache.Token;
            if (cached is not null && _timeProvider.GetUtcNow() < cached.RenewAt) return cached.AccessToken;

            ClientCredentials token = null;

            if (!string.IsNullOrEmpty(cached?.RefreshToken))
                try
                {
                    token = await GetAccessTokenByRefreshTokenAsync(cached.RefreshToken, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (RevenueMonsterException)
                {
                    // The refresh token expired or was revoked; fall back to the client credentials below
                }

            token ??= await GetAccessTokenByClientCredentialsAsync(cancellationToken).ConfigureAwait(false);

            _tokenCache.Token = new CachedToken(token.accessToken, token.refreshToken ?? cached?.RefreshToken,
                _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(token.expiresIn) - _tokenRenewalMargin);

            return token.accessToken;
        }
        finally
        {
            _tokenCache.Lock.Release();
        }
    }

    /// <summary>
    ///     Requests a new access token with the client credentials. The token is not cached; use
    ///     <see cref="GetAccessTokenAsync" /> for that.
    /// </summary>
    public Task<ClientCredentials> GetAccessTokenByClientCredentialsAsync(CancellationToken cancellationToken = default)
    {
        return RequestTokenAsync(new { grantType = "client_credentials" }, cancellationToken);
    }

    /// <summary>
    ///     Requests a new access token with a refresh token. The token is not cached; use
    ///     <see cref="GetAccessTokenAsync" /> for that.
    /// </summary>
    public Task<ClientCredentials> GetAccessTokenByRefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return RequestTokenAsync(new { grantType = "refresh_token", refreshToken }, cancellationToken);
    }

    #endregion

    #region Online checkout

    /// <summary>
    ///     Creates an online checkout and returns its checkout ID and payment page URL.
    /// </summary>
    public Task<WebPaymentResponse> CreateOnlineCheckoutAsync(WebPayment request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendApiRequestAsync<WebPaymentResponse>(HttpMethod.Post, "/payment/online", request, cancellationToken);
    }

    /// <summary>
    ///     Gets an online checkout and its current state.
    /// </summary>
    public Task<OnlineCheckoutResponse> GetOnlineCheckoutAsync(string checkoutId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkoutId);

        return SendApiRequestAsync<OnlineCheckoutResponse>(HttpMethod.Get,
            $"/payment/online?checkoutId={Uri.EscapeDataString(checkoutId)}", null, cancellationToken);
    }

    /// <summary>
    ///     Starts payment of an online checkout with a specific payment method, for example to get a wallet QR code.
    /// </summary>
    public Task<CheckoutByMethodResponse> CreateCheckoutByMethodAsync(CheckoutByMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendApiRequestAsync<CheckoutByMethodResponse>(HttpMethod.Post, "/payment/online/checkout", request,
            cancellationToken);
    }

    /// <summary>
    ///     Gets the banks available for FPX payments, keyed by bank code.
    /// </summary>
    public Task<FpxBankListResponse> GetFpxBanksAsync(CancellationToken cancellationToken = default)
    {
        return SendApiRequestAsync<FpxBankListResponse>(HttpMethod.Get, "/payment/fpx-bank", null, cancellationToken);
    }

    #endregion

    #region QuickPay and transactions

    /// <summary>
    ///     Charges a customer by the payment code shown in their wallet app.
    /// </summary>
    public Task<QuickPayResponse> CreateQuickPayAsync(QuickPay request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendApiRequestAsync<QuickPayResponse>(HttpMethod.Post, "/payment/quickpay", request, cancellationToken);
    }

    /// <summary>
    ///     Gets a transaction by its transaction ID.
    /// </summary>
    public Task<TransactionByIdResponse> GetTransactionByIdAsync(string transactionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        return SendApiRequestAsync<TransactionByIdResponse>(HttpMethod.Get,
            $"/payment/transaction/{Uri.EscapeDataString(transactionId)}", null, cancellationToken);
    }

    /// <summary>
    ///     Gets a transaction by your order ID.
    /// </summary>
    public Task<TransactionByOrderIdResponse> GetTransactionByOrderIdAsync(string orderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        return SendApiRequestAsync<TransactionByOrderIdResponse>(HttpMethod.Get,
            $"/payment/transaction/order/{Uri.EscapeDataString(orderId)}", null, cancellationToken);
    }

    /// <summary>
    ///     Refunds a transaction: returns the funds to the customer, before or after the settlement date depending on
    ///     the payment provider.
    /// </summary>
    public Task<RefundResponse> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendApiRequestAsync<RefundResponse>(HttpMethod.Post, "/payment/refund", request, cancellationToken);
    }

    /// <summary>
    ///     Reverses (cancels) a transaction by your order ID. Only possible within a short window after the
    ///     transaction, such as 15 minutes; meant for cases like a dropped connection, to prevent double charges.
    /// </summary>
    public Task<ReverseResponse> ReverseAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        return SendApiRequestAsync<ReverseResponse>(HttpMethod.Post, "/payment/reverse",
            new ReverseRequest { orderId = orderId }, cancellationToken);
    }

    #endregion

    #region Transaction QR codes

    /// <summary>
    ///     Creates a QR code that customers scan to pay.
    /// </summary>
    public Task<CreateTransactionQrCodeResponse> CreateTransactionQrCodeAsync(TransactionQrCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendApiRequestAsync<CreateTransactionQrCodeResponse>(HttpMethod.Post, "/payment/transaction/qrcode",
            request, cancellationToken);
    }

    /// <summary>
    ///     Gets a transaction QR code by its code.
    /// </summary>
    public Task<TransactionQrCodeResponse> GetTransactionQrCodeAsync(string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return SendApiRequestAsync<TransactionQrCodeResponse>(HttpMethod.Get,
            $"/payment/transaction/qrcode/{Uri.EscapeDataString(code)}", null, cancellationToken);
    }

    /// <summary>
    ///     Gets the successful transactions paid through a transaction QR code.
    /// </summary>
    public Task<QrCodeTransactionsResponse> GetSuccessfulTransactionsByQrCodeAsync(string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var filter = Uri.EscapeDataString("{\"status\":\"SUCCESS\"}");
        return SendApiRequestAsync<QrCodeTransactionsResponse>(HttpMethod.Get,
            $"/payment/transaction/qrcode/{Uri.EscapeDataString(code)}/transactions?filter={filter}", null,
            cancellationToken);
    }

    #endregion

    #region Sending

    /// <summary>
    ///     Sends a signed Open API request with the current access token.
    /// </summary>
    private async Task<T> SendApiRequestAsync<T>(HttpMethod method, string pathAndQuery, object body,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        // Sign exactly the URL and body that are sent
        var requestUri = new Uri(_apiBaseUrl + pathAndQuery);
        var signed = SignatureHelper.SignRequest(body, method.Method, requestUri.AbsoluteUri, _privateKey,
            RandomString.GenerateRandomString(NonceLength),
            _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Nonce-Str", signed.NonceStr);
        request.Headers.Add("X-Timestamp", signed.Timestamp);
        request.Headers.Add("X-Signature", signed.SignatureHeader);
        if (signed.Body is not null)
            request.Content = new StringContent(signed.Body, Encoding.UTF8, "application/json");

        try
        {
            return (await SendAsync<T>(request, cancellationToken).ConfigureAwait(false)).Result;
        }
        catch (RevenueMonsterException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The token was rejected before its expiry; drop it so the next request gets a new one
            _tokenCache.Invalidate(accessToken);
            throw;
        }
    }

    /// <summary>
    ///     Requests an access token from the OAuth API.
    /// </summary>
    private async Task<ClientCredentials> RequestTokenAsync(object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_oauthBaseUrl}/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basicCredentials);
        request.Content = new StringContent(SignatureHelper.GenerateCompactJson(body), Encoding.UTF8,
            "application/json");

        var (token, statusCode, responseBody) =
            await SendAsync<ClientCredentials>(request, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(token.accessToken))
            throw new RevenueMonsterException("Revenue Monster returned a token response without an access token.",
                statusCode, null, responseBody);

        return token;
    }

    /// <summary>
    ///     Sends a request and maps the JSON response onto <typeparamref name="T" />.
    /// </summary>
    /// <exception cref="RevenueMonsterException">
    ///     Thrown when the response contains an error, has a non-success status code or is not a JSON object.
    /// </exception>
    private async Task<(T Result, HttpStatusCode StatusCode, string Body)> SendAsync<T>(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var statusCode = response.StatusCode;

        // Read the body without altering values, so dates stay exactly as sent
        JToken json;
        try
        {
            json = RawJson.Parse(body);
        }
        catch (JsonReaderException ex)
        {
            throw new RevenueMonsterException(
                $"Revenue Monster returned HTTP {(int)statusCode} with a response that is not JSON.", statusCode, null,
                body, ex);
        }

        // An error object means the request failed, whatever the status code
        if (json is JObject responseObject && responseObject["error"] is JObject errorJson)
        {
            var error = errorJson.ToObject<Error>(RawJson.Serializer);
            throw new RevenueMonsterException($"Revenue Monster returned error {error.code}: {error.message}",
                statusCode, error, body);
        }

        if (!response.IsSuccessStatusCode)
            throw new RevenueMonsterException($"Revenue Monster returned HTTP {(int)statusCode}.", statusCode, null,
                body);

        if (json is not JObject)
            throw new RevenueMonsterException("Revenue Monster returned a response that is not a JSON object.",
                statusCode, null, body);

        return (json.ToObject<T>(RawJson.Serializer), statusCode, body);
    }

    #endregion
}