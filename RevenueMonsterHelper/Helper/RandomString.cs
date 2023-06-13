using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

public class RandomString
{
    private static readonly Random Random = new((int)DateTime.Now.Ticks);

    /// <summary>
    ///     Generates a random string of the specified size consisting of uppercase letters.
    /// </summary>
    /// <param name="size">The size of the random string to generate.</param>
    /// <returns>A random string of the specified size.</returns>
    public static string GenerateRandomString(int size)
    {
        var builder = new StringBuilder();

        // Loop to generate individual characters for the random string
        for (var i = 0; i < size; i++)
        {
            // Generate a random character within the range of uppercase letters (A-Z)
            var ch = (char)(Random.Next(26) + 'A');

            // Append the generated character to the string builder
            builder.Append(ch);
        }

        // Return the generated random string
        return builder.ToString();
    }
}