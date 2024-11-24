using System;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings;

internal interface IOutProps<T> {
    bool Includes(T o);
    void Combine(T o);
    void Remove(T o);
}

internal abstract class OutPropsBase<T>(T value) : IOutProps<T>
    where T : struct {
    public T Value { get; protected internal set; } = value;

    public abstract bool Includes(T o);
    public abstract void Combine(T o);
    public abstract void Remove(T o);

    internal class OutPropBaseJsonConverter(Func<T, OutPropsBase<T>> construct) : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(T?);
        }

        public override object? ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer
        ) {
            var t = serializer.Deserialize<T?>(reader);
            if (t == null) {
                return null;
            }

            return construct(t.Value);
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutGeneralProps(p));
}

[Flags]
internal enum OutGeneralProp {
    NONE = 0,
    SESSION_PHASE = 1 << 1,
    MAX_STINT_TIME = 1 << 2,
    MAX_DRIVE_TIME = 1 << 3,
    CAR_CLASS_COLORS = 1 << 4,
    TEAM_CUP_COLORS = 1 << 5,
    TEAM_CUP_TEXT_COLORS = 1 << 6,
    DRIVER_CATEGORY_COLORS = 1 << 7,
    CAR_CLASS_TEXT_COLORS = 1 << 8,
    DRIVER_CATEGORY_TEXT_COLORS = 1 << 9,
    NUM_CLASSES_IN_SESSION = 1 << 10,
    NUM_CUPS_IN_SESSION = 1 << 11,
}

internal static class OutGeneralPropExtensions {
    internal static OutGeneralProp[] Order() {
        return [
            OutGeneralProp.SESSION_PHASE,
            OutGeneralProp.MAX_STINT_TIME,
            OutGeneralProp.MAX_DRIVE_TIME,
            OutGeneralProp.NUM_CLASSES_IN_SESSION,
            OutGeneralProp.NUM_CUPS_IN_SESSION,
            OutGeneralProp.CAR_CLASS_COLORS,
            OutGeneralProp.CAR_CLASS_TEXT_COLORS,
            OutGeneralProp.TEAM_CUP_COLORS,
            OutGeneralProp.TEAM_CUP_TEXT_COLORS,
            OutGeneralProp.DRIVER_CATEGORY_COLORS,
            OutGeneralProp.DRIVER_CATEGORY_TEXT_COLORS,
        ];
    }

    internal static string ToPropName(this OutGeneralProp p) {
        return p switch {
            OutGeneralProp.SESSION_PHASE => "Session.Phase",
            OutGeneralProp.MAX_STINT_TIME => "Session.MaxStintTime",
            OutGeneralProp.MAX_DRIVE_TIME => "Session.MaxDriveTime",
            OutGeneralProp.CAR_CLASS_COLORS => "Color.Class.<class>",
            OutGeneralProp.TEAM_CUP_COLORS => "Color.Cup.<cup>",
            OutGeneralProp.TEAM_CUP_TEXT_COLORS => "Color.Cup.<cup>.Text",
            OutGeneralProp.DRIVER_CATEGORY_COLORS => "Color.DriverCategory.<category>",
            OutGeneralProp.CAR_CLASS_TEXT_COLORS => "Color.Class.<class>.Text",
            OutGeneralProp.DRIVER_CATEGORY_TEXT_COLORS => "Color.DriverCategory.<category>.Text",
            OutGeneralProp.NUM_CLASSES_IN_SESSION => "Session.NumberOfClasses",
            OutGeneralProp.NUM_CUPS_IN_SESSION => "Session.NumberOfCups",
            _ => throw new ArgumentOutOfRangeException("Invalid enum variant"),
        };
    }

    internal static string ToolTipText(this OutGeneralProp p) {
        return p switch {
            OutGeneralProp.SESSION_PHASE => "Session phase.",
            OutGeneralProp.MAX_STINT_TIME => "Maximum driver stint time.",
            OutGeneralProp.MAX_DRIVE_TIME =>
                "Maximum total driving time for driver for player car. This can be different for other teams if they have different number of drivers.",
            OutGeneralProp.CAR_CLASS_COLORS => "Background color for every car class.",
            OutGeneralProp.CAR_CLASS_TEXT_COLORS => "Text color for every car class.",
            OutGeneralProp.TEAM_CUP_COLORS => "Background colors for every team cup category.",
            OutGeneralProp.TEAM_CUP_TEXT_COLORS => "Text colors for every team cup category",
            OutGeneralProp.DRIVER_CATEGORY_COLORS => "Background color for every driver category",
            OutGeneralProp.DRIVER_CATEGORY_TEXT_COLORS => "Text color for every driver category",
            OutGeneralProp.NUM_CLASSES_IN_SESSION => "Number of different classes in current session.",
            OutGeneralProp.NUM_CUPS_IN_SESSION =>
                "Number of different cups (class and team cup category combinations) in current session.",
            OutGeneralProp.NONE => "None",
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutCarProps(p));
}

[Flags]
internal enum OutCarProp {
    NONE = 0,

