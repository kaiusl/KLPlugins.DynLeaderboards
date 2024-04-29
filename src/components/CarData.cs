using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Track;

using ksBroadcastingNetwork;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Car {
    public class CarData {
        public CarClass CarClass { get; private set; }
        public TextBoxColor CarClassColor { get; private set; }

        public string CarNumber { get; private set; } // string because 001 and 1 could be different numbers in some games
        /// <summary>
        /// Pretty car model name.
        /// </summary>
        public string CarModel { get; private set; }
        public string CarManufacturer { get; private set; }
        public string? TeamName { get; private set; }
        public TeamCupCategory TeamCupCategory { get; private set; }
        public TextBoxColor TeamCupCategoryColor { get; private set; }

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
        /// List of all drivers. Current driver is always the first.
        /// </summary>
        public ReadOnlyCollection<Driver> Drivers { get; }
        private List<Driver> _drivers { get; } = new();
        public Driver? CurrentDriver => this._drivers.FirstOrDefault();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int PositionInCup { get; private set; }
        public int? PositionOverallStart { get; private set; }
        public int? PositionInClassStart { get; private set; }
        public int? PositionInCupStart { get; private set; }
        /// <summary>
        /// Index of this car in Values.OverallOrder.
        /// </summary>
        public int IndexOverall { get; private set; }
        /// <summary>
        /// Index of this car in Values.ClassOrder.
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
        /// In range <c>[0, 1]</c>.
        /// </summary>
        public double SplinePosition { get; private set; }
        private double _prevSplinePosition { get; set; }

        /// <summary>
        /// <c>&gt; 0</c> if ahead, <c>&lt; 0</c> if behind. Is in range <c>[-0.5, 0.5]</c>.
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }

        public double TotalSplinePosition { get; private set; } = 0.0;

        public bool JumpedToPits { get; private set; } = false;
        /// <summary>
        /// Has the car crossed the start line at race start. 
        /// </summary>
        internal bool HasCrossedStartLine { get; private set; } = true;
        private bool _isHasCrossedStartLineSet = false;
        public bool IsFinished { get; private set; } = false;
        internal DateTime? FinishTime { get; private set; } = null;

        public int? CurrentStintLaps { get; private set; }
        public TimeSpan? LastStintTime { get; private set; }
        public TimeSpan? CurrentStintTime { get; private set; }
        public int? LastStintLaps { get; private set; }
        private DateTime? _stintStartTime;

        public double MaxSpeed { get; private set; } = 0.0;
        public bool IsConnected { get; private set; }

        internal int MissedUpdates = 0;

        /// <summary>
        /// Car ID.
        /// 
        /// In AC its the drivers name.
        /// In ACC single player its number from 0.
        /// In ACC multiplayer its number from 1000.
        /// </summary>
        internal string Id => this.RawDataNew.Id;
        /// <summary>
        /// Has this car received the update in latest data update.
        /// </summary>
        internal bool IsUpdated { get; set; } = true;

        internal Opponent RawDataNew;
        internal Opponent RawDataOld;

        // In some games the spline position and the lap counter reset at different locations.
        // Since we use total spline position to order the cars on track, we need them to be in sync
        internal enum OffsetLapUpdateType {
            None = 0,
            LapBeforeSpline = 1,
            SplineBeforeLap = 2
        }
        internal OffsetLapUpdateType OffsetLapUpdate { get; private set; } = OffsetLapUpdateType.None;

        private int _lapAtOffsetLapUpdate = -1;
        private bool _isSplinePositionReset = false;

        /// <summary>
        /// To indicate that the gap between this car and some other is more than a lap, 
        /// we add <c>_LAP_GAP_VALUE</c> to the gap in laps.
        /// </summary>
        private readonly static TimeSpan _LAP_GAP_VALUE = TimeSpan.FromSeconds(100_000);
        private readonly static TimeSpan _HALF_LAP_GAP_VALUE = TimeSpan.FromSeconds(_LAP_GAP_VALUE.TotalSeconds / 2);
        private Dictionary<CarClass, TimeSpan?> _splinePositionTimes = [];

        private bool _expectingNewLap = false;

        internal List<double> LapDataPos { get; } = [];
        internal List<double> LapDataTime { get; } = [];
        internal bool LapDataValidForSave { get; private set; } = false;

        internal CarData(Values values, string? focusedCarId, Opponent opponent) {
            this.Drivers = this._drivers.AsReadOnly();

            this.RawDataNew = opponent;
            this.RawDataOld = opponent;

            this.SetStaticCarData(values, opponent);

            this.IsCurrentLapValid = true;

            this.PositionOverall = this.RawDataNew!.Position;
            this.PositionInClass = this.RawDataNew.PositionInClass;
            this.PositionInCup = this.PositionInClass;
            this.UpdateIndependent(values, focusedCarId, opponent);

            this.CheckGameLapTimes(opponent);
        }

        private void CheckGameLapTimes(Opponent opponent) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)opponent;

                var lastLap = accRawData.ExtraData.LastLap;
                if (lastLap != null && lastLap.LaptimeMS != null) {
                    var driverRaw = accRawData.ExtraData.CarEntry.Drivers[lastLap.DriverIndex];
                    var driver = this.Drivers.First(d => d.FirstName == driverRaw.FirstName && d.LastName == driverRaw.LastName);
                    this.LastLap = new Lap(
                        lastLap,
                        this.Laps.New - 1,
                        driver
                    );
                }

                var bestLap = accRawData.ExtraData.BestSessionLap;
                if (bestLap != null && bestLap.LaptimeMS != null && bestLap.IsValidForBest) {
                    var driverRaw = accRawData.ExtraData.CarEntry.Drivers[bestLap.DriverIndex];
                    var driver = this.Drivers.First(d => d.FirstName == driverRaw.FirstName && d.LastName == driverRaw.LastName);
                    this.BestLap = new Lap(
                        bestLap,
                        this.Laps.New - 1, // We don't know the exact number, but say it was last lap
                        driver
                    );
                    driver.BestLap = this.BestLap;
                }
            } else if (DynLeaderboardsPlugin.Game.IsRf2OrLMU
                && opponent.ExtraData.ElementAtOr(0, null) is CrewChiefV4.rFactor2_V2.rFactor2Data.rF2VehicleScoring rf2RawData) {
                {
                    // Rf2 raw values are -1 if lap time is missing
                    var bestLapTime = rf2RawData.mBestLapTime;
                    var bestLapS1 = rf2RawData.mBestLapSector1;
                    var bestLapS2 = rf2RawData.mBestLapSector2 - (bestLapS1 > 0 ? bestLapS1 : 0);
                    var bestLapS3 = bestLapTime - (rf2RawData.mBestLapSector2 > 0 ? rf2RawData.mBestLapSector2 : 0);

                    // Only add sector times if all of them are known, this avoid weird sector times
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
                    } else if (this.BestLap != null) {
                        // sometimes last lap can be missing, but best lap is not. In that case, use best lap for consistent lap times.
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
            var rawClass = CarClass.TryNew(this.RawDataNew.CarClass) ?? CarClass.Default;
            OverridableCarInfo? carInfo = null;
            if (this.RawDataNew.CarName != null) {
                carInfo = values.CarInfos.Get(this.RawDataNew.CarName, rawClass);
            }

            this.CarClass = carInfo?.Class() ?? rawClass;
            this.CarModel = carInfo?.Name() ?? this.RawDataNew.CarName ?? "Unknown";
            this.CarManufacturer = carInfo?.Manufacturer() ?? GetCarManufacturer(this.CarModel);

            var color = values.CarClassColors.Get(this.CarClass);
            this.CarClassColor = new TextBoxColor(
                fg: color.Foreground() ?? this.RawDataNew.CarClassTextColor ?? OverridableTextBoxColor.DEF_FG,
                bg: color.Background() ?? this.RawDataNew.CarClassColor ?? OverridableTextBoxColor.DEF_BG
            );

            this.CarNumber = this.RawDataNew.CarNumber ?? "-1";

            this.TeamName = this.RawDataNew.TeamName;

            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)opponent;
                this.TeamCupCategory = ACCTeamCupCategoryToString(accRawData.ExtraData.CarEntry.CupCategory);
            } else {
                this.TeamCupCategory = TeamCupCategory.Default;
            }

            var cupColor = values.TeamCupCategoryColors.Get(this.TeamCupCategory);
            this.TeamCupCategoryColor = new TextBoxColor(
                fg: cupColor.Foreground() ?? OverridableTextBoxColor.DEF_FG,
                bg: cupColor.Background() ?? OverridableTextBoxColor.DEF_BG
            );
        }

        private static TeamCupCategory ACCTeamCupCategoryToString(byte cupCategory) {
            return cupCategory switch {
                0 => new TeamCupCategory("Overall"),
                1 => new TeamCupCategory("ProAm"),
                2 => new TeamCupCategory("Am"),
                3 => new TeamCupCategory("Silver"),
                4 => new TeamCupCategory("National"),
                _ => TeamCupCategory.Default
            };
        }

        private static string GetCarManufacturer(string? carModel) {
            // TODO: read from LUTs
            return carModel?.Split(' ')[0] ?? "Unknown";
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        internal void UpdateIndependent(Values values, string? focusedCarId, Opponent rawData) {
            // Clear old data

            // Needs to be cleared before UpdateDependsOnOthers, 
            // so that none of the cars have old data in it when we set gaps
            this._splinePositionTimes.Clear();

            // Actual update
            this.IsConnected = rawData.IsConnected
                && (!DynLeaderboardsPlugin.Game.IsAcc || rawData.Coordinates != null); // In ACC the cars remain in opponents list even if they disconnect, 
                                                                                       // however, it's coordinates will be null then 
            if (!this.IsConnected) {
                return;
            } else {
                this.MissedUpdates = 0;
            }

            this.RawDataOld = this.RawDataNew;
            this.RawDataNew = rawData;
            this.IsUpdated = true;

            if (DynLeaderboardsPlugin.Game.IsAcc && focusedCarId != null) {
                this.IsFocused = this.Id == focusedCarId;
            } else {
                this.IsFocused = this.RawDataNew.IsPlayer;
            }

            this.IsBestLapCarOverall = false;
            this.IsBestLapCarInClass = false;
            this.IsBestLapCarInCup = false;

            this.Laps.Update((this.RawDataNew.CurrentLap ?? 1) - 1);
            this.IsNewLap = this.Laps.New - this.Laps.Old == 1;

            this.SetCarLocation(rawData);
            this.UpdatePitInfo(values.Session.SessionPhase);
            this.SetSplinePositions(values);
            this.MaxSpeed = Math.Max(this.MaxSpeed, this.RawDataNew.Speed ?? 0.0);
            this.UpdateDrivers(values, rawData);

            if (this.IsNewLap) {
                Debug.Assert(this.CurrentDriver != null, "Current driver shouldn't be null since someone had to finish this lap.");
                var currentDriver = this.CurrentDriver!;
                currentDriver.TotalLaps += 1;
                this._expectingNewLap = true;
                this._isLastLapInLap = this.IsCurrentLapInLap;
                this._isLastLapOutLap = this.IsCurrentLapOutLap;
                this._isLastLapValid = this.IsCurrentLapValid;

                this.IsCurrentLapValid = true; // if we cross the line in pitlane, new lap is invalid
                this.IsCurrentLapOutLap = false; // also it will be an outlap
                this.IsCurrentLapInLap = false;
            }

            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.Models.ACCOpponent)this.RawDataNew;
                var rawDataOld = (ACSharedMemory.Models.ACCOpponent)this.RawDataOld;

                if (!this.IsNewLap && rawDataNew.ExtraData.CurrentLap.LaptimeMS < rawDataOld.ExtraData.CurrentLap.LaptimeMS) {
                    this._isLastLapInLap = this.IsCurrentLapInLap;
                    this._isLastLapOutLap = this.IsCurrentLapOutLap;
                    this._isLastLapValid = this.IsCurrentLapValid;

                    this.IsCurrentLapValid = true; // if we cross the line in pitlane, new lap is invalid
                    this.IsCurrentLapOutLap = false; // also it will be an outlap
                    this.IsCurrentLapInLap = false;
                }

                if (!this.IsCurrentLapOutLap && rawDataNew.ExtraData.CurrentLap.Type == LapType.Outlap) {
                    this.IsCurrentLapOutLap = true;
                }

                if (!this.IsCurrentLapInLap && rawDataNew.ExtraData.CurrentLap.Type == LapType.Inlap) {
                    this.IsCurrentLapInLap = true;
                }
            } else if (DynLeaderboardsPlugin.Game.IsAMS2) {
                if (!this.IsNewLap && (this.RawDataNew.CurrentLapTime == null || this.RawDataNew.CurrentLapTime < this.RawDataOld.CurrentLapTime)) {
                    this._isLastLapInLap = this.IsCurrentLapInLap;
                    this._isLastLapOutLap = this.IsCurrentLapOutLap;
                    this._isLastLapValid = this.IsCurrentLapValid;

                    this.IsCurrentLapValid = true; // if we cross the line in pitlane, new lap is invalid
                    this.IsCurrentLapOutLap = false; // also it will be an outlap
                    this.IsCurrentLapInLap = false;
                }
            }

            if (this.IsCurrentLapValid) {
                this.CheckIfLapInvalidated(rawData);
            }

            this.UpdateLapTimes(values.TrackData);

            if (values.Session.IsRace) {
                this.HandleJumpToPits(values.Session.SessionType);
                this.CheckForCrossingStartLine(values.Session.SessionPhase);
            }

            this.UpdateStintInfo(values.Session);
            if (!DynLeaderboardsPlugin.Game.IsAMS2) {
                this.HandleOffsetLapUpdates();
            }

            if (this.LapDataValidForSave && (this.IsCurrentLapInLap || this.IsCurrentLapOutLap || !this.IsCurrentLapValid || this.IsInPitLane)) {
                this.LapDataValidForSave = false;
            }

            if (this.RawDataOld.CurrentLapTime > this.RawDataNew.CurrentLapTime) {
                if (this.LapDataValidForSave && this.LapDataPos.Count != 0) {

                    // Add last point
                    var pos = this.RawDataOld.TrackPositionPercent;
                    var time = this.RawDataOld.CurrentLapTime?.TotalSeconds;
                    if (pos != null && time != null) {
                        var lastPos = this.LapDataPos.Last();
                        var lastTime = this.LapDataTime.Last();
                        if (lastPos != pos.Value && (time.Value - lastTime) > PluginSettings.LapDataTimeDelaySec) {
                            this.LapDataPos.Add(pos.Value);
                            this.LapDataTime.Add(time.Value);
                        }
                    }

                    values.TrackData?.OnLapFinished(this.CarClass, this.LapDataPos.AsReadOnly(), this.LapDataTime.AsReadOnly());
                }

                this.LapDataValidForSave = true;
                this.LapDataPos.Clear();
                this.LapDataTime.Clear();
            }

            var rawPos = this.RawDataNew.TrackPositionPercent;
            var rawTime = this.RawDataNew.CurrentLapTime;
            if (this.LapDataValidForSave && rawPos != null && rawTime != null) {
                if (this.LapDataPos.Count == 0) {
                    this.LapDataPos.Add(rawPos.Value);
                    this.LapDataTime.Add(rawTime.Value.TotalSeconds);
                } else {
                    var lastPos = this.LapDataPos.Last();
                    var lastTime = this.LapDataTime.Last();
                    if (lastPos != rawPos.Value && (rawTime.Value.TotalSeconds - lastTime) > PluginSettings.LapDataTimeDelaySec) {
                        this.LapDataPos.Add(rawPos.Value);
                        this.LapDataTime.Add(rawTime.Value.TotalSeconds);
                    }
                }
            }
        }

        private void CheckIfLapInvalidated(Opponent rawData) {
            if (!this.RawDataNew.LapValid) {
                this.IsCurrentLapValid = false;
                return;
            }

            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)rawData;
                if (!accRawData.ExtraData.CurrentLap.IsValidForBest || accRawData.ExtraData.CurrentLap.IsInvalid) {
                    this.IsCurrentLapValid = false;
                }
            } else if (DynLeaderboardsPlugin.Game.IsRf2) {
                // Rf2 doesn't directly export lap validity. But when one exceeds track limits the current sector times are 
                // set to -1.0. We cannot immediately detect the cut in the first sector, but as soon as we reach the 
                // 2nd sector we can detect it, when current sector 1 time is still -1.0.
                if (rawData.ExtraData.First() is CrewChiefV4.rFactor2_V2.rFactor2Data.rF2VehicleScoring rf2RawData) {
                    var curSector = rf2RawData.mSector;
                    if ((curSector == 2 || curSector == 0) && rf2RawData.mCurSector1 == -1.0) {
                        this.IsCurrentLapValid = false;
                    }
                }
            }
        }

        /// <summary>
        /// Requires that this.Laps is already updated.
        /// </summary>
        /// <param name="values"></param>
        /// <exception cref="System.Exception"></exception>
        private void SetSplinePositions(Values values) {
            var newSplinePos = this.RawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
            newSplinePos += values.TrackData!.SplinePosOffset;
            if (newSplinePos > 1) {
                newSplinePos -= 1;
            }
            this._isSplinePositionReset = newSplinePos < 0.1 && this.SplinePosition > 0.9;
            this._prevSplinePosition = this.SplinePosition;
            this.SplinePosition = newSplinePos;
            this.TotalSplinePosition = this.Laps.New + this.SplinePosition;
        }

        private void SetCarLocation(Opponent rawData) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accRawData = (ACSharedMemory.Models.ACCOpponent)rawData;
                var location = accRawData.ExtraData.CarLocation;
                var newLocation = location switch {
                    CarLocationEnum.Track or CarLocationEnum.PitEntry or CarLocationEnum.PitExit => CarLocation.Track,
                    CarLocationEnum.Pitlane => CarLocation.Pitlane,
                    _ => CarLocation.NONE,
                };
                this.Location.Update(newLocation);
            } else {
                if (this.RawDataNew.IsCarInPit) {
                    this.Location.Update(CarLocation.PitBox);
                } else if (this.RawDataNew.IsCarInPitLane) {
                    this.Location.Update(CarLocation.Pitlane);
                } else {
                    this.Location.Update(CarLocation.Track);
                }
            }
        }

        /// <summary>
        /// Requires that this._expectingNewLap is set in this update
        /// </summary>
        private void UpdateLapTimes(TrackData? track) {

            var prevCurrentLapTime = this.CurrentLapTime;
            if (DynLeaderboardsPlugin.Game.IsRf2OrLMU
                && this.RawDataNew.ExtraData.ElementAtOr(0, null) is CrewChiefV4.rFactor2_V2.rFactor2Data.rF2VehicleScoring rf2RawData
                && rf2RawData.mTimeIntoLap > 0 // fall back to SimHub's if rf2 doesn't report current lap time (it's -1 if missing)
            ) {
                this.CurrentLapTime = TimeSpan.FromSeconds(rf2RawData.mTimeIntoLap);
            } else {
                this.CurrentLapTime = this.RawDataNew.CurrentLapTime ?? TimeSpan.Zero;
            }

            // Add the last current lap time as last lap for rF2 if last lap vas invalid. In such case rF2 doesn't provide last lap times
            // and we need to manually calculate it.
            //
            // TODO: this doesn't add sector times, we can potentially calculate those as well
            if (DynLeaderboardsPlugin.Game.IsRf2OrLMU
                && this._expectingNewLap
                && !this._isLastLapValid
                && prevCurrentLapTime > this.CurrentLapTime // CurrentLapTime has reset
                && this.RawDataNew.ExtraData.ElementAtOr(0, null) is CrewChiefV4.rFactor2_V2.rFactor2Data.rF2VehicleScoring rf2RawData2
                && rf2RawData2.mLastSector1 == -1.0 // make sure to only use this method if invalid lap was due to lap cut in which case last lap/sector times are -1
            ) {
                this.LastLap = new Lap(null, prevCurrentLapTime, this.Laps.New, this.CurrentDriver!) {
                    IsValid = this._isLastLapValid,
                    IsOutLap = this._isLastLapOutLap,
                    IsInLap = this._isLastLapInLap,
                };
                this._expectingNewLap = false;
            } else if (DynLeaderboardsPlugin.Game.IsAcc) {
                // Special case ACC lap time updates.
                // This fixes a bug where in qualy/practice session joining mid session misses lap invalidation.
                // Thus possibly setting invalid lap as best lap. The order remains correct (it's set by the position received from ACC)
                // but an invalid lap is shown as a lap.
                // Since the order is supposed to be based on the best lap, this could show weird discrepancy between position and lap time.

                var accRawData = (ACSharedMemory.Models.ACCOpponent)this.RawDataNew;

                // Need to check for new lap time separately since lap update and lap time update may not be in perfect sync
                var lastLap = accRawData.ExtraData.LastLap;
                if (
                    this._expectingNewLap // GetLapTime, GetSectorSplit are relatively expensive and we don't need to check it every update
                    && lastLap != null
                    && lastLap.LaptimeMS != null
                    && lastLap.LaptimeMS != 0
                    && (
                        this.LastLap == null
                        || (lastLap.LaptimeMS != this.LastLap?.Time?.TotalMilliseconds)
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
                        if (this.BestLap?.Time == null || (bestLap.LaptimeMS ?? int.MaxValue) < this.BestLap.Time.Value.TotalMilliseconds) {
                            this.BestLap = new Lap(bestLap!, this.Laps.New, this.CurrentDriver!);
                            this.CurrentDriver!.BestLap = this.BestLap; // If it's car's best lap, it must also be the drivers
                        } else if (this.CurrentDriver?.BestLap?.Time == null || (bestLap.LaptimeMS ?? int.MaxValue) < this.CurrentDriver!.BestLap!.Time.Value.TotalMilliseconds) {
                            this.CurrentDriver!.BestLap = new LapBasic(bestLap!, this.Laps.New, this.CurrentDriver!);
                        }
                    }

                    this.BestSectors.Update(lastLap);
                    this._expectingNewLap = false;
                }
            } else {
                // Need to check for new lap time separately since lap update and lap time update may not be in perfect sync
                if (
                    this._expectingNewLap // GetLapTime, GetSectorSplit are relatively expensive and we don't need to check it every update
                    && this.RawDataNew.LastLapTime != null
                     && this.RawDataNew.LastLapTime != TimeSpan.Zero
                     // Sometimes LastLapTime and LastLapSectorTimes may differ very slightly. Check for both. If both are different then it's new lap.
                     && (
                        this.LastLap == null
                        || (this.RawDataNew.LastLapTime != this.LastLap?.Time && this.RawDataNew.LastLapSectorTimes?.GetLapTime() != this.LastLap?.Time)
                        || this.RawDataNew.LastLapSectorTimes?.GetSectorSplit(1) != this.LastLap?.S1Time
                        || this.RawDataNew.LastLapSectorTimes?.GetSectorSplit(2) != this.LastLap?.S2Time
                        || this.RawDataNew.LastLapSectorTimes?.GetSectorSplit(3) != this.LastLap?.S3Time
                    )
                ) {
                    // Lap time end position may be offset with lap or spline position reset point.
                    this.LastLap = new Lap(this.RawDataNew.LastLapSectorTimes, this.RawDataNew.LastLapTime, this.Laps.New, this.CurrentDriver!) {
                        IsValid = this._isLastLapValid,
                        IsOutLap = this._isLastLapOutLap,
                        IsInLap = this._isLastLapInLap,
                    };

                    //DynLeaderboardsPlugin.LogInfo($"[{this.Id}, #{this.CarNumber}] new last lap: {this.LastLap.Time}");
                    if (this.LastLap.Time != null && this.LastLap.IsValid) {
                        if (this.BestLap?.Time == null || this.LastLap.Time < this.BestLap?.Time) {
                            this.BestLap = this.LastLap;
                            this.CurrentDriver!.BestLap = this.BestLap; // If it's car's best lap, it must also be the drivers
                        } else if (this.CurrentDriver?.BestLap?.Time == null || this.LastLap.Time < this.CurrentDriver!.BestLap!.Time) {
                            this.CurrentDriver!.BestLap = this.LastLap;
                        }
                    }

                    this.BestSectors.Update(this.RawDataNew.BestSectorSplits);
                    this._expectingNewLap = false;
                }
            }
        }

        private void UpdateDrivers(Values values, Opponent rawData) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
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
                    var currentDriverIndex = this._drivers.FindIndex(d => d.FirstName == currentRawDriver.FirstName && d.LastName == currentRawDriver.LastName);
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
                var currentDriverIndex = this._drivers.FindIndex(d => d.FullName == this.RawDataNew.Name);
                if (currentDriverIndex == 0) {
                    // OK, current driver is already first in list
                } else if (currentDriverIndex == -1) {
                    this._drivers.Insert(0, new Driver(values, this.RawDataNew));
                } else {
                    // move current driver to the front
                    this._drivers.MoveElementAt(currentDriverIndex, 0);
                }
            }
        }

        /// <summary>
        /// Requires that
        ///     * this.IsFinished
        ///     * this.Location
        ///     * this.IsInPitLane
        /// are already updated
        /// </summary>
        /// <param name="sessionType"></param>
        private void HandleJumpToPits(SessionType sessionType) {
            Debug.Assert(sessionType == SessionType.Race);
            if (!this.IsFinished // It's okay to jump to the pits after finishing
                && this.Location.Old == CarLocation.Track
                && this.IsInPitLane
            ) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}, #{this.CarNumber}] jumped to pits");
                this.JumpedToPits = true;
            }

            if (this.JumpedToPits && !this.IsInPitLane) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}, #{this.CarNumber}] jumped to pits cleared.");
                this.JumpedToPits = false;
            }
        }

        /// <summary>
        /// Requires that
        ///     * this.SplinePosition
        ///     * this.IsInPitLane
        ///     * this.ExitedPitlane
        ///     * this.Laps
        ///     * this.JumpedToPits
        /// are already updated
        /// </summary>
        /// <param name="sessionPhase"></param>
        private void CheckForCrossingStartLine(SessionPhase sessionPhase) {
            // Initial update before the start of the race
            if ((sessionPhase == SessionPhase.PreSession || sessionPhase == SessionPhase.PreFormation)
                && !this._isHasCrossedStartLineSet
                && this.HasCrossedStartLine
                && (this.SplinePosition > 0.5 || this.IsInPitLane)
                && this.Laps.New == 0
            ) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}, #{this.CarNumber}] has not crossed the start line");
                this.HasCrossedStartLine = false;
                this._isHasCrossedStartLineSet = true;
            }

            if (!this.HasCrossedStartLine && ((this.SplinePosition < 0.5 && !this.JumpedToPits) || this.ExitedPitLane)) {
                DynLeaderboardsPlugin.LogInfo($"[{this.Id}, #{this.CarNumber}] crossed the start line");
                this.HasCrossedStartLine = true;
            }
        }

        /// <summary>
        /// Requires that
        ///     * this.IsNewLap
        ///     * this.SplinePosition
        ///     * this.Laps
        ///     * this.HasCrossedStartLine
        /// are already updated
        /// </summary>
        private void HandleOffsetLapUpdates() {
            // Check for offset lap update
            if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                && this.IsNewLap
                && this.SplinePosition > 0.9
            ) {
                this.OffsetLapUpdate = OffsetLapUpdateType.LapBeforeSpline;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                //DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            } else if (this.OffsetLapUpdate == OffsetLapUpdateType.None
                            && this._isSplinePositionReset
                            && this.Laps.New != this._lapAtOffsetLapUpdate // Remove double detection with above
                            && this.Laps.New == this.Laps.Old
                            && this.HasCrossedStartLine
                ) {
                this.OffsetLapUpdate = OffsetLapUpdateType.SplineBeforeLap;
                this._lapAtOffsetLapUpdate = this.Laps.New;
                //DynLeaderboardsPlugin.LogInfo($"Offset lap update [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
            }

            if (this.OffsetLapUpdate == OffsetLapUpdateType.LapBeforeSpline) {
                if (this.SplinePosition < 0.9) {
                    this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    this._lapAtOffsetLapUpdate = -1;
                    //DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            } else if (this.OffsetLapUpdate == OffsetLapUpdateType.SplineBeforeLap) {
                if (this.Laps.New != this._lapAtOffsetLapUpdate || (this.SplinePosition > 0.025 && this.SplinePosition < 0.9)) {
                    // Second condition is a fallback in case the lap actually shouldn't have been updated (eg at the start line, jumped to pits and then crossed the line in the pits)
                    this.OffsetLapUpdate = OffsetLapUpdateType.None;
                    this._lapAtOffsetLapUpdate = -1;
                    //DynLeaderboardsPlugin.LogInfo($"Offset lap update fixed [{this.Id}]: {this.OffsetLapUpdate}: sp={this.SplinePosition}, oldLap={this.Laps.Old}, newLap={this.Laps.New}");
                }
            }
        }

        /// <summary>
        /// Requires that this.Location is already updated
        /// </summary>
        private void UpdatePitInfo(SessionPhase sessionPhase) {
            this.IsInPitLane = this.Location.New.IsInPits();
            this.ExitedPitLane = this.Location.New == CarLocation.Track && this.Location.Old.IsInPits();
            if (this.ExitedPitLane) {
                DynLeaderboardsPlugin.LogInfo($"Car {this.Id}, #{this.CarNumber} exited pits");
            }
            this.EnteredPitLane = this.Location.New.IsInPits() && this.Location.Old == CarLocation.Track;
            if (this.EnteredPitLane) {
                DynLeaderboardsPlugin.LogInfo($"Car {this.Id}, #{this.CarNumber} entered pits");
            }
            this.PitCount = this.RawDataNew.PitCount ?? 0;

            // TODO: using DateTime.now for timers if OK for online races where the time doesn't stop.
            //       However in SP races when the player pauses the game, usually the time also stops.
            //       Thus the calculated pitstop time would be longer than it actually was.

            var isSession = sessionPhase >= SessionPhase.Session
                || sessionPhase == SessionPhase.Unknown; // For games that don't support SessionPhase
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
        /// Requires that 
        ///     * this.IsNewLap
        ///     * this.ExitedPitLane
        ///     * this.EnteredPitLane
        ///     * this.Location
        /// are updated
        /// </summary>
        /// <param name="session"></param>
        private void UpdateStintInfo(Session session) {
            if (this.IsNewLap && this.CurrentStintLaps != null) {
                this.CurrentStintLaps++;
            }

            // Stint started
            if (this.ExitedPitLane // Pitlane exit
                || (session.IsRace && session.IsSessionStart) // Race start
                || (this._stintStartTime == null && this.Location.New == CarLocation.Track && session.SessionPhase != SessionPhase.PreSession) // We join/start SimHub mid session
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
        /// Sets starting positions for this car.
        /// </summary>
        /// <param name="overall"></param>
        /// <param name="inClass"></param>
        internal void SetStartingPositions(int overall, int inClass, int inCup) {
            this.PositionOverallStart = overall;
            this.PositionInClassStart = inClass;
            this.PositionInCupStart = inCup;
        }

        /// <summary>
        /// Update data that requires that other cars have already received the basic update.
        /// 
        /// This includes for example relative spline positions, gaps and lap time deltas.
        /// </summary>
        /// <param name="focusedCar"></param>
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

            if (values.IsFirstFinished && this.IsNewLap) {
                this.IsFinished = true;
                this.FinishTime = DateTime.Now;
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
                        if (this.PositionOverall > focusedCar.PositionOverall) {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                        } else {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                        }
                    } else {
                        if (this.PositionOverall < focusedCar.PositionOverall) {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                        } else {
                            this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                        }
                    }
                }
            } else if (this.GapToFocusedTotal > _LAP_GAP_VALUE) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
            } else if (this.GapToFocusedTotal < _HALF_LAP_GAP_VALUE) {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                if (this.GapToFocusedOnTrack > TimeSpan.Zero) {
                    if (this.PositionOverall > focusedCar.PositionOverall) {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
                    } else {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                    }
                } else {
                    if (this.PositionOverall < focusedCar.PositionOverall) {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.AHEAD;
                    } else {
                        this.RelativeOnTrackLapDiff = RelativeLapDiff.SAME_LAP;
                    }
                }
            } else {
                this.RelativeOnTrackLapDiff = RelativeLapDiff.BEHIND;
            }
        }

        /// <summary>
        /// Calculates relative spline position from <paramref name="otherCar"/> to `this`.
        ///
        /// Car will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then `this` is ahead of <paramref name="otherCar"/>, if negative it's behind.
        /// </summary>
        /// <returns>
        /// Value in [-0.5, 0.5] or `null` if the result cannot be calculated.
        /// </returns>
        /// <param name="otherCar"></param>
        /// <returns></returns>
        public double CalculateRelativeSplinePositionFrom(CarData otherCar) {
            return CalculateRelativeSplinePosition(toPos: this.SplinePosition, fromPos: otherCar.SplinePosition);
        }

        /// <summary>
        /// Calculates relative spline position of from <paramref name="fromPos"/> to <paramref name="toPos"/>.
        ///
        /// Position will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then `to` is ahead of `from`, if negative it's behind.
        /// </summary>
        /// <param name="toPos"></param>
        /// <param name="fromPos"></param>
        /// <returns>
        /// Value in [-0.5, 0.5].
        /// </returns>
        public static double CalculateRelativeSplinePosition(double fromPos, double toPos) {
            var relSplinePos = toPos - fromPos;
            if (relSplinePos > 0.5) {
                // `to` is more than half a lap ahead, so technically it's closer from behind.
                // Take one lap away to show it behind `from`.
                relSplinePos -= 1.0;
            } else if (relSplinePos < -0.5) {
                // `to` is more than half a lap behind, so it's in front.
                // Add one lap to show it in front of us.
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
                    this.GapToFocusedOnTrack = CalculateOnTrackGap(from: this, to: focusedCar, trackData);
                }

                if (carAheadOnTrack == null) {
                    this.GapToAheadOnTrack = null;
                } else {
                    this.GapToAheadOnTrack = CalculateOnTrackGap(from: carAheadOnTrack, to: this, trackData);
                }
            }

            if (session.IsRace) {
                // Use time gaps on track
                // We update the gap only if CalculateGap returns a proper value because we don't want to update the gap if one of the cars has finished.
                // That would result in wrong gaps. We keep the gaps at the last valid value and update once both cars have finished.

                // Freeze gaps until all is in order again, fixes gap suddenly jumping to larger values as spline positions could be out of sync
                if (trackData != null && this.OffsetLapUpdate == OffsetLapUpdateType.None) {
                    SetGap(from: this, to: leaderCar, other: leaderCar, this.GapToLeader, x => this.GapToLeader = x);
                    SetGap(from: this, to: classLeaderCar, other: classLeaderCar, this.GapToClassLeader, x => this.GapToClassLeader = x);
                    SetGap(from: this, to: cupLeaderCar, other: cupLeaderCar, this.GapToCupLeader, x => this.GapToCupLeader = x);
                    SetGap(from: focusedCar, to: this, other: focusedCar, this.GapToFocusedTotal, x => this.GapToFocusedTotal = x);
                    SetGap(from: this, to: carAhead, other: carAhead, this.GapToAhead, x => this.GapToAhead = x);
                    SetGap(from: this, to: carAheadInClass, other: carAheadInClass, this.GapToAheadInClass, x => this.GapToAheadInClass = x);
                    SetGap(from: this, to: carAheadInCup, other: carAheadInCup, this.GapToAheadInCup, x => this.GapToAheadInCup = x);

                    void SetGap(CarData? from, CarData? to, CarData? other, TimeSpan? currentGap, Action<TimeSpan?> setGap) {
                        if (from == null || to == null) {
                            setGap(null);
                        } else if (other?.OffsetLapUpdate == OffsetLapUpdateType.None) {
                            setGap(CalculateGap(from: from, to: to, trackData));
                        }
                    }

                    if (focusedCar != null && focusedCar.OffsetLapUpdate == OffsetLapUpdateType.None) {
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
                    return toBest != null ? thisBestLap - toBest : null;
                }
            }
        }

        /// <summary>
        /// Calculates gap between two cars.
        /// </summary>
        /// <returns>
        /// The gap in seconds or laps with respect to the <paramref name="from">.
        /// It is positive if <paramref name="to"> is ahead of <paramref name="from"> and negative if behind.
        /// If the gap is larger than a lap we only return the lap part (1lap, 2laps) and add 100_000 to the value to differentiate it from gap on the same lap.
        /// For example 100_002 means that <paramref name="to"> is 2 laps ahead whereas result 99_998 means it's 2 laps behind.
        /// If the result couldn't be calculated it returns <c>double.NaN</c>.
        /// </returns>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static TimeSpan? CalculateGap(CarData from, CarData to, TrackData trackData) {
            if (from.Id == to.Id
                || !to.HasCrossedStartLine
                || !from.HasCrossedStartLine
                || from.OffsetLapUpdate != OffsetLapUpdateType.None
                || to.OffsetLapUpdate != OffsetLapUpdateType.None
            ) {
                return null;
            }

            var flaps = from.Laps.New;
            var tlaps = to.Laps.New;

            // If one of the cars jumped to pits there is no correct way to calculate the gap
            if (flaps == tlaps && (from.JumpedToPits || to.JumpedToPits)) {
                return null;
            }

            if (from.IsFinished && to.IsFinished) {
                if (flaps == tlaps) {
                    // If there IsFinished is set, FinishTime must also be set
                    return from.FinishTime - to.FinishTime;
                } else {
                    return TimeSpan.FromSeconds(tlaps - flaps) + _LAP_GAP_VALUE;
                }
            }

            // Fixes wrong gaps after finish on cars that haven't finished and are in pits.
            // Without this the gap could be off by one lap from the gap calculated from completed laps.
            // This is correct if the session is not finished as you could go out and complete that lap.
            // If session has finished you cannot complete that lap.
            if (tlaps != flaps &&
                ((to.IsFinished && !from.IsFinished && from.IsInPitLane)
                || (from.IsFinished && !to.IsFinished && to.IsInPitLane))
            ) {
                return TimeSpan.FromSeconds(tlaps - flaps) + _LAP_GAP_VALUE;
            }

            var distBetween = to.TotalSplinePosition - from.TotalSplinePosition; // Negative if 'to' is behind
            if (distBetween <= -1) { // 'to' is more than a lap behind of 'from'
                return TimeSpan.FromSeconds(Math.Ceiling(distBetween)) + _LAP_GAP_VALUE;
            } else if (distBetween >= 1) { // 'to' is more than a lap ahead of 'from'
                return TimeSpan.FromSeconds(Math.Floor(distBetween)) + _LAP_GAP_VALUE;
            } else {
                if (from.IsFinished
                    || to.IsFinished
                    || trackData == null
                ) {
                    return null;
                }

                // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
                var toInterp = trackData.LapInterpolators?.GetValueOr(to.CarClass, null);
                var fromInterp = trackData.LapInterpolators?.GetValueOr(from.CarClass, null);
                if (toInterp == null && fromInterp == null) {
                    // lap data is not available, use naive distance based calculation
                    return CalculateNaiveGap(distBetween, trackData);
                }

                TimeSpan? gap;
                // At least one toInterp or fromInterp must be not null, because of the above check
                (LapInterpolator interp, var cls) = toInterp != null ? (toInterp!, to.CarClass) : (fromInterp!, from.CarClass);
                if (distBetween > 0) { // `to` is ahead of `from`
                    gap = CalculateGapBetweenPos(start: from.GetSplinePosTime(cls, trackData), end: to.GetSplinePosTime(cls, trackData), lapTime: interp.LapTime);
                } else { // `to` is behind of `from`
                    gap = -CalculateGapBetweenPos(start: to.GetSplinePosTime(cls, trackData), end: from.GetSplinePosTime(cls, trackData), lapTime: interp.LapTime);
                }
                return gap;
            }
        }

        public static TimeSpan? CalculateOnTrackGap(CarData from, CarData to, TrackData trackData) {
            if (from.Id == to.Id || trackData == null) {
                return null;
            }

            var fromPos = from.SplinePosition;
            var toPos = to.SplinePosition;
            var relativeSplinePos = CalculateRelativeSplinePosition(fromPos: fromPos, toPos: toPos);

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var toInterp = trackData.LapInterpolators?.GetValueOr(to.CarClass, null);
            var fromInterp = trackData.LapInterpolators?.GetValueOr(from.CarClass, null);
            if (toInterp == null && fromInterp == null) {
                // lap data is not available, use naive distance based calculation
                return -CalculateNaiveGap(relativeSplinePos, trackData);
            }

            TimeSpan? gap;
            // At least one toInterp or fromInterp must be not null, because of the above check
            (LapInterpolator interp, var cls) = toInterp != null ? (toInterp!, to.CarClass) : (fromInterp!, from.CarClass);
            if (relativeSplinePos > 0) {
                gap = -CalculateGapBetweenPos(start: from.GetSplinePosTime(cls, trackData), end: to.GetSplinePosTime(cls, trackData), lapTime: interp.LapTime);
            } else {
                gap = CalculateGapBetweenPos(start: to.GetSplinePosTime(cls, trackData), end: from.GetSplinePosTime(cls, trackData), lapTime: interp.LapTime);
            }
            return gap;
        }

        public static TimeSpan CalculateNaiveGap(double splineDist, TrackData trackData) {
            var dist = splineDist * trackData.LengthMeters;
            // use avg speed of 50m/s (180km/h)
            // we could use actual speeds of the cars
            // but the gap will fluctuate either way so I don't think it makes things better.
            // This also avoid the question of which speed to use (faster, slower, average)
            // and what happens if either car is standing (eg speed is 0 and we would divide by 0).
            // It's an just in case backup anyway, so most of the times it should never even be reached.7654
            return TimeSpan.FromSeconds(dist / 50);
        }

        /// <summary>
        /// Calculates the gap in seconds from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <returns>Non-negative value</returns>
        public static TimeSpan CalculateGapBetweenPos(TimeSpan start, TimeSpan end, TimeSpan lapTime) {
            if (end < start) { // Ahead is on another lap, gap is time from `start` to end of the lap, and then to `end`
                return lapTime - start + end;
            } else { // We must be on the same lap, gap is time from `start` to reach `end`
                return end - start;
            }
        }

        /// <summary>
        /// Calculates expected lap time for <paramref name="cls"> class car at the position of <c>this</c> car.
        /// </summary>
        /// <returns>
        /// Lap time in seconds or <c>-1.0</c> if it cannot be calculated.
        /// </returns>>
        /// <param name="cls"></param>
        /// <returns></returns>
        private TimeSpan GetSplinePosTime(CarClass cls, TrackData trackData) {
            // Same interpolated value is needed multiple times in one update, thus cache results.
            var pos = this._splinePositionTimes.GetValueOr(cls, null);
            if (pos != null) {
                return pos.Value;
            }

            // TrackData is passed from Values, Values never stores TrackData without LapInterpolators
            var interp = trackData.LapInterpolators.GetValueOr(cls, null);
            if (interp != null) {
                var result = interp.Interpolate(this.SplinePosition);
                this._splinePositionTimes[cls] = result;
                return result;
            } else {
                return TimeSpan.FromSeconds(-1.0);
            }
        }
    }

    public class Driver {
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string ShortName { get; private set; }
        public string FullName { get; private set; }
        public string InitialPlusLastName { get; private set; }
        public string? Initials { get; private set; }

        public DriverCategory Category { get; private set; } = DriverCategory.Default;
        public string Nationality { get; private set; } = "Unknown";
        public int TotalLaps { get; internal set; } = 0;
        public LapBasic? BestLap { get; internal set; } = null;
        public TextBoxColor CategoryColor { get; private set; }

        private TimeSpan _totalDrivingTime;

        internal Driver(Values v, Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.Initials;
            this.InitialPlusLastName = o.ShortName;

            var col = v.DriverCategoryColors.Get(this.Category);
            this.CategoryColor = new TextBoxColor(
                fg: col.Foreground() ?? OverridableTextBoxColor.DEF_FG,
                bg: col.Background() ?? OverridableTextBoxColor.DEF_BG
            );
        }

        internal Driver(Values v, ksBroadcastingNetwork.Structs.DriverInfo driver) {
            this.FirstName = driver.FirstName;
            this.LastName = driver.LastName;
            this.ShortName = driver.ShortName;
            this.Category = ACCDriverCategoryToPrettyString(driver.Category);
            this.Nationality = ACCNationalityToPrettyString(driver.Nationality);

            this.FullName = this.FirstName + " " + this.LastName;
            this.InitialPlusLastName = this.CreateInitialPlusLastNameACC();
            this.Initials = this.CreateInitialsACC();

            var col = v.DriverCategoryColors.Get(this.Category);
            this.CategoryColor = new TextBoxColor(
                fg: col.Foreground() ?? OverridableTextBoxColor.DEF_FG,
                bg: col.Background() ?? OverridableTextBoxColor.DEF_BG
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

        private string CreateInitialPlusLastNameACC() {
            if (this.FirstName == "") {
                return $"{this.LastName}";
            }
            return $"{this.FirstName![0]}. {this.LastName}";
        }

        private string CreateInitialsACC() {
            if (this.FirstName != "" && this.LastName != "") {
                return $"{this.FirstName![0]}{this.LastName![0]}";
            } else if (this.FirstName == "" && this.LastName != "") {
                return $"{this.LastName![0]}";
            } else if (this.FirstName != "" && this.LastName == "") {
                return $"{this.FirstName![0]}";
            } else {
                return "";
            }
        }

        private static DriverCategory ACCDriverCategoryToPrettyString(ksBroadcastingNetwork.DriverCategory category) {
            return category switch {
                ksBroadcastingNetwork.DriverCategory.Platinum => new DriverCategory("Platinum"),
                ksBroadcastingNetwork.DriverCategory.Gold => new DriverCategory("Gold"),
                ksBroadcastingNetwork.DriverCategory.Silver => new DriverCategory("Silver"),
                ksBroadcastingNetwork.DriverCategory.Bronze => new DriverCategory("Bronze"),
                _ => DriverCategory.Default,
            };
        }

        private static string ACCNationalityToPrettyString(NationalityEnum nationality) {
            return nationality switch {
                NationalityEnum.Italy => "Italy",
                NationalityEnum.Germany => "Germany",
                NationalityEnum.France => "France",
                NationalityEnum.Spain => "Spain",
                NationalityEnum.GreatBritain => "Great Britain",
                NationalityEnum.Hungary => "Hungary",
                NationalityEnum.Belgium => "Belgium",
                NationalityEnum.Switzerland => "Switzerland",
                NationalityEnum.Austria => "Austria",
                NationalityEnum.Russia => "Russia",
                NationalityEnum.Thailand => "Thailand",
                NationalityEnum.Netherlands => "Netherlands",
                NationalityEnum.Poland => "Poland",
                NationalityEnum.Argentina => "Argentina",
                NationalityEnum.Monaco => "Monaco",
                NationalityEnum.Ireland => "Ireland",
                NationalityEnum.Brazil => "Brazil",
                NationalityEnum.SouthAfrica => "South Africa",
                NationalityEnum.PuertoRico => "Puerto Rico",
                NationalityEnum.Slovakia => "Slovakia",
                NationalityEnum.Oman => "Oman",
                NationalityEnum.Greece => "Greece",
                NationalityEnum.SaudiArabia => "Saudi Arabia",
                NationalityEnum.Norway => "Norway",
                NationalityEnum.Turkey => "Turkey",
                NationalityEnum.SouthKorea => "South Korea",
                NationalityEnum.Lebanon => "Lebanon",
                NationalityEnum.Armenia => "Armenia",
                NationalityEnum.Mexico => "Mexico",
                NationalityEnum.Sweden => "Sweden",
                NationalityEnum.Finland => "Finland",
                NationalityEnum.Denmark => "Denmark",
                NationalityEnum.Croatia => "Croatia",
                NationalityEnum.Canada => "Canada",
                NationalityEnum.China => "China",
                NationalityEnum.Portugal => "Portugal",
                NationalityEnum.Singapore => "Singapore",
                NationalityEnum.Indonesia => "Indonesia",
                NationalityEnum.USA => "USA",
                NationalityEnum.NewZealand => "New Zealand",
                NationalityEnum.Australia => "Australia",
                NationalityEnum.SanMarino => "San Marino",
                NationalityEnum.UAE => "UAE",
                NationalityEnum.Luxembourg => "Luxembourg",
                NationalityEnum.Kuwait => "Kuwait",
                NationalityEnum.HongKong => "Hong Kong",
                NationalityEnum.Colombia => "Colombia",
                NationalityEnum.Japan => "Japan",
                NationalityEnum.Andorra => "Andorra",
                NationalityEnum.Azerbaijan => "Azerbaijan",
                NationalityEnum.Bulgaria => "Bulgaria",
                NationalityEnum.Cuba => "Cuba",
                NationalityEnum.CzechRepublic => "Czech Republic",
                NationalityEnum.Estonia => "Estonia",
                NationalityEnum.Georgia => "Georgia",
                NationalityEnum.India => "India",
                NationalityEnum.Israel => "Israel",
                NationalityEnum.Jamaica => "Jamaica",
                NationalityEnum.Latvia => "Latvia",
                NationalityEnum.Lithuania => "Lithuania",
                NationalityEnum.Macau => "Macau",
                NationalityEnum.Malaysia => "Malaysia",
                NationalityEnum.Nepal => "Nepal",
                NationalityEnum.NewCaledonia => "New Caledonia",
                NationalityEnum.Nigeria => "Nigeria",
                NationalityEnum.NorthernIreland => "Northern Ireland",
                NationalityEnum.PapuaNewGuinea => "Papua New Guinea",
                NationalityEnum.Philippines => "Philippines",
                NationalityEnum.Qatar => "Qatar",
                NationalityEnum.Romania => "Romania",
                NationalityEnum.Scotland => "Scotland",
                NationalityEnum.Serbia => "Serbia",
                NationalityEnum.Slovenia => "Slovenia",
                NationalityEnum.Taiwan => "Taiwan",
                NationalityEnum.Ukraine => "Ukraine",
                NationalityEnum.Venezuela => "Venezuela",
                NationalityEnum.Wales => "Wales",
                _ => "Unknown"
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

        internal Sectors(TimeSpan? S1, TimeSpan? S2, TimeSpan? S3) {
            this.S1Time = S1;
            this.S2Time = S2;
            this.S3Time = S3;
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
        public TimeSpan? Time { get; private set; }

        public bool IsOutLap { get; internal set; } = false;
        public bool IsInLap { get; internal set; } = false;
        public bool IsValid { get; internal set; } = true;

        public int LapNumber { get; private set; }
        public Driver Driver { get; private set; }

        internal LapBasic(SectorTimes? sectorTimes, TimeSpan? lapTime, int lapNumber, Driver driver) : base(sectorTimes) {
            this.Time = lapTime ?? sectorTimes?.GetLapTime();

            if (this.Time == TimeSpan.Zero) {
                this.Time = null;
            }

            this.LapNumber = lapNumber;
            this.Driver = driver;
        }

        internal LapBasic(ksBroadcastingNetwork.Structs.LapInfo lap, int lapNumber, Driver driver)
            : base(
                S1: lap.Splits?[0] != null ? TimeSpan.FromMilliseconds(lap.Splits[0]!.Value) : null,
                S2: lap.Splits?[1] != null ? TimeSpan.FromMilliseconds(lap.Splits[1]!.Value) : null,
                S3: lap.Splits?[2] != null ? TimeSpan.FromMilliseconds(lap.Splits[2]!.Value) : null
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
            this.IsOutLap = lap.Type == LapType.Outlap;
            this.IsInLap = lap.Type == LapType.Inlap;
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

        internal LapBasic(TimeSpan? lapTime, TimeSpan? s1, TimeSpan? s2, TimeSpan? s3, int lapNumber, Driver driver) : base(s1, s2, s3) {
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

    public class Lap : LapBasic {
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

        internal Lap(SectorTimes? sectorTimes, TimeSpan? lapTime, int lapNumber, Driver driver) : base(sectorTimes, lapTime, lapNumber, driver) { }
        internal Lap(ksBroadcastingNetwork.Structs.LapInfo lap, int lapNumber, Driver driver) : base(lap, lapNumber, driver) { }
        internal Lap(ksBroadcastingNetwork.Structs.LapInfo lap, int lapNumber, Driver driver, bool isValid, bool isOutLap, bool isInLap)
            : base(lap, lapNumber, driver, isValid: isValid, isOutLap: isOutLap, isInLap: isInLap) { }

        internal Lap(TimeSpan? lapTime, TimeSpan? s1, TimeSpan? s2, TimeSpan? s3, int lapNumber, Driver driver) : base(lapTime, s1, s2, s3, lapNumber, driver) { }

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
            var leaderBest = leaderCar?.BestLap?.Time;
            var classLeaderBest = classLeaderCar?.BestLap?.Time;
            var cupLeaderBest = cupLeaderCar?.BestLap?.Time;
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

            this.DeltaToFocusedBest = focusedBest != null ? this.Time - focusedBest : null;
            this.DeltaToAheadBest = aheadBest != null ? this.Time - aheadBest : null;
            this.DeltaToAheadInClassBest = aheadInClassBest != null ? this.Time - aheadInClassBest : null;
            this.DeltaToAheadInCupBest = aheadInCupBest != null ? this.Time - aheadInCupBest : null;

            var thisBest = thisCar.BestLap?.Time;
            if (thisBest != null) {
                this.DeltaToOwnBest = this.Time - thisBest;
            }

            var leaderLast = leaderCar?.LastLap?.Time;
            var classLeaderLast = classLeaderCar?.LastLap?.Time;
            var cupLeaderLast = cupLeaderCar?.LastLap?.Time;
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

            this.DeltaToFocusedLast = focusedLast != null ? this.Time - focusedLast : null;
            this.DeltaToAheadLast = aheadLast != null ? this.Time - aheadLast : null;
            this.DeltaToAheadInClassLast = aheadInClassLast != null ? this.Time - aheadInClassLast : null;
            this.DeltaToAheadInCupLast = aheadInCupLast != null ? this.Time - aheadInCupLast : null;
        }
    }

    public enum CarLocation {
        NONE = 0,
        Track = 1,
        Pitlane = 2,
        PitBox = 3,
    }

    public static class CarLocationExt {
        public static bool IsInPits(this CarLocation location) {
            return location == CarLocation.Pitlane || location == CarLocation.PitBox;
        }
    }

    public enum RelativeLapDiff {
        AHEAD = 1,
        SAME_LAP = 0,
        BEHIND = -1
    }

    public class NewOld<T> {
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

    [TypeConverter(typeof(CarClassTypeConverter))]
    public readonly record struct CarClass : IComparable<CarClass> {
        private readonly string _cls;

        public CarClass(string cls) {
            this._cls = cls;
        }

        public static CarClass Default = new("");

        public static CarClass? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public override string ToString() {
            return this._cls.ToString();
        }

        public int CompareTo(CarClass other) {
            return this._cls.CompareTo(other._cls);
        }
    }

    internal class CarClassTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new CarClass((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((CarClass)value).AsString();
        }
    }

    [TypeConverter(typeof(TeamCupCategoryTypeConverter))]
    public readonly record struct TeamCupCategory : IComparable<TeamCupCategory> {
        private readonly string _cls;

        public TeamCupCategory(string cls) {
            this._cls = cls;
        }

        public static TeamCupCategory Default = new("Overall");

        public static TeamCupCategory? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public override string ToString() {
            return this._cls.ToString();
        }

        public int CompareTo(TeamCupCategory other) {
            return this._cls.CompareTo(other._cls);
        }
    }

    internal class TeamCupCategoryTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new TeamCupCategory((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((TeamCupCategory)value).AsString();
        }
    }

    [TypeConverter(typeof(DriverCategoryTypeConverter))]
    public readonly record struct DriverCategory : IComparable<DriverCategory> {
        private readonly string _cls;

        public DriverCategory(string cls) {
            this._cls = cls;
        }

        public static DriverCategory Default = new("Platinum");

        public static DriverCategory? TryNew(string? cls) {
            if (cls == null) {
                return null;
            }
            return new(cls);
        }

        public string AsString() {
            return this._cls;
        }

        public override string ToString() {
            return this._cls.ToString();
        }

        public int CompareTo(DriverCategory other) {
            return this._cls.CompareTo(other._cls);
        }
    }

    internal class DriverCategoryTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new DriverCategory((string)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return ((DriverCategory)value).AsString();
        }
    }
}
