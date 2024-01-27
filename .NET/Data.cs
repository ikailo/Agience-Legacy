using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agience.Client
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
                if (dataFormat == DataFormat.STRUCTURED && raw != null)
                {
                    try
                    {
                        var structured = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
                        return new Data(structured);
                    }
                    catch (JsonException)
                    {
                        dataFormat = DataFormat.RAW;
                    }
                }
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
        // TODO: Data should have a unique id and a creator id and timestamp
        // TODO: Data should have a unique hash based on the data. Use as Id?  Data should be immutable.
        //public string? Id { get; }
        //public string? CreatorId { get; }

        public DataFormat Format { get; private set; } = DataFormat.RAW;

        private string? _raw;
        public string? Raw
        {
            get => _raw;
            private set
            {
                _raw = value;
                // Attempt to convert Raw JSON to structured data if the format is STRUCTURED
                TryConvertRawToStructured();
            }
        }

        public Dictionary<string, string>? Structured { get; private set; }

        public Data(string? raw = null, DataFormat dataFormat = DataFormat.RAW)
        {
            Format = dataFormat;
            Raw = raw; // Setting Raw will trigger TryConvertRawToStructured if needed
        }

        public Data(Dictionary<string, string> structured)
        {
            Format = DataFormat.STRUCTURED;
            Structured = structured;
            Raw = JsonSerializer.Serialize(structured);
        }

        private void TryConvertRawToStructured()
        {
            if (Format == DataFormat.RAW && !string.IsNullOrEmpty(Raw))
            {
                // Check if Raw string starts with '{' and ends with '}' indicating a JSON object
                if (Raw.TrimStart().StartsWith("{") && Raw.TrimEnd().EndsWith("}"))
                {
                    try
                    {
                        Structured = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw);
                        if (Structured != null)
                        {
                            Format = DataFormat.STRUCTURED;
                        }
                    }
                    catch (JsonException)
                    {
                        // If deserialization fails, Raw remains unchanged and Structured is null
                    }
                }
            }
        }


        public override string? ToString() => Raw;

        public static implicit operator Data?(string? raw) => new Data(raw);

        public static implicit operator Data?(Dictionary<string, string> structured) => new Data(structured);

        public static implicit operator string?(Data? data) => data?.Raw;

        public static implicit operator Dictionary<string, string>?(Data? data) => data?.Structured;
    }
}
