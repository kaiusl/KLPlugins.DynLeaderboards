namespace KLPlugins.DynLeaderboards;

// IMPORTANT: new leaderboards need to be added to the end in order to not break older configurations
public enum LeaderboardKind {
    NONE,
    OVERALL,
    CLASS,
    RELATIVE_OVERALL,
    RELATIVE_CLASS,
    PARTIAL_RELATIVE_OVERALL,
    PARTIAL_RELATIVE_CLASS,
    RELATIVE_ON_TRACK,
    RELATIVE_ON_TRACK_WO_PIT,
    CUP,
    RELATIVE_CUP,
    PARTIAL_RELATIVE_CUP,
}

internal static class LeaderboardExtensions {
    internal static string Tooltip(this LeaderboardKind l) {
        return l switch {
            LeaderboardKind.OVERALL => "`N` top overall positions. `N` can be set below.",
            LeaderboardKind.CLASS => "`N` top class positions. `N` can be set below.",
            LeaderboardKind.CUP => "`N` top class and cup positions. `N` can be set below.",
            LeaderboardKind.RELATIVE_OVERALL =>
                "`2N + 1` relative positions to the focused car in overall order. `N` can be set below.",
            LeaderboardKind.RELATIVE_CLASS =>
                "`2N + 1` relative positions to the focused car in focused car's class order. `N` can be set below.",
            LeaderboardKind.RELATIVE_CUP =>
                "`2N + 1` relative positions to the focused car in focused car's class and cup order. `N` can be set below.",
            LeaderboardKind.RELATIVE_ON_TRACK =>
                "`2N + 1` relative positions to the focused car on track. `N` can be set below.",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT =>
                "`2N + 1` relative positions to the focused car on track excluding the cars in the pit lane which are not on the same lap as the focused car. `N` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL =>
                "`N` top positions and `2M + 1` relative positions in overall order. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. `N` and `M` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS =>
                "`N` top positions and `2M + 1` relative positions in focused car's class order. If the focused car is inside the first `N + M + 1` positions the order will be just as the class leaderboard. `N` and `M` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_CUP =>
                "`N` top positions and `2M + 1` relative positions in focused car's class and cup order. If the focused car is inside the first `N + M + 1` positions the order will be just as the cup leaderboard. `N` and `M` can be set below.",
            _ => "Unknown",
        };
    }
}