using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper;

public class RandomString
{
    private static readonly Random Random = new Random((int) DateTime.Now.Ticks);

    public static string GenerateRandomString(int size)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 65)));
            builder.Append(ch);
        }

        return builder.ToString();
    }
}