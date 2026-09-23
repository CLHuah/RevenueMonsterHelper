using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevenueMonsterLibrary.Helper;

/// <summary>
///     Reads JSON without converting date-like strings or otherwise changing values, so they can be signed or mapped
///     onto models exactly as they were received.
/// </summary>
internal static class RawJson
{
    /// <summary>
    ///     Serializer that leaves date-like strings as strings and rejects trailing content.
    /// </summary>
    public static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        DateParseHandling = DateParseHandling.None,
        CheckAdditionalContent = true
    });

    /// <summary>
    ///     Parses JSON text into a token without altering any values.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed token; a JSON null token when the text is the literal null.</returns>
    /// <exception cref="JsonReaderException">Thrown when the text is not valid JSON.</exception>
    public static JToken Parse(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        return Serializer.Deserialize<JToken>(reader) ?? JValue.CreateNull();
    }
}
