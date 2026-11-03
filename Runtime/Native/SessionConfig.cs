using System;
using System.Collections.Generic;
using System.Text;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Zenoh session mode.
    /// </summary>
    public enum SessionMode
    {
        /// <summary>
        /// Peer mode - connects to other peers and routers.
        /// </summary>
        Peer,

        /// <summary>
        /// Client mode - connects only to routers.
        /// </summary>
        Client,

        /// <summary>
        /// Router mode - acts as a router for other peers and clients.
        /// </summary>
        Router
    }

    /// <summary>
    /// Type-safe configuration builder for Zenoh sessions.
    /// </summary>
    public class SessionConfig
    {
        private SessionMode? _mode;
        private readonly List<string> _connectEndpoints = new List<string>();
        private readonly List<string> _listenEndpoints = new List<string>();
        private bool? _multicastScouting;
        private string? _multicastInterface;
        private string? _multicastAddress;
        private int? _scoutingTimeout;
        private bool? _timestampingEnabled;
        private string? _authUser;
        private string? _authPassword;
        private bool? _sharedMemoryEnabled;

        /// <summary>
        /// Creates a new default configuration.
        /// </summary>
        public SessionConfig()
        {
        }

        /// <summary>
        /// Sets the session mode.
        /// </summary>
        /// <param name="mode">The session mode (Peer, Client, or Router).</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithMode(SessionMode mode)
        {
            _mode = mode;
            return this;
        }

        /// <summary>
        /// Adds an endpoint to connect to.
        /// </summary>
        /// <param name="endpoint">The endpoint URI (e.g., "tcp/192.168.1.1:7447").</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithConnect(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));
            _connectEndpoints.Add(endpoint);
            return this;
        }

        /// <summary>
        /// Adds multiple endpoints to connect to.
        /// </summary>
        /// <param name="endpoints">The endpoint URIs.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithConnect(params string[] endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                WithConnect(endpoint);
            }
            return this;
        }

        /// <summary>
        /// Adds an endpoint to listen on.
        /// </summary>
        /// <param name="endpoint">The endpoint URI (e.g., "tcp/0.0.0.0:7447").</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithListen(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));
            _listenEndpoints.Add(endpoint);
            return this;
        }

        /// <summary>
        /// Adds multiple endpoints to listen on.
        /// </summary>
        /// <param name="endpoints">The endpoint URIs.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithListen(params string[] endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                WithListen(endpoint);
            }
            return this;
        }

        /// <summary>
        /// Enables or disables multicast scouting for peer discovery.
        /// </summary>
        /// <param name="enabled">True to enable multicast scouting, false to disable.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithMulticastScouting(bool enabled)
        {
            _multicastScouting = enabled;
            return this;
        }

        /// <summary>
        /// Sets the network interface for multicast scouting.
        /// </summary>
        /// <param name="interfaceName">The network interface name (e.g., "eth0").</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithMulticastInterface(string interfaceName)
        {
            _multicastInterface = interfaceName;
            return this;
        }

        /// <summary>
        /// Sets the multicast address for scouting.
        /// </summary>
        /// <param name="address">The multicast IPv4 address.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithMulticastAddress(string address)
        {
            _multicastAddress = address;
            return this;
        }

        /// <summary>
        /// Sets the scouting timeout in milliseconds.
        /// </summary>
        /// <param name="timeoutMs">The timeout in milliseconds.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithScoutingTimeout(int timeoutMs)
        {
            if (timeoutMs < 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be non-negative");
            _scoutingTimeout = timeoutMs;
            return this;
        }

        /// <summary>
        /// Enables or disables automatic timestamping of publications.
        /// </summary>
        /// <param name="enabled">True to enable timestamping, false to disable.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithTimestamping(bool enabled)
        {
            _timestampingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the authentication credentials.
        /// </summary>
        /// <param name="user">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithAuth(string user, string password)
        {
            _authUser = user;
            _authPassword = password;
            return this;
        }

        /// <summary>
        /// Enables or disables shared memory transport.
        /// </summary>
        /// <param name="enabled">True to enable shared memory, false to disable.</param>
        /// <returns>This configuration instance for chaining.</returns>
        public SessionConfig WithSharedMemory(bool enabled)
        {
            _sharedMemoryEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Converts this configuration to a JSON string for the native layer.
        /// Uses StringBuilder to minimize allocations.
        /// </summary>
        /// <returns>The JSON configuration string.</returns>
        public string ToJson()
        {
            // Pre-calculate approximate capacity to minimize reallocations
            int estimatedCapacity = 64;
            if (_connectEndpoints.Count > 0) estimatedCapacity += 50 * _connectEndpoints.Count;
            if (_listenEndpoints.Count > 0) estimatedCapacity += 50 * _listenEndpoints.Count;
            
            var sb = new StringBuilder(estimatedCapacity);
            sb.Append('{');
            bool needsComma = false;

            if (_mode.HasValue)
            {
                sb.Append("\"mode\":\"");
                sb.Append(_mode.Value.ToString().ToLowerInvariant());
                sb.Append('"');
                needsComma = true;
            }

            if (_connectEndpoints.Count > 0)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"connect\":{\"endpoints\":[");
                for (int i = 0; i < _connectEndpoints.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"');
                    AppendEscapedJson(sb, _connectEndpoints[i]);
                    sb.Append('"');
                }
                sb.Append("]}");
                needsComma = true;
            }

            if (_listenEndpoints.Count > 0)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"listen\":{\"endpoints\":[");
                for (int i = 0; i < _listenEndpoints.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"');
                    AppendEscapedJson(sb, _listenEndpoints[i]);
                    sb.Append('"');
                }
                sb.Append("]}");
                needsComma = true;
            }

            // Build scouting section
            bool hasScoutingContent = _multicastScouting.HasValue || 
                                       !string.IsNullOrEmpty(_multicastInterface) ||
                                       !string.IsNullOrEmpty(_multicastAddress) ||
                                       _scoutingTimeout.HasValue;

            if (hasScoutingContent)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"scouting\":{");
                bool scoutingNeedsComma = false;

                bool hasMulticast = _multicastScouting.HasValue || 
                                    !string.IsNullOrEmpty(_multicastInterface) ||
                                    !string.IsNullOrEmpty(_multicastAddress);

                if (hasMulticast)
                {
                    sb.Append("\"multicast\":{");
                    bool multicastNeedsComma = false;

                    if (_multicastScouting.HasValue)
                    {
                        sb.Append("\"enabled\":");
                        sb.Append(_multicastScouting.Value ? "true" : "false");
                        multicastNeedsComma = true;
                    }

                    if (!string.IsNullOrEmpty(_multicastInterface))
                    {
                        if (multicastNeedsComma) sb.Append(',');
                        sb.Append("\"interface\":\"");
                        AppendEscapedJson(sb, _multicastInterface);
                        sb.Append('"');
                        multicastNeedsComma = true;
                    }

                    if (!string.IsNullOrEmpty(_multicastAddress))
                    {
                        if (multicastNeedsComma) sb.Append(',');
                        sb.Append("\"address\":\"");
                        AppendEscapedJson(sb, _multicastAddress);
                        sb.Append('"');
                    }

                    sb.Append('}');
                    scoutingNeedsComma = true;
                }

                if (_scoutingTimeout.HasValue)
                {
                    if (scoutingNeedsComma) sb.Append(',');
                    sb.Append("\"timeout\":");
                    sb.Append(_scoutingTimeout.Value);
                }

                sb.Append('}');
                needsComma = true;
            }

            if (_timestampingEnabled.HasValue)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"timestamping\":{\"enabled\":");
                sb.Append(_timestampingEnabled.Value ? "true" : "false");
                sb.Append('}');
                needsComma = true;
            }

            // Build transport section
            bool hasTransport = !string.IsNullOrEmpty(_authUser) || 
                               !string.IsNullOrEmpty(_authPassword) ||
                               _sharedMemoryEnabled.HasValue;

            if (hasTransport)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"transport\":{");
                bool transportNeedsComma = false;

                if (!string.IsNullOrEmpty(_authUser) || !string.IsNullOrEmpty(_authPassword))
                {
                    sb.Append("\"auth\":{\"usrpwd\":{\"user\":\"");
                    AppendEscapedJson(sb, _authUser ?? "");
                    sb.Append("\",\"password\":\"");
                    AppendEscapedJson(sb, _authPassword ?? "");
                    sb.Append("\"}}");
                    transportNeedsComma = true;
                }

                if (_sharedMemoryEnabled.HasValue)
                {
                    if (transportNeedsComma) sb.Append(',');
                    sb.Append("\"shared_memory\":{\"enabled\":");
                    sb.Append(_sharedMemoryEnabled.Value ? "true" : "false");
                    sb.Append('}');
                }

                sb.Append('}');
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendEscapedJson(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Creates a default peer mode configuration.
        /// </summary>
        public static SessionConfig Peer() => new SessionConfig().WithMode(SessionMode.Peer);

        /// <summary>
        /// Creates a client mode configuration that connects to the specified router.
        /// </summary>
        /// <param name="endpoint">The router endpoint to connect to.</param>
        public static SessionConfig Client(string endpoint) =>
            new SessionConfig()
                .WithMode(SessionMode.Client)
                .WithConnect(endpoint);

        /// <summary>
        /// Creates a router mode configuration that listens on the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to listen on.</param>
        public static SessionConfig Router(string endpoint) =>
            new SessionConfig()
                .WithMode(SessionMode.Router)
                .WithListen(endpoint);
    }
}
