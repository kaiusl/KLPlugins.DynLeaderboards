using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Track;

using Newtonsoft.Json;

using SimHub.Plugins;

using SimHubAMS2 = PCarsSharedMemory.AMS2.Models;

namespace KLPlugins.DynLeaderboards;

internal class TextBoxColors<K> : IEnumerable<KeyValuePair<K, OverridableTextBoxColor>> {
    private readonly SortedDictionary<K, OverridableTextBoxColor> _colors;

    internal TextBoxColors(SortedDictionary<K, OverridableTextBoxColor> colors) {
        this._colors = colors;
    }

    internal OverridableTextBoxColor Get(K key) {
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

internal class OverridableTextBoxColor {
    [JsonIgnore] internal const string DEF_FG = "#FFFFFF";
    [JsonIgnore] internal const string DEF_BG = "#000000";

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
        // note: this must be done before ResetBackground and ResetForeground
        // because otherwise they could wrongly set colors to default (not base) values
        this.IsEnabled = this._base?.Fg != null && this._base?.Bg != null;

        this.ResetBackground();
        this.ResetForeground();
    }

    internal void Enable() {
        this.IsEnabled = true;

        if (this.BackgroundDontCheckEnabled() == null) {
            // we are explicitly asked to enable colors
            // there must be some value in it
            this.SetBackground(OverridableTextBoxColor.DEF_BG);
        }

        if (this.ForegroundDontCheckEnabled() == null) {
            // same as above
            this.SetForeground(OverridableTextBoxColor.DEF_FG);
        }
    }

    internal void Disable() {
        this.IsEnabled = false;
    }

    internal string? Foreground() {
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
        this._overrides ??= new TextBoxColor();
        this._overrides.Fg = fg;
    }

    internal void ResetForeground() {
        if (this._overrides != null) {
            this._overrides.Fg = null;
        }

        if (this.IsEnabled && this.BaseForeground() == null) {
            // if color is enabled there must be some value in it
            this.SetForeground(OverridableTextBoxColor.DEF_FG);
        }
    }

    internal string? Background() {
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
        this._overrides ??= new TextBoxColor();
        this._overrides.Bg = bg;
    }

    internal void ResetBackground() {
        if (this._overrides != null) {
            this._overrides.Bg = null;
        }

        // if (this.IsEnabled && this.BaseBackground() == null) {
        //     // if color is enabled there must be some value in it
        //     this.SetBackground(OverridableTextBoxColor.DEF_BG);
        // }
    }
}

public class TextBoxColor {
    [JsonProperty] public string? Fg { get; internal set; }
    [JsonProperty] public string? Bg { get; internal set; }

    [JsonConstructor]
    internal TextBoxColor(string? fg, string? bg) {
        this.Fg = fg;
        this.Bg = bg;
    }

    internal TextBoxColor() { }

    internal TextBoxColor Clone() {
        return new TextBoxColor(this.Fg, this.Bg);
    }

    internal static TextBoxColor Default() {
        return new TextBoxColor(OverridableTextBoxColor.DEF_FG, OverridableTextBoxColor.DEF_BG);
    }
}

internal class CarInfos : IEnumerable<KeyValuePair<string, OverridableCarInfo>> {
    private readonly Dictionary<string, OverridableCarInfo> _infos;

    internal CarInfos(Dictionary<string, OverridableCarInfo> infos) {
        this._infos = infos;
    }

    internal OverridableCarInfo Get(string key, CarClass carClass) {
        if (!this._infos.ContainsKey(key)) {
            var c = new OverridableCarInfo();
            c.DisableClass();
            c.DisableName();
            c.SetClass(carClass);
            c.SetName(key);
            c.SetManufacturer(key.Split(' ')[0]);

            this._infos[key] = c;
        }

        return this._infos[key];
    }

    internal void TryRemove(string key) {
        if (!this._infos.TryGetValue(key, out var c)) {
            return;
        }

        if (c.Base != null) {
            c.Reset(key);
            c.DisableName();
            c.DisableClass();
        } else {
            this._infos.Remove(key);
        }
    }

    internal bool CanBeRemoved(string key) {
        if (!this._infos.TryGetValue(key, out var c)) {
            return false;
        }

        return c.Base == null;
    }

    internal void RenameClass(CarClass oldCls, CarClass newCls) {
        foreach (var kv in this._infos) {
            if (kv.Value.ClassDontCheckEnabled() == oldCls) {
                // this happens in two cases:
                // 1. override was set to old class, just set it to new class
                // 2. base was set to old class. 
                //    But we cannot and don't want to change base values as they are not saved. 
                //    So we set the override class to the new one.
                //    This also allows "Reset" button to reset to plugin defaults as promised.
                kv.Value.SetClass(newCls);
            }
        }
    }

    internal bool ContainsCar(string key) {
        return this._infos.ContainsKey(key);
    }

    internal bool ContainsClassIncludeDisabled(CarClass cls) {
        return this._infos.Any(kv => kv.Value.ClassDontCheckEnabled() == cls);
    }

