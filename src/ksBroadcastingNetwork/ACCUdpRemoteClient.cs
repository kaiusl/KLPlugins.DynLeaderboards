﻿// Original from ACC Broadcasting SDK example (Assetto Corsa Competizione Dedicated Server\sdk\broadcasting)

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

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
        private readonly Task _listenerTask;

        /// <summary>
        /// To get the events delivered inside the UI thread, just create this object from the UI thread/synchronization context.
        /// </summary>
        internal ACCUdpRemoteClient(string ip, int port, string displayName, string connectionPassword, string commandPassword, int msRealtimeUpdateInterval) {
            this._ipPort = $"{ip}:{port}";
            this.MessageHandler = new BroadcastingNetworkProtocol(this._ipPort, this.Send);
            this._client = new UdpClient();
            this._client.Connect(ip, port);

            this._displayName = displayName;
            this._connectionPassword = connectionPassword;
            this._commandPassword = commandPassword;
            this._msRealtimeUpdateInterval = msRealtimeUpdateInterval;
            this.IsConnected = false;
            this.MessageHandler.OnConnectionStateChanged += this.OnBroadcastConnectionStateChanged;

            DynLeaderboardsPlugin.LogInfo("Requested broadcast connection");
            this._listenerTask = this.ConnectAndRun();
        }

        internal ACCUdpRemoteClient(ACCUdpRemoteClientConfig cfg) : this(cfg.Ip, cfg.Port, cfg.DisplayName, cfg.ConnectionPassword, cfg.CommandPassword, cfg.UpdateIntervalMs) {
        }

        private void Send(byte[] payload) {
            if (this._client == null) {
                DynLeaderboardsPlugin.LogWarn($"Tried to send a message to ACC but our client has already been shut down.");
                return;
            }
            _ = this._client.Send(payload, payload.Length);
        }

        internal void Shutdown() {
            this.ShutdownAsync().ContinueWith(t => {
                if (t.Exception?.InnerExceptions?.Any() == true) {
                    DynLeaderboardsPlugin.LogError($"Client shut down with {t.Exception.InnerExceptions.Count} errors");
                } else {
                    DynLeaderboardsPlugin.LogInfo("Client shut down asynchronously");
                }
            });
        }

        internal async Task ShutdownAsync() {
            if (this._listenerTask != null && !this._listenerTask.IsCompleted) {
                this.MessageHandler.Disconnect();
                this._client?.Close();
                this._client = null;
                this.IsConnected = false;
                await this._listenerTask;
            }
        }

        private async Task ConnectAndRun() {
            this.RequestConnection();
            while (this._client != null) {
                try {
                    var udpPacket = await this._client.ReceiveAsync();
                    using var ms = new System.IO.MemoryStream(udpPacket.Buffer);
                    using var reader = new System.IO.BinaryReader(ms);
                    this.MessageHandler.ProcessMessage(reader);
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
            this.IsConnected = false;
        }

        private void RequestConnection() {
            this.MessageHandler.RequestConnection(
                    displayName: this._displayName,
                    connectionPassword: this._connectionPassword,
                    msRealtimeUpdateInterval: this._msRealtimeUpdateInterval,
                    commandPassword: this._commandPassword
                );
        }

        private void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
            if (connectionSuccess) {
                DynLeaderboardsPlugin.LogInfo("Connected to broadcast client.");
                this.IsConnected = true;
            } else {
                DynLeaderboardsPlugin.LogError($"Failed to connect to broadcast client. Err: {error}. Trying again..");
                this.RequestConnection();
            }
        }

        #region IDisposable Support

        private bool _disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing) {
            if (!this._disposedValue) {
                if (disposing) {
                    try {
                        DynLeaderboardsPlugin.LogInfo("Disposed.");
                        if (this._client != null) {
                            this.MessageHandler.Disconnect();
                            this._client.Close();
                            this._client.Dispose();
                            this._client = null;
                            this.IsConnected = false;
                        }
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                this._disposedValue = true;
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
            this.Dispose(true);
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
#pragma warning disable IDE1006
            // Property names must match with the ones in json
            private int _udpListenerPort;
            public int updListenerPort {
                readonly get => this._udpListenerPort;
                set {
                    ValidatePort(value);
                    this._udpListenerPort = value;
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

            internal readonly void Validate() {
                ValidatePort(this.updListenerPort);
            }
        }

        internal string Ip { get; }
        internal int Port => this._config.updListenerPort;
        internal string DisplayName { get; }
        internal string ConnectionPassword => this._config.connectionPassword;
        internal string CommandPassword => this._config.commandPassword;
        internal int UpdateIntervalMs { get; }

        private ACCBroadcastConfig _config;

        /// <summary>
        /// Port, connectionPassword, commandPassword are read from the ..\\Assetto Corsa Competizione\\Config\\broadcasting.json.
        /// </summary>
        internal ACCUdpRemoteClientConfig(string ip, string displayName, int updateTime) {
            try {
                var configPath = $"{DynLeaderboardsPlugin.Settings.AccDataLocation}\\Config\\broadcasting.json";
                var rawJson = File.ReadAllText(configPath, Encoding.Unicode).Replace("\"", "'");
                this._config = JsonConvert.DeserializeObject<ACCBroadcastConfig>(rawJson);
                this._config.Validate();
            } catch (Exception e) {
                DynLeaderboardsPlugin.LogWarn($"Couldn't read broadcasting.json. Using default, it may or may not work. Underlying error: {e}.");
                this._config = ACCBroadcastConfig.ACCDefault();
            }

            this.Ip = ip;
            this.DisplayName = displayName;
            this.UpdateIntervalMs = updateTime;
        }
    }
}