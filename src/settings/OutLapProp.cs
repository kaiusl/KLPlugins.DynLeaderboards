using System;


namespace KLPlugins.Leaderboard {
    [Flags]
    public enum OutLapProp {
        None = 0,
        Laps = 1 << 0,
        LastLapTime = 1 << 1,
        LastLapSectors = 1 << 2,
        BestLapTime = 1 << 3,
        BestLapSectors = 1 << 4,
        BestSectors = 1 << 5,
        CurrentLapTime = 1 << 6,

        BestLapDeltaToOverallBest = 1 << 10,
        BestLapDeltaToClassBest = 1 << 11,
        BestLapDeltaToLeaderBest = 1 << 12,
        BestLapDeltaToClassLeaderBest = 1 << 13,
        BestLapDeltaToFocusedBest = 1 << 14,
        BestLapDeltaToAheadBest = 1 << 15,
        BestLapDeltaToAheadInClassBest = 1 << 16,

        LastLapDeltaToOverallBest = 1 << 17,
        LastLapDeltaToClassBest = 1 << 18,
        LastLapDeltaToLeaderBest = 1 << 19,
        LastLapDeltaToClassLeaderBest = 1 << 20,
        LastLapDeltaToFocusedBest = 1 << 21,
        LastLapDeltaToAheadBest = 1 << 22,
        LastLapDeltaToAheadInClassBest = 1 << 23,
        LastLapDeltaToOwnBest = 1 << 24,

        LastLapDeltaToLeaderLast = 1 << 25,
        LastLapDeltaToClassLeaderLast = 1 << 26,
        LastLapDeltaToFocusedLast = 1 << 27,
        LastLapDeltaToAheadLast = 1 << 28,
        LastLapDeltaToAheadInClassLast = 1 << 29,
    }

    static class OutLapPropExtensions {
        public static bool Includes(this OutLapProp p, OutLapProp o) => (p & o) != 0;
        public static bool IncludesAny(this OutLapProp p, params OutLapProp[] others) {
            foreach (var o in others) {
                if (p.Includes(o)) {
                    return true;
                }
            }
            return false;
        }
        public static bool IncludesAll(this OutLapProp p, params OutLapProp[] others) {
            foreach (var o in others) {
                if (!p.Includes(o)) {
                    return false;
                }
            }
            return true;
        }

        public static void Combine(ref this OutLapProp p, OutLapProp o) => p |= o;
        public static void Remove(ref this OutLapProp p, OutLapProp o) => p &= ~o;

        public static string ToPropName(this OutLapProp p) {
            switch (p) {
                case OutLapProp.Laps:
                    return "Laps.Count";
                case OutLapProp.LastLapTime:
                    return "Laps.Last.Time";
                case OutLapProp.LastLapSectors:
                    return "Laps.Last.S1/2/3";
                case OutLapProp.BestLapTime:
                    return "Laps.Best.Time";
                case OutLapProp.BestLapSectors:
                    return "Laps.Best.S1/2/3";
                case OutLapProp.BestSectors:
                    return "BestS1/2/3";
                case OutLapProp.CurrentLapTime:
                    return "Laps.Current.Time";
                case OutLapProp.BestLapDeltaToOverallBest:
                    return "Laps.Best.Delta.ToOverallBest";
                case OutLapProp.BestLapDeltaToClassBest:
                    return "Laps.Best.Delta.ToClassBest";
                case OutLapProp.BestLapDeltaToLeaderBest:
                    return "Laps.Best.Delta.ToLeaderBest";
                case OutLapProp.BestLapDeltaToClassLeaderBest:
                    return "Laps.Best.Delta.ToClassLeaderBest";
                case OutLapProp.BestLapDeltaToFocusedBest:
                    return "Laps.Best.Delta.ToFocusedBest";
                case OutLapProp.BestLapDeltaToAheadBest:
                    return "Laps.Best.Delta.ToAheadBest";
                case OutLapProp.BestLapDeltaToAheadInClassBest:
                    return "Laps.Best.Delta.ToAheadInClassBest";
                case OutLapProp.LastLapDeltaToOverallBest:
                    return "Laps.Last.Delta.ToOverallBest";
                case OutLapProp.LastLapDeltaToClassBest:
                    return "Laps.Last.Delta.ToClassBest";
                case OutLapProp.LastLapDeltaToLeaderBest:
                    return "Laps.Last.Delta.ToLeaderBest";
                case OutLapProp.LastLapDeltaToClassLeaderBest:
                    return "Laps.Last.Delta.ToClassLeaderBest";
                case OutLapProp.LastLapDeltaToFocusedBest:
                    return "Laps.Last.Delta.ToFocusedBest";
                case OutLapProp.LastLapDeltaToAheadBest:
                    return "Laps.Last.Delta.ToAheadBest";
                case OutLapProp.LastLapDeltaToAheadInClassBest:
                    return "Laps.Last.Delta.ToAheadInClassBest";
                case OutLapProp.LastLapDeltaToOwnBest:
                    return "Laps.Last.Delta.ToOwnBest";
                case OutLapProp.LastLapDeltaToLeaderLast:
                    return "Laps.Last.Delta.ToLeaderLast";
                case OutLapProp.LastLapDeltaToClassLeaderLast:
                    return "Laps.Last.Delta.ToClassLeaderLast";
                case OutLapProp.LastLapDeltaToFocusedLast:
                    return "Laps.Last.Delta.ToFocusedLast";
                case OutLapProp.LastLapDeltaToAheadLast:
                    return "Laps.Last.Delta.ToAheadLast";
                case OutLapProp.LastLapDeltaToAheadInClassLast:
                    return "Laps.Last.Delta.ToAheadInClassLast";
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum variant");
            }
        }

