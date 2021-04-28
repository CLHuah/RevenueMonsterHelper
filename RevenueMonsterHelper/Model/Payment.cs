using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model
{
    public class ExtraInfo
    {
        public string reference { get; set; }
        public string type { get; set; }
    }

    public class QuickPay
    {
        public string authCode { get; set; }
        public ExtraInfo extraInfo { get; set; }
        public string ipAddress { get; set; }
        public Order order { get; set; }
        public string storeId { get; set; }
    }


    public class PaymentTransactionByOrderID
    {
        public string code { get; set; }
        public Error error { get; set; }
        public TransactionQuickPay item { get; set; }
    }

    public class TransactionQuickPay
    {
        public long balanceAmount { get; set; }
        public string createdAt { get; set; }
        public string currencyType { get; set; }
        public Error error { get; set; }
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
        public string userId { get; set; }
    }

    public class WebPayment
    {
        public WebPayment()
        {
            order = new Order();
        }

        public string layoutVersion { get; set; } // v1 / v2 (Supported Credit Card)
        public IList<string> method { get; set; }

        public string notifyUrl { get; set; }

        public Order order { get; set; }

        public string redirectUrl { get; set; }
        public string storeId { get; set; }

        public string type { get; set; } // WEB_PAYMENT or MOBILE_PAYMENT
    }

    public class Order
    {
        public string additionalData { get; set; }
        public long amount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string currencyType { get; set; }

        public string detail { get; set; }
        public string id { get; set; }
        public string title { get; set; }
    }

    public class WebPaymentResponse
    {
        public string code { get; set; }
        public Item item { get; set; }
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
}