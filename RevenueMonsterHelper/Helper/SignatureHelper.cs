using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RevenueMonsterLibrary.Helper
{
    /// <summary>
    ///     Signature algorithm is used to sign your payment API request with a private key to obtain additional security.
    /// </summary>
    public class SignatureHelper
    {
        public static string GenerateSignature(object data, string method, string nonceStr, string privateKey,
            string requestUrl, string signType, string timestamp)
        {
            string plainText;
            if (data != null)
            {
                var encodedData = Encode.Base64Encode(GenerateCompactJson(data));
                plainText =
                    $"data={encodedData}&method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";
            }
            else
            {
                plainText =
                    $"method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";
            }

            var plainTextByte = Encoding.UTF8.GetBytes(plainText);
            var provider = PemKeyHelper.GetRSAProviderFromPemFile(privateKey);
            var signedBytes = provider.SignData(plainTextByte, CryptoConfig.MapNameToOID("SHA256"));
            return Convert.ToBase64String(signedBytes);
        }

        public static string GenerateSignature(string compactJson, string method, string nonceStr, string privateKey,
            string requestUrl, string signType, string timestamp)
        {
            string plainText;
            if (!string.IsNullOrWhiteSpace(compactJson))
            {
                var encodedData = Encode.Base64Encode(compactJson);
                plainText =
                    $"data={encodedData}&method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";
            }
            else
            {
                plainText =
                    $"method={method}&nonceStr={nonceStr}&requestUrl={requestUrl}&signType={signType}&timestamp={timestamp}";
            }

            var plainTextByte = Encoding.UTF8.GetBytes(plainText);
            var provider = PemKeyHelper.GetRSAProviderFromPemFile(privateKey);
            var signedBytes = provider.SignData(plainTextByte, CryptoConfig.MapNameToOID("SHA256"));
            return Convert.ToBase64String(signedBytes);
        }

        public static string GenerateCompactJson(object data)
        {
            var dataStr = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            //sorting purpose
            var obj = Newtonsoft.Json.Linq.JObject.Parse(dataStr);
            Sort(obj);
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static void Sort(Newtonsoft.Json.Linq.JObject jObj)
        {
            var props = jObj.Properties().ToList();
            foreach (var prop in props)
            {
                prop.Remove();
            }

            foreach (var prop in props.OrderBy(p => p.Name))
            {
                jObj.Add(prop);
                if (prop.Value is Newtonsoft.Json.Linq.JObject) Sort((Newtonsoft.Json.Linq.JObject) prop.Value);
            }
        }

        public static bool VerifySignature(object data, string method, string nonceStr, string publicKey, string requestUrl,
            string signType, string timestamp, string signature)
        {
            var result = false;
            try
            {
                var sb = new StringBuilder();
                if (data != null)
                {
                    var encodedData = Encode.Base64Encode(GenerateCompactJson(data));
                    sb.Append($"data={encodedData}&");
                }

                sb.Append($"method={method}&");
                sb.Append($"nonceStr={nonceStr}&");
                if (!string.IsNullOrWhiteSpace(requestUrl)) sb.Append($"requestUrl={requestUrl}&");
                sb.Append($"signType={signType}&");
                sb.Append($"timestamp={timestamp}");

                var plainText = sb.ToString();

                var plainTextByte = Encoding.UTF8.GetBytes(plainText);
                var signatureByte = Convert.FromBase64String(signature);
                var provider = PemKeyHelper.GetRSAProviderFromPemFile(publicKey);
                result = provider.VerifyData(plainTextByte, CryptoConfig.MapNameToOID("SHA256"), signatureByte);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error", ex.Message);
            }

            return result;
        }
    }
}