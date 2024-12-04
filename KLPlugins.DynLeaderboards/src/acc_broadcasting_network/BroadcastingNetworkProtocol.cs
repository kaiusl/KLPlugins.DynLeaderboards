using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ksBroadcastingNetwork;
using ksBroadcastingNetwork.Structs;

using DriverCategory = ksBroadcastingNetwork.DriverCategory;

namespace KLPlugins.DynLeaderboards.AccBroadcastingNetwork;

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
        SAVE_MANUAL_REPLAY_HIGHLIGHT =
            60, // TODO, but planned: saving manual replays gives distributed clients the possibility to see the play the same replay
    }

    private enum InboundMessageTypes : byte {
        REGISTRATION_RESULT = 1,
        REALTIME_UPDATE = 2,
        REALTIME_CAR_UPDATE = 3,
        ENTRY_LIST = 4,
        ENTRY_LIST_CAR = 6,
        TRACK_DATA = 5,
        BROADCASTING_EVENT = 7,
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

    public const int BROADCASTING_PROTOCOL_VERSION = 4;
    public int ConnectionId { get; private set; }

    private readonly string _connectionIdentifier;
    private readonly SendMessageDelegate _send;
    private readonly SimHubAccCarsInfo _simHubAccCarsInfo = new();

    internal delegate void SendMessageDelegate(byte[] payload);

    #region Events

    internal delegate void ConnectionStateChangedDelegate(
        int connectionId,
        bool connectionSuccess,
        bool isReadonly,
        string error
    );

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
            throw new ArgumentNullException(
                nameof(connectionIdentifier),
                "No connection identifier set; we use this to distinguish different connections. Using the remote IP:Port is a good idea"
            );
        }

        this._connectionIdentifier = connectionIdentifier;
        this._send = sendMessageDelegate;
    }

    internal void OnConnection(int connectionId, bool connectionSuccess, bool isReadonly, string errMsg) {
        this.ConnectionId = connectionId;
        this.OnConnectionStateChanged?.Invoke(this.ConnectionId, connectionSuccess, isReadonly, errMsg);

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
                var errMsg = BroadcastingNetworkProtocol.ReadString(br);

                this.OnConnectionStateChanged?.Invoke(this.ConnectionId, connectionSuccess, isReadonly, errMsg);

                // In case this was successful, we will request the initial data
                this.RequestEntryList();
                this.RequestTrackData();
                break;
            }
            case InboundMessageTypes.ENTRY_LIST: {
                this._entryListCars.Clear();
                this.OnNewEntrylist?.Invoke(this._connectionIdentifier);
                break;
            }
            case InboundMessageTypes.ENTRY_LIST_CAR: {
                var carInfo = BroadcastingNetworkProtocol.ReadCarInfo(br);
                this._entryListCars.Add(new CarInfoMinimal(carInfo.CarIndex, (ushort)carInfo.Drivers.Count));
                this.OnEntrylistUpdate?.Invoke(this._connectionIdentifier, carInfo);
                break;
            }
            case InboundMessageTypes.REALTIME_UPDATE: {
                #if TIMINGS
                        var timer = DynLeaderboardsPlugin._timers?.AddAndRestart("RealtimeUpdate");
                #endif
                var update = BroadcastingNetworkProtocol.ReadRealtimeUpdate(br);
                this.OnRealtimeUpdate?.Invoke(this._connectionIdentifier, update);
                #if TIMINGS
                        timer?.StopAndWriteMicros();
                #endif
                break;
            }
            case InboundMessageTypes.REALTIME_CAR_UPDATE: {
                #if TIMINGS
                        var timer = DynLeaderboardsPlugin._timers?.AddAndRestart("RealtimeCarUpdate");
                #endif
                var carUpdate = BroadcastingNetworkProtocol.ReadRealtimeCarUpdate(br);
                // the concept is: "don't know a car or driver? ask for an entry list update"
                var carEntryIndex = this._entryListCars.FindIndex(x => x.Id == carUpdate.CarIndex);
                if (carEntryIndex == -1 || this._entryListCars[carEntryIndex].DriverCount != carUpdate.DriverCount) {
                    // Add small wait before a new request so we don't spam ACC with multiple requests
                    // The new entry list update may take some time to be sent
                    if ((DateTime.Now - this._lastEntrylistRequest).TotalSeconds > 5) {
                        this._lastEntrylistRequest = DateTime.Now;
                        this.RequestEntryList();
                    }
                } else {
                    this.OnRealtimeCarUpdate?.Invoke(this._connectionIdentifier, carUpdate);
                    #if TIMINGS
                            timer?.StopAndWriteMicros();
                    # endif
                }

                break;
            }
            case InboundMessageTypes.TRACK_DATA: {
                var trackData = BroadcastingNetworkProtocol.ReadTrackData(br);
                this.OnTrackDataUpdate?.Invoke(this._connectionIdentifier, trackData);
                break;
            }
            case InboundMessageTypes.BROADCASTING_EVENT: {
                var evt = BroadcastingNetworkProtocol.ReadBroadcastingEvent(br);
                this.OnBroadcastingEvent?.Invoke(this._connectionIdentifier, evt);
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
    ///     Will try to register this client in the targeted ACC instance.
    ///     Needs to be called once, before anything else can happen.
    /// </summary>
    internal void RequestConnection(
        string displayName,
        string connectionPassword,
        int msRealtimeUpdateInterval,
        string commandPassword
    ) {
        this.SendRequest(
            OutboundMessageTypes.REGISTER_COMMAND_APPLICATION,
            br => {
                br.Write((byte)BroadcastingNetworkProtocol.BROADCASTING_PROTOCOL_VERSION);
                BroadcastingNetworkProtocol.WriteString(br, displayName);
                BroadcastingNetworkProtocol.WriteString(br, connectionPassword);
                br.Write(msRealtimeUpdateInterval);
                BroadcastingNetworkProtocol.WriteString(br, commandPassword);
            }
        );
    }

    internal void Disconnect() {
        this.SendRequest(OutboundMessageTypes.UNREGISTER_COMMAND_APPLICATION, br => br.Write(this.ConnectionId));
    }

    /// <summary>
    ///     Will ask the ACC client for an updated entry list, containing all car and driver data.
    ///     The client will send this automatically when something changes; however if you detect a carIndex or driverIndex,
    ///     this may cure the
    ///     problem for future updates
    /// </summary>
    internal void RequestEntryList() {
        this.SendRequest(OutboundMessageTypes.REQUEST_ENTRY_LIST, br => br.Write(this.ConnectionId));
    }

    internal void RequestTrackData() {
        this.SendRequest(OutboundMessageTypes.REQUEST_TRACK_DATA, br => br.Write(this.ConnectionId));
    }

    private void SendRequest(OutboundMessageTypes msgType, Action<BinaryWriter> msg) {
        using var ms = new MemoryStream();
        using var br = new BinaryWriter(ms);
        br.Write((byte)msgType); // First byte is always the command type
        msg(br);
        this._send(ms.ToArray());
    }

    private static TrackData ReadTrackData(BinaryReader br) {
        var _ = br.ReadInt32(); // connectionId
        return new TrackData {
            TrackName = BroadcastingNetworkProtocol.ReadString(br),
            TrackId = br.ReadInt32(),
            TrackMeters = br.ReadInt32(),
        };
    }

    private static BroadcastingEvent ReadBroadcastingEvent(BinaryReader br) {
        return new BroadcastingEvent {
            Type = (BroadcastingCarEventType)br.ReadByte(),
            Msg = BroadcastingNetworkProtocol.ReadString(br),
            TimeMs = br.ReadInt32(),
            CarId = br.ReadInt32(),
        };
    }

    private static CarInfo ReadCarInfo(BinaryReader br) {
        var info = new CarInfo {
            CarIndex = br.ReadUInt16(),
            CarModelType = br.ReadByte(),
            TeamName = BroadcastingNetworkProtocol.ReadString(br),
            RaceNumber = br.ReadInt32(),
            // Cup: Overall/Pro = 0, ProAm = 1, Am = 2, Silver = 3, National = 4
            CupCategory = br.ReadByte(),
            CurrentDriverIndex = br.ReadByte(),
            // Nationality = (NationalityEnum)br.ReadUInt16(),
        };
        var Nationality = (NationalityEnum)br.ReadUInt16();
        // Now the drivers on this car:
        var driversOnCarCount = br.ReadByte();

        for (var di = 0; di < driversOnCarCount; di++) {
            info.Drivers.Add(BroadcastingNetworkProtocol.ReadDriverInfo(br));
        }

        return info;
    }

    private static DriverInfo ReadDriverInfo(BinaryReader br) {
        return new DriverInfo {
            FirstName = BroadcastingNetworkProtocol.ReadString(br),
            LastName = BroadcastingNetworkProtocol.ReadString(br),
            ShortName = BroadcastingNetworkProtocol.ReadString(br),
            Category = (DriverCategory)br.ReadByte(), // Platinum = 3, Gold = 2, Silver = 1, Bronze = 0
            Nationality = (NationalityEnum)br.ReadUInt16(),
        };
    }

    private static RealtimeUpdate ReadRealtimeUpdate(BinaryReader br) {
        var update = new RealtimeUpdate {
            EventIndex = br.ReadUInt16(),
            SessionIndex = br.ReadUInt16(),
            SessionType = (RaceSessionType)br.ReadByte(),
            Phase = (ksBroadcastingNetwork.SessionPhase)br.ReadByte(),
            SessionTime = TimeSpan.FromMilliseconds(br.ReadSingle()),
            SessionRemainingTime = TimeSpan.FromMilliseconds(br.ReadSingle()),
            FocusedCarIndex = br.ReadInt32(),
            ActiveCameraSet = BroadcastingNetworkProtocol.ReadString(br),
            ActiveCamera = BroadcastingNetworkProtocol.ReadString(br),
            CurrentHudPage = BroadcastingNetworkProtocol.ReadString(br),
            IsReplayPlaying = br.ReadByte() > 0,
        };

        if (update.IsReplayPlaying) {
            update.ReplaySessionTime = br.ReadSingle();
            update.ReplayRemainingTime = br.ReadSingle();
        }

        update.TimeOfDay = TimeSpan.FromMilliseconds(br.ReadSingle());
        update.AmbientTemp = br.ReadByte();
        update.TrackTemp = br.ReadByte();
        update.Clouds = br.ReadByte() / 10.0f;
        update.RainLevel = br.ReadByte() / 10.0f;
        update.Wetness = br.ReadByte() / 10.0f;

        update.BestSessionLap = BroadcastingNetworkProtocol.ReadLapInfo(br);

        return update;
    }

    private static LapInfo ReadLapInfo(BinaryReader br) {
        var lap = new LapInfo();

        var lapTime = br.ReadInt32();
        if (lapTime == int.MaxValue) {
            lap.LaptimeMS = null;
        } else {
            lap.LaptimeMS = lapTime;
        }

        lap.CarIndex = br.ReadUInt16();
        lap.DriverIndex = br.ReadUInt16();

        lap.Splits = [];
        var splitCount = br.ReadByte();
        for (var i = 0; i < splitCount; i++) {
            var split = br.ReadInt32();
            if (split == int.MaxValue) {
                lap.Splits.Add(null);
            } else {
                lap.Splits.Add(split);
            }
        }

        while (lap.Splits.Count < 3) {
            lap.Splits.Add(null);
        }

        lap.IsInvalid = br.ReadByte() > 0;
        lap.IsValidForBest = br.ReadByte() > 0;

        var isOutLap = br.ReadByte() > 0;
        var isInLap = br.ReadByte() > 0;

        if (isOutLap) {
            lap.Type = LapType.Outlap;
        } else if (isInLap) {
            lap.Type = LapType.Inlap;
        } else {
            lap.Type = LapType.Regular;
        }

        return lap;
    }

    private static RealtimeCarUpdate ReadRealtimeCarUpdate(BinaryReader br) {
        var update = new RealtimeCarUpdate {
            CarIndex = br.ReadUInt16(),
            DriverIndex = br.ReadUInt16(), // Driver swap will make this change
            DriverCount = br.ReadByte(),
            Gear = br.ReadByte() - 2, // -2 makes the R -1, N 0 and the rest as-is
            WorldPosX = br.ReadSingle(),
            WorldPosY = br.ReadSingle(),
            Yaw = br.ReadSingle(),
            CarLocation = (CarLocationEnum)br.ReadByte(), // - , Track, Pitlane, PitEntry, PitExit = 4
            Kmh = br.ReadUInt16(),
            Position = br.ReadUInt16(), // official P/Q/R position (1 based)
            CupPosition = br.ReadUInt16(), // official P/Q/R position (1 based)
            TrackPosition = br.ReadUInt16(), // position on track (1 based)
            SplinePosition = br.ReadSingle(),
            Laps = br.ReadUInt16(),
            Delta = br.ReadInt32(), // Realtime delta to best session lap
            BestSessionLap = BroadcastingNetworkProtocol.ReadLapInfo(br),
            LastLap = BroadcastingNetworkProtocol.ReadLapInfo(br),
            CurrentLap = BroadcastingNetworkProtocol.ReadLapInfo(br),
        };
        return update;
    }
}