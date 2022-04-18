using System;


namespace KLPlugins.DynLeaderboards {
    [Flags]
    public enum OutGeneralProp {
        None = 0,
        SessionPhase = 1 << 1,
        MaxStintTime = 1 << 2,
        MaxDriveTime = 1 << 3,
        CarClassColors = 1 << 4,
        TeamCupColors = 1 << 5,
        TeamCupTextColors = 1 << 6,
        DriverCategoryColors = 1 << 7,
    }

    static class OutGeneralPropExtensions {
        public static bool Includes(this OutGeneralProp p, OutGeneralProp o) => (p & o) != 0;
        public static void Combine(ref this OutGeneralProp p, OutGeneralProp o) => p |= o;
        public static void Remove(ref this OutGeneralProp p, OutGeneralProp o) => p &= ~o;

        public static string ToolTipText(this OutGeneralProp p) {
            switch (p) {
                case OutGeneralProp.SessionPhase:
                    return "Session phase.";
                case OutGeneralProp.MaxStintTime:
                    return "Maximum driver stint time.";
                case OutGeneralProp.MaxDriveTime:
                    return "Maximum total driving time for driver for player car. This can be different for other teams if they have different number of drivers.";
                case OutGeneralProp.CarClassColors:
                    return "Color for every car class.";
                case OutGeneralProp.TeamCupColors:
                    return "Background colors for every team cup category.";
                case OutGeneralProp.TeamCupTextColors:
                    return "Text colors for every team cup category";
                case OutGeneralProp.DriverCategoryColors:
                    return "Color for every driver category";
                case OutGeneralProp.None:
                    return "None";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }
}