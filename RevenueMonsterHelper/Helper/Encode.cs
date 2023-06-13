using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     Provides methods for encode a plain text string to Base64.
/// </summary>
public static class Encode
{
    /// <summary>
    ///     Encodes a plain text string to Base64.
    /// </summary>
    /// <param name="plainText">The plain text to encode.</param>
    /// <returns>The Base64 encoded string.</returns>
    public static string Base64Encode(string plainText)
    {
        // Convert the plain text to bytes using UTF-8 encoding
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

        // Convert the bytes to Base64 string and return the result
        return Convert.ToBase64String(plainTextBytes);
    }
}