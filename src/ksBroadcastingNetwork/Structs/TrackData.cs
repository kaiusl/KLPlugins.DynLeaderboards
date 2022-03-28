using KLPlugins.Leaderboard.Enums;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs
{
    public class TrackData {
        public string TrackName { get; internal set; }
        public int TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        public Dictionary<string, List<string>> CameraSets { get; internal set; }
        public IEnumerable<string> HUDPages { get; internal set; }
        public static Dictionary<CarClass, LinearSpline> LapInterpolators { get; private set; }

        /// <summary>
        /// Read default lap data for calculation of gaps.
        /// </summary>
        public static void ReadDefBestLaps() {
            if (LapInterpolators != null) return;

            LapInterpolators = new Dictionary<CarClass, LinearSpline>();
            AddLapInterpolator(CarClass.GT3, new CarClass[] { });
            AddLapInterpolator(CarClass.GT4, new CarClass[] { });
            AddLapInterpolator(CarClass.TCX, new CarClass[] { });
            AddLapInterpolator(CarClass.CUP21, new CarClass[] { CarClass.CUP17, CarClass.ST21, CarClass.ST15, CarClass.CHL });
            AddLapInterpolator(CarClass.CUP17, new CarClass[] { CarClass.CUP21, CarClass.ST21, CarClass.ST15, CarClass.CHL });
            AddLapInterpolator(CarClass.ST15, new CarClass[] { CarClass.ST21, CarClass.CUP17, CarClass.CUP21, CarClass.CHL });
            AddLapInterpolator(CarClass.ST21, new CarClass[] { CarClass.ST15, CarClass.CUP17, CarClass.CUP21, CarClass.CHL });
            AddLapInterpolator(CarClass.CHL, new CarClass[] { CarClass.ST21, CarClass.CUP21, CarClass.CUP17,  CarClass.ST15 });
        }


        private static void AddLapInterpolator(CarClass cls, CarClass[] replacements) {
            var fname = $"{LeaderboardPlugin.Settings.PluginDataLocation}\\laps\\{Values.TrackData.TrackId}_{cls}.txt";
            if (!File.Exists(fname)) {
                foreach (var replacement in replacements) {
                    fname = $"{LeaderboardPlugin.Settings.PluginDataLocation}\\laps\\{Values.TrackData.TrackId}_{replacement}.txt";
                    if (File.Exists(fname)) {
                        break;
                    }
                }

                if (!File.Exists(fname)) return; 
            }

            var pos = new List<double>();
            var time = new List<double>();

            foreach (var l in File.ReadLines(fname)) {
                var splits = l.Split(';');
                double p = float.Parse(splits[0]);
                var t = double.Parse(splits[1]) / 1000.0;
                pos.Add(p);
                time.Add(t);
            }

            LapInterpolators[cls] = LinearSpline.InterpolateSorted(pos.ToArray(), time.ToArray());
        }

    }
}
