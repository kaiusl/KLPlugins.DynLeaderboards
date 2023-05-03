// Original from ACC Broadcasting SDK example (Assetto Corsa Competizione Dedicated Server\sdk\broadcasting)

using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.DynLeaderboards.ksBroadcastingNetwork {

    internal class ACCUdpRemoteClient : IDisposable {
        internal BroadcastingNetworkProtocol MessageHandler { get; }
        internal bool IsConnected { get; private set; }

        private string _ipPort { get; }
        private string _displayName { get; }
        private string _connectionPassword { get; }
        private string _commandPassword { get; }
        private int _msRealtimeUpdateInterval { get; }
        private UdpClient? _client;
        private Task _listenerTask;

        /// <summary>
        /// To get the events delivered inside the UI thread, just create this object from the UI thread/synchronization context.
        /// </summary>
        internal ACCUdpRemoteClient(string ip, int port, string displayName, string connectionPassword, string commandPassword, int msRealtimeUpdateInterval) {
            _ipPort = $"{ip}:{port}";
            MessageHandler = new BroadcastingNetworkProtocol(_ipPort, Send);
            _client = new UdpClient();
            _client.Connect(ip, port);

            _displayName = displayName;
            _connectionPassword = connectionPassword;
            _commandPassword = commandPassword;
            _msRealtimeUpdateInterval = msRealtimeUpdateInterval;
            IsConnected = false;
            MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;

            DynLeaderboardsPlugin.LogInfo("Requested broadcast connection");
            _listenerTask = ConnectAndRun();
        }

        internal ACCUdpRemoteClient(ACCUdpRemoteClientConfig cfg) : this(cfg.Ip, cfg.Port, cfg.DisplayName, cfg.ConnectionPassword, cfg.CommandPassword, cfg.UpdateIntervalMs) {
        }

        private void Send(byte[] payload) {
            if (_client == null) {
                DynLeaderboardsPlugin.LogWarn($"Tried to send a message to ACC but our client has already been shut down.");
                return;
            }
            var sent = _client.Send(payload, payload.Length);
        }

        internal void Shutdown() {
            ShutdownAsync().ContinueWith(t => {
                if (t.Exception?.InnerExceptions?.Any() == true) {
                    DynLeaderboardsPlugin.LogError($"Client shut down with {t.Exception.InnerExceptions.Count} errors");
                } else {
                    DynLeaderboardsPlugin.LogInfo("Client shut down asynchronously");
                }
            });
        }

        internal async Task ShutdownAsync() {
            if (_listenerTask != null && !_listenerTask.IsCompleted) {
                MessageHandler.Disconnect();
                _client?.Close();
                _client = null;
                IsConnected = false;
                await _listenerTask;
            }
        }

        private async Task ConnectAndRun() {
            RequestConnection();
            while (_client != null) {
                try {
                    var udpPacket = await _client.ReceiveAsync();
                    using (var ms = new System.IO.MemoryStream(udpPacket.Buffer))
                    using (var reader = new System.IO.BinaryReader(ms)) {
                        MessageHandler.ProcessMessage(reader);
                    }
                } catch (ObjectDisposedException) {
                    // Shutdown happened
                    DynLeaderboardsPlugin.LogInfo("Broadcast client shut down.");
                    break;
                } catch (SocketException ex) {
                    // Other exceptions
                    DynLeaderboardsPlugin.LogInfo($"Failed to receive ACC message. Err {ex}.");
                } catch (Exception ex) {
                    // Other exceptions
                    DynLeaderboardsPlugin.LogInfo($"Failed to process ACC message. Err {ex}.");
                }
            }
            IsConnected = false;
        }

        private void RequestConnection() {
            MessageHandler.RequestConnection(
                    displayName: _displayName,
                    connectionPassword: _connectionPassword,
                    msRealtimeUpdateInterval: _msRealtimeUpdateInterval,
                    commandPassword: _commandPassword
                );
        }

        private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
            if (connectionSuccess) {
                DynLeaderboardsPlugin.LogInfo("Connected to broadcast client.");
                IsConnected = true;
            } else {
                DynLeaderboardsPlugin.LogError($"Failed to connect to broadcast client. Err: {error}. Trying again..");
                RequestConnection();
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    try {
                        DynLeaderboardsPlugin.LogInfo("Disposed.");
                        if (_client != null) {
                            MessageHandler.Disconnect();
                            _client.Close();
                            _client.Dispose();
                            _client = null;
                            IsConnected = false;
                        }
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ACCUdpRemoteClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }

    /// <summary>
    /// Configuration of ACCUdpRemoteClient
    /// </summary>
    internal class ACCUdpRemoteClientConfig {
        /// Class to read acc\Config\broadcasting.json
        private struct ACCBroadcastConfig {
            private int _udpListenerPort;
            public int updListenerPort {
                get => _udpListenerPort;
                set {
                    ValidatePort(value);
                    _udpListenerPort = value;
                }
            }
            public string connectionPassword { get; set; }
            public string commandPassword { get; set; }

            internal static ACCBroadcastConfig ACCDefault() {
                return new ACCBroadcastConfig {
                    updListenerPort = 9000,
                    connectionPassword = "asd",
                    commandPassword = ""
                };
            }

            internal static void ValidatePort(int port) {
                if (port < 1024 || port > 65535) {
                    throw new InvalidDataException($"The port set in '{DynLeaderboardsPlugin.Settings.AccDataLocation}\\Config\\broadcasting.json' is invalid. It must be between 1024 and 65535.");
                }
            }

            internal void Validate() {
                ValidatePort(updListenerPort);
            }
        }

        internal string Ip { get; }
        internal int Port => _config.updListenerPort;
        internal string DisplayName { get; }
        internal string ConnectionPassword => _config.connectionPassword;
        internal string CommandPassword => _config.commandPassword;
        internal int UpdateIntervalMs { get; }

        private ACCBroadcastConfig _config;

        /// <summary>
        /// Port, connectionPassword, commandPassword are read from the ..\\Assetto Corsa Competizione\\Config\\broadcasting.json.
        /// </summary>
        internal ACCUdpRemoteClientConfig(string ip, string displayName, int updateTime) {
            try {
                var configPath = $"{DynLeaderboardsPlugin.Settings.AccDataLocation}\\Config\\broadcasting.json";
                var rawJson = File.ReadAllText(configPath, Encoding.Unicode).Replace("\"", "'");
                _config = JsonConvert.DeserializeObject<ACCBroadcastConfig>(rawJson);
                _config.Validate();
            } catch (Exception e) {
                DynLeaderboardsPlugin.LogWarn($"Couldn't read broadcasting.json. Using default, it may or may not work. Underlying error: {e}.");
                _config = ACCBroadcastConfig.ACCDefault();
            }

            Ip = ip;
            DisplayName = displayName;
            UpdateIntervalMs = updateTime;
        }
    }
}