    internal bool ContainsClass(CarClass cls) {
        return this._infos.Any(kv => kv.Value.Class() == cls);
    }

    public IEnumerator<KeyValuePair<string, OverridableCarInfo>> GetEnumerator() {
        return this._infos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this._infos.GetEnumerator();
    }

    internal static CarInfos ReadFromJson(string path, string basePath) {
        Dictionary<string, OverridableCarInfo>? infos = null;
        if (File.Exists(path)) {
            var json = File.ReadAllText(path);
            infos = JsonConvert.DeserializeObject<Dictionary<string, OverridableCarInfo>>(json);
        }

        infos ??= [];

        if (File.Exists(basePath)) {
            var json = File.ReadAllText(basePath);
            var bases = JsonConvert.DeserializeObject<Dictionary<string, CarInfo>>(json) ?? [];
            foreach (var kv in bases) {
                if (infos.ContainsKey(kv.Key)) {
                    infos[kv.Key].SetBase(kv.Value);
                } else {
                    var info = new OverridableCarInfo(kv.Value);
                    infos[kv.Key] = info;
                }
            }
        }

        return new CarInfos(infos);
    }

    internal void WriteToJson(string path, string derivedPath) {
        File.WriteAllText(path, JsonConvert.SerializeObject(this._infos, Formatting.Indented));
    }
}

internal class OverridableCarInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonIgnore] internal CarInfo? Base { get; private set; }
    [JsonProperty("Overrides")] internal CarInfo? Overrides { get; private set; }
    [JsonProperty] internal bool IsNameEnabled { get; private set; } = true;
    [JsonProperty] internal bool IsClassEnabled { get; private set; } = true;

    [JsonConstructor]
    internal OverridableCarInfo(
        CarInfo? @base,
        CarInfo? overrides = null,
        bool? isNameEnabled = null,
        bool? isClassEnabled = null
    ) {
        this.SetBase(@base);
        this.SetOverrides(overrides);

        if (isNameEnabled != null) {
            this.IsNameEnabled = isNameEnabled.Value;
        }

        if (isClassEnabled != null) {
            this.IsClassEnabled = isClassEnabled.Value;
        }
    }

    internal OverridableCarInfo(string? name, string? manufacturer, CarClass? cls) : this(
        null,
        new CarInfo(name, manufacturer, cls)
    ) { }

    internal OverridableCarInfo() : this(null) { }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal string Debug() {
        return
            $"CarInfo: base: {JsonConvert.SerializeObject(this.Base)}, overrides: {JsonConvert.SerializeObject(this.Overrides)}";
    }

    internal void SetOverrides(CarInfo? overrides) {
        this.Overrides = overrides;
    }

    internal void SetBase(CarInfo? @base) {
        this.Base = @base;

        if (this.NameDontCheckEnabled() == null) {
            this.DisableName();
        }

        if (this.ClassDontCheckEnabled() == null) {
            this.DisableClass();
        }
    }

    internal void SetBase(string key, CarInfo? @base) {
        this.SetBase(@base);

        if (this.Manufacturer() == null) {
            this.SetManufacturer(key.Split(' ')[0]);
        }
    }

    internal void Reset() {
        this.ResetName();
        this.ResetClass();
        this.ResetManufacturer();
    }

    internal void Reset(string key) {
        this.ResetName();
        this.ResetClass();
        this.ResetManufacturer(key);
    }

    internal string? BaseName() {
        return this.Base?.Name;
    }

    internal string? Name() {
        if (!this.IsNameEnabled) {
            return null;
        }

        return this.Overrides?.Name ?? this.Base?.Name;
    }

    internal string? NameDontCheckEnabled() {
        return this.Overrides?.Name ?? this.Base?.Name;
    }

    internal void SetName(string name) {
        this.Overrides ??= new CarInfo();
        this.Overrides.Name = name;

        this.InvokePropertyChanged(nameof(this.Name));
    }

    internal void ResetName() {
        if (this.Overrides != null) {
            this.Overrides.Name = null;
        }

        // default is enabled if base is present
        this.IsNameEnabled = this.Base?.Name != null;

        this.InvokePropertyChanged(nameof(this.Name));
        this.InvokePropertyChanged(nameof(this.IsNameEnabled));
    }

    internal void DisableName() {
        this.IsNameEnabled = false;
        this.InvokePropertyChanged(nameof(this.Name));
        this.InvokePropertyChanged(nameof(this.IsNameEnabled));
    }

    internal void EnableName() {
        this.IsNameEnabled = true;
        this.InvokePropertyChanged(nameof(this.Name));
        this.InvokePropertyChanged(nameof(this.IsNameEnabled));
    }

    internal void EnableName(string key) {
        this.EnableName();

        if (this.NameDontCheckEnabled() == null) {
            // we are explicitly asked to enable colors
            // there must be some value in it
            this.SetName(key);
        }
    }

    internal string? BaseManufacturer() {
        return this.Base?.Manufacturer;
    }

    internal string? Manufacturer() {
        return this.Overrides?.Manufacturer ?? this.Base?.Manufacturer;
    }

    internal void SetManufacturer(string manufacturer) {
        this.Overrides ??= new CarInfo();
        this.Overrides.Manufacturer = manufacturer;

        this.InvokePropertyChanged(nameof(this.Manufacturer));
    }

    internal void ResetManufacturer() {
        if (this.Overrides != null) {
            this.Overrides.Manufacturer = null;
        }

        this.InvokePropertyChanged(nameof(this.Manufacturer));
    }

    internal void ResetManufacturer(string key) {
        this.ResetManufacturer();

        if (this.Manufacturer() == null) {
            // default is the first word of full name/key
            this.SetManufacturer(key.Split(' ')[0]);
        }
    }

    internal CarClass? BaseClass() {
        return this.Base?.Class;
    }

    internal CarClass? Class() {
        if (!this.IsClassEnabled) {
            return null;
        }

        return this.ClassDontCheckEnabled();
    }

    internal CarClass? ClassDontCheckEnabled() {
        return this.Overrides?.Class ?? this.Base?.Class;
    }

    internal void SetClass(CarClass cls) {
        this.Overrides ??= new CarInfo();
        this.Overrides.Class = cls;

        this.InvokePropertyChanged(nameof(this.Class));
        this.InvokePropertyChanged(nameof(this.IsClassEnabled));
    }

    internal void ResetClass() {
        if (this.Overrides != null) {
            this.Overrides.Class = null;
        }

        // default is enabled if base is present
        this.IsClassEnabled = this.Base?.Class != null;

        this.InvokePropertyChanged(nameof(this.Class));
        this.InvokePropertyChanged(nameof(this.IsClassEnabled));
    }

    internal void DisableClass() {
        this.IsClassEnabled = false;

        this.InvokePropertyChanged(nameof(this.Class));
        this.InvokePropertyChanged(nameof(this.IsClassEnabled));
    }

    internal void EnableClass() {
        this.IsClassEnabled = true;

        if (this.ClassDontCheckEnabled() == null) {
            // we are explicitly asked to enable colors
            // there must be some value in it
            this.SetClass(CarClass.Default);
        } else {
            this.InvokePropertyChanged(nameof(this.Class));
            this.InvokePropertyChanged(nameof(this.IsClassEnabled));
        }
    }
}

