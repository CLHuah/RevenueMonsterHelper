using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     Signature algorithm is used to sign your payment API request with a private key to obtain additional security.
/// </summary>
public static class SignatureHelper
{
    /// <summary>
    ///     Generates compact JSON representation of the provided object after sorting its properties.
    /// </summary>
    /// <param name="data">The object to be converted to compact JSON.</param>
    /// <returns>Compact JSON string.</returns>
    public static string GenerateCompactJson(object data)
    {
        // Check if the input data is null
        if (data == null) throw new ArgumentNullException(nameof(data), "Input data cannot be null.");

        // Serialize the object to JSON string
        var dataStr = JsonConvert.SerializeObject(data);

        // Parse the JSON string into a JObject and sort its properties
        var sortedObj = SortProperties(JObject.Parse(dataStr));

        // Convert the sorted JObject to a compact JSON string
        var jsonString = sortedObj.ToString(Formatting.None);

        // Replace special characters
        jsonString = jsonString.Replace("<", "\\u003c").Replace(">", "\\u003e").Replace("&", "\\u0026");

        return jsonString;
    }

    /// <summary>
    ///     Generates a digital signature based on the provided data object and parameters.
    /// </summary>
    /// <param name="data">The data object to be included in the signature.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="privateKey">The private key used for signing.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type.</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(object data, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Generate compact JSON from the data object
        var compactJson = data != null ? GenerateCompactJson(data) : null;

        // Use the core signature generation method
        return GenerateSignatureCore(compactJson, method, nonceStr, privateKey, requestUrl, signType, timestamp);
    }

    /// <summary>
    ///     Generates a digital signature based on the provided compact JSON and parameters.
    /// </summary>
    /// <param name="compactJson">The compact JSON representation of the data.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="privateKey">The private key used for signing.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type.</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(string compactJson, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Use the core signature generation method
        return GenerateSignatureCore(compactJson, method, nonceStr, privateKey, requestUrl, signType, timestamp);
    }

    /// <summary>
    ///     Verifies the signature based on the provided parameters.
    /// </summary>
    /// <param name="data">The data to be included in the signature verification.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="publicKey">The public key for signature verification.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type (must be "SHA256").</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <param name="signature">The signature to be verified.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(object data, string method, string nonceStr, string publicKey, string requestUrl,
        string signType, string timestamp, string signature)
    {
        // Ensure that signType is "SHA256"
        if (!string.Equals(signType, "SHA256", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid signType. Only 'SHA256' is supported.");

        var sb = new StringBuilder();

        // Add data to the signature if present
        if (data != null)
        {
            var encodedData = Encode.Base64Encode(GenerateCompactJson(data));
            sb.Append($"data={encodedData}&");
        }

        // Add other parameters to the signature
        sb.Append($"method={method}&");
        sb.Append($"nonceStr={nonceStr}&");
        if (!string.IsNullOrWhiteSpace(requestUrl)) sb.Append($"requestUrl={requestUrl}&");
        sb.Append($"signType={signType}&");
        sb.Append($"timestamp={timestamp}");

        var plainText = sb.ToString();

        // Convert the plain text to bytes for signature verification
        var plainTextByte = Encoding.UTF8.GetBytes(plainText);

        // Convert the signature from base64 to bytes
        var signatureByte = Convert.FromBase64String(signature);

        // Get the RSA provider from the provided public key
        var provider = PemKeyHelper.GetRSAProviderFromPemFile(publicKey);

        // Get the OID for SHA256
        var sha256Oid = CryptoConfig.MapNameToOID("SHA256") ??
                        throw new InvalidOperationException("Unable to retrieve OID for SHA256.");

        // Verify the signature using SHA256
        var result = provider.VerifyData(plainTextByte, sha256Oid, signatureByte);

        return result;
    }

    /// <summary>
    ///     Generates a digital signature based on the provided parameters.
    /// </summary>
    /// <param name="compactJson">The compact JSON representation of the data.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="privateKey">The private key used for signing.</param>
    /// <param name="requestUrl">The request URL (optional).</param>
    /// <param name="signType">The signature type.</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    private static string GenerateSignatureCore(string compactJson, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Ensure that signType is "SHA256"
        if (!string.Equals(signType, "SHA256", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid signType. Only 'SHA256' is supported.");

        // Construct the plain text for signing
        var plainText = $"{(compactJson != null ? $"data={Encode.Base64Encode(compactJson)}&" : "")}" +
                        $"method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";

        // Convert the plain text to bytes for signature generation
        var plainTextByte = Encoding.UTF8.GetBytes(plainText);

        // Get the RSA provider from the provided private key
        var provider = PemKeyHelper.GetRSAProviderFromPemFile(privateKey);

        // Get the OID for SHA256
        var sha256Oid = CryptoConfig.MapNameToOID("SHA256") ??
                        throw new InvalidOperationException("Unable to retrieve OID for SHA256.");

        // Generate the digital signature using SHA256
        var signedBytes = provider.SignData(plainTextByte, sha256Oid);

        // Return the base64-encoded digital signature
        return Convert.ToBase64String(signedBytes);
    }

    /// <summary>
    ///     Sorts the properties of a JSON object alphabetically.
    /// </summary>
    /// <param name="jObj">The JSON object to be sorted.</param>
    /// <returns>JSON object with sorted properties.</returns>
    private static JObject SortProperties(JObject jObj)
    {
        // Get a list of properties from the JObject
        var properties = jObj.Properties().ToList();

        // Remove all existing properties from the JObject
        foreach (var prop in properties) prop.Remove();

        // Add the properties back to the JObject in alphabetical order
        foreach (var prop in properties.OrderBy(p => p.Name))
        {
            // If the property value is another JObject, recursively sort its properties
            if (prop.Value is JObject nestedObject)
            {
                SortProperties(nestedObject);
            }

            jObj.Add(prop.Name, prop.Value);
        }

        // Return the JObject with sorted properties
        return jObj;
    }

}