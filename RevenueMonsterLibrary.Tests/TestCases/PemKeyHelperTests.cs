using System.Security.Cryptography;

namespace RevenueMonsterLibrary.Tests.TestCases;

[TestClass]
public class PemKeyHelperTests
{
    private const string SamplePrivateKey = """
                                            -----BEGIN RSA PRIVATE KEY-----
                                            MIIEowIBAAKCAQEAgmdGkwQhb7NrN9Ia2cYdmXNdaFxtaTXHTG9LfouAMzdP6PcQ
                                            TupLocJO8UoUFVC1CHYtv0AUT4aaeSW24yx35eDGq/qdtP9Ie6UluUExxBXMYuai
                                            L9wCYltn52YsA/tbrf4dsX8jAoTbjFDnVnUcW9SsJm+EmIJePVm5HBSxTzFF5sYt
                                            xAN4V8uu9HNIAc7Lf4puvSiQ+7/iyJSBQDBRetWuqFT7uxUBp3mVw6ssAwIkDAYi
                                            RPqTBUjy/GMMyo88UVnc8jgm9r0ZHYQo4aQkomnS/NvBXoUP7u/tPg3qydE4cLoI
                                            U9ns84NdxI4bwIeMHaIWGMqlLjYPgyLHv8/ONwIDAQABAoIBACTXsOTQgfHhKyW2
                                            QsfMZYh5Q6a8llznSMubliTGnQ3bTsRvKThikcO99jfNyibLipo9aWdjX1momfQo
                                            Z6d/ZNCZ1Qe54tzEU2I7opDYjorr7bbzmlcTPck0MgL6puzpE1nxNcp0NRv9FVpr
                                            cTDIHZ8EUy74yumby6xhsR7x6baJttsz7TFcwdGwQ8eUjkKzZSK2MxRC/HdqUBuC
                                            hOpe4NbKFpc++nQT0xAMcgJPqM5e+xOHxoiqjXBjoY4t4CNG1TEsFbp8nHpteZUd
                                            HvwgT6VPV2AbGB1yNZeUsiUeTd/JmwLtFW46D724VSb/voA4OIndLApCaubaiXJk
                                            Os/7sqECgYEA6iA0wSkiZgZMqLbu+4UjhGxDqPfm9TzGO/+faxYMW8JCStv0NBlB
                                            AQog0XvXMYv/JJdZM6fSqAfd+uV8EatXI+pJGjtOkWpbAcJI+7BYoiXajHR0Rek1
                                            sEgRz4ePQ4XLwH8hFcoe2dQo3zc4HGdGCsL8Vt/V6w2HexIz+2u46xUCgYEAjpY/
                                            5sCuCag9FRBURRYrMmr/PYYgXGPoPMxZkTPgcfcHO0w31OsEvJfSvOMY5hXKg1Hn
                                            zUJ/macvUzRjDM2a+LavVLxMwfhrU6ge+Gpu6Dj+JkxryvosGnE/NIc0OpC8CsRp
                                            xMGYJqXLmwrq9uNBfquy4gQplMe752Iy6lr3txsCgYEAwXt8HWVxF/98urDzLskI
                                            YRdXkvvL0j83U74ccNL3w5z2TAcZ+TQllEZaeMRvQnL/l627+gRnApX9zKp5reB7
                                            UyL65sK8W1AZhYqZ5eYGWXoO8qQKsvvXcqcckPTmYFbh22M0ZF10wW+jl1R6+n4e
                                            VCZvgxvYHThBDmQssUqEt0ECgYB4BMhs9yqHPnGL1V7ZeDuMCLwnkUqT+hR1eJy7
                                            uCroQsJ3i0RDmD84b6MXEyDWKul3d+3hvGOjdjJDmwj0sewMBdm3PXhunSfaFt1a
                                            xFvuZGqo2hKxm7qb108Ya+Xulf1yIrnUwA/OGCg1rQ7yGX/7m5LK5C4L6cOkk+e/
                                            lCqGmQKBgHP8Vu/acxzW1KjC7IotN66bc9P2xlJA+UQIVtQKisRqGQR2qiCDVCCy
                                            jI+wGu4GACrItk0vXwky8m4tkXaF+Fa3Mpy/uFF5oERMIkCjpopHfABRHRGifhxp
                                            v3mozZ6/hG6RCLBIOPSnsgRjfFqQWHObYPxartNixWDcsCkhdNFy
                                            -----END RSA PRIVATE KEY-----
                                            """;

    private const string SamplePublicKey = """
                                           -----BEGIN PUBLIC KEY-----
                                               MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAgmdGkwQhb7NrN9Ia2cYd
                                                   mXNdaFxtaTXHTG9LfouAMzdP6PcQTupLocJO8UoUFVC1CHYtv0AUT4aaeSW24yx3
                                               5eDGq/qdtP9Ie6UluUExxBXMYuaiL9wCYltn52YsA/tbrf4dsX8jAoTbjFDnVnUc
                                                   W9SsJm+EmIJePVm5HBSxTzFF5sYtxAN4V8uu9HNIAc7Lf4puvSiQ+7/iyJSBQDBR
                                                   etWuqFT7uxUBp3mVw6ssAwIkDAYiRPqTBUjy/GMMyo88UVnc8jgm9r0ZHYQo4aQk
                                                   omnS/NvBXoUP7u/tPg3qydE4cLoIU9ns84NdxI4bwIeMHaIWGMqlLjYPgyLHv8/O
                                                   NwIDAQAB
                                               -----END PUBLIC KEY-----
                                           """;

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GetRSAProviderFromPemFile_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        const string pemStr = "Invalid PEM format";

        // Act
        PemKeyHelper.CreateRSAFromPem(pemStr);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GetRSAProviderFromPemFile_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? pemStr = null;

        // Act
        PemKeyHelper.CreateRSAFromPem(pemStr);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    public void GetRSAProviderFromPemFile_WithValidPrivateKey_ReturnsRSAProvider()
    {
        // Act
        var result = PemKeyHelper.CreateRSAFromPem(SamplePrivateKey);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(RSA));
    }

    [TestMethod]
    public void GetRSAProviderFromPemFile_WithValidPublicKey_ReturnsRSAProvider()
    {
        // Act
        var result = PemKeyHelper.CreateRSAFromPem(SamplePublicKey);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(RSA));
    }
}