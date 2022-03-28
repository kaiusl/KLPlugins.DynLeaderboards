// Original from ACC Broadcasting SDK example (Assetto Corsa Competizione Dedicated Server\sdk\broadcasting)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KLPlugins.Leaderboard;
using Newtonsoft.Json;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork {
    public class ACCUdpRemoteClient : IDisposable
    {
        public BroadcastingNetworkProtocol MessageHandler { get; }
        public string IpPort { get; }
        public string DisplayName { get; }
        public string ConnectionPassword { get; }
        public string CommandPassword { get; }
        public int MsRealtimeUpdateInterval { get; }
        public bool IsConnected { get; private set; }

        private UdpClient _client;
        private Task _listenerTask;

        /// <summary>
        /// To get the events delivered inside the UI thread, just create this object from the UI thread/synchronization context.
        /// </summary>
        public ACCUdpRemoteClient(string ip, int port, string displayName, string connectionPassword, string commandPassword, int msRealtimeUpdateInterval)
        {
            IpPort = $"{ip}:{port}";
            MessageHandler = new BroadcastingNetworkProtocol(IpPort, Send);
            _client = new UdpClient();
            _client.Connect(ip, port);

            DisplayName = displayName;
            ConnectionPassword = connectionPassword;
            CommandPassword = commandPassword;
            MsRealtimeUpdateInterval = msRealtimeUpdateInterval;
            IsConnected = false;
            MessageHandler.OnConnectionStateChanged += OnBroadcastConnectionStateChanged;

            LeaderboardPlugin.LogInfo("Requested broadcast connection");
            _listenerTask = Connect();
        }

        public ACCUdpRemoteClient(ACCUdpRemoteClientConfig cfg) : this(cfg.Ip, cfg.Port, cfg.DisplayName, cfg.ConnectionPassword, cfg.CommandPassword, cfg.UpdateIntervalMs) { }

        private void Send(byte[] payload)
        {
            var sent = _client.Send(payload, payload.Length);
        }

        public void Shutdown()
        {
            ShutdownAsync().ContinueWith(t =>
             {
                 if (t.Exception?.InnerExceptions?.Any() == true)
                     LeaderboardPlugin.LogError($"Client shut down with {t.Exception.InnerExceptions.Count} errors");
                 else
                     LeaderboardPlugin.LogInfo("Client shut down asynchronously");

             });
        }

        public async Task ShutdownAsync()
        {
            if (_listenerTask != null && !_listenerTask.IsCompleted)
            {
                MessageHandler.Disconnect();
                _client.Close();
                _client = null;
                IsConnected = false;
                await _listenerTask;
            }
        }

        private async Task Connect() {
            MessageHandler.RequestConnection(DisplayName, ConnectionPassword, MsRealtimeUpdateInterval, CommandPassword);
            while (_client != null && !IsConnected) {
                try {
                    var udpPacket = await _client.ReceiveAsync();
                    using (var ms = new System.IO.MemoryStream(udpPacket.Buffer))
                    using (var reader = new System.IO.BinaryReader(ms)) {
                        MessageHandler.ProcessMessage(reader);
                    }
                } catch (ObjectDisposedException) {
                    // Shutdown happened
                    LeaderboardPlugin.LogInfo("Broadcast client shut down.");
                    break;
                } catch (Exception ex) {
                    // Other exceptions
                    LeaderboardPlugin.LogInfo($"Couldn't connect to broadcast client. Err {ex}");
                }
            }
        }

        private async Task Run()
        {
            while (_client != null)
            {
                try
                {
                    var udpPacket = await _client.ReceiveAsync();
                    using (var ms = new System.IO.MemoryStream(udpPacket.Buffer))
                    using (var reader = new System.IO.BinaryReader(ms))
                    {
                        MessageHandler.ProcessMessage(reader);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Shutdown happened
                    LeaderboardPlugin.LogInfo("Broadcast client shut down.");
                    break;
                }
                catch (Exception ex)
                {
                    // Other exceptions
                    LeaderboardPlugin.LogInfo($"Couldn't connect to broadcast client. Err {ex}");
                }
            }
            IsConnected = false;
        }

        private async void OnBroadcastConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadonly, string error) {
            if (connectionSuccess) {
                LeaderboardPlugin.LogInfo("Connected to broadcast client.");
                IsConnected = true;
                await _listenerTask;
                _listenerTask = Run();
            } else {
                LeaderboardPlugin.LogWarn($"Failed to connect to broadcast client. Err: {error}");
                MessageHandler.RequestConnection(DisplayName, ConnectionPassword, MsRealtimeUpdateInterval, CommandPassword);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        LeaderboardPlugin.LogInfo("Disposed.");
                        if (_client != null)
                        {
                            _client.Close();
                            _client.Dispose();
                            _client = null;
                            IsConnected = false;
                        }

                    }
                    catch (Exception ex)
                    {
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
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    /// <summary>
    /// Configuration of ACCUdpRemoteClient
    /// </summary>
    public class ACCUdpRemoteClientConfig { 
        class ACCBroadcastConfig {
            // Class to read acc\Config\broadcasting.json
            internal int udpListenerPort { get; set; }
            internal string connectionPassword { get; set; }
            internal string commandPassword { get; set; }

            private const int _defUdpListenerPort = 9000;
            private const string _defConnectionPassword = "asd";
            private const string _defCommandPassowrd = "";

            internal ACCBroadcastConfig() {
                udpListenerPort = _defUdpListenerPort;
                connectionPassword = _defConnectionPassword;
                commandPassword = _defCommandPassowrd;
            }

            internal void Validate() {
                if (udpListenerPort < 1024 || udpListenerPort > 65535) {
                    udpListenerPort = _defUdpListenerPort;
                    throw new Exception($"Set broadcasting udp port in '{LeaderboardPlugin.Settings.AccDataLocation}\\Config\\broadcasting.json' is invalid. Must be between 1024 and 65535.");
                }
            }
        }

        public string Ip { get; private set; }
        public int Port => _config.udpListenerPort;
        public string DisplayName { get; private set; }
        public string ConnectionPassword => _config.connectionPassword;
        public string CommandPassword => _config.commandPassword;
        public int UpdateIntervalMs { get; private set; }

        private ACCBroadcastConfig _config;

        /// <summary>
        /// Port, connectionPassword, commandPassword are read from the ..\\Assetto Corsa Competizione\\Config\\broadcasting.json. 
        /// </summary>
        public ACCUdpRemoteClientConfig(string ip, string displayName, int updateTime) {
            try {
                var configPath = $"{LeaderboardPlugin.Settings.AccDataLocation}\\Config\\broadcasting.json";
                _config = JsonConvert.DeserializeObject<ACCBroadcastConfig>(File.ReadAllText(configPath, Encoding.Unicode).Replace("\"", "'"));
                _config.Validate();
            } catch (Exception e) {
                LeaderboardPlugin.LogWarn($"Couldn't read broadcasting.json because {e}. Using default.");
                _config = new ACCBroadcastConfig();
            }

            Ip = ip;
            DisplayName = displayName;
            UpdateIntervalMs = updateTime;
        }
    }

    

}
