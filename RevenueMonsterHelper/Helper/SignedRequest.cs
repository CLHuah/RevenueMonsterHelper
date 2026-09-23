using RevenueMonsterLibrary.Constants;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     A signed Revenue Monster request: the body to send and the values for the signature headers.
/// </summary>
public sealed class SignedRequest
{
    /// <summary>
    ///     The request body to send, exactly as it was signed, or null when the request has no body.
    /// </summary>
    public string Body { get; init; }

    /// <summary>
    ///     The value for the X-Nonce-Str header.
    /// </summary>
    public string NonceStr { get; init; }

    /// <summary>
    ///     The base64-encoded signature.
    /// </summary>
    public string Signature { get; init; }

    /// <summary>
    ///     The value for the X-Signature header, in the form "sha256 {signature}".
    /// </summary>
    public string SignatureHeader => $"{SignTypes.Sha256} {Signature}";

    /// <summary>
    ///     The value for the X-Timestamp header (Unix time in seconds).
    /// </summary>
    public string Timestamp { get; init; }
}