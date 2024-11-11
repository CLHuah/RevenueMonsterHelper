# RevenueMonsterHelper

A .NET library for integrating with Revenue Monster's payment API services. This library provides helper functions for authentication, signature generation, and payment processing.

## Features

- Base64 encoding/decoding utilities
- RSA key handling (PEM format support)
- Digital signature generation and verification
- Payment transaction models

## Installation

Add the library to your .NET project:

```sh
dotnet add package RevenueMonsterLibrary
```

## Usage

Generating Signatures

```cs
using RevenueMonsterLibrary.Helper;

// Generate signature for API requests
string signature = SignatureHelper.GenerateSignature(
    data: payload,
    method: "POST", 
    nonceStr: "RANDOM_STRING",
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

## Requirements
* .NET 8.0 or higher
* Newtonsoft.Json 13.0.3 or higher

## Testing
* The project includes MSTest unit tests. Run tests using: