using KLPlugins.Leaderboard.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard {
    public class CarClassArray<T> {
        private const int _numClasses = 9;
        private T[] _data = new T[_numClasses];

        public CarClassArray() { }

        public CarClassArray(T defValue) {
            SetAll(defValue);
        }

        public T this[CarClass key] {
            get => _data[(int)key];
            set => _data[(int)key] = value;
        }

        public void SetAll(T value) {
            for (int i = 0; i < _numClasses; i++) {
                _data[i] = value;
            }
        }
    }

}
