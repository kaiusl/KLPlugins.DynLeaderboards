using System;

namespace KLPlugins.DynLeaderboards.Car {

    internal class CarClassArray<T> {
        private const int _numClasses = 10;
        private readonly T[] _data = new T[_numClasses];
        public Func<CarClass, T> DefaultValue { get; private set; }

        public CarClassArray(T defValue) {
            this.DefaultValue = (_) => defValue;
            this.Reset();
        }

        public CarClassArray(Func<CarClass, T> defaultGenerator) {
            this.DefaultValue = defaultGenerator;
            this.Reset();
        }

        public T this[CarClass key] {
            get => this._data[(int)key];
            set => this._data[(int)key] = value;
        }

        public void Reset() {
            foreach (var v in (CarClass[])Enum.GetValues(typeof(CarClass))) {
                this._data[(int)v] = this.DefaultValue(v);
            }
        }
    }
}