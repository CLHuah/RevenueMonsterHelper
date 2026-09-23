using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model;

/// <summary>
///     Customer details for an online checkout.
/// </summary>
public class Customer
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string countryCode { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string email { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string phoneNumber { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string userId { get; set; }
}

/// <summary>
///     Extra information for an online checkout.
/// </summary>
public class CheckoutExtraInfo
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<CheckoutExtraFee> extraFee { get; set; }
}

/// <summary>
///     A fee to charge on top of an online checkout.
/// </summary>
public class CheckoutExtraFee
{
    public long amount { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string referenceId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string source { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string type { get; set; }
}

/// <summary>
///     A merchant's own promotion applied to an online checkout.
/// </summary>
public class InHousePromo
{
    public long amount { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string label { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string source { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string uniqueId { get; set; }
}

/// <summary>
///     One part of an order amount.
/// </summary>
public class AmountBreakdown
{
    public long amount { get; set; }
    public bool isDiscountAllowed { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string label { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string type { get; set; }
}

/// <summary>
///     A voucher to apply to a payment.
/// </summary>
public class VoucherCode
{
    public string code { get; set; }
}

/// <summary>
///     An online checkout and its current state.
/// </summary>
public class OnlineCheckout
{
    public string createdAt { get; set; }
    public string endAt { get; set; }
    public string id { get; set; }
    public List<string> method { get; set; }
    public string notifyUrl { get; set; }
    public Order order { get; set; }
    public string platform { get; set; }
    public string redirectUrl { get; set; }
    public string startAt { get; set; }
    public string status { get; set; }
    public string transactionId { get; set; }
    public string type { get; set; }
    public string updatedAt { get; set; }
}

/// <summary>
///     Request to start payment of an online checkout with a specific payment method, for example to show the
///     wallet's QR code on your own page instead of redirecting to the checkout page.
/// </summary>
public class CheckoutByMethodRequest
{
    public string checkoutId { get; set; }

    /// <summary>
    ///     FPX details, when paying by FPX.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public CheckoutFpx fpx { get; set; }

    /// <summary>
    ///     GoBiz details, when paying by GoBiz.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public CheckoutGobiz gobiz { get; set; }

    public string method { get; set; }
    public string type { get; set; }
}

public class CheckoutFpx
{
    public string bankCode { get; set; }
}

public class CheckoutGobiz
{
    public string type { get; set; }
}

/// <summary>
///     Result of starting payment of an online checkout with a specific payment method.
/// </summary>
public class CheckoutByMethodResult
{
    public string data { get; set; }
    public CheckoutQrCode qrcode { get; set; }
    public PaymentTransaction transaction { get; set; }
    public string type { get; set; }
    public string url { get; set; }
}

public class CheckoutQrCode
{
    public string base64Image { get; set; }
    public string data { get; set; }
}

/// <summary>
///     A bank available for FPX payments.
/// </summary>
public class FpxBank
{
    public string code { get; set; }
    public bool isOnline { get; set; }
    public string name { get; set; }
}