using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using KLPlugins.DynLeaderboards.Car;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings;

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
            if (kv.Value.ClassDontCheckEnabled() == oldCls)
                // this happens in two cases:
                // 1. override was set to old class, just set it to new class
                // 2. base was set to old class. 
                //    But we cannot and don't want to change base values as they are not saved. 
                //    So we set the override class to the new one.
                //    This also allows "Reset" button to reset to plugin defaults as promised.
            {
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
    [JsonProperty] internal CarClass SimHubCarClass { get; set; } = CarClass.Default;

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

    private void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
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

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
    }

    internal void ResetName() {
        if (this.Overrides != null) {
            this.Overrides.Name = null;
        }

        // default is enabled if base is present
        this.IsNameEnabled = this.Base?.Name != null;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsNameEnabled));
    }

    internal void DisableName() {
        this.IsNameEnabled = false;
        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsNameEnabled));
    }

    internal void EnableName() {
        this.IsNameEnabled = true;
        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsNameEnabled));
    }

    internal void EnableName(string key) {
        if (this.Overrides?.Name == null) {
            this.Overrides ??= new CarInfo();
            this.Overrides.Name = key;
        }

        this.EnableName();
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

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Manufacturer));
    }

    internal void ResetManufacturer() {
        if (this.Overrides != null) {
            this.Overrides.Manufacturer = null;
        }

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Manufacturer));
    }

    internal void ResetManufacturer(string key) {
        this.ResetManufacturer();

        if (this.Manufacturer() == null)
            // default is the first word of full name/key
        {
            this.SetManufacturer(key.Split(' ')[0]);
        }
    }

    internal CarClass? BaseClass() {
        return this.Base?.Class;
    }

    internal CarClass Class() {
        if (!this.IsClassEnabled) {
            return this.SimHubCarClass;
        }

        return this.ClassDontCheckEnabled();
    }

    internal CarClass ClassDontCheckEnabled() {
        return this.Overrides?.Class ?? this.Base?.Class ?? this.SimHubCarClass;
    }

    internal void SetClass(CarClass cls) {
        this.Overrides ??= new CarInfo();
        this.Overrides.Class = cls;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsClassEnabled));
    }

    internal void ResetClass() {
        if (this.Overrides != null) {
            this.Overrides.Class = null;
        }

        // default is enabled if base is present
        this.IsClassEnabled = this.Base?.Class != null;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsClassEnabled));
    }

    internal void DisableClass() {
        this.IsClassEnabled = false;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsClassEnabled));
    }

    internal void EnableClass() {
        this.IsClassEnabled = true;
        // we are explicitly asked to enable class, there must be some override in it
        // it must be whatever we are showing at the moment, and it should never change
        if (this.Overrides?.Class == null) {
            this.Overrides ??= new CarInfo();
            this.Overrides.Class = this.ClassDontCheckEnabled();
        }

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo.IsClassEnabled));
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