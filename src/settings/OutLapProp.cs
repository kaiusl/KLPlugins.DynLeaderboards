using System;

namespace KLPlugins.DynLeaderboards.Settings {

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

        internal static bool Includes(this OutLapProp p, OutLapProp o) {
            return (p & o) != 0;
        }

        internal static bool IncludesAny(this OutLapProp p, params OutLapProp[] others) {
            foreach (var o in others) {
                if (p.Includes(o)) {
                    return true;
                }
            }
            return false;
        }

        internal static bool IncludesAll(this OutLapProp p, params OutLapProp[] others) {
            foreach (var o in others) {
                if (!p.Includes(o)) {
                    return false;
                }
            }
            return true;
        }

        internal static void Combine(ref this OutLapProp p, OutLapProp o) {
            p |= o;
        }

        internal static void Remove(ref this OutLapProp p, OutLapProp o) {
            p &= ~o;
        }

        internal static OutLapProp[] Order() {
            return new OutLapProp[] {
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

                OutLapProp.LastLapDeltaToLeaderLast,
                OutLapProp.LastLapDeltaToClassLeaderLast,
                OutLapProp.LastLapDeltaToCupLeaderLast,
                OutLapProp.LastLapDeltaToFocusedLast,
                OutLapProp.LastLapDeltaToAheadLast,
                OutLapProp.LastLapDeltaToAheadInClassLast,
                OutLapProp.LastLapDeltaToAheadInCupLast,
                
                OutLapProp.DynamicBestLapDeltaToFocusedBest,
                OutLapProp.DynamicLastLapDeltaToFocusedBest,
                OutLapProp.DynamicLastLapDeltaToFocusedLast,
            };
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
}