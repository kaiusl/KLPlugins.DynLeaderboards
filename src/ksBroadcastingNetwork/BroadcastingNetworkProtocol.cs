using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Track;

namespace KLPlugins.DynLeaderboards.ksBroadcastingNetwork {
    internal class BroadcastingNetworkProtocol {

        private enum OutboundMessageTypes : byte {
            REGISTER_COMMAND_APPLICATION = 1,
            UNREGISTER_COMMAND_APPLICATION = 9,

            REQUEST_ENTRY_LIST = 10,
            REQUEST_TRACK_DATA = 11,

            CHANGE_HUD_PAGE = 49,
            CHANGE_FOCUS = 50,
            INSTANT_REPLAY_REQUEST = 51,

            PLAY_MANUAL_REPLAY_HIGHLIGHT = 52, // TODO, but planned
            SAVE_MANUAL_REPLAY_HIGHLIGHT = 60  // TODO, but planned: saving manual replays gives distributed clients the possibility to see the play the same replay
        }

        private enum InboundMessageTypes : byte {
            REGISTRATION_RESULT = 1,
            REALTIME_UPDATE = 2,
            REALTIME_CAR_UPDATE = 3,
            ENTRY_LIST = 4,
            ENTRY_LIST_CAR = 6,
            TRACK_DATA = 5,
            BROADCASTING_EVENT = 7
        }

        /// Struct that stores minimal amount of car info which is needed by the
        /// BroadcastigNetworkProtocol to properly function
        private readonly struct CarInfoMinimal {
            internal ushort Id { get; }
            internal ushort DriverCount { get; }

            internal CarInfoMinimal(ushort id, ushort driverCount) {
                this.Id = id;
                this.DriverCount = driverCount;
            }
        }

        public const int BroadcastingProtocolVersion = 4;
        public int ConnectionId { get; private set; }

        private double _trackSplinePosOffset = 0.0;
        private readonly string _connectionIdentifier;
        private readonly SendMessageDelegate _send;
        internal delegate void SendMessageDelegate(byte[] payload);

        #region Events

        internal delegate void ConnectionStateChangedDelegate(int connectionId, bool connectionSuccess, bool isReadonly, string error);
        internal delegate void TrackDataUpdateDelegate(string sender, TrackData trackUpdate);
        internal delegate void NewEntryListDelegate(string sender);
        internal delegate void EntryListUpdateDelegate(string sender, in CarInfo car);
        internal delegate void RealtimeUpdateDelegate(string sender, RealtimeUpdate update);
        internal delegate void RealtimeCarUpdateDelegate(string sender, RealtimeCarUpdate carUpdate);
        internal delegate void BroadcastingEventDelegate(string sender, in BroadcastingEvent evt);

        internal event ConnectionStateChangedDelegate? OnConnectionStateChanged;
        internal event TrackDataUpdateDelegate? OnTrackDataUpdate;
        internal event NewEntryListDelegate? OnNewEntrylist;
        internal event EntryListUpdateDelegate? OnEntrylistUpdate;
        internal event RealtimeUpdateDelegate? OnRealtimeUpdate;
        internal event RealtimeCarUpdateDelegate? OnRealtimeCarUpdate;
        internal event BroadcastingEventDelegate? OnBroadcastingEvent;

        #endregion Events

        #region EntryList handling

        // To avoid huge UDP pakets for longer entry lists, we will first receive the indexes of cars and drivers,
        // cache the entries and wait for the detailled updates
        private readonly List<CarInfoMinimal> _entryListCars = new();

        #endregion EntryList handling

        #region optional failsafety - detect when we have a desync and need a new entry list

        private DateTime _lastEntrylistRequest = DateTime.Now;

        #endregion optional failsafety - detect when we have a desync and need a new entry list

        internal BroadcastingNetworkProtocol(string connectionIdentifier, SendMessageDelegate sendMessageDelegate) {
            if (string.IsNullOrEmpty(connectionIdentifier)) {
                throw new ArgumentNullException(nameof(connectionIdentifier), $"No connection identifier set; we use this to distinguish different connections. Using the remote IP:Port is a good idea");
            }

            this._connectionIdentifier = connectionIdentifier;
            this._send = sendMessageDelegate;
        }

        internal void OnConnection(int connectionId, bool connectionSuccess, bool isReadonly, string errMsg) {
            this.ConnectionId = connectionId;
            OnConnectionStateChanged?.Invoke(this.ConnectionId, connectionSuccess, isReadonly, errMsg);

            // In case this was successful, we will request the initial data
            this.RequestEntryList();
            this.RequestTrackData();
        }

        internal void ProcessMessage(BinaryReader br) {
            // Any message starts with an 1-byte command type
            var messageType = (InboundMessageTypes)br.ReadByte();
            switch (messageType) {
                case InboundMessageTypes.REGISTRATION_RESULT: {
                        this.ConnectionId = br.ReadInt32();
                        var connectionSuccess = br.ReadByte() > 0;
                        var isReadonly = br.ReadByte() == 0;
                        var errMsg = ReadString(br);

                        OnConnectionStateChanged?.Invoke(this.ConnectionId, connectionSuccess, isReadonly, errMsg);

                        // In case this was successful, we will request the initial data
                        this.RequestEntryList();
                        this.RequestTrackData();
                        break;
                    }
                case InboundMessageTypes.ENTRY_LIST: {
                        this._entryListCars.Clear();
                        OnNewEntrylist?.Invoke(this._connectionIdentifier);
                        break;
                    }
                case InboundMessageTypes.ENTRY_LIST_CAR: {
                        var carInfo = new CarInfo(br);
                        this._entryListCars.Add(new CarInfoMinimal(carInfo.Id, (ushort)carInfo.Drivers.Length));
                        OnEntrylistUpdate?.Invoke(this._connectionIdentifier, carInfo);
                        break;
                    }
                case InboundMessageTypes.REALTIME_UPDATE: {
#if TIMINGS
                        var timer = DynLeaderboardsPlugin._timers?.AddAndRestart("RealtimeUpdate");
#endif
                        var update = new RealtimeUpdate(br);
                        OnRealtimeUpdate?.Invoke(this._connectionIdentifier, update);
#if TIMINGS
                        timer?.StopAndWriteMicros();
#endif
                        break;
                    }
                case InboundMessageTypes.REALTIME_CAR_UPDATE: {
#if TIMINGS
                        var timer = DynLeaderboardsPlugin._timers?.AddAndRestart("RealtimeCarUpdate");
#endif
                        var carUpdate = new RealtimeCarUpdate(br, this._trackSplinePosOffset);
                        // the concept is: "don't know a car or driver? ask for an entry list update"
                        var carEntryIndex = this._entryListCars.FindIndex(x => x.Id == carUpdate.CarId);
                        if (carEntryIndex == -1 || this._entryListCars[carEntryIndex].DriverCount != carUpdate.DriverCount) {
                            // Add small wait before a new request so we don't spam ACC with multiple requests
                            // The new entry list update may take some time to be sent
                            if ((DateTime.Now - this._lastEntrylistRequest).TotalSeconds > 5) {
                                this._lastEntrylistRequest = DateTime.Now;
                                this.RequestEntryList();
                            }
                        } else {
                            OnRealtimeCarUpdate?.Invoke(this._connectionIdentifier, carUpdate);
#if TIMINGS
                            timer?.StopAndWriteMicros();
# endif
                        }
                        break;
                    }
                case InboundMessageTypes.TRACK_DATA: {
                        var trackData = new TrackData(br);
                        this._trackSplinePosOffset = trackData.SplinePosOffset;
                        OnTrackDataUpdate?.Invoke(this._connectionIdentifier, trackData);
                        break;
                    }
                case InboundMessageTypes.BROADCASTING_EVENT: {
                        var evt = new BroadcastingEvent(br);
                        OnBroadcastingEvent?.Invoke(this._connectionIdentifier, evt);
                        break;
                    }
                default:
                    break;
            }
        }

