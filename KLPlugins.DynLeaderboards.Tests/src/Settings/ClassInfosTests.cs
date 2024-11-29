using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Settings;

using Newtonsoft.Json;

using Xunit;

namespace KLPlugins.DynLeaderboards.Tests.Settings;

[TestSubject(typeof(OverridableClassInfo))]
public class OverridableClassInfoTests {
    [Theory]
    [InlineData("""{"Overrides":null,"IsColorEnabled":true,"IsReplaceWithEnabled":false,"DuplicatedFrom":[]}""")]
    [InlineData(
        """{"Overrides":{"Color":{"Fg":"#ffffff","Bg":"#000000"},"ReplaceWith":"GT3","ShortName":"AD"},"IsColorEnabled":false,"IsReplaceWithEnabled":true,"DuplicatedFrom":["GT3","GT2"]}"""
    )]
    public void JsonRoundTrip(string json) {
        var settings = JsonConvert.DeserializeObject<OverridableClassInfo>(json);
        Assert.NotNull(settings);

        var newJson = JsonConvert.SerializeObject(settings);
        Assert.Equal(json, newJson);
    }
}

[TestSubject(typeof(ClassInfo))]
public class ClassInfoTests {
    [Theory]
    [InlineData("""{"Color":{"Fg":"#ffffff","Bg":"#000000"},"ReplaceWith":"GT3","ShortName":"AD"}""")]
    [InlineData("""{"Color":null,"ReplaceWith":null,"ShortName":null}""")]
    public void JsonRoundTrip(string json) {
        var settings = JsonConvert.DeserializeObject<ClassInfo>(json);
        Assert.NotNull(settings);

        var newJson = JsonConvert.SerializeObject(settings);
        Assert.Equal(json, newJson);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"ReplaceWith":"GT3"}""")]
    public void FromJson(string json) {
        var settings = JsonConvert.DeserializeObject<ClassInfo>(json);
        Assert.NotNull(settings);
    }
}

[TestSubject(typeof(SimHubClassColors))]
public class SimHubClassColorsTest {
    [Fact]
    public void FromJson() {
        const string JSON = """
                            {"AssignedColors": [
                                {"Target": "GT3", "Color": "#FFFF1493"},
                                {"Target": "GT2", "Color": "#FFCA2B30"},
                            ]} 
                            """;

        var color = SimHubClassColors.FromJson(JSON);
        Assert.Equal(2, color.AssignedColors.Count);
        var gt3 = color.AssignedColors[new CarClass("GT3")];
        Assert.NotNull(gt3);
        Assert.Equal("#FFFF1493", gt3.Bg);

        var gt2 = color.AssignedColors[new CarClass("GT2")];
        Assert.NotNull(gt2);
        Assert.Equal("#FFCA2B30", gt2.Bg);
    }
}