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

        public int Laps => this._rawDataNew.CurrentLap ?? 0;
        public double CurrentLapTime => this._rawDataNew.CurrentLapHighPrecision ?? double.NaN;
        public bool IsCurrentLapOutLap => this._rawDataNew.PitOutAtLap == this.Laps;
        public bool IsLastLapOutLap => this._rawDataNew.PitOutAtLap == this.Laps - 1;
        public bool IsCurrentLapInLap => this._rawDataNew.PitEnterAtLap == this.Laps;
        public bool IsLastLapInLap => this._rawDataNew.PitEnterAtLap == this.Laps - 1;
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

        private Opponent _rawDataNew;
        private Opponent _rawDataOld;

        public CarData(Opponent rawData) {
            this._rawDataNew = rawData;
            this.Update(rawData);
            this.PositionOverall = this._rawDataNew!.Position;
            this.PositionInClass = this._rawDataNew.PositionInClass;
        }

        public void Update(Opponent rawData) {
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
                // OK!
            } else {
                var driver = this.Drivers[currentDriverIndex];
                this.Drivers.RemoveAt(currentDriverIndex);
                this.Drivers.Insert(0, driver);
            }
        }

        public void SetOverallPosition(int overall) {
            this.PositionOverall = overall;
        }


        public void SetClassPosition(int cls) {
            this.PositionInClass = cls;
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