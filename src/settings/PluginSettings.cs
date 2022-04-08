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
        public int NumClassRelativePos { get; set; } = _defNumRelativePos;
        public int NumDrivers { get; set; } = _defNumDrivers;
        public int BroadcastDataUpdateRateMs { get; set; } = _defUpdateInterval;
        public int PartialRelativeOverallNumOverallPos { get; set; } = _defNumRelativePos;
        public int PartialRelativeOverallNumRelativePos { get; set; } = _defNumRelativePos;
        public int PartialRelativeClassNumClassPos { get; set; } = _defNumRelativePos;
        public int PartialRelativeClassNumRelativePos { get; set; } = _defNumRelativePos;

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
        public Dictionary<TeamCupCategory, string> TeamCupCategoryColors { get; set; } = CreateDefCupColors();
        public Dictionary<TeamCupCategory, string> TeamCupCategoryTextColors { get; set; } = CreateDefCupTextColors();
        public Dictionary<DriverCategory, string> DriverCategoryColors { get; set; } = CreateDefDriverCategoryColors();

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

        private static Dictionary<TeamCupCategory, string> CreateDefCupColors() {
            var cupColors = new Dictionary<TeamCupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                var cup = (TeamCupCategory)c;
                cupColors.Add(cup, cup.GetACCColor());
            }
            return cupColors;
        }

        private static Dictionary<TeamCupCategory, string> CreateDefCupTextColors() {
            var cupTextColors = new Dictionary<TeamCupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                var cup = (TeamCupCategory)c;
                cupTextColors.Add(cup, cup.GetACCTextColor());
            }
            return cupTextColors;
        }

        private static Dictionary<DriverCategory, string> CreateDefDriverCategoryColors() { 
            var dict = new Dictionary<DriverCategory, string>(4);
            foreach (var c in Enum.GetValues(typeof(DriverCategory))) { 
                var cat = (DriverCategory)c;
                if (cat == DriverCategory.Error) continue;
                dict.Add(cat, cat.GetAccColor());
            }
            return dict;
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