    CAR_NUMBER = 1 << 0,
    CAR_MODEL = 1 << 1,
    CAR_MANUFACTURER = 1 << 2,
    CAR_CLASS = 1 << 3,
    TEAM_NAME = 1 << 4,
    TEAM_CUP_CATEGORY = 1 << 5,
    CAR_CLASS_COLOR = 1 << 6,
    TEAM_CUP_CATEGORY_COLOR = 1 << 7,
    TEAM_CUP_CATEGORY_TEXT_COLOR = 1 << 8,

    IS_FINISHED = 1 << 9,
    MAX_SPEED = 1 << 10,
    IS_FOCUSED = 1 << 11,
    IS_OVERALL_BEST_LAP_CAR = 1 << 12,
    IS_CLASS_BEST_LAP_CAR = 1 << 13,
    RELATIVE_ON_TRACK_LAP_DIFF = 1 << 14,
    IS_CUP_BEST_LAP_CAR = 1 << 15,

    CAR_CLASS_TEXT_COLOR = 1 << 16,

    CAR_NUMBER_TEXT = 1 << 17,
    CAR_CLASS_SHORT_NAME = 1 << 18,
}

internal static class OutCarPropExtensions {
    internal static OutCarProp[] OrderCarInformation() {
        return [
            OutCarProp.CAR_NUMBER,
            OutCarProp.CAR_NUMBER_TEXT,
            OutCarProp.CAR_MODEL,
            OutCarProp.CAR_MANUFACTURER,
            OutCarProp.CAR_CLASS,
            OutCarProp.CAR_CLASS_SHORT_NAME,
            OutCarProp.CAR_CLASS_COLOR,
            OutCarProp.CAR_CLASS_TEXT_COLOR,
            OutCarProp.TEAM_NAME,
            OutCarProp.TEAM_CUP_CATEGORY,
            OutCarProp.TEAM_CUP_CATEGORY_COLOR,
            OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR,
        ];
    }

    internal static OutCarProp[] OrderOther() {
        return [
            OutCarProp.IS_FINISHED,
            OutCarProp.MAX_SPEED,
            OutCarProp.IS_FOCUSED,
            OutCarProp.IS_OVERALL_BEST_LAP_CAR,
            OutCarProp.IS_CLASS_BEST_LAP_CAR,
            OutCarProp.IS_CUP_BEST_LAP_CAR,
            OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF,
        ];
    }

