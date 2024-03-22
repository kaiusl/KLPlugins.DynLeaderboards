using System;

namespace KLPlugins.DynLeaderboards.Settings {

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
        CarClassTextColors = 1 << 8,
        DriverCategoryTextColors = 1 << 9,
    }

    internal static class OutGeneralPropExtensions {

        public static bool Includes(this OutGeneralProp p, OutGeneralProp o) {
            return (p & o) != 0;
        }

        public static void Combine(ref this OutGeneralProp p, OutGeneralProp o) {
            p |= o;
        }

        public static void Remove(ref this OutGeneralProp p, OutGeneralProp o) {
            p &= ~o;
        }

        public static string ToolTipText(this OutGeneralProp p) {
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
                OutGeneralProp.None => "None",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }
}