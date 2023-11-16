namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class SignatureHelperTests
{
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