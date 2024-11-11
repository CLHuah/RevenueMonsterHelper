namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class RandomStringTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void GenerateRandomString_InvalidSize_ThrowsException(int invalidSize)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => RandomString.GenerateRandomString(invalidSize));
    }

    [TestMethod]
    public void GenerateRandomString_ShouldReturnStringWithSpecifiedSize()
    {
        // Arrange
        const int size = 30;

        // Act
        var randomString = RandomString.GenerateRandomString(size);

        // Assert
        Assert.AreEqual(size, randomString.Length);
    }

    [TestMethod]
    public void GenerateRandomString_UniqueResults()
    {
        // Arrange
        const int numberOfStringsToGenerate = 100;
        var generatedStrings = new HashSet<string>();

        // Act
        for (var i = 0; i < numberOfStringsToGenerate; i++)
        {
            var result = RandomString.GenerateRandomString(10);

            // Assert uniqueness
            Assert.IsFalse(generatedStrings.Contains(result), $"Duplicate string generated: {result}");

            // Add to the set for future comparison
            generatedStrings.Add(result);
        }
    }
}