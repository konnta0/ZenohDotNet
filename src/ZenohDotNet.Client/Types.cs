using System;

namespace ZenohDotNet.Client;

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
public enum Encoding
{
    /// <summary>
    /// Empty/unspecified encoding.
    /// </summary>
    Empty = 0,

    /// <summary>
    /// application/octet-stream
    /// </summary>
    ApplicationOctetStream = 1,

    /// <summary>
    /// text/plain
    /// </summary>
    TextPlain = 2,

    /// <summary>
    /// application/json
    /// </summary>
    ApplicationJson = 3,

    /// <summary>
    /// text/json
    /// </summary>
    TextJson = 4,

    /// <summary>
    /// application/cbor
    /// </summary>
    ApplicationCbor = 5,

    /// <summary>
    /// application/yaml
    /// </summary>
    ApplicationYaml = 6,

    /// <summary>
    /// text/yaml
    /// </summary>
    TextYaml = 7,

    /// <summary>
    /// text/xml
    /// </summary>
    TextXml = 8,

    /// <summary>
    /// application/xml
    /// </summary>
    ApplicationXml = 9,

    /// <summary>
    /// text/csv
    /// </summary>
    TextCsv = 10,

    /// <summary>
    /// application/protobuf
    /// </summary>
    ApplicationProtobuf = 11,

    /// <summary>
    /// text/html
    /// </summary>
    TextHtml = 12
}

/// <summary>
/// Priority of messages.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Real-time priority (highest).
    /// </summary>
    RealTime = 1,

    /// <summary>
    /// Interactive high priority.
    /// </summary>
    InteractiveHigh = 2,

    /// <summary>
    /// Interactive low priority.
    /// </summary>
    InteractiveLow = 3,

    /// <summary>
    /// Data high priority.
    /// </summary>
    DataHigh = 4,

    /// <summary>
    /// Data priority (default).
    /// </summary>
    Data = 5,

    /// <summary>
    /// Data low priority.
    /// </summary>
    DataLow = 6,

    /// <summary>
    /// Background priority (lowest).
    /// </summary>
    Background = 7
}

/// <summary>
/// Congestion control strategy for publishers.
/// </summary>
public enum CongestionControl
{
    /// <summary>
    /// Block if the buffer is full.
    /// </summary>
    Block = 0,

    /// <summary>
    /// Drop the message if the buffer is full.
    /// </summary>
    Drop = 1
}

/// <summary>
/// Represents a Zenoh timestamp.
/// </summary>
public readonly struct Timestamp : IEquatable<Timestamp>
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

    /// <summary>
    /// Initializes a new instance of the Timestamp struct.
    /// </summary>
    /// <param name="timeNtp64">The NTP64 time value.</param>
    /// <param name="id">The unique ID of the timestamp source.</param>
    public Timestamp(ulong timeNtp64, byte[] id)
    {
        TimeNtp64 = timeNtp64;
        Id = id ?? new byte[16];
    }

    /// <inheritdoc/>
    public bool Equals(Timestamp other)
    {
        if (TimeNtp64 != other.TimeNtp64) return false;
        if (Id is null && other.Id is null) return true;
        if (Id is null || other.Id is null) return false;
        return Id.AsSpan().SequenceEqual(other.Id);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Timestamp other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(TimeNtp64, Id?.Length ?? 0);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Timestamp left, Timestamp right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Timestamp left, Timestamp right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Timestamp {{ Time = {AsDateTime:O} }}";
    }
}

/// <summary>
/// Options for configuring a publisher.
/// </summary>
public sealed class PublisherOptions
{
    /// <summary>
    /// Gets or sets the congestion control strategy.
    /// </summary>
    public CongestionControl CongestionControl { get; set; } = CongestionControl.Drop;

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Data;

    /// <summary>
    /// Gets or sets whether express mode is enabled (lower latency, less reliable).
    /// </summary>
    public bool IsExpress { get; set; } = false;

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public PublisherOptions()
    {
    }
}
