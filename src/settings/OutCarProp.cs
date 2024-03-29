using System;

namespace KLPlugins.DynLeaderboards.Settings {

    [Flags]
    internal enum OutCarProp {
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
        IsOverallBestLapCar = 1 << 12,
        IsClassBestLapCar = 1 << 13,
        RelativeOnTrackLapDiff = 1 << 14,
        IsCupBestLapCar = 1 << 15,

        CarClassTextColor = 1 << 16,
    }

    internal static class OutCarPropExtensions {

        internal static bool Includes(this OutCarProp p, OutCarProp o) {
            return (p & o) != 0;
        }

        internal static void Combine(ref this OutCarProp p, OutCarProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutCarProp p, OutCarProp o) {
            p &= ~o;
        }

        internal static OutCarProp[] Order() {
            return new[] {
                 OutCarProp.CarNumber,
                 OutCarProp.CarModel,
                 OutCarProp.CarManufacturer,
                 OutCarProp.CarClass,
                 OutCarProp.TeamName,
                 OutCarProp.TeamCupCategory,
                 OutCarProp.CarClassColor,
                 OutCarProp.CarClassTextColor,
                 OutCarProp.TeamCupCategoryColor,
                 OutCarProp.TeamCupCategoryTextColor,
                 OutCarProp.IsFinished,
                 OutCarProp.MaxSpeed,
                 OutCarProp.IsFocused,
                 OutCarProp.IsOverallBestLapCar,
                 OutCarProp.IsClassBestLapCar,
                 OutCarProp.IsCupBestLapCar,
                 OutCarProp.RelativeOnTrackLapDiff,
             };
        }

