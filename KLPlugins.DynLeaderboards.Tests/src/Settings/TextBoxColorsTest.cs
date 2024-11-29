using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Settings;

using Newtonsoft.Json;

using Xunit;

namespace KLPlugins.DynLeaderboards.Tests.Settings.TextBoxColorsTests;

[TestSubject(typeof(TextBoxColors<object>))]
public class TextBoxColorsTest {
    [Fact]
    public void NotSerializable() {
        Assert.Throws<NotSerializableException>(() => JsonConvert.SerializeObject(new TextBoxColors<object>([])));
        Assert.Throws<NotDeserializableException>(() => JsonConvert.DeserializeObject<TextBoxColors<object>>("{}"));
    }
}

[TestSubject(typeof(OverridableTextBoxColor))]
public class OverridableTextBoxColorTest {
    [Theory]
    [InlineData("""{"Overrides":null,"IsEnabled":true}""")]
    [InlineData("""{"Overrides":{"Fg":"fg","Bg":"bg"},"IsEnabled":false}""")]
    public void JsonRoundTrip(string json) {
        var settings = JsonConvert.DeserializeObject<OverridableTextBoxColor>(json);
        Assert.NotNull(settings);

        var newJson = JsonConvert.SerializeObject(settings);
        Assert.Equal(json, newJson);
    }
}