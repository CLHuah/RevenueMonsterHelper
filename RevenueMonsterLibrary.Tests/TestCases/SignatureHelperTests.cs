namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class SignatureHelperTests
{
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
    [ExpectedException(typeof(ArgumentNullException))]
    public void GenerateCompactJson_NullInput_ShouldThrowException()
    {
        // Act
        SignatureHelper.GenerateCompactJson(null);

        // Assert
        // Exception is expected, so nothing to assert explicitly
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
        public string InnerName { get; set; }
    }

    private class OuterClass
    {
        public InnerClass Inner { get; set; }
        public int OuterId { get; set; }
        public string OuterName { get; set; }
    }
}