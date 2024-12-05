// Original from ACC Broadcasting SDK example (Assetto Corsa Competizione Dedicated Server\sdk\broadcasting)

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using KLPlugins.DynLeaderboards.Log;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.AccBroadcastingNetwork;

internal sealed class AccUdpRemoteClient : IDisposable {
    internal BroadcastingNetworkProtocol _MessageHandler { get; }
    internal bool _IsConnected { get; private set; }

    private string _ipPort { get; }
    private string _displayName { get; }
    private string _connectionPassword { get; }
    private string _commandPassword { get; }
    private int _msRealtimeUpdateInterval { get; }
    private UdpClient? _client;
    private Task? _listenerTask;

    internal DateTime _LastUpdate { get; private set; } = DateTime.Now;

    /// <summary>
    ///     To get the events delivered inside the UI thread, just create this object from the UI thread/synchronization
    ///     context.
    /// </summary>
    private AccUdpRemoteClient(
        string ip,
        int port,
        string displayName,
        string connectionPassword,
        string commandPassword,
        int msRealtimeUpdateInterval,
        int delay = 0
    ) {
        this._ipPort = $"{ip}:{port}";
        this._MessageHandler = new BroadcastingNetworkProtocol(this._ipPort, this.Send);
        this._client = new UdpClient();
        this._client.Connect(ip, port);

        this._displayName = displayName;
        this._connectionPassword = connectionPassword;
        this._commandPassword = commandPassword;
        this._msRealtimeUpdateInterval = msRealtimeUpdateInterval;
        this._IsConnected = false;
        this._MessageHandler.OnConnectionStateChanged += this.OnBroadcastConnectionStateChanged;
        this._listenerTask = this.ConnectAndRun(delay);
    }

    internal AccUdpRemoteClient(AccUdpRemoteClientConfig cfg, int delay = 0) : this(
        cfg._Ip,
        cfg._Port,
        cfg._DisplayName,
        cfg._ConnectionPassword,
        cfg._CommandPassword,
        cfg._UpdateIntervalMs,
        delay
    ) { }

    ~AccUdpRemoteClient() {
        this.Dispose();
    }

    private void Send(byte[] payload) {
        if (this._client == null) {
            Logging.LogWarn("Tried to send a message to ACC but our client has already been shut down.");
            return;
        }

        _ = this._client.Send(payload, payload.Length);
    }

    internal async Task ShutdownAsync() {
        if (this._listenerTask != null && !this._listenerTask.IsCompleted) {
            if (this._IsConnected) {
                this._MessageHandler.Disconnect();
            }

            this._client?.Close();
            this._client = null;
            this._IsConnected = false;
            await this._listenerTask;
            this._listenerTask = null;
        }
    }

    private async Task ConnectAndRun(int delay = 0) {
        // delay first request so that when we exist sessions the game's server is not actually running anymore
        // otherwise we immediately reconnect again
        if (delay > 0) {
            await Task.Delay(delay);
        }

        while (this._client != null) {
            this.RequestConnection();
            try {
                var result = await Task.WhenAny(this._client.ReceiveAsync(), Task.Delay(5000));
                if (result is not Task<UdpReceiveResult> udpResult) {
                    throw new TimeoutException();
                }

                var udpPacket = await udpResult;
                using var ms = new MemoryStream(udpPacket.Buffer);
                using var reader = new BinaryReader(ms);
                this._LastUpdate = DateTime.Now;
                this._MessageHandler.ProcessMessage(reader);
                Logging.LogInfo("Connected!");
                break;
            } catch (ObjectDisposedException) {
                // Shutdown happened
                Logging.LogInfo("Broadcast client shut down.");
                break;
            } catch (Exception ex) {
                // Other exceptions
                Logging.LogWarn("Failed to connect to broadcast client. Trying again in 5s.");
            }

            await Task.Delay(5000);
        }

        while (this._client != null) {
            try {
                var result = await Task.WhenAny(this._client.ReceiveAsync(), Task.Delay(5000));
                if (result is Task<UdpReceiveResult> udpResult) {
                    var udpPacket = await udpResult;
                    using var ms = new MemoryStream(udpPacket.Buffer);
                    using var reader = new BinaryReader(ms);
                    this._LastUpdate = DateTime.Now;
                    this._MessageHandler.ProcessMessage(reader);
                } else {
                    throw new TimeoutException();
                }
            } catch (ObjectDisposedException) {
                // Shutdown happened
                Logging.LogInfo("Broadcast client shut down.");
                break;
            } catch (Exception ex) {
                // Other exceptions
                Logging.LogInfo($"Failed to process ACC message. Err {ex}.");
            }
        }

        this._IsConnected = false;
    }

