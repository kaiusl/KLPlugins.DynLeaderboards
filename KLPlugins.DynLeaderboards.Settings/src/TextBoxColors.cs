using System.Collections;
using System.Collections.Generic;
using System.IO;

using KLPlugins.DynLeaderboards.Common;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings;

public class TextBoxColors<K> : IEnumerable<KeyValuePair<K, OverridableTextBoxColor>> {
    private readonly SortedDictionary<K, OverridableTextBoxColor> _colors;

    internal TextBoxColors(SortedDictionary<K, OverridableTextBoxColor> colors) {
        this._colors = colors;
    }

    public OverridableTextBoxColor GetOrAdd(K key) {
        if (!this._colors.ContainsKey(key)) {
            var c = new OverridableTextBoxColor();
            c.Disable();
            this._colors[key] = c;
        }

        return this._colors[key];
    }

    internal bool ContainsKey(K key) {
        return this._colors.ContainsKey(key);
    }

    internal void Remove(K key) {
        if (!this._colors.ContainsKey(key)) {
            return;
        }

        this._colors.Remove(key);
    }

    public IEnumerator<KeyValuePair<K, OverridableTextBoxColor>> GetEnumerator() {
        return this._colors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this._colors.GetEnumerator();
    }

    internal static TextBoxColors<K> ReadFromJson(string path, string basePath) {
        SortedDictionary<K, OverridableTextBoxColor>? colors = null;
        if (File.Exists(path)) {
            var json = File.ReadAllText(path);
            colors = JsonConvert.DeserializeObject<SortedDictionary<K, OverridableTextBoxColor>>(json);
        }

        colors ??= new SortedDictionary<K, OverridableTextBoxColor>();

        if (File.Exists(basePath)) {
            var json = File.ReadAllText(basePath);
            var bases = JsonConvert.DeserializeObject<Dictionary<K, TextBoxColor>>(json) ?? [];
            foreach (var kv in bases) {
                if (colors.ContainsKey(kv.Key)) {
                    colors[kv.Key].SetBase(kv.Value);
                } else {
                    var color = new OverridableTextBoxColor(kv.Value);
                    colors[kv.Key] = color;
                }
            }
        }

        return new TextBoxColors<K>(colors);
    }

    internal void WriteToJson(string path) {
        File.WriteAllText(path, JsonConvert.SerializeObject(this._colors, Formatting.Indented));
    }
}

public class OverridableTextBoxColor {
    [JsonIgnore] public const string DEF_FG = "#FFFFFF";

    [JsonIgnore] public const string DEF_BG = "#000000";

    [JsonIgnore] private TextBoxColor? _base;

    [JsonProperty("overrides")] private TextBoxColor? _overrides;

    [JsonProperty] public bool IsEnabled { get; private set; } = true;

    internal OverridableTextBoxColor(TextBoxColor @base) {
        this._base = @base;
    }

    internal OverridableTextBoxColor() { }

    internal void SetOverrides(TextBoxColor? overrides) {
        this._overrides = overrides;
    }

    internal void SetBase(TextBoxColor? @base) {
        this._base = @base;

        if (this.ForegroundDontCheckEnabled() == null || this.BackgroundDontCheckEnabled() == null) {
            this.Disable();
        }
    }

    internal bool HasBase() {
        return this._base != null;
    }

    internal void Reset() {
        // default is enabled if base is present
        this.IsEnabled = this.HasBase();
        this._overrides = null;
    }

    internal void Enable() {
        this.IsEnabled = true;

        // we are explicitly asked to enable colors
        // there must be some value in it
        this._overrides = this._base == null ? TextBoxColor.Default() : this._base.Clone();
    }

    internal void Disable() {
        this.IsEnabled = false;
    }

    public string? Foreground() {
        if (!this.IsEnabled) {
            return null;
        }

        return this.ForegroundDontCheckEnabled();
    }

    internal string? ForegroundDontCheckEnabled() {
        return this._overrides?.Fg ?? this._base?.Fg;
    }

    internal string? BaseForeground() {
        return this._base?.Fg;
    }

    internal void SetForeground(string fg) {
        if (this._overrides == null) {
            this._overrides = TextBoxColor.FromFg(fg);
        } else {
            this._overrides.Fg = fg;
        }
    }

    public string? Background() {
        if (!this.IsEnabled) {
            return null;
        }

        return this.BackgroundDontCheckEnabled();
    }

    internal string? BackgroundDontCheckEnabled() {
        return this._overrides?.Bg ?? this._base?.Bg;
    }

    internal string? BaseBackground() {
        return this._base?.Bg;
    }

    internal void SetBackground(string bg) {
        if (this._overrides == null) {
            this._overrides = TextBoxColor.FromBg(bg);
        } else {
            this._overrides.Bg = bg;
        }
    }
}

public class TextBoxColor {
    [JsonProperty] public string Fg { get; internal set; }

    [JsonProperty] public string Bg { get; internal set; }

    [JsonConstructor]
    public TextBoxColor(string fg, string bg) {
        this.Fg = fg;
        this.Bg = bg;
    }

    internal TextBoxColor Clone() {
        return new TextBoxColor(this.Fg, this.Bg);
    }

    internal static TextBoxColor Default() {
        return new TextBoxColor(OverridableTextBoxColor.DEF_FG, OverridableTextBoxColor.DEF_BG);
    }

    internal static TextBoxColor FromFg(string fg) {
        var bg = ColorTools.ComplementaryBlackOrWhite(fg);
        return new TextBoxColor(fg: fg, bg: bg);
    }

    internal static TextBoxColor FromBg(string bg) {
        var fg = ColorTools.ComplementaryBlackOrWhite(bg);
        return new TextBoxColor(fg: fg, bg: bg);
    }
}