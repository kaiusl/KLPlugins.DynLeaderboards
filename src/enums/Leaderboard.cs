using System;

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
    internal static string ToDisplayString(this LeaderboardKind kind) {
        return kind switch {
            LeaderboardKind.NONE => "None",
            LeaderboardKind.OVERALL => "Overall",
            LeaderboardKind.CLASS => "Class",
            LeaderboardKind.RELATIVE_OVERALL => "Relative overall",
            LeaderboardKind.RELATIVE_CLASS => "Relative class",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL => "Partial relative overall",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS => "Partial relative class",
            LeaderboardKind.RELATIVE_ON_TRACK => "Relative on track",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT => "Relative on track (wo pit)",
            LeaderboardKind.CUP => "Cup",
            LeaderboardKind.RELATIVE_CUP => "Relative cup",
            LeaderboardKind.PARTIAL_RELATIVE_CUP => "Partial relative cup",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    internal static string ToCompactString(this LeaderboardKind kind) {
        return kind switch {
            LeaderboardKind.NONE => "None",
            LeaderboardKind.OVERALL => "Overall",
            LeaderboardKind.CLASS => "Class",
            LeaderboardKind.RELATIVE_OVERALL => "RelativeOverall",
            LeaderboardKind.RELATIVE_CLASS => "RelativeClass",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL => "PartialRelativeOverall",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS => "PartialRelativeClass",
            LeaderboardKind.RELATIVE_ON_TRACK => "RelativeOnTrack",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT => "RelativeOnTrackWoPit",
            LeaderboardKind.CUP => "Cup",
            LeaderboardKind.RELATIVE_CUP => "RelativeCup",
            LeaderboardKind.PARTIAL_RELATIVE_CUP => "PartialRelativeCup",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

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