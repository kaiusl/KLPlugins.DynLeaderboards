using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Helpers;

using MathNet.Numerics.Interpolation;

using Newtonsoft.Json;

using Xceed.Wpf.Toolkit.Core.Converters;

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
        public double LengthMeters { get; }
        public double SplinePosOffset { get; }
        internal Dictionary<string, LapInterpolator> LapInterpolators = [];

        internal TrackData(GameData data) {

            this.Name = data.NewData.TrackName;
            this.Id = data.NewData.TrackId;
            this.LengthMeters = data.NewData.TrackLength;
            this.SplinePosOffset = ReadSplinePosOffsets()?.GetValueOr(this.Id, 0.0) ?? 0.0;

            this.CreateInterpolators();
        }

        private static Dictionary<string, double>? ReadSplinePosOffsets() {
            var path = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\{DynLeaderboardsPlugin.Game.Name}\\SplinePosOffsets.json";
            if (File.Exists(path)) {
                return JsonConvert.DeserializeObject<Dictionary<string, double>>(File.ReadAllText(path));
            } else {
                return null;
            }
        }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        private void CreateInterpolators() {
            var lapsDataPath = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\";
            if (!Directory.Exists(lapsDataPath)) {
                return;
            }
            foreach (var path in Directory.GetFiles(lapsDataPath)) {
                if (!path.EndsWith(".txt")) continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith(this.Id)) {
                    var carClass = fileName.Substring(this.Id.Length + 1);
                    this.AddLapInterpolator(path, carClass);
                }
            }
        }

        /// Assumes that `this.LapInterpolators != null`
        private void AddLapInterpolator(string fname, string carClass) {
            Debug.Assert(this.LapInterpolators != null, "Expected this.LapInterpolators != null");

            //var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\{this.Id}_{carClass}.txt";
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