using MathNet.Numerics.Statistics;
using System.Collections.Generic;

namespace KLPlugins.DynLeaderboards.Helpers {

    internal class Statistics {
        public DescriptiveStatistics Stats;
        public List<double> data = new List<double>();
        public double Median { get; private set; } = 0.0;

        public void Add(double v) {
            data.Add(v);
            Stats = new DescriptiveStatistics(data);
            Median = MathNet.Numerics.Statistics.Statistics.Median(data);
            if (double.IsNaN(Median))
                Median = 0.0;
        }

        public void Reset() {
            Stats = null;
            data.Clear();
            Median = 0.0;
        }
    }
}