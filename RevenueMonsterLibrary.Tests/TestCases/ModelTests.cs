using Newtonsoft.Json;
using RevenueMonsterLibrary.Constants;
using RevenueMonsterLibrary.Model;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class ModelTests
{
    [TestMethod]
    public void WebPayment_OptionalPropertiesUnset_SerializesAsBefore()
    {
        // Arrange
        var payment = new WebPayment
        {
            layoutVersion = "v1",
            method = ["TNG_MY"],
            notifyUrl = "https://example.com/notify",
            redirectUrl = "https://example.com/return",
            storeId = "1",
            type = "WEB_PAYMENT",
            order = new Order
            {
                additionalData = "a", amount = 100, currencyType = "MYR", detail = "d", id = "o1", title = "t"
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(payment);

        // Assert - no new property appears unless it is set
        Assert.AreEqual(
            """{"layoutVersion":"v1","method":["TNG_MY"],"notifyUrl":"https://example.com/notify","order":{"additionalData":"a","amount":100,"currencyType":"MYR","detail":"d","id":"o1","title":"t"},"redirectUrl":"https://example.com/return","storeId":"1","type":"WEB_PAYMENT"}""",
            json);
    }

    [TestMethod]
    public void WebPayment_OptionalPropertiesSet_AreSentWithoutNullFields()
    {
        // Arrange
        var payment = new WebPayment
        {
            customer = new Customer { email = "a@example.com" },
            expiresInSeconds = 600,
            voucher = new VoucherCode { code = "V1" }
        };

        // Act
        var json = JsonConvert.SerializeObject(payment);

        // Assert
        StringAssert.Contains(json, "\"customer\":{\"email\":\"a@example.com\"}");
        StringAssert.Contains(json, "\"expiresInSeconds\":600");
        StringAssert.Contains(json, "\"voucher\":{\"code\":\"V1\"}");
    }

    [TestMethod]
    public void QuickPay_VoucherUnset_SerializesAsBefore()
    {
        // Arrange
        var quickPay = new QuickPay
        {
            authCode = "123",
            ipAddress = "127.0.0.1",
            storeId = "1",
            order = new Order { id = "o1", title = "t", amount = 100 }
        };

        // Act
        var json = JsonConvert.SerializeObject(quickPay);

        // Assert
        Assert.AreEqual(
            """{"authCode":"123","ipAddress":"127.0.0.1","order":{"additionalData":null,"amount":100,"detail":null,"id":"o1","title":"t"},"storeId":"1"}""",
            json);
    }

    [TestMethod]
    public void RefundRequest_MatchesDocumentedRequestBody()
    {
        // Arrange - request body from the Revenue Monster refund documentation
        const string documented = """
                                  {
                                    "transactionId": "180730103903010431152179",
                                    "refund": {
                                      "type": "FULL",
                                      "currencyType": "MYR",
                                      "amount": 100
                                    },
                                    "reason": "test"
                                  }
                                  """;
        var request = new RefundRequest
        {
            transactionId = "180730103903010431152179",
            refund = new RefundDetail { type = RefundTypes.Full, currencyType = CurrencyTypes.MalaysianRinggit, amount = 100 },
            reason = "test"
        };

        // Act & Assert
        Assert.AreEqual(SignatureHelper.GenerateCompactJsonFromRaw(documented), SignatureHelper.GenerateCompactJson(request));
    }

    [TestMethod]
    public void ReverseRequest_MatchesDocumentedRequestBody()
    {
        // Arrange - request body from the Revenue Monster reverse documentation
        const string documented = """{ "orderId": "180730103903010431152179" }""";
        var request = new ReverseRequest { orderId = "180730103903010431152179" };

        // Act & Assert
        Assert.AreEqual(SignatureHelper.GenerateCompactJsonFromRaw(documented), SignatureHelper.GenerateCompactJson(request));
    }

    [TestMethod]
    public void Notify_WithExtraInfo_MapsExtraFeesAndKeepsOtherProperties()
    {
        // Arrange
        const string body = """
                            {
                              "eventType": "PAYMENT",
                              "data": {
                                "transactionId": "t1",
                                "payee": { "userId": "u1", "subUserId": "s1" },
                                "extraInfo": {
                                  "extraFee": [ { "type": "SERVICE", "amount": 50, "isRefundAllowed": true } ],
                                  "card": { "brand": "VISA" }
                                }
                              }
                            }
                            """;

        // Act
        var notify = JsonConvert.DeserializeObject<Notify>(body)!;

        // Assert
        Assert.AreEqual("s1", notify.data.payee.subUserId);
        Assert.AreEqual(50, notify.data.extraInfo.extraFee.Single().amount);
        Assert.IsTrue(notify.data.extraInfo.extraFee.Single().isRefundAllowed);
        Assert.AreEqual("VISA", (string?)notify.data.extraInfo.extensionData["card"]["brand"]);
    }

    [TestMethod]
    public void PaymentTransactionByOrderID_LegacyName_StillDeserializesTheTransaction()
    {
        // Arrange
        const string body = """{"item":{"transactionId":"t1","status":"SUCCESS"},"code":"SUCCESS"}""";

        // Act
#pragma warning disable CS0618 // the obsolete name is what this test covers
        var response = JsonConvert.DeserializeObject<PaymentTransactionByOrderID>(body)!;
#pragma warning restore CS0618

        // Assert
        Assert.IsInstanceOfType<TransactionByOrderIdResponse>(response);
        Assert.AreEqual("t1", response.item.transactionId);
        Assert.AreEqual("SUCCESS", response.item.status);
    }

    [TestMethod]
    public void Notify_Data_IsAPaymentTransactionWithVoucher()
    {
        // Arrange
        const string body = """{"data":{"transactionId":"t1","order":{"id":"o1"},"voucher":{"code":"V1"}}}""";

        // Act
        var notify = JsonConvert.DeserializeObject<Notify>(body)!;

        // Assert
        Assert.IsInstanceOfType<PaymentTransaction>(notify.data);
        Assert.AreEqual("t1", notify.data.transactionId);
        Assert.AreEqual("o1", notify.data.order.id);
        Assert.AreEqual("V1", notify.data.voucher.code);
    }

    [TestMethod]
    public void Error_WithDebugAndDescription_Deserializes()
    {
        // Act
        var error = JsonConvert.DeserializeObject<Error>(
            """{"code":"VALIDATION","message":"Invalid","debug":"order.amount","description":{"field":"amount"}}""")!;

        // Assert
        Assert.AreEqual("VALIDATION", error.code);
        Assert.AreEqual("order.amount", error.debug);
        Assert.IsNotNull(error.description);
    }

    [TestMethod]
    public void GetVoucherBatchesResult_DeserializesItemsAndMeta()
    {
        // Act
        var result = JsonConvert.DeserializeObject<GetVoucherBatchesResult>(
            """{"code":"SUCCESS","items":[{"key":"k1"}],"meta":{"count":1,"cursor":"c"}}""")!;

        // Assert
        Assert.AreEqual("SUCCESS", result.code);
        Assert.AreEqual("k1", result.items.Single().key);
        Assert.AreEqual(1, result.meta.count);
    }
}
