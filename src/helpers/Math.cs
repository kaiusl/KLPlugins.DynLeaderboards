using System.Collections.Generic;

using MathNet.Numerics.Statistics;

namespace KLPlugins.DynLeaderboards.Helpers {

    internal class Statistics {
        public DescriptiveStatistics? Stats;
        public List<double> data = new();
        public double Median { get; private set; } = 0.0;

        public void Add(double v) {
            this.data.Add(v);
            this.Stats = new DescriptiveStatistics(this.data);
            this.Median = MathNet.Numerics.Statistics.Statistics.Median(this.data);
            if (double.IsNaN(this.Median)) {
                this.Median = 0.0;
            }
        }

        public void Reset() {
            this.Stats = null;
            this.data.Clear();
            this.Median = 0.0;
        }
    }
}