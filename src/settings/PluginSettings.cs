using KLPlugins.Leaderboard.Enums;
using KLPlugins.Leaderboard.ksBroadcastingNetwork;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public int NumOnTrackRelativePos { get; set; } = _defNumRelativePos;
        public int NumOverallRelativePos { get; set; } = _defNumRelativePos;
        public int NumDrivers { get; set; } = _defNumDrivers;
        public int BroadcastDataUpdateRateMs { get; set; } = _defUpdateInterval;
        public int PartialRelativeNumOverallPos { get; set; } = 5;
        public int PartialRelativeNumRelativePos { get; set; } = 5;

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

        public Dictionary<CarClass, string> CarClassColors { get; set; } = CreateDefCarClassColors();
        public Dictionary<CupCategory, string> CupColors { get; set; } = CreateDefCupColors();
        public Dictionary<CupCategory, string> CupTextColors { get; set; } = CreateDefCupTextColors();


        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\Leaderboard";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private const int _defNumOverallPos = 30;
        private const int _defNumRelativePos = 5;
        private const int _defUpdateInterval = 1000;
        private const int _updateIntervalMax = 5000;
        private const int _updateIntevalMin = 50;
        private const int _defNumDrivers = 4;


        private static Dictionary<CarClass, string> CreateDefCarClassColors() { 
            var carClassColors = new Dictionary<CarClass, string>(8);
            foreach (var c in Enum.GetValues(typeof(CarClass))) {
                var cls = (CarClass)c;
                if (cls == CarClass.Unknown || cls == CarClass.Overall) continue;
                carClassColors.Add(cls, cls.GetACCColor());
            }
            return carClassColors;
        }

        private static Dictionary<CupCategory, string> CreateDefCupColors() {
            var cupColors = new Dictionary<CupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(CupCategory))) {
                var cup = (CupCategory)c;
                cupColors.Add(cup, cup.GetACCColor());
            }
            return cupColors;
        }

        private static Dictionary<CupCategory, string> CreateDefCupTextColors() {
            var cupTextColors = new Dictionary<CupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(CupCategory))) {
                var cup = (CupCategory)c;
                cupTextColors.Add(cup, cup.GetACCTextColor());
            }
            return cupTextColors;
        }



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