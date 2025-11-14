using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Helpers
{
    public class ExceptionJsonConverter : JsonConverter<Exception>
    {
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing Exception is not supported");
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Message", value.Message);
            writer.WriteString("Type", value.GetType().Name);
            if (value.InnerException != null)
            {
                writer.WritePropertyName("InnerException");
                Write(writer, value.InnerException, options);
            }
            writer.WriteEndObject();
        }
    }
}

