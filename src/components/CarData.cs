using System;
using System.Collections.Generic;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards.Car {

    public class CarData {

        public string CarClass => this.RawDataNew.CarClass ?? "";
        public string CarClassColor => this.RawDataNew.CarClassColor;
        public string CarClassTextColor => this.RawDataNew.CarClassTextColor;

        public string CarNumber => this.RawDataNew.CarNumber;
        public string CarModel => this.RawDataNew.CarName;
        public string TeamName => this.RawDataNew.TeamName;

        public int Laps => this.RawDataNew.CurrentLap - 1 ?? 0;
        public double CurrentLapTime => this.RawDataNew.CurrentLapHighPrecision ?? double.NaN;
        public bool IsCurrentLapOutLap => this.RawDataNew.PitOutAtLap == this.Laps + 1;
        public bool IsLastLapOutLap => this.RawDataNew.PitOutAtLap == this.Laps;
        public bool IsCurrentLapInLap => this.RawDataNew.PitEnterAtLap == this.Laps + 1;
        public bool IsLastLapInLap => this.RawDataNew.PitEnterAtLap == this.Laps;
        public bool IsCurrentLapValid => this.RawDataNew.LapValid;
        public bool IsLastLapValid { get; private set; }
        public SectorTimes LastLap => this.RawDataNew.LastLapSectorTimes;
        public SectorTimes BestLap => this.RawDataNew.BestLapSectorTimes;
        public SectorSplits BestSectors => this.RawDataNew.BestSectorSplits;

        public bool IsFocused => this.RawDataNew.IsPlayer;

        public List<Driver> Drivers { get; } = new();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int PositionOverallStart => this.RawDataNew.StartPosition ?? -1;
        public int PositionInClassStart => this.RawDataNew.StartPositionClass ?? -1;
        public int IndexOverall => this.PositionOverall - 1;
        public int IndexClass => this.PositionInClass - 1;

        public bool IsInPitLane => this.RawDataNew.IsCarInPitLane;
        public int PitCount => this.RawDataNew.PitCount ?? 0;
        public double PitTimeLast => this.RawDataNew.PitLastDuration?.TotalSeconds ?? 0.0;


        public double GapToLeader => (this.RawDataNew.LapsToLeader ?? 0) * 10000 + this.RawDataNew.GaptoLeader ?? 0;
        public double GapToClassLeader => (this.RawDataNew.LapsToClassLeader ?? 0) * 10000 + this.RawDataNew.GaptoClassLeader ?? 0;
        public double GapToFocusedTotal => (this.RawDataNew.LapsToPlayer ?? 0) * 10000 + this.RawDataNew.GaptoPlayer ?? 0;

        public double SplinePosition => this.RawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
        /// <summary>
        /// > 0 if ahead, < 0 if behind. Is in range [-0.5, 0.5].
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }
        public double TotalSplinePosition { get; private set; } = 0.0;

        internal string Id => this.RawDataNew.Id;
        internal bool IsUpdated { get; set; }

        public bool IsFinished { get; private set; } = false;
        public long? FinishTime { get; private set; } = null;
        internal Opponent RawDataNew;
        internal Opponent RawDataOld;

        public bool IsNewLap { get; private set; } = false;

        public CarData(Opponent rawData) {
            this.RawDataNew = rawData;
            this.UpdateIndependent(rawData);
            this.PositionOverall = this.RawDataNew!.Position;
            this.PositionInClass = this.RawDataNew.PositionInClass;
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        public void UpdateIndependent(Opponent rawData) {
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

            this.TotalSplinePosition = this.Laps + this.SplinePosition;
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
            } else if (focusedCar != null) {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePosition(focusedCar);
            }

            if (v.IsFirstFinished && this.IsNewLap) {
                this.IsFinished = true;
                this.FinishTime = DateTime.Now.Ticks;
            }
        }

        public void SetOverallPosition(int overall) {
            this.PositionOverall = overall;
        }


        public void SetClassPosition(int cls) {
            this.PositionInClass = cls;
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
}