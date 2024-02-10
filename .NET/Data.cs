using System;
using System.Collections;
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
    public class Data : IEnumerable<KeyValuePair<string, string?>>, IEquatable<Data>
    {
        // TODO: Data should have a unique id and a creator id and timestamp
        // TODO: Data should have a unique hash based on the data. Use as Id?  Data should be immutable.
        //public string? Id { get; }
        //public string? CreatorId { get; }

        public DataFormat Format { get; private set; } = DataFormat.RAW;

        private Dictionary<string, string?>? _structured;
        public Dictionary<string, string?>? Structured // TODO: Return Readonly ?
        {
            get => _structured;
            private set
            {
                _structured = value;
                Raw = JsonSerializer.Serialize(value);
            }
        }
        
        private string? _raw;        
        public string? Raw
        {
            get => _raw;
            private set
            {
                _raw = value;                
                TryConvertRawToStructured();
            }
        }        

        public Data(string? raw = null, DataFormat dataFormat = DataFormat.RAW)
        {
            Format = dataFormat;
            Raw = raw; 
        }

        public Data(IEnumerable<KeyValuePair<string, string?>> structured) :
            this(new Dictionary<string, string?>(structured))
        { }

        public Data(Dictionary<string, string?> structured)
        {
            Format = DataFormat.STRUCTURED;
            _structured = structured;
            Raw = JsonSerializer.Serialize(structured);
        }

        public string? this[string key]
        {
            get
            {
                if (_structured != null && _structured.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null; // Return null if the key is not found
            }
            set
            {
                if (_structured == null)
                {
                    _structured = new Dictionary<string, string?>();
                }
                _structured[key] = value;
            }
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
                        _structured = JsonSerializer.Deserialize<Dictionary<string, string?>>(Raw);
                        if (_structured != null)
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

        public void Add(string key, string value)
        {
            _structured?.Add(key, value);
        }

        internal bool ContainsKey(string key)
        {
            return _structured?.ContainsKey(key) ?? false;
        }
        public IEnumerator GetEnumerator()
        {
            return _structured?.GetEnumerator() ?? default;
        }

        IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
        {
            return _structured?.GetEnumerator() ?? default;
        }

        public bool Equals(Data? other)
        {
            return other != null && Raw == other.Raw;
        }

        public static implicit operator Data?(string? raw) => new Data(raw);

        public static implicit operator Data?(Dictionary<string, string?> structured) => new Data(structured);

        public static implicit operator string?(Data? data) => data?.Raw;

        public static implicit operator Dictionary<string, string?>?(Data? data) => data?._structured;        
    }
}
