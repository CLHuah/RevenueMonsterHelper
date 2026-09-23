namespace RevenueMonsterLibrary.Model;

/// <summary>
///     Request to refund a successful transaction.
/// </summary>
public class RefundRequest
{
    public string reason { get; set; }
    public RefundDetail refund { get; set; }
    public string transactionId { get; set; }
}

public class RefundDetail
{
    /// <summary>
    ///     Amount to refund, in the smallest currency unit (e.g. sen).
    /// </summary>
    public long amount { get; set; }

    public string currencyType { get; set; }

    /// <summary>
    ///     Refund type as defined by the Revenue Monster refund API.
    /// </summary>
    public string type { get; set; }
}

/// <summary>
///     Request to reverse a transaction that timed out or failed part-way.
/// </summary>
public class ReverseRequest
{
    public string orderId { get; set; }
}
