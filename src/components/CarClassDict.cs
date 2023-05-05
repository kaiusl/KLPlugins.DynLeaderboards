using System;

namespace KLPlugins.DynLeaderboards.Car {

    internal class CarClassArray<T> {
        private const int _numClasses = 10;
        private readonly T[] _data = new T[_numClasses];
        public T DefaultValue { get; private set; }

        public CarClassArray(T defValue = default!) {
            this.DefaultValue = defValue;
            this.Reset();
        }

        public CarClassArray(Func<CarClass, T> generator, T defValue = default!) {
            this.DefaultValue = defValue;
            foreach (var v in (CarClass[])Enum.GetValues(typeof(CarClass))) {
                this._data[(int)v] = generator(v);
            }
        }

        public T this[CarClass key] {
            get => this._data[(int)key];
            set => this._data[(int)key] = value;
        }

        public void Reset() {
            for (int i = 0; i < _numClasses; i++) {
                this._data[i] = this.DefaultValue;
            }
        }
    }
}