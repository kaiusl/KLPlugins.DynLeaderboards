using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace KLPlugins.Leaderboard {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings
    {
        public int SpeedWarningLevel = 100;
    }

    public class Settings {
        public string PluginDataLocation { get; set; }
        public string AccDataLocation { get; set; }
        public bool Log { get; set; }
        public int NumOverallPos { get; set; }
        public int NumRelativePos { get; set; }
        public int BroadcastDataUpdateRateMs { get; set; }

        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\Leaderboard";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private const int _defNumOverallPos = 30;
        private const int _defNumRelativePos = 5;
        private const int _defUpdateInterval = 1000;
        private const int _updateIntervalMax = 5000;
        private const int _updateIntevalMin = 50;

        public Settings() {
            PluginDataLocation = _defPluginsDataLocation;
            AccDataLocation = _defAccDataLocation;
            Log = true;
            NumOverallPos = _defNumOverallPos;
            NumRelativePos = _defNumRelativePos;
            BroadcastDataUpdateRateMs = _defUpdateInterval;
        }

        public void Validate() {
            BroadcastDataUpdateRateMs = Clamp(BroadcastDataUpdateRateMs, _updateIntevalMin, _updateIntervalMax);

            if (NumOverallPos < 0) {
                NumOverallPos = _defNumOverallPos;
                LeaderboardPlugin.LogWarn($"Set number of overall positions is negative. Must be positive, will use default {_defNumOverallPos}.");
            }
            if (NumRelativePos < 0) {
                NumRelativePos = 0;
                LeaderboardPlugin.LogWarn("Set number of relative positions is negative. Must be positive, will use 0 for now.");
            }

            if (!Directory.Exists(AccDataLocation)) {
                if (Directory.Exists(_defAccDataLocation)) {
                    AccDataLocation = _defAccDataLocation; 
                    LeaderboardPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                } else {
                    LeaderboardPlugin.LogWarn("Set ACC data location doesn't exist. Please check your configuration file.");
                }

            }
        }

        private int Clamp(int v, int min, int max) {
            if (v < min) return min;
            else if (v > max) return max;
            return v;
        }


    }
}