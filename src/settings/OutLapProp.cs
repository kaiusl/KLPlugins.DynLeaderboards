using System;


namespace KLPlugins.DynLeaderboards {
    [Flags]
    public enum OutLapProp : long {
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
                case OutLapProp.DynamicBestLapDeltaToFocusedBest:
                    return "Laps.Best.Delta.Dynamic.ToFocusedBest";
                case OutLapProp.DynamicLastLapDeltaToFocusedBest:
                    return "Laps.Last.Delta.Dynamic.ToFocusedBest";
                case OutLapProp.DynamicLastLapDeltaToFocusedLast:
                    return "Laps.Last.Delta.Dynamic.ToFocusedLast";
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum variant");
            }
        }

        public static string ToolTipText(this OutLapProp p) {
            switch (p) {
                case OutLapProp.Laps:
                    return "Number of completed laps";
                case OutLapProp.LastLapTime:
                    return "Last lap time.";
                case OutLapProp.LastLapSectors:
                    return "Last lap sector times.";
                case OutLapProp.BestLapTime:
                    return "Best lap time.";
                case OutLapProp.BestLapSectors:
                    return "Best lap sector times.";
                case OutLapProp.BestSectors:
                    return "Best sector times.";
                case OutLapProp.CurrentLapTime:
                    return "Current lap time.";
                case OutLapProp.BestLapDeltaToOverallBest:
                    return "Best lap delta to the overall best lap.";
                case OutLapProp.BestLapDeltaToClassBest:
                    return "Best lap delta to the class best lap.";
                case OutLapProp.BestLapDeltaToLeaderBest:
                    return "Best lap delta to the leader's best lap.";
                case OutLapProp.BestLapDeltaToClassLeaderBest:
                    return "Best lap delta to the class leader's best lap.";
                case OutLapProp.BestLapDeltaToFocusedBest:
                    return "Best lap delta to the focused car's best lap.";
                case OutLapProp.BestLapDeltaToAheadBest:
                    return "Best lap delta to the ahead car's best lap.";
                case OutLapProp.BestLapDeltaToAheadInClassBest:
                    return "Best lap delta to the in class ahead car's best lap.";
                case OutLapProp.LastLapDeltaToOverallBest:
                    return "Last lap delta to the overall best lap.";
                case OutLapProp.LastLapDeltaToClassBest:
                    return "Last lap delta to the class best lap.";
                case OutLapProp.LastLapDeltaToLeaderBest:
                    return "Last lap delta to the leader's best lap.";
                case OutLapProp.LastLapDeltaToClassLeaderBest:
                    return "Last lap delta to the class leader's best lap.";
                case OutLapProp.LastLapDeltaToFocusedBest:
                    return "Last lap delta to the focused car's best lap.";
                case OutLapProp.LastLapDeltaToAheadBest:
                    return "Last lap delta to the ahead car's best lap.";
                case OutLapProp.LastLapDeltaToAheadInClassBest:
                    return "Last lap delta to the in class car ahead's best lap.";
                case OutLapProp.LastLapDeltaToOwnBest:
                    return "Last lap delta to own best lap.";
                case OutLapProp.LastLapDeltaToLeaderLast:
                    return "Last lap delta to the leader's last lap.";
                case OutLapProp.LastLapDeltaToClassLeaderLast:
                    return "Last lap delta to the class leaders last lap.";
                case OutLapProp.LastLapDeltaToFocusedLast:
                    return "Last lap delta to the focused car's last lap.";
                case OutLapProp.LastLapDeltaToAheadLast:
                    return "Last lap delta to the ahead car's last lap.";
                case OutLapProp.LastLapDeltaToAheadInClassLast:
                    return "Last lap delta to the in class ahead car's last lap.";
                case OutLapProp.DynamicBestLapDeltaToFocusedBest:
                    return @"Best lap delta to the car's best based on currently displayed dynamic leaderboard. 
Overall -> delta to leader's best lap, 
Class -> delta to class leader's best lap, 
Any relative -> delta to focused car's best lap";
                case OutLapProp.DynamicLastLapDeltaToFocusedBest:
                    return @"Last lap delta to the car's best based on currently displayed dynamic leaderboard. 
Overall -> delta to leader's best lap,
Class -> delta to class leader's best lap, 
Any relative -> delta to focused car's best lap";
                case OutLapProp.DynamicLastLapDeltaToFocusedLast:
                    return @"Last lap delta to the car's last based on currently displayed dynamic leaderboard. 
Overall -> delta to leader's last lap, 
Class -> delta to class leader's last lap, 
Any relative -> delta to focused car's last lap";
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum variant");
            }
        }
    }

}