using System.Security.Cryptography;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class PemKeyHelperTests
{
    [TestMethod]
    public void CreateRSAFromPem_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        const string pemStr = "Invalid PEM format";

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() => PemKeyHelper.CreateRSAFromPem(pemStr));
    }

    [TestMethod]
    public void CreateRSAFromPem_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => PemKeyHelper.CreateRSAFromPem(null!));
    }

    [TestMethod]
    public void CreateRSAFromPem_WithPkcs8PrivateKey_ReturnsRSAWithPrivateKey()
    {
        // Arrange - re-encode the PKCS#1 sample key as PKCS#8 ("BEGIN PRIVATE KEY")
        using var source = PemKeyHelper.CreateRSAFromPem(TestKeys.PrivateKey);
        var pkcs8Pem = source.ExportPkcs8PrivateKeyPem();

        // Act
        using var result = PemKeyHelper.CreateRSAFromPem(pkcs8Pem);

        // Assert
        Assert.AreSequenceEqual(source.ExportParameters(false).Modulus, result.ExportParameters(false).Modulus);
    }

    [TestMethod]
    public void CreateRSAFromPem_WithValidPrivateKey_ReturnsRSAWithPrivateKey()
    {
        // Act
        using var result = PemKeyHelper.CreateRSAFromPem(TestKeys.PrivateKey);

        // Assert
        Assert.AreEqual(2048, result.KeySize);
        Assert.IsNotNull(result.ExportParameters(true).D);
    }

    [TestMethod]
    public void CreateRSAFromPem_WithValidPublicKey_ReturnsRSAWithPublicKeyOnly()
    {
        // Act
        using var result = PemKeyHelper.CreateRSAFromPem(TestKeys.PublicKey);

        // Assert
        Assert.AreEqual(2048, result.KeySize);
        Assert.ThrowsExactly<CryptographicException>(() => result.ExportParameters(true));
    }
}