using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using KLPlugins.DynLeaderboards.Common;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Settings;

// Use FromJson and WriteToJson methods
[JsonConverter(typeof(FailJsonConverter))]
public sealed class CarInfos : IEnumerable<KeyValuePair<string, OverridableCarInfo>> {
    private readonly Dictionary<string, OverridableCarInfo> _infos;

    private CarInfos(Dictionary<string, OverridableCarInfo> infos) {
        this._infos = infos;
    }

    public OverridableCarInfo GetOrAdd(string key, CarClass carClass, CarClass? rawClass = null) {
        if (!this._infos.TryGetValue(key, out var info)) {
            var c = new OverridableCarInfo();
            c.DisableClass();
            c.DisableName();
            c.SetClass(carClass);
            c.SetName(key);
            c.SetManufacturer(key.Split(' ')[0]);
            if (rawClass != null) {
                c._SimHubCarClass = rawClass.Value;
            }

            this._infos[key] = c;

            return c;
        }

        if (rawClass != null) {
            info._SimHubCarClass = rawClass.Value;
        }

        return this._infos[key];
    }

    internal void TryRemove(string key) {
        if (!this._infos.TryGetValue(key, out var c)) {
            return;
        }

        if (c._Base != null) {
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

        return c._Base == null;
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

    private class FailJsonConverter : Common.FailJsonConverter {
        public FailJsonConverter() {
            this.SerializeMsg =
                $"`{nameof(CarInfos)}` cannot be serialized directly, use `{nameof(ClassInfos.WriteToJson)}` method instead";
            this.DeserializeMsg =
                $"`{nameof(CarInfos)}` cannot be deserialized directly, use `{nameof(ClassInfos.ReadFromJson)}` method instead";
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class OverridableCarInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    internal CarInfo? _Base { get; private set; }

    [JsonProperty("Overrides")]
    internal CarInfo? _Overrides { get; private set; }

    [JsonProperty("IsNameEnabled", Required = Required.Always)]
    internal bool _IsNameEnabled { get; private set; } = true;

    [JsonProperty("IsClassEnabled", Required = Required.Always)]
    internal bool _IsClassEnabled { get; private set; } = true;

    [JsonProperty("SimHubCarClass")]
    internal CarClass _SimHubCarClass { get; set; } = CarClass.Default;

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
            this._IsNameEnabled = isNameEnabled.Value;
        }

        if (isClassEnabled != null) {
            this._IsClassEnabled = isClassEnabled.Value;
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
            $"CarInfo: base: {JsonConvert.SerializeObject(this._Base)}, overrides: {JsonConvert.SerializeObject(this._Overrides)}";
    }

    internal void SetOverrides(CarInfo? overrides) {
        this._Overrides = overrides;
    }

    internal void SetBase(CarInfo? @base) {
        this._Base = @base;

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
        return this._Base?._Name;
    }

    public string? Name() {
        if (!this._IsNameEnabled) {
            return null;
        }

        return this._Overrides?._Name ?? this._Base?._Name;
    }

    internal string? NameDontCheckEnabled() {
        return this._Overrides?._Name ?? this._Base?._Name;
    }

    internal void SetName(string name) {
        this._Overrides ??= new CarInfo();
        this._Overrides._Name = name;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
    }

    internal void ResetName() {
        if (this._Overrides != null) {
            this._Overrides._Name = null;
        }

        // default is enabled if base is present
        this._IsNameEnabled = this._Base?._Name != null;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsNameEnabled));
    }

    internal void DisableName() {
        this._IsNameEnabled = false;
        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsNameEnabled));
    }

    internal void EnableName() {
        this._IsNameEnabled = true;
        this.InvokePropertyChanged(nameof(OverridableCarInfo.Name));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsNameEnabled));
    }

    internal void EnableName(string key) {
        if (this._Overrides?._Name == null) {
            this._Overrides ??= new CarInfo();
            this._Overrides._Name = key;
        }

        this.EnableName();
    }

    internal string? BaseManufacturer() {
        return this._Base?._Manufacturer;
    }

    public string? Manufacturer() {
        return this._Overrides?._Manufacturer ?? this._Base?._Manufacturer;
    }

    internal void SetManufacturer(string manufacturer) {
        this._Overrides ??= new CarInfo();
        this._Overrides._Manufacturer = manufacturer;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Manufacturer));
    }

    internal void ResetManufacturer() {
        if (this._Overrides != null) {
            this._Overrides._Manufacturer = null;
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
        return this._Base?._Class;
    }

    public CarClass Class() {
        if (!this._IsClassEnabled) {
            return this._SimHubCarClass;
        }

        return this.ClassDontCheckEnabled();
    }

    internal CarClass ClassDontCheckEnabled() {
        return this._Overrides?._Class ?? this._Base?._Class ?? this._SimHubCarClass;
    }

    internal void SetClass(CarClass cls) {
        this._Overrides ??= new CarInfo();
        this._Overrides._Class = cls;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsClassEnabled));
    }

    internal void ResetClass() {
        if (this._Overrides != null) {
            this._Overrides._Class = null;
        }

        // default is enabled if base is present
        this._IsClassEnabled = this._Base?._Class != null;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsClassEnabled));
    }

    internal void DisableClass() {
        this._IsClassEnabled = false;

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsClassEnabled));
    }

    internal void EnableClass() {
        this._IsClassEnabled = true;
        // we are explicitly asked to enable class, there must be some override in it
        // it must be whatever we are showing at the moment, and it should never change
        if (this._Overrides?._Class == null) {
            this._Overrides ??= new CarInfo();
            this._Overrides._Class = this.ClassDontCheckEnabled();
        }

        this.InvokePropertyChanged(nameof(OverridableCarInfo.Class));
        this.InvokePropertyChanged(nameof(OverridableCarInfo._IsClassEnabled));
    }
}

[JsonObject(MemberSerialization.OptIn)]
internal sealed class CarInfo {
    [JsonProperty("Name")]
    internal string? _Name { get; set; }

    [JsonProperty("Manufacturer")]
    internal string? _Manufacturer { get; set; }

    [JsonProperty("Class")]
    internal CarClass? _Class { get; set; }

    [JsonConstructor]
    public CarInfo(string? name, string? manufacturer, CarClass? cls) {
        this._Name = name;
        this._Manufacturer = manufacturer;
        this._Class = cls;
    }

    public CarInfo() { }
}