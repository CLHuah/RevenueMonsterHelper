using System;
using System.Security.Cryptography;

namespace RevenueMonsterLibrary.Helper;

public static class PemKeyHelper
{
    /// <summary>
    ///     Creates an RSA instance from a PEM formatted string.
    ///     Supports private keys (PKCS#1 "RSA PRIVATE KEY", PKCS#8 "PRIVATE KEY") and
    ///     public keys (X.509 "PUBLIC KEY", PKCS#1 "RSA PUBLIC KEY").
    /// </summary>
    /// <param name="pemContent">The PEM formatted string containing either an RSA private or public key.</param>
    /// <returns>
    ///     An RSA instance initialized with the key data from the PEM string. The caller owns the instance and should
    ///     dispose it.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when pemContent is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the PEM string format is invalid or contains an unsupported key type.</exception>
    /// <remarks>
    ///     The method automatically detects whether the input is a private or public key from the PEM label.
    ///     Uses the built-in <see cref="RSA.ImportFromPem(ReadOnlySpan{char})" />, so the key is held in memory only
    ///     and works on every platform .NET supports.
    /// </remarks>
    /// <example>
    ///     <code>
    /// string pemContent = File.ReadAllText("key.pem");
    /// using RSA rsa = PemKeyHelper.CreateRSAFromPem(pemContent);
    /// </code>
    /// </example>
    public static RSA CreateRSAFromPem(string pemContent)
    {
        // Validate that input string is not null
        ArgumentNullException.ThrowIfNull(pemContent);

        var rsa = RSA.Create();

        try
        {
            // Import the key; the PEM label decides whether it is a private or public key
            rsa.ImportFromPem(pemContent);
            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            rsa.Dispose();

            // Wrap parse failures in ArgumentException with the original as inner exception
            throw new ArgumentException("Invalid PEM content or format", nameof(pemContent), ex);
        }
    }
}
