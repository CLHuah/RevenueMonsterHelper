using System;
using System.Security.Cryptography;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     A static class that provides a method to generate a random string of uppercase letters.
/// </summary>
public static class RandomString
{
    // The characters a generated string is made of
    private const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    ///     Generates a random string of the specified size consisting of uppercase letters.
    /// </summary>
    /// <param name="size">The size of the random string to generate.</param>
    /// <returns>A random string of the specified size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the size is zero or negative.</exception>
    /// <remarks>
    ///     Uses a cryptographically secure random number generator, so it is safe to call from multiple threads and
    ///     suitable for nonces.
    /// </remarks>
    public static string GenerateRandomString(int size)
    {
        // Validate the size parameter and throw an exception if it is less than or equal to zero.
        return size <= 0
            ? throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.")
            :
            // Pick each character uniformly from the allowed set
            RandomNumberGenerator.GetString(UppercaseLetters, size);
    }
}