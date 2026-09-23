using System;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model;

// One response type per API call. Calls that return the same item still get their own type, so a call's response
// can gain fields without affecting the others.

/// <summary>
///     Response of creating an online checkout.
/// </summary>
public class WebPaymentResponse : ApiResponse<Item>
{
}

/// <summary>
///     Response of getting an online checkout.
/// </summary>
public class OnlineCheckoutResponse : ApiResponse<OnlineCheckout>
{
}

/// <summary>
///     Response of starting an online checkout payment with a specific payment method.
/// </summary>
public class CheckoutByMethodResponse : ApiResponse<CheckoutByMethodResult>
{
}

/// <summary>
///     Response of getting the FPX banks, keyed by bank code.
/// </summary>
public class FpxBankListResponse : ApiResponse<Dictionary<string, FpxBank>>
{
}

/// <summary>
///     Response of a QuickPay payment.
/// </summary>
public class QuickPayResponse : ApiResponse<PaymentTransaction>
{
}

/// <summary>
///     Response of getting a transaction by its transaction ID.
/// </summary>
public class TransactionByIdResponse : ApiResponse<PaymentTransaction>
{
}

/// <summary>
///     Response of getting a transaction by its order ID.
/// </summary>
public class TransactionByOrderIdResponse : ApiResponse<PaymentTransaction>
{
}

/// <summary>
///     Earlier name of <see cref="TransactionByOrderIdResponse" />, kept so existing code still compiles.
/// </summary>
[Obsolete("Use TransactionByOrderIdResponse.")]
public class PaymentTransactionByOrderID : TransactionByOrderIdResponse
{
}

/// <summary>
///     Response of a refund.
/// </summary>
public class RefundResponse : ApiResponse<PaymentTransaction>
{
}

/// <summary>
///     Response of a reversal.
/// </summary>
public class ReverseResponse : ApiResponse<PaymentTransaction>
{
}

/// <summary>
///     Response of creating a transaction QR code.
/// </summary>
public class CreateTransactionQrCodeResponse : ApiResponse<TransactionQrCode>
{
}

/// <summary>
///     Response of getting a transaction QR code by its code.
/// </summary>
public class TransactionQrCodeResponse : ApiResponse<TransactionQrCode>
{
}

/// <summary>
///     Response of getting the transactions paid through a transaction QR code.
/// </summary>
public class QrCodeTransactionsResponse : ApiListResponse<PaymentTransaction>
{
}
