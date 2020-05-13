using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RevenueMonsterLibrary.Model
{
    public class Store
    {
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string countryCode { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public GeoLocation geoLocation { get; set; }
        public string id { get; set; }
        public Uri imageUrl { get; set; }
        public string name { get; set; }
        public string phoneNumber { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string status { get; set; }
        public DateTimeOffset updatedAt { get; set; }
    }

    public class GeoLocation
    {
        public long latitude { get; set; }
        public long longitude { get; set; }
    }

    public class Payee
    {
        public string userId { get; set; }
    }

    public class WebPayment
    {
        public WebPayment()
        {
            order = new Order();
        }

        public Order order { get; set; }
        public IList<string> method { get; set; }

        public string type { get; set; } // WEB_PAYMENT or MOBILE_PAYMENT
        public string storeId { get; set; }

        public string redirectUrl { get; set; }

        public string notifyUrl { get; set; }
        public string layoutVersion { get; set; } // v1 / v2 (Supported Credit Card)
    }

    public class Order
    {
        public string id { get; set; }
        public string title { get; set; }
        public string detail { get; set; }
        public string additionalData { get; set; }
        public long amount { get; set; }
        public string currencyType { get; set; }
    }

    public class WebPaymentResponse
    {
        public Item item { get; set; }
        public string code { get; set; }
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
        public DateTimeOffset createdAt { get; set; }
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
        public DateTimeOffset transactionAt { get; set; }
        public string transactionId { get; set; }
        public string type { get; set; }
        public DateTimeOffset updatedAt { get; set; }
    }
}