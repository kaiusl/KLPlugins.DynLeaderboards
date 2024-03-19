using System.Collections.Generic;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards.Car {

    public class CarData {

        public string CarClass => this._rawDataNew.CarClass;
        public string CarClassColor => this._rawDataNew.CarClassColor;
        public string CarClassTextColor => this._rawDataNew.CarClassTextColor;

        public string CarNumber => this._rawDataNew.CarNumber;
        public string CarModel => this._rawDataNew.CarName;
        public string TeamName => this._rawDataNew.TeamName;

        public int Laps => this._rawDataNew.CurrentLap - 1 ?? 0;
        public double CurrentLapTime => this._rawDataNew.CurrentLapHighPrecision ?? double.NaN;
        public bool IsCurrentLapOutLap => this._rawDataNew.PitOutAtLap == this.Laps + 1;
        public bool IsLastLapOutLap => this._rawDataNew.PitOutAtLap == this.Laps;
        public bool IsCurrentLapInLap => this._rawDataNew.PitEnterAtLap == this.Laps + 1;
        public bool IsLastLapInLap => this._rawDataNew.PitEnterAtLap == this.Laps;
        public bool IsCurrentLapValid => this._rawDataNew.LapValid;
        public bool IsLastLapValid { get; private set; }
        public SectorTimes LastLap => this._rawDataNew.LastLapSectorTimes;
        public SectorTimes BestLap => this._rawDataNew.BestLapSectorTimes;
        public SectorSplits BestSectors => this._rawDataNew.BestSectorSplits;

        public bool IsFocused => this._rawDataNew.IsPlayer;

        public List<Driver> Drivers { get; } = new();

        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int PositionOverallStart => this._rawDataNew.StartPosition ?? -1;
        public int PositionInClassStart => this._rawDataNew.StartPositionClass ?? -1;
        public int IndexOverall => this.PositionOverall - 1;
        public int IndexClass => this.PositionInClass - 1;

        public bool IsInPitLane => this._rawDataNew.IsCarInPitLane;
        public int PitCount => this._rawDataNew.PitCount ?? 0;
        public double PitTimeLast => this._rawDataNew.PitLastDuration?.TotalSeconds ?? 0.0;


        public double GapToLeader => (this._rawDataNew.LapsToLeader ?? 0) * 10000 + this._rawDataNew.GaptoLeader ?? 0;
        public double GapToClassLeader => (this._rawDataNew.LapsToClassLeader ?? 0) * 10000 + this._rawDataNew.GaptoClassLeader ?? 0;
        public double GapToFocusedTotal => (this._rawDataNew.LapsToPlayer ?? 0) * 10000 + this._rawDataNew.GaptoPlayer ?? 0;

        public double SplinePosition => this._rawDataNew.TrackPositionPercent ?? throw new System.Exception("TrackPositionPercent is null");
        /// <summary>
        /// > 0 if ahead, < 0 if behind. Is in range [-0.5, 0.5].
        /// </summary>
        public double RelativeSplinePositionToFocusedCar { get; private set; }

        private Opponent _rawDataNew;
        private Opponent _rawDataOld;

        public CarData(Opponent rawData) {
            this._rawDataNew = rawData;
            this.UpdateIndependent(rawData);
            this.PositionOverall = this._rawDataNew!.Position;
            this.PositionInClass = this._rawDataNew.PositionInClass;
        }

        /// <summary>
        /// Update data that is independent of other cars data.
        /// </summary>
        /// <param name="rawData"></param>
        public void UpdateIndependent(Opponent rawData) {
            this._rawDataOld = this._rawDataNew;
            this._rawDataNew = rawData;

            if (this.Laps > this._rawDataOld.CurrentLap) {
                // new lap
                this.IsLastLapValid = this._rawDataOld.LapValid;
            }

            var currentDriverIndex = this.Drivers.FindIndex(d => d.FullName == this._rawDataNew.Name);
            if (currentDriverIndex == -1) {
                this.Drivers.Insert(0, new Driver(this._rawDataNew));
            } else if (currentDriverIndex == 0) {
                // OK, current driver is already first in list
            } else {
                // move current driver to the front
                var driver = this.Drivers[currentDriverIndex];
                this.Drivers.RemoveAt(currentDriverIndex);
                this.Drivers.Insert(0, driver);
            }
        }

        /// <summary>
        /// Update data that requires that other cars have already received the basic update.
        /// 
        /// This includes for example relative spline positions, gaps and lap time deltas.
        /// </summary>
        /// <param name="focusedCar"></param>
        public void UpdateDependsOnOthers(CarData focusedCar) {
            if (this.IsFocused) {
                this.RelativeSplinePositionToFocusedCar = 0;
            } else {
                this.RelativeSplinePositionToFocusedCar = this.CalculateRelativeSplinePosition(focusedCar);
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

        public Driver(Opponent o) {
            this.FullName = o.Name;
            this.ShortName = o.ShortName;
        }
    }
}