using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JSON for <see cref="Col"/>. Newtonsoft would otherwise fill a channel that is absent from
/// the file with <c>default(float)</c>, so a hand-written <c>{"R":1,"G":0,"B":0}</c> would load
/// as fully transparent rather than the opaque red it reads as. Alpha defaults to 1 here;
/// lower-case channel names are accepted too, since that is how the value prints.
/// </summary>
public sealed class ColJsonConverter : JsonConverter<Col>
{
    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, Col value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("R");
        writer.WriteValue(value.R);
        writer.WritePropertyName("G");
        writer.WriteValue(value.G);
        writer.WritePropertyName("B");
        writer.WriteValue(value.B);
        writer.WritePropertyName("A");
        writer.WriteValue(value.A);
        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override Col ReadJson(JsonReader reader, Type objectType, Col existingValue,
                                 bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return default;

        var o = JObject.Load(reader);

        float Channel(string name, float fallback)
        {
            var token = o[name] ?? o[name.ToLowerInvariant()];
            return token == null || token.Type == JTokenType.Null ? fallback : token.Value<float>();
        }

        return new Col(Channel("R", 0f), Channel("G", 0f), Channel("B", 0f), Channel("A", 1f));
    }
}
