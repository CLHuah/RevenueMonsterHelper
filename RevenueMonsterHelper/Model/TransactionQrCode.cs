using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model;

/// <summary>
///     Request to create a QR code that customers scan to pay.
/// </summary>
public class TransactionQrCodeRequest
{
    public long amount { get; set; }
    public string currencyType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public TransactionQrCodeExpiry expiry { get; set; }

    /// <summary>
    ///     Whether the amount is fixed (true) or entered by the customer (false).
    /// </summary>
    public bool isPreFillAmount { get; set; }

    public List<string> method { get; set; }
    public TransactionQrCodeOrder order { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string redirectUrl { get; set; }

    public string storeId { get; set; }
    public string type { get; set; }
}

public class TransactionQrCodeExpiry
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? day { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string expiredAt { get; set; }

    public string type { get; set; }
}

public class TransactionQrCodeOrder
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string additionalData { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string detail { get; set; }

    public string title { get; set; }
}

/// <summary>
///     A QR code that customers scan to pay.
/// </summary>
public class TransactionQrCode
{
    public long amount { get; set; }
    public string code { get; set; }
    public string createdAt { get; set; }
    public string currencyType { get; set; }
    public Expiry expiry { get; set; }
    public bool isPreFillAmount { get; set; }
    public List<string> method { get; set; }
    public TransactionQrCodeOrder order { get; set; }
    public string platform { get; set; }
    public string qrCodeUrl { get; set; }
    public string redirectUrl { get; set; }
    public string status { get; set; }
    public Store store { get; set; }
    public string type { get; set; }
    public string updatedAt { get; set; }
}