    internal static string ToPropName(this OutCarProp p) {
        return p switch {
            OutCarProp.CAR_NUMBER => "Car.Number",
            OutCarProp.CAR_NUMBER_TEXT => "Car.Number.Text",
            OutCarProp.CAR_MODEL => "Car.Model",
            OutCarProp.CAR_MANUFACTURER => "Car.Manufacturer",
            OutCarProp.CAR_CLASS => "Car.Class",
            OutCarProp.CAR_CLASS_SHORT_NAME => "Car.Class.Short",
            OutCarProp.TEAM_NAME => "Team.Name",
            OutCarProp.TEAM_CUP_CATEGORY => "Team.CupCategory",
            OutCarProp.IS_FINISHED => "IsFinished",
            OutCarProp.MAX_SPEED => "MaxSpeed",
            OutCarProp.CAR_CLASS_COLOR => "Car.Class.Color",
            OutCarProp.CAR_CLASS_TEXT_COLOR => "Car.Class.TextColor",
            OutCarProp.TEAM_CUP_CATEGORY_COLOR => "Team.CupCategory.Color",
            OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR => "Team.CupCategory.TextColor",
            OutCarProp.IS_FOCUSED => "IsFocused",
            OutCarProp.IS_OVERALL_BEST_LAP_CAR => "IsOverallBestLapCar",
            OutCarProp.IS_CLASS_BEST_LAP_CAR => "IsClassBestLapCar",
            OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF => "RelativeOnTrackLapDiff",
            OutCarProp.IS_CUP_BEST_LAP_CAR => "IsCupBestLapCar",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutCarProp p) {
        return p switch {
            OutCarProp.NONE => "None",
            OutCarProp.CAR_NUMBER => "Car number as an integer.",
            OutCarProp.CAR_NUMBER_TEXT => "Car number as a text. Allows to differentiate 01 and 1 for example.",
            OutCarProp.CAR_MODEL => "Car model name.",
            OutCarProp.CAR_MANUFACTURER => "Car manufacturer.",
            OutCarProp.CAR_CLASS => "Car class.",
            OutCarProp.CAR_CLASS_SHORT_NAME => "Car class short name.",
            OutCarProp.TEAM_NAME => "Team name.",
            OutCarProp.TEAM_CUP_CATEGORY => "Team cup category.",
            OutCarProp.IS_FINISHED => "Is the car finished?",
            OutCarProp.MAX_SPEED => "Maximum speed in this session.",
            OutCarProp.CAR_CLASS_COLOR =>
                "Car class background color. Values can be changed in \"General settings\" tab.",
            OutCarProp.CAR_CLASS_TEXT_COLOR =>
                "Car class text color. Values can be changed in \"General settings\" tab.",
            OutCarProp.TEAM_CUP_CATEGORY_COLOR =>
                "Team cup category background color. Values can be changed in \"General settings\" tab.",
            OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR =>
                "Team cup category text color. Values can be changed in \"General settings\" tab.",
            OutCarProp.IS_FOCUSED => "Is this the focused car?",
            OutCarProp.IS_OVERALL_BEST_LAP_CAR => "Is this the car that has overall best lap?",
            OutCarProp.IS_CLASS_BEST_LAP_CAR => "Is this the car that has class best lap?",
            OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF =>
                "Show if this car is ahead or behind by the lap on the relative on track. 1: this car is ahead by a lap, 0: same lap, -1: this car is behind by a lap.",
            OutCarProp.IS_CUP_BEST_LAP_CAR => "Is this the car that has cup best lap?",
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutPitProps(p));
}

[Flags]
internal enum OutPitProp {
    NONE = 0,
    IS_IN_PIT_LANE = 1 << 0,
    PIT_STOP_COUNT = 1 << 1,
    PIT_TIME_TOTAL = 1 << 2,
    PIT_TIME_LAST = 1 << 3,
    PIT_TIME_CURRENT = 1 << 4,
}

internal static class OutPitPropExtensions {
    internal static OutPitProp[] Order() {
        return [
            OutPitProp.IS_IN_PIT_LANE,
            OutPitProp.PIT_STOP_COUNT,
            OutPitProp.PIT_TIME_TOTAL,
            OutPitProp.PIT_TIME_LAST,
            OutPitProp.PIT_TIME_CURRENT,
        ];
    }

    internal static string ToPropName(this OutPitProp p) {
        return p switch {
            OutPitProp.IS_IN_PIT_LANE => "Pit.IsIn",
            OutPitProp.PIT_STOP_COUNT => "Pit.Count",
            OutPitProp.PIT_TIME_TOTAL => "Pit.Time.Total",
            OutPitProp.PIT_TIME_LAST => "Pit.Time.Last",
            OutPitProp.PIT_TIME_CURRENT => "Pit.Time.Current",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutPitProp p) {
        return p switch {
            OutPitProp.IS_IN_PIT_LANE => "Is the car in pit lane?",
            OutPitProp.PIT_STOP_COUNT => "Number of pitstops.",
            OutPitProp.PIT_TIME_TOTAL => "Total time spent in pits.",
            OutPitProp.PIT_TIME_LAST => "Last pit time.",
            OutPitProp.PIT_TIME_CURRENT => "Current time in pits.",
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutPosProps(p));
}

[Flags]
internal enum OutPosProp {
    NONE = 0,
    OVERALL_POSITION = 1 << 0,
    OVERALL_POSITION_START = 1 << 1,
    CLASS_POSITION = 1 << 2,
    CLASS_POSITION_START = 1 << 3,
    DYNAMIC_POSITION = 1 << 4,
    DYNAMIC_POSITION_START = 1 << 5,
    CUP_POSITION = 1 << 6,
    CUP_POSITION_START = 1 << 7,
}

internal static class OutPosPropExtensions {
    internal static OutPosProp[] Order() {
        return [
            OutPosProp.OVERALL_POSITION,
            OutPosProp.OVERALL_POSITION_START,
            OutPosProp.CLASS_POSITION,
            OutPosProp.CLASS_POSITION_START,
            OutPosProp.CUP_POSITION,
            OutPosProp.CUP_POSITION_START,
        ];
    }

    internal static OutPosProp[] OrderDynamic() {
        return [OutPosProp.DYNAMIC_POSITION, OutPosProp.DYNAMIC_POSITION_START];
    }

    internal static string ToPropName(this OutPosProp p) {
        return p switch {
            OutPosProp.CLASS_POSITION => "Position.Class",
            OutPosProp.CLASS_POSITION_START => "Position.Class.Start",
            OutPosProp.OVERALL_POSITION => "Position.Overall",
            OutPosProp.OVERALL_POSITION_START => "Position.Overall.Start",
            OutPosProp.DYNAMIC_POSITION => "Position.Dynamic",
            OutPosProp.DYNAMIC_POSITION_START => "Position.Dynamic.Start",
            OutPosProp.CUP_POSITION => "Position.Cup",
            OutPosProp.CUP_POSITION_START => "Position.Cup.Start",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutPosProp p) {
        return p switch {
            OutPosProp.CLASS_POSITION => "Current class position",
            OutPosProp.OVERALL_POSITION => "Current overall position",
            OutPosProp.CUP_POSITION => "Current cup position",
            OutPosProp.CLASS_POSITION_START => "Class position at race start",
            OutPosProp.OVERALL_POSITION_START => "Overall position at race start",
            OutPosProp.CUP_POSITION_START => "Cup position at race start",
            OutPosProp.DYNAMIC_POSITION => @"Position that changes based of currently displayed dynamic leaderboard.
Any overall -> overall position,
Any class -> class position,
Any cup -> cup position,
RelativeOnTrack -> overall position",
            OutPosProp.DYNAMIC_POSITION_START =>
                @"Position at the race start that changes based of currently displayed dynamic leaderboard.
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutGapProps(p));
}

[Flags]
internal enum OutGapProp {
    NONE = 0,
    GAP_TO_LEADER = 1 << 0,
    GAP_TO_CLASS_LEADER = 1 << 1,
    GAP_TO_FOCUSED_TOTAL = 1 << 2,
    GAP_TO_FOCUSED_ON_TRACK = 1 << 3,
    GAP_TO_AHEAD_OVERALL = 1 << 4,
    GAP_TO_AHEAD_IN_CLASS = 1 << 5,
    GAP_TO_AHEAD_ON_TRACK = 1 << 6,
    GAP_TO_CUP_LEADER = 1 << 7,
    GAP_TO_AHEAD_IN_CUP = 1 << 8,
    DYNAMIC_GAP_TO_FOCUSED = 1 << 20,
    DYNAMIC_GAP_TO_AHEAD = 1 << 21,
}

internal static class OutGapPropExtensions {
    internal static OutGapProp[] Order() {
        return [
            OutGapProp.GAP_TO_LEADER,
            OutGapProp.GAP_TO_CLASS_LEADER,
            OutGapProp.GAP_TO_CUP_LEADER,
            OutGapProp.GAP_TO_FOCUSED_TOTAL,
            OutGapProp.GAP_TO_FOCUSED_ON_TRACK,
            OutGapProp.GAP_TO_AHEAD_OVERALL,
            OutGapProp.GAP_TO_AHEAD_IN_CLASS,
            OutGapProp.GAP_TO_AHEAD_IN_CUP,
            OutGapProp.GAP_TO_AHEAD_ON_TRACK,
        ];
    }

    internal static OutGapProp[] OrderDynamic() {
        return [OutGapProp.DYNAMIC_GAP_TO_FOCUSED, OutGapProp.DYNAMIC_GAP_TO_AHEAD];
    }

    internal static string ToPropName(this OutGapProp p) {
        return p switch {
            OutGapProp.GAP_TO_LEADER => "Gap.ToOverallLeader",
            OutGapProp.GAP_TO_CLASS_LEADER => "Gap.ToClassLeader",
            OutGapProp.GAP_TO_CUP_LEADER => "Gap.ToCupLeader",
            OutGapProp.GAP_TO_FOCUSED_TOTAL => "Gap.ToFocused.Total",
            OutGapProp.GAP_TO_FOCUSED_ON_TRACK => "Gap.ToFocused.OnTrack",
            OutGapProp.GAP_TO_AHEAD_OVERALL => "Gap.ToAhead.Overall",
            OutGapProp.GAP_TO_AHEAD_IN_CLASS => "Gap.ToAhead.Class",
            OutGapProp.GAP_TO_AHEAD_IN_CUP => "Gap.ToAhead.Cup",
            OutGapProp.GAP_TO_AHEAD_ON_TRACK => "Gap.ToAhead.OnTrack",
            OutGapProp.DYNAMIC_GAP_TO_FOCUSED => "Gap.Dynamic.ToFocused",
            OutGapProp.DYNAMIC_GAP_TO_AHEAD => "Gap.Dynamic.ToAhead",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutGapProp p) {
        return p switch {
            OutGapProp.GAP_TO_LEADER => "Total gap to the leader. Always positive.",
            OutGapProp.GAP_TO_CLASS_LEADER => "Total gap to the class leader. Always positive.",
            OutGapProp.GAP_TO_CUP_LEADER => "Total gap to the cup leader. Always Positive.",
            OutGapProp.GAP_TO_FOCUSED_TOTAL =>
                "Total gap to the focused car. The gap is positive if the car is ahead of the focused car. Negative if the car is behind.",
            OutGapProp.GAP_TO_FOCUSED_ON_TRACK =>
                "On track gap to the focused car. The gap is positive if the car is ahead of the focused car. Negative if the car is behind.",
            OutGapProp.GAP_TO_AHEAD_OVERALL => "Total gap to the car ahead in overall. Always positive.",
            OutGapProp.GAP_TO_AHEAD_IN_CLASS => "Total gap to the car ahead in class. Always positive.",
            OutGapProp.GAP_TO_AHEAD_IN_CUP => "Total gap to the car ahead in cup. Always positive.",
            OutGapProp.GAP_TO_AHEAD_ON_TRACK => "Relative on track gap to car ahead. Always positive.",
            OutGapProp.DYNAMIC_GAP_TO_FOCUSED => @"Gap that changes based of currently displayed dynamic leaderboard.
Overall -> gap to leader,
Class -> gap to class leader,
Cup -> gap to cup leader,
PartialRelativeOverall/PartialRelativeClass/RelativePverall/RelativeClass -> gap to focused total,
RelativeOnTrack -> gap to focused on track.",
            OutGapProp.DYNAMIC_GAP_TO_AHEAD =>
                @"Gap to the car ahead that changes based on the currently displayed dynamic leaderboard.
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutStintProps(p));
}

[Flags]
internal enum OutStintProp {
    NONE = 0,
    CURRENT_STINT_TIME = 1 << 0,
    CURRENT_STINT_LAPS = 1 << 1,
    LAST_STINT_TIME = 1 << 2,
    LAST_STINT_LAPS = 1 << 3,
}

internal static class OutStintPropExtensions {
    internal static OutStintProp[] Order() {
        return [
            OutStintProp.CURRENT_STINT_TIME,
            OutStintProp.CURRENT_STINT_LAPS,
            OutStintProp.LAST_STINT_TIME,
            OutStintProp.LAST_STINT_LAPS,
        ];
    }

    internal static string ToPropName(this OutStintProp p) {
        return p switch {
            OutStintProp.CURRENT_STINT_TIME => "Stint.Current.Time",
            OutStintProp.CURRENT_STINT_LAPS => "Stint.Current.Laps",
            OutStintProp.LAST_STINT_TIME => "Stint.Last.Time",
            OutStintProp.LAST_STINT_LAPS => "Stint.Last.Laps",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutStintProp p) {
        return p switch {
            OutStintProp.CURRENT_STINT_TIME => "Current stint time.",
            OutStintProp.LAST_STINT_TIME => "Last stint time.",
            OutStintProp.CURRENT_STINT_LAPS => "Number of laps completed in current stint",
            OutStintProp.LAST_STINT_LAPS => "Number of laps completed in last stint",
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutLapProps(p));
}

[Flags]
internal enum OutLapProp : long {
    NONE = 0,
    LAPS = 1L << 0,
    LAST_LAP_TIME = 1L << 1,
    LAST_LAP_SECTORS = 1L << 2,
    BEST_LAP_TIME = 1L << 3,
    BEST_LAP_SECTORS = 1L << 4,
    BEST_SECTORS = 1L << 5,
    CURRENT_LAP_TIME = 1L << 6,

    BEST_LAP_DELTA_TO_OVERALL_BEST = 1L << 10,
    BEST_LAP_DELTA_TO_CLASS_BEST = 1L << 11,

    BEST_LAP_DELTA_TO_LEADER_BEST = 1L << 12,
    BEST_LAP_DELTA_TO_CLASS_LEADER_BEST = 1L << 13,

    BEST_LAP_DELTA_TO_FOCUSED_BEST = 1L << 14,
    BEST_LAP_DELTA_TO_AHEAD_BEST = 1L << 15,
    BEST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST = 1L << 16,

    DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST = 1L << 17,

    LAST_LAP_DELTA_TO_OVERALL_BEST = 1L << 18,
    LAST_LAP_DELTA_TO_CLASS_BEST = 1L << 19,

    LAST_LAP_DELTA_TO_LEADER_BEST = 1L << 20,
    LAST_LAP_DELTA_TO_CLASS_LEADER_BEST = 1L << 21,

    LAST_LAP_DELTA_TO_FOCUSED_BEST = 1L << 22,
    LAST_LAP_DELTA_TO_AHEAD_BEST = 1L << 23,
    LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST = 1L << 24,

    LAST_LAP_DELTA_TO_OWN_BEST = 1L << 25,
    DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_BEST = 1L << 26,

    LAST_LAP_DELTA_TO_LEADER_LAST = 1L << 27,
    LAST_LAP_DELTA_TO_CLASS_LEADER_LAST = 1L << 28,

    LAST_LAP_DELTA_TO_FOCUSED_LAST = 1L << 29,
    LAST_LAP_DELTA_TO_AHEAD_LAST = 1L << 30,
    LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_LAST = 1L << 31,
    DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST = 1L << 32,

    CURRENT_LAP_IS_VALID = 1L << 33,
    LAST_LAP_IS_VALID = 1L << 34,
    CURRENT_LAP_IS_OUT_LAP = 1L << 35,
    LAST_LAP_IS_OUT_LAP = 1L << 36,
    CURRENT_LAP_IS_IN_LAP = 1L << 37,
    LAST_LAP_IS_IN_LAP = 1L << 38,

    BEST_LAP_DELTA_TO_CUP_BEST = 1L << 39,
    BEST_LAP_DELTA_TO_CUP_LEADER_BEST = 1L << 40,
    BEST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST = 1L << 41,
    LAST_LAP_DELTA_TO_CUP_BEST = 1L << 42,
    LAST_LAP_DELTA_TO_CUP_LEADER_BEST = 1L << 43,
    LAST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST = 1L << 44,
    LAST_LAP_DELTA_TO_CUP_LEADER_LAST = 1L << 45,
    LAST_LAP_DELTA_TO_AHEAD_IN_CUP_LAST = 1L << 46,
}

internal static class OutLapPropExtensions {
    internal static OutLapProp[] Order() {
        return [
            OutLapProp.LAPS,
            OutLapProp.LAST_LAP_TIME,
            OutLapProp.LAST_LAP_SECTORS,
            OutLapProp.LAST_LAP_IS_VALID,
            OutLapProp.LAST_LAP_IS_OUT_LAP,
            OutLapProp.LAST_LAP_IS_IN_LAP,
            OutLapProp.BEST_LAP_TIME,
            OutLapProp.BEST_LAP_SECTORS,
            OutLapProp.BEST_SECTORS,
            OutLapProp.CURRENT_LAP_TIME,
            OutLapProp.CURRENT_LAP_IS_VALID,
            OutLapProp.CURRENT_LAP_IS_OUT_LAP,
            OutLapProp.CURRENT_LAP_IS_IN_LAP,
        ];
    }

    internal static OutLapProp[] OrderDeltaBestToBest() {
        return [
            OutLapProp.BEST_LAP_DELTA_TO_OVERALL_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_CUP_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_LEADER_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_LEADER_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_CUP_LEADER_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_FOCUSED_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST,
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST,
        ];
    }

    internal static OutLapProp[] OrderDeltaLastToBest() {
        return [
            OutLapProp.LAST_LAP_DELTA_TO_OVERALL_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_CUP_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST,
            OutLapProp.LAST_LAP_DELTA_TO_OWN_BEST,
        ];
    }

    internal static OutLapProp[] OrderDeltaLastToLast() {
        return [
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_LAST,
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_LAST,
        ];
    }

    internal static OutLapProp[] OrderDynamic() {
        return [
            OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST,
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_BEST,
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST,
        ];
    }

    internal static string ToPropName(this OutLapProp p) {
        return p switch {
            OutLapProp.LAPS => "Laps.Count",
            OutLapProp.LAST_LAP_TIME => "Laps.Last.Time",
            OutLapProp.LAST_LAP_SECTORS => "Laps.Last.S1/2/3",
            OutLapProp.BEST_LAP_TIME => "Laps.Best.Time",
            OutLapProp.BEST_LAP_SECTORS => "Laps.Best.S1/2/3",
            OutLapProp.BEST_SECTORS => "BestS1/2/3",
            OutLapProp.CURRENT_LAP_TIME => "Laps.Current.Time",
            OutLapProp.BEST_LAP_DELTA_TO_OVERALL_BEST => "Laps.Best.Delta.ToOverallBest",
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_BEST => "Laps.Best.Delta.ToClassBest",
            OutLapProp.BEST_LAP_DELTA_TO_CUP_BEST => "Laps.Best.Delta.ToCupBest",
            OutLapProp.BEST_LAP_DELTA_TO_LEADER_BEST => "Laps.Best.Delta.ToLeaderBest",
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_LEADER_BEST => "Laps.Best.Delta.ToClassLeaderBest",
            OutLapProp.BEST_LAP_DELTA_TO_CUP_LEADER_BEST => "Laps.Best.Delta.ToCupLeaderBest",
            OutLapProp.BEST_LAP_DELTA_TO_FOCUSED_BEST => "Laps.Best.Delta.ToFocusedBest",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_BEST => "Laps.Best.Delta.ToAheadBest",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST => "Laps.Best.Delta.ToAheadInClassBest",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST => "Laps.Best.Delta.ToAheadInCupBest",
            OutLapProp.LAST_LAP_DELTA_TO_OVERALL_BEST => "Laps.Last.Delta.ToOverallBest",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_BEST => "Laps.Last.Delta.ToClassBest",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_BEST => "Laps.Last.Delta.ToCupBest",
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_BEST => "Laps.Last.Delta.ToLeaderBest",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_BEST => "Laps.Last.Delta.ToClassLeaderBest",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_BEST => "Laps.Last.Delta.ToCupLeaderBest",
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_BEST => "Laps.Last.Delta.ToFocusedBest",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_BEST => "Laps.Last.Delta.ToAheadBest",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST => "Laps.Last.Delta.ToAheadInClassBest",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST => "Laps.Last.Delta.ToAheadInCupBest",
            OutLapProp.LAST_LAP_DELTA_TO_OWN_BEST => "Laps.Last.Delta.ToOwnBest",
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_LAST => "Laps.Last.Delta.ToLeaderLast",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_LAST => "Laps.Last.Delta.ToClassLeaderLast",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_LAST => "Laps.Last.Delta.ToCupLeaderLast",
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_LAST => "Laps.Last.Delta.ToFocusedLast",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_LAST => "Laps.Last.Delta.ToAheadLast",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_LAST => "Laps.Last.Delta.ToAheadInClassLast",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_LAST => "Laps.Last.Delta.ToAheadInCupLast",
            OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST => "Laps.Best.Delta.Dynamic.ToFocusedBest",
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_BEST => "Laps.Last.Delta.Dynamic.ToFocusedBest",
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST => "Laps.Last.Delta.Dynamic.ToFocusedLast",
            OutLapProp.CURRENT_LAP_IS_VALID => "Laps.Current.IsValid",
            OutLapProp.LAST_LAP_IS_VALID => "Laps.Last.IsValid",
            OutLapProp.CURRENT_LAP_IS_OUT_LAP => "Laps.Current.IsOutLap",
            OutLapProp.LAST_LAP_IS_OUT_LAP => "Laps.Last.IsOutLap",
            OutLapProp.CURRENT_LAP_IS_IN_LAP => "Laps.Current.IsInLap",
            OutLapProp.LAST_LAP_IS_IN_LAP => "Laps.Last.IsInLap",
            _ => throw new ArgumentOutOfRangeException("Invalid enum variant"),
        };
    }

    internal static string ToolTipText(this OutLapProp p) {
        return p switch {
            OutLapProp.LAPS => "Number of completed laps",
            OutLapProp.LAST_LAP_TIME => "Last lap time.",
            OutLapProp.LAST_LAP_SECTORS => "Last lap sector times.",
            OutLapProp.BEST_LAP_TIME => "Best lap time.",
            OutLapProp.BEST_LAP_SECTORS => "Best lap sector times.",
            OutLapProp.BEST_SECTORS => "Best sector times.",
            OutLapProp.CURRENT_LAP_TIME => "Current lap time.",
            OutLapProp.BEST_LAP_DELTA_TO_OVERALL_BEST => "Best lap delta to the overall best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_BEST => "Best lap delta to the class best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_CUP_BEST => "Best lap delta to the cup best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_LEADER_BEST => "Best lap delta to the leader's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_CLASS_LEADER_BEST => "Best lap delta to the class leader's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_CUP_LEADER_BEST => "Best lap delta to the cup leader's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_FOCUSED_BEST => "Best lap delta to the focused car's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_BEST => "Best lap delta to the ahead car's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST => "Best lap delta to the in class ahead car's best lap.",
            OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST => "Best lap delta to the in cup ahead car's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_OVERALL_BEST => "Last lap delta to the overall best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_BEST => "Last lap delta to the class best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_BEST => "Last lap delta to the cup best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_BEST => "Last lap delta to the leader's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_BEST => "Last lap delta to the class leader's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_BEST => "Last lap delta to the cup leader's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_BEST => "Last lap delta to the focused car's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_BEST => "Last lap delta to the ahead car's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST => "Last lap delta to the in class car ahead's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST => "Last lap delta to the in cup car ahead's best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_OWN_BEST => "Last lap delta to own best lap.",
            OutLapProp.LAST_LAP_DELTA_TO_LEADER_LAST => "Last lap delta to the leader's last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_LAST => "Last lap delta to the class leaders last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_LAST => "Last lap delta to the cup leaders last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_LAST => "Last lap delta to the focused car's last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_LAST => "Last lap delta to the ahead car's last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_LAST => "Last lap delta to the in class ahead car's last lap.",
            OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_LAST => "Last lap delta to the in cup ahead car's last lap.",
            OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST =>
                @"Best lap delta to the car's best based on currently displayed dynamic leaderboard.
Overall -> delta to leader's best lap,
Class -> delta to class leader's best lap,
Cup -> delta to cup leader's best lap,
Any relative -> delta to focused car's best lap",
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_BEST =>
                @"Last lap delta to the car's best based on currently displayed dynamic leaderboard.
Overall -> delta to leader's best lap,
Class -> delta to class leader's best lap,
Cup -> delta to cup leader's best lap,
Any relative -> delta to focused car's best lap",
            OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST =>
                @"Last lap delta to the car's last based on currently displayed dynamic leaderboard.
Overall -> delta to leader's last lap,
Class -> delta to class leader's last lap,
Cup -> delta to cup leader's last lap,
Any relative -> delta to focused car's last lap",
            OutLapProp.CURRENT_LAP_IS_VALID => "Is current lap valid?",
            OutLapProp.LAST_LAP_IS_VALID => "Was last lap valid?",
            OutLapProp.CURRENT_LAP_IS_OUT_LAP => "Is current lap an out lap?",
            OutLapProp.LAST_LAP_IS_OUT_LAP => "Was last lap an out lap?",
            OutLapProp.CURRENT_LAP_IS_IN_LAP => "Is current lap an in lap?",
            OutLapProp.LAST_LAP_IS_IN_LAP => "Was last lap an in lap?",
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

    public class JsonConvert() : OutPropBaseJsonConverter(p => new OutDriverProps(p));
}

[Flags]
internal enum OutDriverProp {
    NONE = 0,

    FIRST_NAME = 1 << 2,
    LAST_NAME = 1 << 3,
    SHORT_NAME = 1 << 4,
    FULL_NAME = 1 << 5,
    INITIAL_PLUS_LAST_NAME = 1 << 6,
    NATIONALITY = 1 << 7,
    CATEGORY = 1 << 8,
    TOTAL_LAPS = 1 << 9,
    TOTAL_DRIVING_TIME = 1 << 10,
    BEST_LAP_TIME = 1 << 11,
    CATEGORY_COLOR_DEPRECATED = 1 << 12,
    CATEGORY_COLOR_TEXT = 1 << 13,
    CATEGORY_COLOR = 1 << 14,
}

internal static class OutDriverPropExtensions {
    internal static OutDriverProp[] Order() {
        return [
            OutDriverProp.FIRST_NAME,
            OutDriverProp.LAST_NAME,
            OutDriverProp.SHORT_NAME,
            OutDriverProp.FULL_NAME,
            OutDriverProp.INITIAL_PLUS_LAST_NAME,
            OutDriverProp.NATIONALITY,
            OutDriverProp.CATEGORY,
            OutDriverProp.TOTAL_LAPS,
            OutDriverProp.TOTAL_DRIVING_TIME,
            OutDriverProp.BEST_LAP_TIME,
            OutDriverProp.CATEGORY_COLOR_DEPRECATED,
            OutDriverProp.CATEGORY_COLOR,
            OutDriverProp.CATEGORY_COLOR_TEXT,
        ];
    }

    internal static string ToPropName(this OutDriverProp p) {
        return p switch {
            OutDriverProp.FIRST_NAME => "FirstName",
            OutDriverProp.LAST_NAME => "LastName",
            OutDriverProp.SHORT_NAME => "ShortName",
            OutDriverProp.FULL_NAME => "FullName",
            OutDriverProp.INITIAL_PLUS_LAST_NAME => "InitialPlusLastName",
            OutDriverProp.NATIONALITY => "Nationality",
            OutDriverProp.CATEGORY => "Category",
            OutDriverProp.TOTAL_LAPS => "TotalLaps",
            OutDriverProp.TOTAL_DRIVING_TIME => "TotalDrivingTime",
            OutDriverProp.BEST_LAP_TIME => "BestLapTime",
            OutDriverProp.CATEGORY_COLOR_DEPRECATED => "CategoryColor",
            OutDriverProp.CATEGORY_COLOR => "Category.Color",
            OutDriverProp.CATEGORY_COLOR_TEXT => "Category.TextColor",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }

    internal static string ToolTipText(this OutDriverProp p) {
        return p switch {
            OutDriverProp.NONE => "None",
            OutDriverProp.FIRST_NAME => "First name (Abcde)",
            OutDriverProp.LAST_NAME => "Last name (Fghij)",
            OutDriverProp.SHORT_NAME => "Short name (AFG)",
            OutDriverProp.FULL_NAME => "Full name (Abcde Fghij)",
            OutDriverProp.INITIAL_PLUS_LAST_NAME => "Initial + last name (A. Fghij)",
            OutDriverProp.NATIONALITY => "Nationality",
            OutDriverProp.CATEGORY => "Driver category (Platinum, Gold, Silver, Bronze)",
            OutDriverProp.TOTAL_LAPS => "Total number of completed laps",
            OutDriverProp.TOTAL_DRIVING_TIME => "Total driving time in seconds",
            OutDriverProp.BEST_LAP_TIME => "Best lap time in seconds",
            OutDriverProp.CATEGORY_COLOR_DEPRECATED => "DEPRECATED. Use Category.Color instead.",
            OutDriverProp.CATEGORY_COLOR => "Background color for driver category",
            OutDriverProp.CATEGORY_COLOR_TEXT => "Text color for driver category",
            _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
        };
    }
}