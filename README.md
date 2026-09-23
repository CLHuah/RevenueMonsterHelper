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
using RevenueMonsterLibrary.Helper;

// Generate signature for API requests
string signature = SignatureHelper.GenerateSignature(
    data: payload,
    method: "POST", 
    nonceStr: RandomString.GenerateRandomString(32),
    privateKey: "YOUR_PRIVATE_KEY",
    requestUrl: "API_ENDPOINT",
    signType: "SHA256",
    timestamp: "TIMESTAMP"
);
```

Verifying Signatures

```cs
using RevenueMonsterLibrary.Helper;

bool isValid = SignatureHelper.VerifySignature(
    data: receivedData,
    method: "POST",
    nonceStr: "RECEIVED_NONCE",
    publicKey: "MERCHANT_PUBLIC_KEY",
    requestUrl: "CALLBACK_URL",
    signType: "SHA256", 
    timestamp: "RECEIVED_TIMESTAMP",
    signature: "RECEIVED_SIGNATURE"
);
```

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
* Newtonsoft.Json 13.0.4 or higher (used by the model attributes)

## Testing
The project includes MSTest unit tests. Run them from the repository root:

```sh
dotnet test RevenueMonsterLibrary.slnx
```
