using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Agience.Client
{
    public class Id : IComparable<Id>
    {
        // hash compute of an id allows to verify which agent created this contextId. If that's ever needed.

        private readonly string _id;
        private readonly byte[] _unixTimestampBytes = new byte[8];
        private readonly byte[] _hashComputeBytes = new byte[8];

        internal Id(long unixTimestamp, byte[] idHash)
        {
            _unixTimestampBytes = BitConverter.GetBytes(unixTimestamp);
            _hashComputeBytes = MD5.HashData(idHash.Concat(_unixTimestampBytes).ToArray()).Take(8).ToArray(); // just half of the hash to fit into 8 bytes. Is this ok?
            _id = Base64UrlEncoder.Encode(_unixTimestampBytes.Concat(_hashComputeBytes).ToArray());
        }

        [JsonConstructor]
        public Id(string informationId)
        {
            _id = informationId;

            var informationBytes = Base64UrlEncoder.DecodeBytes(_id);

            Array.Copy(informationBytes, _unixTimestampBytes, 8);
            Array.Copy(informationBytes, 8, _hashComputeBytes, 0, 8);
        }

        public static Id Create(string creatorIdBase64)
        {

            return new Id(GetTimestampTicks(), GetBase64Bytes(creatorIdBase64, 8));
        }

        public static long GetTimestampTicks()
        {
            return (DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).Ticks;
        }

        public static byte[] GetBase64Bytes(string base64, int count)
        {
            return Base64UrlEncoder.DecodeBytes(base64).Take(count).ToArray();
        }

        public int CompareTo(Id? other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            var result = BitConverter.ToInt64(_unixTimestampBytes).CompareTo(
                BitConverter.ToInt64(other._unixTimestampBytes)
            );

            if (result == 0)
            {
                return BitConverter.ToInt64(_hashComputeBytes).CompareTo(
                    BitConverter.ToInt64(other._hashComputeBytes)
                );
            }
            return result;
        }

        public static implicit operator Id(string value) => new Id(value);

        public static implicit operator string(Id value) => value.ToString();

        public override string ToString()
        {
            return _id;
        }
    }
}