using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RevenueMonsterLibrary.Helper;

public static class SignatureHelper
{
    // Defines the only supported signature algorithm type
    private const string SupportedSignType = "SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Skip null properties
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Produce compact JSON without whitespace
        WriteIndented = false,
        
        // Prevent escaping of special characters
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    ///     Generates compact JSON representation of the provided object after sorting its properties.
    /// </summary>
    /// <param name="data">The object to be converted to compact JSON.</param>
    /// <returns>Compact JSON string.</returns>
    public static string GenerateCompactJson(object data)
    {
        // Validate input - throw if data is null
        ArgumentNullException.ThrowIfNull(data);

        // Convert object to JSON string using configured options
        var jsonString = JsonSerializer.Serialize(data, JsonOptions);

        // Parse JSON string into document for manipulation
        using var document = JsonDocument.Parse(jsonString);

        // Sort all properties recursively starting from root
        var sortedJson = SortJsonNode(document.RootElement);

        // Convert back to string and escape special characters
        return EscapeSpecialCharacters(sortedJson.ToJsonString(JsonOptions));
    }

    /// <summary>
    ///     Generates a digital signature based on the provided data object and parameters.
    /// </summary>
    /// <param name="data">The data object to be included in the signature.</param>
    /// <param name="method">The HTTP method used in the request.</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="privateKey">The private key used for signing.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type (must be SHA256).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(object data, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Validate that signature type is SHA256
        ValidateSignType(signType);

        // Convert object to compact JSON if not null
        var compactJson = data is null ? null : GenerateCompactJson(data);

