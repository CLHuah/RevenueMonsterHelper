using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model;

public class ExtraInfo
{
    public string reference { get; set; }
    public string type { get; set; }
}

public class QuickPay
{
    public string authCode { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public ExtraInfo extraInfo { get; set; }

    public string ipAddress { get; set; }
    public Order order { get; set; }

    public string storeId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string terminalId { get; set; }

    /// <summary>
    ///     Voucher to apply to the payment.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public VoucherCode voucher { get; set; }
}

public class PaymentTransactionByOrderID : ApiResponse<TransactionQuickPay>
{
}

/// <summary>
///     A payment transaction.
/// </summary>
public class TransactionQuickPay
{
    public long balanceAmount { get; set; }
    public string createdAt { get; set; }
    public string currencyType { get; set; }
    public Error error { get; set; }
    public TransactionExtraInfo extraInfo { get; set; }
    public string method { get; set; }
    public Order order { get; set; }
    public Payee payee { get; set; }
    public string platform { get; set; }
    public string referenceId { get; set; }
    public string region { get; set; }
    public string status { get; set; }
    public Store store { get; set; }
    public string terminalId { get; set; }
    public string transactionAt { get; set; }
    public string transactionId { get; set; }
    public string type { get; set; }
    public string updatedAt { get; set; }
}

/// <summary>
///     Extra information on a transaction. Properties this model does not declare are kept in
///     <see cref="extensionData" />.
/// </summary>
public class TransactionExtraInfo
{
    public List<ExtraFee> extraFee { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JToken> extensionData { get; set; }
}

/// <summary>
///     A fee charged on top of a transaction.
/// </summary>
public class ExtraFee
{
    public long amount { get; set; }
    public string feeType { get; set; }
    public long feeValue { get; set; }
    public bool isIncludedMDR { get; set; }
    public bool isRefundAllowed { get; set; }
    public string referenceId { get; set; }
    public string type { get; set; }
}

public class Expiry
{
    public float day { get; set; }
    public string expiredAt { get; set; }
    public string type { get; set; }
}

public class Store
{
    public string addressLine1 { get; set; }
    public string addressLine2 { get; set; }
    public string city { get; set; }
    public string country { get; set; }
    public string countryCode { get; set; }
    public string createdAt { get; set; }
    public GeoLocation geoLocation { get; set; }
    public string id { get; set; }
    public Uri imageUrl { get; set; }
    public string name { get; set; }
    public string phoneNumber { get; set; }
    public string postCode { get; set; }
    public string state { get; set; }
    public string status { get; set; }
    public string updatedAt { get; set; }
}

public class GeoLocation
{
    public decimal latitude { get; set; }
    public decimal longitude { get; set; }
}

public class Payee
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string subUserId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string userId { get; set; }
}

/// <summary>
///     Request to create an online checkout. Optional properties that are left unset are not sent.
/// </summary>
public class WebPayment
{
    public WebPayment()
    {
        order = new Order();
    }

    /// <summary>
    ///     Customer details, used for example to prefill the checkout page.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Customer customer { get; set; }

    /// <summary>
    ///     Payment methods to hide from the checkout page.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IList<string> excludeMethod { get; set; }

    /// <summary>
    ///     How long the checkout stays open, in seconds.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? expiresInSeconds { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public CheckoutExtraInfo extraInfo { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<InHousePromo> inHousePromo { get; set; }

    public string layoutVersion { get; set; } // v1 / v2 (Supported Credit Card), see CheckoutLayoutVersions
    public IList<string> method { get; set; }

    public string notifyUrl { get; set; }

    public Order order { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IList<string> paymentOrders { get; set; }

    public string redirectUrl { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string source { get; set; }

    public string storeId { get; set; }

    public string type { get; set; } // WEB_PAYMENT or MOBILE_PAYMENT, see PaymentTypes

    /// <summary>
    ///     Voucher to apply to the payment.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public VoucherCode voucher { get; set; }
}

public class Order
{
    public string additionalData { get; set; }
    public long amount { get; set; }

    /// <summary>
    ///     Breakdown of <see cref="amount" /> into its parts.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<AmountBreakdown> amountBreakdown { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string currencyType { get; set; }

    public string detail { get; set; }
    public string id { get; set; }
    public string title { get; set; }
}

public class WebPaymentResponse : ApiResponse<Item>
{
}

public class Item
{
    public string checkoutId { get; set; }
    public string url { get; set; }
}

public class Notify
{
    public Data data { get; set; }
    public string eventType { get; set; }
}

public class Data
{
    public long balanceAmount { get; set; }
    public string createdAt { get; set; }
    public string currencyType { get; set; }
    public Error error { get; set; }
    public TransactionExtraInfo extraInfo { get; set; }
    public string method { get; set; }
    public Order order { get; set; }
    public Payee payee { get; set; }
    public string platform { get; set; }
    public string referenceId { get; set; }
    public string region { get; set; }
    public string status { get; set; }
    public Store store { get; set; }
    public string terminalId { get; set; }
    public string transactionAt { get; set; }
    public string transactionId { get; set; }
    public string type { get; set; }
    public string updatedAt { get; set; }
    public Voucher voucher { get; set; }
}
