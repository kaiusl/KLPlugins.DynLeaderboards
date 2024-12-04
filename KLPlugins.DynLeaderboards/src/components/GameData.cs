using System;
using System.Collections.Generic;
using System.Linq;

using ACSharedMemory.Models;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.AccBroadcastingNetwork;

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

    internal GameDataBase(AccBroadcastingRawData data) {
        this.AirTemperature = data._RealtimeUpdate.AmbientTemp;
        this.Opponents = data._RealtimeCarUpdates.Select(
                Opponent (carUpdate) => {
                    var (car, name, cls) = carUpdate;
                    var driverInfo = car.CarEntry.Drivers[car.DriverIndex];

                    return new ACCOpponent {
                        Id = car.CarIndex.ToString(),
                        Coordinates = new double[3],
                        Name = driverInfo.FirstName + " " + driverInfo.LastName,
                        TeamName = car.CarEntry.TeamName,
                        Initials = driverInfo.ShortName,
                        Position = car.Position,
                        LivePosition = car.LivePosition,
                        IsPlayer = false,
                        BestLapTime = TimeSpan.FromMilliseconds(car.BestSessionLap.LaptimeMS.GetValueOrDefault()),
                        LastLapTime = TimeSpan.FromMilliseconds(car.LastLap.LaptimeMS.GetValueOrDefault()),
                        CurrentLap = car.Laps + 1,
                        TrackPositionPercent = car.SplinePosition, // this.GetTrackPositionPercent(i.Value),
                        CurrentLapHighPrecision = car.Laps + car.SplinePosition,
                        IsCarInPit = car.CarLocation.ToString().ToLower().Contains("pit"),
                        IsCarInPitLane = car.CarLocation.ToString().ToLower().Contains("pit"),
                        Speed = car.Kmh,
                        CarNumber = car.CarEntry.RaceNumber.ToString(),
                        CarName = name,
                        CarClass = cls.AsString(),
                        BestLapSectorTimes =
                            SectorTimes.FromMillisecondsSplits(
                                car.BestSessionLap?.Splits?.Select((Func<int?, double?>)(j => j)).ToArray()
                                ?? new double?[0]
                            ),
                        LastLapSectorTimes =
                            SectorTimes.FromMillisecondsSplits(
                                car.LastLap?.Splits?.Select((Func<int?, double?>)(j => j)).ToArray()
                                ?? new double?[0]
                            ),
                        CurrentLapSectorTimes = new SectorTimes(),
                        CurrentLapTime = TimeSpan.FromMilliseconds(car.CurrentLap.LaptimeMS.GetValueOrDefault()),
                        ExtraData = car,
                    };
                }
            )
            .ToList();
        this.RemainingLaps = -1;
        this.SessionTimeLeft = data._RealtimeUpdate.SessionRemainingTime;
        this.SessionTypeName = data._RealtimeUpdate.SessionType.ToString();
        this.TotalLaps = -1;
        this._trackConfig = "";
        this.TrackName = data._TrackData?.TrackName ?? "";
        this.TrackLength = data._TrackData?.TrackMeters ?? 0;
        this.TyrePressureFrontLeft = -1.0;

        this._rawObject = data;
    }

    public object? GetRawDataObject() {
        return this._rawObject;
    }
}