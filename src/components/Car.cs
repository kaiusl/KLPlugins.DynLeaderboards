using GameReaderCommon;

namespace KLPlugins.DynLeaderboards.Car {

    public class CarData {
        public string DriverName => this._rawData.Name;
        public string CarClass => this._rawData.CarClass;
        public bool IsFocused => this._rawData.IsPlayer;
        public int PositionOverall { get; private set; }
        public int PositionInClass { get; private set; }
        public int IndexOverall => this.PositionOverall - 1;
        public int IndexClass => this.PositionInClass - 1;

        private Opponent _rawData;

        public CarData(Opponent rawData) {
            this._rawData = rawData;
            this.PositionOverall = this._rawData.Position;
            this.PositionInClass = this._rawData.PositionInClass;
        }

        public void Update(Opponent rawData) {
            this._rawData = rawData;
        }

        public void SetOverallPosition(int overall) {
            this.PositionOverall = overall;
        }


        public void SetClassPosition(int cls) {
            this.PositionInClass = cls;
        }

    }
}