        internal static string ReadString(BinaryReader br) {
            var length = br.ReadUInt16();
            var bytes = br.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteString(BinaryWriter bw, string s) {
            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(Convert.ToUInt16(bytes.Length));
            bw.Write(bytes);
        }

        /// <summary>
        /// Will try to register this client in the targeted ACC instance.
        /// Needs to be called once, before anything else can happen.
        /// </summary>
        /// <param name="connectionPassword"></param>
        /// <param name="msRealtimeUpdateInterval"></param>
        /// <param name="commandPassword"></param>
        internal void RequestConnection(string displayName, string connectionPassword, int msRealtimeUpdateInterval, string commandPassword) {
            this.SendRequest(OutboundMessageTypes.REGISTER_COMMAND_APPLICATION, (br) => {
                br.Write((byte)BroadcastingProtocolVersion);
                WriteString(br, displayName);
                WriteString(br, connectionPassword);
                br.Write(msRealtimeUpdateInterval);
                WriteString(br, commandPassword);
            });
        }

        internal void Disconnect() {
            this.SendRequest(OutboundMessageTypes.UNREGISTER_COMMAND_APPLICATION, (br) => br.Write(this.ConnectionId));
        }

        /// <summary>
        /// Will ask the ACC client for an updated entry list, containing all car and driver data.
        /// The client will send this automatically when something changes; however if you detect a carIndex or driverIndex, this may cure the
        /// problem for future updates
        /// </summary>
        internal void RequestEntryList() {
            this.SendRequest(OutboundMessageTypes.REQUEST_ENTRY_LIST, (br) => br.Write(this.ConnectionId));
        }

        internal void RequestTrackData() {
            this.SendRequest(OutboundMessageTypes.REQUEST_TRACK_DATA, (br) => br.Write(this.ConnectionId));
        }

        private void SendRequest(OutboundMessageTypes msgType, Action<BinaryWriter> msg) {
            using var ms = new MemoryStream();
            using var br = new BinaryWriter(ms);
            br.Write((byte)msgType); // First byte is always the command type
            msg(br);
            this._send(ms.ToArray());
        }
    }

    internal readonly struct LapInfo {
        public double? Laptime { get; }
        public double?[] Splits { get; }
        public ushort CarId { get; }
        public ushort DriverIndex { get; }
        public bool IsInvalid { get; }
        public bool IsValidForBest { get; }
        public LapType Type { get; }

        internal LapInfo(BinaryReader br) {
            var laptime = br.ReadInt32();
            if (laptime == int.MaxValue) {
                this.Laptime = null;
            } else {
                this.Laptime = laptime / 1000.0;
            }

            this.CarId = br.ReadUInt16();
            this.DriverIndex = br.ReadUInt16();

            this.Splits = new double?[3];
            var splitCount = br.ReadByte();
            for (int i = 0; i < splitCount; i++) {
                var split = br.ReadInt32();
                if (split == int.MaxValue) {
                    this.Splits[i] = null;
                } else {
                    this.Splits[i] = split / 1000.0;
                }
            }

            this.IsInvalid = br.ReadByte() > 0;
            this.IsValidForBest = br.ReadByte() > 0;

            var isOutlap = br.ReadByte() > 0;
            var isInlap = br.ReadByte() > 0;

            if (isOutlap) {
                this.Type = LapType.Outlap;
            } else if (isInlap) {
                this.Type = LapType.Inlap;
            } else {
                this.Type = LapType.Regular;
            }
        }

        public override string ToString() {
            return $"{this.Laptime,5}|{string.Join("|", this.Splits)}";
        }
    }

