namespace KLPlugins.DynLeaderboards {

    public enum Leaderboard {
        None,
        Overall,
        Class,
        RelativeOverall,
        RelativeClass,
        PartialRelativeOverall,
        PartialRelativeClass,
        RelativeOnTrack
    }

    internal static class LeaderboardExtensions {

        public static string Tooltip(this Leaderboard l) {
            switch (l) {
                case Leaderboard.Overall:
                    return "`N` top overall positions. `N` can be set below.";

                case Leaderboard.Class:
                    return "`N` top class positions. `N` can be set below.";

                case Leaderboard.RelativeOverall:
                    return "`2N + 1` relative positions to the focused car in overall order. `N` can be set below.";

                case Leaderboard.RelativeClass:
                    return "`2N + 1` relative positions to the focused car in focused car's class order. `N` can be set below.";

                case Leaderboard.RelativeOnTrack:
                    return "`2N + 1` relative positions to the focused car on track. `N` can be set below.";

                case Leaderboard.PartialRelativeOverall:
                    return "`N` top positions and `2M + 1` relative positions in overall order. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. `N` and `M` can be set below.";

                case Leaderboard.PartialRelativeClass:
                    return "`N` top positions and `2M + 1` relative positions in focused car's class order. If the focused car is inside the first `N + M + 1` positions the order will be just as the class leaderboard. `N` and `M` can be set below.";

                default:
                    return "Unknown";
            }
        }
    }
}