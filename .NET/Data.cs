using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agience.Client
{
    public class DataJsonConverter : JsonConverter<Data>
    {
        public override Data Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
            var data = new Data { Raw = jsonObject.GetRawText() };
            return data;
        }

        public override void Write(Utf8JsonWriter writer, Data data, JsonSerializerOptions options)
        {   
            writer.WriteStringValue(data.Raw);            
        }
    }

    [JsonConverter(typeof(DataJsonConverter))]
    [Serializable]
    public class Data : ISerializable, IEnumerable<KeyValuePair<string, string?>>
    {
        private Dictionary<string, string?>? _structured;
        private string? _raw;

        [JsonPropertyName("Raw")]
        public string? Raw
        {
            get
            {
                return _structured == null ? _raw : JsonSerializer.Serialize(_structured);
            }
            set
            {
                _raw = value;

                if (!string.IsNullOrEmpty(_raw) && _raw.StartsWith("{") && _raw.EndsWith("}"))
                {
                    try
                    {
                        _structured = JsonSerializer.Deserialize<Dictionary<string, string?>>(_raw);
                    }
                    catch
                    {
                        // If deserialization fails, Structured remains null
                    }
                }
            }
        }

        public Data() { }

        protected Data(SerializationInfo info, StreamingContext context)
        {
            Raw = info.GetString("Raw");
        }

        public void Add(string key, string? value)
        {
            if (_structured == null)
            {
                _structured = new();
            }
            _structured.Add(key, value);
        }

        public string? this[string key]
        {
            get => _structured?.TryGetValue(key, out var value) ?? false ? value : null;
            set => Add(key, value);
        }

        public static implicit operator string?(Data? data)
        {
            return data?.Raw;
        }

        public static implicit operator Data(string? raw)
        {
            return new Data { Raw = raw };
        }

        public override string ToString()
        {
            return Raw ?? string.Empty;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Raw", Raw);
        }

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return _structured?.GetEnumerator() ?? ((IEnumerable<KeyValuePair<string, string?>>)Array.Empty<KeyValuePair<string, string?>>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
