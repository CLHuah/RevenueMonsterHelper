namespace RevenueMonsterLibrary.Model;

public class Error
{
    public string code { get; set; }

    /// <summary>
    ///     Additional detail for troubleshooting, when provided.
    /// </summary>
    public string debug { get; set; }

    /// <summary>
    ///     Structured detail about the error, when provided. Its shape depends on the error.
    /// </summary>
    public object description { get; set; }

    public string message { get; set; }
}