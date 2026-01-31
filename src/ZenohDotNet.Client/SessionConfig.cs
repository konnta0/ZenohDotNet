using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ZenohDotNet.Client;

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
public sealed class SessionConfig
{
    private SessionMode? _mode;
    private readonly List<string> _connectEndpoints = new();
    private readonly List<string> _listenEndpoints = new();
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
        ArgumentNullException.ThrowIfNull(endpoint);
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
        ArgumentNullException.ThrowIfNull(endpoint);
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
        ArgumentOutOfRangeException.ThrowIfNegative(timeoutMs);
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
    /// </summary>
    /// <returns>The JSON configuration string.</returns>
    internal string ToJson()
    {
        var config = new Dictionary<string, object>();

        if (_mode.HasValue)
        {
            config["mode"] = _mode.Value.ToString().ToLowerInvariant();
        }

        if (_connectEndpoints.Count > 0)
        {
            config["connect"] = new Dictionary<string, object>
            {
                ["endpoints"] = _connectEndpoints
            };
        }

        if (_listenEndpoints.Count > 0)
        {
            config["listen"] = new Dictionary<string, object>
            {
                ["endpoints"] = _listenEndpoints
            };
        }

        var scouting = new Dictionary<string, object>();
        var multicast = new Dictionary<string, object>();

        if (_multicastScouting.HasValue)
        {
            multicast["enabled"] = _multicastScouting.Value;
        }

        if (!string.IsNullOrEmpty(_multicastInterface))
        {
            multicast["interface"] = _multicastInterface;
        }

        if (!string.IsNullOrEmpty(_multicastAddress))
        {
            multicast["address"] = _multicastAddress;
        }

        if (multicast.Count > 0)
        {
            scouting["multicast"] = multicast;
        }

        if (_scoutingTimeout.HasValue)
        {
            scouting["timeout"] = _scoutingTimeout.Value;
        }

        if (scouting.Count > 0)
        {
            config["scouting"] = scouting;
        }

        if (_timestampingEnabled.HasValue)
        {
            config["timestamping"] = new Dictionary<string, object>
            {
                ["enabled"] = _timestampingEnabled.Value
            };
        }

        var transport = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(_authUser) || !string.IsNullOrEmpty(_authPassword))
        {
            transport["auth"] = new Dictionary<string, object>
            {
                ["usrpwd"] = new Dictionary<string, object>
                {
                    ["user"] = _authUser ?? "",
                    ["password"] = _authPassword ?? ""
                }
            };
        }

        if (_sharedMemoryEnabled.HasValue)
        {
            transport["shared_memory"] = new Dictionary<string, object>
            {
                ["enabled"] = _sharedMemoryEnabled.Value
            };
        }

        if (transport.Count > 0)
        {
            config["transport"] = transport;
        }

        if (config.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(config);
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
