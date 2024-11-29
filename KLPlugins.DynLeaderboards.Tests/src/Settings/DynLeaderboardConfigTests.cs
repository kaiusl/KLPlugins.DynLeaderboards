using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Settings;

using Newtonsoft.Json;

using Xunit;

namespace KLPlugins.DynLeaderboards.Tests.Settings;


[TestSubject(typeof(DynLeaderboardConfig))]
public class DynLeaderboardConfigTests {
    [Fact]
    public void JsonRoundTrip() {
        const string json = """
                            {
                              "Version": 421,
                              "Name": "Dynamic2",
                              "OutCarProps": 2,
                              "OutPitProps": 2,
                              "OutPosProps": 8,
                              "OutGapProps": 4,
                              "OutStingProps": 0,
                              "OutDriverProps": 32,
                              "OutLapProps": 8,
                              "NumOverallPos": 12,
                              "NumClassPos": 10,
                              "NumCupPos": 11,
                              "NumOnTrackRelativePos": 1,
                              "NumOverallRelativePos": 2,
                              "NumClassRelativePos": 3,
                              "NumCupRelativePos": 4,
                              "NumDrivers": 3,
                              "PartialRelativeOverallNumOverallPos": 8,
                              "PartialRelativeOverallNumRelativePos": 7,
                              "PartialRelativeClassNumClassPos": 10,
                              "PartialRelativeClassNumRelativePos": 9,
                              "PartialRelativeCupNumCupPos": 6,
                              "PartialRelativeCupNumRelativePos": 11,
                              "Order": [
                                {
                                  "Kind": 1,
                                  "RemoveIfSingleClass": false,
                                  "RemoveIfSingleCup": false,
                                  "IsEnabled": false
                                },
                                {
                                  "Kind": 2,
                                  "RemoveIfSingleClass": true,
                                  "RemoveIfSingleCup": true,
                                  "IsEnabled": false
                                },
                                {
                                  "Kind": 9,
                                  "RemoveIfSingleClass": true,
                                  "RemoveIfSingleCup": false,
                                  "IsEnabled": true
                                },
                                {
                                  "Kind": 5,
                                  "RemoveIfSingleClass": false,
                                  "RemoveIfSingleCup": true,
                                  "IsEnabled": true
                                },
                                {
                                  "Kind": 6,
                                  "RemoveIfSingleClass": true,
                                  "RemoveIfSingleCup": true,
                                  "IsEnabled": false
                                },
                                {
                                  "Kind": 11,
                                  "RemoveIfSingleClass": false,
                                  "RemoveIfSingleCup": false,
                                  "IsEnabled": true
                                }
                              ],
                              "CurrentLeaderboardIdx": 0,
                              "IsEnabled": false
                            }
                            """;
        var settings = JsonConvert.DeserializeObject<DynLeaderboardConfig>(json);
        Assert.NotNull(settings);
        
        var newJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
        Assert.Equal(json, newJson);
    }
}