using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Settings;

using MathNet.Numerics.Interpolation;

using Newtonsoft.Json;
namespace KLPlugins.DynLeaderboards.Track {
    internal class LapInterpolator {
        internal TimeSpan LapTime { get; private set; }
        private LinearSpline _interpolator { get; set; }
        private double[] _rawPos { get; set; }
        private double[] _rawTime { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // These are set in this.Init
        internal LapInterpolator(string path, double splinePosOffset) {
            var (rawPos, rawTime) = this.ReadLapInterpolatorData(path);
            this.Init(rawPos.AsReadOnly(), rawTime.AsReadOnly(), splinePosOffset);
        }

        internal LapInterpolator(ReadOnlyCollection<double> rawPos, ReadOnlyCollection<double> rawTime, double splinePosOffset) {
            this.Init(rawPos, rawTime, splinePosOffset);
        }
#pragma warning restore CS8618 

        private void Init(ReadOnlyCollection<double> rawPos, ReadOnlyCollection<double> rawTime, double splinePosOffset) {
            if (rawPos.Count != rawTime.Count) {
                throw new Exception($"Position and time data have different lengths (pos.Count={rawPos.Count}, time.Count={rawTime.Count}). Cannot build a lap interpolator from such data.");
            }

            // We need to make a copy to keep the same data that we received. 
            this._rawPos = rawPos.ToArray();
            this._rawTime = rawTime.ToArray();

            var (pos, time) = this.ProcessLapInterpolatorData(rawPos, rawTime, splinePosOffset);
            var interpolator = LinearSpline.InterpolateSorted(pos.ToArray(), time.ToArray());
            this._interpolator = interpolator;
            this.LapTime = TimeSpan.FromSeconds(this._interpolator.Interpolate(1.0));
        }

        private Tuple<List<double>, List<double>> ReadLapInterpolatorData(string path) {
            // Default lap_data files have 200 data points
            var pos = new List<double>(200);
            var time = new List<double>(200);

            var lines = File.ReadAllLines(path);
            foreach (var l in lines) {
                if (l == "") {
                    continue;
                }

                var splits = l.Split(';');
                double p = float.Parse(splits[0]);
                double t = float.Parse(splits[1]);

                pos.Add(p);
                time.Add(t);
            }

            return new Tuple<List<double>, List<double>>(pos, time);
        }

        internal void WriteLapInterpolatorData(string path) {
            var txt = "";
            foreach (var (splinePos, lapTime) in this._rawPos.Zip(this._rawTime, (a, b) => (a, b))) {
                txt += splinePos.ToString("F5") + ";" + lapTime.ToString("F3") + "\n";
            }

            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }

            // Create backups of current files

            if (File.Exists(path) && File.ReadAllText(path) != txt) {
                if (File.Exists($"{path}.10.bak")) {
                    File.Delete($"{path}.10.bak");
                }
                for (var i = 9; i > 0; i--) {
                    var bakpath = $"{path}.{i}.bak";
                    if (File.Exists(bakpath)) {
                        File.Move(bakpath, $"{path}.{i + 1}.bak");
                    }
                }

                File.Move(path, $"{path}.1.bak");
            }

            File.WriteAllText(path, txt);
        }

        private Tuple<List<double>, List<double>> ProcessLapInterpolatorData(ReadOnlyCollection<double> rawPos, ReadOnlyCollection<double> rawTime, double splinePosOffset) {
            // Default lap_data files have 200 data points
            var pos = new List<double>(200);
            var time = new List<double>(200);
            pos.Add(0.0);
            time.Add(0.0);

            var i = 0;
            // Find first point where spline position is < 0.1.
            // On some tracks there may be an offset and the data starts at 0.9x or something.
            // That is wrong pos to start. We use this.SplinePosOffset to correct it but it's not perfect, 
            // or it may be missing where it's needed.
            for (; i < rawPos.Count;) {
                var p = rawPos[i] + splinePosOffset;
                if (p > 1.0) {
                    p -= 1.0;
                }

                if (p < 0.1) {
                    break;
                } else {
                    i++;
                }
            }

            for (; i < rawPos.Count; i++) {

                double p = rawPos[i] + splinePosOffset;
                var t = rawTime[i];

                if (p > 1.0) {
                    p -= 1.0;
                }

                if (p == pos.Last() || t == time.Last()) {
                    // Interpolator expects increasing set of values. 
                    // If two consecutive values are the same, the interpolator can return double.NaN.
                    continue;
                }

                if (p < pos.Last() || p > 1.0) {
                    // pos needs to increasing for the linear interpolator to properly work
                    break;
                }

                pos.Add(p);
                time.Add(t);
            }

            // Extrapolate so that last point is at 1.0
            if (pos.Last() != 1.0) {
                var x0 = pos[pos.Count - 2];
                var x1 = pos.Last();

                var y0 = time[pos.Count - 2];
                var y1 = time.Last();

                pos.Add(1.0);
                var slope = (y1 - y0) / (x1 - x0);
                time.Add(y0 + (1.0 - x0) * slope);
            }

            //DynLeaderboardsPlugin.LogInfo($"Read {pos.Count} points from {fname}. pos={pos.ToJson()}, time={time.ToJson()}");

            return new Tuple<List<double>, List<double>>(pos, time);
        }

        internal TimeSpan Interpolate(double splinePos) {
            return TimeSpan.FromSeconds(this._interpolator.Interpolate(splinePos));
        }
    }

