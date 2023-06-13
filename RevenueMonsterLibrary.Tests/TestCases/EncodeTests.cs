namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class EncodeTests
{
    [TestMethod]
    public void Base64Encode_EncodesPlainTextToBase64()
    {
        // Arrange
        const string plainText = "Hello, World!";
        const string expectedBase64 = "SGVsbG8sIFdvcmxkIQ==";

        // Act
        var actualBase64 = Encode.Base64Encode(plainText);

        // Assert
        Assert.AreEqual(expectedBase64, actualBase64);
    }
}