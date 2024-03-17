using GameReaderCommon;

namespace KLPlugins.DynLeaderboards.Car {

    public class CarData {
        public string DriverName => this._rawDataNew.Name;

        public string CarClass => this._rawDataNew.CarClass;

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



        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
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
        }

        public void SetOverallPosition(int overall) {
            this.PositionOverall = overall;
        }


        public void SetClassPosition(int cls) {
            this.PositionInClass = cls;
        }

    }
}