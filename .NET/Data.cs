using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Technologai
{
    public class DataJsonConverter : JsonConverter<Data>
    {
        public override Data Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            DataFormat? dataFormat = null;
            string? raw = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "Format")
                    {
                        dataFormat = (DataFormat)reader.GetInt32();
                    }
                    else if (propertyName == "Raw")
                    {
                        raw = reader.GetString();
                    }
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
            }

            if (dataFormat.HasValue)
            {
                return new Data(raw, dataFormat.Value);
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Data value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Format", (int)value.Format);
            writer.WriteString("Raw", value.Raw);
            writer.WriteEndObject();
        }
    }

    public enum DataFormat
    {
        RAW = 0,
        STRUCTURED = 1
    }

    [JsonConverter(typeof(DataJsonConverter))]
    public class Data
    {
        public DataFormat Format { get; } = DataFormat.RAW;

        // Raw data is just a string
        public string? Raw { get; }

        // Structured data is key/value pairs        
        public Dictionary<string, string>? Structured { get; }

        public Data(string? raw = null, DataFormat dataFormat = DataFormat.RAW)
        {
            Raw = raw;
            Format = dataFormat;

            if (dataFormat == DataFormat.STRUCTURED)
            {
                try
                {
                    Structured = raw == null ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
                }
                catch (JsonException)
                {
                    // Fallback to Raw
                    Format = DataFormat.RAW;                    
                }
            }
        }

        public Data(Dictionary<string, string> structured)
        {
            Format = DataFormat.STRUCTURED;
            Structured = structured;
            Raw = JsonSerializer.Serialize(structured);
        }
       
        public override string? ToString() => Raw;

        public static Data? Create(string raw)
        {
            return new Data(raw);
        }

        public static Data? Create(Exception exception)
        {
            return Create("error", exception.Message);
        }

        public static Data? Create(string key, string value)
        {
            return new Data(new Dictionary<string, string>()
            {
                { key, value }
            });
        }

        public static Data? Create(string key, IEnumerable<IConvertible> value)
        {
            return Create(key, JsonSerializer.Serialize(value));            
        }

        public static implicit operator Data?(string? raw) => new Data(raw);

        public static implicit operator Data?(Dictionary<string, string> structured) => new Data(structured);

        public static implicit operator string?(Data? data) => data?.Raw;

        public static implicit operator Dictionary<string, string>?(Data? data) => data?.Structured;
    }
}
