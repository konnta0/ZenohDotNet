using System;
using System.Collections.Generic;
using System.Text;

namespace ZenohDotNet.Unity
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
    /// Unity-compatible version using manual JSON serialization.
    /// </summary>
    public sealed class SessionConfig
    {
        private SessionMode? _mode;
        private readonly List<string> _connectEndpoints = new List<string>();
        private readonly List<string> _listenEndpoints = new List<string>();
        private bool? _multicastScouting;
        private string _multicastInterface;
        private string _multicastAddress;
        private int? _scoutingTimeout;
        private bool? _timestampingEnabled;
        private string _authUser;
        private string _authPassword;
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
        public SessionConfig WithMode(SessionMode mode)
        {
            _mode = mode;
            return this;
        }

        /// <summary>
        /// Adds an endpoint to connect to.
        /// </summary>
        public SessionConfig WithConnect(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException(nameof(endpoint));
            _connectEndpoints.Add(endpoint);
            return this;
        }

        /// <summary>
        /// Adds multiple endpoints to connect to.
        /// </summary>
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
        public SessionConfig WithListen(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException(nameof(endpoint));
            _listenEndpoints.Add(endpoint);
            return this;
        }

        /// <summary>
        /// Adds multiple endpoints to listen on.
        /// </summary>
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
        public SessionConfig WithMulticastScouting(bool enabled)
        {
            _multicastScouting = enabled;
            return this;
        }

        /// <summary>
        /// Sets the network interface for multicast scouting.
        /// </summary>
        public SessionConfig WithMulticastInterface(string interfaceName)
        {
            _multicastInterface = interfaceName;
            return this;
        }

        /// <summary>
        /// Sets the multicast address for scouting.
        /// </summary>
        public SessionConfig WithMulticastAddress(string address)
        {
            _multicastAddress = address;
            return this;
        }

        /// <summary>
        /// Sets the scouting timeout in milliseconds.
        /// </summary>
        public SessionConfig WithScoutingTimeout(int timeoutMs)
        {
            if (timeoutMs < 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _scoutingTimeout = timeoutMs;
            return this;
        }

        /// <summary>
        /// Enables or disables automatic timestamping of publications.
        /// </summary>
        public SessionConfig WithTimestamping(bool enabled)
        {
            _timestampingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the authentication credentials.
        /// </summary>
        public SessionConfig WithAuth(string user, string password)
        {
            _authUser = user;
            _authPassword = password;
            return this;
        }

        /// <summary>
        /// Enables or disables shared memory transport.
        /// </summary>
        public SessionConfig WithSharedMemory(bool enabled)
        {
            _sharedMemoryEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Converts this configuration to a JSON string for the native layer.
        /// Uses manual JSON building for Unity compatibility.
        /// </summary>
        internal string ToJson()
        {
            var parts = new List<string>();

            if (_mode.HasValue)
            {
                parts.Add($"\"mode\":\"{_mode.Value.ToString().ToLowerInvariant()}\"");
            }

            if (_connectEndpoints.Count > 0)
            {
                var endpoints = string.Join(",", _connectEndpoints.ConvertAll(e => $"\"{EscapeJson(e)}\""));
                parts.Add($"\"connect\":{{\"endpoints\":[{endpoints}]}}");
            }

            if (_listenEndpoints.Count > 0)
            {
                var endpoints = string.Join(",", _listenEndpoints.ConvertAll(e => $"\"{EscapeJson(e)}\""));
                parts.Add($"\"listen\":{{\"endpoints\":[{endpoints}]}}");
            }

            var scoutingParts = new List<string>();
            var multicastParts = new List<string>();

            if (_multicastScouting.HasValue)
            {
                multicastParts.Add($"\"enabled\":{(_multicastScouting.Value ? "true" : "false")}");
            }

            if (!string.IsNullOrEmpty(_multicastInterface))
            {
                multicastParts.Add($"\"interface\":\"{EscapeJson(_multicastInterface)}\"");
            }

            if (!string.IsNullOrEmpty(_multicastAddress))
            {
                multicastParts.Add($"\"address\":\"{EscapeJson(_multicastAddress)}\"");
            }

            if (multicastParts.Count > 0)
            {
                scoutingParts.Add($"\"multicast\":{{{string.Join(",", multicastParts)}}}");
            }

            if (_scoutingTimeout.HasValue)
            {
                scoutingParts.Add($"\"timeout\":{_scoutingTimeout.Value}");
            }

            if (scoutingParts.Count > 0)
            {
                parts.Add($"\"scouting\":{{{string.Join(",", scoutingParts)}}}");
            }

            if (_timestampingEnabled.HasValue)
            {
                parts.Add($"\"timestamping\":{{\"enabled\":{(_timestampingEnabled.Value ? "true" : "false")}}}");
            }

            var transportParts = new List<string>();

            if (!string.IsNullOrEmpty(_authUser) || !string.IsNullOrEmpty(_authPassword))
            {
                var user = EscapeJson(_authUser ?? "");
                var password = EscapeJson(_authPassword ?? "");
                transportParts.Add($"\"auth\":{{\"usrpwd\":{{\"user\":\"{user}\",\"password\":\"{password}\"}}}}");
            }

            if (_sharedMemoryEnabled.HasValue)
            {
                transportParts.Add($"\"shared_memory\":{{\"enabled\":{(_sharedMemoryEnabled.Value ? "true" : "false")}}}");
            }

            if (transportParts.Count > 0)
            {
                parts.Add($"\"transport\":{{{string.Join(",", transportParts)}}}");
            }

            if (parts.Count == 0)
            {
                return "{}";
            }

            return "{" + string.Join(",", parts) + "}";
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

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
        public static SessionConfig Client(string endpoint) =>
            new SessionConfig()
                .WithMode(SessionMode.Client)
                .WithConnect(endpoint);

        /// <summary>
        /// Creates a router mode configuration that listens on the specified endpoint.
        /// </summary>
        public static SessionConfig Router(string endpoint) =>
            new SessionConfig()
                .WithMode(SessionMode.Router)
                .WithListen(endpoint);
    }
}
