using System;
using System.Text;

namespace RevenueMonsterLibrary.Helper
{
    public static class Encode
    {
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}