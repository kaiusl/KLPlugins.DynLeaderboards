using KLPlugins.DynLeaderboards.Car;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KLPlugins.DynLeaderboards.Track {
    class LapInterpolator {
        public LinearSpline Interpolator { get; }
        public double LapTime { get; }

        public LapInterpolator(LinearSpline interpolator, double lapTime) {
            Interpolator = interpolator;
            LapTime = lapTime;
        }
    }

    class TrackData {
        public string TrackName { get; internal set; }
        public TrackType TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        internal static CarClassArray<LapInterpolator> LapInterpolators = null;
        public double SplinePosOffset => TrackId.SplinePosOffset();

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        internal static void ReadDefBestLaps() {
            LapInterpolators = new CarClassArray<LapInterpolator>(null);

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

        private static void AddLapInterpolator(CarClass cls) {
            var fname = $"{DynLeaderboardsPlugin.Settings.PluginDataLocation}\\laps_data\\{Values.TrackData.TrackId}_{cls}.txt";
            if (!File.Exists(fname)) {
                DynLeaderboardsPlugin.LogWarn($"Couldn't build lap interpolator for {cls} because no suitable track data exists.");
                return;
            }

            try {
                var data = ReadLapInterpolatorData(fname);
                LapInterpolators[cls] = new LapInterpolator(LinearSpline.InterpolateSorted(data.Item1, data.Item2), data.Item2.Last());
                DynLeaderboardsPlugin.LogInfo($"Build lap interpolator for {cls} from file {fname}");
            } catch (Exception ex) {
                DynLeaderboardsPlugin.LogError($"Failed to read {fname} with error: {ex}");
            }
        }

        private static Tuple<double[], double[]> ReadLapInterpolatorData(string fname) {
            var pos = new List<double>();
            var time = new List<double>();
            foreach (var l in File.ReadLines(fname)) {
                if (l == "") continue;
                // Data order: splinePositions, lap time in ms, speed in kmh
                var splits = l.Split(';');
                double p = float.Parse(splits[0]) + Values.TrackData.TrackId.SplinePosOffset();
                if (p > 1) {
                    p -= 1;
                }

                var t = double.Parse(splits[1]);
                pos.Add(p);
                time.Add(t);
            }
            return new Tuple<double[], double[]>(pos.ToArray(), time.ToArray());
        }

        private static void SetReplacements(CarClass cls, CarClass[] replacements) {
            if (LapInterpolators[cls] == null) {
                foreach (var r in replacements) {
                    if (LapInterpolators[r] != null) {
                        LapInterpolators[cls] = LapInterpolators[r];
                        DynLeaderboardsPlugin.LogInfo($"Found replacement lap interpolator for {cls} from {r}");
                        return;
                    }
                }
                DynLeaderboardsPlugin.LogWarn($"Couldn't find replacement lap interpolator for {cls}. Will have to use simple gap calculation.");
            }
        }
    }
}
