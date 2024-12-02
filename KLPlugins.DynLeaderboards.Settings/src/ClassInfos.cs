using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings;

// Use FromJson and WriteToJson methods
[JsonConverter(typeof(FailJsonConverter))]
public sealed class ClassInfos : IEnumerable<KeyValuePair<CarClass, OverridableClassInfo>> {
    private readonly Dictionary<CarClass, OverridableClassInfo> _infos;
    private readonly SimHubClassColors _simHubClassColors;

    private ClassInfos(Dictionary<CarClass, OverridableClassInfo> infos, SimHubClassColors simHubClassColors) {
        this._infos = infos;
        this._simHubClassColors = simHubClassColors;
    }

    internal OverridableClassInfo GetOrAdd(CarClass cls) {
        if (!this._infos.ContainsKey(cls)) {
            var c = new OverridableClassInfo(null, null);
            if (this._simHubClassColors.AssignedColors.TryGetValue(cls, out var shColor)) {
                c._SimHubColor = shColor;
            }

            this._infos[cls] = c;
        }

        return this._infos[cls];
    }

    public (CarClass, OverridableClassInfo) GetFollowReplaceWith(CarClass cls) {
        var clsOut = cls;
        var info = this.GetOrAdd(cls);
        var nextCls = info.ReplaceWith;

        var seenClasses = new List<CarClass> { cls };

        while (nextCls != null && nextCls != clsOut) {
            clsOut = nextCls.Value;
            info = this.GetOrAdd(clsOut);

            if (seenClasses.Contains(clsOut)) {
                Logging.LogWarn(
                    $"Loop detected in class \"replace with\" values: {string.Join(" -> ", seenClasses)} -> {clsOut}"
                );
                break;
            }

            seenClasses.Add(clsOut);

            nextCls = info.ReplaceWith;
        }

        return (clsOut, info);
    }

    internal ClassInfo? GetBaseFollowDuplicates(CarClass cls) {
        var info = this.GetOrAdd(cls);

        if (info._Base != null) {
            return info._Base;
        }

        foreach (var dup in info._DuplicatedFrom.Reverse())
            // Don't use this.Get as that will add missing classes
            // We don't want it here, just skip the duplicates that don't exist anymore
        {
            if (this._infos.TryGetValue(dup, out info)) {
                if (info._Base != null) {
                    return info._Base;
                }
            }
        }

        return null;
    }

    internal static ClassInfos ReadFromJson(string path, string basePath) {
        Dictionary<CarClass, OverridableClassInfo>? infos = null;
        if (File.Exists(path)) {
            var json = File.ReadAllText(path);
            infos = JsonConvert.DeserializeObject<Dictionary<CarClass, OverridableClassInfo>>(json);
        }

        infos ??= [];

        if (File.Exists(basePath)) {
            var json = File.ReadAllText(basePath);
            var bases = JsonConvert.DeserializeObject<Dictionary<CarClass, ClassInfo>>(json) ?? [];
            foreach (var kv in bases) {
                if (infos.ContainsKey(kv.Key)) {
                    infos[kv.Key].SetRealBase(kv.Value);
                } else {
                    var isColorEnabled = kv.Value._Color?.Fg != null && kv.Value._Color?.Bg != null;
                    var isReplaceWithEnabled = kv.Value._ReplaceWith != null;
                    var info = new OverridableClassInfo(
                        @base: kv.Value,
                        null,
                        isColorEnabled: isColorEnabled,
                        isReplaceWithEnabled: isReplaceWithEnabled
                    );

                    infos[kv.Key] = info;
                }
            }
        }

        var defBase = new ClassInfo(
            color: TextBoxColor.Default(),
            replaceWith: null,
            shortName: "-"
        );
        if (infos.ContainsKey(CarClass.Default)) {
            infos[CarClass.Default].SetRealBase(defBase);
        } else {
            infos[CarClass.Default] = new OverridableClassInfo(@base: defBase, overrides: null);
        }

        var simHubClassColorsPath = PluginPaths._SimhubClassColorsPath;
        SimHubClassColors simHubClassColors;
        if (File.Exists(simHubClassColorsPath)) {
            var json = File.ReadAllText(simHubClassColorsPath);
            simHubClassColors = SimHubClassColors.FromJson(json);
        } else {
            simHubClassColors = new SimHubClassColors();
        }

        var c = new ClassInfos(infos, simHubClassColors);

        // Make sure that all "replace with" values are also in the dict
        foreach (var key in c._infos.Keys.ToList()) {
            var it = c.GetOrAdd(key);
            if (it._ReplaceWithDontCheckEnabled != null) {
                var _ = c.GetOrAdd(it._ReplaceWithDontCheckEnabled!.Value);
            }

            if (it._BaseReplaceWith != null) {
                var _ = c.GetOrAdd(it._BaseReplaceWith!.Value);
            }

            if (it._Base == null) {
                it.SetBase(c.GetBaseFollowDuplicates(key));
            }
        }

        foreach (var kv in c._infos) {
            if (c._simHubClassColors.AssignedColors.TryGetValue(kv.Key, out var shColor)) {
                kv.Value._SimHubColor = shColor;
            }

            kv.Value.CheckEnabled();
        }

        return c;
    }

