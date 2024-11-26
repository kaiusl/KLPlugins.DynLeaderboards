using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using KLPlugins.DynLeaderboards.Car;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards;

internal class ClassInfos : IEnumerable<KeyValuePair<CarClass, OverridableClassInfo>> {
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
                c.SimHubColor = shColor;
            }

            this._infos[cls] = c;
        }

        return this._infos[cls];
    }

    internal (CarClass, OverridableClassInfo) GetFollowReplaceWith(CarClass cls) {
        var clsOut = cls;
        var info = this.GetOrAdd(cls);
        var nextCls = info.ReplaceWith();

        var seenClasses = new List<CarClass> { cls };

        while (nextCls != null && nextCls != clsOut) {
            clsOut = nextCls.Value;
            info = this.GetOrAdd(clsOut);

            if (seenClasses.Contains(clsOut)) {
                DynLeaderboardsPlugin.LogWarn(
                    $"Loop detected in class \"replace with\" values: {string.Join(" -> ", seenClasses)} -> {clsOut}"
                );
                break;
            }

            seenClasses.Add(clsOut);

            nextCls = info.ReplaceWith();
        }

        return (clsOut, info);
    }

    internal ClassInfo? GetBaseFollowDuplicates(CarClass cls) {
        var info = this.GetOrAdd(cls);

        if (info.Base != null) {
            return info.Base;
        }

        foreach (var dup in info.DuplicatedFrom.Reverse()) {
            // Don't use this.Get as that will add missing classes
            // We don't want it here, just skip the duplicates that don't exist anymore
            if (this._infos.TryGetValue(dup, out info)) {
                if (info.Base != null) {
                    return info.Base;
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
                    var isColorEnabled = kv.Value.Color?.Fg != null && kv.Value.Color?.Bg != null;
                    var isReplaceWithEnabled = kv.Value.ReplaceWith != null;
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

        var simHubClassColorsPath = $"PluginsData\\{DynLeaderboardsPlugin.Game.Name}\\ColorPalette.json";
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
            if (it.ReplaceWithDontCheckEnabled() != null) {
                var _ = c.GetOrAdd(it.ReplaceWithDontCheckEnabled()!.Value);
            }

            if (it.BaseReplaceWith() != null) {
                var _ = c.GetOrAdd(it.BaseReplaceWith()!.Value);
            }

            if (it.Base == null) {
                it.SetBase(c.GetBaseFollowDuplicates(key));
            }
        }

        foreach (var kv in c._infos) {
            if (c._simHubClassColors.AssignedColors.TryGetValue(kv.Key, out var shColor)) {
                kv.Value.SimHubColor = shColor;
            }

            kv.Value.CheckEnabled();
        }

        return c;
    }

    internal void WriteToJson(string path, string derivedPath) {
        File.WriteAllText(path, JsonConvert.SerializeObject(this._infos, Formatting.Indented));
    }

    public IEnumerator<KeyValuePair<CarClass, OverridableClassInfo>> GetEnumerator() {
        return this._infos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this._infos.GetEnumerator();
    }


    /// <summary>
    ///     This is the glue between ClassInfos and settings UI
    /// </summary>
    internal class Manager
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

        internal OverridableClassInfo.Manager GetFollowReplaceWith(CarClass cls) {
            var clsOut = cls;
            var manager = this.GetOrAdd(cls);
            var nextCls = manager.Info.ReplaceWith(); // don't use manager.ReplaceWith, it defaults to default class

            var seenClasses = new List<CarClass> { cls };

            while (nextCls != null && nextCls != clsOut) {
                clsOut = nextCls.Value;
                manager = this.GetOrAdd(clsOut);

                if (seenClasses.Contains(clsOut)) {
                    DynLeaderboardsPlugin.LogWarn(
                        $"Loop detected in class \"replace with\" values: {string.Join(" -> ", seenClasses)} -> {clsOut}"
                    );
                    break;
                }

                seenClasses.Add(clsOut);

                nextCls = manager.Info.ReplaceWith();
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
                manager.IsColorEnabled = false;
                manager.IsReplaceWithEnabled = false;
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
            if (c.HasBase() || c.Key == CarClass.Default || this.IsUsedInAnyReplaceWith(c.Key)) {
                return false;
            }

            return true;
        }

        internal bool IsUsedInAnyReplaceWith(CarClass cls) {
            return this._classManagers.Values.Any(it => it.ReplaceWith == cls);
        }

        internal void Duplicate(CarClass old, CarClass @new) {
            if (!this._classManagers.ContainsKey(old) || this._classManagers.ContainsKey(@new)) {
                return;
            }

            var info = this._classManagers[old].Info.Duplicate(old);
            if (this._baseInfos._simHubClassColors.AssignedColors.TryGetValue(@new, out var shColors)) {
                info.SimHubColor = shColors;
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

internal class OverridableClassInfo {
    [JsonIgnore] internal ClassInfo? Base { get; private set; }

    // If a class had been duplicated from it can have a "false" base from its parent. 
    // This flag helps to differentiate those cases. 
    // Note that just checking if DuplicatedFrom == null is not enough.
    // A class that was duplicated could end up receiving a base in later updates.
    [JsonIgnore] internal bool HasRealBase { get; private set; } = false;
    [JsonProperty] internal ClassInfo? Overrides { get; private set; }
    [JsonProperty] internal bool IsColorEnabled { get; private set; }

    [JsonProperty] internal bool IsReplaceWithEnabled { get; private set; }

    // Used to determine what base should this class receive if it was duplicated from another class
    [JsonProperty] internal ImmutableList<CarClass> DuplicatedFrom { get; private set; }
    [JsonIgnore] internal TextBoxColor? SimHubColor { get; set; } = null;


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
            this.IsColorEnabled = isColorEnabled.Value;
        }

        if (isReplaceWithEnabled != null) {
            this.IsReplaceWithEnabled = isReplaceWithEnabled.Value;
        }

        this.DuplicatedFrom = duplicatedFrom ?? ImmutableList<CarClass>.Empty;
        this.SimHubColor = simHubColor;
    }

    internal OverridableClassInfo Duplicate(CarClass thisClass) {
        return new OverridableClassInfo(
            this.Base?.Clone(),
            this.Overrides?.Clone(),
            isColorEnabled: this.IsColorEnabled,
            isReplaceWithEnabled: this.IsReplaceWithEnabled,
            duplicatedFrom: this.DuplicatedFrom.Add(thisClass),
            // don't duplicate SimHub color because it's associated with class name
            simHubColor: null
        );
    }

    internal void SetOverrides(ClassInfo? overrides) {
        this.Overrides = overrides;
    }

    internal void SetBase(ClassInfo? @base) {
        this.Base = @base;
    }

    internal void CheckEnabled() {
        // check enabled properties. New base may have missing properties.
        // Make sure not to enable these as they may have been explicitly disabled before.
        // It's ok to disable.
        if (this.ReplaceWithDontCheckEnabled() == null) {
            // replace with cannot be enabled if there is no replace with set
            this.IsReplaceWithEnabled = false;
        }

        if (this.ForegroundDontCheckEnabled() == null || this.BackgroundDontCheckEnabled() == null) {
            this.IsColorEnabled = false;
        }
    }

    internal void SetRealBase(ClassInfo? @base) {
        this.SetBase(@base);
        this.HasRealBase = true;
    }

    internal string? Foreground() {
        if (!this.IsColorEnabled) {
            return this.SimHubColor?.Fg;
        }

        return this.ForegroundDontCheckEnabled();
    }

    internal string? ForegroundDontCheckEnabled() {
        return this.Overrides?.Color?.Fg ?? this.Base?.Color?.Fg ?? this.SimHubColor?.Fg;
    }

    internal string? BaseForeground() {
        return this.Base?.Color?.Fg;
    }

    internal string? Background() {
        if (!this.IsColorEnabled) {
            return this.SimHubColor?.Bg;
        }

        return this.BackgroundDontCheckEnabled();
    }

    internal string? BackgroundDontCheckEnabled() {
        return this.Overrides?.Color?.Bg ?? this.Base?.Color?.Bg ?? this.SimHubColor?.Bg;
    }

    internal string? BaseBackground() {
        return this.Base?.Color?.Bg;
    }

    internal CarClass? BaseReplaceWith() {
        return this.Base?.ReplaceWith;
    }

    internal CarClass? ReplaceWith() {
        if (!this.IsReplaceWithEnabled) {
            return null;
        }

        return this.ReplaceWithDontCheckEnabled();
    }

    internal CarClass? ReplaceWithDontCheckEnabled() {
        return this.Overrides?.ReplaceWith ?? this.Base?.ReplaceWith;
    }

    internal string? BaseShortName() {
        return this.Base?.ShortName;
    }

    internal string? ShortName() {
        return this.Overrides?.ShortName ?? this.Base?.ShortName;
    }

    /// <summary>
    ///     This is the glue between OverridableClassInfo and settings UI
    /// </summary>
    internal class Manager : INotifyPropertyChanged {
        /// <summary>
        ///     This is the glue between OverridableClassInfo and settings UI
        /// </summary>
        public Manager(CarClass key, OverridableClassInfo info) {
            this.Info = info;
            this.Key = key;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
            if (this.PropertyChanged == null) {
                return;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal OverridableClassInfo Info { get; }
        internal CarClass Key { get; }

        internal bool HasBase() {
            return this.Info.Base != null && this.Info.HasRealBase;
        }

        internal string? Background {
            get => this.Info.Background();
            set {
                this.Info.Overrides ??= new ClassInfo();
                if (value == null) {
                    this.Info.Overrides.Color = null;
                } else {
                    this.Info.Overrides.Color ??= TextBoxColor.FromBg(value);
                    this.Info.Overrides.Color.Bg = value;
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(this.Foreground));
            }
        }

        internal string? Foreground {
            get => this.Info.Foreground();
            set {
                this.Info.Overrides ??= new ClassInfo();
                if (value == null) {
                    this.Info.Overrides.Color = null;
                } else {
                    this.Info.Overrides.Color ??= TextBoxColor.FromFg(value);
                    this.Info.Overrides.Color.Fg = value;
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(this.Background));
            }
        }

        internal bool IsColorEnabled {
            get => this.Info.IsColorEnabled;
            set {
                this.Info.IsColorEnabled = value;
                if (value) {
                    if (this.Info.Overrides?.Color == null) {
                        this.Info.Overrides ??= new ClassInfo();
                        this.Info.Overrides.Color = this.Info.Base?.Color?.Clone()
                            ?? this.Info.SimHubColor?.Clone()
                            ?? TextBoxColor.Default();
                    }
                }

                this.InvokePropertyChanged();
                this.InvokePropertyChanged(nameof(this.Background));
                this.InvokePropertyChanged(nameof(this.Foreground));
            }
        }

        internal void ResetColors() {
            // default is enabled, is we have base
            // don't call the setter for this.IsColorEnabled, that would notify bg and fg change unnecessarily
            this.Info.IsColorEnabled = this.Info.Base?.Color != null;

            // don't call this.ResetBackground() or this.ResetForeground(),
            // we don't want to write base colors to overrides
            if (this.Info.Overrides != null) {
                this.Info.Overrides.Color = null;
            }

            this.InvokePropertyChanged(nameof(this.IsColorEnabled));
            this.InvokePropertyChanged(nameof(this.Foreground));
            this.InvokePropertyChanged(nameof(this.Background));
        }

        internal string ShortName {
            get => this.Info.ShortName() ?? this.Key.AsString();
            set {
                this.Info.Overrides ??= new ClassInfo();
                this.Info.Overrides.ShortName = value;
                this.InvokePropertyChanged();
            }
        }

        internal void ResetShortName() {
            if (this.Info.Overrides != null) {
                this.Info.Overrides.ShortName = null;
            }

            this.InvokePropertyChanged(nameof(this.ShortName));
        }

        internal CarClass? ReplaceWith {
            get => this.Info.ReplaceWith();
            set {
                this.Info.Overrides ??= new ClassInfo();
                this.Info.Overrides.ReplaceWith = value;
                this.InvokePropertyChanged();
            }
        }

        internal bool IsReplaceWithEnabled {
            get => this.Info.IsReplaceWithEnabled;
            set {
                this.Info.IsReplaceWithEnabled = value;
                this.InvokePropertyChanged();
                var notified = false;

                if (value) {
                    if (this.Info.ReplaceWithDontCheckEnabled() == null) {
                        this.ReplaceWith = CarClass.Default;
                        notified = true;
                    }
                }

                if (!notified) {
                    this.InvokePropertyChanged(nameof(this.ReplaceWith));
                }
            }
        }

        internal void ResetReplaceWith() {
            if (this.Info.Overrides != null) {
                this.Info.Overrides.ReplaceWith = null;
            }

            // default is enabled if base is present
            this.IsReplaceWithEnabled = this.Info.Base?.ReplaceWith != null;
            // No need to notify, this is done in the setter above
        }

        internal void Reset() {
            this.ResetColors();
            this.ResetShortName();
            this.ResetReplaceWith();
        }

        internal void DisableAll() {
            this.IsColorEnabled = false;
            this.IsReplaceWithEnabled = false;
        }

        internal void EnableAll() {
            this.IsColorEnabled = true;
            this.IsReplaceWithEnabled = true;
        }
    }
}

internal class ClassInfo {
    [JsonProperty] internal TextBoxColor? Color { get; set; }
    [JsonProperty] internal CarClass? ReplaceWith { get; set; }
    [JsonProperty] internal string? ShortName { get; set; }

    [JsonConstructor]
    internal ClassInfo(TextBoxColor? color, CarClass? replaceWith, string? shortName) {
        this.Color = color;
        this.ReplaceWith = replaceWith;
        this.ShortName = shortName;
    }

    internal ClassInfo() { }

    internal ClassInfo Clone() {
        return new ClassInfo(this.Color?.Clone(), this.ReplaceWith, this.ShortName);
    }
}