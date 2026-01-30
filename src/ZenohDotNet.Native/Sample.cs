using System;
using System.Text;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Indicates the kind of a sample (Put or Delete).
    /// </summary>
    public enum SampleKind
    {
        /// <summary>
        /// A put operation.
        /// </summary>
        Put = 0,

        /// <summary>
        /// A delete operation.
        /// </summary>
        Delete = 1
    }

    /// <summary>
    /// Encoding type for payloads.
    /// </summary>
    public enum PayloadEncoding
    {
        Empty = 0,
        ApplicationOctetStream = 1,
        TextPlain = 2,
        ApplicationJson = 3,
        TextJson = 4,
        ApplicationCbor = 5,
        ApplicationYaml = 6,
        TextYaml = 7,
        TextXml = 8,
        ApplicationXml = 9,
        TextCsv = 10,
        ApplicationProtobuf = 11,
        TextHtml = 12,
    }

    /// <summary>
    /// Represents a Zenoh timestamp.
    /// </summary>
    public readonly struct Timestamp
    {
        /// <summary>
        /// NTP64 time (seconds since epoch in upper 32 bits, fraction in lower 32 bits).
        /// </summary>
        public ulong TimeNtp64 { get; }

        /// <summary>
        /// Unique ID of the timestamp source.
        /// </summary>
        public byte[] Id { get; }

        /// <summary>
        /// Gets the timestamp as a DateTime (approximate, UTC).
        /// </summary>
        public DateTime AsDateTime
        {
            get
            {
                // Upper 32 bits are seconds since 1900-01-01
                var seconds = (long)(TimeNtp64 >> 32);
                // NTP epoch is 1900-01-01, Unix epoch is 1970-01-01
                var unixSeconds = seconds - 2208988800L;
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }
        }

        public Timestamp(ulong timeNtp64, byte[] id)
        {
            TimeNtp64 = timeNtp64;
            Id = id ?? new byte[16];
        }
    }

    /// <summary>
    /// Represents a sample received from a Zenoh subscriber.
    /// </summary>
    public class Sample
    {
        /// <summary>
        /// Gets the key expression of this sample.
        /// </summary>
        public string KeyExpression { get; }

        /// <summary>
        /// Gets the raw payload data.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Gets the kind of this sample (Put or Delete).
        /// </summary>
        public SampleKind Kind { get; }

        /// <summary>
        /// Gets the encoding of this sample.
        /// </summary>
        public PayloadEncoding Encoding { get; }

        /// <summary>
        /// Gets whether this sample has a valid timestamp.
        /// </summary>
        public bool HasTimestamp { get; }

        /// <summary>
        /// Gets the timestamp of this sample (if available).
        /// </summary>
        public Timestamp? Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the Sample class.
        /// </summary>
        public Sample(string keyExpression, byte[] payload, SampleKind kind = SampleKind.Put)
            : this(keyExpression, payload, kind, PayloadEncoding.Empty, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Sample class with encoding and timestamp.
        /// </summary>
        public Sample(string keyExpression, byte[] payload, SampleKind kind, PayloadEncoding encoding, Timestamp? timestamp)
        {
            KeyExpression = keyExpression ?? throw new ArgumentNullException(nameof(keyExpression));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Kind = kind;
            Encoding = encoding;
            HasTimestamp = timestamp.HasValue;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the payload as a UTF-8 encoded string.
        /// </summary>
        public string GetPayloadAsString()
        {
            return System.Text.Encoding.UTF8.GetString(Payload);
        }

        /// <summary>
        /// Returns a string representation of this sample.
        /// </summary>
        public override string ToString()
        {
            return $"Sample[KeyExpr={KeyExpression}, PayloadLen={Payload.Length}, Kind={Kind}, Encoding={Encoding}]";
        }
    }
}
