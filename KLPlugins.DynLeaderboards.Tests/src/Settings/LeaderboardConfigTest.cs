using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Settings;

using Newtonsoft.Json;

using Xunit;

namespace KLPlugins.DynLeaderboards.Tests.Settings;

[TestSubject(typeof(LeaderboardConfig))]
public class LeaderboardConfigTest {
    [Fact]
    public void FromJsonV3() {
        const string JSON = """
                            {
                              "Kind": 2,
                              "RemoveIfSingleClass": true,
                              "RemoveIfSingleCup": true,
                              "IsEnabled": false
                            }
                            """;
        var settings = JsonConvert.DeserializeObject<LeaderboardConfig>(JSON);
        Assert.NotNull(settings);

        var newJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
        Assert.Equal(JSON, newJson);
    }

    [Fact]
    public void FromJsonV2() {
        const string JSON = "2";
        var settings = JsonConvert.DeserializeObject<LeaderboardConfig>(JSON);
        Assert.NotNull(settings);

        Assert.Equal((LeaderboardKind)2, settings!.Kind);
        // none of the leaderboards were removed automatically
        Assert.False(settings.RemoveIfSingleClass);
        Assert.False(settings.RemoveIfSingleCup);
        // if leaderboard was present in old config, it was enabled
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void LeaderboardKindValues() {
        // these cannot ever change!
        Assert.Equal(0, (int)LeaderboardKind.NONE);
        Assert.Equal(1, (int)LeaderboardKind.OVERALL);
        Assert.Equal(2, (int)LeaderboardKind.CLASS);
        Assert.Equal(3, (int)LeaderboardKind.RELATIVE_OVERALL);
        Assert.Equal(4, (int)LeaderboardKind.RELATIVE_CLASS);
        Assert.Equal(5, (int)LeaderboardKind.PARTIAL_RELATIVE_OVERALL);
        Assert.Equal(6, (int)LeaderboardKind.PARTIAL_RELATIVE_CLASS);
        Assert.Equal(7, (int)LeaderboardKind.RELATIVE_ON_TRACK);
        Assert.Equal(8, (int)LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT);
        Assert.Equal(9, (int)LeaderboardKind.CUP);
        Assert.Equal(10, (int)LeaderboardKind.RELATIVE_CUP);
        Assert.Equal(11, (int)LeaderboardKind.PARTIAL_RELATIVE_CUP);
    }
}