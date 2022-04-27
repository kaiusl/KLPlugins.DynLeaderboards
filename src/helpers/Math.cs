namespace KLPlugins.DynLeaderboards.Helpers {
    class RunningAvg {
        public double Avg { get; private set; } = 0;
        private int _n = 0;

        public void Add(double v) {
            Avg = (Avg * _n + v) / (_n + 1);
            _n++;
        }

        public void Reset() {
            Avg = 0;
            _n = 0;
        }
    }
}
