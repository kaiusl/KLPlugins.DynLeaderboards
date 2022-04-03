using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace KLPlugins.Leaderboard {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings {
        public int SpeedWarningLevel = 100;
    }

    public class Settings {
        public string PluginDataLocation { get; set; }
        public string AccDataLocation { get; set; }
        public bool Log { get; set; }
        public int NumOverallPos { get; set; }
        public int NumRelativePos { get; set; }
        public int NumDrivers { get; set; }
        public int BroadcastDataUpdateRateMs { get; set; }
        public string[] Properties { get; set; }
        public string[] DriverProperties { get; set; }

        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\Leaderboard";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private const int _defNumOverallPos = 30;
        private const int _defNumRelativePos = 5;
        private const int _defUpdateInterval = 1000;
        private const int _updateIntervalMax = 5000;
        private const int _updateIntevalMin = 50;
        private const int _defNumDrivers = 4;

        internal ExposedProperties ExposedProperties => _exposedProperties;
        private ExposedProperties _exposedProperties;

        internal ExposedDriverProperties ExposedDriverProperties => _exposedDriverProperties;
        private ExposedDriverProperties _exposedDriverProperties;

        public Settings() {
            PluginDataLocation = _defPluginsDataLocation;
            AccDataLocation = _defAccDataLocation;
            Log = true;
            NumOverallPos = _defNumOverallPos;
            NumRelativePos = _defNumRelativePos;
            NumDrivers = _defNumDrivers;
            BroadcastDataUpdateRateMs = _defUpdateInterval;
            Properties = ((ExposedProperties[])Enum.GetValues(typeof(ExposedProperties))).Select(x => x.ToString()).ToArray();
            DriverProperties = ((ExposedDriverProperties[])Enum.GetValues(typeof(ExposedDriverProperties))).Select(x => x.ToString()).ToArray();
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
            if (NumDrivers < 0) { 
                NumDrivers = _defNumDrivers;
                LeaderboardPlugin.LogWarn($"Set number of drivers is negative. Must be positive, will use default {_defNumDrivers} for now.");
            }

            if (!Directory.Exists(AccDataLocation)) {
                if (Directory.Exists(_defAccDataLocation)) {
                    AccDataLocation = _defAccDataLocation;
                    LeaderboardPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                } else {
                    LeaderboardPlugin.LogWarn("Set ACC data location doesn't exist. Please check your configuration file.");
                }
            }

            foreach (var v in Properties) {
                if (Enum.TryParse(v, out ExposedProperties newVariant)) {
                    _exposedProperties |= newVariant;
                } else {
                    LeaderboardPlugin.LogWarn($"Found unknown setting '{v}' in Properties");
                }
            }

            foreach (var v in DriverProperties) {
                if (Enum.TryParse(v, out ExposedDriverProperties newVariant)) {
                    _exposedDriverProperties |= newVariant;
                } else {
                    LeaderboardPlugin.LogWarn($"Found unknown setting '{v}' in DriverProperties");
                }
            }

        }

        private int Clamp(int v, int min, int max) {
            if (v < min) return min;
            else if (v > max) return max;
            return v;
        }
    }

    [Flags]
    public enum ExposedDriverProperties {
        None = 0,
        FirstName = 1 << 1,
        LastName = 1 << 2,
        ShortName = 1 << 3,
        FullName = 1 << 4,
        InitialPlusLastName = 1 << 5,
        Nationality = 1 << 6,
        Category = 1 << 7,
        TotalLaps = 1 << 8,
        TotalDrivingTime = 1 << 9,
        BestLap = 1 << 10,
    }


    [Flags]
    public enum ExposedProperties : long {
        None = 0,
        NumberOfLaps = 1L << 0,
        LastLap = 1L << 1,
        LastLapSectors = 1L << 2,
        BestLap = 1L << 3,
        BestLapSectors = 1L << 4,
        BestSectors = 1L << 5,
        CurrentLap = 1L << 6,

        CurrentDriver = 1L << 7,
        AllDrivers = 1L << 8,

        CarNumber = 1L << 9,
        CarModel = 1L << 10,
        CarManufacturer = 1L << 11,
        CarClass = 1L << 12,
        TeamName = 1L << 13,
        CupCategory = 1L << 14,

        CurrentDeltaToBest = 1L << 15,
        CurrentStintTime = 1L << 16,
        LastStintTime = 1L << 17,
        CurrentStintLaps = 1L << 18,
        LastStintLaps = 1L << 19,

        DistanceToLeader = 1L << 20,
        DistanceToClassLeader = 1L << 21,
        DistanceToFocusedTotal = 1L << 22,
        DistanceToFocusedOnTrack = 1L << 23,

        GapToLeader = 1L << 24,
        GapToClassLeader = 1L << 25,
        GapToFocusedTotal = 1L << 26,
        GapToFocusedOnTrack = 1L << 27,
        GapToAhead = 1L << 28,
        GapToAheadInClass = 1L << 29,

        ClassPosition = 1L << 30,
        OverallPosition = 1L << 31,
        ClassPositionStart = 1L << 32,
        OverallPositionStart = 1L << 33,

        IsInPitLane = 1L << 34,
        PitStopCount = 1L << 35,
        PitTimeTotal = 1L << 36,
        PitTimeLast = 1L << 37,
        PitTimeCurrent = 1L << 38,

        IsFinished = 1L << 39,

        InClassPositions = 1L << 40,
        RelativePositions = 1L << 41,
        FocusedCarPosition = 1L << 42,
        OverallBestLapPosition = 1L << 43,
        InClassBestLapPosition = 1L << 44,

        SessionPhase = 1L << 45,
        MaxStintTime = 1L << 46,
        MaxDriveTime = 1L << 47
    }

    static class FlagsExtensionMethods {

        public static bool Includes(this ExposedProperties p, ExposedProperties o) {
            return (p & o) != 0;
        }

        public static bool Includes(this ExposedDriverProperties p, ExposedDriverProperties o) {
            return (p & o) != 0;
        }
    }
}