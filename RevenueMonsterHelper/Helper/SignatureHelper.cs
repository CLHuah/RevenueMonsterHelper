using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     Signs Revenue Monster API requests and verifies Revenue Monster signatures (RSA-SHA256 with PKCS#1 v1.5 padding).
/// </summary>
/// <remarks>
///     Revenue Monster checks a signature against its own canonical form of the request body it receives, so the
///     signed JSON must describe exactly the body that is sent. Serialize request bodies with
///     <c>JsonConvert.SerializeObject</c> (the serializer used here), or send the output of
///     <see cref="GenerateCompactJson" /> as the request body.
/// </remarks>
public static class SignatureHelper
{
    // The only supported signature algorithm, as it appears in the signed string
    private const string SupportedSignType = "sha256";

    // Reads raw JSON without converting date-like strings, so values are signed exactly as they were sent
    private static readonly JsonSerializer RawJsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        DateParseHandling = DateParseHandling.None,
        CheckAdditionalContent = true
    });

    /// <summary>
    ///     Generates the canonical compact JSON of the provided object: properties sorted at every level, no
    ///     whitespace, and Revenue Monster's escaping and number format.
    /// </summary>
    /// <param name="data">
    ///     The object to convert. Accepts plain objects (serialized with <c>JsonConvert.SerializeObject</c>),
    ///     Newtonsoft <see cref="JToken" />s and System.Text.Json documents, elements and nodes.
    /// </param>
    /// <returns>Compact JSON string.</returns>
    /// <remarks>
    ///     To verify a webhook, pass the raw request body to <see cref="GenerateCompactJsonFromRaw" /> or
    ///     <see cref="VerifySignatureFromRawBody" /> instead. Deserializing the body first drops properties the model
    ///     does not declare, and parsing it into a <see cref="JToken" /> with default settings can rewrite dates.
    /// </remarks>
    public static string GenerateCompactJson(object data)
    {
        // Validate input - throw if data is null
        ArgumentNullException.ThrowIfNull(data);

        // Turn every kind of input into JSON text so it all goes through the same parser
        var json = data switch
        {
            JToken token => token.ToString(Formatting.None),
            System.Text.Json.JsonDocument document => document.RootElement.GetRawText(),
            System.Text.Json.JsonElement element => element.GetRawText(),
            System.Text.Json.Nodes.JsonNode node => node.ToJsonString(),
            _ => JsonConvert.SerializeObject(data)
        };

        return GenerateCompactJsonFromRaw(json);
    }

    /// <summary>
    ///     Generates the canonical compact JSON of a raw JSON string, such as a webhook request body.
    /// </summary>
    /// <param name="json">The raw JSON text.</param>
    /// <returns>Compact JSON string, or an empty string when <paramref name="json" /> is blank.</returns>
    public static string GenerateCompactJsonFromRaw(string json)
    {
        // Validate input - throw if json is null
        ArgumentNullException.ThrowIfNull(json);

        // A blank body has no canonical form
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;

        // Parse without altering any values
        using var reader = new JsonTextReader(new StringReader(json));
        var token = RawJsonSerializer.Deserialize<JToken>(reader) ?? JValue.CreateNull();

        // Write the canonical form
        var builder = new StringBuilder(json.Length);
        WriteCanonicalJson(builder, token);
        return builder.ToString();
    }

    /// <summary>
    ///     Generates a digital signature based on the provided data object and parameters.
    /// </summary>
    /// <param name="data">The request body, or null when the request has none.</param>
    /// <param name="method">The HTTP method used in the request (any casing).</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="privateKey">The RSA private key in PEM format used for signing.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature type (must be SHA256, any casing).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(object data, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Convert object to canonical compact JSON if not null
        var compactJson = data is null ? null : GenerateCompactJson(data);

        return GenerateSignature(compactJson, method, nonceStr, privateKey, requestUrl, signType, timestamp);
    }

    /// <summary>
    ///     Generates a digital signature based on the provided compact JSON and parameters.
    /// </summary>
    /// <param name="compactJson">
    ///     The canonical compact JSON of the request body (see <see cref="GenerateCompactJson" />), or null when the
    ///     request has none.
    /// </param>
    /// <param name="method">The HTTP method used in the request (any casing).</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="privateKey">The RSA private key in PEM format used for signing.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature type (must be SHA256, any casing).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <returns>The generated digital signature as a base64-encoded string.</returns>
    public static string GenerateSignature(string compactJson, string method, string nonceStr, string privateKey,
        string requestUrl, string signType, string timestamp)
    {
        // Build the string to sign
        var signatureInput = BuildSignatureInput(compactJson, method, nonceStr, requestUrl,
            NormalizeSignType(signType), timestamp);

        // Create RSA provider from the PEM-formatted private key
        using var provider = PemKeyHelper.CreateRSAFromPem(privateKey);

        // Sign the input data using SHA256 algorithm and PKCS1 padding
        var signedBytes = provider.SignData(Encoding.UTF8.GetBytes(signatureInput), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signedBytes);
    }

    /// <summary>
    ///     Verifies a digital signature using RSA-SHA256 with PKCS1 padding.
    /// </summary>
    /// <param name="data">The request body, or null when the request has none.</param>
    /// <param name="method">The HTTP method used in the request (any casing).</param>
    /// <param name="nonceStr">A random string to prevent replay attacks.</param>
    /// <param name="publicKey">The RSA public key in PEM format for verification.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature algorithm type (must be SHA256, any casing).</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <param name="signature">The Base64-encoded signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(object data, string method, string nonceStr, string publicKey, string requestUrl,
        string signType, string timestamp, string signature)
    {
        // Convert data object to canonical compact JSON if not null
        var compactJson = data is null ? null : GenerateCompactJson(data);

        return VerifySignatureCore(compactJson, method, nonceStr, publicKey, requestUrl, signType, timestamp,
            signature);
    }

    /// <summary>
    ///     Verifies a digital signature against a raw request body, such as a webhook payload.
    /// </summary>
    /// <param name="rawBody">The request body exactly as received, or null/empty when there is none.</param>
    /// <param name="method">The HTTP method used in the request (any casing).</param>
    /// <param name="nonceStr">The nonce string sent with the request.</param>
    /// <param name="publicKey">The RSA public key in PEM format for verification.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature algorithm type (must be SHA256, any casing).</param>
    /// <param name="timestamp">The timestamp sent with the request.</param>
    /// <param name="signature">The Base64-encoded signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    /// <remarks>
    ///     Prefer this over <see cref="VerifySignature" /> for incoming requests: the body is canonicalized as
    ///     received, so properties a model does not declare are still covered by the check.
    /// </remarks>
    public static bool VerifySignatureFromRawBody(string rawBody, string method, string nonceStr, string publicKey,
        string requestUrl, string signType, string timestamp, string signature)
    {
        // Canonicalize the body exactly as received
        var compactJson = rawBody is null ? null : GenerateCompactJsonFromRaw(rawBody);

        return VerifySignatureCore(compactJson, method, nonceStr, publicKey, requestUrl, signType, timestamp,
            signature);
    }

    /// <summary>
    ///     Builds the string that is signed and verified.
    /// </summary>
    /// <param name="compactJson">The canonical compact JSON of the body (can be null).</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="nonceStr">The nonce string.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="signType">The signature type.</param>
    /// <param name="timestamp">The request timestamp.</param>
    /// <returns>A concatenated string of all parameters in alphabetical order.</returns>
    /// <remarks>
    ///     The returned string format is:
    ///     "data={base64_encoded_json}&amp;method={method}&amp;nonceStr={nonceStr}&amp;requestUrl={requestUrl}&amp;signType={signType}&amp;timestamp={timestamp}"
    ///     The method is lowercased. Parameters with empty values are left out, and a body that is empty, {} or null
    ///     leaves out the data parameter.
    /// </remarks>
    internal static string BuildSignatureInput(string compactJson, string method, string nonceStr, string requestUrl,
        string signType, string timestamp)
    {
        // Encode the body only when it carries data
        var data = IsEmptyBody(compactJson) ? null : Encode.Base64Encode(compactJson);

        // Parameters in alphabetical order
        (string Key, string Value)[] parameters =
        [
            ("data", data),
            ("method", method?.ToLowerInvariant()),
            ("nonceStr", nonceStr),
            ("requestUrl", requestUrl),
            ("signType", signType),
            ("timestamp", timestamp)
        ];

        // Join the non-empty parameters as key=value pairs
        return string.Join("&", parameters
            .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
            .Select(parameter => $"{parameter.Key}={parameter.Value}"));
    }

    /// <summary>
    ///     Verifies a signature against the canonical compact JSON of the body.
    /// </summary>
    private static bool VerifySignatureCore(string compactJson, string method, string nonceStr, string publicKey,
        string requestUrl, string signType, string timestamp, string signature)
    {
        // Build the string that should have been signed
        var signatureInput = BuildSignatureInput(compactJson, method, nonceStr, requestUrl,
            NormalizeSignType(signType), timestamp);

        // A missing signature, or one that is not valid base64, cannot match
        if (string.IsNullOrEmpty(signature)) return false;
        var signatureBytes = new byte[signature.Length]; // decoded data is always shorter than its base64 text
        if (!Convert.TryFromBase64String(signature, signatureBytes, out var signatureLength)) return false;

        // Create RSA provider from public key PEM
        using var provider = PemKeyHelper.CreateRSAFromPem(publicKey);

        // Verify signature using RSA-SHA256 with PKCS1 padding
        return provider.VerifyData(Encoding.UTF8.GetBytes(signatureInput), signatureBytes.AsSpan(0, signatureLength),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    ///     Validates that the signature type is SHA256 and returns it in the form used in the signed string.
    /// </summary>
    /// <param name="signType">The signature type to validate.</param>
    /// <returns>The normalized signature type.</returns>
    /// <exception cref="ArgumentException">Thrown when signature type is not SHA256.</exception>
    private static string NormalizeSignType(string signType)
    {
        // Compare input sign type with supported type (case-insensitive)
        if (!string.Equals(signType, SupportedSignType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid signType. Only '{SupportedSignType}' is supported.",
                nameof(signType));

        return SupportedSignType;
    }

    /// <summary>
    ///     Checks whether a compact JSON body carries no data.
    /// </summary>
    private static bool IsEmptyBody(string compactJson)
    {
        return string.IsNullOrWhiteSpace(compactJson) || compactJson.Trim() is "{}" or "null";
    }

    /// <summary>
    ///     Writes a JSON token in canonical form: object properties sorted by ordinal key at every level (including
    ///     objects inside arrays) and no whitespace.
    /// </summary>
    private static void WriteCanonicalJson(StringBuilder builder, JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                builder.Append('{');
                var firstProperty = true;
                foreach (var property in ((JObject)token).Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!firstProperty) builder.Append(',');
                    firstProperty = false;

                    WriteJsonString(builder, property.Name);
                    builder.Append(':');
                    WriteCanonicalJson(builder, property.Value);
                }

                builder.Append('}');
                break;

            case JTokenType.Array:
                builder.Append('[');
                var firstItem = true;
                foreach (var item in (JArray)token)
                {
                    if (!firstItem) builder.Append(',');
                    firstItem = false;

                    WriteCanonicalJson(builder, item);
                }

                builder.Append(']');
                break;

            case JTokenType.String:
                WriteJsonString(builder, (string)token);
                break;

            case JTokenType.Integer:
            case JTokenType.Float:
                builder.Append(FormatNumber(((JValue)token).Value));
                break;

            case JTokenType.Boolean:
                builder.Append((bool)token ? "true" : "false");
                break;

            case JTokenType.Null:
            case JTokenType.Undefined:
                builder.Append("null");
                break;

            default:
                throw new InvalidOperationException($"Unexpected JSON token type '{token.Type}'.");
        }
    }

    /// <summary>
    ///     Writes a quoted JSON string. Quotes, backslashes and control characters are escaped, as are the
    ///     HTML-sensitive characters &lt; &gt; &amp; and the line separators U+2028 and U+2029. All other characters,
    ///     including emoji and other non-ASCII text, are written as-is.
    /// </summary>
    private static void WriteJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var c in value)
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '<' or '>' or '&' or (char)0x2028 or (char)0x2029:
                case < ' ':
                    builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default:
                    builder.Append(c);
                    break;
            }

        builder.Append('"');
    }

    /// <summary>
    ///     Formats a JSON number in canonical form: the value as a double, written with the shortest digits that
    ///     round-trip, in plain notation for magnitudes from 1e-6 up to (not including) 1e21 and exponent notation
    ///     outside that range. For example 100.50 becomes "100.5", 100.0 becomes "100", 0.00000015 becomes "1.5e-7" and
    ///     1e21 becomes "1e+21".
    /// </summary>
    private static string FormatNumber(object number)
    {
        // Every number is compared as a double
        var value = number is BigInteger bigInteger
            ? (double)bigInteger
            : Convert.ToDouble(number, CultureInfo.InvariantCulture);

        if (value == 0) return double.IsNegative(value) ? "-0" : "0";

        // Shortest round-trip digits, e.g. "1.5E-07", "123.45" or "1E+21"
        var roundTrip = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture);
        var exponentIndex = roundTrip.IndexOf('E');
        var mantissa = exponentIndex < 0 ? roundTrip : roundTrip[..exponentIndex];
        var exponent = exponentIndex < 0
            ? 0
            : int.Parse(roundTrip[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        // Split into significant digits and the position of the decimal point relative to them
        var pointIndex = mantissa.IndexOf('.');
        var digits = pointIndex < 0 ? mantissa : mantissa.Remove(pointIndex, 1);
        var pointPosition = (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent;
        pointPosition -= digits.Length - digits.TrimStart('0').Length;
        digits = digits.Trim('0');

        var sign = value < 0 ? "-" : "";
        var magnitude = Math.Abs(value);

        // Exponent notation, e.g. "1.5e-7" or "1e+21"
        if (magnitude < 1e-6 || magnitude >= 1e21)
        {
            var power = pointPosition - 1;
            var fraction = digits.Length > 1 ? "." + digits[1..] : "";
            return $"{sign}{digits[0]}{fraction}e{(power < 0 ? '-' : '+')}{Math.Abs(power)}";
        }

        // Plain notation, e.g. "0.0000015", "100.5" or "123000"
        if (pointPosition <= 0) return $"{sign}0.{new string('0', -pointPosition)}{digits}";
        if (pointPosition >= digits.Length) return sign + digits + new string('0', pointPosition - digits.Length);
        return $"{sign}{digits[..pointPosition]}.{digits[pointPosition..]}";
    }
}