        // Generate signature using core method with built input string
        return GenerateSignatureCore(
            BuildSignatureInput(compactJson, method, nonceStr, requestUrl, signType, timestamp), privateKey);
    }

    /// <summary>
    ///     Generates a digital signature based on the provided compact JSON and parameters.
    /// </summary>
    /// <param name="compactJson">The compact JSON representation of the data.</param>
    /// <param name="method">The HTTP method used in the request.</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="privateKey">The private key used for signing.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type (must be SHA256).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(string compactJson, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Validate that signature type is SHA256
        ValidateSignType(signType);

        // Generate signature using core method with built input string
        return GenerateSignatureCore(
            BuildSignatureInput(compactJson, method, nonceStr, requestUrl, signType, timestamp), privateKey);
    }

    /// <summary>
    ///     Verifies a digital signature using RSA-SHA256 with PKCS1 padding.
    /// </summary>
    /// <param name="data">The data to be included in the signature verification.</param>
    /// <param name="method">The HTTP method used in the request.</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="publicKey">The RSA public key in PEM format for verification.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature algorithm type (must be SHA256).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <param name="signature">The Base64-encoded signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(object data, string method, string nonceStr, string publicKey, string requestUrl,
        string signType, string timestamp, string signature)
    {
        // Ensure signature type is SHA256
        ValidateSignType(signType);

        // Convert data object to compact JSON if not null
        var compactJson = data is null ? null : GenerateCompactJson(data);

        // Create standardized string for signature verification
        var signatureInput = BuildSignatureInput(compactJson, method, nonceStr, requestUrl, signType, timestamp);

        // Create RSA provider from public key PEM
        using var provider = PemKeyHelper.CreateRSAFromPem(publicKey);

        // Verify signature using RSA-SHA256 with PKCS1 padding
        return provider.VerifyData(Encoding.UTF8.GetBytes(signatureInput), // Convert input to bytes
            Convert.FromBase64String(signature), // Decode base64 signature
            HashAlgorithmName.SHA256, // Use SHA256 algorithm
            RSASignaturePadding.Pkcs1); // Use PKCS1 padding
    }

    /// <summary>
    ///     Builds the standardized input string used for signature generation and verification.
    /// </summary>
    /// <param name="compactJson">The compact JSON representation of the data (can be null).</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature type.</param>
    /// <param name="timestamp">The request timestamp.</param>
    /// <returns>A concatenated string of all parameters in a specific order.</returns>
    /// <remarks>
    ///     The returned string format is:
    ///     "data={base64_encoded_json}&method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}
    ///     &timestamp={timestamp}"
    ///     If compactJson is null, the data parameter is omitted.
    /// </remarks>
    private static string BuildSignatureInput(string? compactJson, string method, string nonceStr, string requestUrl,
        string signType, string timestamp)
    {
        // Create data component if JSON exists, otherwise empty string
        var dataComponent = compactJson is null ? "" : $"data={Encode.Base64Encode(compactJson)}&";

        // Concatenate all parameters in specific order for consistent signature
        return
            $"{dataComponent}method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";
    }

    /// <summary>
    ///     Creates a new JsonObject with properties sorted alphabetically by name.
    /// </summary>
    /// <param name="element">The JsonElement to sort.</param>
    /// <returns>A new JsonObject with sorted properties.</returns>
    /// <remarks>
    ///     For nested objects, the method recursively sorts their properties as well.
    ///     For non-object values, the original JSON representation is preserved.
    /// </remarks>
    private static JsonObject CreateSortedObject(JsonElement element)
    {
        // Initialize new JSON object for sorted properties
        var sortedObject = new JsonObject();

        // Get all properties and sort them by name
        var properties = element.EnumerateObject().OrderBy(p => p.Name).ToList();

        foreach (var property in properties)
        {
            // Handle nested objects recursively, otherwise parse as regular JSON value
            var value = property.Value.ValueKind == JsonValueKind.Object
                ? SortJsonNode(property.Value) // Recursively sort nested objects
                : JsonNode.Parse(property.Value.GetRawText()); // Parse other values as-is

            // Add property to sorted object
            sortedObject.Add(property.Name, value);
        }

        return sortedObject;
    }

    /// <summary>
    ///     Escapes special characters in the input string by converting them to their Unicode representations.
    /// </summary>
    /// <param name="input">The input string containing potential special characters.</param>
    /// <returns>A string with special characters escaped to Unicode format.</returns>
    /// <remarks>
    ///     Converts:
    ///     - '&lt;' to '\u003c'
    ///     - '&gt;' to '\u003e'
    ///     - '&amp;' to '\u0026'
    /// </remarks>
    private static string EscapeSpecialCharacters(string input)
    {
        return input.Replace("<", "\\u003c").Replace(">", "\\u003e").Replace("&", "\\u0026");
    }

    /// <summary>
    ///     Generates a digital signature using RSA-SHA256 with PKCS1 padding.
    /// </summary>
    /// <param name="signatureInput">The input string to be signed.</param>
    /// <param name="privateKey">The RSA private key in PEM format used for signing.</param>
    /// <returns>Base64 encoded signature string.</returns>
    private static string GenerateSignatureCore(string signatureInput, string privateKey)
    {
        // Create RSA provider from the PEM-formatted private key
        using var provider = PemKeyHelper.CreateRSAFromPem(privateKey);

        // Sign the input data using SHA256 algorithm and PKCS1 padding
        var signedBytes = provider.SignData(Encoding.UTF8.GetBytes(signatureInput), // Convert input to UTF8 bytes
            HashAlgorithmName.SHA256, // Use SHA256 hashing algorithm
            RSASignaturePadding.Pkcs1); // Use PKCS1 padding scheme

        // Convert the signed bytes to Base64 string
        return Convert.ToBase64String(signedBytes);
    }

    /// <summary>
    ///     Sorts a JsonElement node and its children recursively.
    /// </summary>
    /// <param name="element">The JsonElement to sort.</param>
    /// <returns>A sorted JsonNode representation of the input element.</returns>
    /// <remarks>
    ///     Handles three cases:
    ///     - Objects: Creates a new sorted object
    ///     - Arrays: Preserves array order
    ///     - Other values: Converts to JsonValue
    /// </remarks>
    private static JsonNode SortJsonNode(JsonElement element)
    {
        // Use pattern matching to handle different JSON value types
        return element.ValueKind switch
        {
            JsonValueKind.Object => CreateSortedObject(element), // Sort object properties
            JsonValueKind.Array => JsonArray.Create(element)!, // Preserve array as-is
            _ => JsonValue.Create(element)! // Convert primitive values
        };
    }

    /// <summary>
    ///     Validates that the signature type matches the supported SHA256 algorithm.
    /// </summary>
    /// <param name="signType">The signature type to validate.</param>
    /// <exception cref="ArgumentException">Thrown when signature type is not SHA256.</exception>
    private static void ValidateSignType(string signType)
    {
        // Compare input sign type with supported type (case-insensitive)
        if (!string.Equals(signType, SupportedSignType, StringComparison.OrdinalIgnoreCase))
            // Throw exception if sign type is not supported
            throw new ArgumentException($"Invalid signType. Only '{SupportedSignType}' is supported.");
    }
}