namespace RevenueMonsterLibrary.Model;

/// <summary>
///     Request to refund a transaction: returns the funds to the customer, before or after the settlement date
///     depending on the payment provider.
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
    ///     Refund type, e.g. <see cref="Constants.RefundTypes.Full" />.
    /// </summary>
    public string type { get; set; }
}

/// <summary>
///     Request to reverse (cancel) a transaction. Only possible within a short window after the transaction, such as
///     15 minutes; meant for cases like a dropped connection, to prevent double charges.
/// </summary>
public class ReverseRequest
{
    public string orderId { get; set; }
}
