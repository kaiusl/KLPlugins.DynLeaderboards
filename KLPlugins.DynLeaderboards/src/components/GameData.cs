using System;
using System.Collections.Generic;
using System.Linq;

using ACSharedMemory.Models;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards;

internal class GameData {
    internal GameDataBase _OldData;
    internal GameDataBase _NewData;

    public GameData(StatusDataBase oldData, StatusDataBase newData) {
        this._OldData = new GameDataBase(oldData);
        this._NewData = new GameDataBase(newData);
    }

    public GameData(GameDataBase oldData, GameDataBase newData) {
        this._OldData = oldData;
        this._NewData = newData;
    }

    internal void Update(StatusDataBase data) {
        (this._OldData, this._NewData) = (this._NewData, this._OldData);
        this._NewData = new GameDataBase(data);
    }

    internal void Update(GameDataBase data) {
        (this._OldData, this._NewData) = (this._NewData, this._OldData);
        this._NewData = data;
    }
}

internal class GameDataBase {
    private object? _rawObject { get; }

    public double AirTemperature { get; private set; }
    public List<Opponent> Opponents { get; private set; }
    public int RemainingLaps { get; private set; }
    public TimeSpan SessionTimeLeft { get; private set; }

    public string SessionTypeName { get; private set; }
    public int TotalLaps { get; private set; }
    public string TrackCode =>
        string.IsNullOrEmpty(this._trackConfig) ? this.TrackName : this.TrackName + "-" + this._trackConfig;

    private string _trackConfig { get; }

    public string TrackName { get; }
    public double TyrePressureFrontLeft { get; private set; }
    public double TrackLength { get; private set; }

    internal GameDataBase(StatusDataBase data) {
        this.AirTemperature = data.AirTemperature;
        this.Opponents = data.Opponents;
        this.RemainingLaps = data.RemainingLaps;
        this.SessionTimeLeft = data.SessionTimeLeft;
        this.SessionTypeName = data.SessionTypeName;
        this.TotalLaps = data.TotalLaps;
        this._trackConfig = data.TrackConfig;
        this.TrackName = data.TrackName;
        this.TrackLength = data.TrackLength;
        this.TyrePressureFrontLeft = data.TyrePressureFrontLeft;

        this._rawObject = data.GetRawDataObject();
    }

    public object? GetRawDataObject() {
        return this._rawObject;
    }
}