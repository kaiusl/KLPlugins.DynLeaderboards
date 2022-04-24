using KLPlugins.DynLeaderboards.Enums;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WoteverCommon;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards {

    /// <summary>
    /// Contains all the wanted properties
    /// </summary>
    public class OutProperties {
        public OutCarProp Car = OutCarProp.None;
        public OutPitProp Pit = OutPitProp.None;
        public OutPosProp Pos = OutPosProp.None;
        public OutGapProp Gap = OutGapProp.None;
        public OutStintProp Stint = OutStintProp.None;
        public OutDriverProp Driver = OutDriverProp.None;
        public OutLapProp Lap = OutLapProp.None;

        public static OutProperties AccDynLeaderboardProperties() {
            return new OutProperties {
                Car = OutCarProp.CarNumber
                    | OutCarProp.CarClass
                    | OutCarProp.IsFinished
                    | OutCarProp.CarClassColor
                    | OutCarProp.TeamCupCategoryColor
                    | OutCarProp.TeamCupCategoryTextColor,
                Pit = OutPitProp.IsInPitLane,
                Pos = OutPosProp.OverallPosition | OutPosProp.ClassPosition,
                Gap = OutGapProp.DynamicGapToFocused,
                Stint = OutStintProp.None,
                Driver = OutDriverProp.InitialPlusLastName,
                Lap = OutLapProp.Laps
                    | OutLapProp.LastLapTime
                    | OutLapProp.BestLapTime
                    | OutLapProp.DynamicBestLapDeltaToFocusedBest
                    | OutLapProp.DynamicLastLapDeltaToFocusedLast
            };
        }
    }

    /// <summary>
    /// Contains all the numbers of wanted items
    /// </summary>
    public class NumberOfItems {
        public int OverallPos { get; set; } = 16;
        public int ClassPos { get; set; } = 16;
        public int OnTrackRelativePos { get; set; } = 5;
        public int OverallRelativePos { get; set; } = 5;
        public int ClassRelativePos { get; set; } = 5;
        public int Drivers { get; set; } = 1;
        public int PartialRelativeOverall_OverallPos { get; set; } = 5;
        public int PartialRelativeOverall_RelativePos { get; set; } = 5;
        public int PartialRelativeClass_ClassPos { get; set; } = 5;
        public int PartialRelativeClass_RelativePos { get; set; } = 5;

        public NumberOfItems() { }
    }

    /// <summary>
    /// Configuration of single dynamic leaderboard
    /// </summary>
    public class DynLeaderboardConfig {
        public bool IsSetInSimHub { get; internal set; }
        public string Name { get; internal set; }
        public OutProperties OutProps { get; internal set; }
        public NumberOfItems NumItems { get; set; } // Needs to be public set as it doesn't work with TwoWay WPF binding otherwise
        public int CurrentLeaderboardIdx { get; internal set; } = 0;

        public List<Leaderboard> Order { get; internal set; } = new List<Leaderboard>() {
                Leaderboard.Overall,
                Leaderboard.Class,
                Leaderboard.PartialRelativeOverall,
                Leaderboard.PartialRelativeClass,
                Leaderboard.RelativeOverall,
                Leaderboard.RelativeClass,
                Leaderboard.RelativeOnTrack
            };

        public Leaderboard CurrentLeaderboard() {
            if (Order.Count > 0) {
                return Order[CurrentLeaderboardIdx];
            } else {
                return Leaderboard.None;
            }
        }

        internal static DynLeaderboardConfig AccDynLeaderboardConfig() {
            return new DynLeaderboardConfig(
                "Dynamic",
                OutProperties.AccDynLeaderboardProperties(),
                new NumberOfItems(),
                new List<Leaderboard> {   Leaderboard.Overall,
                    Leaderboard.Class,
                    Leaderboard.PartialRelativeOverall,
                    Leaderboard.PartialRelativeClass,
                    Leaderboard.RelativeOverall,
                    Leaderboard.RelativeClass,
                    Leaderboard.RelativeOnTrack},
                true);
        }

        internal DynLeaderboardConfig(string name) : this(
                name,
                new OutProperties(),
                new NumberOfItems(),
                new List<Leaderboard> {   Leaderboard.Overall,
                    Leaderboard.Class,
                    Leaderboard.PartialRelativeOverall,
                    Leaderboard.PartialRelativeClass,
                    Leaderboard.RelativeOverall,
                    Leaderboard.RelativeClass,
                    Leaderboard.RelativeOnTrack},
                true
        ) {}


        public DynLeaderboardConfig(string name, OutProperties outProps, NumberOfItems numItems, List<Leaderboard> order, bool isSetInSimHub = false) {
            Name = name;
            OutProps = outProps;
            NumItems = numItems;
            Order = order;
            IsSetInSimHub = isSetInSimHub;
        }

    }


    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings {
        public int Version { get { return _version; } }
        internal string PluginDataLocation { get; set; }
        public string AccDataLocation { get; internal set; }
        public bool Log { get; internal set; }
        public int BroadcastDataUpdateRateMs { get; set; } // Needs to be public set as it doesn't work with TwoWay WPF binding otherwise
        public List<DynLeaderboardConfig> DynLeaderboardConfigs { get; internal set; }
        public OutGeneralProp OutGeneralProps = OutGeneralProp.None;

        public Dictionary<CarClass, string> CarClassColors { get; internal set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryColors { get; internal set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryTextColors { get; internal set; }
        public Dictionary<DriverCategory, string> DriverCategoryColors { get; internal set; }

        private const int _version = 2;
        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\DynLeaderboards";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private delegate JObject Migration(JObject o);
        private static Dictionary<string, Migration> _migrations = CreateMigrationsDict();
        private const string _settingsFileName = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";

        public PluginSettings() {
            PluginDataLocation = _defPluginsDataLocation;
            AccDataLocation = _defAccDataLocation;
            Log = false;
            BroadcastDataUpdateRateMs = 200;
            DynLeaderboardConfigs = new List<DynLeaderboardConfig>();
            CarClassColors = CreateDefCarClassColors();
            TeamCupCategoryColors = CreateDefCupColors();
            TeamCupCategoryTextColors = CreateDefCupTextColors();
            DriverCategoryColors = CreateDefDriverCategoryColors();
        }

        internal static PluginSettings ReadSettings() {
            if (!File.Exists(_settingsFileName)) return new PluginSettings();
            var json = File.ReadAllText(_settingsFileName);

            JObject o = JObject.Parse(json);
            int version = 1;
            if (o.ContainsKey("Version")) {
                version = (int)o["Version"];
            }

            if (version != _version) {
                for (int i = version + 1; i <= _version; i++) {
                    o = _migrations[$"{version}_{i}"](o);
                }                
            }
            return o.ToObject<PluginSettings>();
        }

        internal void Save(IPlugin plugin) {
            DynLeaderboardConfigs.RemoveAll(x => !x.IsSetInSimHub); // These are added dynamically, thus if we don't remove them, they will be added multiple times
            plugin.SaveCommonSettings("GeneralSettings", this);
        }

        public int GetMaxNumClassPos() {
            int max = 0;
            if (DynLeaderboardConfigs.Count > 0) {
                foreach (var v in DynLeaderboardConfigs) {
                    max = Math.Max(max, v.NumItems.ClassPos);
                }
            }
            return max;
        }

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


        internal bool SetAccDataLocation(string newLoc) {
            if (!Directory.Exists($"{newLoc}\\Config")) {
                if (Directory.Exists($"{_defAccDataLocation}\\Config")) {
                    AccDataLocation = _defAccDataLocation;
                    DynLeaderboardsPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                    return false;
                } else {
                    DynLeaderboardsPlugin.LogWarn("Set ACC data location doesn't exist. Please check your configuration file.");
                    return false;
                }
            } else {
                AccDataLocation = newLoc;
                return true;
            }
        }

        #region Migrations

        private static Dictionary<string, Migration> CreateMigrationsDict() {
            var res = new Dictionary<string, Migration>();
            res["1_2"] = Mig1To2;

            return res;
        }

        private static JObject Mig1To2(JObject o) {
            JObject res = JObject.FromObject(new {
                Version = 2,
                AccDataLocation = o["AccDataLocation"],
                Log = o["Log"],
                BroadcastDataUpdateRateMs = o["BroadcastDataUpdateRateMs"],
                DynLeaderboardConfigs =
                    from c in o["DynLeaderboardConfigs"].Children().ToList()
                    select new {
                        Name = c["Name"],
                        OutProps = new {
                            Car = c["OutCarProps"],
                            Pit = c["OutPitProps"],
                            Pos = c["OutPosProps"],
                            Gap = c["OutGapProps"],
                            Stint = c["OutStintProps"],
                            Driver = c["OutDriverProps"],
                            Lap = c["OutLapProps"]
                        },
                        NumItems = new {
                            OverallPos = c["NumOverallPos"],
                            ClassPos = c["NumClassPos"],
                            OnTrackRelativePos = c["NumOnTrackRelativePos"],
                            OverallRelativePos = c["NumOverallRelativePos"],
                            ClassRelativePos = c["NumClassRelativePos"],
                            Drivers = c["NumDrivers"],
                            PartialRelativeOverall_OverallPos = c["PartialRelativeOverallNumOverallPos"],
                            PartialRelativeOverall_RelativePos = c["PartialRelativeOverallNumRelativePos"],
                            PartialRelativeClass_ClassPos = c["PartialRelativeClassNumClassPos"],
                            PartialRelativeClass_RelativePos = c["PartialRelativeClassNumRelativePos"],
                        },
                        CurrentLeaderboardIdx = c["CurrentLeaderboardIdx"],
                        Order = c["Order"],
                        IsSetInSimHub = true
                    },
                OutGeneralProps = o["OutGeneralProps"],
                CarClassColors = o["CarClassColors"],
                TeamCupCategoryColors = o["TeamCupCategoryColors"],
                TeamCupCategoryTextColors = o["TeamCupCategoryTextColors"],
                DriverCategoryColors = o["DriverCategoryColors"]
            });

            SimHub.Logging.Current.Info($"Migrated settings from 1 to 2.");

            return res;
        }

        #endregion
    }



}