using KLPlugins.DynLeaderboards.Enums;

namespace KLPlugins.DynLeaderboards {
    public class CarClassArray<T> {
        private const int _numClasses = 9;
        private T[] _data = new T[_numClasses];
        public T DefaultValue { get; private set; } = default(T);

        public CarClassArray() { }

        public CarClassArray(T defValue) {
            DefaultValue = defValue;
            Reset();
        }

        public T this[CarClass key] {
            get => _data[(int)key];
            set => _data[(int)key] = value;
        }

        public void Reset() {
            for (int i = 0; i < _numClasses; i++) {
                _data[i] = DefaultValue;
            }
        }
    }

}
