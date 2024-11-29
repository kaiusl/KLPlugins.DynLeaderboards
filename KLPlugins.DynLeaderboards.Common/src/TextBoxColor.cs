using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Common;

[JsonObject(MemberSerialization.OptIn)]
[method: JsonConstructor]
public class TextBoxColor(string fg, string bg) {
    public const string DEF_FG = "#FFFFFF";
    public const string DEF_BG = "#000000";
    [JsonProperty("Fg", Required = Required.Always)]
    public string Fg { get; set; } = fg;

    [JsonProperty("Bg", Required = Required.Always)]
    public string Bg { get; set; } = bg;

    public TextBoxColor Clone() {
        return new TextBoxColor(this.Fg, this.Bg);
    }

    public ReadOnlyTextBoxColor AsReadonly() => new(this);

    public static TextBoxColor Default() {
        return new TextBoxColor(TextBoxColor.DEF_FG, TextBoxColor.DEF_BG);
    }

    public static TextBoxColor FromFg(string fg) {
        var bg = ColorTools.ComplementaryBlackOrWhite(fg);
        return new TextBoxColor(fg: fg, bg: bg);
    }

    public static TextBoxColor FromBg(string bg) {
        var fg = ColorTools.ComplementaryBlackOrWhite(bg);
        return new TextBoxColor(fg: fg, bg: bg);
    }
}

public readonly struct ReadOnlyTextBoxColor {
    private readonly TextBoxColor _inner;

    internal ReadOnlyTextBoxColor(TextBoxColor inner) => this._inner = inner;

    public string Fg => this._inner.Fg;
    public string Bg => this._inner.Bg;

    public TextBoxColor ToOwned() => this._inner.Clone();
}