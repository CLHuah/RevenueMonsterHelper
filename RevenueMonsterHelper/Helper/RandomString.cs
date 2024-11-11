using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     A static class that provides a method to generate a random string of uppercase letters.
/// </summary>
public static class RandomString
{
    // A thread-safe random number generator that is initialized with the current time as the seed.
    private static readonly Random Random = new((int)DateTime.Now.Ticks);

    /// <summary>
    ///     Generates a random string of the specified size consisting of uppercase letters.
    /// </summary>
    /// <param name="size">The size of the random string to generate.</param>
    /// <returns>A random string of the specified size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the size is negative.</exception>
    public static string GenerateRandomString(int size)
    {
        // Validate the size parameter and throw an exception if it is less than or equal to zero.
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        // Use a StringBuilder to efficiently append characters to the random string.
        var builder = new StringBuilder();

        // Loop to generate individual characters for the random string
        for (var i = 0; i < size; i++)
        {
            // Generate a random number between 0 and 25, inclusive, and add it to the ASCII code of 'A' to get a random uppercase letter.
            var ch = (char)(Random.Next(26) + 'A');

            // Append the generated character to the string builder
            builder.Append(ch);
        }

        // Return the generated random string
        return builder.ToString();
    }
}