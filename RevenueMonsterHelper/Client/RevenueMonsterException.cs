using RevenueMonsterLibrary.Model;
using System;
using System.Net;

namespace RevenueMonsterLibrary.Client;

/// <summary>
///     Thrown when Revenue Monster returns an error, or a response that cannot be read.
/// </summary>
public sealed class RevenueMonsterException : Exception
{
    public RevenueMonsterException(string message, HttpStatusCode statusCode, Error error, string responseBody,
        Exception innerException = null) : base(message, innerException)
    {
        StatusCode = statusCode;
        Error = error;
        ResponseBody = responseBody;
    }

    /// <summary>
    ///     The error returned by Revenue Monster, or null when the response did not contain one.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    ///     The error code returned by Revenue Monster, or null when the response did not contain one.
    /// </summary>
    public string ErrorCode => Error?.code;

    /// <summary>
    ///     The raw response body.
    /// </summary>
    public string ResponseBody { get; }

    /// <summary>
    ///     The HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}