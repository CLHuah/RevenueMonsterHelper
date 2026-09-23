using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevenueMonsterLibrary.Model;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class SignatureHelperTests
{
    private const string Method = "post";
    private const string NonceStr = "VYNknZohxwicZMaWbNdBKUrnrxDtaRhN";
    private const string RequestUrl = "https://sb-open.revenuemonster.my/v3/payment/online";
    private const string SignType = "sha256";
    private const string Timestamp = "1528450585";

    // Reference signature produced independently with OpenSSL over the same input:
    //   printf '%s' "data=<base64 compact json>&method=post&nonceStr=...&timestamp=1528450585" |
    //     openssl dgst -sha256 -sign private.pem | base64
    private const string ExpectedSignature =
        "L+9xMTlg7mg9XqdkG1jz2Kzjz4wyyFLHzXpbf9wl1H58WC6qb+Wr9s8l8osqwsYsBfKfU957rEZFf9FvdPZ98uuS7xF5/7R00l8zJb0ZVab+8ZDmeg6a2ymiYvR0hSADrakhF69Fef8on63CYvy2bxEyLftTqvpQTactVOWEXPmIZkkTd/Dx+qMbuTtbFzE4ETOV67g7F6T6s+1WMNMH86YSOc9m5KCKppjHbL+CtxqZDlBdCfcKoDSVYPwae8zsw0GiBTuya2K0ehL+www1kv2sTmYeXXzbijuWmqu6BZH5SOnYKRpk3RlW9MeaEEmICQ30z2uQbUbq30gGyqBJSw==";

    private static readonly object SignedPayload = new
    {
        storeId = "123",
        order = new
        {
            title = "A&B",
            amount = 100
        }
    };

    // A webhook body in the order and layout it arrives in, including a property no model declares
    private const string WebhookBody = """
                                       {
                                         "eventType": "PAYMENT",
                                         "data": {
                                           "status": "SUCCESS",
                                           "order": { "id": "o1", "amount": 1865 },
                                           "extraInfo": { "card": {} }
                                         }
                                       }
                                       """;

    private const string WebhookCanonicalJson =
        """{"data":{"extraInfo":{"card":{}},"order":{"amount":1865,"id":"o1"},"status":"SUCCESS"},"eventType":"PAYMENT"}""";

    [TestMethod]
    public void GenerateCompactJsonFromRaw_WebhookBody_KeepsEveryPropertySorted()
    {
        // Act
        var result = SignatureHelper.GenerateCompactJsonFromRaw(WebhookBody);

        // Assert
        Assert.AreEqual(WebhookCanonicalJson, result);
    }

    [TestMethod]
    public void GenerateCompactJson_JObjectInput_MatchesRawBody()
    {
        // Act
        var result = SignatureHelper.GenerateCompactJson(JObject.Parse(WebhookBody));

        // Assert
        Assert.AreEqual(WebhookCanonicalJson, result);
    }

    [TestMethod]
    public void GenerateCompactJson_JsonElementInput_MatchesRawBody()
    {
        // Arrange
        using var document = JsonDocument.Parse(WebhookBody);

        // Act
        var result = SignatureHelper.GenerateCompactJson(document.RootElement);

        // Assert
        Assert.AreEqual(WebhookCanonicalJson, result);
    }

    [TestMethod]
    public void GenerateCompactJsonFromRaw_DateStrings_AreKeptAsSent()
    {
        // Arrange
        const string body = """{"createdAt":"2023-01-01T00:00:00.000Z","updatedAt":"2023-01-01T08:00:00+08:00"}""";

        // Act
        var result = SignatureHelper.GenerateCompactJsonFromRaw(body);

        // Assert
        Assert.AreEqual(body, result);
    }

    [TestMethod]
    public void GenerateCompactJson_NonAsciiText_IsWrittenAsIs()
    {
        // Act
        var result = SignatureHelper.GenerateCompactJson(new { name = "Teh Tarik 🍵", note = "中文" });

        // Assert
        Assert.AreEqual("{\"name\":\"Teh Tarik 🍵\",\"note\":\"中文\"}", result);
    }

    [TestMethod]
    public void GenerateCompactJson_LineSeparatorsAndControlCharacters_AreEscaped()
    {
        // Arrange - U+2028 and U+2029 are built from code points to keep them out of the source text
        var text = "a" + (char)0x2028 + "b" + (char)0x2029 + "c\n\t" + (char)0x01;

        // Act
        var result = SignatureHelper.GenerateCompactJson(new { text });

        // Assert
        Assert.AreEqual("{\"text\":\"a\\u2028b\\u2029c\\n\\t\\u0001\"}", result);
    }

    [TestMethod]
    public void GenerateCompactJson_NullProperties_MatchSerializedRequestBody()
    {
        // Arrange - properties left unset are sent as null
        var payment = new WebPayment
        {
            storeId = "1",
            type = "WEB_PAYMENT",
            order = new Order { id = "o1", title = "t", amount = 100, currencyType = "MYR" }
        };
        var requestBody = JsonConvert.SerializeObject(payment);

        // Act
        var result = SignatureHelper.GenerateCompactJson(payment);

        // Assert
        Assert.AreEqual(SignatureHelper.GenerateCompactJsonFromRaw(requestBody), result);
        Assert.AreEqual(
            "{\"layoutVersion\":null,\"method\":null,\"notifyUrl\":null,\"order\":{\"additionalData\":null,\"amount\":100,\"currencyType\":\"MYR\",\"detail\":null,\"id\":\"o1\",\"title\":\"t\"},\"redirectUrl\":null,\"storeId\":\"1\",\"type\":\"WEB_PAYMENT\"}",
            result);
    }

    [TestMethod]
    public void GenerateCompactJson_NullPropertiesMarkedIgnore_AreLeftOut()
    {
        // Arrange - extraInfo, terminalId and order.currencyType are marked NullValueHandling.Ignore
        var quickPay = new QuickPay
        {
            authCode = "123",
            ipAddress = "127.0.0.1",
            storeId = "1",
            order = new Order { id = "o1", title = "t", amount = 100 }
        };

        // Act
        var result = SignatureHelper.GenerateCompactJson(quickPay);

        // Assert
        Assert.AreEqual(
            "{\"authCode\":\"123\",\"ipAddress\":\"127.0.0.1\",\"order\":{\"additionalData\":null,\"amount\":100,\"detail\":null,\"id\":\"o1\",\"title\":\"t\"},\"storeId\":\"1\"}",
            result);
    }

    [TestMethod]
    public void GenerateCompactJson_ObjectsInsideArrays_AreSorted()
    {
        // Act
        var result = SignatureHelper.GenerateCompactJson(new
        {
            items = new[] { new { b = 1, a = 2 } },
            tags = new[] { "z", "a" }
        });

        // Assert - array order is kept, object keys are sorted
        Assert.AreEqual("{\"items\":[{\"a\":2,\"b\":1}],\"tags\":[\"z\",\"a\"]}", result);
    }

    [TestMethod]
    public void GenerateCompactJson_MixedCaseKeys_AreSortedByOrdinal()
    {
        // Act
        var result = SignatureHelper.GenerateCompactJson(new { b = 1, B = 2, a = 3 });

        // Assert
        Assert.AreEqual("{\"B\":2,\"a\":3,\"b\":1}", result);
    }

    [TestMethod]
    [DataRow("1865", "1865")]
    [DataRow("-2.5", "-2.5")]
    [DataRow("100.50", "100.5")]
    [DataRow("100.0", "100")]
    [DataRow("1e2", "100")]
    [DataRow("0.0000015", "0.0000015")]
    [DataRow("0.00000015", "1.5e-7")]
    [DataRow("1000000000000000", "1000000000000000")]
    [DataRow("1e21", "1e+21")]
    public void GenerateCompactJsonFromRaw_Numbers_UseCanonicalFormat(string number, string expected)
    {
        // Act
        var result = SignatureHelper.GenerateCompactJsonFromRaw($"{{\"n\":{number}}}");

        // Assert
        Assert.AreEqual($"{{\"n\":{expected}}}", result);
    }

    [TestMethod]
    public void GenerateCompactJson_DecimalAndFloatProperties_UseCanonicalFormat()
    {
        // Act - JsonConvert writes these as 100.0, 1.0 and 0.25 in the request body
        var result = SignatureHelper.GenerateCompactJson(new { amount = 100m, day = 1f, rate = 0.25m });

        // Assert
        Assert.AreEqual("{\"amount\":100,\"day\":1,\"rate\":0.25}", result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("{}")]
    [DataRow("null")]
    public void BuildSignatureInput_EmptyBody_LeavesOutData(string? compactJson)
    {
        // Act
        var result = SignatureHelper.BuildSignatureInput(compactJson, "get", NonceStr, RequestUrl, SignType, Timestamp);

        // Assert
        Assert.AreEqual(
            $"method=get&nonceStr={NonceStr}&requestUrl={RequestUrl}&signType={SignType}&timestamp={Timestamp}",
            result);
    }

    [TestMethod]
    public void BuildSignatureInput_EmptyRequestUrl_LeavesOutRequestUrl()
    {
        // Act
        var result = SignatureHelper.BuildSignatureInput(null, "get", NonceStr, "", SignType, Timestamp);

        // Assert
        Assert.AreEqual($"method=get&nonceStr={NonceStr}&signType={SignType}&timestamp={Timestamp}", result);
    }

    [TestMethod]
    public void BuildSignatureInput_UppercaseMethod_IsLowercased()
    {
        // Act
        var result = SignatureHelper.BuildSignatureInput("{\"a\":1}", "POST", NonceStr, RequestUrl, SignType,
            Timestamp);

        // Assert
        Assert.AreEqual(
            $"data=eyJhIjoxfQ==&method=post&nonceStr={NonceStr}&requestUrl={RequestUrl}&signType={SignType}&timestamp={Timestamp}",
            result);
    }

    [TestMethod]
    public void GenerateSignature_UppercaseMethodAndSignType_MatchesReferenceSignature()
    {
        // Act
        var signature = SignatureHelper.GenerateSignature(SignedPayload, "POST", NonceStr, TestKeys.PrivateKey,
            RequestUrl, "SHA256", Timestamp);

        // Assert
        Assert.AreEqual(ExpectedSignature, signature);
    }

    [TestMethod]
    public void GenerateSignature_EmptyObjectBody_SignsLikeNoBody()
    {
        // Act
        var withEmptyObject = SignatureHelper.GenerateSignature(new { }, "get", NonceStr, TestKeys.PrivateKey,
            RequestUrl, SignType, Timestamp);
        var withoutBody = SignatureHelper.GenerateSignature((object?)null, "get", NonceStr, TestKeys.PrivateKey,
            RequestUrl, SignType, Timestamp);

        // Assert
        Assert.AreEqual(withoutBody, withEmptyObject);
    }

    [TestMethod]
    public void VerifySignatureFromRawBody_ReorderedAndIndentedBody_ReturnsTrue()
    {
        // Arrange - same content as SignedPayload, in a different key order and layout
        const string rawBody = """
                               {
                                 "storeId": "123",
                                 "order": { "title": "A&B", "amount": 100 }
                               }
                               """;

        // Act
        var isValid = SignatureHelper.VerifySignatureFromRawBody(rawBody, Method, NonceStr, TestKeys.PublicKey,
            RequestUrl, SignType, Timestamp, ExpectedSignature);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifySignatureFromRawBody_TamperedBody_ReturnsFalse()
    {
        // Arrange
        const string rawBody = """{"storeId":"123","order":{"title":"A&B","amount":999}}""";

        // Act
        var isValid = SignatureHelper.VerifySignatureFromRawBody(rawBody, Method, NonceStr, TestKeys.PublicKey,
            RequestUrl, SignType, Timestamp, ExpectedSignature);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void VerifySignature_JObjectBody_ReturnsTrue()
    {
        // Arrange
        var body = JObject.Parse("""{"storeId":"123","order":{"title":"A&B","amount":100}}""");

        // Act
        var isValid = SignatureHelper.VerifySignature(body, Method, NonceStr, TestKeys.PublicKey, RequestUrl,
            SignType, Timestamp, ExpectedSignature);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not base64!")]
    public void VerifySignature_MalformedSignature_ReturnsFalse(string signature)
    {
        // Act
        var isValid = SignatureHelper.VerifySignature(SignedPayload, Method, NonceStr, TestKeys.PublicKey,
            RequestUrl, SignType, Timestamp, signature);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void GenerateSignature_KnownPayload_MatchesOpenSslReferenceSignature()
    {
        // Act
        var signature = SignatureHelper.GenerateSignature(SignedPayload, Method, NonceStr, TestKeys.PrivateKey,
            RequestUrl, SignType, Timestamp);

        // Assert
        Assert.AreEqual(ExpectedSignature, signature);
    }

    [TestMethod]
    public void VerifySignature_OpenSslReferenceSignature_ReturnsTrue()
    {
        // Act
        var isValid = SignatureHelper.VerifySignature(SignedPayload, Method, NonceStr, TestKeys.PublicKey, RequestUrl,
            SignType, Timestamp, ExpectedSignature);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifySignature_TamperedPayload_ReturnsFalse()
    {
        // Arrange
        var tampered = new { storeId = "123", order = new { title = "A&B", amount = 999 } };

        // Act
        var isValid = SignatureHelper.VerifySignature(tampered, Method, NonceStr, TestKeys.PublicKey, RequestUrl,
            SignType, Timestamp, ExpectedSignature);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void GenerateCompactJson_ComplexObject_ReturnsSortedAndEscapedResult()
    {
        // Arrange
        var testObject = new
        {
            zValue = "last",
            specialText = "a<b>&c",
            nested = new
            {
                b = 2,
                a = 1
            },
            aValue = "first"
        };

        // Act
        var result = SignatureHelper.GenerateCompactJson(testObject);

        // Assert
        Assert.AreEqual(
            "{\"aValue\":\"first\",\"nested\":{\"a\":1,\"b\":2},\"specialText\":\"a\\u003cb\\u003e\\u0026c\",\"zValue\":\"last\"}",
            result);
    }

    [TestMethod]
    public void GenerateCompactJson_NullInput_ShouldThrowException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => SignatureHelper.GenerateCompactJson(null!));
    }

    [TestMethod]
    public void GenerateCompactJson_ShouldReturnSortedJson()
    {
        // Arrange
        var testData = new OuterClass
        {
            OuterId = 2,
            OuterName = "Outer",
            Inner = new InnerClass
            {
                InnerId = 1,
                InnerName = "Inner"
            }
        };

        // Act
        var result = SignatureHelper.GenerateCompactJson(testData);

        // Assert
        const string expectedJson =
            "{\"Inner\":{\"InnerId\":1,\"InnerName\":\"Inner\"},\"OuterId\":2,\"OuterName\":\"Outer\"}";

        Assert.AreEqual(expectedJson, result);
    }

    [TestMethod]
    public void GenerateCompactJson_SpecialCharacters_ReturnsEscapedCharacters()
    {
        // Arrange
        var testObject = new
        {
            text = "a<b>c&d"
        };

        // Act
        var result = SignatureHelper.GenerateCompactJson(testObject);

        // Assert
        Assert.AreEqual("{\"text\":\"a\\u003cb\\u003ec\\u0026d\"}", result);
    }

    // Define a sample class for testing
    private class InnerClass
    {
        public int InnerId { get; set; }
        public string? InnerName { get; set; }
    }

    private class OuterClass
    {
        public InnerClass? Inner { get; set; }
        public int OuterId { get; set; }
        public string? OuterName { get; set; }
    }
}