internal class CarInfo {
    [JsonProperty] internal string? Name { get; set; }
    [JsonProperty] internal string? Manufacturer { get; set; }
    [JsonProperty] internal CarClass? Class { get; set; }

    [JsonConstructor]
    internal CarInfo(string? name, string? manufacturer, CarClass? cls) {
        this.Name = name;
        this.Manufacturer = manufacturer;
        this.Class = cls;
    }

    internal CarInfo() { }
}

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
                    var info = new OverridableClassInfo(@base: kv.Value, null);
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

        internal void InvokePropertyChanged(
            [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null
        ) {
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
                this.Info.Overrides.Color ??= new TextBoxColor();
                this.Info.Overrides.Color.Bg = value;
                this.InvokePropertyChanged();
            }
        }

        internal string? Foreground {
            get => this.Info.Foreground();
            set {
                this.Info.Overrides ??= new ClassInfo();
                this.Info.Overrides.Color ??= new TextBoxColor();
                this.Info.Overrides.Color.Fg = value;
                this.InvokePropertyChanged();
            }
        }

        internal bool IsColorEnabled {
            get => this.Info.IsColorEnabled;
            set {
                this.Info.IsColorEnabled = value;
                this.InvokePropertyChanged();
                var notifiedBg = false;
                var notifiedFg = false;
                if (value) {
                    if (this.Info.Overrides?.Color?.Bg == null) {
                        // we are explicitly asked to enable colors
                        // there must be some value in it
                        // the value must be whatever was shown at the moment of enabling
                        this.Background = this.Info.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG;
                        notifiedBg = true;
                    }

                    if (this.Info.Overrides?.Color?.Fg == null) {
                        // same as above
                        this.Foreground = this.Info.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG;
                        notifiedFg = true;
                    }
                }

                if (!notifiedBg) {
                    this.InvokePropertyChanged(nameof(this.Background));
                }

                if (!notifiedFg) {
                    this.InvokePropertyChanged(nameof(this.Foreground));
                }
            }
        }

        internal void ResetBackground() {
            if (this.Info.Overrides?.Color != null) {
                this.Info.Overrides.Color.Bg = null;
            }

            if (this.IsColorEnabled && this.Info.Overrides?.Color?.Bg == null) {
                // if color is enabled there must be some value in it
                // the value must be whatever was shown at the moment of enabling
                this.Background = this.Info.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG;
            } else {
                this.InvokePropertyChanged(nameof(this.Background));
            }
        }

        internal void ResetForeground() {
            if (this.Info.Overrides?.Color != null) {
                this.Info.Overrides.Color.Fg = null;
            }

            if (this.IsColorEnabled && this.Info.Overrides?.Color?.Fg == null) {
                // if color is enabled there must be some value in it
                // the value must be whatever was shown at the moment of enabling
                this.Foreground = this.Info.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG;
            } else {
                this.InvokePropertyChanged(nameof(this.Foreground));
            }
        }

        internal void ResetColors() {
            // don't call the setter for this.IsColorEnabled, that would notify bg and fg change unnecessarily
            this.Info.IsColorEnabled = this.Info.Base?.Color?.Fg != null && this.Info.Base?.Color?.Bg != null;
            this.InvokePropertyChanged(nameof(this.IsColorEnabled));

            this.ResetBackground();
            this.ResetForeground();
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

/// <summary>
///     Storage and calculation of new properties
/// </summary>
public class Values : IDisposable {
    public TrackData? TrackData { get; private set; }
    public Session Session { get; } = new();
    public Booleans Booleans { get; } = new();

    public ReadOnlyCollection<CarData> OverallOrder { get; }
    public ReadOnlyCollection<CarData> ClassOrder { get; }
    public ReadOnlyCollection<CarData> CupOrder { get; }
    public ReadOnlyCollection<CarData> RelativeOnTrackAheadOrder { get; }
    public ReadOnlyCollection<CarData> RelativeOnTrackBehindOrder { get; }
    private List<CarData> _overallOrder { get; } = [];
    private List<CarData> _classOrder { get; } = [];
    private List<CarData> _cupOrder { get; } = [];
    private List<CarData> _relativeOnTrackAheadOrder { get; } = [];
    private List<CarData> _relativeOnTrackBehindOrder { get; } = [];
    public CarData? FocusedCar { get; private set; } = null;

    public int NumClassesInSession { get; private set; } = 0;
    public int NumCupsInSession { get; private set; } = 0;

    internal CarInfos CarInfos { get; private set; }

    private const string _CAR_INFOS_FILENAME = "CarInfos";

    private static string CarInfosPath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Values._CAR_INFOS_FILENAME}.json";
    }

    private static string CarInfosBasePath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Values._CAR_INFOS_FILENAME}.base.json";
    }

    private static CarInfos ReadCarInfos() {
        var basesPath = Values.CarInfosBasePath();
        var path = Values.CarInfosPath();
        return CarInfos.ReadFromJson(basePath: basesPath, path: path);
    }

    private void WriteCarInfos() {
        var path = Values.CarInfosPath();
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid car infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.CarInfos.WriteToJson(path: path, derivedPath: Values.CarInfosBasePath());
    }

    internal ClassInfos ClassInfos { get; }
    private const string _CLASS_INFOS_FILENAME = "ClassInfos";

    private static string ClassInfosPath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Values._CLASS_INFOS_FILENAME}.json";
    }

    private static string ClassInfosBasePath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Values._CLASS_INFOS_FILENAME}.base.json";
    }

    private static ClassInfos ReadClassInfos() {
        var basesPath = Values.ClassInfosBasePath();
        var path = Values.ClassInfosPath();
        return ClassInfos.ReadFromJson(basePath: basesPath, path: path);
    }

    private void WriteClassInfos() {
        var path = Values.ClassInfosPath();
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid class infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.ClassInfos.WriteToJson(path: path, derivedPath: Values.ClassInfosBasePath());
    }

    internal TextBoxColors<TeamCupCategory> TeamCupCategoryColors { get; }
    internal TextBoxColors<DriverCategory> DriverCategoryColors { get; }

    private static string TextBoxColorsPath(string fileName) {
        return $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.json";
    }

    private static TextBoxColors<K> ReadTextBoxColors<K>(string fileName) {
        var basesPath = $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.base.json";
        var path = Values.TextBoxColorsPath(fileName);
        return TextBoxColors<K>.ReadFromJson(basePath: basesPath, path: path);
    }

    private static void WriteTextBoxColors<K>(TextBoxColors<K> colors, string fileName) {
        var path = Values.TextBoxColorsPath(fileName);
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid text box colors path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        colors.WriteToJson(path);
    }

    internal bool IsFirstFinished { get; private set; } = false;
    private bool _startingPositionsSet = false;

    private const string _TEAM_CUP_CATEGORY_COLORS_JSON_NAME = "TeamCupCategoryColors";
    private const string _DRIVER_CATEGORY_COLORS_JSON_NAME = "DriverCategoryColors";

    internal Values() {
        this.CarInfos = Values.ReadCarInfos();
        this.ClassInfos = Values.ReadClassInfos();
        this.TeamCupCategoryColors =
            Values.ReadTextBoxColors<TeamCupCategory>(Values._TEAM_CUP_CATEGORY_COLORS_JSON_NAME);
        this.TeamCupCategoryColors.Get(TeamCupCategory.Default);
        this.DriverCategoryColors = Values.ReadTextBoxColors<DriverCategory>(Values._DRIVER_CATEGORY_COLORS_JSON_NAME);
        this.DriverCategoryColors.Get(DriverCategory.Default);

        this.OverallOrder = this._overallOrder.AsReadOnly();
        this.ClassOrder = this._classOrder.AsReadOnly();
        this.CupOrder = this._cupOrder.AsReadOnly();
        this.RelativeOnTrackAheadOrder = this._relativeOnTrackAheadOrder.AsReadOnly();
        this.RelativeOnTrackBehindOrder = this._relativeOnTrackBehindOrder.AsReadOnly();

        this.Reset();
    }

    internal void RereadCarInfos() {
        this.CarInfos = Values.ReadCarInfos();
    }

    internal void Reset() {
        DynLeaderboardsPlugin.LogInfo("Values.Reset()");
        this.Session.Reset();
        this.ResetWithoutSession();
    }

    internal void ResetWithoutSession() {
        DynLeaderboardsPlugin.LogInfo("Values.ResetWithoutSession()");
        this.Booleans.Reset();
        this._overallOrder.Clear();
        this._classOrder.Clear();
        this._cupOrder.Clear();
        this._relativeOnTrackAheadOrder.Clear();
        this._relativeOnTrackBehindOrder.Clear();
        this.FocusedCar = null;
        this.IsFirstFinished = false;
        this._startingPositionsSet = false;
        this.NumClassesInSession = 0;
        this.NumCupsInSession = 0;
    }

    #region IDisposable Support

    ~Values() {
        this.Dispose(false);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed = false;

    protected virtual void Dispose(bool disposing) {
        if (!this._isDisposed) {
            if (disposing) {
                this.WriteCarInfos();
                this.WriteClassInfos();
                Values.WriteTextBoxColors(this.TeamCupCategoryColors, Values._TEAM_CUP_CATEGORY_COLORS_JSON_NAME);
                Values.WriteTextBoxColors(this.DriverCategoryColors, Values._DRIVER_CATEGORY_COLORS_JSON_NAME);
                this.TrackData?.Dispose();
                DynLeaderboardsPlugin.LogInfo("Disposed");
            }

            this._isDisposed = true;
        }
    }

    public void Dispose() {
        this.Dispose(true);
    }

    #endregion IDisposable Support

    internal void UpdateClassInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateClassInfos(this);
        }
    }

    internal void UpdateCarInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateCarInfos(this);
        }
    }

    internal void UpdateTeamCupInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateTeamCupInfos(this);
        }
    }

    internal void UpdateDriverInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateDriverInfos(this);
        }
    }

    private int _skipCarUpdatesCount = 0;

    internal void OnDataUpdate(PluginManager _, GameData data) {
        this.Session.OnDataUpdate(data);

        if (this.Booleans.NewData.IsNewEvent
            || this.Session.IsNewSession
            || this.TrackData?.PrettyName != data.NewData.TrackName) {
            DynLeaderboardsPlugin.LogInfo(
                $"newEvent={this.Booleans.NewData.IsNewEvent}, newSession={this.Session.IsNewSession}"
            );
            this.ResetWithoutSession();
            this.Booleans.OnNewEvent(this.Session.SessionType);
            if (this.TrackData == null || this.TrackData.PrettyName != data.NewData.TrackName) {
                this.TrackData?.Dispose();
                this.TrackData = new TrackData(data);
            }
       
            foreach (var car in this.OverallOrder) {
                this.TrackData.BuildLapInterpolator(car.CarClass);
            }

            this._skipCarUpdatesCount = 0;

            DynLeaderboardsPlugin.LogInfo(
                $"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}, len={this.TrackData.LengthMeters}"
            );
        }

        if (this.TrackData == null) {
            this.TrackData = new TrackData(data);
            foreach (var car in this.OverallOrder) {
                this.TrackData.BuildLapInterpolator(car.CarClass);
            }
        }

        this.TrackData.OnDataUpdate();

        if (this.TrackData.LengthMeters == 0) {
            // In ACC sometimes the track length is not immediately available, and is 0.
            this.TrackData.SetLength(data);
        }

        this.Booleans.OnDataUpdate(data, this);

        // Skip car updates for few updated after new session so that everything from the SimHub's side would be reset
        // Atm this is important in AMS2, so that old session data doesn't leak into new session
        if (this._skipCarUpdatesCount > 100) {
            this.UpdateCars(data);
        } else {
            this._skipCarUpdatesCount++;
        }
    }

    // Temporary dicts used in UpdateCars. Don't allocate new one at each update, just clear them.
    private readonly Dictionary<CarClass, CarData> _classBestLapCars = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _cupBestLapCars = [];
    private readonly Dictionary<CarClass, int> _classPositions = [];
    private readonly Dictionary<CarClass, CarData> _classLeaders = [];
    private readonly Dictionary<CarClass, CarData> _carAheadInClass = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), int> _cupPositions = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _cupLeaders = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _carAheadInCup = [];

    private void UpdateCars(GameData data) {
        this._classBestLapCars.Clear();
        this._cupBestLapCars.Clear();
        this._classPositions.Clear();
        this._classLeaders.Clear();
        this._carAheadInClass.Clear();
        this._cupPositions.Clear();
        this._cupLeaders.Clear();
        this._carAheadInCup.Clear();

        IEnumerable<(Opponent, int)> cars = data.NewData.Opponents.WithIndex();

        string? focusedCarId = null;
        if (DynLeaderboardsPlugin.Game.IsAcc) {
            var accRawData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
            focusedCarId = accRawData.Realtime?.FocusedCarIndex.ToString();
        }

        CarData? overallBestLapCar = null;
        foreach (var (opponent, i) in cars) {
            if ((DynLeaderboardsPlugin.Game.IsAcc && opponent.Id == "Me")
                || (DynLeaderboardsPlugin.Game.IsAms2 && opponent.Id == "Safety Car  (AI)")
            ) {
                continue;
            }

            // Most common case is that the car's position hasn't changed
            var car = this._overallOrder.ElementAtOrDefault(i);
            if (car == null || car.Id != opponent.Id) {
                car = this._overallOrder.Find(c => c.Id == opponent.Id);
            }

            if (car == null) {
                if (!opponent.IsConnected || (DynLeaderboardsPlugin.Game.IsAcc && opponent.Coordinates == null)) {
                    continue;
                }

                car = new CarData(this, focusedCarId, opponent, data);
                this._overallOrder.Add(car);
                this.TrackData?.BuildLapInterpolator(car.CarClass);
            } else {
                Debug.Assert(car.Id == opponent.Id);
                car.UpdateIndependent(this, focusedCarId, opponent, data);

                // Note: car.IsFinished is actually updated in car.UpdateDependsOnOthers.
                // Thus, if the player manages to finish the race and exit before the first update, we would remove them.
                // However, that is practically impossible.
                if (!car.IsFinished && car.MissedUpdates > 500) {
                    continue;
                }
            }

            if (car.IsFocused) {
                this.FocusedCar = car;
            }

            if (car.BestLap?.Time != null) {
                if (!this._classBestLapCars.ContainsKey(car.CarClass)) {
                    this._classBestLapCars[car.CarClass] = car;
                } else {
                    var currentClassBestLap =
                        this._classBestLapCars[car.CarClass].BestLap!.Time!; // If it's in the dict, it cannot be null

                    if (car.BestLap.Time < currentClassBestLap) {
                        this._classBestLapCars[car.CarClass] = car;
                    }
                }

                if (!this._cupBestLapCars.ContainsKey((car.CarClass, car.TeamCupCategory))) {
                    this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)] = car;
                } else {
                    var currentCupBestLap =
                        this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)].BestLap!
                            .Time!; // If it's in the dict, it cannot be null

                    if (car.BestLap.Time < currentCupBestLap) {
                        this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)] = car;
                    }
                }

                if (
                    overallBestLapCar == null
                    || car.BestLap.Time < overallBestLapCar.BestLap!.Time! // If it's set, it cannot be null
                ) {
                    overallBestLapCar = car;
                }
            }
        }

        for (var i = this._overallOrder.Count - 1; i >= 0; i--) {
            var car = this._overallOrder[i];
            if (!car.IsFinished && car.MissedUpdates > 500) {
                this._overallOrder.RemoveAt(i);
                DynLeaderboardsPlugin.LogInfo($"Removed disconnected car {car.Id}, #{car.CarNumberAsString}");
            }
        }

        if (!this._startingPositionsSet && this.Session.IsRace && this._overallOrder.Count != 0) {
            this.SetStartingOrder();
        }

        this.SetOverallOrder(data);

        if (!this.IsFirstFinished && this._overallOrder.Count > 0 && this.Session.SessionType == SessionType.RACE) {
            var first = this._overallOrder.First();
            if (this.Session.IsLapLimited) {
                this.IsFirstFinished = first.Laps.New == data.NewData.TotalLaps;
            } else if (this.Session.IsTimeLimited) {
                this.IsFirstFinished = data.NewData.SessionTimeLeft.TotalSeconds <= 0 && first.IsNewLap;
            }

            if (this.IsFirstFinished) {
                DynLeaderboardsPlugin.LogInfo($"First finished: id={this._overallOrder.First().Id}");
            }
        }

        this._classOrder.Clear();
        this._cupOrder.Clear();
        this._relativeOnTrackAheadOrder.Clear();
        this._relativeOnTrackBehindOrder.Clear();
        var focusedClass = this.FocusedCar?.CarClass;
        var focusedCup = this.FocusedCar?.TeamCupCategory;
        foreach (var (car, i) in this._overallOrder.WithIndex()) {
            if (!car.IsUpdated) {
                car.MissedUpdates += 1;
                //DynLeaderboardsPlugin.LogInfo($"Car [{car.Id}, #{car.CarNumber}] missed update: {car.MissedUpdates}");
            }

            car.IsUpdated = false;

            if (!this._classPositions.ContainsKey(car.CarClass)) {
                this._classPositions.Add(car.CarClass, 1);
                this._classLeaders.Add(car.CarClass, car);
            }

            if (!this._cupPositions.ContainsKey((car.CarClass, car.TeamCupCategory))) {
                this._cupPositions.Add((car.CarClass, car.TeamCupCategory), 1);
                this._cupLeaders.Add((car.CarClass, car.TeamCupCategory), car);
            }

            if (focusedClass != null && car.CarClass == focusedClass) {
                this._classOrder.Add(car);
                if (focusedCup != null && car.TeamCupCategory == focusedCup) {
                    this._cupOrder.Add(car);
                }
            }

            car.UpdateDependsOnOthers(
                values: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: this._classBestLapCars.GetValueOr(car.CarClass, null),
                cupBestLapCar: this._cupBestLapCars.GetValueOr((car.CarClass, car.TeamCupCategory), null),
                leaderCar: this._overallOrder.First(), // If we get there, there must be at least on car
                classLeaderCar: this._classLeaders[car.CarClass], // If we get there, the leader must be present
                cupLeaderCar: this._cupLeaders
                    [(car.CarClass, car.TeamCupCategory)], // If we get there, the leader must be present
                focusedCar: this.FocusedCar,
                carAhead: i > 0 ? this._overallOrder[i - 1] : null,
                carAheadInClass: this._carAheadInClass.GetValueOr(car.CarClass, null),
                carAheadInCup: this._carAheadInCup.GetValueOr((car.CarClass, car.TeamCupCategory), null),
                carAheadOnTrack: this.GetCarAheadOnTrack(car),
                overallPosition: i + 1,
                classPosition: this._classPositions[car.CarClass]++,
                cupPosition: this._cupPositions[(car.CarClass, car.TeamCupCategory)]++
            );

            if (car.IsFocused) {
                // nothing to do
            } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                this._relativeOnTrackAheadOrder.Add(car);
            } else {
                this._relativeOnTrackBehindOrder.Add(car);
            }

            this._carAheadInClass[car.CarClass] = car;
            this._carAheadInCup[(car.CarClass, car.TeamCupCategory)] = car;
        }

        if (this.FocusedCar != null) {
            this._relativeOnTrackAheadOrder.Sort(
                (c1, c2) => c1.RelativeSplinePositionToFocusedCar.CompareTo(c2.RelativeSplinePositionToFocusedCar)
            );
            this._relativeOnTrackBehindOrder.Sort(
                (c1, c2) => c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar)
            );
        }

        this.NumClassesInSession = this._classPositions.Count;
        this.NumCupsInSession = this._cupPositions.Count;
    }

    private CarData? GetCarAheadOnTrack(CarData c) {
        var thisPos = c.SplinePosition;

        // Closest car ahead is the one with the smallest positive relative spline position.
        CarData? closestCar = null;
        var relSplinePos = double.MaxValue;
        foreach (var car in this._overallOrder) {
            var carPos = car.SplinePosition;

            var pos = CarData.CalculateRelativeSplinePosition(toPos: carPos, fromPos: thisPos);
            if (pos > 0 && pos < relSplinePos) {
                closestCar = car;
                relSplinePos = pos;
            }
        }

        return closestCar;
    }

    private void SetStartingOrder() {
        // This method is called after we have checked that all cars have NewData
        this._overallOrder.Sort(
            (a, b) => a.RawDataNew.Position.CompareTo(b.RawDataNew.Position)
        ); // Spline position may give wrong results if cars are sitting on the grid, thus NewData.Position

        var classPositions = new Dictionary<CarClass, int>(1); // Keep track of what class position are we at the moment
        var cupPositions =
            new Dictionary<(CarClass, TeamCupCategory), int>(1); // Keep track of what cup position are we at the moment
        foreach (var (car, i) in this._overallOrder.WithIndex()) {
            var thisClass = car.CarClass;
            var thisCup = car.TeamCupCategory;
            if (!classPositions.ContainsKey(thisClass)) {
                classPositions.Add(thisClass, 0);
            }

            if (!cupPositions.ContainsKey((thisClass, thisCup))) {
                cupPositions.Add((thisClass, thisCup), 0);
            }

            var classPos = ++classPositions[thisClass];
            var cupPos = ++cupPositions[(thisClass, thisCup)];
            car.SetStartingPositions(i + 1, classPos, cupPos);
        }

        this._startingPositionsSet = true;
    }

    private void SetOverallOrder(GameData gameData) {
        // Sort cars in overall position order
        if (this.Session.SessionType == SessionType.RACE) {
            // In race use TotalSplinePosition (splinePosition + laps) which updates real time.
            // RealtimeCarUpdate.Position only updates at the end of sector

            int Cmp(CarData a, CarData b) {
                if (a == b) {
                    return 0;
                }

                // Sort cars that have crossed the start line always in front of cars who haven't
                // This affect order at race starts where the number of completed laps is 0 either
                // side of the line but the spline position flips from 1 to 0
                if (a.HasCrossedStartLine && !b.HasCrossedStartLine) {
                    return -1;
                }

                if (b.HasCrossedStartLine && !a.HasCrossedStartLine) {
                    return 1;
                }

                // Always compare by laps first
                var aLaps = a.Laps.New;
                var bLaps = b.Laps.New;
                if (aLaps != bLaps) {
                    return bLaps.CompareTo(aLaps);
                }

                // Keep order if one of the cars has offset lap update, could cause jumping otherwise
                if (a.OffsetLapUpdate != 0 || b.OffsetLapUpdate != 0) {
                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                // If car jumped to the pits we need to but it behind everyone on that same lap, but it's okay for the finished car to jump to the pits
                if (a.JumpedToPits && !b.JumpedToPits && !a.IsFinished) {
                    return 1;
                }

                if (b.JumpedToPits && !a.JumpedToPits && !b.IsFinished) {
                    return -1;
                }

                if (a.IsFinished || b.IsFinished) {
                    // We cannot use NewData.Position to set results after finish because, if someone finished and leaves the server then the positions of the guys behind him would be wrong by one.
                    // Need to use FinishTime
                    if (!a.IsFinished || !b.IsFinished) {
                        // If one hasn't finished and their number of laps is same, that means that the car who has finished must be lap down.
                        // Thus, it should be behind the one who hasn't finished.
                        var aFTime = a.FinishTime?.Ticks ?? long.MinValue;
                        var bFTime = b.FinishTime?.Ticks ?? long.MinValue;
                        return aFTime.CompareTo(bFTime);
                    } else {
                        // Both cars have finished
                        var aFTime = a.FinishTime?.Ticks ?? long.MaxValue;
                        var bFTime = b.FinishTime?.Ticks ?? long.MaxValue;

                        if (aFTime == bFTime) {
                            return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
                        }

                        return aFTime.CompareTo(bFTime);
                    }
                }

                if (DynLeaderboardsPlugin.Game.IsAms2) {
                    // Spline pos == 0.0 if race has not started, race state is 1 if that's the case, use games positions
                    // If using AMS2 shared memory data, UDP data is not supported atm
                    if (gameData.NewData.GetRawDataObject() is SimHubAMS2.AMS2APIStruct { mRaceState: < 2 }) {
                        return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
                    }

                    // cars that didn't finish (DQ, DNS, DNF) should always be at the end
                    var aDidNotFinish = a.RawDataNew.DidNotFinish ?? false;
                    var bDidNotFinish = b.RawDataNew.DidNotFinish ?? false;
                    if (aDidNotFinish || bDidNotFinish) {
                        if (aDidNotFinish && bDidNotFinish) {
                            return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
                        }

                        if (aDidNotFinish) {
                            return 1;
                        }

                        return -1;
                    }
                }

                // Keep order, make sort stable, fixes jumping, essentially keeps the cars in previous order
                if (a.TotalSplinePosition == b.TotalSplinePosition) {
                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
            }

            this._overallOrder.Sort(Cmp);
        } else {
            // In other sessions TotalSplinePosition doesn't make any sense, use Position
            int Cmp(CarData a, CarData b) {
                if (a == b) {
                    return 0;
                }

                var aPos = a.RawDataNew.Position;
                var bPos = b.RawDataNew.Position;
                if (aPos == bPos) {
                    // if aPos == bPos, one cad probably left but maybe not. 
                    // Use old position to keep the order stable and not cause flickering.
                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                // Need to use RawDataNew.Position because the CarData.PositionOverall is updated based of the result of this sort
                return aPos.CompareTo(bPos);
            }

            this._overallOrder.Sort(Cmp);
        }
    }

    internal void OnGameStateChanged(bool running, PluginManager _) {
        if (running) { } else {
            this.Reset();
            // dispose track data on session end, so that we save the interpolators data after session,
            // where we have lots of time
            this.TrackData?.SaveData();
        }
    }
}

internal class SimHubClassColors {
    [JsonProperty] public Dictionary<CarClass, TextBoxColor> AssignedColors = [];

    public static SimHubClassColors FromJson(string json) {
        var self = new SimHubClassColors();

        var raw = JsonConvert.DeserializeObject<Raw>(json);
        if (raw != null) {
            foreach (var color in raw.AssignedColors) {
                var cls = new CarClass(color.Target);

                var lstar = ColorTools.LStar(color.Color);
                var fg = lstar > 70 ? "#000000" : "#FFFFFF";

                var col = new TextBoxColor(fg, color.Color);
                self.AssignedColors.Add(cls, col);
            }
        }

        return self;
    }

    [method: JsonConstructor]
    private class Raw(List<RawColor> assignedColors) {
        [JsonProperty] public List<RawColor> AssignedColors = assignedColors;
    }

    [method: JsonConstructor]
    private class RawColor(string target, string color) {
        [JsonProperty] public string Target { get; } = target;
        [JsonProperty] public string Color { get; } = color;
    }
}

internal static class ColorTools {
    public static double Lightness(string color) {
        var col = WindowsMediaColorExtensions.FromHex(color);
        var r = ColorTools.ToLinRgb(col.R / 255.0);
        var g = ColorTools.ToLinRgb(col.G / 255.0);
        var b = ColorTools.ToLinRgb(col.B / 255.0);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public static double LStar(string color) {
        var y = ColorTools.Lightness(color);
        if (y < 0.008856) {
            return y * 903.3;
        }

        return Math.Pow(y, 1.0 / 3.0) * 116.0 - 16.0;
    }

    private static double ToLinRgb(double c) {
        if (c <= 0.04045) {
            return c / 12.92;
        }

        var step1 = (c + 0.055) / 1.055;
        return Math.Pow(step1, 2.4);
    }
}