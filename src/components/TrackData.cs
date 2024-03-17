using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Helpers;

using MathNet.Numerics.Interpolation;

namespace KLPlugins.DynLeaderboards.Track {
    internal class LapInterpolator {
        internal double LapTime { get; }
        private LinearSpline _interpolator { get; }

        internal LapInterpolator(LinearSpline interpolator, double lapTime) {
            this._interpolator = interpolator;
            this.LapTime = lapTime;
        }

        internal double Interpolate(double splinePos) {
            return this._interpolator.Interpolate(splinePos);
        }
    }

    public class TrackData {
        public string Name { get; }
        public string Id { get; }
        public float LengthMeters { get; }
        public double SplinePosOffset { get; }
        internal Dictionary<string, LapInterpolator?>? LapInterpolators = null;

        internal TrackData(string name, TrackType id, float lengthMeters) {
            this.Name = name;
            this.Id = id;
            this.LengthMeters = lengthMeters;
            this.SplinePosOffset = this.Id.SplinePosOffset();
        }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        internal void ReadDefBestLaps() {
            this.LapInterpolators = new();
        }

        /// Assumes that `this.LapInterpolators != null`
        private void AddLapInterpolator(string carClass) {
            Debug.Assert(this.LapInterpolators != null);
            var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\laps_data\\{this.Id}_{carClass}.txt";
            if (!File.Exists(fname)) {
                DynLeaderboardsPlugin.LogInfo($"Couldn't build lap interpolator for {carClass} because no suitable track data exists.");
                return;
            }

            try {
                var data = this.ReadLapInterpolatorData(fname);
                this.LapInterpolators![carClass] = new LapInterpolator(LinearSpline.InterpolateSorted(data.Item1.ToArray(), data.Item2.ToArray()), data.Item2.Last());
                DynLeaderboardsPlugin.LogInfo($"Build lap interpolator for {carClass} from file {fname}");
            } catch (Exception ex) {
                DynLeaderboardsPlugin.LogError($"Failed to read {fname} with error: {ex}");
            }
        }

        private Tuple<List<double>, List<double>> ReadLapInterpolatorData(string fname) {
            // Default lap_data files have 200 data points
            var pos = new List<double>(200);
            var time = new List<double>(200);
            foreach (var l in File.ReadLines(fname)) {
                if (l == "") {
                    continue;
                }
                // Data order: splinePositions, lap time in ms, speed in kmh
                var splits = l.Split(';');
                double p = float.Parse(splits[0]) + this.SplinePosOffset;
                var t = double.Parse(splits[1]);
                pos.Add(p);
                time.Add(t);
            }
            return new Tuple<List<double>, List<double>>(pos, time);
        }
    }
}