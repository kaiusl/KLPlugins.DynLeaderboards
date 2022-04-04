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

        public ExposedCarProperties ExposedCarProperties { get; set; }
        public ExposedDriverProperties ExposedDriverProperties { get; set; }
        public ExposedOrderings ExposedOrderings { get; set; }
        public ExposedGeneralProperties ExposedGeneralProperties { get; set; }

        private const string _defPluginsDataLocation = "PluginsData\\KLPlugins\\Leaderboard";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private const int _defNumOverallPos = 30;
        private const int _defNumRelativePos = 5;
        private const int _defUpdateInterval = 1000;
        private const int _updateIntervalMax = 5000;
        private const int _updateIntevalMin = 50;
        private const int _defNumDrivers = 4;
       

        public void AddExposedProperty(ExposedCarProperties newProp) { 
            ExposedCarProperties |= newProp;
        }

        public void RemoveExposedProperty(ExposedCarProperties oldProp) {
            ExposedCarProperties &= ~oldProp;
        }

        public void AddExposedDriverProperty(ExposedDriverProperties newProp) {
            ExposedDriverProperties |= newProp;
        }

        public void RemoveExposedDriverProperty(ExposedDriverProperties oldProp) {
            ExposedDriverProperties &= ~oldProp;
        }

        public void AddExposedOrdering(ExposedOrderings newProp) {
            ExposedOrderings |= newProp;
        }

        public void RemoveExposedOrdering(ExposedOrderings oldProp) {
            ExposedOrderings &= ~oldProp;
        }

        public void AddExposedGeneralProperty(ExposedGeneralProperties newProp) {
            ExposedGeneralProperties |= newProp;
        }

        public void RemoveExposedGeneralProperty(ExposedGeneralProperties oldProp) {
            ExposedGeneralProperties &= ~oldProp;
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
        BestLapTime = 1 << 10,
    }

    static class ExposedDriverPropertiesExtensions {

        public static bool Includes(this ExposedDriverProperties p, ExposedDriverProperties o) {
            return (p & o) != 0;
        }


        public static string ToolTipText(this ExposedDriverProperties p) {
            switch (p) {
                case ExposedDriverProperties.None:
                    return "None";
                case ExposedDriverProperties.FirstName:
                    return "First name (Abcde)";
                case ExposedDriverProperties.LastName:
                    return "Last name (Fghij)";
                case ExposedDriverProperties.ShortName:
                    return "Short name (AFG)";
                case ExposedDriverProperties.FullName:
                    return "Full name (Abcde Fghij)";
                case ExposedDriverProperties.InitialPlusLastName:
                    return "Initial + first name (A. Fghij)";
                case ExposedDriverProperties.Nationality:
                    return "Nationality";
                case ExposedDriverProperties.Category:
                    return "Driver category (Platinum, Gold, Silver, Bronze)";
                case ExposedDriverProperties.TotalLaps:
                    return "Total completed laps";
                case ExposedDriverProperties.TotalDrivingTime:
                    return "Total driving time in seconds";
                case ExposedDriverProperties.BestLapTime:
                    return "Best lap time in seconds";
                default:
                    return "Unknown";
            }
        }

    }


    [Flags]
    public enum ExposedCarProperties : long {
        None = 0,
        NumberOfLaps = 1L << 0,
        LastLapTime = 1L << 1,
        LastLapSectors = 1L << 2,
        BestLapTime = 1L << 3,
        BestLapSectors = 1L << 4,
        BestSectors = 1L << 5,
        CurrentLapTime = 1L << 6,
        CurrentLapDeltaToBest = 1L << 7,

        CurrentDriverInfo = 1L << 8,
        AllDriversInfo = 1L << 9,

        CarNumber = 1L << 10,
        CarModel = 1L << 11,
        CarManufacturer = 1L << 12,
        CarClass = 1L << 13,
        TeamName = 1L << 14,
        TeamCupCategory = 1L << 15,

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
        MaxSpeed = 1L << 40,
    }

    static class ExposedPropertiesExtensions {

        public static bool Includes(this ExposedCarProperties p, ExposedCarProperties o) {
            return (p & o) != 0;
        }

        public static string ToPropName(this ExposedCarProperties p) {
            switch (p) {
                case ExposedCarProperties.LastLapSectors:
                    return "LastLapS1/2/3";
                case ExposedCarProperties.BestLapSectors:
                    return "BestLapS1/2/3";
                case ExposedCarProperties.BestSectors:
                    return "BestS1/2/3";
                default:
                    return p.ToString();
            }
        }

        public static string ToolTipText(this ExposedCarProperties p) {
            switch (p) {
                case ExposedCarProperties.NumberOfLaps:
                    return "Number of laps completed";
                case ExposedCarProperties.LastLapTime:
                    return "Last lap time in seconds";
                case ExposedCarProperties.LastLapSectors:
                    return "Last lap sector times in seconds";
                case ExposedCarProperties.BestLapTime:
                    return "Best lap time in seconds";
                case ExposedCarProperties.BestLapSectors:
                    return "Best lap sector times in seconds";
                case ExposedCarProperties.BestSectors:
                    return "Best sector times in seconds";
                case ExposedCarProperties.CurrentLapTime:
                    return "Current lap time in seconds";
                case ExposedCarProperties.CurrentDriverInfo:
                    return "Current driver information";
                case ExposedCarProperties.AllDriversInfo:
                    return "Information of all drivers";
                case ExposedCarProperties.CarNumber:
                    return "Car number";
                case ExposedCarProperties.CarModel:
                    return "Car model name";
                case ExposedCarProperties.CarManufacturer:
                    return "Car manufacrurer";
                case ExposedCarProperties.CarClass:
                    return "Car class (GT3, GT4, ST15, ST21, CHL, CUP17, CUP21, TCX)";
                case ExposedCarProperties.TeamName:
                    return "Team name";
                case ExposedCarProperties.TeamCupCategory:
                    return "Team cup category (Overall/Pro, ProAm, Am, Silver, National)";
                case ExposedCarProperties.CurrentLapDeltaToBest:
                    return "Current lap time delta to best lap time in seconds";
                case ExposedCarProperties.CurrentStintTime:
                    return "Current stint time in seconds";
                case ExposedCarProperties.LastStintTime:
                    return "Last stint time in seconds";
                case ExposedCarProperties.CurrentStintLaps:
                    return "Number of laps completed in current stint";
                case ExposedCarProperties.LastStintLaps:
                    return "Number of laps completed in last stint";
                case ExposedCarProperties.DistanceToLeader:
                    return "Total distance to the leader in meters";
                case ExposedCarProperties.DistanceToClassLeader:
                    return "Total distance to the class leader in meters";
                case ExposedCarProperties.DistanceToFocusedTotal:
                    return "Total distance to the focused car in meters";
                case ExposedCarProperties.DistanceToFocusedOnTrack:
                    return "On track distance to the focused car in meters";
                case ExposedCarProperties.GapToLeader:
                    return "Total gap to the leader in seconds";
                case ExposedCarProperties.GapToClassLeader:
                    return "Total gap to the class leader in seconds";
                case ExposedCarProperties.GapToFocusedTotal:
                    return "Total gap to the focused car in seconds";
                case ExposedCarProperties.GapToFocusedOnTrack:
                    return "On track gap to the focused car in seconds";
                case ExposedCarProperties.GapToAhead:
                    return "Total gap to the car ahead in overall in seconds";
                case ExposedCarProperties.GapToAheadInClass:
                    return "Total gap to the car ahead in class in seconds";
                case ExposedCarProperties.ClassPosition:
                    return "Class position";
                case ExposedCarProperties.OverallPosition:
                    return "Overall position";
                case ExposedCarProperties.ClassPositionStart:
                    return "Class position at the race start";
                case ExposedCarProperties.OverallPositionStart:
                    return "Overall position at the race start";
                case ExposedCarProperties.IsInPitLane:
                    return "Is the car in pit lane?";
                case ExposedCarProperties.PitStopCount:
                    return "Number of pitstops";
                case ExposedCarProperties.PitTimeTotal:
                    return "Total time spent in pits in seconds";
                case ExposedCarProperties.PitTimeLast:
                    return "Last pit time in seconds";
                case ExposedCarProperties.PitTimeCurrent:
                    return "Current time in pits in seconds";
                case ExposedCarProperties.IsFinished:
                    return "Is the car finished?";
                default:
                    return "None";
            }
        }
    }

    [Flags]
    public enum ExposedGeneralProperties {
        None = 0,
        SessionPhase = 1 << 1,
        MaxStintTime = 1 << 2,
        MaxDriveTime = 1 << 3,
    }

    static class ExposedOrderingsExtensions {

        public static bool Includes(this ExposedGeneralProperties p, ExposedGeneralProperties o) {
            return (p & o) != 0;
        }

        public static string ToolTipText(this ExposedGeneralProperties p) {
            switch (p) {
                case ExposedGeneralProperties.SessionPhase:
                    return "Session phase.";
                case ExposedGeneralProperties.MaxStintTime:
                    return "Maximum driver stint time.";
                case ExposedGeneralProperties.MaxDriveTime:
                    return "Maximum total driving time for driver for player car. This can be different for other teams if they have different number of drivers.";
                default:
                    return "None";
            }
        }
    }

    [Flags]
    public enum ExposedOrderings {
        None = 0,
        InClassPositions = 1 << 1,
        RelativePositions = 1 << 2,
        FocusedCarPosition = 1 << 3,
        OverallBestLapPosition = 1 << 4,
        InClassBestLapPosition = 1 << 5,
    }

    static class ExposedGeneralPropertiesExtensions {

        public static bool Includes(this ExposedOrderings p, ExposedOrderings o) {
            return (p & o) != 0;
        }

        public static string ToPropName(this ExposedOrderings p) {
            switch (p) {
                case ExposedOrderings.InClassPositions:
                    return "InClass.xx.OverallPosition";
                case ExposedOrderings.RelativePositions:
                    return "Relative.xx.OverallPosition";
                case ExposedOrderings.FocusedCarPosition:
                    return "Focused.OverallPosition";
                case ExposedOrderings.OverallBestLapPosition:
                    return "Overall.BestLapCar.OverallPosition";
                case ExposedOrderings.InClassBestLapPosition:
                    return "InClass.BestLapCar.OverallPosition";
                default:
                    return "None";
            }
        }

        public static string ToolTipText(this ExposedOrderings p) {
            switch (p) {
                case ExposedOrderings.InClassPositions:
                    return @"Overall positions of cars in focused car's class. Used to create class leaderboards.
For car properties use JavaScript function ´InClass(pos, propname)´";
                case ExposedOrderings.RelativePositions:
                    return @"Overall positions of closest cars on track. Used to create relative leaderboards.
For car properties use JavaScript function  ´Relative(pos, propname)´";
                case ExposedOrderings.FocusedCarPosition:
                    return @"Overall position of focused car.
For car properties use JavaScript function ´Focused(propname)´";
                case ExposedOrderings.OverallBestLapPosition:
                    return @"Overall position of the overll best lap car.
For car properties use JavaScript function  ´OverallBestLapCar(propname)´.";
                case ExposedOrderings.InClassBestLapPosition:
                    return @"Overall position of the class best lap car. 
For car properties use JavaScript function  ´InClassBestLapCar(propname)´.";
                default:
                    return "None";
            }
        }
    }

}