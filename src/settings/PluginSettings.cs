using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;


namespace KLPlugins.Leaderboard {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings {
        internal string PluginDataLocation { get; set; } = _defPluginsDataLocation;
        public string AccDataLocation { get; set; } = _defAccDataLocation;
        public bool Log { get; set; } = false;
        public int NumOverallPos { get; set; } = _defNumOverallPos;
        public int NumRelativePos { get; set; } = _defNumRelativePos;
        public int NumDrivers { get; set; } = _defNumDrivers;
        public int BroadcastDataUpdateRateMs { get; set; } = _defUpdateInterval;

        public OutCarProp OutCarProps;
        public OutPitProp OutPitProps;
        public OutPosProp OutPosProps;
        public OutGapProp OutGapProps;
        public OutDistanceProp OutDistanceProps;
        public OutStintProp OutStintProps;
        public OutDriverProp OutDriverProps;
        public OutOrder OutOrders;
        public OutGeneralProp OutGeneralProps;
        public OutLapProp OutLapProps;

        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\Leaderboard";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private const int _defNumOverallPos = 30;
        private const int _defNumRelativePos = 5;
        private const int _defUpdateInterval = 1000;
        private const int _updateIntervalMax = 5000;
        private const int _updateIntevalMin = 50;
        private const int _defNumDrivers = 4;


       
        public bool SetAccDataLocation(string newLoc) {
            if (!Directory.Exists($"{newLoc}\\Config")) {
                if (Directory.Exists($"{_defAccDataLocation}\\Config")) {
                    AccDataLocation = _defAccDataLocation;
                    LeaderboardPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                    return false;
                } else {
                    LeaderboardPlugin.LogWarn("Set ACC data location doesn't exist. Please check your configuration file.");
                    return false;
                }
            } else {
                AccDataLocation = newLoc;
                return true;
            }
        }
    }

   

}