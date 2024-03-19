using System;
using System.Collections.Generic;
using System.Diagnostics;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards.Car {

    public struct NewOld<T> {
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

    public class CarData {

        public string CarClass { get; private set; }
        public string CarClassColor { get; private set; }
        public string CarClassTextColor { get; private set; }

        public string CarNumber { get; private set; }
        public string CarModel { get; private set; }
        public string? TeamName { get; private set; }

        public NewOld<CarLocation> Location { get; private set; } = new(CarLocation.NONE);

        public int Laps { get; private set; }
        public double CurrentLapTime { get; private set; }
        public bool IsCurrentLapOutLap { get; private set; }
        public bool IsLastLapOutLap { get; private set; }
        public bool IsCurrentLapInLap { get; private set; }
        public bool IsLastLapInLap { get; private set; }
        public bool IsCurrentLapValid { get; private set; }
        public bool IsLastLapValid { get; private set; }
        public SectorTimes LastLap => this.RawDataNew.LastLapSectorTimes;
        public SectorTimes BestLap => this.RawDataNew.BestLapSectorTimes;
        public SectorSplits BestSectors => this.RawDataNew.BestSectorSplits;

        public bool IsFocused => this.RawDataNew.IsPlayer;

        public List<Driver> Drivers { get; } = new();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int? PositionOverallStart { get; private set; }
        public int? PositionInClassStart { get; private set; }
        public int IndexOverall { get; private set; }
        public int IndexClass { get; private set; }

        public bool IsInPitLane { get; private set; }
        public int PitCount { get; private set; }
        public double PitTimeLast { get; private set; }


        public double GapToLeader { get; private set; }
        public double GapToClassLeader { get; private set; }
        public double GapToFocusedTotal { get; private set; }
        public double SplinePosition { get; private set; }

        /// <summary>
        /// > 0 if ahead, < 0 if behind. Is in range [-0.5, 0.5].
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }
        public double TotalSplinePosition { get; private set; } = 0.0;

        public bool JumpedToPits { get; private set; } = false;
        public bool IsFinished { get; private set; } = false;
        public long? FinishTime { get; private set; } = null;

        internal string Id => this.RawDataNew.Id;
        internal bool IsUpdated { get; set; }

        internal Opponent RawDataNew;
        internal Opponent RawDataOld;

        public bool IsNewLap { get; private set; } = false;

        public CarData(Values values, Opponent rawData) {
            this.RawDataNew = rawData;
            this.UpdateIndependent(values, rawData);
            this.CarClass = this.RawDataNew.CarClass ?? "";
            this.CarClassColor = this.RawDataNew.CarClassColor ?? "#FFFFFF";
            this.CarClassTextColor = this.RawDataNew.CarClassTextColor ?? "#000000";
            this.CarNumber = this.RawDataNew.CarNumber ?? "-1";
            this.CarModel = this.RawDataNew.CarName ?? "Unknown";
            this.TeamName = this.RawDataNew.TeamName;
            this.PositionOverall = this.RawDataNew!.Position;
            this.PositionInClass = this.RawDataNew.PositionInClass;
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        public void UpdateIndependent(Values values, Opponent rawData) {
            this.RawDataOld = this.RawDataNew;
            this.RawDataNew = rawData;

            this.IsNewLap = this.RawDataNew.CurrentLap > this.RawDataOld.CurrentLap;
            if (this.IsNewLap) {
                // new lap
                this.IsLastLapValid = this.RawDataOld.LapValid;
            }

            var currentDriverIndex = this.Drivers.FindIndex(d => d.FullName == this.RawDataNew.Name);
            if (currentDriverIndex == -1) {
                this.Drivers.Insert(0, new Driver(this.RawDataNew));
            } else if (currentDriverIndex == 0) {
                // OK, current driver is already first in list
            } else {
                // move current driver to the front
                var driver = this.Drivers[currentDriverIndex];
                this.Drivers.RemoveAt(currentDriverIndex);
                this.Drivers.Insert(0, driver);
            }

            if (this.RawDataNew.IsCarInPit) {
                this.Location.Update(CarLocation.PitBox);
            } else if (this.RawDataNew.IsCarInPitLane) {
                this.Location.Update(CarLocation.Pitlane);
            } else {
                this.Location.Update(CarLocation.Track);
            }

            this.Laps = (this.RawDataNew.CurrentLap ?? 1) - 1;
            this.CurrentLapTime = this.RawDataNew.CurrentLapTime?.TotalSeconds ?? 0.0;

            this.IsInPitLane = this.Location.New == CarLocation.Pitlane || this.Location.New == CarLocation.PitBox;
            this.PitCount = this.RawDataNew.PitCount ?? 0;
            this.PitTimeLast = this.RawDataNew.PitLastDuration?.TotalSeconds ?? 0.0;
            this.IsCurrentLapOutLap = (this.RawDataNew.PitOutAtLap ?? -1) == this.Laps + 1;
            this.IsLastLapOutLap = (this.RawDataNew.PitOutAtLap ?? -1) == this.Laps;
            this.IsCurrentLapInLap = (this.RawDataNew.PitEnterAtLap ?? -1) == this.Laps + 1;
            this.IsLastLapInLap = (this.RawDataNew.PitEnterAtLap ?? -1) == this.Laps;

            this.PositionInClassStart = this.RawDataNew.StartPositionClass;
            this.PositionOverallStart = this.RawDataNew.StartPosition;

            this.SplinePosition = this.RawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
            this.TotalSplinePosition = this.Laps + this.SplinePosition;

            this.HandleJumpToPits(values.Session.SessionType);
        }

        void HandleJumpToPits(SessionType sessionType) {
            if (sessionType == SessionType.Race
                && !this.IsFinished // It's okay to jump to the pits after finishing
                && this.Location.Old == CarLocation.Track
                && this.IsInPitLane
            ) {
                this.JumpedToPits = true;
            }

            if (this.JumpedToPits && !this.IsInPitLane) {
                this.JumpedToPits = false;
            }
        }

        /// <summary>
        /// Update data that requires that other cars have already received the basic update.
        /// 
        /// This includes for example relative spline positions, gaps and lap time deltas.
        /// </summary>
        /// <param name="focusedCar"></param>
        public void UpdateDependsOnOthers(Values v, CarData? focusedCar) {
            if (this.IsFocused) {
                this.RelativeSplinePositionToFocusedCar = 0;
                this.GapToFocusedTotal = 0;
            } else if (focusedCar != null) {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePosition(focusedCar);
                // TODO: fix it to proper one
                this.GapToFocusedTotal = (this.RawDataNew.LapsToPlayer ?? 0) * 10000 + this.RawDataNew.GaptoPlayer ?? 0;
            }

            if (v.IsFirstFinished && this.IsNewLap) {
                this.IsFinished = true;
                this.FinishTime = DateTime.Now.Ticks;
            }

            this.GapToLeader = (this.RawDataNew.LapsToLeader ?? 0) * 10000 + this.RawDataNew.GaptoLeader ?? 0;
            this.GapToClassLeader = (this.RawDataNew.LapsToClassLeader ?? 0) * 10000 + this.RawDataNew.GaptoClassLeader ?? 0;
        }

        public void SetOverallPosition(int overall) {
            Debug.Assert(overall > 0);
            this.PositionOverall = overall;
            this.IndexOverall = overall - 1;
        }


        public void SetClassPosition(int cls) {
            Debug.Assert(cls > 0);
            this.PositionInClass = cls;
            this.IndexClass = cls - 1;
        }

        /// <summary>
        /// Calculates relative spline position from `this` to <paramref name="otherCar"/>.
        ///
        /// Car will be shown ahead if it's ahead by less than half a lap, otherwise it's behind.
        /// If result is positive then `this` is ahead of <paramref name="otherCar"/>, if negative it's behind.
        /// </summary>
        /// <returns>
        /// Value in [-0.5, 0.5] or `null` if the result cannot be calculated.
        /// </returns>
        /// <param name="otherCar"></param>
        /// <returns></returns>
        public double CalculateRelativeSplinePosition(CarData otherCar) {
            return CalculateRelativeSplinePosition(this.SplinePosition, otherCar.SplinePosition);
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
        public static double CalculateRelativeSplinePosition(double toPos, double fromPos) {
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

    }

    public class Driver {
        public string FullName { get; private set; }
        public string ShortName { get; private set; }
        public string InitialPlusLastName { get; private set; }

        public Driver(Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.Initials;
            this.InitialPlusLastName = o.ShortName;
        }
    }

    public enum CarLocation {
        NONE = 0,
        Track = 1,
        Pitlane = 2,
        PitBox = 3,
    }
}