    internal readonly struct BroadcastingEvent {
        public BroadcastingCarEventType Type { get; }
        public string Msg { get; }
        public double Time { get; }
        public int CarId { get; }

        internal BroadcastingEvent(BinaryReader br) {
            this.Type = (BroadcastingCarEventType)br.ReadByte();
            this.Msg = BroadcastingNetworkProtocol.ReadString(br);
            this.Time = br.ReadInt32() / 1000.0;
            this.CarId = br.ReadInt32();
        }
    }

    internal readonly struct CarInfo {
        public ushort Id { get; }
        public CarType ModelType { get; }
        public CarClass Class { get; }
        public string TeamName { get; }
        public int RaceNumber { get; }
        public TeamCupCategory CupCategory { get; }
        public int CurrentDriverIndex { get; }
        public DriverInfo[] Drivers { get; }
        public NationalityEnum TeamNationality { get; }

        public CarInfo(BinaryReader br) {
            this.Id = br.ReadUInt16();
            this.ModelType = (CarType)br.ReadByte(); // Byte sized car model
            this.Class = this.ModelType.Class();
            this.TeamName = BroadcastingNetworkProtocol.ReadString(br);
            this.RaceNumber = br.ReadInt32();
            this.CupCategory = (TeamCupCategory)br.ReadByte(); // Cup: Overall/Pro = 0, ProAm = 1, Am = 2, Silver = 3, National = 4
            this.CurrentDriverIndex = br.ReadByte();
            this.TeamNationality = (NationalityEnum)br.ReadUInt16();

            // Now the drivers on this car:
            var driversOnCarCount = br.ReadByte();
            this.Drivers = new DriverInfo[driversOnCarCount];
            for (int di = 0; di < driversOnCarCount; di++) {
                this.Drivers[di] = new DriverInfo(br);
            }
        }

        public string GetCurrentDriverName() {
            if (this.CurrentDriverIndex < this.Drivers.Length) {
                return this.Drivers[this.CurrentDriverIndex].LastName;
            }

            return "nobody(?)";
        }
    }

    internal readonly struct DriverInfo {
        public string FirstName { get; }
        public string LastName { get; }
        public string ShortName { get; }
        public DriverCategory Category { get; }
        public NationalityEnum Nationality { get; }

        internal DriverInfo(BinaryReader br) {
            this.FirstName = BroadcastingNetworkProtocol.ReadString(br);
            this.LastName = BroadcastingNetworkProtocol.ReadString(br);
            this.ShortName = BroadcastingNetworkProtocol.ReadString(br);
            this.Category = (DriverCategory)br.ReadByte(); // Platinum = 3, Gold = 2, Silver = 1, Bronze = 0
            this.Nationality = (NationalityEnum)br.ReadUInt16();
        }
    }

    internal class RealtimeCarUpdate {
        public int CarId { get; }
        public int DriverIndex { get; } // This changes after the first sector
        public int Gear { get; }
        public float WorldPosX { get; }
        public float WorldPosY { get; }
        public float Yaw { get; }
        public CarLocationEnum CarLocation { get; }
        public int Kmh { get; }
        public int Position { get; }
        public int TrackPosition { get; } // Seems to be zero always
        public double SplinePosition { get; }
        public int Delta { get; }
        public LapInfo BestSessionLap { get; } // This contains all the bests. Best lap time and best sectors not the sectors of the best lap.
        public LapInfo LastLap { get; }
        public LapInfo CurrentLap { get; }
        public int Laps { get; }
        public ushort CupPosition { get; }
        public byte DriverCount { get; }
        public bool IsInPitlane { get; }
        public bool IsOnTrack { get; }

