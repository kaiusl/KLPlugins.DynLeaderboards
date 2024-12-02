using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Track;

using ShAccBroadcasting = ksBroadcastingNetwork;
using ShAms2 = PCarsSharedMemory.AMS2;
using ShR3E = R3E;
using ShRf2 = CrewChiefV4.rFactor2_V2.rFactor2Data;

namespace KLPlugins.DynLeaderboards.Car {
    public sealed class CarData {
        public CarClass CarClass { get; private set; }
        public string CarClassShortName { get; private set; } = null!;
        public TextBoxColor CarClassColor { get; private set; } = null!;

        // string because 001 and 1 could be different numbers in some games
        public string? CarNumberAsString { get; private set; } = null;

        public int? CarNumberAsInt { get; private set; } = null;

        /// <summary>
        ///     Pretty car model name.
        /// </summary>
        public string CarModel { get; private set; } = null!;

        public string CarManufacturer { get; private set; } = null!;
        public string? TeamName { get; private set; }
        public TeamCupCategory TeamCupCategory { get; private set; }
        public TextBoxColor TeamCupCategoryColor { get; private set; } = null!;

        public NewOld<CarLocation> Location { get; } = new(CarLocation.NONE);

        public NewOld<int> Laps { get; } = new(0);
        public bool IsNewLap { get; private set; } = false;
        public TimeSpan CurrentLapTime { get; private set; }
        public bool IsCurrentLapOutLap { get; private set; }
        public bool IsCurrentLapInLap { get; private set; }
        public bool IsCurrentLapValid { get; private set; }
        private bool _isLastLapOutLap { get; set; }
        private bool _isLastLapInLap { get; set; }
        private bool _isLastLapValid { get; set; }
        public Lap? LastLap { get; private set; }
        public Lap? BestLap { get; private set; }
        public Sectors BestSectors { get; } = new();

        public bool IsBestLapCarOverall { get; private set; }
        public bool IsBestLapCarInClass { get; private set; }
        public bool IsBestLapCarInCup { get; private set; }

        public bool IsFocused { get; private set; }

        /// <summary>
        ///     List of all drivers. Current driver is always the first.
        /// </summary>
        public ReadOnlyCollection<Driver> Drivers { get; }

        private List<Driver> _drivers { get; } = [];
        public Driver? CurrentDriver => this._drivers.FirstOrDefault();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int PositionInCup { get; private set; }
        public int? PositionOverallStart { get; private set; }
        public int? PositionInClassStart { get; private set; }
        public int? PositionInCupStart { get; private set; }

        /// <summary>
        ///     Index of this car in Values.OverallOrder.
        /// </summary>
        public int IndexOverall { get; private set; }

        /// <summary>
        ///     Index of this car in Values.ClassOrder.
        /// </summary>
        public int IndexClass { get; private set; }

        public int IndexCup { get; private set; }
        public bool IsInPitLane { get; private set; }
        public bool ExitedPitLane { get; private set; }
        public bool EnteredPitLane { get; private set; }
        public int PitCount { get; private set; }
        public DateTime? PitEntryTime { get; private set; }
        public TimeSpan? PitTimeLast { get; private set; }
        public TimeSpan TotalPitTime { get; private set; }
        public TimeSpan? PitTimeCurrent { get; private set; }

        public TimeSpan? GapToLeader { get; private set; }
        public TimeSpan? GapToClassLeader { get; private set; }
        public TimeSpan? GapToFocusedTotal { get; private set; }
        public TimeSpan? GapToFocusedOnTrack { get; private set; }
        public TimeSpan? GapToAheadOnTrack { get; private set; }
        public TimeSpan? GapToCupLeader { get; private set; }
        public TimeSpan? GapToAhead { get; private set; }
        public TimeSpan? GapToAheadInClass { get; private set; }
        public TimeSpan? GapToAheadInCup { get; private set; }
        public RelativeLapDiff RelativeOnTrackLapDiff { get; private set; }

        /// <summary>
        ///     In range <c>[0, 1]</c>.
        /// </summary>
        public double SplinePosition { get; private set; }

        /// <summary>
        ///     <c>&gt; 0</c> if ahead, <c>&lt; 0</c> if behind. Is in range <c>[-0.5, 0.5]</c>.
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }

        public double TotalSplinePosition { get; private set; } = 0.0;

        public bool JumpedToPits { get; private set; } = false;

        /// <summary>
        ///     Has the car crossed the start line at race start.
        /// </summary>
        internal bool _HasCrossedStartLine { get; private set; } = true;

        private bool _isHasCrossedStartLineSet = false;
        public bool IsFinished { get; private set; } = false;
        internal DateTime? _FinishTime { get; private set; } = null;

        public int? CurrentStintLaps { get; private set; }
        public TimeSpan? LastStintTime { get; private set; }
        public TimeSpan? CurrentStintTime { get; private set; }
        public int? LastStintLaps { get; private set; }
        private DateTime? _stintStartTime;

        public double MaxSpeed { get; private set; } = 0.0;
        public bool IsConnected { get; private set; }

        internal int _MissedUpdates = 0;

        /// <summary>
        ///     Car ID.
        ///     In AC, it's the drivers name.
        ///     In ACC single player, it's number from 0.
        ///     In ACC multiplayer, it's number from 1000.
        /// </summary>
        internal string _Id => this._RawDataNew.Id;

        /// <summary>
        ///     Has this car received the update in latest data update.
        /// </summary>
        internal bool _IsUpdated { get; set; } = true;

        internal Opponent _RawDataNew;
        internal Opponent _RawDataOld;

        // In some games the spline position and the lap counter reset at different locations.
        // Since we use total spline position to order the cars on track, we need them to be in sync
        internal enum OffsetLapUpdateType {
            NONE = 0,
            LAP_BEFORE_SPLINE = 1,
            SPLINE_BEFORE_LAP = 2,
        }

        internal OffsetLapUpdateType _OffsetLapUpdate { get; private set; } = OffsetLapUpdateType.NONE;

        private int _lapAtOffsetLapUpdate = -1;
        private bool _isSplinePositionReset = false;

        /// <summary>
        ///     To indicate that the gap between this car and some other is more than a lap,
        ///     we add <c>_LAP_GAP_VALUE</c> to the gap in laps.
        /// </summary>
        private static readonly TimeSpan _lapGapValue = TimeSpan.FromSeconds(100_000);

        private static readonly TimeSpan _halfLapGapValue =
            TimeSpan.FromSeconds(CarData._lapGapValue.TotalSeconds / 2);

        private readonly Dictionary<CarClass, TimeSpan?> _splinePositionTimes = [];

        private bool _expectingNewLap = false;

        internal List<double> _LapDataPos { get; } = [];
        internal List<double> _LapDataTime { get; } = [];
        internal bool _LapDataValidForSave { get; private set; } = false;

        internal CarData(Values values, string? focusedCarId, Opponent opponent, GameData gameData) {
            this.Drivers = this._drivers.AsReadOnly();

            this._RawDataNew = opponent;
            this._RawDataOld = opponent;

            this.SetStaticCarData(values, opponent);

            this.IsCurrentLapValid = true;

            this.PositionOverall = this._RawDataNew.Position;
            this.PositionInClass = this._RawDataNew.PositionInClass;
            this.PositionInCup = this.PositionInClass;
            this.UpdateIndependent(values, focusedCarId, opponent, gameData);

            this.CheckGameLapTimes(opponent);
        }

