using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Settings;

using Newtonsoft.Json;

using Xunit;

namespace KLPlugins.DynLeaderboards.Tests.Settings;

[TestSubject(typeof(PluginSettings))]
public class PluginSettingsTests {
    [Fact]
    public void FromJson() {
        const string JSON = """
                            {
                                "Version": 450,
                                "AccDataLocation": "ACCPath",
                                "AcRootLocation": null,
                                "Log": true,
                                "BroadcastDataUpdateRateMs": 500,
                                "OutGeneralProps": 254
                            }
                            """;
        var settings = JsonConvert.DeserializeObject<PluginSettings>(JSON);
        Assert.NotNull(settings);

        Assert.Equal(450, settings!.Version);
        Assert.Equal("ACCPath", settings.AccDataLocation);
        Assert.Null(settings.AcRootLocation);
        Assert.True(settings.Log);
        Assert.Equal(500, settings.BroadcastDataUpdateRateMs);
        Assert.Equal((OutGeneralProp)254, settings.OutGeneralProps.Value);
    }

    [Fact]
    public void ToJson() {
        const string JSON = """
                            {
                              "Version": 450,
                              "AccDataLocation": "ACCPath",
                              "AcRootLocation": null,
                              "Log": true,
                              "BroadcastDataUpdateRateMs": 500,
                              "OutGeneralProps": 254
                            }
                            """;
        var settings = JsonConvert.DeserializeObject<PluginSettings>(JSON);
        Assert.NotNull(settings);

        var newJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
        Assert.Equal(JSON, newJson);
    }
}