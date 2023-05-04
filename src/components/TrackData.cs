using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KLPlugins.DynLeaderboards.Car;
using MathNet.Numerics.Interpolation;

namespace KLPlugins.DynLeaderboards.Track {
    internal class LapInterpolator {
        internal double LapTime { get; }
        private LinearSpline _interpolator { get; }

        internal LapInterpolator(LinearSpline interpolator, double lapTime) {
            _interpolator = interpolator;
            LapTime = lapTime;
        }

        internal double Interpolate(double splinePos) {
            return _interpolator.Interpolate(splinePos);
        }
    }

    internal class TrackData {
        public string Name { get; }
        public TrackType Id { get; }
        public float LengthMeters { get; }
        public double SplinePosOffset { get; }
        internal CarClassArray<LapInterpolator?>? LapInterpolators = null;

        internal TrackData(BinaryReader br) {
            _ = br.ReadInt32(); // connectionId
            Name = ksBroadcastingNetwork.BroadcastingNetworkProtocol.ReadString(br);
            Id = (TrackType)br.ReadInt32();
            LengthMeters = br.ReadInt32();
            SplinePosOffset = Id.SplinePosOffset();
        }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        internal void ReadDefBestLaps() {
            LapInterpolators = new CarClassArray<LapInterpolator?>(null);

            AddLapInterpolator(CarClass.GT3);
            AddLapInterpolator(CarClass.GT4);
            AddLapInterpolator(CarClass.TCX);
            AddLapInterpolator(CarClass.CUP21);
            AddLapInterpolator(CarClass.CUP17);
            AddLapInterpolator(CarClass.ST15);
            AddLapInterpolator(CarClass.ST21);
            AddLapInterpolator(CarClass.CHL);

            SetReplacements(CarClass.GT3, new CarClass[] { CarClass.CUP21, CarClass.ST21, CarClass.CUP17, CarClass.ST15, CarClass.CHL });
            SetReplacements(CarClass.CUP21, new CarClass[] { CarClass.CUP17, CarClass.ST21, CarClass.ST15, CarClass.CHL, CarClass.GT3 });
            SetReplacements(CarClass.CUP17, new CarClass[] { CarClass.CUP21, CarClass.ST21, CarClass.ST15, CarClass.CHL, CarClass.GT3 });
            SetReplacements(CarClass.ST21, new CarClass[] { CarClass.CUP21, CarClass.CUP17, CarClass.ST15, CarClass.CHL, CarClass.GT3 });
            SetReplacements(CarClass.ST15, new CarClass[] { CarClass.ST21, CarClass.CUP21, CarClass.CUP17, CarClass.CHL, CarClass.GT3 });
        }

        /// Assumes that `this.LapInterpolators != null`
        private void AddLapInterpolator(CarClass cls) {
            Debug.Assert(LapInterpolators != null);
            var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\laps_data\\{Id}_{cls}.txt";
            if (!File.Exists(fname)) {
                DynLeaderboardsPlugin.LogInfo($"Couldn't build lap interpolator for {cls} because no suitable track data exists.");
                return;
            }

            try {
                var data = ReadLapInterpolatorData(fname);
                LapInterpolators![cls] = new LapInterpolator(LinearSpline.InterpolateSorted(data.Item1.ToArray(), data.Item2.ToArray()), data.Item2.Last());
                DynLeaderboardsPlugin.LogInfo($"Build lap interpolator for {cls} from file {fname}");
            } catch (Exception ex) {
                DynLeaderboardsPlugin.LogError($"Failed to read {fname} with error: {ex}");
            }
        }

        private Tuple<List<double>, List<double>> ReadLapInterpolatorData(string fname) {
            // Default lap_data files have 200 data points
            var pos = new List<double>(200);
            var time = new List<double>(200);
            foreach (var l in File.ReadLines(fname)) {
                if (l == "")
                    continue;
                // Data order: splinePositions, lap time in ms, speed in kmh
                var splits = l.Split(';');
                double p = float.Parse(splits[0]) + SplinePosOffset;
                var t = double.Parse(splits[1]);
                pos.Add(p);
                time.Add(t);
            }
            return new Tuple<List<double>, List<double>>(pos, time);
        }

        /// Assumes that `this.LapInterpolators != null`
        private void SetReplacements(CarClass cls, CarClass[] replacements) {
            Debug.Assert(LapInterpolators != null);
            if (LapInterpolators![cls] != null) {
                return;
            }

            foreach (var r in replacements) {
                if (LapInterpolators[r] != null) {
                    LapInterpolators[cls] = LapInterpolators[r];
                    DynLeaderboardsPlugin.LogInfo($"Found replacement lap interpolator for {cls} from {r}");
                    return;
                }
            }
            DynLeaderboardsPlugin.LogError($"Couldn't find replacement lap interpolator for {cls}. Gaps cannot be calculated for this class.");
        }
    }
}