        internal RealtimeCarUpdate(BinaryReader br, double trackSplinePosOffset) {
            this.CarId = br.ReadUInt16();
            this.DriverIndex = br.ReadUInt16(); // Driver swap will make this change
            this.DriverCount = br.ReadByte();
            this.Gear = br.ReadByte() - 2; // -2 makes the R -1, N 0 and the rest as-is
            this.WorldPosX = br.ReadSingle();
            this.WorldPosY = br.ReadSingle();
            this.Yaw = br.ReadSingle();
            this.CarLocation = (CarLocationEnum)br.ReadByte(); // - , Track, Pitlane, PitEntry, PitExit = 4
            this.Kmh = br.ReadUInt16();
            this.Position = br.ReadUInt16(); // official P/Q/R position (1 based)
            this.CupPosition = br.ReadUInt16(); // official P/Q/R position (1 based)
            this.TrackPosition = br.ReadUInt16(); // position on track (1 based)

            var splinePos = br.ReadSingle() + trackSplinePosOffset;
            if (splinePos >= 1) {
                splinePos -= 1;
            }
            this.SplinePosition = splinePos; // track position between 0.0 and 1.0
            this.Laps = br.ReadUInt16();

            this.Delta = br.ReadInt32(); // Realtime delta to best session lap
            this.BestSessionLap = new LapInfo(br);
            this.LastLap = new LapInfo(br);
            this.CurrentLap = new LapInfo(br);
            this.IsInPitlane = this.CarLocation == CarLocationEnum.Pitlane;
            this.IsOnTrack = this.CarLocation == CarLocationEnum.Track;
        }
    }

    internal class RealtimeUpdate {
        public int EventIndex { get; }
        public int SessionIndex { get; }
        public SessionPhase Phase { get; }
        public TimeSpan SessionRunningTime { get; } // Time the session has been running
        public TimeSpan RemainingTime { get; } // Seems to be zero always
        public TimeSpan SystemTime { get; } // Real world time
        public float RainLevel { get; }
        public float Clouds { get; }
        public float Wetness { get; }
        public LapInfo BestSessionLap { get; }
        public ushort BestLapCarIndex { get; }
        public ushort BestLapDriverIndex { get; }
        public int FocusedCarIndex { get; }
        public string ActiveCameraSet { get; }
        public string ActiveCamera { get; }
        public bool IsReplayPlaying { get; }
        public float ReplaySessionTime { get; }
        public float ReplayRemainingTime { get; }
        public TimeSpan SessionEndTime { get; } // Seems to be zero always
        public TimeSpan SessionRemainingTime { get; } // Time left until the session end
        public RaceSessionType SessionType { get; }
        public byte AmbientTemp { get; }
        public byte TrackTemp { get; }
        public string CurrentHudPage { get; }
        public DateTime RecieveTime { get; }

        internal RealtimeUpdate(BinaryReader br) {
            this.RecieveTime = DateTime.Now;
            this.EventIndex = (int)br.ReadUInt16();
            this.SessionIndex = (int)br.ReadUInt16();
            this.SessionType = (RaceSessionType)br.ReadByte();
            this.Phase = (SessionPhase)br.ReadByte();
            this.SessionRunningTime = TimeSpan.FromMilliseconds(br.ReadSingle());
            this.SessionRemainingTime = TimeSpan.FromMilliseconds(br.ReadSingle());

            this.FocusedCarIndex = br.ReadInt32();
            this.ActiveCameraSet = BroadcastingNetworkProtocol.ReadString(br);
            this.ActiveCamera = BroadcastingNetworkProtocol.ReadString(br);
            this.CurrentHudPage = BroadcastingNetworkProtocol.ReadString(br);

            this.IsReplayPlaying = br.ReadByte() > 0;
            if (this.IsReplayPlaying) {
                this.ReplaySessionTime = br.ReadSingle();
                this.ReplayRemainingTime = br.ReadSingle();
            }

            this.SystemTime = TimeSpan.FromMilliseconds(br.ReadSingle());
            this.AmbientTemp = br.ReadByte();
            this.TrackTemp = br.ReadByte();
            this.Clouds = br.ReadByte() / 10.0f;
            this.RainLevel = br.ReadByte() / 10.0f;
            this.Wetness = br.ReadByte() / 10.0f;

            this.BestSessionLap = new LapInfo(br);
        }
    }
}