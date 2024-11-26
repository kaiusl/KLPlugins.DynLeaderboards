using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Settings;

using MathNet.Numerics.Interpolation;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Track;

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

    internal LapInterpolator(
        ReadOnlyCollection<double> rawPos,
        ReadOnlyCollection<double> rawTime,
        double splinePosOffset
    ) {
        this.Init(rawPos, rawTime, splinePosOffset);
    }
    #pragma warning restore CS8618

    private void Init(ReadOnlyCollection<double> rawPos, ReadOnlyCollection<double> rawTime, double splinePosOffset) {
        if (rawPos.Count != rawTime.Count) {
            throw new Exception(
                $"Position and time data have different lengths (pos.Count={rawPos.Count}, time.Count={rawTime.Count}). Cannot build a lap interpolator from such data."
            );
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
        if (dirPath != null && !Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        // Create backups of current files

        if (File.Exists(path) && File.ReadAllText(path) != txt) {
            if (File.Exists($"{path}.10.bak")) {
                File.Delete($"{path}.10.bak");
            }

            for (var i = 9; i > 0; i--) {
                var backupPath = $"{path}.{i}.bak";
                if (File.Exists(backupPath)) {
                    File.Move(backupPath, $"{path}.{i + 1}.bak");
                }
            }

            File.Move(path, $"{path}.1.bak");
        }

        File.WriteAllText(path, txt);
    }

    private Tuple<List<double>, List<double>> ProcessLapInterpolatorData(
        ReadOnlyCollection<double> rawPos,
        ReadOnlyCollection<double> rawTime,
        double splinePosOffset
    ) {
        // Default lap_data files have 200 data points
        var pos = new List<double>(200);
        var time = new List<double>(200);
        pos.Add(0.0);
        time.Add(0.0);

        var i = 0;
        // Find first point where spline position is < 0.1.
        // On some tracks there may be an offset and the data starts at 0.9x or something.
        // That is wrong pos to start. We use this.SplinePosOffset to correct it, but it's not perfect, 
        // or it may be missing where it's needed.
        for (; i < rawPos.Count;) {
            var p = rawPos[i] + splinePosOffset;
            if (p > 1.0) {
                p -= 1.0;
            }

            if (p < 0.1) {
                break;
            }

            i++;
        }

        for (; i < rawPos.Count; i++) {
            var p = rawPos[i] + splinePosOffset;
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

public class SplinePosOffset {
    [JsonProperty("min")] private double? _min = null;
    [JsonProperty("max")] private double? _max = null;
    [JsonIgnore] public double Value { get; private set; } = 0;

    [JsonConstructor]
    internal SplinePosOffset(double? min, double? max) {
        this._min = min;
        this._max = max;
        if (this._min != null && this._max != null) {
            this.Value = (this._max.Value + this._min.Value) / 2.0;
        }
    }

    internal SplinePosOffset() { }

    private void Reset() {
        this._min = null;
        this._max = null;
        this.Value = 0;
    }

    internal void Update(double offset) {
        // If we get conflicting offsets, just reset.
        // 
        // Most likely these could happen if there is no real offset needed and 
        // the data is somewhat noisy.
        if (this._min != null) {
            if (Math.Abs(this._min.Value - offset) > 0.05) {
                DynLeaderboardsPlugin.LogInfo(
                    $"Got conflicting min offsets: old={this._min.Value:F5}, new={offset:F5}"
                );
                this.Reset();
            }
        }

        if (this._max != null) {
            if (Math.Abs(this._max.Value - offset) > 0.05) {
                DynLeaderboardsPlugin.LogInfo(
                    $"Got conflicting max offsets: old={this._max.Value:F5}, new={offset:F5}"
                );
                this.Reset();
            }
        }

        this._min = Math.Min(this._min ?? offset, offset);
        this._max = Math.Max(this._max ?? offset, offset);
        // Don't update this.Value since all the lap interpolators are built with the value read at track loading.
        // Updating this.Value will result in wrong gaps.
        // New value will be loaded at next track loading.

        DynLeaderboardsPlugin.LogInfo(
            $"Updated SplinePosOffset: {this._min.Value:F5}..{this._max.Value:F5} -> {(this._min + this._max) / 2.0:F5}"
        );
    }
}

public class TrackData {
    public string PrettyName { get; }
    public string Id { get; }
    public double LengthMeters { get; private set; }
    public SplinePosOffset SplinePosOffset { get; }

    // Idea with building lap interpolators is to read and build them on another thread which
    // adds them to this._builtLapInterpolators. On main data update thread we then check once
    // at update start if there are any new lap interpolators and add them to this.LapInterpolators.
    // Alternatively we could use ConcurrentDictionary for this.LapInterpolators but we need to 
    // access it a lot per one update. So it would include a lot of unnecessary synchronizations
    // while with current solution we only have one synchronized access.
    internal readonly Dictionary<CarClass, LapInterpolator> LapInterpolators = [];
    internal readonly HashSet<CarClass> UpdatedInterpolators = [];

    private readonly ConcurrentQueue<(CarClass, LapInterpolator)> _builtLapInterpolators = [];

    // To avoid building same lap interpolator multiple times.
    // IMPORTANT: it needs to be locked before use.
    private readonly List<CarClass> _lapInterpolatorsInBuilding = [];
    private static Dictionary<string, SplinePosOffset> _splinePosOffsets = [];

    internal TrackData(GameData data) {
        TrackData._splinePosOffsets = TrackData.ReadSplinePosOffsets(DynLeaderboardsPlugin.Game.Name);

        this.PrettyName = data.NewData.TrackName;
        this.Id = data.NewData.TrackCode;
        this.LengthMeters = data.NewData.TrackLength;
        this.SplinePosOffset = TrackData._splinePosOffsets.GetOrAddValue(this.Id, new SplinePosOffset());
    }

    internal void SetLength(GameData data) {
        this.LengthMeters = data.NewData.TrackLength;
    }

    internal static void OnPluginInit(string gameName) {
        TrackData._splinePosOffsets = TrackData.ReadSplinePosOffsets(gameName);
    }

    internal void OnDataUpdate() {
        while (this._builtLapInterpolators.TryDequeue(out var kv)) {
            if (!this.LapInterpolators.ContainsKey(kv.Item1)) {
                this.LapInterpolators[kv.Item1] = kv.Item2;

                DynLeaderboardsPlugin.LogInfo($"Added LapInterpolator for {kv.Item1}");
            }

            this._lapInterpolatorsInBuilding.Remove(kv.Item1);
        }
    }

    internal void SaveData() {
        DynLeaderboardsPlugin.LogInfo("Saving track data");
        foreach (var kv in this.UpdatedInterpolators) {
            var path =
                $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\{this.Id}_{kv}.txt";
            if (this.LapInterpolators.TryGetValue(kv, out var interp)) {
                interp.WriteLapInterpolatorData(path);
            }
        }

        this.UpdatedInterpolators.Clear();

        this.WriteSplinePosOffsets();
    }

    internal void Dispose() {
        DynLeaderboardsPlugin.LogInfo("Disposing track data");
        this.SaveData();
    }

    internal void OnLapFinished(
        CarClass cls,
        ReadOnlyCollection<double> lapDataPos,
        ReadOnlyCollection<double> lapDataTime
    ) {
        if (lapDataPos.Count < 20 || lapDataTime.Count != lapDataPos.Count) {
            return;
        }

        var firstPosRaw = lapDataPos.First();
        var firstPos = firstPosRaw + this.SplinePosOffset.Value;
        if (firstPos >= 1) {
            firstPos -= 1;
        }

        var lastPosRaw = lapDataPos.Last();
        var lastPos = lastPosRaw + this.SplinePosOffset.Value;
        if (lastPos > 1) {
            lastPos -= 1;
        }

        // #if DEBUG
        // var path =
        //     $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data_summary\\{this.Id}.txt";
        // Directory.CreateDirectory(Path.GetDirectoryName(path));
        // File.AppendAllText(
        //     path,
        //     $"{firstPosRaw:F5};{lastPosRaw:F5};{lapDataTime.First():F3};{lapDataTime.Last():F3};{cls.AsString()}\n"
        // );
        // #endif

        if (firstPos < 0.05 && lastPos > 0.95 && lapDataTime.First() < PluginSettings.LAP_DATA_TIME_DELAY_SEC * 5) {
            var newLapTime = lapDataTime.Last();
            var newLapLastPos = lapDataPos.Last();

            var current = this.LapInterpolators.GetValueOr(cls, null);
            if (current == null || current.Interpolate(newLapLastPos).TotalSeconds > newLapTime) {
                this.AddLapInterpolator(rawPos: lapDataPos, rawTime: lapDataTime, cls);
                this.UpdatedInterpolators.Add(cls);
                DynLeaderboardsPlugin.LogInfo($"Saved new best lap for {cls}: {newLapTime}.");
            }
        } else if (
            lastPosRaw
            < firstPosRaw // if spline pos is offset, last pos must be smaller than first (lap end must be before lap start)
            && (firstPosRaw > 0.9 || lastPosRaw < 0.1) // sanity check for offset spline pos
            && Math.Abs(lastPosRaw - firstPosRaw) < 0.05 // make sure we completed whole lap
        ) {
            DynLeaderboardsPlugin.LogWarn(
                $"Possible missing lap offset detected: {this.Id} - {cls}. FirstPos: {firstPosRaw}({firstPos}), LastPos: {lastPosRaw}({lastPos}). Suggested lap position offset is {1 - firstPosRaw}."
            );
            if (firstPosRaw > 0.9) {
                // lap time resets before spline pos
                // example: lap starts at 0.99, so offset is 1 - 0.99 = 0.01. Thus, new lap starts at 0.0.
                this.SplinePosOffset.Update(1 - firstPosRaw);
            } else if (lastPosRaw < 0.1) {
                // lap time resets after spline pos
                // example: lap ends at 0.01, so offset is 1 - 0.01 = 0.99. Thus, new lap ends at 1.0. Start will be > 1.0, but we subtract 1 from it.
                this.SplinePosOffset.Update(1 - lastPosRaw);
            }
        } else {
            DynLeaderboardsPlugin.LogInfo(
                $"Collected invalid lap data for {this.Id} - {cls}. FirstPos: {firstPosRaw}({firstPos}), LastPos: {lastPosRaw}({lastPos})."
            );
        }
    }

    private static Dictionary<string, SplinePosOffset> ReadSplinePosOffsets(string gameName) {
        var path = $"{PluginSettings.PLUGIN_DATA_DIR}\\{gameName}\\SplinePosOffsets.json";
        if (File.Exists(path)) {
            return JsonConvert.DeserializeObject<Dictionary<string, SplinePosOffset>>(File.ReadAllText(path)) ?? [];
        }

        return [];
    }

    private void WriteSplinePosOffsets() {
        var path = $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\SplinePosOffsets.json";
        var dirPath = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dirPath);

        File.WriteAllText(path, JsonConvert.SerializeObject(TrackData._splinePosOffsets, Formatting.Indented));
    }

    internal void BuildLapInterpolator(CarClass carClass) {
        if (this.LapInterpolators.ContainsKey(carClass)) {
            return;
        }

        if (this._lapInterpolatorsInBuilding.Contains(carClass)) {
            return;
        }

        this._lapInterpolatorsInBuilding.Add(carClass);

        Task.Run(() => this.BuildLapInterpolatorInner(carClass));
    }

    private void BuildLapInterpolatorInner(CarClass carClass) {
        var path =
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\laps_data\\{this.Id}_{carClass}.txt";

        if (!File.Exists(path)) {
            DynLeaderboardsPlugin.LogInfo(
                $"Couldn't build lap interpolator for {carClass} because no suitable track data exists."
            );
            return;
        }

        try {
            DynLeaderboardsPlugin.LogInfo($"Build lap interpolator for {carClass} from file {path}");
            var interp = new LapInterpolator(path, this.SplinePosOffset.Value);
            this._builtLapInterpolators.Enqueue((carClass, interp));
        } catch (Exception ex) {
            DynLeaderboardsPlugin.LogError($"Failed to read {path} with error: {ex}");
        }
    }

    private void AddLapInterpolator(
        ReadOnlyCollection<double> rawPos,
        ReadOnlyCollection<double> rawTime,
        CarClass carClass
    ) {
        if (rawPos.Count != rawTime.Count) {
            DynLeaderboardsPlugin.LogError(
                $"Tried to add a lap interpolator where rawPos and rawTime have different lengths: {rawPos.Count} != {rawTime.Count}."
            );
            return;
        }

        this.LapInterpolators[carClass] = new LapInterpolator(rawPos, rawTime, this.SplinePosOffset.Value);
    }
}