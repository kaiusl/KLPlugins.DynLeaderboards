using System;
using System.Collections.Generic;

using ACSharedMemory.ACC.Reader;
using ACSharedMemory.Reader;

using GameReaderCommon;

using PCarsSharedMemory.AMS2.Models;

using R3E.Data;

using RfactorReader.RF2;

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
    public SessionType SessionType { get; private set; }
    public SessionPhase SessionPhase { get; private set; } = SessionPhase.UNKNOWN;
    public int SessionIndex { get; private set; } = 0;
    public int TotalLaps { get; private set; }
    public string TrackName { get; private set; }
    public double TrackLength { get; private set; }
    public double TyrePressureFrontLeft { get; private set; }
    public string? FocusedCarId { get; private set; } = null;

    internal GameDataBase(StatusDataBase data) {
        this.AirTemperature = data.AirTemperature;
        this.Opponents = data.Opponents;
        this.RemainingLaps = data.RemainingLaps;
        this.SessionTimeLeft = data.SessionTimeLeft;
        this.TotalLaps = data.TotalLaps;
        this.TrackName = data.TrackCode; // full track name: track name + configuration
        this.TrackLength = data.TrackLength;
        this.TyrePressureFrontLeft = data.TyrePressureFrontLeft;

        this._rawObject = data.GetRawDataObject();

        switch (this._rawObject) {
            case ACCRawData accRawData:
                this.SessionType = SessionTypeExtensions.FromGameData(accRawData);
                if (accRawData.Realtime is not null) {
                    this.SessionPhase = SessionPhaseExtensions.FromGameData(accRawData.Realtime);
                    this.SessionIndex = accRawData.Realtime.SessionIndex;
                    this.FocusedCarId = accRawData.Realtime?.FocusedCarIndex.ToString();
                }

                break;
            case ACRawData acRawData:
                this.SessionType = SessionTypeExtensions.FromGameData(acRawData);
                break;
            case WrapV2 rf2RawData:
                this.SessionType = SessionTypeExtensions.FromString(data.SessionTypeName);
                this.SessionPhase = SessionPhaseExtensions.FromGameData(rf2RawData);
                break;
            case Shared r3ERawData:
                this.SessionType = SessionTypeExtensions.FromString(data.SessionTypeName);
                this.SessionPhase = SessionPhaseExtensions.FromGameData(r3ERawData);
                break;
            case AMS2APIStruct ams2RawData:
                this.SessionType = SessionTypeExtensions.FromGameData(ams2RawData);
                this.SessionPhase = SessionPhaseExtensions.FromGameData(ams2RawData);
                break;
            default:
                this.SessionType = SessionTypeExtensions.FromString(data.SessionTypeName);
                break;
        }
    }

    public object? GetRawDataObject() {
        return this._rawObject;
    }
}