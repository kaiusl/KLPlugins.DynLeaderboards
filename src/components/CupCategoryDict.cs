using System;

using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;

namespace KLPlugins.DynLeaderboards {

    internal class CupCategoryArray<T> {
        private const int _numCupCategories = 5;
        private readonly T[] _data = new T[_numCupCategories];
        public Func<TeamCupCategory, T> DefaultValue { get; private set; }

        public CupCategoryArray(T defValue) {
            this.DefaultValue = (_) => defValue;
            this.Reset();
        }

        public CupCategoryArray(Func<TeamCupCategory, T> defValue) {
            this.DefaultValue = defValue;
            this.Reset();
        }

        public T this[TeamCupCategory key] {
            get => this._data[(int)key];
            set => this._data[(int)key] = value;
        }

        public void Reset() {
            foreach (var v in (TeamCupCategory[])Enum.GetValues(typeof(TeamCupCategory))) {
                this._data[(int)v] = this.DefaultValue(v);
            }
        }
    }
}