        internal static string ToPropName(this OutCarProp p) {
            return p switch {
                OutCarProp.CarNumber => "Car.Number",
                OutCarProp.CarModel => "Car.Model",
                OutCarProp.CarManufacturer => "Car.Manufacturer",
                OutCarProp.CarClass => "Car.Class",
                OutCarProp.TeamName => "Team.Name",
                OutCarProp.TeamCupCategory => "Team.CupCategory",
                OutCarProp.IsFinished => "IsFinished",
                OutCarProp.MaxSpeed => "MaxSpeed",
                OutCarProp.CarClassColor => "Car.Class.Color",
                OutCarProp.CarClassTextColor => "Car.Class.TextColor",
                OutCarProp.TeamCupCategoryColor => "Team.CupCategory.Color",
                OutCarProp.TeamCupCategoryTextColor => "Team.CupCategory.TextColor",
                OutCarProp.IsFocused => "IsFocused",
                OutCarProp.IsOverallBestLapCar => "IsOverallBestLapCar",
                OutCarProp.IsClassBestLapCar => "IsClassBestLapCar",
                OutCarProp.RelativeOnTrackLapDiff => "RelativeOnTrackLapDiff",
                OutCarProp.IsCupBestLapCar => "IsCupBestLapCar",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutCarProp p) {
            return p switch {
                OutCarProp.None => "None",
                OutCarProp.CarNumber => "Car number.",
                OutCarProp.CarModel => "Car model name.",
                OutCarProp.CarManufacturer => "Car manufacturer.",
                OutCarProp.CarClass => "Car class (GT2, GT3, GT4, ST15, ST21, CHL, CUP17, CUP21, TCX).",
                OutCarProp.TeamName => "Team name.",
                OutCarProp.TeamCupCategory => "Team cup category (Overall/Pro, ProAm, Am, Silver, National).",
                OutCarProp.IsFinished => "Is the car finished?",
                OutCarProp.MaxSpeed => "Maximum speed in this session.",
                OutCarProp.CarClassColor => "Car class background color. Values can be changed in \"General settings\" tab.",
                OutCarProp.CarClassTextColor => "Car class text color. Values can be changed in \"General settings\" tab.",
                OutCarProp.TeamCupCategoryColor => "Team cup category background color. Values can be changed in \"General settings\" tab.",
                OutCarProp.TeamCupCategoryTextColor => "Team cup category text color. Values can be changed in \"General settings\" tab.",
                OutCarProp.IsFocused => "Is this the focused car?",
                OutCarProp.IsOverallBestLapCar => "Is this the car that has overall best lap?",
                OutCarProp.IsClassBestLapCar => "Is this the car that has class best lap?",
                OutCarProp.RelativeOnTrackLapDiff => "Show if this car is ahead or behind by the lap on the relative on track. 1: this car is ahead by a lap, 0: same lap, -1: this car is behind by a lap.",
                OutCarProp.IsCupBestLapCar => "Is this the car that has cup best lap?",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [Flags]
    internal enum OutPitProp {
        None = 0,
        IsInPitLane = 1 << 0,
        PitStopCount = 1 << 1,
        PitTimeTotal = 1 << 2,
        PitTimeLast = 1 << 3,
        PitTimeCurrent = 1 << 4,
    }

    internal static class OutPitPropExtensions {

        internal static bool Includes(this OutPitProp p, OutPitProp o) {
            return (p & o) != 0;
        }

        internal static void Combine(ref this OutPitProp p, OutPitProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutPitProp p, OutPitProp o) {
            p &= ~o;
        }

        internal static OutPitProp[] Order() {
            return new[] {
                OutPitProp.IsInPitLane,
                OutPitProp.PitStopCount,
                OutPitProp.PitTimeTotal,
                OutPitProp.PitTimeLast,
                OutPitProp.PitTimeCurrent,
             };
        }

        internal static string ToPropName(this OutPitProp p) {
            return p switch {
                OutPitProp.IsInPitLane => "Pit.IsIn",
                OutPitProp.PitStopCount => "Pit.Count",
                OutPitProp.PitTimeTotal => "Pit.Time.Total",
                OutPitProp.PitTimeLast => "Pit.Time.Last",
                OutPitProp.PitTimeCurrent => "Pit.Time.Current",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutPitProp p) {
            return p switch {
                OutPitProp.IsInPitLane => "Is the car in pit lane?",
                OutPitProp.PitStopCount => "Number of pitstops.",
                OutPitProp.PitTimeTotal => "Total time spent in pits.",
                OutPitProp.PitTimeLast => "Last pit time.",
                OutPitProp.PitTimeCurrent => "Current time in pits.",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [Flags]
    internal enum OutPosProp {
        None = 0,
        OverallPosition = 1 << 0,
        OverallPositionStart = 1 << 1,
        ClassPosition = 1 << 2,
        ClassPositionStart = 1 << 3,
        DynamicPosition = 1 << 4,
        DynamicPositionStart = 1 << 5,
        CupPosition = 1 << 6,
        CupPositionStart = 1 << 7,
    }

    internal static class OutPosPropExtensions {

        internal static bool Includes(this OutPosProp p, OutPosProp o) {
            return (p & o) != 0;
        }

        internal static bool IncludesAny(this OutPosProp p, params OutPosProp[] others) {
            foreach (var o in others) {
                if (p.Includes(o)) {
                    return true;
                }
            }
            return false;
        }

        internal static bool IncludesAll(this OutPosProp p, params OutPosProp[] others) {
            foreach (var o in others) {
                if (!p.Includes(o)) {
                    return false;
                }
            }
            return true;
        }

        internal static void Combine(ref this OutPosProp p, OutPosProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutPosProp p, OutPosProp o) {
            p &= ~o;
        }

        internal static OutPosProp[] Order() {
            return new[] {
                OutPosProp.OverallPosition,
                OutPosProp.OverallPositionStart,
                OutPosProp.ClassPosition,
                OutPosProp.ClassPositionStart,
                OutPosProp.CupPosition,
                OutPosProp.CupPositionStart,
                OutPosProp.DynamicPosition,
                OutPosProp.DynamicPositionStart
             };
        }

        internal static string ToPropName(this OutPosProp p) {
            return p switch {
                OutPosProp.ClassPosition => "Position.Class",
                OutPosProp.ClassPositionStart => "Position.Class.Start",
                OutPosProp.OverallPosition => "Position.Overall",
                OutPosProp.OverallPositionStart => "Position.Overall.Start",
                OutPosProp.DynamicPosition => "Position.Dynamic",
                OutPosProp.DynamicPositionStart => "Position.Dynamic.Start",
                OutPosProp.CupPosition => "Position.Cup",
                OutPosProp.CupPositionStart => "Position.Cup.Start",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutPosProp p) {
            return p switch {
                OutPosProp.ClassPosition => "Current class position",
                OutPosProp.OverallPosition => "Current overall position",
                OutPosProp.CupPosition => "Current cup position",
                OutPosProp.ClassPositionStart => "Class position at race start",
                OutPosProp.OverallPositionStart => "Overall position at race start",
                OutPosProp.CupPositionStart => "Cup position at race start",
                OutPosProp.DynamicPosition => @"Position that changes based of currently displayed dynamic leaderboard.
Any overall -> ovarall position,
Any class -> class position,
Any cup -> cup position,
RelativeOnTrack -> overall position",
                OutPosProp.DynamicPositionStart => @"Position at the race start that changes based of currently displayed dynamic leaderboard.
Any overall -> ovarall position,
Any class -> class position,
Any cup -> cup position,
RelativeOnTrack -> overall position",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [Flags]
    internal enum OutGapProp {
        None = 0,
        GapToLeader = 1 << 0,
        GapToClassLeader = 1 << 1,
        GapToFocusedTotal = 1 << 2,
        GapToFocusedOnTrack = 1 << 3,
        GapToAheadOverall = 1 << 4,
        GapToAheadInClass = 1 << 5,
        GapToAheadOnTrack = 1 << 6,
        GapToCupLeader = 1 << 7,
        GapToAheadInCup = 1 << 8,
        DynamicGapToFocused = 1 << 20,
        DynamicGapToAhead = 1 << 21,
    }

    internal static class OutGapPropExtensions {

        internal static bool Includes(this OutGapProp p, OutGapProp o) {
            return (p & o) != 0;
        }

        internal static void Combine(ref this OutGapProp p, OutGapProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutGapProp p, OutGapProp o) {
            p &= ~o;
        }

        internal static OutGapProp[] Order() {
            return new[] {
                 OutGapProp.GapToLeader,
                 OutGapProp.GapToClassLeader,
                 OutGapProp.GapToCupLeader,
                 OutGapProp.GapToFocusedTotal,
                 OutGapProp.GapToFocusedOnTrack,
                 OutGapProp.GapToAheadOverall,
                 OutGapProp.GapToAheadInClass,
                 OutGapProp.GapToAheadInCup,
                 OutGapProp.GapToAheadOnTrack,
                 OutGapProp.DynamicGapToFocused,
                 OutGapProp.DynamicGapToAhead,
             };
        }

        internal static string ToPropName(this OutGapProp p) {
            return p switch {
                OutGapProp.GapToLeader => "Gap.ToOverallLeader",
                OutGapProp.GapToClassLeader => "Gap.ToClassLeader",
                OutGapProp.GapToCupLeader => "Gap.ToCupLeader",
                OutGapProp.GapToFocusedTotal => "Gap.ToFocused.Total",
                OutGapProp.GapToFocusedOnTrack => "Gap.ToFocused.OnTrack",
                OutGapProp.GapToAheadOverall => "Gap.ToAhead.Overall",
                OutGapProp.GapToAheadInClass => "Gap.ToAhead.Class",
                OutGapProp.GapToAheadInCup => "Gap.ToAhead.Cup",
                OutGapProp.GapToAheadOnTrack => "Gap.ToAhead.OnTrack",
                OutGapProp.DynamicGapToFocused => "Gap.Dynamic.ToFocused",
                OutGapProp.DynamicGapToAhead => "Gap.Dynamic.ToAhead",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutGapProp p) {
            return p switch {
                OutGapProp.GapToLeader => "Total gap to the leader. Always positive.",
                OutGapProp.GapToClassLeader => "Total gap to the class leader. Always positive.",
                OutGapProp.GapToCupLeader => "Total gap to the cup leader. Always Positive.",
                OutGapProp.GapToFocusedTotal => "Total gap to the focused car. The gap is positive if the car is ahead of the focused car. Negative if the car is behind.",
                OutGapProp.GapToFocusedOnTrack => "On track gap to the focused car. The gap is positive if the car is ahead of the focused car. Negative if the car is behind.",
                OutGapProp.GapToAheadOverall => "Total gap to the car ahead in overall. Always positive.",
                OutGapProp.GapToAheadInClass => "Total gap to the car ahead in class. Always positive.",
                OutGapProp.GapToAheadInCup => "Total gap to the car ahead in cup. Always positive.",
                OutGapProp.GapToAheadOnTrack => "Relative on track gap to car ahead. Always positive.",
                OutGapProp.DynamicGapToFocused => @"Gap that changes based of currently displayed dynamic leaderboard.
Overall -> gap to leader,
Class -> gap to class leader,
Cup -> gap to cup leader,
PartialRelativeOverall/PartialRelativeClass/RelativePverall/RelativeClass -> gap to focused total,
RelativeOnTrack -> gap to focused on track.",
                OutGapProp.DynamicGapToAhead => @"Gap to the car ahead that changes based on the currently displayed dynamic leaderboard.
Overall/RelativeOverall/PartialRelativeOverall -> gap to ahead in overall order,
Class/RelativeClass/PartialRelativeClass -> gap to ahead in class order,
Cup/RelativeCup/PartialRelativeCup -> gap to ahead in cup order,
RelativeOnTrack -> gap to ahead on track.",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [Flags]
    internal enum OutStintProp {
        None = 0,
        CurrentStintTime = 1 << 0,
        CurrentStintLaps = 1 << 1,
        LastStintTime = 1 << 2,
        LastStintLaps = 1 << 3,
    }

    internal static class OutStintPropExtensions {

        internal static bool Includes(this OutStintProp p, OutStintProp o) {
            return (p & o) != 0;
        }

        internal static void Combine(ref this OutStintProp p, OutStintProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutStintProp p, OutStintProp o) {
            p &= ~o;
        }

        internal static OutStintProp[] Order() {
            return new[] {
                 OutStintProp.CurrentStintTime,
                 OutStintProp.CurrentStintLaps,
                 OutStintProp.LastStintTime,
                 OutStintProp.LastStintLaps
             };
        }

        internal static string ToPropName(this OutStintProp p) {
            return p switch {
                OutStintProp.CurrentStintTime => "Stint.Current.Time",
                OutStintProp.CurrentStintLaps => "Stint.Current.Laps",
                OutStintProp.LastStintTime => "Stint.Last.Time",
                OutStintProp.LastStintLaps => "Stint.Last.Laps",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutStintProp p) {
            return p switch {
                OutStintProp.CurrentStintTime => "Current stint time.",
                OutStintProp.LastStintTime => "Last stint time.",
                OutStintProp.CurrentStintLaps => "Number of laps completed in current stint",
                OutStintProp.LastStintLaps => "Number of laps completed in last stint",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }
}