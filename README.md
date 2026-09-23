# RevenueMonsterHelper

[![CI](https://github.com/CLHuah/RevenueMonsterHelper/actions/workflows/ci.yml/badge.svg)](https://github.com/CLHuah/RevenueMonsterHelper/actions/workflows/ci.yml)

A .NET library for integrating with Revenue Monster's payment API services. It provides an API client, request signing, webhook verification and the request/response models.

## Features

- `RevenueMonsterClient`: OAuth access tokens (cached and renewed automatically), request signing, and typed calls for online checkout, QuickPay, transactions, refunds, reversals, transaction QR codes and FPX banks
- Request signing and webhook verification that match Revenue Monster's canonical form
- RSA key handling (PEM format support)
- Random nonce generation and Base64 utilities
- Request/response models and constants

## Installation

The package is not published to NuGet. Reference the project directly:

```sh
dotnet add reference path/to/RevenueMonsterHelper/RevenueMonsterLibrary.csproj
```

Or build the DLL and reference it:

```sh
dotnet build RevenueMonsterLibrary.slnx -c Release
# Output: RevenueMonsterHelper/bin/Release/net10.0/RevenueMonsterLibrary.dll
```

## Usage

### Calling the API

```cs
using RevenueMonsterLibrary.Client;
using RevenueMonsterLibrary.Constants;
using RevenueMonsterLibrary.Model;

var options = new RevenueMonsterOptions
{
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = "YOUR_CLIENT_SECRET",
    PrivateKey = File.ReadAllText("private.pem"), // its public key is uploaded to the merchant portal
    Environment = RevenueMonsterEnvironment.Sandbox // the default; use Production to go live
};

var client = new RevenueMonsterClient(new HttpClient(), options);

var checkout = await client.CreateOnlineCheckoutAsync(new WebPayment
{
    storeId = "YOUR_STORE_ID",
    type = PaymentTypes.WebPayment,
    layoutVersion = CheckoutLayoutVersions.V1,
    redirectUrl = "https://example.com/payment/return",
    notifyUrl = "https://example.com/payment/notify",
    order = new Order
    {
        id = "ORDER-1001",
        title = "Order 1001",
        detail = "Order 1001",
        amount = 1000, // in sen
        currencyType = CurrencyTypes.MalaysianRinggit
    }
});

// Send the customer to checkout.item.url
```

With ASP.NET Core, register the options and the client as a typed `HttpClient`:

```cs
builder.Services.AddSingleton(options);
builder.Services.AddHttpClient<RevenueMonsterClient>();
```

| Method | Endpoint | Response (`item`) |
|---|---|---|
| `CreateOnlineCheckoutAsync` | `POST /payment/online` | `WebPaymentResponse` (`Item`) |
| `GetOnlineCheckoutAsync` | `GET /payment/online?checkoutId=` | `OnlineCheckoutResponse` (`OnlineCheckout`) |
| `CreateCheckoutByMethodAsync` | `POST /payment/online/checkout` | `CheckoutByMethodResponse` (`CheckoutByMethodResult`) |
| `GetFpxBanksAsync` | `GET /payment/fpx-bank` | `FpxBankListResponse` (banks by code) |
| `CreateQuickPayAsync` | `POST /payment/quickpay` | `QuickPayResponse` (`PaymentTransaction`) |
| `GetTransactionByIdAsync` | `GET /payment/transaction/{transactionId}` | `TransactionByIdResponse` (`PaymentTransaction`) |
| `GetTransactionByOrderIdAsync` | `GET /payment/transaction/order/{orderId}` | `TransactionByOrderIdResponse` (`PaymentTransaction`) |
| `RefundAsync` | `POST /payment/refund` | `RefundResponse` (`PaymentTransaction`) |
| `ReverseAsync` | `POST /payment/reverse` | `ReverseResponse` (`PaymentTransaction`) |
| `CreateTransactionQrCodeAsync` | `POST /payment/transaction/qrcode` | `CreateTransactionQrCodeResponse` (`TransactionQrCode`) |
| `GetTransactionQrCodeAsync` | `GET /payment/transaction/qrcode/{code}` | `TransactionQrCodeResponse` (`TransactionQrCode`) |
| `GetSuccessfulTransactionsByQrCodeAsync` | `GET /payment/transaction/qrcode/{code}/transactions` | `QrCodeTransactionsResponse` (`items`: `PaymentTransaction`) |

Every response has `code`, `error` and `item` (or `items`), from the generic `ApiResponse<T>` / `ApiListResponse<T>`. `TransactionQuickPay` and `PaymentTransactionByOrderID` are the earlier names of `PaymentTransaction` and `TransactionByOrderIdResponse`; they still work but are marked obsolete.

Access tokens are requested on first use, cached, and renewed 60 seconds before they expire (with the refresh token when possible). Clients with the same credentials share the cached token, so a client created per request does not request a new token each time. Call `GetAccessTokenAsync()` if you need a token for your own requests.

Errors are thrown as `RevenueMonsterException`:

```cs
try
{
    await client.RefundAsync(refundRequest);
}
catch (RevenueMonsterException ex)
{
    // ex.StatusCode, ex.ErrorCode, ex.Error?.message, ex.Error?.debug, ex.ResponseBody
}
```

### Verifying Webhooks

Verify against the raw request body before deserializing it. A model drops properties it does not declare, which makes the signature check fail.

```cs
using Newtonsoft.Json;
using RevenueMonsterLibrary.Helper;
using RevenueMonsterLibrary.Model;

app.MapPost("/payment/notify", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    var isValid = SignatureHelper.VerifyWebhook(
        rawBody,
        method: request.Method,
        requestUrl: "https://example.com/payment/notify", // the notify URL sent to Revenue Monster
        nonceStr: request.Headers["X-Nonce-Str"],
        timestamp: request.Headers["X-Timestamp"],
        signatureHeader: request.Headers["X-Signature"],
        publicKey: revenueMonsterPublicKey); // Revenue Monster's public key from the merchant portal

    if (!isValid) return Results.Unauthorized();

    var notify = JsonConvert.DeserializeObject<Notify>(rawBody);
    // ...
    return Results.Ok();
});
```

### Signing Requests Yourself

When you send requests without `RevenueMonsterClient`, use `SignRequest` and send its `Body` unchanged, so the body always matches the signature:

```cs
using RevenueMonsterLibrary.Helper;

var signed = SignatureHelper.SignRequest(payload, "POST", requestUrl, privateKey);

// Body:     signed.Body (JSON)
// Headers:  Authorization: Bearer {accessToken}
//           X-Nonce-Str:   signed.NonceStr
//           X-Timestamp:   signed.Timestamp
//           X-Signature:   signed.SignatureHeader   ("sha256 {signature}")
```

Revenue Monster checks the signature against the body it receives. If you serialize the body yourself instead, use `JsonConvert.SerializeObject`, the serializer used for signing. The lower-level `SignatureHelper.GenerateSignature`, `VerifySignature`, `VerifySignatureFromRawBody` and `GenerateCompactJson` are also available.

### Loading an RSA Key from PEM

```cs
using System.Security.Cryptography;
using RevenueMonsterLibrary.Helper;

// Supports "RSA PRIVATE KEY", "PRIVATE KEY", "PUBLIC KEY" and "RSA PUBLIC KEY" PEM blocks
using RSA rsa = PemKeyHelper.CreateRSAFromPem(File.ReadAllText("private.pem"));
byte[] signed = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
```

### Constants

`RevenueMonsterLibrary.Constants` has the API base URLs (`RevenueMonsterUrls`), `SignTypes`, OAuth `Scopes`, `PaymentTypes`, `CheckoutLayoutVersions`, `CurrencyTypes` and `RefundTypes`.

## Requirements
* .NET 10.0 or higher
* Newtonsoft.Json 13.0.4 or higher (used for signing, the client and the model attributes)

## Testing
The project includes MSTest unit tests. Run them from the repository root:

```sh
dotnet test RevenueMonsterLibrary.slnx
```

CI (`.github/workflows/ci.yml`) builds with warnings as errors and runs the tests on Ubuntu and Windows for every pull request to `master` and every push to `master` and `Development`.
