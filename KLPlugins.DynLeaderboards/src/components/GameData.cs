using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KLPlugins.DynLeaderboards.Car;

using ShGameReaderCommon = GameReaderCommon;
using ShAcc = ACSharedMemory.ACC;
using ShAccBroadcasting = ksBroadcastingNetwork;
using ShAc = ACSharedMemory;
using ShAms2 = PCarsSharedMemory.AMS2;
using ShR3E = R3E;
using ShRf2Data = CrewChiefV4.rFactor2_V2.rFactor2Data;

namespace KLPlugins.DynLeaderboards;

internal class GameData {
    internal GameDataBase _OldData;
    internal GameDataBase _NewData;

    public GameData(ShGameReaderCommon.StatusDataBase oldData, ShGameReaderCommon.StatusDataBase newData) {
        this._OldData = new GameDataBase(oldData);
        this._NewData = new GameDataBase(newData);
    }

    public GameData(GameDataBase oldData, GameDataBase newData) {
        this._OldData = oldData;
        this._NewData = newData;
    }

    internal void Update(ShGameReaderCommon.StatusDataBase data) {
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
    public List<ShGameReaderCommon.Opponent> Opponents { get; private set; }
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

    internal GameDataBase(ShGameReaderCommon.StatusDataBase data) {
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
            case ShAcc.Reader.ACCRawData accRawData:
                this.SessionType = SessionTypeExtensions.FromGameData(accRawData);
                if (accRawData.Realtime is not null) {
                    this.SessionPhase = SessionPhaseExtensions.FromGameData(accRawData.Realtime);
                    this.SessionIndex = accRawData.Realtime.SessionIndex;
                    this.FocusedCarId = accRawData.Realtime?.FocusedCarIndex.ToString();
                }

                break;
            case ShAc.Reader.ACRawData acRawData:
                this.SessionType = SessionTypeExtensions.FromGameData(acRawData);
                break;
            case RfactorReader.RF2.WrapV2 rf2RawData:
                this.SessionType = SessionTypeExtensions.FromString(data.SessionTypeName);
                this.SessionPhase = SessionPhaseExtensions.FromGameData(rf2RawData);
                break;
            case ShR3E.Data.Shared r3ERawData:
                this.SessionType = SessionTypeExtensions.FromString(data.SessionTypeName);
                this.SessionPhase = SessionPhaseExtensions.FromGameData(r3ERawData);
                break;
            case ShAms2.Models.AMS2APIStruct ams2RawData:
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

internal class OpponentExtra {
    public bool IsCurrentLapValid { get; private set; }
    public bool IsCurrentLapOutLap { get; private set; } = false;
    public bool IsCurrentLapInLap { get; private set; } = false;
    public bool IsCurrentLapTimeReset { get; private set; } = false;
    public TimeSpan? CurrentLapTime { get; private set; }
    private TimeSpan? _prevCurrentLapTime { get; set; }
    public FinishStatus FinishStatus { get; private set; } = FinishStatus.UNKNWOWN;
    public CarLocation Location { get; private set; }

    private ShGameReaderCommon.Opponent? _rawOldOpponent = null;


    public OpponentExtra(GameData data, ShGameReaderCommon.Opponent opponent) {
        this.Update(data, opponent);
    }

    private OpponentExtra() { }

    public OpponentExtra Clone() {
        return new OpponentExtra { IsCurrentLapValid = this.IsCurrentLapValid, FinishStatus = this.FinishStatus };
    }

    public void Update(GameData data, ShGameReaderCommon.Opponent opponent) {
        this.IsCurrentLapValid = opponent.LapValid;
        this.FinishStatus = FinishStatus.UNKNWOWN;
        this._prevCurrentLapTime = this.CurrentLapTime;
        if (opponent.CurrentLapTime != null) {
            this.CurrentLapTime = opponent.CurrentLapTime.Value;
        } else if (opponent.GuessedLapStartTime != null) {
            // R3E sets current lap time to zero immediately after the lap is invalidated,
            // but we can calculate it our selves.
            //
            // However this can also be used generally.
            this.CurrentLapTime = DynLeaderboardsPlugin._UpdateTime - opponent.GuessedLapStartTime.Value;
        } else {
            this.CurrentLapTime = null;
        }

        this.IsCurrentLapTimeReset = this.CurrentLapTime < TimeSpan.FromSeconds(1)
            && this._prevCurrentLapTime > this.CurrentLapTime;

        if (opponent.IsCarInPit) {
            this.Location = CarLocation.PIT_BOX;
        } else if (opponent.IsCarInPitLane) {
            this.Location = CarLocation.PIT_LANE;
        } else {
            this.Location = CarLocation.TRACK;
        }

        // Game specific overrides
        if (opponent is ShAc.Models.ACCOpponent accOpponent) {
            this.Update(accOpponent);
        } else if (opponent.ExtraData.FirstOrDefault() is ShRf2Data.rF2VehicleScoring rf2Opponent) {
            this.Update(rf2Opponent);
        } else {
            switch (data._NewData.GetRawDataObject()) {
                case ShAms2.Models.AMS2APIStruct ams2RawData: {
                    var index = -1;
                    for (var j = 0; j < ams2RawData.mNumParticipants; j++) {
                        var participantData = ams2RawData.mParticipantData[j];
                        var name = ShAms2.Models.PC2Helper.getNameFromBytes(participantData.mName);
                        if (name == opponent.Id) {
                            index = j;
                            break;
                        }
                    }

                    if (index != -1) {
                        this.Update(opponent, ams2RawData, index);
                    }
                }
                    break;
                case ShR3E.Data.Shared r3ERawData: {
                    static string? GetName(byte[]? data) {
                        return data == null ? null : Encoding.UTF8.GetString(data).Split(default(char))[0];
                    }

                    for (var j = 0; j < r3ERawData.NumCars; j++) {
                        ref readonly var participantData = ref r3ERawData.DriverData[j];
                        var name = GetName(participantData.DriverInfo.Name);
                        if (!string.IsNullOrEmpty(name) && name == opponent.Id) {
                            this.Update(opponent, participantData);
                            break;
                        }
                    }
                }
                    break;
                default:
                    break;
            }
        }

        this._rawOldOpponent = opponent;
    }

    public void Update(ShAc.Models.ACCOpponent accOpponent) {
        var currentLap = accOpponent.ExtraData.CurrentLap;
        this.IsCurrentLapValid &= currentLap.IsValidForBest && !currentLap.IsInvalid;
        this.IsCurrentLapOutLap = currentLap.Type == ShAccBroadcasting.LapType.Outlap;
        this.IsCurrentLapInLap = currentLap.Type == ShAccBroadcasting.LapType.Inlap;

        var location = accOpponent.ExtraData.CarLocation;
        this.Location = location switch {
            ShAccBroadcasting.CarLocationEnum.Track
                or ShAccBroadcasting.CarLocationEnum.PitEntry
                or ShAccBroadcasting.CarLocationEnum.PitExit => CarLocation.TRACK,

            ShAccBroadcasting.CarLocationEnum.Pitlane => CarLocation.PIT_LANE,
            _ => CarLocation.NONE,
        };
    }

    public void Update(ShRf2Data.rF2VehicleScoring rf2Opponent) {
        // Rf2 doesn't directly export lap validity. But when one exceeds track limits the current sector times are 
        // set to -1.0. We cannot immediately detect the cut in the first sector, but as soon as we reach the 
        // 2nd sector we can detect it, when current sector 1 time is still -1.0.
        this.IsCurrentLapValid &= !(rf2Opponent.mSector is 2 or 0 && rf2Opponent.mCurSector1 == -1.0);
        if (rf2Opponent.mTimeIntoLap > 0) {
            // fall back to SimHub's if rf2 doesn't report current lap time (it's -1 if missing)
            this.CurrentLapTime = TimeSpan.FromSeconds(rf2Opponent.mTimeIntoLap);
        }
    }

    public void Update(
        ShGameReaderCommon.Opponent opponent,
        ShAms2.Models.AMS2APIStruct ams2RawData,
        int index
    ) {
        this.IsCurrentLapValid &= !Convert.ToBoolean(ams2RawData.mLapsInvalidated[index]);
        // in AMS2 lap time goes briefly to null on lap time reset
        this.IsCurrentLapTimeReset &= this._prevCurrentLapTime != null && opponent.CurrentLapTime == null;
    }

    public void Update(ShGameReaderCommon.Opponent opponent, ShR3E.Data.DriverData r3EOpponent) {
        this.IsCurrentLapValid &= r3EOpponent.CurrentLapValid != 0;
        this.FinishStatus = (FinishStatus)r3EOpponent.FinishStatus;
        var oldRawCurrentLapTime = this._rawOldOpponent?.CurrentLapTime;
        var newrawCurrentLapTime = opponent.CurrentLapTime;
        // in R3E lap time on invalid lap is shown as TimeSpan.Zero,
        this.IsCurrentLapTimeReset &= (oldRawCurrentLapTime is null || oldRawCurrentLapTime == TimeSpan.Zero)
            && newrawCurrentLapTime is not null
            && newrawCurrentLapTime != TimeSpan.Zero;
    }
}

internal enum FinishStatus {
    UNKNWOWN = 0,
    NONE = 1,
    FINISHED = 2,
    DNF = 3,
    DNQ = 4,
    DNS = 5,
    DQ = 6,
}