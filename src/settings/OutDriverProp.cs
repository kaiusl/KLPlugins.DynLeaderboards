using System;

namespace KLPlugins.DynLeaderboards.Settings {

    [Flags]
    public enum OutDriverProp {
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
        CategoryColor = 1 << 12,
    }

    internal static class OutDriverPropExtensions {

        public static bool Includes(this OutDriverProp p, OutDriverProp o) => (p & o) != 0;

        public static void Combine(ref this OutDriverProp p, OutDriverProp o) {
            p |= o;
        }

        public static void Remove(ref this OutDriverProp p, OutDriverProp o) => p &= ~o;

        public static string ToolTipText(this OutDriverProp p) {
            switch (p) {
                case OutDriverProp.None:
                    return "None";

                case OutDriverProp.FirstName:
                    return "First name (Abcde)";

                case OutDriverProp.LastName:
                    return "Last name (Fghij)";

                case OutDriverProp.ShortName:
                    return "Short name (AFG)";

                case OutDriverProp.FullName:
                    return "Full name (Abcde Fghij)";

                case OutDriverProp.InitialPlusLastName:
                    return "Initial + first name (A. Fghij)";

                case OutDriverProp.Nationality:
                    return "Nationality";

                case OutDriverProp.Category:
                    return "Driver category (Platinum, Gold, Silver, Bronze)";

                case OutDriverProp.TotalLaps:
                    return "Total number of completed laps";

                case OutDriverProp.TotalDrivingTime:
                    return "Total driving time in seconds";

                case OutDriverProp.BestLapTime:
                    return "Best lap time in seconds";

                case OutDriverProp.CategoryColor:
                    return "Color for driver category";

                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }
}