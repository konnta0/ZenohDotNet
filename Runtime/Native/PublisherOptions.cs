using System;

namespace ZenohDotNet.Native
{
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
    /// Options for configuring a publisher.
    /// </summary>
    public class PublisherOptions
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
}
