using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;

namespace RevenueMonsterLibrary.Helper;

public static class PemKeyHelper
{
    /// <summary>
    ///     Creates an RSA instance from a PEM formatted string.
    /// Supports both private keys (PKCS#1, PKCS#8) and public keys.
    /// </summary>
    /// <param name="pemContent">The PEM formatted string containing either an RSA private or public key.</param>
    /// <returns>
    ///     An RSA instance initialized with the key data from the PEM string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when pemContent is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the PEM string format is invalid or contains an unsupported key type.</exception>
    /// <remarks>
    ///     The method automatically detects whether the input is a private or public key and handles the conversion
    ///     accordingly.
    ///     Uses BouncyCastle's PemReader for parsing the PEM format.
    /// </remarks>
    /// <example>
    ///     <code>
    /// string pemContent = File.ReadAllText("key.pem");
    /// RSA rsa = PemKeyHelper.CreateRSAFromPem(pemContent);
    /// </code>
    /// </example>
    public static RSA CreateRSAFromPem(string pemContent)
    {
        // Validate that input string is not null
        ArgumentNullException.ThrowIfNull(pemContent);

        // Create string reader and PEM reader to parse the content
        using var stringReader = new StringReader(pemContent);
        var pemReader = new PemReader(stringReader);

        try
        {
            // Read the PEM object from the content
            var pemObject = pemReader.ReadObject();

            // If the object is a key pair, extract the private key component
            if (pemObject is AsymmetricCipherKeyPair keyPair) pemObject = keyPair.Private;

            // Convert the PEM object to RSA based on its type
            return pemObject switch
            {
                // Convert PKCS#1 private key format
                RsaPrivateCrtKeyParameters privateKey => DotNetUtilities.ToRSA(privateKey),

                // Convert PKCS#8 private key format by constructing parameters
                RsaPrivateKeyStructure pkcs1PrivateKey => DotNetUtilities.ToRSA(new RsaPrivateCrtKeyParameters(
                    pkcs1PrivateKey.Modulus, // n
                    pkcs1PrivateKey.PublicExponent, // e
                    pkcs1PrivateKey.PrivateExponent, // d
                    pkcs1PrivateKey.Prime1, // p
                    pkcs1PrivateKey.Prime2, // q
                    pkcs1PrivateKey.Exponent1, // dp
                    pkcs1PrivateKey.Exponent2, // dq
                    pkcs1PrivateKey.Coefficient // qInv
                )),

                // Convert public key format
                RsaKeyParameters publicKey => DotNetUtilities.ToRSA(publicKey),

                // Handle unsupported key formats with descriptive error
                _ => throw new ArgumentException($"Unsupported key format: {pemObject?.GetType().Name ?? "null"}",
                    nameof(pemContent))
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            // Wrap any unexpected exceptions in ArgumentException with the original as inner exception
            throw new ArgumentException("Invalid PEM content or format", nameof(pemContent), ex);
        }
    }
}