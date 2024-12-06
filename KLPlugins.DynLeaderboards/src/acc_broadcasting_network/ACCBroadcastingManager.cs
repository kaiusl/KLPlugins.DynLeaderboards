using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;

using ksBroadcastingNetwork.Structs;

namespace KLPlugins.DynLeaderboards.AccBroadcastingNetwork;

internal class AccBroadcastingManager {
    private AccUdpRemoteClient? _client;

    private AccBroadcastingRawData? _data = null;
    private readonly Dictionary<ushort, CarInfo> _entryList = new();
    private static readonly SimHubAccCarsInfo _broadcastIdToNameAndClass;
    private TrackData? _trackData = null;

    internal bool _IsConnected => this._client?._IsConnected ?? false;
    internal DateTime _LastUpdate => this._client?._LastUpdate ?? DateTime.Now;
    public event Action<GameDataBase>? OnDataUpdated;

    static AccBroadcastingManager() {
        AccBroadcastingManager._broadcastIdToNameAndClass = new SimHubAccCarsInfo();
    }

    public AccBroadcastingManager(int delay = 0) {
        this._client = new AccUdpRemoteClient(
            new AccUdpRemoteClientConfig(
                "127.0.0.1",
                "DynLeaderboardsPlugin",
                DynLeaderboardsPlugin._Settings.BroadcastDataUpdateRateMs
            ),
            delay
        );
        this._client._MessageHandler.OnNewEntrylist += this.OnNewEntryList;
        this._client._MessageHandler.OnEntrylistUpdate += this.OnEntryListUpdate;
        this._client._MessageHandler.OnRealtimeCarUpdate += this.OnRealtimeCarUpdate;
        this._client._MessageHandler.OnRealtimeUpdate += this.OnRealtimeUpdate;
        this._client._MessageHandler.OnTrackDataUpdate += this.OnTrackDataUpdate;
        this._client._MessageHandler.OnBroadcastingEvent += this.OnBroadcastingEvent;
    }

    ~AccBroadcastingManager() {
        this.Dispose();
    }

    internal void Dispose() {
        Logging.LogInfo("Disposing...");
        this.OnDataUpdated = null;

        if (this._client != null) {
            this._client._MessageHandler.OnNewEntrylist -= this.OnNewEntryList;
            this._client._MessageHandler.OnEntrylistUpdate -= this.OnEntryListUpdate;
            this._client._MessageHandler.OnRealtimeCarUpdate -= this.OnRealtimeCarUpdate;
            this._client._MessageHandler.OnRealtimeUpdate -= this.OnRealtimeUpdate;
            this._client._MessageHandler.OnTrackDataUpdate -= this.OnTrackDataUpdate;
            this._client._MessageHandler.OnBroadcastingEvent -= this.OnBroadcastingEvent;
            this._client.Dispose();
            this._client = null;
        }
    }

    private void OnNewEntryList(string sender) {
        this._entryList.Clear();
    }

    private void OnEntryListUpdate(string sender, in CarInfo carInfo) {
        var id = carInfo.CarIndex;
        this._entryList.Add(id, carInfo);
    }

    private void OnRealtimeUpdate(string sender, RealtimeUpdate update) {
        if (this._data != null && this._data._RealtimeCarUpdates.Count != 0) {
            this._data._TrackData = this._trackData;
            this.OnDataUpdated?.Invoke(new GameDataBase(this._data));
        }

        // must create new raw data since GameDataBase will hold onto a reference of the raw data it got
        // and we don't want to change that data
        this._data = new AccBroadcastingRawData(update, this._data?._RealtimeCarUpdates.Count ?? 1);
    }

    private void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate carUpdate) {
        if (!this._entryList.TryGetValue((ushort)carUpdate.CarIndex, out var carInfo)) {
            // missing car, wait for new entry list before adding it
            return;
        }

        carUpdate.CarEntry = carInfo;
        var info = AccBroadcastingManager._broadcastIdToNameAndClass.GetCarInfo(carInfo.CarModelType);
        var name = info?.Item1 ?? carInfo.CarModelType.ToString();
        var cls = info?.Item2 ?? CarClass.Default;
        this._data!._RealtimeCarUpdates.Add((carUpdate, name, cls));
    }

    private void OnTrackDataUpdate(string sender, TrackData trackData) {
        this._trackData = trackData;
    }


    private void OnBroadcastingEvent(string sender, in BroadcastingEvent broadcastingEvent) {
        // Logging.LogInfo($"Broadcasting event: {broadcastingEvent}");
    }
}

internal class AccBroadcastingRawData {
    public AccBroadcastingRawData(RealtimeUpdate realtimeUpdate, int expectedCarCount = 1) {
        this._RealtimeUpdate = realtimeUpdate;
        this._RealtimeCarUpdates = new List<(RealtimeCarUpdate, string, CarClass)>(expectedCarCount);
    }

    internal TrackData? _TrackData { get; set; }
    internal RealtimeUpdate _RealtimeUpdate { get; set; }
    internal List<(RealtimeCarUpdate, string, CarClass)> _RealtimeCarUpdates { get; set; }
}

internal class SimHubAccCarsInfo {
    private readonly Dictionary<byte, (string, CarClass)> _broadcastIdToNameAndClass = new();

    internal SimHubAccCarsInfo() {
        const string BROADCAST_ID_TO_CAR_ID_PATH = ".\\LookupTables\\AssettoCorsaCompetizione.BroadcastIdToCarId.csv";
        const string CAR_ID_TO_CAR_NAME_PATH = ".\\LookupTables\\AssettoCorsaCompetizione.CarNames.csv";
        const string CAR_ID_TO_CAR_CLASS_PATH = ".\\LookupTables\\AssettoCorsaCompetizione.CarClasses.csv";

        if (!File.Exists(BROADCAST_ID_TO_CAR_ID_PATH)
            || !File.Exists(CAR_ID_TO_CAR_NAME_PATH)
            || !File.Exists(CAR_ID_TO_CAR_CLASS_PATH)) {
            return;
        }

        var carIdToCarName = File.ReadAllLines(CAR_ID_TO_CAR_NAME_PATH)
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("//"))
            .Select(
                l => {
                    var splits = l.Split('\t');
                    return (splits[0].Trim(), splits[1].Trim());
                }
            )
            .ToDictionary(a => a.Item1, a => a.Item2);

        var carIdToCarClass = File.ReadAllLines(CAR_ID_TO_CAR_CLASS_PATH)
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("//"))
            .Select(
                l => {
                    var splits = l.Split('\t');
                    return (splits[0].Trim(), new CarClass(splits[1].Trim()));
                }
            )
            .ToDictionary(a => a.Item1, a => a.Item2);

        foreach (var l in File.ReadLines(BROADCAST_ID_TO_CAR_ID_PATH)) {
            if (string.IsNullOrEmpty(l) || l.StartsWith("//")) {
                continue;
            }

            var splits = l.Split('\t');
            var broadcastId = byte.Parse(splits[0].Trim());
            var carId = splits[1].Trim();
            this._broadcastIdToNameAndClass.Add(broadcastId, (carIdToCarName[carId], carIdToCarClass[carId]));
        }
    }

    internal (string, CarClass)? GetCarInfo(byte broadcastId) {
        return this._broadcastIdToNameAndClass.TryGetValue(broadcastId, out var val) ? val : null;
    }
}