    public class TrackData {
        public string PrettyName { get; }
        public string Id { get; }
        public double LengthMeters { get; private set; }
        public double SplinePosOffset { get; }
        internal Dictionary<CarClass, LapInterpolator> LapInterpolators = [];
        private static Dictionary<string, double>? _splinePosOffsets = null;

        internal TrackData(GameData data) {
            this.PrettyName = data.NewData.TrackName;
            this.Id = data.NewData.TrackCode;
            this.LengthMeters = data.NewData.TrackLength;
            this.SplinePosOffset = _splinePosOffsets?.GetValueOr(this.Id, 0.0) ?? 0.0;

            this.AddLapInterpolators();
        }

        internal void SetLength(GameData data) {
            this.LengthMeters = data.NewData.TrackLength;
        }

        internal static void OnPluginInit(string gameName) {
            _splinePosOffsets = ReadSplinePosOffsets(gameName);
        }

        internal void Dispose() {
            foreach (var kv in this.LapInterpolators) {
                var path = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\{this.Id}_{kv.Key}.txt";
                kv.Value.WriteLapInterpolatorData(path);
            }
        }

        internal void OnLapFinished(CarClass cls, ReadOnlyCollection<double> lapDataPos, ReadOnlyCollection<double> lapDataTime) {
            if (lapDataPos.Count < 20 || lapDataTime.Count != lapDataPos.Count) {
                return;
            }

            var firstPosRaw = lapDataPos.First();
            var firstPos = firstPosRaw + this.SplinePosOffset;
            if (firstPos >= 1) {
                firstPos -= 1;
            }

            var lastPosRaw = lapDataPos.Last();
            var lastPos = lastPosRaw + this.SplinePosOffset;
            if (lastPos > 1) {
                lastPos -= 1;
            }

#if DEBUG
            var path = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data_summary\\{this.Id}.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, $"{firstPosRaw:F5};{lastPosRaw:F5};{lapDataTime.First():F3};{lapDataTime.Last():F3};{cls.AsString()}\n");
#endif

            if (firstPos < 0.05 && lastPos > 0.95 && firstPos < PluginSettings.LapDataTimeDelaySec * 5) {
                var newLapTime = lapDataTime.Last();
                var newLapLastPos = lapDataPos.Last();

                var current = this.LapInterpolators.GetValueOr(cls, null);
                if (current == null || current.Interpolate(newLapLastPos).TotalSeconds > newLapTime) {
                    this.AddLapInterpolator(rawPos: lapDataPos, rawTime: lapDataTime, cls);
                    DynLeaderboardsPlugin.LogInfo($"Saved new best lap for {cls}: {newLapTime}.");
                }
            } else if (firstPosRaw > lastPosRaw) {
                DynLeaderboardsPlugin.LogWarn($"Possible missing lap offset detected: {this.Id} - {cls}. FirstPos: {firstPosRaw}({firstPos}), LastPos: {lastPosRaw}({lastPos}). Suggested lap position offset is {1 - firstPosRaw}.");
            } else {
                DynLeaderboardsPlugin.LogInfo($"Collected invalid lap data for {this.Id} - {cls}. FirstPos: {firstPosRaw}({firstPos}), LastPos: {lastPosRaw}({lastPos}).");
            }
        }

        private static Dictionary<string, double>? ReadSplinePosOffsets(string gameName) {
            var path = $"{PluginSettings.PluginDataDir}\\{gameName}\\SplinePosOffsets.json";
            if (File.Exists(path)) {
                return JsonConvert.DeserializeObject<Dictionary<string, double>>(File.ReadAllText(path));
            } else {
                return null;
            }
        }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        private void AddLapInterpolators() {
            var lapsDataPath = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\";
            if (!Directory.Exists(lapsDataPath)) {
                return;
            }
            foreach (var path in Directory.GetFiles(lapsDataPath)) {
                if (!path.EndsWith(".txt")) continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith(this.Id)) {
                    var carClass = new CarClass(fileName.Substring(this.Id.Length + 1));
                    this.AddLapInterpolator(path, carClass);
                }
            }
        }

        /// Assumes that `this.LapInterpolators != null`
        private void AddLapInterpolator(string path, CarClass carClass) {
            this.LapInterpolators ??= [];

            if (!File.Exists(path)) {
                DynLeaderboardsPlugin.LogInfo($"Couldn't build lap interpolator for {carClass} because no suitable track data exists.");
                return;
            }

            try {
                this.LapInterpolators[carClass] = new LapInterpolator(path, this.SplinePosOffset);
                DynLeaderboardsPlugin.LogInfo($"Build lap interpolator for {carClass} from file {path}");
            } catch (Exception ex) {
                DynLeaderboardsPlugin.LogError($"Failed to read {path} with error: {ex}");
            }
        }

        private void AddLapInterpolator(ReadOnlyCollection<double> rawPos, ReadOnlyCollection<double> rawTime, CarClass carClass) {
            if (rawPos.Count != rawTime.Count) {
                DynLeaderboardsPlugin.LogError($"Tried to add a lap interpolator where rawPos and rawTime have different lengths: {rawPos.Count} != {rawTime.Count}.");
                return;
            }

            this.LapInterpolators ??= [];
            this.LapInterpolators[carClass] = new LapInterpolator(rawPos, rawTime, this.SplinePosOffset);
        }
    }
}