        public static string ToolTipText(this OutLapProp p) {
            switch (p) {
                case OutLapProp.Laps:
                    return "Number of completed laps";
                case OutLapProp.LastLapTime:
                    return "Last lap time in seconds";
                case OutLapProp.LastLapSectors:
                    return "Last lap sector times in seconds";
                case OutLapProp.BestLapTime:
                    return "Best lap time in seconds";
                case OutLapProp.BestLapSectors:
                    return "Best lap sector times in seconds";
                case OutLapProp.BestSectors:
                    return "Best sector times in seconds";
                case OutLapProp.CurrentLapTime:
                    return "Current lap time in seconds";
                case OutLapProp.BestLapDeltaToOverallBest:
                    return "Best lap delta to the overall best lap in seconds.";
                case OutLapProp.BestLapDeltaToClassBest:
                    return "Best lap delta to the class best lap in seconds.";
                case OutLapProp.BestLapDeltaToLeaderBest:
                    return "Best lap delta to the leader's best lap in seconds.";
                case OutLapProp.BestLapDeltaToClassLeaderBest:
                    return "Best lap delta to the class leader's best lap in seconds.";
                case OutLapProp.BestLapDeltaToFocusedBest:
                    return "Best lap delta to the focused car's best lap in seconds.";
                case OutLapProp.BestLapDeltaToAheadBest:
                    return "Best lap delta to the ahead car's best lap in seconds.";
                case OutLapProp.BestLapDeltaToAheadInClassBest:
                    return "Best lap delta to the in class ahead car's best lap in seconds.";
                case OutLapProp.LastLapDeltaToOverallBest:
                    return "Last lap delta to the overall best lap in seconds.";
                case OutLapProp.LastLapDeltaToClassBest:
                    return "Last lap delta to the class best lap in seconds.";
                case OutLapProp.LastLapDeltaToLeaderBest:
                    return "Last lap delta to the leader's best lap in seconds.";
                case OutLapProp.LastLapDeltaToClassLeaderBest:
                    return "Last lap delta to the class leader's best lap in seconds.";
                case OutLapProp.LastLapDeltaToFocusedBest:
                    return "Last lap delta to the focused car's best lap in seconds.";
                case OutLapProp.LastLapDeltaToAheadBest:
                    return "Last lap delta to the ahead car's best lap in seconds.";
                case OutLapProp.LastLapDeltaToAheadInClassBest:
                    return "Last lap delta to the in class car ahead's best lap in seconds.";
                case OutLapProp.LastLapDeltaToOwnBest:
                    return "Last lap delta to own best lap in seconds.";
                case OutLapProp.LastLapDeltaToLeaderLast:
                    return "Last lap delta to the leader's last lap in seconds.";
                case OutLapProp.LastLapDeltaToClassLeaderLast:
                    return "Last lap delta to the class leaders last lap in seconds.";
                case OutLapProp.LastLapDeltaToFocusedLast:
                    return "Last lap delta to the focused car's last lap in seconds.";
                case OutLapProp.LastLapDeltaToAheadLast:
                    return "Last lap delta to the ahead car's last lap in seconds.";
                case OutLapProp.LastLapDeltaToAheadInClassLast:
                    return "Last lap delta to the in class ahead car's last lap in seconds.";
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum variant");
            }
        }
    }

}