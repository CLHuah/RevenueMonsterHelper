namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class RandomStringTests
{
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
}