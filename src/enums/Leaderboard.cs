namespace KLPlugins.DynLeaderboards {

    // IMPORTANT: new leaderboards need to be added to the end in order to not break older configurations
    public enum Leaderboard {
        None,
        Overall,
        Class,
        RelativeOverall,
        RelativeClass,
        PartialRelativeOverall,
        PartialRelativeClass,
        RelativeOnTrack,
        RelativeOnTrackWoPit,
        Cup,
        RelativeCup,
        PartialRelativeCup,
    }

    internal static class LeaderboardExtensions {

        internal static string Tooltip(this Leaderboard l) {
            return l switch {
                Leaderboard.Overall => "`N` top overall positions. `N` can be set below.",
                Leaderboard.Class => "`N` top class positions. `N` can be set below.",
                Leaderboard.Cup => "`N` top class and cup positions. `N` can be set below.",
                Leaderboard.RelativeOverall => "`2N + 1` relative positions to the focused car in overall order. `N` can be set below.",
                Leaderboard.RelativeClass => "`2N + 1` relative positions to the focused car in focused car's class order. `N` can be set below.",
                Leaderboard.RelativeCup => "`2N + 1` relative positions to the focused car in focused car's class and cup order. `N` can be set below.",
                Leaderboard.RelativeOnTrack => "`2N + 1` relative positions to the focused car on track. `N` can be set below.",
                Leaderboard.RelativeOnTrackWoPit => "`2N + 1` relative positions to the focused car on track excluding the cars in the pitlane which are not on the same lap as the focused car. `N` can be set below.",
                Leaderboard.PartialRelativeOverall => "`N` top positions and `2M + 1` relative positions in overall order. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. `N` and `M` can be set below.",
                Leaderboard.PartialRelativeClass => "`N` top positions and `2M + 1` relative positions in focused car's class order. If the focused car is inside the first `N + M + 1` positions the order will be just as the class leaderboard. `N` and `M` can be set below.",
                Leaderboard.PartialRelativeCup => "`N` top positions and `2M + 1` relative positions in focused car's class and cup order. If the focused car is inside the first `N + M + 1` positions the order will be just as the cup leaderboard. `N` and `M` can be set below.",
                _ => "Unknown",
            };
        }
    }
}