    private void RequestConnection() {
        Logging.LogInfo("Requested connection to broadcast client.");
        this._MessageHandler.RequestConnection(
            displayName: this._displayName,
            connectionPassword: this._connectionPassword,
            msRealtimeUpdateInterval: this._msRealtimeUpdateInterval,
            commandPassword: this._commandPassword
        );
    }

    private void OnBroadcastConnectionStateChanged(
        int connectionId,
        bool connectionSuccess,
        bool isReadonly,
        string error
    ) {
        if (connectionSuccess) {
            Logging.LogInfo("Connected to broadcast client.");
            this._IsConnected = true;
        } else {
            Logging.LogError($"Failed to connect to broadcast client. Err: {error}. Trying again..");
            this.RequestConnection();
        }
    }

    #region IDisposable Support

    private bool _disposedValue = false; // To detect redundant calls

    private void Dispose(bool disposing) {
        if (!this._disposedValue) {
            if (disposing) {
                try {
                    Logging.LogInfo("Disposing...");
                    this.ShutdownAsync()
                        .ContinueWith(
                            t => {
                                if (t.Exception?.InnerExceptions?.Any() == true) {
                                    Logging.LogError(
                                        $"Client shut down with {t.Exception.InnerExceptions.Count} errors"
                                    );
                                } else {
                                    Logging.LogInfo("Client shut down asynchronously");
                                }
                            }
                        )
                        .RunSynchronously();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            this._disposedValue = true;
        }
    }


    public void Dispose() {
        this.Dispose(true);
        // GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}

/// <summary>
///     Configuration of ACCUdpRemoteClient
/// </summary>
internal class AccUdpRemoteClientConfig {
    /// Class to read acc\Config\broadcasting.json
    private struct AccBroadcastConfig {
        #pragma warning disable IDE1006
        // Property names must match with the ones in json
        private int _udpListenerPort;
        public int UpdListenerPort {
            readonly get => this._udpListenerPort;
            set {
                AccBroadcastConfig.ValidatePort(value);
                this._udpListenerPort = value;
            }
        }
        public string ConnectionPassword { get; set; }
        public string CommandPassword { get; set; }

        internal static AccBroadcastConfig AccDefault() {
            return new AccBroadcastConfig { UpdListenerPort = 9000, ConnectionPassword = "asd", CommandPassword = "" };
        }

        private static void ValidatePort(int port) {
            if (port is < 1024 or > 65535) {
                throw new InvalidDataException(
                    $"The port set in '{DynLeaderboardsPlugin._Settings.AccDataLocation}\\Config\\broadcasting.json' is invalid. It must be between 1024 and 65535."
                );
            }
        }

        internal readonly void Validate() {
            AccBroadcastConfig.ValidatePort(this.UpdListenerPort);
        }
    }

    internal string _Ip { get; }
    internal int _Port => this._config.UpdListenerPort;
    internal string _DisplayName { get; }
    internal string _ConnectionPassword => this._config.ConnectionPassword;
    internal string _CommandPassword => this._config.CommandPassword;
    internal int _UpdateIntervalMs { get; }

    private readonly AccBroadcastConfig _config;

    /// <summary>
    ///     Port, connectionPassword, commandPassword are read from the ..\\Assetto Corsa
    ///     Competizione\\Config\\broadcasting.json.
    /// </summary>
    internal AccUdpRemoteClientConfig(string ip, string displayName, int updateTime) {
        try {
            var configPath = $"{DynLeaderboardsPlugin._Settings.AccDataLocation}\\Config\\broadcasting.json";
            var rawJson = File.ReadAllText(configPath, Encoding.Unicode).Replace("\"", "'");
            this._config = JsonConvert.DeserializeObject<AccBroadcastConfig>(rawJson);
            this._config.Validate();
        } catch (Exception e) {
            Logging.LogWarn(
                $"Couldn't read broadcasting.json. Using default, it may or may not work. Underlying error: {e}."
            );
            this._config = AccBroadcastConfig.AccDefault();
        }

        this._Ip = ip;
        this._DisplayName = displayName;
        this._UpdateIntervalMs = updateTime;
    }
}