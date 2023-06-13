using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     Provides methods for decoding base64 encoded data.
/// </summary>
public static class Decode
{
    /// <summary>
    ///     Decodes a base64 encoded string into its original UTF-8 string representation.
    /// </summary>
    /// <param name="base64EncodedData">The base64 encoded string to decode.</param>
    /// <returns>The decoded UTF-8 string.</returns>
    public static string Base64Decode(string base64EncodedData)
    {
        // Convert the base64 encoded string to bytes
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);

        // Decode the bytes using UTF-8 encoding and return the resulting string
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}