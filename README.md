# RevenueMonsterHelper

A .NET library for integrating with Revenue Monster's payment API services. This library provides helper functions for authentication, signature generation, and payment processing.

## Features

- Base64 encoding/decoding utilities
- RSA key handling (PEM format support)
- Digital signature generation and verification
- Random nonce generation
- Payment transaction models

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

Generating Signatures

```cs
using Newtonsoft.Json;
using RevenueMonsterLibrary.Helper;

// Generate signature for API requests
string signature = SignatureHelper.GenerateSignature(
    data: payload,
    method: "POST", 
    nonceStr: RandomString.GenerateRandomString(32),
    privateKey: "YOUR_PRIVATE_KEY",
    requestUrl: "API_ENDPOINT",
    signType: "sha256",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
);

// Send the body serialized with JsonConvert (the serializer used for signing),
// or send SignatureHelper.GenerateCompactJson(payload) as the body
string body = JsonConvert.SerializeObject(payload);

// X-Signature header value: "sha256 {signature}"
```

Revenue Monster checks the signature against the body it receives, so the signed data must describe exactly the body you send. The method is lowercased and `signType` must be SHA256 (any casing).

Verifying Webhook Signatures

Verify against the raw request body, before deserializing it. A model drops properties it does not declare, which makes the signature check fail.

```cs
using RevenueMonsterLibrary.Helper;

bool isValid = SignatureHelper.VerifySignatureFromRawBody(
    rawBody: "RAW_REQUEST_BODY",
    method: "POST",
    nonceStr: "X_NONCE_STR_HEADER",
    publicKey: "REVENUE_MONSTER_PUBLIC_KEY",
    requestUrl: "YOUR_NOTIFY_URL",
    signType: "sha256",
    timestamp: "X_TIMESTAMP_HEADER",
    signature: "X_SIGNATURE_HEADER_WITHOUT_SHA256_PREFIX"
);
```

`SignatureHelper.VerifySignature` accepts an object instead of the raw body (plain objects, Newtonsoft `JToken`s and System.Text.Json elements).

Loading an RSA key from PEM

```cs
using System.Security.Cryptography;
using RevenueMonsterLibrary.Helper;

// Supports "RSA PRIVATE KEY", "PRIVATE KEY", "PUBLIC KEY" and "RSA PUBLIC KEY" PEM blocks
using RSA rsa = PemKeyHelper.CreateRSAFromPem(File.ReadAllText("private.pem"));
byte[] signed = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
```

## Requirements
* .NET 10.0 or higher
* Newtonsoft.Json 13.0.4 or higher (used for signing and by the model attributes)

## Testing
The project includes MSTest unit tests. Run them from the repository root:

```sh
dotnet test RevenueMonsterLibrary.slnx
```
