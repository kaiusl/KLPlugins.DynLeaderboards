using System;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings {
    internal interface IOutProps<T> {
        bool Includes(T o);
        void Combine(T o);
        void Remove(T o);
    }

    internal abstract class OutPropsBase<T>(T value) : IOutProps<T> where T : struct {
        public T Value { get; protected internal set; } = value;

        public abstract bool Includes(T o);
        public abstract void Combine(T o);
        public abstract void Remove(T o);

        internal class OutPropBaseJsonConverter(Func<T, OutPropsBase<T>> construct) : JsonConverter {

            private readonly Func<T, OutPropsBase<T>> _construct = construct;

            public override bool CanConvert(Type objectType) {
                return objectType == typeof(T?);
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
                var t = serializer.Deserialize<T?>(reader);
                if (t == null) {
                    return null;
                }
                return _construct(t.Value);
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
                if (value is OutPropsBase<T> props) {
                    writer.WriteValue(props.Value);
                } else {
                    throw new ArgumentException("value must be OutPropsBase<T>");
                }
            }
        }
    }

    [JsonConverter(typeof(JsonConvert))]
    internal class OutGeneralProps : OutPropsBase<OutGeneralProp> {
        internal OutGeneralProps(OutGeneralProp value) : base(value) { }

        public override void Combine(OutGeneralProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutGeneralProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutGeneralProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutGeneralProps(p)) {
            }
        }
    }

    [Flags]
    internal enum OutGeneralProp {
        None = 0,
        SessionPhase = 1 << 1,
        MaxStintTime = 1 << 2,
        MaxDriveTime = 1 << 3,
        CarClassColors = 1 << 4,
        TeamCupColors = 1 << 5,
        TeamCupTextColors = 1 << 6,
        DriverCategoryColors = 1 << 7,
        CarClassTextColors = 1 << 8,
        DriverCategoryTextColors = 1 << 9,
        NumClassesInSession = 1 << 10,
        NumCupsInSession = 1 << 11
    }

    internal static class OutGeneralPropExtensions {

        internal static OutGeneralProp[] Order() {
            return [
                OutGeneralProp.SessionPhase,
                OutGeneralProp.MaxStintTime,
                OutGeneralProp.MaxDriveTime,
                OutGeneralProp.NumClassesInSession,
                OutGeneralProp.NumCupsInSession,
                OutGeneralProp.CarClassColors,
                OutGeneralProp.CarClassTextColors,
                OutGeneralProp.TeamCupColors,
                OutGeneralProp.TeamCupTextColors,
                OutGeneralProp.DriverCategoryColors,
                OutGeneralProp.DriverCategoryTextColors
             ];
        }

        internal static string ToPropName(this OutGeneralProp p) {
            return p switch {
                OutGeneralProp.SessionPhase => "Session.Phase",
                OutGeneralProp.MaxStintTime => "Session.MaxStintTime",
                OutGeneralProp.MaxDriveTime => "Session.MaxDriveTime",
                OutGeneralProp.CarClassColors => "Color.Class.<class>",
                OutGeneralProp.TeamCupColors => "Color.Cup.<cup>",
                OutGeneralProp.TeamCupTextColors => "Color.Cup.<cup>.Text",
                OutGeneralProp.DriverCategoryColors => "Color.DriverCategory.<category>",
                OutGeneralProp.CarClassTextColors => "Color.Class.<class>.Text",
                OutGeneralProp.DriverCategoryTextColors => "Color.DriverCategory.<category>.Text",
                OutGeneralProp.NumClassesInSession => "Session.NumberOfClasses",
                OutGeneralProp.NumCupsInSession => "Session.NumberOfCups",
                _ => throw new ArgumentOutOfRangeException("Invalid enum variant"),
            };
        }

        internal static string ToolTipText(this OutGeneralProp p) {
            return p switch {
                OutGeneralProp.SessionPhase => "Session phase.",
                OutGeneralProp.MaxStintTime => "Maximum driver stint time.",
                OutGeneralProp.MaxDriveTime => "Maximum total driving time for driver for player car. This can be different for other teams if they have different number of drivers.",
                OutGeneralProp.CarClassColors => "Background color for every car class.",
                OutGeneralProp.CarClassTextColors => "Text color for every car class.",
                OutGeneralProp.TeamCupColors => "Background colors for every team cup category.",
                OutGeneralProp.TeamCupTextColors => "Text colors for every team cup category",
                OutGeneralProp.DriverCategoryColors => "Background color for every driver category",
                OutGeneralProp.DriverCategoryTextColors => "Text color for every driver category",
                OutGeneralProp.NumClassesInSession => "Number of different classes in current session.",
                OutGeneralProp.NumCupsInSession => "Number of different cups (class and team cup category combinations) in current session.",
                OutGeneralProp.None => "None",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [JsonConverter(typeof(JsonConvert))]
    internal class OutCarProps : OutPropsBase<OutCarProp> {
        internal OutCarProps(OutCarProp value) : base(value) { }

        public override void Combine(OutCarProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutCarProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutCarProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutCarProps(p)) {
            }
        }
    }

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

        CarNumberText = 1 << 17,
        CarClassShortName = 1 << 18,
    }

    internal static class OutCarPropExtensions {
        internal static OutCarProp[] OrderCarInformation() {
            return [
                 OutCarProp.CarNumber,
                 OutCarProp.CarNumberText,
                 OutCarProp.CarModel,
                 OutCarProp.CarManufacturer,
                 OutCarProp.CarClass,
                 OutCarProp.CarClassShortName,
                OutCarProp.CarClassColor,
                 OutCarProp.CarClassTextColor,
                 OutCarProp.TeamName,
                 OutCarProp.TeamCupCategory,
                 OutCarProp.TeamCupCategoryColor,
                 OutCarProp.TeamCupCategoryTextColor
             ];
        }

        internal static OutCarProp[] OrderOther() {
            return [
                 OutCarProp.IsFinished,
                 OutCarProp.MaxSpeed,
                 OutCarProp.IsFocused,
                 OutCarProp.IsOverallBestLapCar,
                 OutCarProp.IsClassBestLapCar,
                 OutCarProp.IsCupBestLapCar,
                 OutCarProp.RelativeOnTrackLapDiff
             ];
        }

        internal static string ToPropName(this OutCarProp p) {
            return p switch {
                OutCarProp.CarNumber => "Car.Number",
                OutCarProp.CarNumberText => "Car.Number.Text",
                OutCarProp.CarModel => "Car.Model",
                OutCarProp.CarManufacturer => "Car.Manufacturer",
                OutCarProp.CarClass => "Car.Class",
                OutCarProp.CarClassShortName => "Car.Class.Short",
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
                OutCarProp.CarNumber => "Car number as an integer.",
                OutCarProp.CarNumberText => "Car number as a text. Allows to differentiate 01 and 1 for example.",
                OutCarProp.CarModel => "Car model name.",
                OutCarProp.CarManufacturer => "Car manufacturer.",
                OutCarProp.CarClass => "Car class.",
                OutCarProp.CarClassShortName => "Car class short name.",
                OutCarProp.TeamName => "Team name.",
                OutCarProp.TeamCupCategory => "Team cup category.",
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

    [JsonConverter(typeof(JsonConvert))]
    internal class OutPitProps : OutPropsBase<OutPitProp> {
        internal OutPitProps(OutPitProp value) : base(value) { }

        public override void Combine(OutPitProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutPitProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutPitProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutPitProps(p)) {
            }
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
        internal static OutPitProp[] Order() {
            return [
                OutPitProp.IsInPitLane,
                OutPitProp.PitStopCount,
                OutPitProp.PitTimeTotal,
                OutPitProp.PitTimeLast,
                OutPitProp.PitTimeCurrent,
             ];
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

    [JsonConverter(typeof(JsonConvert))]
    internal class OutPosProps : OutPropsBase<OutPosProp> {
        internal OutPosProps(OutPosProp value) : base(value) { }

        public override void Combine(OutPosProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutPosProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutPosProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutPosProps(p)) {
            }
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

        internal static OutPosProp[] Order() {
            return [
                OutPosProp.OverallPosition,
                OutPosProp.OverallPositionStart,
                OutPosProp.ClassPosition,
                OutPosProp.ClassPositionStart,
                OutPosProp.CupPosition,
                OutPosProp.CupPositionStart,
             ];
        }

        internal static OutPosProp[] OrderDynamic() {
            return [
                OutPosProp.DynamicPosition,
                OutPosProp.DynamicPositionStart
             ];
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
Any overall -> overall position,
Any class -> class position,
Any cup -> cup position,
RelativeOnTrack -> overall position",
                OutPosProp.DynamicPositionStart => @"Position at the race start that changes based of currently displayed dynamic leaderboard.
Any overall -> overall position,
Any class -> class position,
Any cup -> cup position,
RelativeOnTrack -> overall position",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }

    [JsonConverter(typeof(JsonConvert))]
    internal class OutGapProps : OutPropsBase<OutGapProp> {
        internal OutGapProps(OutGapProp value) : base(value) { }

        public override void Combine(OutGapProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutGapProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutGapProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutGapProps(p)) {
            }
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
        internal static OutGapProp[] Order() {
            return [
                 OutGapProp.GapToLeader,
                 OutGapProp.GapToClassLeader,
                 OutGapProp.GapToCupLeader,
                 OutGapProp.GapToFocusedTotal,
                 OutGapProp.GapToFocusedOnTrack,
                 OutGapProp.GapToAheadOverall,
                 OutGapProp.GapToAheadInClass,
                 OutGapProp.GapToAheadInCup,
                 OutGapProp.GapToAheadOnTrack,
             ];
        }

        internal static OutGapProp[] OrderDynamic() {
            return [
                 OutGapProp.DynamicGapToFocused,
                 OutGapProp.DynamicGapToAhead,
             ];
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

    [JsonConverter(typeof(JsonConvert))]
    internal class OutStintProps : OutPropsBase<OutStintProp> {
        internal OutStintProps(OutStintProp value) : base(value) { }

        public override void Combine(OutStintProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutStintProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutStintProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutStintProps(p)) {
            }
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

        internal static OutStintProp[] Order() {
            return [
                 OutStintProp.CurrentStintTime,
                 OutStintProp.CurrentStintLaps,
                 OutStintProp.LastStintTime,
                 OutStintProp.LastStintLaps
             ];
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

    [JsonConverter(typeof(JsonConvert))]
    internal class OutLapProps : OutPropsBase<OutLapProp> {
        internal OutLapProps(OutLapProp value) : base(value) { }

        public override void Combine(OutLapProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutLapProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutLapProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutLapProps(p)) {
            }
        }
    }

    [Flags]
    internal enum OutLapProp : long {
        None = 0,
        Laps = 1L << 0,
        LastLapTime = 1L << 1,
        LastLapSectors = 1L << 2,
        BestLapTime = 1L << 3,
        BestLapSectors = 1L << 4,
        BestSectors = 1L << 5,
        CurrentLapTime = 1L << 6,

        BestLapDeltaToOverallBest = 1L << 10,
        BestLapDeltaToClassBest = 1L << 11,

        BestLapDeltaToLeaderBest = 1L << 12,
        BestLapDeltaToClassLeaderBest = 1L << 13,

        BestLapDeltaToFocusedBest = 1L << 14,
        BestLapDeltaToAheadBest = 1L << 15,
        BestLapDeltaToAheadInClassBest = 1L << 16,

        DynamicBestLapDeltaToFocusedBest = 1L << 17,

        LastLapDeltaToOverallBest = 1L << 18,
        LastLapDeltaToClassBest = 1L << 19,

        LastLapDeltaToLeaderBest = 1L << 20,
        LastLapDeltaToClassLeaderBest = 1L << 21,

        LastLapDeltaToFocusedBest = 1L << 22,
        LastLapDeltaToAheadBest = 1L << 23,
        LastLapDeltaToAheadInClassBest = 1L << 24,

        LastLapDeltaToOwnBest = 1L << 25,
        DynamicLastLapDeltaToFocusedBest = 1L << 26,

        LastLapDeltaToLeaderLast = 1L << 27,
        LastLapDeltaToClassLeaderLast = 1L << 28,

        LastLapDeltaToFocusedLast = 1L << 29,
        LastLapDeltaToAheadLast = 1L << 30,
        LastLapDeltaToAheadInClassLast = 1L << 31,
        DynamicLastLapDeltaToFocusedLast = 1L << 32,

        CurrentLapIsValid = 1L << 33,
        LastLapIsValid = 1L << 34,
        CurrentLapIsOutLap = 1L << 35,
        LastLapIsOutLap = 1L << 36,
        CurrentLapIsInLap = 1L << 37,
        LastLapIsInLap = 1L << 38,

        BestLapDeltaToCupBest = 1L << 39,
        BestLapDeltaToCupLeaderBest = 1L << 40,
        BestLapDeltaToAheadInCupBest = 1L << 41,
        LastLapDeltaToCupBest = 1L << 42,
        LastLapDeltaToCupLeaderBest = 1L << 43,
        LastLapDeltaToAheadInCupBest = 1L << 44,
        LastLapDeltaToCupLeaderLast = 1L << 45,
        LastLapDeltaToAheadInCupLast = 1L << 46,
    }

    internal static class OutLapPropExtensions {

        internal static OutLapProp[] Order() {
            return [
                OutLapProp.Laps,
                OutLapProp.LastLapTime,
                OutLapProp.LastLapSectors,
                OutLapProp.LastLapIsValid,
                OutLapProp.LastLapIsOutLap,
                OutLapProp.LastLapIsInLap,
                OutLapProp.BestLapTime,
                OutLapProp.BestLapSectors,
                OutLapProp.BestSectors,
                OutLapProp.CurrentLapTime,
                OutLapProp.CurrentLapIsValid,
                OutLapProp.CurrentLapIsOutLap,
                OutLapProp.CurrentLapIsInLap,
            ];
        }

        internal static OutLapProp[] OrderDeltaBestToBest() {
            return [
                OutLapProp.BestLapDeltaToOverallBest,
                OutLapProp.BestLapDeltaToClassBest,
                OutLapProp.BestLapDeltaToCupBest,
                OutLapProp.BestLapDeltaToLeaderBest,
                OutLapProp.BestLapDeltaToClassLeaderBest,
                OutLapProp.BestLapDeltaToCupLeaderBest,
                OutLapProp.BestLapDeltaToFocusedBest,
                OutLapProp.BestLapDeltaToAheadBest,
                OutLapProp.BestLapDeltaToAheadInClassBest,
                OutLapProp.BestLapDeltaToAheadInCupBest,
            ];
        }

        internal static OutLapProp[] OrderDeltaLastToBest() {
            return [
                OutLapProp.LastLapDeltaToOverallBest,
                OutLapProp.LastLapDeltaToClassBest,
                OutLapProp.LastLapDeltaToCupBest,
                OutLapProp.LastLapDeltaToLeaderBest,
                OutLapProp.LastLapDeltaToClassLeaderBest,
                OutLapProp.LastLapDeltaToCupLeaderBest,
                OutLapProp.LastLapDeltaToFocusedBest,
                OutLapProp.LastLapDeltaToAheadBest,
                OutLapProp.LastLapDeltaToAheadInClassBest,
                OutLapProp.LastLapDeltaToAheadInCupBest,
                OutLapProp.LastLapDeltaToOwnBest,
            ];
        }

        internal static OutLapProp[] OrderDeltaLastToLast() {
            return [
                OutLapProp.LastLapDeltaToLeaderLast,
                OutLapProp.LastLapDeltaToClassLeaderLast,
                OutLapProp.LastLapDeltaToCupLeaderLast,
                OutLapProp.LastLapDeltaToFocusedLast,
                OutLapProp.LastLapDeltaToAheadLast,
                OutLapProp.LastLapDeltaToAheadInClassLast,
                OutLapProp.LastLapDeltaToAheadInCupLast,
            ];
        }

        internal static OutLapProp[] OrderDynamic() {
            return [
                OutLapProp.DynamicBestLapDeltaToFocusedBest,
                OutLapProp.DynamicLastLapDeltaToFocusedBest,
                OutLapProp.DynamicLastLapDeltaToFocusedLast,
            ];
        }

        internal static string ToPropName(this OutLapProp p) {
            return p switch {
                OutLapProp.Laps => "Laps.Count",
                OutLapProp.LastLapTime => "Laps.Last.Time",
                OutLapProp.LastLapSectors => "Laps.Last.S1/2/3",
                OutLapProp.BestLapTime => "Laps.Best.Time",
                OutLapProp.BestLapSectors => "Laps.Best.S1/2/3",
                OutLapProp.BestSectors => "BestS1/2/3",
                OutLapProp.CurrentLapTime => "Laps.Current.Time",
                OutLapProp.BestLapDeltaToOverallBest => "Laps.Best.Delta.ToOverallBest",
                OutLapProp.BestLapDeltaToClassBest => "Laps.Best.Delta.ToClassBest",
                OutLapProp.BestLapDeltaToCupBest => "Laps.Best.Delta.ToCupBest",
                OutLapProp.BestLapDeltaToLeaderBest => "Laps.Best.Delta.ToLeaderBest",
                OutLapProp.BestLapDeltaToClassLeaderBest => "Laps.Best.Delta.ToClassLeaderBest",
                OutLapProp.BestLapDeltaToCupLeaderBest => "Laps.Best.Delta.ToCupLeaderBest",
                OutLapProp.BestLapDeltaToFocusedBest => "Laps.Best.Delta.ToFocusedBest",
                OutLapProp.BestLapDeltaToAheadBest => "Laps.Best.Delta.ToAheadBest",
                OutLapProp.BestLapDeltaToAheadInClassBest => "Laps.Best.Delta.ToAheadInClassBest",
                OutLapProp.BestLapDeltaToAheadInCupBest => "Laps.Best.Delta.ToAheadInCupBest",
                OutLapProp.LastLapDeltaToOverallBest => "Laps.Last.Delta.ToOverallBest",
                OutLapProp.LastLapDeltaToClassBest => "Laps.Last.Delta.ToClassBest",
                OutLapProp.LastLapDeltaToCupBest => "Laps.Last.Delta.ToCupBest",
                OutLapProp.LastLapDeltaToLeaderBest => "Laps.Last.Delta.ToLeaderBest",
                OutLapProp.LastLapDeltaToClassLeaderBest => "Laps.Last.Delta.ToClassLeaderBest",
                OutLapProp.LastLapDeltaToCupLeaderBest => "Laps.Last.Delta.ToCupLeaderBest",
                OutLapProp.LastLapDeltaToFocusedBest => "Laps.Last.Delta.ToFocusedBest",
                OutLapProp.LastLapDeltaToAheadBest => "Laps.Last.Delta.ToAheadBest",
                OutLapProp.LastLapDeltaToAheadInClassBest => "Laps.Last.Delta.ToAheadInClassBest",
                OutLapProp.LastLapDeltaToAheadInCupBest => "Laps.Last.Delta.ToAheadInCupBest",
                OutLapProp.LastLapDeltaToOwnBest => "Laps.Last.Delta.ToOwnBest",
                OutLapProp.LastLapDeltaToLeaderLast => "Laps.Last.Delta.ToLeaderLast",
                OutLapProp.LastLapDeltaToClassLeaderLast => "Laps.Last.Delta.ToClassLeaderLast",
                OutLapProp.LastLapDeltaToCupLeaderLast => "Laps.Last.Delta.ToCupLeaderLast",
                OutLapProp.LastLapDeltaToFocusedLast => "Laps.Last.Delta.ToFocusedLast",
                OutLapProp.LastLapDeltaToAheadLast => "Laps.Last.Delta.ToAheadLast",
                OutLapProp.LastLapDeltaToAheadInClassLast => "Laps.Last.Delta.ToAheadInClassLast",
                OutLapProp.LastLapDeltaToAheadInCupLast => "Laps.Last.Delta.ToAheadInCupLast",
                OutLapProp.DynamicBestLapDeltaToFocusedBest => "Laps.Best.Delta.Dynamic.ToFocusedBest",
                OutLapProp.DynamicLastLapDeltaToFocusedBest => "Laps.Last.Delta.Dynamic.ToFocusedBest",
                OutLapProp.DynamicLastLapDeltaToFocusedLast => "Laps.Last.Delta.Dynamic.ToFocusedLast",
                OutLapProp.CurrentLapIsValid => "Laps.Current.IsValid",
                OutLapProp.LastLapIsValid => "Laps.Last.IsValid",
                OutLapProp.CurrentLapIsOutLap => "Laps.Current.IsOutLap",
                OutLapProp.LastLapIsOutLap => "Laps.Last.IsOutLap",
                OutLapProp.CurrentLapIsInLap => "Laps.Current.IsInLap",
                OutLapProp.LastLapIsInLap => "Laps.Last.IsInLap",
                _ => throw new ArgumentOutOfRangeException("Invalid enum variant"),
            };
        }

        internal static string ToolTipText(this OutLapProp p) {
            return p switch {
                OutLapProp.Laps => "Number of completed laps",
                OutLapProp.LastLapTime => "Last lap time.",
                OutLapProp.LastLapSectors => "Last lap sector times.",
                OutLapProp.BestLapTime => "Best lap time.",
                OutLapProp.BestLapSectors => "Best lap sector times.",
                OutLapProp.BestSectors => "Best sector times.",
                OutLapProp.CurrentLapTime => "Current lap time.",
                OutLapProp.BestLapDeltaToOverallBest => "Best lap delta to the overall best lap.",
                OutLapProp.BestLapDeltaToClassBest => "Best lap delta to the class best lap.",
                OutLapProp.BestLapDeltaToCupBest => "Best lap delta to the cup best lap.",
                OutLapProp.BestLapDeltaToLeaderBest => "Best lap delta to the leader's best lap.",
                OutLapProp.BestLapDeltaToClassLeaderBest => "Best lap delta to the class leader's best lap.",
                OutLapProp.BestLapDeltaToCupLeaderBest => "Best lap delta to the cup leader's best lap.",
                OutLapProp.BestLapDeltaToFocusedBest => "Best lap delta to the focused car's best lap.",
                OutLapProp.BestLapDeltaToAheadBest => "Best lap delta to the ahead car's best lap.",
                OutLapProp.BestLapDeltaToAheadInClassBest => "Best lap delta to the in class ahead car's best lap.",
                OutLapProp.BestLapDeltaToAheadInCupBest => "Best lap delta to the in cup ahead car's best lap.",
                OutLapProp.LastLapDeltaToOverallBest => "Last lap delta to the overall best lap.",
                OutLapProp.LastLapDeltaToClassBest => "Last lap delta to the class best lap.",
                OutLapProp.LastLapDeltaToCupBest => "Last lap delta to the cup best lap.",
                OutLapProp.LastLapDeltaToLeaderBest => "Last lap delta to the leader's best lap.",
                OutLapProp.LastLapDeltaToClassLeaderBest => "Last lap delta to the class leader's best lap.",
                OutLapProp.LastLapDeltaToCupLeaderBest => "Last lap delta to the cup leader's best lap.",
                OutLapProp.LastLapDeltaToFocusedBest => "Last lap delta to the focused car's best lap.",
                OutLapProp.LastLapDeltaToAheadBest => "Last lap delta to the ahead car's best lap.",
                OutLapProp.LastLapDeltaToAheadInClassBest => "Last lap delta to the in class car ahead's best lap.",
                OutLapProp.LastLapDeltaToAheadInCupBest => "Last lap delta to the in cup car ahead's best lap.",
                OutLapProp.LastLapDeltaToOwnBest => "Last lap delta to own best lap.",
                OutLapProp.LastLapDeltaToLeaderLast => "Last lap delta to the leader's last lap.",
                OutLapProp.LastLapDeltaToClassLeaderLast => "Last lap delta to the class leaders last lap.",
                OutLapProp.LastLapDeltaToCupLeaderLast => "Last lap delta to the cup leaders last lap.",
                OutLapProp.LastLapDeltaToFocusedLast => "Last lap delta to the focused car's last lap.",
                OutLapProp.LastLapDeltaToAheadLast => "Last lap delta to the ahead car's last lap.",
                OutLapProp.LastLapDeltaToAheadInClassLast => "Last lap delta to the in class ahead car's last lap.",
                OutLapProp.LastLapDeltaToAheadInCupLast => "Last lap delta to the in cup ahead car's last lap.",
                OutLapProp.DynamicBestLapDeltaToFocusedBest => @"Best lap delta to the car's best based on currently displayed dynamic leaderboard.
Overall -> delta to leader's best lap,
Class -> delta to class leader's best lap,
Cup -> delta to cup leader's best lap,
Any relative -> delta to focused car's best lap",
                OutLapProp.DynamicLastLapDeltaToFocusedBest => @"Last lap delta to the car's best based on currently displayed dynamic leaderboard.
Overall -> delta to leader's best lap,
Class -> delta to class leader's best lap,
Cup -> delta to cup leader's best lap,
Any relative -> delta to focused car's best lap",
                OutLapProp.DynamicLastLapDeltaToFocusedLast => @"Last lap delta to the car's last based on currently displayed dynamic leaderboard.
Overall -> delta to leader's last lap,
Class -> delta to class leader's last lap,
Cup -> delta to cup leader's last lap,
Any relative -> delta to focused car's last lap",
                OutLapProp.CurrentLapIsValid => "Is current lap valid?",
                OutLapProp.LastLapIsValid => "Was last lap valid?",
                OutLapProp.CurrentLapIsOutLap => "Is current lap an out lap?",
                OutLapProp.LastLapIsOutLap => "Was last lap an out lap?",
                OutLapProp.CurrentLapIsInLap => "Is current lap an in lap?",
                OutLapProp.LastLapIsInLap => "Was last lap an in lap?",
                _ => throw new ArgumentOutOfRangeException("Invalid enum variant"),
            };
        }
    }

    [JsonConverter(typeof(JsonConvert))]
    internal class OutDriverProps : OutPropsBase<OutDriverProp> {
        internal OutDriverProps(OutDriverProp value) : base(value) { }

        public override void Combine(OutDriverProp prop) {
            this.Value |= prop;
        }

        public override bool Includes(OutDriverProp prop) {
            return (this.Value & prop) != 0;
        }

        public override void Remove(OutDriverProp prop) {
            this.Value &= ~prop;
        }

        public class JsonConvert : OutPropBaseJsonConverter {
            public JsonConvert() : base(p => new OutDriverProps(p)) {
            }
        }
    }

    [Flags]
    internal enum OutDriverProp {
        None = 0,

        FirstName = 1 << 2,
        LastName = 1 << 3,
        ShortName = 1 << 4,
        FullName = 1 << 5,
        InitialPlusLastName = 1 << 6,
        Nationality = 1 << 7,
        Category = 1 << 8,
        TotalLaps = 1 << 9,
        TotalDrivingTime = 1 << 10,
        BestLapTime = 1 << 11,
        CategoryColorDeprecated = 1 << 12,
        CategoryColorText = 1 << 13,
        CategoryColor = 1 << 14,
    }

    internal static class OutDriverPropExtensions {
        internal static OutDriverProp[] Order() {
            return [
                OutDriverProp.FirstName,
                OutDriverProp.LastName,
                OutDriverProp.ShortName,
                OutDriverProp.FullName,
                OutDriverProp.InitialPlusLastName,
                OutDriverProp.Nationality,
                OutDriverProp.Category,
                OutDriverProp.TotalLaps,
                OutDriverProp.TotalDrivingTime,
                OutDriverProp.BestLapTime,
                OutDriverProp.CategoryColorDeprecated,
                OutDriverProp.CategoryColor,
                OutDriverProp.CategoryColorText
             ];
        }

        internal static string ToPropName(this OutDriverProp p) {
            return p switch {
                OutDriverProp.FirstName => "FirstName",
                OutDriverProp.LastName => "LastName",
                OutDriverProp.ShortName => "ShortName",
                OutDriverProp.FullName => "FullName",
                OutDriverProp.InitialPlusLastName => "InitialPlusLastName",
                OutDriverProp.Nationality => "Nationality",
                OutDriverProp.Category => "Category",
                OutDriverProp.TotalLaps => "TotalLaps",
                OutDriverProp.TotalDrivingTime => "TotalDrivingTime",
                OutDriverProp.BestLapTime => "BestLapTime",
                OutDriverProp.CategoryColorDeprecated => "CategoryColor",
                OutDriverProp.CategoryColor => "Category.Color",
                OutDriverProp.CategoryColorText => "Category.TextColor",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }

        internal static string ToolTipText(this OutDriverProp p) {
            return p switch {
                OutDriverProp.None => "None",
                OutDriverProp.FirstName => "First name (Abcde)",
                OutDriverProp.LastName => "Last name (Fghij)",
                OutDriverProp.ShortName => "Short name (AFG)",
                OutDriverProp.FullName => "Full name (Abcde Fghij)",
                OutDriverProp.InitialPlusLastName => "Initial + last name (A. Fghij)",
                OutDriverProp.Nationality => "Nationality",
                OutDriverProp.Category => "Driver category (Platinum, Gold, Silver, Bronze)",
                OutDriverProp.TotalLaps => "Total number of completed laps",
                OutDriverProp.TotalDrivingTime => "Total driving time in seconds",
                OutDriverProp.BestLapTime => "Best lap time in seconds",
                OutDriverProp.CategoryColorDeprecated => "DEPRECATED. Use Category.Color instead.",
                OutDriverProp.CategoryColor => "Background color for driver category",
                OutDriverProp.CategoryColorText => "Text color for driver category",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }
}