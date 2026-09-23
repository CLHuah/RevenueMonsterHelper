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