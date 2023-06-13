namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class DecodeTests
{
    [TestMethod]
    public void Base64Decode_DecodesBase64EncodedString()
    {
        // Arrange
        const string base64EncodedString = "SGVsbG8gV29ybGQh"; // "Hello World!" in base64

        // Act
        var decodedString = Decode.Base64Decode(base64EncodedString);

        // Assert
        const string expectedString = "Hello World!";
        Assert.AreEqual(expectedString, decodedString);
    }
}