using System;


namespace KLPlugins.DynLeaderboards {


    [Flags]
    public enum OutCarProp {
        None = 0,

        CarNumber = 1 << 0,
        CarModel = 1 << 1,
        CarManufacturer = 1 << 2,
        CarClass = 1 << 3,
        TeamName = 1 << 4,
        TeamCupCategory = 1 << 5,
        CarClassColor = 1 << 6,
        TeamCupCategoryColor = 1 << 7,
        TeamCupCategoryTextColor = 1 << 8,

        IsFinished = 1 << 9,
        MaxSpeed = 1 << 10,
        IsFocused = 1 << 11,
    }

    static class OutCarPropExtensions {
        public static bool Includes(this OutCarProp p, OutCarProp o) => (p & o) != 0;
        public static void Combine(ref this OutCarProp p, OutCarProp o) => p |= o;
        public static void Remove(ref this OutCarProp p, OutCarProp o) => p &= ~o;

        public static string ToPropName(this OutCarProp p) {
            switch (p) {
               case OutCarProp.CarNumber:
                    return "Car.Number";
                case OutCarProp.CarModel:
                    return "Car.Model";
                case OutCarProp.CarManufacturer:
                    return "Car.Manufacturer";
                case OutCarProp.CarClass:
                    return "Car.Class";
                case OutCarProp.TeamName:
                    return "Team.Name";
                case OutCarProp.TeamCupCategory:
                    return "Team.CupCategory";
                case OutCarProp.IsFinished:
                    return "IsFinished";
                case OutCarProp.MaxSpeed:
                    return "MaxSpeed";
                case OutCarProp.CarClassColor:
                    return "Car.Class.Color";
                case OutCarProp.TeamCupCategoryColor:
                    return "Team.CupCategory.Color";
                case OutCarProp.TeamCupCategoryTextColor:
                    return "Team.CupCategory.TextColor";
                case OutCarProp.IsFocused:
                    return "IsFocused";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }

        public static string ToolTipText(this OutCarProp p) {
            switch (p) {
                case OutCarProp.None:
                    return "None";
                case OutCarProp.CarNumber:
                    return "Car number.";
                case OutCarProp.CarModel:
                    return "Car model name.";
                case OutCarProp.CarManufacturer:
                    return "Car manufacturer.";
                case OutCarProp.CarClass:
                    return "Car class (GT3, GT4, ST15, ST21, CHL, CUP17, CUP21, TCX).";
                case OutCarProp.TeamName:
                    return "Team name.";
                case OutCarProp.TeamCupCategory:
                    return "Team cup category (Overall/Pro, ProAm, Am, Silver, National).";
                case OutCarProp.IsFinished:
                    return "Is the car finished?";
                case OutCarProp.MaxSpeed:
                    return "Maximum speed in this session.";
                case OutCarProp.CarClassColor:
                    return "Car class color. Values can be changed in \"General settings\" tab.";
                case OutCarProp.TeamCupCategoryColor:
                    return "Team cup category background color. Values can be changed in \"General settings\" tab..";
                case OutCarProp.TeamCupCategoryTextColor:
                    return "Team cup category text color. Values can be changed in \"General settings\" tab..";
                case OutCarProp.IsFocused:
                    return "Is this the focused car?";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }


    [Flags]
    public enum OutPitProp {
        None = 0,
        IsInPitLane = 1 << 0,
        PitStopCount = 1 << 1,
        PitTimeTotal = 1 << 2,
        PitTimeLast = 1 << 3,
        PitTimeCurrent = 1 << 4,
    }


    static class OutPitPropExtensions {
        public static bool Includes(this OutPitProp p, OutPitProp o) => (p & o) != 0;
        public static void Combine(ref this OutPitProp p, OutPitProp o) => p |= o;
        public static void Remove(ref this OutPitProp p, OutPitProp o) => p &= ~o;

        public static string ToPropName(this OutPitProp p) {
            switch (p) {
                case OutPitProp.IsInPitLane:
                    return "Pit.IsIn";
                case OutPitProp.PitStopCount:
                    return "Pit.Count";
                case OutPitProp.PitTimeTotal:
                    return "Pit.Time.Total";
                case OutPitProp.PitTimeLast:
                    return "Pit.Time.Last";
                case OutPitProp.PitTimeCurrent:
                    return "Pit.Time.Current";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }

        public static string ToolTipText(this OutPitProp p) {
            switch (p) {
                case OutPitProp.IsInPitLane:
                    return "Is the car in pit lane?";
                case OutPitProp.PitStopCount:
                    return "Number of pitstops.";
                case OutPitProp.PitTimeTotal:
                    return "Total time spent in pits.";
                case OutPitProp.PitTimeLast:
                    return "Last pit time.";
                case OutPitProp.PitTimeCurrent:
                    return "Current time in pits.";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }


    [Flags]
    public enum OutPosProp {
        None = 0,
        OverallPosition = 1 << 0,
        OverallPositionStart = 1 << 1,
        ClassPosition = 1 << 2,
        ClassPositionStart = 1 << 3,
    }

    static class OutPosPropExtensions {
        public static bool Includes(this OutPosProp p, OutPosProp o) => (p & o) != 0;
        public static bool IncludesAny(this OutPosProp p, params OutPosProp[] others) {
            foreach (var o in others) {
                if (p.Includes(o)) {
                    return true;
                }
            }
            return false;
        }
        public static bool IncludesAll(this OutPosProp p, params OutPosProp[] others) {
            foreach (var o in others) {
                if (!p.Includes(o)) {
                    return false;
                }
            }
            return true;
        }


        public static void Combine(ref this OutPosProp p, OutPosProp o) => p |= o;
        public static void Remove(ref this OutPosProp p, OutPosProp o) => p &= ~o;

        public static string ToPropName(this OutPosProp p) {
            switch (p) {
                case OutPosProp.ClassPosition:
                    return "Position.Class";
                case OutPosProp.ClassPositionStart:
                    return "Position.Class.Start";
                case OutPosProp.OverallPosition:
                    return "Position.Overall";
                case OutPosProp.OverallPositionStart:
                    return "Position.Overall.Start";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }

        public static string ToolTipText(this OutPosProp p) {
            switch (p) {
                case OutPosProp.ClassPosition:
                    return "Current class position";
                case OutPosProp.OverallPosition:
                    return "Current overall position";
                case OutPosProp.ClassPositionStart:
                    return "Class position at race start";
                case OutPosProp.OverallPositionStart:
                    return "Overall position at race start";

                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }


    [Flags]
    public enum OutGapProp {
        None = 0,
        GapToLeader = 1 << 0,
        GapToClassLeader = 1 << 1,
        GapToFocusedTotal = 1 << 2,
        GapToFocusedOnTrack = 1 << 3,
        GapToAheadOverall = 1 << 4,
        GapToAheadInClass = 1 << 5,
        GapToAheadOnTrack = 1 << 6,
        DynamicGapToFocused = 1 << 20,
        DynamicGapToAhead = 1 << 21,
    }

    static class OutGapPropExtensions {
        public static bool Includes(this OutGapProp p, OutGapProp o) => (p & o) != 0;
        public static void Combine(ref this OutGapProp p, OutGapProp o) => p |= o;
        public static void Remove(ref this OutGapProp p, OutGapProp o) => p &= ~o;

        public static string ToPropName(this OutGapProp p) {
            switch (p) {
                 case OutGapProp.GapToLeader:
                    return "Gap.ToOverallLeader";
                case OutGapProp.GapToClassLeader:
                    return "Gap.ToClassLeader";
                case OutGapProp.GapToFocusedTotal:
                    return "Gap.ToFocused.Total";
                case OutGapProp.GapToFocusedOnTrack:
                    return "Gap.ToFocused.OnTrack";
                case OutGapProp.GapToAheadOverall:
                    return "Gap.ToAhead.Overall";
                case OutGapProp.GapToAheadInClass:
                    return "Gap.ToAhead.Class";
                case OutGapProp.GapToAheadOnTrack:
                    return "Gap.ToAhead.OnTrack";
                case OutGapProp.DynamicGapToFocused:
                    return "Gap.Dynamic.ToFocused";
                case OutGapProp.DynamicGapToAhead:
                    return "Gap.Dynamic.ToAhead";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }

        public static string ToolTipText(this OutGapProp p) {
            switch (p) {
                case OutGapProp.GapToLeader:
                    return "Total gap to the leader.";
                case OutGapProp.GapToClassLeader:
                    return "Total gap to the class leader.";
                case OutGapProp.GapToFocusedTotal:
                    return "Total gap to the focused car.";
                case OutGapProp.GapToFocusedOnTrack:
                    return "On track gap to the focused car.";
                case OutGapProp.GapToAheadOverall:
                    return "Total gap to the car ahead in overall.";
                case OutGapProp.GapToAheadInClass:
                    return "Total gap to the car ahead in class.";
                case OutGapProp.GapToAheadOnTrack:
                    return "Relative on track gap to car ahead.";
                case OutGapProp.DynamicGapToFocused:
                    return @"Gap that changes based of currently displayed dynamic leaderboard. 
Overall -> gap to leader, 
Class -> gap to class leader, 
PartialRelativeOverall/PartialRelativeClass/RelativePverall/RelativeClass -> gap to focused total, 
RelativeOnTrack -> gap to focused on track.";
                case OutGapProp.DynamicGapToAhead:
                    return @"Gap to the car ahead that changes based on the currently displayed dynamic leaderboard. 
Overall/RelativeOverall/PartialRelativeOverall -> gap to ahead in overall order, 
Class/RelativeClass/PartialRelativeClass -> gap to ahead in class order, 
RelativeOnTrack -> gap to ahead on track.";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }


    [Flags]
    public enum OutStintProp {
        None = 0,
        CurrentStintTime = 1 << 0,
        CurrentStintLaps = 1 << 1,
        LastStintTime = 1 << 2,
        LastStintLaps = 1 << 3,
    }

    static class OutStintPropExtensions {
        public static bool Includes(this OutStintProp p, OutStintProp o) => (p & o) != 0;
        public static void Combine(ref this OutStintProp p, OutStintProp o) => p |= o;
        public static void Remove(ref this OutStintProp p, OutStintProp o) => p &= ~o;

        public static string ToPropName(this OutStintProp p) {
            switch (p) {
                case OutStintProp.CurrentStintTime:
                    return "Stint.Current.Time";
                case OutStintProp.CurrentStintLaps:
                    return "Stint.Current.Laps";
                case OutStintProp.LastStintTime:
                    return "Stint.Last.Time";
                case OutStintProp.LastStintLaps:
                    return "Stint.Last.Laps";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }

        public static string ToolTipText(this OutStintProp p) {
            switch (p) {
                case OutStintProp.CurrentStintTime:
                    return "Current stint time.";
                case OutStintProp.LastStintTime:
                    return "Last stint time.";
                case OutStintProp.CurrentStintLaps:
                    return "Number of laps completed in current stint";
                case OutStintProp.LastStintLaps:
                    return "Number of laps completed in last stint";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }

}