    internal void WriteToJson(string path) {
        File.WriteAllText(path, JsonConvert.SerializeObject(this._infos, Formatting.Indented));
    }

    public IEnumerator<KeyValuePair<CarClass, OverridableClassInfo>> GetEnumerator() {
        return this._infos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this._infos.GetEnumerator();
    }

    private class FailJsonConverter : Common.FailJsonConverter {
        public FailJsonConverter() {
            this.SerializeMsg =
                $"`{nameof(ClassInfos)}` cannot be serialized, use `{nameof(ClassInfos.WriteToJson)}` method instead";
            this.DeserializeMsg =
                $"`{nameof(ClassInfos)}` cannot be deserialized, use `{nameof(ClassInfos.ReadFromJson)}` method instead";
        }
    }


    /// <summary>
    ///     This is the glue between ClassInfos and settings UI
    /// </summary>
    internal sealed class Manager
        : IEnumerable<KeyValuePair<CarClass, OverridableClassInfo.Manager>>, INotifyCollectionChanged {
        private readonly ClassInfos _baseInfos;
        private readonly Dictionary<CarClass, OverridableClassInfo.Manager> _classManagers = [];
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        internal Manager(ClassInfos infos) {
            this._baseInfos = infos;
            this.Update();
        }

        internal void Update() {
            foreach (var kv in this._baseInfos) {
                this.TryAdd(kv.Key, kv.Value);
            }
        }

        internal OverridableClassInfo.Manager? TryAdd(CarClass cls) {
            if (this._classManagers.ContainsKey(cls)) {
                return null;
            }

            return this.AddDoesntExist(cls);
        }

        internal OverridableClassInfo.Manager? TryAdd(CarClass cls, OverridableClassInfo info) {
            if (this._classManagers.ContainsKey(cls)) {
                return null;
            }

            return this.AddDoesntExist(cls, info);
        }

        internal OverridableClassInfo.Manager AddDoesntExist(CarClass cls) {
            var info = this._baseInfos.GetOrAdd(cls);
            return this.AddDoesntExist(cls, info);
        }

        internal OverridableClassInfo.Manager AddDoesntExist(CarClass cls, OverridableClassInfo info) {
            var newItem = new OverridableClassInfo.Manager(cls, info);
            this._classManagers[cls] = newItem;
            this.CollectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem)
            );
            return newItem;
        }

        internal OverridableClassInfo.Manager? Get(CarClass cls) {
            return !this._classManagers.TryGetValue(cls, out var manager) ? null : manager;
        }

        internal OverridableClassInfo.Manager GetOrAdd(CarClass cls) {
            return !this._classManagers.TryGetValue(cls, out var add) ? this.AddDoesntExist(cls) : add;
        }

        internal OverridableClassInfo.Manager GetOrAddFollowReplaceWith(CarClass cls) {
            var clsOut = cls;
            var manager = this.GetOrAdd(cls);
            var nextCls = manager._Info.ReplaceWith; // don't use manager.ReplaceWith, it defaults to default class

            var seenClasses = new List<CarClass> { cls };

            while (nextCls != null && nextCls != clsOut) {
                clsOut = nextCls.Value;
                manager = this.GetOrAdd(clsOut);

                if (seenClasses.Contains(clsOut)) {
                    Logging.LogWarn(
                        $"Loop detected in class \"replace with\" values: {string.Join(" -> ", seenClasses)} -> {clsOut}"
                    );
                    break;
                }

                seenClasses.Add(clsOut);

                nextCls = manager._Info.ReplaceWith;
            }

            return manager;
        }

        internal void Remove(CarClass cls) {
            if (!this._classManagers.TryGetValue(cls, out var manager)) {
                return;
            }

            if (this.CanBeRemoved(manager)) {
                this._classManagers.Remove(cls);
                this._baseInfos._infos.Remove(cls);
                this.CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, manager)
                );
            } else {
                // Don't remove if it has base data or if it is the default class,
                // just disable
                manager.Reset();
                manager._IsColorEnabled = false;
                manager._IsReplaceWithEnabled = false;
            }
        }

        internal bool CanBeRemoved(CarClass cls) {
            if (!this._classManagers.ContainsKey(cls)) {
                return false;
            }

            return this.CanBeRemovedKnownToExist(cls);
        }

        internal bool CanBeRemovedKnownToExist(CarClass cls) {
            var c = this._classManagers[cls];
            return this.CanBeRemoved(c);
        }

        internal bool CanBeRemoved(OverridableClassInfo.Manager c) {
            if (c.HasBase() || c._Key == CarClass.Default || this.IsUsedInAnyReplaceWith(c._Key)) {
                return false;
            }

            return true;
        }

        internal bool IsUsedInAnyReplaceWith(CarClass cls) {
            return this._classManagers.Values.Any(it => it._ReplaceWith == cls);
        }

        internal void Duplicate(CarClass old, CarClass @new) {
            if (!this._classManagers.ContainsKey(old) || this._classManagers.ContainsKey(@new)) {
                return;
            }

            var info = this._classManagers[old]._Info.Duplicate(old);
            if (this._baseInfos._simHubClassColors.AssignedColors.TryGetValue(@new, out var shColors)) {
                info._SimHubColor = shColors;
            }

            this._baseInfos._infos[@new] = info;

            this.AddDoesntExist(@new, info);
        }

        internal bool ContainsClass(CarClass cls) {
            return this._classManagers.ContainsKey(cls);
        }

        public IEnumerator<KeyValuePair<CarClass, OverridableClassInfo.Manager>> GetEnumerator() {
            return this._classManagers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this._classManagers.GetEnumerator();
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class OverridableClassInfo {
    public string? Foreground => this._IsColorEnabled ? this._ForegroundDontCheckEnabled : this._SimHubColor?.Fg;
    public string? Background => this._IsColorEnabled ? this._BackgroundDontCheckEnabled : this._SimHubColor?.Bg;
    public CarClass? ReplaceWith => this._IsReplaceWithEnabled ? this._ReplaceWithDontCheckEnabled : null;
    public string? ShortName => this._Overrides?._ShortName ?? this._Base?._ShortName;

    internal ClassInfo? _Base { get; private set; }

    // If a class had been duplicated from it can have a "false" base from its parent. 
    // This flag helps to differentiate those cases. 
    // Note that just checking if DuplicatedFrom == null is not enough.
    // A class that was duplicated could end up receiving a base in later updates.
    internal bool _HasRealBase { get; private set; } = false;

    [JsonProperty("Overrides")]
    internal ClassInfo? _Overrides { get; private set; }

    [JsonProperty("IsColorEnabled", Required = Required.Always)]
    internal bool _IsColorEnabled { get; private set; }

    [JsonProperty("IsReplaceWithEnabled", Required = Required.Always)]
    internal bool _IsReplaceWithEnabled { get; private set; }

    // Used to determine what base should this class receive if it was duplicated from another class
    [JsonProperty("DuplicatedFrom")]
    internal ImmutableList<CarClass> _DuplicatedFrom { get; private set; }

    internal TextBoxColor? _SimHubColor { get; set; } = null;


    internal string? _ForegroundDontCheckEnabled =>
        this._Overrides?._Color?.Fg ?? this._Base?._Color?.Fg ?? this._SimHubColor?.Fg;
    internal string? _BaseForeground => this._Base?._Color?.Fg;
    internal string? _BackgroundDontCheckEnabled =>
        this._Overrides?._Color?.Bg ?? this._Base?._Color?.Bg ?? this._SimHubColor?.Bg;
    internal string? _BaseBackground => this._Base?._Color?.Bg;
    internal CarClass? _BaseReplaceWith => this._Base?._ReplaceWith;
    internal CarClass? _ReplaceWithDontCheckEnabled => this._Overrides?._ReplaceWith ?? this._Base?._ReplaceWith;
    internal string? _BaseShortName => this._Base?._ShortName;


    [JsonConstructor]
    internal OverridableClassInfo(
        ClassInfo? @base,
        ClassInfo? overrides,
        bool? isColorEnabled = null,
        bool? isReplaceWithEnabled = null,
        ImmutableList<CarClass>? duplicatedFrom = null,
        TextBoxColor? simHubColor = null
    ) {
        this.SetBase(@base);
        this.SetOverrides(overrides);
        if (isColorEnabled != null) {
            this._IsColorEnabled = isColorEnabled.Value;
        }

        if (isReplaceWithEnabled != null) {
            this._IsReplaceWithEnabled = isReplaceWithEnabled.Value;
        }

        this._DuplicatedFrom = duplicatedFrom ?? ImmutableList<CarClass>.Empty;
        this._SimHubColor = simHubColor;
    }

    internal OverridableClassInfo Duplicate(CarClass thisClass) {
        return new OverridableClassInfo(
            this._Base?.Clone(),
            this._Overrides?.Clone(),
            isColorEnabled: this._IsColorEnabled,
            isReplaceWithEnabled: this._IsReplaceWithEnabled,
            duplicatedFrom: this._DuplicatedFrom.Add(thisClass),
            // don't duplicate SimHub color because it's associated with class name
            simHubColor: null
        );
    }

    internal void SetOverrides(ClassInfo? overrides) {
        this._Overrides = overrides;
    }

    internal void SetBase(ClassInfo? @base) {
        this._Base = @base;
    }

    internal void CheckEnabled() {
        // check enabled properties. New base may have missing properties.
        // Make sure not to enable these as they may have been explicitly disabled before.
        // It's ok to disable.
        if (this._ReplaceWithDontCheckEnabled == null)
            // replace with cannot be enabled if there is no replace with set
        {
            this._IsReplaceWithEnabled = false;
        }

        if (this._ForegroundDontCheckEnabled == null || this._BackgroundDontCheckEnabled == null) {
            this._IsColorEnabled = false;
        }
    }

    internal void SetRealBase(ClassInfo? @base) {
        this.SetBase(@base);
        this._HasRealBase = true;
    }

    /// <summary>
    ///     This is the glue between OverridableClassInfo and settings UI
    /// </summary>
    internal sealed class Manager : INotifyPropertyChanged {
        /// <summary>
        ///     This is the glue between OverridableClassInfo and settings UI
        /// </summary>
        public Manager(CarClass key, OverridableClassInfo info) {
            this._Info = info;
            this._Key = key;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
            if (this.PropertyChanged == null) {
                return;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal OverridableClassInfo _Info { get; }
        internal CarClass _Key { get; }

        internal bool HasBase() {
            return this._Info._Base != null && this._Info._HasRealBase;
        }

        internal string? _Background {
            get => this._Info.Background;
            set {
                this._Info._Overrides ??= new ClassInfo();
                if (value == null) {
                    this._Info._Overrides._Color = null;
                } else {
                    this._Info._Overrides._Color ??= TextBoxColor.FromBg(value);
                    this._Info._Overrides._Color.Bg = value;
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(Manager._Foreground));
            }
        }

        internal string? _Foreground {
            get => this._Info.Foreground;
            set {
                this._Info._Overrides ??= new ClassInfo();
                if (value == null) {
                    this._Info._Overrides._Color = null;
                } else {
                    this._Info._Overrides._Color ??= TextBoxColor.FromFg(value);
                    this._Info._Overrides._Color.Fg = value;
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(Manager._Background));
            }
        }

        internal bool _IsColorEnabled {
            get => this._Info._IsColorEnabled;
            set {
                this._Info._IsColorEnabled = value;
                if (value) {
                    if (this._Info._Overrides?._Color == null) {
                        this._Info._Overrides ??= new ClassInfo();
                        this._Info._Overrides._Color = this._Info._Base?._Color?.Clone()
                            ?? this._Info._SimHubColor?.Clone()
                            ?? TextBoxColor.Default();
                    }
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(Manager._Background));
                this.InvokePropertyChanged(nameof(Manager._Foreground));
            }
        }

        internal void ResetColors() {
            // default is enabled, is we have base
            // don't call the setter for this.IsColorEnabled, that would notify bg and fg change unnecessarily
            this._Info._IsColorEnabled = this._Info._Base?._Color != null;

            // don't call this.ResetBackground() or this.ResetForeground(),
            // we don't want to write base colors to overrides
            if (this._Info._Overrides != null) {
                this._Info._Overrides._Color = null;
            }

            this.InvokePropertyChanged(nameof(Manager._IsColorEnabled));
            this.InvokePropertyChanged(nameof(Manager._Foreground));
            this.InvokePropertyChanged(nameof(Manager._Background));
        }

        internal string _ShortName {
            get => this._Info.ShortName ?? this._Key.AsString();
            set {
                this._Info._Overrides ??= new ClassInfo();
                this._Info._Overrides._ShortName = value;
                this.InvokePropertyChanged();
            }
        }

        internal void ResetShortName() {
            if (this._Info._Overrides != null) {
                this._Info._Overrides._ShortName = null;
            }

            this.InvokePropertyChanged(nameof(Manager._ShortName));
        }

        internal CarClass? _ReplaceWith {
            get => this._Info.ReplaceWith;
            set {
                this._Info._Overrides ??= new ClassInfo();
                this._Info._Overrides._ReplaceWith = value;
                this.InvokePropertyChanged();
            }
        }

        internal bool _IsReplaceWithEnabled {
            get => this._Info._IsReplaceWithEnabled;
            set {
                this._Info._IsReplaceWithEnabled = value;
                this.InvokePropertyChanged();

                if (value) {
                    if (this._Info._Overrides?._ReplaceWith == null) {
                        this._Info._Overrides ??= new ClassInfo();
                        this._Info._Overrides._ReplaceWith =
                            this._Info._ReplaceWithDontCheckEnabled ?? CarClass.Default;
                    }
                }

                this.InvokePropertyChanged(nameof(Manager._ReplaceWith));
            }
        }

        internal void ResetReplaceWith() {
            if (this._Info._Overrides != null) {
                this._Info._Overrides._ReplaceWith = null;
            }

            // default is enabled if base is present
            this._IsReplaceWithEnabled = this._Info._Base?._ReplaceWith != null;
            // No need to notify, this is done in the setter above
        }

        internal void Reset() {
            this.ResetColors();
            this.ResetShortName();
            this.ResetReplaceWith();
        }

        internal void DisableAll() {
            this._IsColorEnabled = false;
            this._IsReplaceWithEnabled = false;
        }

        internal void EnableAll() {
            this._IsColorEnabled = true;
            this._IsReplaceWithEnabled = true;
        }
    }
}

internal sealed class ClassInfo {
    [JsonProperty("Color")]
    internal TextBoxColor? _Color { get; set; }

    [JsonProperty("ReplaceWith")]
    internal CarClass? _ReplaceWith { get; set; }

    [JsonProperty("ShortName")]
    internal string? _ShortName { get; set; }

    [JsonConstructor]
    internal ClassInfo(TextBoxColor? color, CarClass? replaceWith, string? shortName) {
        this._Color = color;
        this._ReplaceWith = replaceWith;
        this._ShortName = shortName;
    }

    internal ClassInfo() { }

    internal ClassInfo Clone() {
        return new ClassInfo(this._Color?.Clone(), this._ReplaceWith, this._ShortName);
    }
}

[JsonConverter(typeof(FailJsonConverter))]
internal sealed class SimHubClassColors {
    public readonly Dictionary<CarClass, TextBoxColor> AssignedColors = [];

    public static SimHubClassColors FromJson(string json) {
        var self = new SimHubClassColors();

        var raw = JsonConvert.DeserializeObject<Raw>(json);
        if (raw != null) {
            foreach (var color in raw.AssignedColors) {
                var cls = new CarClass(color.Target);
                var fg = ColorTools.ComplementaryBlackOrWhite(color.Color);
                var col = new TextBoxColor(fg, color.Color);
                self.AssignedColors.Add(cls, col);
            }
        }

        return self;
    }

    [method: JsonConstructor]
    private class Raw(List<RawColor> assignedColors) {
        [JsonProperty("AssignedColors")]
        public List<RawColor> AssignedColors = assignedColors;
    }

    [method: JsonConstructor]
    private class RawColor(string target, string color) {
        [JsonProperty("Target")]
        public string Target { get; } = target;

        [JsonProperty("Color")]
        public string Color { get; } = color;
    }

    private class FailJsonConverter : Common.FailJsonConverter {
        public FailJsonConverter() {
            this.SerializeMsg = $"`{nameof(SimHubClassColors)}` cannot be serialized";
            this.DeserializeMsg =
                $"`{nameof(SimHubClassColors)}` cannot be deserialized, use `{nameof(SimHubClassColors.FromJson)}` method instead";
        }
    }
}