        private void CheckGameLapTimes(Opponent opponent) {
            if (DynLeaderboardsPlugin._Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)opponent;

                var lastLap = accRawData.ExtraData.LastLap;
                if (lastLap != null && lastLap.LaptimeMS != null) {
                    var driverRaw = accRawData.ExtraData.CarEntry.Drivers[lastLap.DriverIndex];
                    var driver = this.Drivers.First(
                        d => d.FirstName == driverRaw.FirstName && d.LastName == driverRaw.LastName
                    );
                    this.LastLap = new Lap(
                        lastLap,
                        this.Laps.New - 1,
                        driver
                    );
                }

                var bestLap = accRawData.ExtraData.BestSessionLap;
                if (bestLap != null && bestLap.LaptimeMS != null && bestLap.IsValidForBest) {
                    var driverRaw = accRawData.ExtraData.CarEntry.Drivers[bestLap.DriverIndex];
                    var driver = this.Drivers.First(
                        d => d.FirstName == driverRaw.FirstName && d.LastName == driverRaw.LastName
                    );
                    this.BestLap = new Lap(
                        bestLap,
                        this.Laps.New - 1, // We don't know the exact number, but say it was last lap
                        driver
                    );
                    driver.BestLap = this.BestLap;
                }
            } else if (DynLeaderboardsPlugin._Game.IsRf2OrLmu
                && opponent.ExtraData.ElementAtOr(0, null) is
                    CrewChiefV4.rFactor2_V2.rFactor2Data.rF2VehicleScoring
                    rf2RawData) {
                {
                    // Rf2 raw values are -1 if lap time is missing
                    var bestLapTime = rf2RawData.mBestLapTime;
                    var bestLapS1 = rf2RawData.mBestLapSector1;
                    var bestLapS2 = rf2RawData.mBestLapSector2 - (bestLapS1 > 0 ? bestLapS1 : 0);
                    var bestLapS3 = bestLapTime - (rf2RawData.mBestLapSector2 > 0 ? rf2RawData.mBestLapSector2 : 0);

                    // Only add sector times if all of them are known, this avoids weird sector times
                    if (bestLapS1 <= 0 || bestLapS2 <= 0 || bestLapS3 <= 0) {
                        bestLapS1 = -1;
                        bestLapS2 = -1;
                        bestLapS3 = -1;
                    }

                    if (bestLapTime > 0) {
                        this.BestLap = new Lap(
                            lapTime: TimeSpan.FromSeconds(bestLapTime),
                            s1: bestLapS1 > 0 ? TimeSpan.FromSeconds(bestLapS1) : null,
                            s2: bestLapS2 > 0 ? TimeSpan.FromSeconds(bestLapS2) : null,
                            s3: bestLapS3 > 0 ? TimeSpan.FromSeconds(bestLapS3) : null,
                            this.Laps.New - 1,
                            this.CurrentDriver! // it's our best guess
                        );
                        this.CurrentDriver!.BestLap = this.BestLap;
                    }
                }
                {
                    var lastLapTime = rf2RawData.mLastLapTime;
                    var lastLapS1 = rf2RawData.mLastSector1;
                    var lastLapS2 = rf2RawData.mLastSector2 - (lastLapS1 > 0 ? lastLapS1 : 0);
                    var lastLapS3 = lastLapTime - (rf2RawData.mLastSector2 > 0 ? rf2RawData.mLastSector2 : 0);

                    if (lastLapS1 <= 0 || lastLapS2 <= 0 || lastLapS3 <= 0) {
                        lastLapS1 = -1;
                        lastLapS2 = -1;
                        lastLapS3 = -1;
                    }

                    if (lastLapTime > 0) {
                        this.LastLap = new Lap(
                            lapTime: TimeSpan.FromSeconds(lastLapTime),
                            s1: lastLapS1 > 0 ? TimeSpan.FromSeconds(lastLapS1) : null,
                            s2: lastLapS2 > 0 ? TimeSpan.FromSeconds(lastLapS2) : null,
                            s3: lastLapS3 > 0 ? TimeSpan.FromSeconds(lastLapS3) : null,
                            this.Laps.New - 1,
                            this.CurrentDriver!
                        );
                    } else if (this.BestLap != null)
                        // sometimes last lap can be missing, but best lap is not. In that case, use best lap for consistent lap times.
                    {
                        this.LastLap = this.BestLap;
                    }
                }
            } else {
                var lastLap = opponent.LastLapSectorTimes;
                if (lastLap != null) {
                    this.LastLap = new Lap(
                        lastLap,
                        opponent.LastLapTime,
                        this.Laps.New - 1,
                        this.CurrentDriver!
                    );
                }

                var bestLap = opponent.BestLapSectorTimes;
                if (bestLap != null) {
                    this.BestLap = new Lap(
                        bestLap,
                        opponent.BestLapTime,
                        this.Laps.New - 1,
                        this.CurrentDriver!
                    );
                }
            }
        }

        private void SetStaticCarData(Values values, Opponent opponent) {
            var rawClass = CarClass.TryNew(this._RawDataNew.CarClass) ?? CarClass.Default;
            OverridableCarInfo? carInfo = null;
            if (this._RawDataNew.CarName != null) {
                carInfo = DynLeaderboardsPlugin._Settings.Infos.CarInfos.GetOrAdd(
                    this._RawDataNew.CarName,
                    rawClass,
                    rawClass
                );
            }

            this.SetStaticCarInfo(carInfo);

            var (cls, classInfo) =
                DynLeaderboardsPlugin._Settings.Infos.ClassInfos.GetFollowReplaceWith(carInfo?.Class ?? rawClass);
            this.SetStaticClassInfo(cls, classInfo);

            this.CarNumberAsString = this._RawDataNew.CarNumber;
            if (int.TryParse(this.CarNumberAsString, out var number)) {
                this.CarNumberAsInt = number;
            }

            this.TeamName = this._RawDataNew.TeamName;

            if (DynLeaderboardsPlugin._Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)opponent;
                this.TeamCupCategory = CarData.AccTeamCupCategoryToString(accRawData.ExtraData.CarEntry.CupCategory);
            } else {
                this.TeamCupCategory = TeamCupCategory.Default;
            }

            this.SetTeamCupColors(values);
        }

        internal void UpdateCarInfos(Values values) {
            var rawClass = CarClass.TryNew(this._RawDataNew.CarClass) ?? CarClass.Default;
            OverridableCarInfo? carInfo = null;
            if (this._RawDataNew.CarName != null) {
                carInfo = DynLeaderboardsPlugin._Settings.Infos.CarInfos.GetOrAdd(
                    this._RawDataNew.CarName,
                    rawClass,
                    rawClass
                );
            }

            this.SetStaticCarInfo(carInfo);

            var (cls, classInfo) =
                DynLeaderboardsPlugin._Settings.Infos.ClassInfos.GetFollowReplaceWith(carInfo?.Class ?? rawClass);
            this.SetStaticClassInfo(cls, classInfo);
        }

        internal void UpdateClassInfos(Values values) {
            var rawClass = CarClass.TryNew(this._RawDataNew.CarClass) ?? CarClass.Default;
            OverridableCarInfo? carInfo = null;
            if (this._RawDataNew.CarName != null) {
                carInfo = DynLeaderboardsPlugin._Settings.Infos.CarInfos.GetOrAdd(
                    this._RawDataNew.CarName,
                    rawClass,
                    rawClass
                );
            }

            var (cls, classInfo) =
                DynLeaderboardsPlugin._Settings.Infos.ClassInfos.GetFollowReplaceWith(carInfo?.Class ?? rawClass);
            this.SetStaticClassInfo(cls, classInfo);
        }

        private void SetStaticCarInfo(OverridableCarInfo? carInfo) {
            this.CarModel = carInfo?.Name ?? this._RawDataNew.CarName ?? "Unknown";
            this.CarManufacturer = carInfo?.Manufacturer ?? CarData.GetCarManufacturer(this.CarModel);
        }


        private void SetStaticClassInfo(CarClass cls, OverridableClassInfo classInfo) {
            this.CarClass = cls;
            this.CarClassShortName = classInfo.ShortName ?? cls.AsString();

            this.CarClassColor = new TextBoxColor(
                fg: classInfo.Foreground ?? this._RawDataNew.CarClassTextColor ?? TextBoxColor.DEF_FG,
                bg: classInfo.Background ?? this._RawDataNew.CarClassColor ?? TextBoxColor.DEF_BG
            );
        }

        private void SetTeamCupColors(Values values) {
            var cupColor = DynLeaderboardsPlugin._Settings.Infos.TeamCupCategoryColors.GetOrAdd(this.TeamCupCategory);
            this.TeamCupCategoryColor = new TextBoxColor(
                fg: cupColor.Foreground ?? TextBoxColor.DEF_FG,
                bg: cupColor.Background ?? TextBoxColor.DEF_BG
            );
        }

        internal void UpdateTeamCupInfos(Values values) {
            this.SetTeamCupColors(values);
        }

        internal void UpdateDriverInfos(Values values) {
            foreach (var driver in this.Drivers) {
                driver.UpdateDriverInfos(values);
            }
        }

        private static TeamCupCategory AccTeamCupCategoryToString(byte cupCategory) {
            return cupCategory switch {
                0 => new TeamCupCategory("Overall"),
                1 => new TeamCupCategory("ProAm"),
                2 => new TeamCupCategory("Am"),
                3 => new TeamCupCategory("Silver"),
                4 => new TeamCupCategory("National"),
                _ => TeamCupCategory.Default,
            };
        }

        private static string GetCarManufacturer(string? carModel) {
            // TODO: read from LUTs
            return carModel?.Split(' ')[0] ?? "Unknown";
        }

        private AMS2.RawOpponentData? _rawAms2DataNew { get; set; }

        private R3E.RawOpponentData? _rawR3EDataNew { get; set; }

        /// <summary>
        ///     Update data that is independent of other cars' data.
        /// </summary>
        internal void UpdateIndependent(Values values, string? focusedCarId, Opponent rawData, GameData gameData) {
            if (rawData.TrackPositionPercent == null)
                // This happens occasionally. Just wait for the next update.
            {
                return;
            }
            // Clear old data

            // Needs to be cleared before UpdateDependsOnOthers, 
            // so that none of the cars have old data in it when we set gaps
            this._splinePositionTimes.Clear();

            // Actual update
            // In ACC the cars remain in opponents list even if they disconnect, 
            this.IsConnected = rawData.IsConnected
                && (!DynLeaderboardsPlugin._Game.IsAcc || rawData.Coordinates != null);
            // however, it's coordinates will be null then 
            if (!this.IsConnected) {
                return;
            }

            this._MissedUpdates = 0;

            this._RawDataOld = this._RawDataNew;
            this._RawDataNew = rawData;
            this._IsUpdated = true;

            if (DynLeaderboardsPlugin._Game.IsAms2) {
                if (gameData.NewData.GetRawDataObject() is ShAms2.Models.AMS2APIStruct rawAms2data) {
                    var index = -1;
                    for (var i = 0; i < rawAms2data.mNumParticipants; i++) {
                        var participantData = rawAms2data.mParticipantData[i];
                        var name = ShAms2.Models.PC2Helper.getNameFromBytes(participantData.mName);
                        if (name == this._Id) {
                            index = i;
                            break;
                        }
                    }

                    if (index != -1) {
                        this._rawAms2DataNew = new AMS2.RawOpponentData(
                            raceState: Convert.ToInt32(rawAms2data.mRaceStates[index]),
                            isCurrentLapInvalidated: Convert.ToBoolean(rawAms2data.mLapsInvalidated[index])
                        );
                    } else {
                        this._rawAms2DataNew = null;
                    }
                }
            } else if (DynLeaderboardsPlugin._Game.IsR3E) {
                static string? GetName(byte[]? data) {
                    if (data == null) {
                        return null;
                    }

                    return Encoding.UTF8.GetString(data).Split(default(char))[0];
                }

                if (gameData.NewData.GetRawDataObject() is ShR3E.Data.Shared rawR3EData) {
                    var index = -1;
                    ref readonly var participantData = ref rawR3EData.DriverData[0];
                    for (var i = 0; i < rawR3EData.NumCars; i++) {
                        participantData = ref rawR3EData.DriverData[i];
                        var name = GetName(participantData.DriverInfo.Name);
                        if (!string.IsNullOrEmpty(name) && name == this._Id) {
                            index = i;
                            this._rawR3EDataNew = new R3E.RawOpponentData(in participantData);
                            break;
                        }
                    }

                    if (index == -1) {
                        this._rawR3EDataNew = null;
                    }
                }
            }

            if (DynLeaderboardsPlugin._Game.IsAcc && focusedCarId != null) {
                this.IsFocused = this._Id == focusedCarId;
            } else {
                this.IsFocused = this._RawDataNew.IsPlayer;
            }

            this.IsBestLapCarOverall = false;
            this.IsBestLapCarInClass = false;
            this.IsBestLapCarInCup = false;

            this.Laps.Update((this._RawDataNew.CurrentLap ?? 1) - 1);
            this.IsNewLap = this.Laps.New - this.Laps.Old == 1;

            this.SetCarLocation(rawData);
            this.UpdatePitInfo(values.Session.SessionPhase);
            this.SetSplinePositions(values);
            this.MaxSpeed = Math.Max(this.MaxSpeed, this._RawDataNew.Speed ?? 0.0);
            this.UpdateDrivers(values, rawData);

            if (this.IsNewLap) {
                Debug.Assert(
                    this.CurrentDriver != null,
                    "Current driver shouldn't be null since someone had to finish this lap."
                );
                var currentDriver = this.CurrentDriver!;
                currentDriver.TotalLaps += 1;
                this._expectingNewLap = true;
                this._isLastLapInLap = this.IsCurrentLapInLap;
                this._isLastLapOutLap = this.IsCurrentLapOutLap;
                this._isLastLapValid = this.IsCurrentLapValid;

                this.IsCurrentLapValid = true;
                this.IsCurrentLapOutLap = false;
                this.IsCurrentLapInLap = false;
            }

            if (DynLeaderboardsPlugin._Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.Models.ACCOpponent)this._RawDataNew;
                var rawDataOld = (ACSharedMemory.Models.ACCOpponent)this._RawDataOld;

                if (!this.IsNewLap
                    && rawDataNew.ExtraData.CurrentLap.LaptimeMS < rawDataOld.ExtraData.CurrentLap.LaptimeMS) {
                    this._isLastLapInLap = this.IsCurrentLapInLap;
                    this._isLastLapOutLap = this.IsCurrentLapOutLap;
                    this._isLastLapValid = this.IsCurrentLapValid;

                    this.IsCurrentLapValid = true;
                    this.IsCurrentLapOutLap = false;
                    this.IsCurrentLapInLap = false;
                }

                if (!this.IsCurrentLapOutLap
                    && rawDataNew.ExtraData.CurrentLap.Type == ShAccBroadcasting.LapType.Outlap) {
                    this.IsCurrentLapOutLap = true;
                }

                if (!this.IsCurrentLapInLap
                    && rawDataNew.ExtraData.CurrentLap.Type == ShAccBroadcasting.LapType.Inlap) {
                    this.IsCurrentLapInLap = true;
                }
            } else if (DynLeaderboardsPlugin._Game.IsAms2) {
                if (!this.IsNewLap
                    && (this._RawDataNew.CurrentLapTime == null
                        || this._RawDataNew.CurrentLapTime < this._RawDataOld.CurrentLapTime)) {
                    this._isLastLapInLap = this.IsCurrentLapInLap;
                    this._isLastLapOutLap = this.IsCurrentLapOutLap;
                    this._isLastLapValid = this.IsCurrentLapValid;

                    this.IsCurrentLapValid = true;
                    this.IsCurrentLapOutLap = false;
                    this.IsCurrentLapInLap = false;
                }
            }

            if (this.IsCurrentLapValid) {
                this.CheckIfLapInvalidated(rawData);

                if (!this.IsCurrentLapValid) {
                    Logging.LogInfo($"Invalidated lap #{this.CarNumberAsString} [{this._Id}]");
                }
            }

            this.UpdateLapTimes(values.Session);

            if (values.Session.IsRace) {
                this.HandleJumpToPits(values.Session.SessionType);
                this.CheckForCrossingStartLine(values.Session.SessionPhase);
            }

            this.UpdateStintInfo(values.Session);
            if (values.Session.IsRace && !DynLeaderboardsPlugin._Game.IsAms2) {
                this.HandleOffsetLapUpdates();
            }

            if (this._LapDataValidForSave
                && (this.IsCurrentLapInLap || this.IsCurrentLapOutLap || !this.IsCurrentLapValid || this.IsInPitLane)) {
                this._LapDataValidForSave = false;
                Logging.LogInfo($"Invalidated lap for save #{this.CarNumberAsString} [{this._Id}]");
            }

            if (this._RawDataOld.CurrentLapTime > this._RawDataNew.CurrentLapTime
                // in AMS2 lap time goes briefly to null on lap time reset
                || (DynLeaderboardsPlugin._Game.IsAms2
                    && this._RawDataOld.CurrentLapTime != null
                    && this._RawDataNew.CurrentLapTime == null)
                // in R3E lap time on invalid lap is shown as TimeSpan.Zero,
                // thus above would only trigger on a second valid lap in a row
                || (DynLeaderboardsPlugin._Game.IsR3E
                    && (this._RawDataOld.CurrentLapTime == TimeSpan.Zero || this._RawDataOld.CurrentLapTime == null)
                    && this._RawDataNew.CurrentLapTime != TimeSpan.Zero
                    && this._RawDataNew.CurrentLapTime != null)
            ) {
                if (this._LapDataValidForSave && this._LapDataPos.Count > 20) {
                    // Add last point
                    var pos = this._RawDataOld.TrackPositionPercent;
                    var time = this._RawDataOld.CurrentLapTime?.TotalSeconds;
                    if (pos != null && time != null) {
                        var lastPos = this._LapDataPos.Last();
                        var lastTime = this._LapDataTime.Last();
                        if (lastPos < pos.Value && time.Value - lastTime > PluginSettings.LAP_DATA_TIME_DELAY_SEC) {
                            this._LapDataPos.Add(pos.Value);
                            this._LapDataTime.Add(time.Value);
                        }
                    }

                    values.TrackData?.OnLapFinished(
                        this.CarClass,
                        this._LapDataPos.AsReadOnly(),
                        this._LapDataTime.AsReadOnly()
                    );
                }

                this._LapDataValidForSave = true;
                this._LapDataPos.Clear();
                this._LapDataTime.Clear();
            }

            var rawPos = this._RawDataNew.TrackPositionPercent;
            var rawTime = this._RawDataNew.CurrentLapTime;
            if (this._LapDataValidForSave && rawPos != null && rawTime != null) {
                if (this._LapDataPos.Count == 0) {
                    this._LapDataPos.Add(rawPos.Value);
                    this._LapDataTime.Add(rawTime.Value.TotalSeconds);
                } else {
                    var lastPos = this._LapDataPos.Last();
                    var lastTime = this._LapDataTime.Last();
                    if (lastPos < rawPos.Value
                        && rawTime.Value.TotalSeconds - lastTime > PluginSettings.LAP_DATA_TIME_DELAY_SEC) {
                        this._LapDataPos.Add(rawPos.Value);
                        this._LapDataTime.Add(rawTime.Value.TotalSeconds);
                    }
                }
            }
        }

        private void CheckIfLapInvalidated(Opponent rawData) {
            if (!this._RawDataNew.LapValid) {
                this.IsCurrentLapValid = false;
                return;
            }

            if (DynLeaderboardsPlugin._Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)rawData;
                if (!accRawData.ExtraData.CurrentLap.IsValidForBest || accRawData.ExtraData.CurrentLap.IsInvalid) {
                    this.IsCurrentLapValid = false;
                }
            } else if (DynLeaderboardsPlugin._Game.IsRf2) {
                // Rf2 doesn't directly export lap validity. But when one exceeds track limits the current sector times are 
                // set to -1.0. We cannot immediately detect the cut in the first sector, but as soon as we reach the 
                // 2nd sector we can detect it, when current sector 1 time is still -1.0.
                if (rawData.ExtraData.First() is ShRf2.rF2VehicleScoring rf2RawData) {
                    var curSector = rf2RawData.mSector;
                    if (curSector is 2 or 0 && rf2RawData.mCurSector1 == -1.0) {
                        this.IsCurrentLapValid = false;
                    }
                }
            } else if (DynLeaderboardsPlugin._Game.IsAms2) {
                if (this._rawAms2DataNew != null && this._rawAms2DataNew.IsCurrentLapInvalidated) {
                    this.IsCurrentLapValid = false;
                }
            } else if (DynLeaderboardsPlugin._Game.IsR3E) {
                if (this._rawR3EDataNew != null && !this._rawR3EDataNew._IsCurrentLapValid) {
                    this.IsCurrentLapValid = false;
                }
            }
        }

        /// <summary>
        ///     Requires that this.Laps is already updated.
        /// </summary>
        /// <param name="values"></param>
        /// <exception cref="System.Exception"></exception>
        private void SetSplinePositions(Values values) {
            var newSplinePos = this._RawDataNew.TrackPositionPercent
                ?? throw new Exception("TrackPositionPercent is null");
            newSplinePos += values.TrackData!.SplinePosOffset.Value;
            if (newSplinePos > 1) {
                newSplinePos -= 1;
            }

            this._isSplinePositionReset = newSplinePos < 0.1 && this.SplinePosition > 0.9;
            this.SplinePosition = newSplinePos;
            this.TotalSplinePosition = this.Laps.New + this.SplinePosition;
        }

        private void SetCarLocation(Opponent rawData) {
            if (DynLeaderboardsPlugin._Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)rawData;
                var location = accRawData.ExtraData.CarLocation;
                var newLocation = location switch {
                    ShAccBroadcasting.CarLocationEnum.Track
                        or ShAccBroadcasting.CarLocationEnum.PitEntry
                        or ShAccBroadcasting.CarLocationEnum.PitExit => CarLocation.TRACK,

                    ShAccBroadcasting.CarLocationEnum.Pitlane => CarLocation.PIT_LANE,
                    _ => CarLocation.NONE,
                };
                this.Location.Update(newLocation);
            } else {
                if (this._RawDataNew.IsCarInPit) {
                    this.Location.Update(CarLocation.PIT_BOX);
                } else if (this._RawDataNew.IsCarInPitLane) {
                    this.Location.Update(CarLocation.PIT_LANE);
                } else {
                    this.Location.Update(CarLocation.TRACK);
                }
            }
        }

        /// <summary>
        ///     Requires that this._expectingNewLap is set in this update
        /// </summary>
        private void UpdateLapTimes(Session session) {
            var prevCurrentLapTime = this.CurrentLapTime;
            if (DynLeaderboardsPlugin._Game.IsRf2OrLmu
                && this._RawDataNew.ExtraData.ElementAtOr(0, null) is ShRf2.rF2VehicleScoring rf2RawData
                // fall back to SimHub's if rf2 doesn't report current lap time (it's -1 if missing)
                && rf2RawData.mTimeIntoLap > 0
            ) {
                this.CurrentLapTime = TimeSpan.FromSeconds(rf2RawData.mTimeIntoLap);
            } else if (DynLeaderboardsPlugin._Game.IsR3E
                    && (this._RawDataNew.CurrentLapTime == null || this._RawDataNew.CurrentLapTime == TimeSpan.Zero)
                    && this._RawDataNew.GuessedLapStartTime != null)
                // R3E sets current lap time to zero immediately after the lap is invalidated, but we can calculate it our selves
            {
                this.CurrentLapTime = DateTime.Now - this._RawDataNew.GuessedLapStartTime.Value;
            } else {
                this.CurrentLapTime = this._RawDataNew.CurrentLapTime ?? TimeSpan.Zero;
            }

            // Add the last current lap time as last lap for rF2 if last lap vas invalid. In such case rF2 doesn't provide last lap times
            // and we need to manually calculate it.
            //
            // TODO: this doesn't add sector times, we can potentially calculate those as well
            if (DynLeaderboardsPlugin._Game.IsRf2OrLmu
                && this._expectingNewLap
                && !this._isLastLapValid
                // CurrentLapTime has reset
                && prevCurrentLapTime > this.CurrentLapTime
                && this._RawDataNew.ExtraData.ElementAtOr(0, null) is ShRf2.rF2VehicleScoring rf2RawData2
                // make sure to only use this method if invalid lap was due to lap cut in which case last lap/sector times are -1
                && rf2RawData2.mLastSector1 == -1.0
            ) {
                this.LastLap = new Lap(null, prevCurrentLapTime, this.Laps.New, this.CurrentDriver!) {
                    IsValid = this._isLastLapValid, IsOutLap = this._isLastLapOutLap, IsInLap = this._isLastLapInLap,
                };
                this._expectingNewLap = false;
            } else if (DynLeaderboardsPlugin._Game.IsR3E
                && !session.IsRace
                && !this.IsCurrentLapValid
                && !this.IsNewLap
                // CurrentLapTime has reset
                && prevCurrentLapTime > this.CurrentLapTime
                // at invalidation point the new current lap time can be smaller than previous, due to the different methods to calculate is before and after invalidation
                && this.CurrentLapTime < TimeSpan.FromSeconds(1)
            ) {
                // In non-race sessions, the last laps are not sent by R3E if they are invalid. In such case we need to manually calculate it.
                // Note that this time reset is triggered before new lap. 

                // TODO: this doesn't add sector times, we can potentially calculate those as well
                this.LastLap = new Lap(null, prevCurrentLapTime, this.Laps.New, this.CurrentDriver!) {
                    IsValid = this.IsCurrentLapValid,
                    IsOutLap = this.IsCurrentLapOutLap,
                    IsInLap = this.IsCurrentLapInLap,
                };
                this._expectingNewLap = false;
            } else if (DynLeaderboardsPlugin._Game.IsAcc) {
                // Special case ACC lap time updates.
                // This fixes a bug where in qualy/practice session joining mid-session misses lap invalidation.
                // Thus, possibly setting invalid lap as best lap. The order remains correct (it's set by the position received from ACC)
                // but an invalid lap is shown as a lap.
                // Since the order is supposed to be based on the best lap, this could show weird discrepancy between position and lap time.

                var accRawData = (ACSharedMemory.Models.ACCOpponent)this._RawDataNew;

                // Need to check for new lap time separately since lap update and lap time update may not be in perfect sync
                var lastLap = accRawData.ExtraData.LastLap;
                if (
                    // GetLapTime, GetSectorSplit are relatively expensive, and we don't need to check it every update
                    this._expectingNewLap
                    && lastLap != null
                    && lastLap.LaptimeMS != null
                    && lastLap.LaptimeMS != 0
                    && (this.LastLap == null
                        || lastLap.LaptimeMS != this.LastLap?.Time?.TotalMilliseconds
                        || lastLap.Splits.ElementAtOr(0, null) != this.LastLap?.S1Time?.TotalMilliseconds
                        || lastLap.Splits.ElementAtOr(1, null) != this.LastLap?.S2Time?.TotalMilliseconds
                        || lastLap.Splits.ElementAtOr(2, null) != this.LastLap?.S3Time?.TotalMilliseconds
                    )
                ) {
                    // Lap time end position may be offset with lap or spline position reset point.
                    this.LastLap = new Lap(
                        accRawData.ExtraData.LastLap!,
                        this.Laps.New,
                        this.CurrentDriver!,
                        isValid: this._isLastLapValid,
                        isInLap: this._isLastLapInLap,
                        isOutLap: this._isLastLapOutLap
                    );

                    var bestLap = accRawData.ExtraData.BestSessionLap;
                    if (bestLap != null) {
                        if (this.BestLap?.Time == null
                            || (bestLap.LaptimeMS ?? int.MaxValue) < this.BestLap.Time.Value.TotalMilliseconds) {
                            this.BestLap = new Lap(bestLap, this.Laps.New, this.CurrentDriver!);
                            // If it's car's best lap, it must also be the drivers
                            this.CurrentDriver!.BestLap = this.BestLap;
                        } else if (this.CurrentDriver?.BestLap?.Time == null
                            || (bestLap.LaptimeMS ?? int.MaxValue)
                            < this.CurrentDriver!.BestLap!.Time.Value.TotalMilliseconds) {
                            this.CurrentDriver!.BestLap = new LapBasic(bestLap, this.Laps.New, this.CurrentDriver!);
                        }
                    }

                    this.BestSectors.Update(lastLap);
                    this._expectingNewLap = false;
                }
            } else {
                // Need to check for new lap time separately since lap update and lap time update may not be in perfect sync
                if (this._RawDataNew.LastLapTime != TimeSpan.Zero
                    // Sometimes LastLapTime and LastLapSectorTimes may differ very slightly. Check for both. If both are different then it's new lap.
                    && (this.LastLap?.Time == null
                        || (this._RawDataNew.LastLapTime != this.LastLap?.Time
                            && this._RawDataNew.LastLapSectorTimes?.GetLapTime() != this.LastLap?.Time)
                        || this._RawDataNew.LastLapSectorTimes?.GetSectorSplit(1) != this.LastLap?.S1Time
                        || this._RawDataNew.LastLapSectorTimes?.GetSectorSplit(2) != this.LastLap?.S2Time
                        || this._RawDataNew.LastLapSectorTimes?.GetSectorSplit(3) != this.LastLap?.S3Time
                    )
                ) {
                    // Lap time end position may be offset with lap or spline position reset point.
                    this.LastLap =
                        new Lap(
                            this._RawDataNew.LastLapSectorTimes,
                            this._RawDataNew.LastLapTime,
                            this.Laps.New,
                            this.CurrentDriver!
                        ) {
                            IsValid = this._isLastLapValid,
                            IsOutLap = this._isLastLapOutLap,
                            IsInLap = this._isLastLapInLap,
                        };

                    if (this.LastLap.Time != null
                        && this.LastLap.IsValid
                        && !this.LastLap.IsOutLap
                        && !this.LastLap.IsInLap) {
                        if (this.CurrentDriver?.BestLap?.Time == null
                            || this.LastLap.Time < this.CurrentDriver!.BestLap!.Time) {
                            this.CurrentDriver!.BestLap = this.LastLap;
                        }
                    }

                    this.BestSectors.Update(this._RawDataNew.BestSectorSplits);
                }

                if (this._RawDataNew.BestLapTime != TimeSpan.Zero
                    // Sometimes LastLapTime and LastLapSectorTimes may differ very slightly. Check for both. If both are different then it's new lap.
                    && (this.BestLap?.Time == null
                        || (this._RawDataNew.BestLapTime != this.BestLap?.Time
                            && this._RawDataNew.BestLapSectorTimes?.GetLapTime() != this.BestLap?.Time)
                        || this._RawDataNew.BestLapSectorTimes?.GetSectorSplit(1) != this.BestLap?.S1Time
                        || this._RawDataNew.BestLapSectorTimes?.GetSectorSplit(2) != this.BestLap?.S2Time
                        || this._RawDataNew.BestLapSectorTimes?.GetSectorSplit(3) != this.BestLap?.S3Time
                    )
                ) {
                    // Lap time end position may be offset with lap or spline position reset point.
                    this.BestLap =
                        new Lap(
                            this._RawDataNew.BestLapSectorTimes,
                            this._RawDataNew.BestLapTime,
                            this.Laps.New,
                            this.CurrentDriver!
                        ) { IsValid = true, IsOutLap = false, IsInLap = false };

                    if (this.BestLap.Time == this.LastLap?.Time) {
                        this.CurrentDriver!.BestLap = this.BestLap;
                    }
                }
            }
        }

        private void UpdateDrivers(Values values, Opponent rawData) {
            if (DynLeaderboardsPlugin._Game.IsAcc) {
                // ACC has more driver info than generic SimHub interface
                var accOpponent = (ACSharedMemory.Models.ACCOpponent)rawData;
                var realtimeCarUpdate = accOpponent.ExtraData;
                if (this._drivers.Count == 0) {
                    foreach (var driver in realtimeCarUpdate.CarEntry.Drivers) {
                        this._drivers.Add(new Driver(values, driver));
                    }
                } else {
                    // ACC driver name could be different from SimHub's full name
                    var currentRawDriver = realtimeCarUpdate.CarEntry.Drivers[realtimeCarUpdate.DriverIndex];
                    var currentDriverIndex = this._drivers.FindIndex(
                        d => d.FirstName == currentRawDriver.FirstName && d.LastName == currentRawDriver.LastName
                    );
                    if (currentDriverIndex == 0) {
                        // OK, current driver is already first in list
                    } else if (currentDriverIndex == -1) {
                        this._drivers.Insert(0, new Driver(values, currentRawDriver));
                    } else {
                        // move current driver to the front
                        this._drivers.MoveElementAt(currentDriverIndex, 0);
                    }
                }
            } else {
                var currentDriverIndex = this._drivers.FindIndex(d => d.FullName == this._RawDataNew.Name);
                if (currentDriverIndex == 0) {
                    // OK, current driver is already first in list
                } else if (currentDriverIndex == -1) {
                    this._drivers.Insert(0, new Driver(values, this._RawDataNew));
                } else {
                    // move current driver to the front
                    this._drivers.MoveElementAt(currentDriverIndex, 0);
                }
            }
        }

        /// <summary>
        ///     Requires that
        ///     * this.IsFinished
        ///     * this.Location
        ///     * this.IsInPitLane
        ///     are already updated
        /// </summary>
        /// <param name="sessionType"></param>
        private void HandleJumpToPits(SessionType sessionType) {
            Debug.Assert(sessionType == SessionType.RACE);
            if (!this.IsFinished // It's okay to jump to the pits after finishing
                && this.Location.Old == CarLocation.TRACK
                && this.IsInPitLane
            ) {
                Logging.LogInfo($"[{this._Id}, #{this.CarNumberAsString}] jumped to pits");
                this.JumpedToPits = true;
            }

            if (this.JumpedToPits && !this.IsInPitLane) {
                Logging.LogInfo($"[{this._Id}, #{this.CarNumberAsString}] jumped to pits cleared.");
                this.JumpedToPits = false;
            }
        }

        /// <summary>
        ///     Requires that
        ///     * this.SplinePosition
        ///     * this.IsInPitLane
        ///     * this.ExitedPitLane
        ///     * this.Laps
        ///     * this.JumpedToPits
        ///     are already updated
        /// </summary>
        /// <param name="sessionPhase"></param>
        private void CheckForCrossingStartLine(SessionPhase sessionPhase) {
            // Initial update before the start of the race
            if (sessionPhase is SessionPhase.PRE_SESSION or SessionPhase.PRE_FORMATION or SessionPhase.FORMATION_LAP
                && !this._isHasCrossedStartLineSet
                && this._HasCrossedStartLine
                && (this.SplinePosition > 0.5 || this.IsInPitLane)
                && this.Laps.New == 0
            ) {
                Logging.LogInfo($"[{this._Id}, #{this.CarNumberAsString}] has not crossed the start line");
                this._HasCrossedStartLine = false;
                this._isHasCrossedStartLineSet = true;
            }

            if (!this._HasCrossedStartLine
                && ((this.SplinePosition < 0.5 && !this.JumpedToPits) || this.ExitedPitLane)) {
                Logging.LogInfo($"[{this._Id}, #{this.CarNumberAsString}] crossed the start line");
                this._HasCrossedStartLine = true;
            }
        }

        /// <summary>
        ///     Requires that
        ///     * this.IsNewLap
        ///     * this.SplinePosition
        ///     * this.Laps
        ///     * this.HasCrossedStartLine
        ///     are already updated
        /// </summary>
        private void HandleOffsetLapUpdates() {
            // Check for offset lap update
            if (this._OffsetLapUpdate == OffsetLapUpdateType.NONE
                && this.IsNewLap
                && this.SplinePosition > 0.9
            ) {
                this._OffsetLapUpdate = OffsetLapUpdateType.LAP_BEFORE_SPLINE;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                //DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            } else if (this._OffsetLapUpdate == OffsetLapUpdateType.NONE
                && this._isSplinePositionReset
                && this.Laps.New != this._lapAtOffsetLapUpdate // Remove double detection with above
                && this.Laps.New == this.Laps.Old
                && this._HasCrossedStartLine
            ) {
                this._OffsetLapUpdate = OffsetLapUpdateType.SPLINE_BEFORE_LAP;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                //DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            }

            if (this._OffsetLapUpdate == OffsetLapUpdateType.LAP_BEFORE_SPLINE) {
                if (this.SplinePosition < 0.9) {
                    this._OffsetLapUpdate = OffsetLapUpdateType.NONE;
                    this._lapAtOffsetLapUpdate = -1;
                    //DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            } else if (this._OffsetLapUpdate == OffsetLapUpdateType.SPLINE_BEFORE_LAP) {
                if (this.Laps.New != this._lapAtOffsetLapUpdate
                    || this.SplinePosition is > 0.025 and < 0.9) {
                    // Second condition is a fallback in case the lap actually shouldn't have been updated
                    // (e.g. at the start line, jumped to pits and then crossed the line in the pits)
                    this._OffsetLapUpdate = OffsetLapUpdateType.NONE;
                    this._lapAtOffsetLapUpdate = -1;
                    //DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            }
        }

        /// <summary>
        ///     Requires that this.Location is already updated
        /// </summary>
        private void UpdatePitInfo(SessionPhase sessionPhase) {
            this.IsInPitLane = this.Location.New.IsInPits();
            this.ExitedPitLane = this.Location.New == CarLocation.TRACK && this.Location.Old.IsInPits();
            if (this.ExitedPitLane) {
                Logging.LogInfo($"Car {this._Id}, #{this.CarNumberAsString} exited pits");
            }

            this.EnteredPitLane = this.Location.New.IsInPits() && this.Location.Old == CarLocation.TRACK;
            if (this.EnteredPitLane) {
                Logging.LogInfo($"Car {this._Id}, #{this.CarNumberAsString} entered pits");
            }

            this.PitCount = this._RawDataNew.PitCount ?? 0;

            // TODO: using DateTime.now for timers if OK for online races where the time doesn't stop.
            //       However in SP races when the player pauses the game, usually the time also stops.
            //       Thus the calculated pit stop time would be longer than it actually was.

            var isSession =
                sessionPhase is >= SessionPhase.SESSION
                    or SessionPhase.UNKNOWN; // For games that don't support SessionPhase
            if (isSession // Don't start pit time counter if the session hasn't started
                && (this.EnteredPitLane
                    || (this.IsInPitLane && this.PitEntryTime == null)
                    || (this.PitEntryTime == null && this.IsInPitLane) // We join/start SimHub mid session
                )
            ) {
                this.PitEntryTime = DateTime.Now;
                this.IsCurrentLapInLap = true;
            }

            // Pit ended
            if (this.PitEntryTime != null && (this.ExitedPitLane || !this.IsInPitLane)) {
                this.IsCurrentLapOutLap = true;
                this.PitTimeLast = DateTime.Now - this.PitEntryTime;
                this.TotalPitTime += this.PitTimeLast.Value;
                this.PitTimeCurrent = null;
                this.PitEntryTime = null;
            }

            if (this.PitEntryTime != null) {
                var time = DateTime.Now - this.PitEntryTime;
                this.PitTimeCurrent = time;
            }
        }

        /// <summary>
        ///     Requires that
        ///     * this.IsNewLap
        ///     * this.ExitedPitLane
        ///     * this.EnteredPitLane
        ///     * this.Location
        ///     are updated
        /// </summary>
        /// <param name="session"></param>
        private void UpdateStintInfo(Session session) {
            if (this.IsNewLap && this.CurrentStintLaps != null) {
                this.CurrentStintLaps++;
            }

            // Stint started
            if (this.ExitedPitLane // Pit lane exit
                || (session.IsRace && session.IsSessionStart) // Race start
                || (this._stintStartTime == null
                    && this.Location.New == CarLocation.TRACK
                    && session.SessionPhase != SessionPhase.PRE_SESSION) // We join/start SimHub mid session
            ) {
                this._stintStartTime = DateTime.Now;
                this.CurrentStintLaps = 0;
            }

            // Stint ended
            if (this.EnteredPitLane && this._stintStartTime != null) {
                this.LastStintTime = DateTime.Now - this._stintStartTime;
                this.CurrentDriver!.OnStintEnd(this.LastStintTime.Value);
                this.LastStintLaps = this.CurrentStintLaps;
                this._stintStartTime = null;
                this.CurrentStintTime = null;
                this.CurrentStintLaps = null;
            }

            if (this._stintStartTime != null) {
                this.CurrentStintTime = DateTime.Now - this._stintStartTime;
            }
        }

        /// <summary>
        ///     Sets starting positions for this car.
        /// </summary>
        internal void SetStartingPositions(int overall, int inClass, int inCup) {
            this.PositionOverallStart = overall;
            this.PositionInClassStart = inClass;
            this.PositionInCupStart = inCup;
        }

        /// <summary>
        ///     Update data that requires that other cars have already received the basic update.
        ///     This includes for example relative spline positions, gaps and lap time deltas.
        /// </summary>
        internal void UpdateDependsOnOthers(
            Values values,
            CarData? focusedCar,
            CarData? overallBestLapCar,
            CarData? classBestLapCar,
            CarData? cupBestLapCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup,
            CarData? carAheadOnTrack,
            int overallPosition,
            int classPosition,
            int cupPosition
        ) {
            Debug.Assert(overallPosition > 0);
            Debug.Assert(classPosition > 0);
            Debug.Assert(cupPosition > 0);

            this.PositionOverall = overallPosition;
            this.PositionInClass = classPosition;
            this.PositionInCup = cupPosition;

            this.IndexOverall = overallPosition - 1;
            this.IndexClass = classPosition - 1;
            this.IndexCup = cupPosition - 1;

            if (overallBestLapCar == this) {
                this.IsBestLapCarOverall = true;
                this.IsBestLapCarInClass = true;
                this.IsBestLapCarInCup = true;
            } else if (classBestLapCar == this) {
                this.IsBestLapCarInClass = true;
                this.IsBestLapCarInCup = true;
            } else if (cupBestLapCar == this) {
                this.IsBestLapCarInCup = true;
            }

            if (this.IsFocused) {
                this.RelativeSplinePositionToFocusedCar = 0;
                this.GapToFocusedTotal = TimeSpan.Zero;
                this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
            } else if (focusedCar != null) {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePositionFrom(focusedCar);
            }

            if (!this.IsFinished) {
                if (DynLeaderboardsPlugin._Game.IsR3E) {
                    if (this._rawR3EDataNew != null && this._rawR3EDataNew._FinishStatus == R3E.FinishStatus.FINISHED) {
                        this.IsFinished = true;
                        this._FinishTime = DateTime.Now;
                    }
                } else if (values._IsFirstFinished && this.IsNewLap) {
                    this.IsFinished = true;
                    this._FinishTime = DateTime.Now;
                }
            }

            this.BestLap?.CalculateDeltas(
                thisCar: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: classBestLapCar,
                cupBestLapCar: cupBestLapCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                focusedCar: focusedCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup
            );

            this.LastLap?.CalculateDeltas(
                thisCar: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: classBestLapCar,
                cupBestLapCar: cupBestLapCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                focusedCar: focusedCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup
            );

            this.SetGaps(
                focusedCar: focusedCar,
                leaderCar: leaderCar,
                classLeaderCar: classLeaderCar,
                cupLeaderCar: cupLeaderCar,
                carAhead: carAhead,
                carAheadInClass: carAheadInClass,
                carAheadInCup: carAheadInCup,
                carAheadOnTrack: carAheadOnTrack,
                trackData: values.TrackData,
                session: values.Session
            );
        }

        private void SetRelLapDiff(CarData focusedCar) {
            if (this.GapToFocusedTotal == null) {
                if (this.Laps.New < focusedCar.Laps.New) {
                    this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                } else if (this.Laps.New > focusedCar.Laps.New) {
                    this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                } else {
                    if (this.GapToFocusedOnTrack > TimeSpan.Zero) {
                        this.RelativeOnTrackLapDiff = this.PositionOverall > focusedCar.PositionOverall
                            ? RelativeLapDiff.BEHIND
                            : RelativeLapDiff.SAME_LAP;
                    } else {
                        this.RelativeOnTrackLapDiff = this.PositionOverall < focusedCar.PositionOverall
                            ? RelativeLapDiff.AHEAD
                            : RelativeLapDiff.SAME_LAP;
                    }
                }
            } else if (this.GapToFocusedTotal > CarData._lapGapValue) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
            } else if (this.GapToFocusedTotal < CarData._halfLapGapValue) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                if (this.GapToFocusedOnTrack > TimeSpan.Zero) {
                    this.RelativeOnTrackLapDiff = this.PositionOverall > focusedCar.PositionOverall
                        ? RelativeLapDiff.BEHIND
                        : RelativeLapDiff.SAME_LAP;
                } else {
                    this.RelativeOnTrackLapDiff = this.PositionOverall < focusedCar.PositionOverall
                        ? RelativeLapDiff.AHEAD
                        : RelativeLapDiff.SAME_LAP;
                }
            } else {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
            }
        }

        /// <summary>
        ///     Calculates relative spline position from <paramref name="otherCar" /> to `this`.
        ///     Car will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        ///     If result is positive then `this` is ahead of <paramref name="otherCar" />, if negative it's behind.
        /// </summary>
        /// <returns>
        ///     Value in [-0.5, 0.5] or `null` if the result cannot be calculated.
        /// </returns>
        /// <param name="otherCar"></param>
        /// <returns></returns>
        public double CalculateRelativeSplinePositionFrom(CarData otherCar) {
            return CarData.CalculateRelativeSplinePosition(
                toPos: this.SplinePosition,
                fromPos: otherCar.SplinePosition
            );
        }

        /// <summary>
        ///     Calculates relative spline position of from <paramref name="fromPos" /> to <paramref name="toPos" />.
        ///     Position will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        ///     If result is positive then `to` is ahead of `from`, if negative it's behind.
        /// </summary>
        /// <param name="toPos"></param>
        /// <param name="fromPos"></param>
        /// <returns>
        ///     Value in [-0.5, 0.5].
        /// </returns>
        public static double CalculateRelativeSplinePosition(double fromPos, double toPos) {
            var relSplinePos = toPos - fromPos;
            if (relSplinePos > 0.5)
                // `to` is more than half a lap ahead, so technically it's closer behind.
                // Take one lap away to show it behind `from`.
            {
                relSplinePos -= 1.0;
            } else if (relSplinePos < -0.5)
                // `to` is more than half a lap behind, so it's in front.
                // Add one lap to show it in front of us.
            {
                relSplinePos += 1.0;
            }

            return relSplinePos;
        }

        private void SetGaps(
            CarData? focusedCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup,
            CarData? carAheadOnTrack,
            TrackData? trackData,
            Session session
        ) {
            // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
            if (trackData != null) {
                if (focusedCar == null) {
                    this.GapToFocusedTotal = null;
                } else {
                    this.GapToFocusedOnTrack = CarData.CalculateOnTrackGap(from: this, to: focusedCar, trackData);
                }

                this.GapToAheadOnTrack = carAheadOnTrack == null
                    ? null
                    : CarData.CalculateOnTrackGap(from: carAheadOnTrack, to: this, trackData);
            }

            if (session.IsRace) {
                // Use time gaps on track
                // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished.
                // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                if (trackData != null && this._OffsetLapUpdate == OffsetLapUpdateType.NONE) {
                    SetGap(from: this, to: leaderCar, other: leaderCar, x => this.GapToLeader = x);
                    SetGap(
                        from: this,
                        to: classLeaderCar,
                        other: classLeaderCar,
                        x => this.GapToClassLeader = x
                    );
                    SetGap(
                        from: this,
                        to: cupLeaderCar,
                        other: cupLeaderCar,
                        x => this.GapToCupLeader = x
                    );
                    SetGap(
                        from: focusedCar,
                        to: this,
                        other: focusedCar,
                        x => this.GapToFocusedTotal = x
                    );
                    SetGap(from: this, to: carAhead, other: carAhead, x => this.GapToAhead = x);
                    SetGap(
                        from: this,
                        to: carAheadInClass,
                        other: carAheadInClass,
                        x => this.GapToAheadInClass = x
                    );
                    SetGap(
                        from: this,
                        to: carAheadInCup,
                        other: carAheadInCup,
                        x => this.GapToAheadInCup = x
                    );

                    void SetGap(
                        CarData? from,
                        CarData? to,
                        CarData? other,
                        Action<TimeSpan?> setGap
                    ) {
                        if (from == null || to == null) {
                            setGap(null);
                        } else if (other?._OffsetLapUpdate == OffsetLapUpdateType.NONE) {
                            setGap(CarData.CalculateGap(from: from, to: to, trackData));
                        }
                    }

                    if (focusedCar != null && focusedCar._OffsetLapUpdate == OffsetLapUpdateType.NONE) {
                        this.SetRelLapDiff(focusedCar);
                    }
                }
            } else {
                // Use best laps to calculate gaps
                var thisBestLap = this.BestLap?.Time;
                if (thisBestLap == null) {
                    this.GapToLeader = null;
                    this.GapToClassLeader = null;
                    this.GapToCupLeader = null;
                    this.GapToFocusedTotal = null;
                    this.GapToAheadInClass = null;
                    this.GapToAheadInCup = null;
                    this.GapToAhead = null;
                    return;
                }

                this.GapToLeader = CalculateBestLapDelta(leaderCar);
                this.GapToClassLeader = CalculateBestLapDelta(classLeaderCar);
                this.GapToCupLeader = CalculateBestLapDelta(cupLeaderCar);
                this.GapToFocusedTotal = CalculateBestLapDelta(focusedCar);
                this.GapToAhead = CalculateBestLapDelta(carAhead);
                this.GapToAheadInClass = CalculateBestLapDelta(carAheadInClass);
                this.GapToAheadInCup = CalculateBestLapDelta(carAheadInCup);

                TimeSpan? CalculateBestLapDelta(CarData? to) {
                    var toBest = to?.BestLap?.Time;
                    return thisBestLap - toBest;
                }
            }
        }

        /// <summary>
        ///     Calculates gap between two cars.
        /// </summary>
        /// <returns>
        ///     The gap in seconds or laps with respect to the <paramref name="from" />.
        ///     It is positive if  <paramref name="to" />  is ahead of  <paramref name="from" />
        ///     and negative if behind.
        ///     If the gap is larger than a lap we only return the lap part (1lap, 2laps) and add 100_000 to the value
        ///     to differentiate it from gap on the same lap.
        ///     For example 100_002 means that <paramref name="to" />
        ///     is 2 laps ahead whereas result 99_998 means it's 2 laps behind.
        ///     If the result couldn't be calculated it returns <c>double.NaN</c>.
        /// </returns>
        public static TimeSpan? CalculateGap(CarData from, CarData to, TrackData trackData) {
            if (from._Id == to._Id
                || !to._HasCrossedStartLine
                || !from._HasCrossedStartLine
                || from._OffsetLapUpdate != OffsetLapUpdateType.NONE
                || to._OffsetLapUpdate != OffsetLapUpdateType.NONE
            ) {
                return null;
            }

            var fromLaps = from.Laps.New;
            var toLaps = to.Laps.New;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (fromLaps == toLaps && (from.JumpedToPits || to.JumpedToPits)) {
                return null;
            }

            if (from.IsFinished && to.IsFinished) {
                if (fromLaps == toLaps)
                    // If there IsFinished is set, FinishTime must also be set
                {
                    return from._FinishTime - to._FinishTime;
                }

                return TimeSpan.FromSeconds(toLaps - fromLaps) + CarData._lapGapValue;
            }

            // Fixes wrong gaps after finish on cars that haven't finished and are in pits.
            // Without this the gap could be off by one lap from the gap calculated from completed laps.
            // This is correct if the session is not finished as you could go out and complete that lap.
            // If session has finished you cannot complete that lap.
            if (toLaps != fromLaps
                && ((to.IsFinished && !from.IsFinished && from.IsInPitLane)
                    || (from.IsFinished && !to.IsFinished && to.IsInPitLane))
            ) {
                return TimeSpan.FromSeconds(toLaps - fromLaps) + CarData._lapGapValue;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'to' is behind
            if (distBetween <= -1)
                // 'to' is more than a lap behind of 'from'
            {
                return TimeSpan.FromSeconds(Math.Ceiling(distBetween)) + CarData._lapGapValue;
            }

            if (distBetween >= 1)
                // 'to' is more than a lap ahead of 'from'
            {
                return TimeSpan.FromSeconds(Math.Floor(distBetween)) + CarData._lapGapValue;
            }

            if (from.IsFinished
                || to.IsFinished
            ) {
                return null;
            }

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var toInterp = trackData._LapInterpolators.GetValueOr(to.CarClass, null);
            var fromInterp = trackData._LapInterpolators.GetValueOr(from.CarClass, null);
            if (toInterp == null && fromInterp == null)
                // lap data is not available, use naive distance based calculation
            {
                return CarData.CalculateNaiveGap(distBetween, trackData);
            }

            TimeSpan? gap;
            // At least one toInterp or fromInterp must be not null, because of the above check
            var (interp, cls) = toInterp != null ? (toInterp, to.CarClass) : (fromInterp!, from.CarClass);
            if (distBetween > 0)
                // `to` is ahead of `from`
            {
                gap = CarData.CalculateGapBetweenPos(
                    start: from.GetSplinePosTime(cls, trackData),
                    end: to.GetSplinePosTime(cls, trackData),
                    lapTime: interp._LapTime
                );
            } else
                // `to` is behind of `from`
            {
                gap = -CarData.CalculateGapBetweenPos(
                    start: to.GetSplinePosTime(cls, trackData),
                    end: from.GetSplinePosTime(cls, trackData),
                    lapTime: interp._LapTime
                );
            }

            return gap;
        }

        public static TimeSpan? CalculateOnTrackGap(CarData from, CarData to, TrackData trackData) {
            if (from._Id == to._Id) {
                return null;
            }

            var fromPos = from.SplinePosition;
            var toPos = to.SplinePosition;
            var relativeSplinePos = CarData.CalculateRelativeSplinePosition(fromPos: fromPos, toPos: toPos);

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var toInterp = trackData._LapInterpolators.GetValueOr(to.CarClass, null);
            var fromInterp = trackData._LapInterpolators.GetValueOr(from.CarClass, null);
            if (toInterp == null && fromInterp == null)
                // lap data is not available, use naive distance based calculation
            {
                return -CarData.CalculateNaiveGap(relativeSplinePos, trackData);
            }

            TimeSpan? gap;
            // At least one toInterp or fromInterp must be not null, because of the above check
            var (interp, cls) = toInterp != null ? (toInterp, to.CarClass) : (fromInterp!, from.CarClass);
            if (relativeSplinePos > 0) {
                gap = -CarData.CalculateGapBetweenPos(
                    start: from.GetSplinePosTime(cls, trackData),
                    end: to.GetSplinePosTime(cls, trackData),
                    lapTime: interp._LapTime
                );
            } else {
                gap = CarData.CalculateGapBetweenPos(
                    start: to.GetSplinePosTime(cls, trackData),
                    end: from.GetSplinePosTime(cls, trackData),
                    lapTime: interp._LapTime
                );
            }

            return gap;
        }

        public static TimeSpan CalculateNaiveGap(double splineDist, TrackData trackData) {
            var dist = splineDist * trackData.LengthMeters;
            // use avg speed of 50m/s (180km/h)
            // we could use actual speeds of the cars
            // but the gap will fluctuate either way, so I don't think it makes things better.
            // This also avoid the question of which speed to use (faster, slower, average)
            // and what happens if either car is standing (e.g. speed is 0, and we would divide by 0).
            // It's just in case backup anyway, so most of the time it should never even be reached.
            return TimeSpan.FromSeconds(dist / 50);
        }

        /// <summary>
        ///     Calculates the gap in seconds from <paramref name="start" /> to <paramref name="end" />.
        /// </summary>
        /// <returns>Non-negative value</returns>
        public static TimeSpan CalculateGapBetweenPos(TimeSpan start, TimeSpan end, TimeSpan lapTime) {
            if (end < start)
                // Ahead is on another lap, gap is time from `start` to end of the lap, and then to `end`
            {
                return lapTime - start + end; // We must be on the same lap, gap is time from `start` to reach `end`
            }

            return end - start;
        }

        /// <summary>
        ///     Calculates expected lap time for <paramref name="cls" /> class car at the position of <c>this</c> car.
        /// </summary>
        /// <returns>
        ///     Lap time in seconds or <c>-1.0</c> if it cannot be calculated.
        /// </returns>
        private TimeSpan GetSplinePosTime(CarClass cls, TrackData trackData) {
            // Same interpolated value is needed multiple times in one update, thus cache results.
            var pos = this._splinePositionTimes.GetValueOr(cls, null);
            if (pos != null) {
                return pos.Value;
            }

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var interp = trackData._LapInterpolators.GetValueOr(cls, null);
            if (interp != null) {
                var result = interp.Interpolate(this.SplinePosition);
                this._splinePositionTimes[cls] = result;
                return result;
            }

            return TimeSpan.FromSeconds(-1.0);
        }
    }

    public sealed class Driver {
        public string? FirstName { get; }
        public string? LastName { get; }
        public string ShortName { get; private set; }
        public string FullName { get; }
        public string InitialPlusLastName { get; private set; }
        public string? Initials { get; private set; }

        public DriverCategory Category { get; } = DriverCategory.Default;
        public string Nationality { get; private set; } = "Unknown";
        public int TotalLaps { get; internal set; } = 0;
        public LapBasic? BestLap { get; internal set; } = null;
        public TextBoxColor CategoryColor { get; private set; }

        private TimeSpan _totalDrivingTime;

        internal Driver(Values v, Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.Initials;
            this.InitialPlusLastName = o.ShortName;

            var col = DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors.GetOrAdd(this.Category);
            this.CategoryColor = new TextBoxColor(
                fg: col.Foreground ?? TextBoxColor.DEF_FG,
                bg: col.Background ?? TextBoxColor.DEF_BG
            );
        }

        internal Driver(Values v, ShAccBroadcasting.Structs.DriverInfo driver) {
            this.FirstName = driver.FirstName;
            this.LastName = driver.LastName;
            this.ShortName = driver.ShortName;
            this.Category = Driver.AccDriverCategoryToPrettyString(driver.Category);
            this.Nationality = Driver.AccNationalityToPrettyString(driver.Nationality);

            this.FullName = this.FirstName + " " + this.LastName;
            this.InitialPlusLastName = this.CreateInitialPlusLastNameAcc();
            this.Initials = this.CreateInitialsAcc();

            var col = DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors.GetOrAdd(this.Category);
            this.CategoryColor = new TextBoxColor(
                fg: col.Foreground ?? TextBoxColor.DEF_FG,
                bg: col.Background ?? TextBoxColor.DEF_BG
            );
        }

        internal void UpdateDriverInfos(Values values) {
            var col = DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors.GetOrAdd(this.Category);
            this.CategoryColor = new TextBoxColor(
                fg: col.Foreground ?? TextBoxColor.DEF_FG,
                bg: col.Background ?? TextBoxColor.DEF_BG
            );
        }

        internal void OnStintEnd(TimeSpan lastStintTime) {
            this._totalDrivingTime += lastStintTime;
        }

        internal TimeSpan GetTotalDrivingTime(bool isDriving, TimeSpan? currentStintTime = null) {
            if (isDriving && currentStintTime != null) {
                return this._totalDrivingTime + currentStintTime.Value;
            }

            return this._totalDrivingTime;
        }

        private string CreateInitialPlusLastNameAcc() {
            if (this.FirstName == "") {
                return $"{this.LastName}";
            }

            return $"{this.FirstName![0]}. {this.LastName}";
        }

        private string CreateInitialsAcc() {
            if (this.FirstName != "" && this.LastName != "") {
                return $"{this.FirstName![0]}{this.LastName![0]}";
            }

            if (this.FirstName == "" && this.LastName != "") {
                return $"{this.LastName![0]}";
            }

            if (this.FirstName != "" && this.LastName == "") {
                return $"{this.FirstName![0]}";
            }

            return "";
        }

        private static DriverCategory AccDriverCategoryToPrettyString(ksBroadcastingNetwork.DriverCategory category) {
            return category switch {
                ksBroadcastingNetwork.DriverCategory.Platinum => new DriverCategory("Platinum"),
                ksBroadcastingNetwork.DriverCategory.Gold => new DriverCategory("Gold"),
                ksBroadcastingNetwork.DriverCategory.Silver => new DriverCategory("Silver"),
                ksBroadcastingNetwork.DriverCategory.Bronze => new DriverCategory("Bronze"),
                _ => DriverCategory.Default,
            };
        }

        private static string AccNationalityToPrettyString(ShAccBroadcasting.NationalityEnum nationality) {
            return nationality switch {
                ShAccBroadcasting.NationalityEnum.Italy => "Italy",
                ShAccBroadcasting.NationalityEnum.Germany => "Germany",
                ShAccBroadcasting.NationalityEnum.France => "France",
                ShAccBroadcasting.NationalityEnum.Spain => "Spain",
                ShAccBroadcasting.NationalityEnum.GreatBritain => "Great Britain",
                ShAccBroadcasting.NationalityEnum.Hungary => "Hungary",
                ShAccBroadcasting.NationalityEnum.Belgium => "Belgium",
                ShAccBroadcasting.NationalityEnum.Switzerland => "Switzerland",
                ShAccBroadcasting.NationalityEnum.Austria => "Austria",
                ShAccBroadcasting.NationalityEnum.Russia => "Russia",
                ShAccBroadcasting.NationalityEnum.Thailand => "Thailand",
                ShAccBroadcasting.NationalityEnum.Netherlands => "Netherlands",
                ShAccBroadcasting.NationalityEnum.Poland => "Poland",
                ShAccBroadcasting.NationalityEnum.Argentina => "Argentina",
                ShAccBroadcasting.NationalityEnum.Monaco => "Monaco",
                ShAccBroadcasting.NationalityEnum.Ireland => "Ireland",
                ShAccBroadcasting.NationalityEnum.Brazil => "Brazil",
                ShAccBroadcasting.NationalityEnum.SouthAfrica => "South Africa",
                ShAccBroadcasting.NationalityEnum.PuertoRico => "Puerto Rico",
                ShAccBroadcasting.NationalityEnum.Slovakia => "Slovakia",
                ShAccBroadcasting.NationalityEnum.Oman => "Oman",
                ShAccBroadcasting.NationalityEnum.Greece => "Greece",
                ShAccBroadcasting.NationalityEnum.SaudiArabia => "Saudi Arabia",
                ShAccBroadcasting.NationalityEnum.Norway => "Norway",
                ShAccBroadcasting.NationalityEnum.Turkey => "Turkey",
                ShAccBroadcasting.NationalityEnum.SouthKorea => "South Korea",
                ShAccBroadcasting.NationalityEnum.Lebanon => "Lebanon",
                ShAccBroadcasting.NationalityEnum.Armenia => "Armenia",
                ShAccBroadcasting.NationalityEnum.Mexico => "Mexico",
                ShAccBroadcasting.NationalityEnum.Sweden => "Sweden",
                ShAccBroadcasting.NationalityEnum.Finland => "Finland",
                ShAccBroadcasting.NationalityEnum.Denmark => "Denmark",
                ShAccBroadcasting.NationalityEnum.Croatia => "Croatia",
                ShAccBroadcasting.NationalityEnum.Canada => "Canada",
                ShAccBroadcasting.NationalityEnum.China => "China",
                ShAccBroadcasting.NationalityEnum.Portugal => "Portugal",
                ShAccBroadcasting.NationalityEnum.Singapore => "Singapore",
                ShAccBroadcasting.NationalityEnum.Indonesia => "Indonesia",
                ShAccBroadcasting.NationalityEnum.USA => "USA",
                ShAccBroadcasting.NationalityEnum.NewZealand => "New Zealand",
                ShAccBroadcasting.NationalityEnum.Australia => "Australia",
                ShAccBroadcasting.NationalityEnum.SanMarino => "San Marino",
                ShAccBroadcasting.NationalityEnum.UAE => "UAE",
                ShAccBroadcasting.NationalityEnum.Luxembourg => "Luxembourg",
                ShAccBroadcasting.NationalityEnum.Kuwait => "Kuwait",
                ShAccBroadcasting.NationalityEnum.HongKong => "Hong Kong",
                ShAccBroadcasting.NationalityEnum.Colombia => "Colombia",
                ShAccBroadcasting.NationalityEnum.Japan => "Japan",
                ShAccBroadcasting.NationalityEnum.Andorra => "Andorra",
                ShAccBroadcasting.NationalityEnum.Azerbaijan => "Azerbaijan",
                ShAccBroadcasting.NationalityEnum.Bulgaria => "Bulgaria",
                ShAccBroadcasting.NationalityEnum.Cuba => "Cuba",
                ShAccBroadcasting.NationalityEnum.CzechRepublic => "Czech Republic",
                ShAccBroadcasting.NationalityEnum.Estonia => "Estonia",
                ShAccBroadcasting.NationalityEnum.Georgia => "Georgia",
                ShAccBroadcasting.NationalityEnum.India => "India",
                ShAccBroadcasting.NationalityEnum.Israel => "Israel",
                ShAccBroadcasting.NationalityEnum.Jamaica => "Jamaica",
                ShAccBroadcasting.NationalityEnum.Latvia => "Latvia",
                ShAccBroadcasting.NationalityEnum.Lithuania => "Lithuania",
                ShAccBroadcasting.NationalityEnum.Macau => "Macau",
                ShAccBroadcasting.NationalityEnum.Malaysia => "Malaysia",
                ShAccBroadcasting.NationalityEnum.Nepal => "Nepal",
                ShAccBroadcasting.NationalityEnum.NewCaledonia => "New Caledonia",
                ShAccBroadcasting.NationalityEnum.Nigeria => "Nigeria",
                ShAccBroadcasting.NationalityEnum.NorthernIreland => "Northern Ireland",
                ShAccBroadcasting.NationalityEnum.PapuaNewGuinea => "Papua New Guinea",
                ShAccBroadcasting.NationalityEnum.Philippines => "Philippines",
                ShAccBroadcasting.NationalityEnum.Qatar => "Qatar",
                ShAccBroadcasting.NationalityEnum.Romania => "Romania",
                ShAccBroadcasting.NationalityEnum.Scotland => "Scotland",
                ShAccBroadcasting.NationalityEnum.Serbia => "Serbia",
                ShAccBroadcasting.NationalityEnum.Slovenia => "Slovenia",
                ShAccBroadcasting.NationalityEnum.Taiwan => "Taiwan",
                ShAccBroadcasting.NationalityEnum.Ukraine => "Ukraine",
                ShAccBroadcasting.NationalityEnum.Venezuela => "Venezuela",
                ShAccBroadcasting.NationalityEnum.Wales => "Wales",
                ShAccBroadcasting.NationalityEnum.Any => "Any",
                (ShAccBroadcasting.NationalityEnum)78 => "Iran",
                (ShAccBroadcasting.NationalityEnum)79 => "Bahrain",
                (ShAccBroadcasting.NationalityEnum)80 => "Zimbabwe",
                (ShAccBroadcasting.NationalityEnum)81 => "Chinese Taipei",
                (ShAccBroadcasting.NationalityEnum)82 => "Chile",
                (ShAccBroadcasting.NationalityEnum)83 => "Uruguay",
                (ShAccBroadcasting.NationalityEnum)84 => "Madagascar",
                (ShAccBroadcasting.NationalityEnum)85 => "Malta",
                (ShAccBroadcasting.NationalityEnum)86 => "England",
                (ShAccBroadcasting.NationalityEnum)87 => "Bosnia and Herzegovina",
                (ShAccBroadcasting.NationalityEnum)88 => "Morocco",
                (ShAccBroadcasting.NationalityEnum)89 => "Sri Lanka",

                _ => nationality.ToString(),
            };
        }
    }

    public class Sectors {
        public TimeSpan? S1Time { get; private set; }
        public TimeSpan? S2Time { get; private set; }
        public TimeSpan? S3Time { get; private set; }

        internal Sectors(SectorTimes? sectorTimes) {
            this.S1Time = sectorTimes?.GetSectorSplit(1);
            this.S2Time = sectorTimes?.GetSectorSplit(2);
            this.S3Time = sectorTimes?.GetSectorSplit(3);

            if (this.S1Time == TimeSpan.Zero) {
                this.S1Time = null;
            }

            if (this.S2Time == TimeSpan.Zero) {
                this.S2Time = null;
            }

            if (this.S3Time == TimeSpan.Zero) {
                this.S3Time = null;
            }
        }

        internal Sectors(TimeSpan? s1, TimeSpan? s2, TimeSpan? s3) {
            this.S1Time = s1;
            this.S2Time = s2;
            this.S3Time = s3;
        }

        internal Sectors(Sectors other) {
            this.S1Time = other.S1Time;
            this.S2Time = other.S2Time;
            this.S3Time = other.S3Time;
        }

        internal Sectors() { }

        internal void Update(SectorSplits? sectorSplits) {
            this.S1Time = sectorSplits?.GetSectorSplit(1);
            this.S2Time = sectorSplits?.GetSectorSplit(2);
            this.S3Time = sectorSplits?.GetSectorSplit(3);

            if (this.S1Time == TimeSpan.Zero) {
                this.S1Time = null;
            }

            if (this.S2Time == TimeSpan.Zero) {
                this.S2Time = null;
            }

            if (this.S3Time == TimeSpan.Zero) {
                this.S3Time = null;
            }
        }

        internal void Update(ksBroadcastingNetwork.Structs.LapInfo lap) {
            var s1 = lap.Splits.ElementAtOr(0, null);
            var s2 = lap.Splits.ElementAtOr(1, null);
            var s3 = lap.Splits.ElementAtOr(2, null);
            this.S1Time = s1 != null && s1 != 0 ? TimeSpan.FromMilliseconds(s1.Value) : null;
            this.S2Time = s2 != null && s2 != 0 ? TimeSpan.FromMilliseconds(s2.Value) : null;
            this.S3Time = s3 != null && s3 != 0 ? TimeSpan.FromMilliseconds(s3.Value) : null;
        }
    }

    public class LapBasic : Sectors {
        public TimeSpan? Time { get; }

        public bool IsOutLap { get; internal set; } = false;
        public bool IsInLap { get; internal set; } = false;
        public bool IsValid { get; internal set; } = true;

        public int LapNumber { get; }
        public Driver Driver { get; }

        internal LapBasic(
            SectorTimes? sectorTimes,
            TimeSpan? lapTime,
            int lapNumber,
            Driver driver
        ) : base(sectorTimes) {
            this.Time = lapTime ?? sectorTimes?.GetLapTime();

            if (this.Time == TimeSpan.Zero) {
                this.Time = null;
            }

            this.LapNumber = lapNumber;
            this.Driver = driver;
        }

        internal LapBasic(ksBroadcastingNetwork.Structs.LapInfo lap, int lapNumber, Driver driver)
            : base(
                s1: lap.Splits?[0] != null ? TimeSpan.FromMilliseconds(lap.Splits[0]!.Value) : null,
                s2: lap.Splits?[1] != null ? TimeSpan.FromMilliseconds(lap.Splits[1]!.Value) : null,
                s3: lap.Splits?[2] != null ? TimeSpan.FromMilliseconds(lap.Splits[2]!.Value) : null
            ) {
            if (lap.LaptimeMS != null) {
                this.Time = TimeSpan.FromMilliseconds(lap.LaptimeMS.Value);
            }

            if (this.Time == TimeSpan.Zero) {
                this.Time = null;
            }

            this.LapNumber = lapNumber;
            this.Driver = driver;

            this.IsValid = lap.IsValidForBest;
            this.IsOutLap = lap.Type == ShAccBroadcasting.LapType.Outlap;
            this.IsInLap = lap.Type == ShAccBroadcasting.LapType.Inlap;
        }

        internal LapBasic(
            ksBroadcastingNetwork.Structs.LapInfo lap,
            int lapNumber,
            Driver driver,
            bool isValid,
            bool isOutLap,
            bool isInLap
        ) : this(lap, lapNumber, driver) {
            this.IsValid &= isValid;
            this.IsOutLap |= isOutLap;
            this.IsInLap |= isInLap;
        }

        internal LapBasic(
            TimeSpan? lapTime,
            TimeSpan? s1,
            TimeSpan? s2,
            TimeSpan? s3,
            int lapNumber,
            Driver driver
        ) : base(s1, s2, s3) {
            this.Time = lapTime;
            this.LapNumber = lapNumber;
            this.Driver = driver;
        }

        internal LapBasic(Lap lap) : base(lap) {
            this.Time = lap.Time;
            this.LapNumber = lap.LapNumber;
            this.Driver = lap.Driver;
            this.IsOutLap = lap.IsOutLap;
            this.IsInLap = lap.IsInLap;
            this.IsValid = lap.IsValid;
        }
    }

    public sealed class Lap : LapBasic {
        public TimeSpan? DeltaToOwnBest { get; private set; }

        public TimeSpan? DeltaToOverallBest { get; private set; }
        public TimeSpan? DeltaToClassBest { get; private set; }
        public TimeSpan? DeltaToCupBest { get; private set; }
        public TimeSpan? DeltaToLeaderBest { get; private set; }
        public TimeSpan? DeltaToClassLeaderBest { get; private set; }
        public TimeSpan? DeltaToCupLeaderBest { get; private set; }
        public TimeSpan? DeltaToFocusedBest { get; private set; }
        public TimeSpan? DeltaToAheadBest { get; private set; }
        public TimeSpan? DeltaToAheadInClassBest { get; private set; }
        public TimeSpan? DeltaToAheadInCupBest { get; private set; }

        public TimeSpan? DeltaToLeaderLast { get; private set; }
        public TimeSpan? DeltaToClassLeaderLast { get; private set; }
        public TimeSpan? DeltaToCupLeaderLast { get; private set; }
        public TimeSpan? DeltaToFocusedLast { get; private set; }
        public TimeSpan? DeltaToAheadLast { get; private set; }
        public TimeSpan? DeltaToAheadInClassLast { get; private set; }
        public TimeSpan? DeltaToAheadInCupLast { get; private set; }

        internal Lap(SectorTimes? sectorTimes, TimeSpan? lapTime, int lapNumber, Driver driver) : base(
            sectorTimes,
            lapTime,
            lapNumber,
            driver
        ) { }

        internal Lap(ksBroadcastingNetwork.Structs.LapInfo lap, int lapNumber, Driver driver) : base(
            lap,
            lapNumber,
            driver
        ) { }

        internal Lap(
            ksBroadcastingNetwork.Structs.LapInfo lap,
            int lapNumber,
            Driver driver,
            bool isValid,
            bool isOutLap,
            bool isInLap
        )
            : base(lap, lapNumber, driver, isValid: isValid, isOutLap: isOutLap, isInLap: isInLap) { }

        internal Lap(TimeSpan? lapTime, TimeSpan? s1, TimeSpan? s2, TimeSpan? s3, int lapNumber, Driver driver) : base(
            lapTime,
            s1,
            s2,
            s3,
            lapNumber,
            driver
        ) { }

        internal void CalculateDeltas(
            CarData thisCar,
            CarData? overallBestLapCar,
            CarData? classBestLapCar,
            CarData? cupBestLapCar,
            CarData leaderCar,
            CarData classLeaderCar,
            CarData cupLeaderCar,
            CarData? focusedCar,
            CarData? carAhead,
            CarData? carAheadInClass,
            CarData? carAheadInCup
        ) {
            if (this.Time == null) {
                return;
            }

            var overallBest = overallBestLapCar?.BestLap?.Time;
            var classBest = classBestLapCar?.BestLap?.Time;
            var cupBest = cupBestLapCar?.BestLap?.Time;
            var leaderBest = leaderCar.BestLap?.Time;
            var classLeaderBest = classLeaderCar.BestLap?.Time;
            var cupLeaderBest = cupLeaderCar.BestLap?.Time;
            var focusedBest = focusedCar?.BestLap?.Time;
            var aheadBest = carAhead?.BestLap?.Time;
            var aheadInClassBest = carAheadInClass?.BestLap?.Time;
            var aheadInCupBest = carAheadInCup?.BestLap?.Time;

            if (overallBest != null) {
                this.DeltaToOverallBest = this.Time - overallBest;
            }

            if (classBest != null) {
                this.DeltaToClassBest = this.Time - classBest;
            }

            if (cupBest != null) {
                this.DeltaToCupBest = this.Time - cupBest;
            }

            if (leaderBest != null) {
                this.DeltaToLeaderBest = this.Time - leaderBest;
            }

            if (classLeaderBest != null) {
                this.DeltaToClassLeaderBest = this.Time - classLeaderBest;
            }

            if (cupLeaderBest != null) {
                this.DeltaToCupLeaderBest = this.Time - cupLeaderBest;
            }

            this.DeltaToFocusedBest = this.Time - focusedBest;
            this.DeltaToAheadBest = this.Time - aheadBest;
            this.DeltaToAheadInClassBest = this.Time - aheadInClassBest;
            this.DeltaToAheadInCupBest = this.Time - aheadInCupBest;

            var thisBest = thisCar.BestLap?.Time;
            if (thisBest != null) {
                this.DeltaToOwnBest = this.Time - thisBest;
            }

            var leaderLast = leaderCar.LastLap?.Time;
            var classLeaderLast = classLeaderCar.LastLap?.Time;
            var cupLeaderLast = cupLeaderCar.LastLap?.Time;
            var focusedLast = focusedCar?.LastLap?.Time;
            var aheadLast = carAhead?.LastLap?.Time;
            var aheadInClassLast = carAheadInClass?.LastLap?.Time;
            var aheadInCupLast = carAheadInCup?.LastLap?.Time;

            if (leaderLast != null) {
                this.DeltaToLeaderLast = this.Time - leaderLast;
            }

            if (classLeaderLast != null) {
                this.DeltaToClassLeaderLast = this.Time - classLeaderLast;
            }

            if (cupLeaderLast != null) {
                this.DeltaToCupLeaderLast = this.Time - cupLeaderLast;
            }

            this.DeltaToFocusedLast = this.Time - focusedLast;
            this.DeltaToAheadLast = this.Time - aheadLast;
            this.DeltaToAheadInClassLast = this.Time - aheadInClassLast;
            this.DeltaToAheadInCupLast = this.Time - aheadInCupLast;
        }
    }

    public enum CarLocation {
        NONE = 0,
        TRACK = 1,
        PIT_LANE = 2,
        PIT_BOX = 3,
    }

    public static class CarLocationExt {
        public static bool IsInPits(this CarLocation location) {
            return location is CarLocation.PIT_LANE or CarLocation.PIT_BOX;
        }
    }

    public enum RelativeLapDiff {
        AHEAD = 1,
        SAME_LAP = 0,
        BEHIND = -1,
    }

    public sealed class NewOld<T> {
        public T New { get; private set; }
        public T Old { get; private set; }

        internal NewOld(T data) {
            this.New = data;
            this.Old = data;
        }

        internal void Update(T data) {
            this.Old = this.New;
            this.New = data;
        }
    }

    namespace R3E {
        // Reference https://github.com/sector3studios/r3e-api

        internal enum FinishStatus {
            UNKNOWN = -1,
            NONE = 0,
            FINISHED = 1,
            DNF = 2,
            DNQ = 3,
            DNS = 4,
            DQ = 5,
        }

        internal sealed class RawOpponentData {
            internal FinishStatus _FinishStatus { get; }
            internal bool _IsCurrentLapValid { get; }

            internal RawOpponentData(ref readonly ShR3E.Data.DriverData data) {
                this._FinishStatus = (FinishStatus)data.FinishStatus;
                this._IsCurrentLapValid = data.CurrentLapValid > 0;
            }
        }
    }

    namespace AMS2 {
        internal sealed class RawOpponentData(int raceState, bool isCurrentLapInvalidated) {
            public int RaceState { get; private set; } = raceState;
            public bool IsCurrentLapInvalidated { get; } = isCurrentLapInvalidated;
        }
    }
}