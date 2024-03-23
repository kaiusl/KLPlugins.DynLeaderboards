using System;

namespace KLPlugins.DynLeaderboards.Settings {

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
        CategoryColor = 1 << 12,
    }

    internal static class OutDriverPropExtensions {

        internal static bool Includes(this OutDriverProp p, OutDriverProp o) {
            return (p & o) != 0;
        }

        internal static void Combine(ref this OutDriverProp p, OutDriverProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutDriverProp p, OutDriverProp o) {
            p &= ~o;
        }

        internal static OutDriverProp[] Order() {
            return new[] {
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
                OutDriverProp.CategoryColor
             };
        }

        internal static string ToolTipText(this OutDriverProp p) {
            return p switch {
                OutDriverProp.None => "None",
                OutDriverProp.FirstName => "First name (Abcde)",
                OutDriverProp.LastName => "Last name (Fghij)",
                OutDriverProp.ShortName => "Short name (AFG)",
                OutDriverProp.FullName => "Full name (Abcde Fghij)",
                OutDriverProp.InitialPlusLastName => "Initial + first name (A. Fghij)",
                OutDriverProp.Nationality => "Nationality",
                OutDriverProp.Category => "Driver category (Platinum, Gold, Silver, Bronze)",
                OutDriverProp.TotalLaps => "Total number of completed laps",
                OutDriverProp.TotalDrivingTime => "Total driving time in seconds",
                OutDriverProp.BestLapTime => "Best lap time in seconds",
                OutDriverProp.CategoryColor => "Color for driver category",
                _ => throw new ArgumentOutOfRangeException($"Invalid enum variant {p}"),
            };
        }
    }
}