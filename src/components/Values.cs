using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace KLPlugins.DynLeaderboards {
    internal class TextBoxColors<K> : IEnumerable<KeyValuePair<K, OverridableTextBoxColor>> {
        private SortedDictionary<K, OverridableTextBoxColor> _colors;

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
            colors ??= new();

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
        }

        internal bool HasBase() {
            return this._base != null;
        }

        internal void Reset() {
            this._overrides = null;
            this.IsEnabled = true;
        }

        internal void Enable() {
            this.IsEnabled = true;
        }

        internal void Disable() {
            this.IsEnabled = false;
        }

        internal string? Foreground() {
            if (!this.IsEnabled) {
                return null;
            }

            return this._overrides?.Fg ?? this._base?.Fg ?? DEF_FG;
        }

        internal string ForegroundDontCheckEnabled() {
            return this._overrides?.Fg ?? this._base?.Fg ?? DEF_FG;
        }

        internal string BaseForeground() {
            return this._base?.Fg ?? DEF_FG;
        }

        internal void SetForeground(string fg) {
            this._overrides ??= new();
            this._overrides.Fg = fg;
        }

        internal void ResetForeground() {
            if (this._overrides != null) {
                this._overrides.Fg = null;
            }
        }

        internal string? Background() {
            if (!this.IsEnabled) {
                return null;
            }

            return this._overrides?.Bg ?? this._base?.Bg ?? DEF_BG;
        }

        internal string BackgroundDontCheckEnabled() {
            return this._overrides?.Bg ?? this._base?.Bg ?? DEF_BG;
        }

        internal string BaseBackground() {
            return this._base?.Bg ?? DEF_BG;
        }

        internal void SetBackground(string bg) {
            this._overrides ??= new();
            this._overrides.Bg = bg;
        }

        internal void ResetBackground() {
            if (this._overrides != null) {
                this._overrides.Bg = null;
            }
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
                c.SetManufacturer(key.Split(' ').First());

                this._infos[key] = c;
            }

            return this._infos[key];
        }

        internal void Remove(string key) {
            if (!this._infos.ContainsKey(key)) {
                return;
            }

            var c = this._infos[key];
            if (c.Base != null) {
                c.Reset();
                c.DisableName();
                c.DisableClass();
            } else {
                this._infos.Remove(key);
            }
        }

        internal bool ContainsClass(CarClass cls) {
            foreach (var kv in this._infos) {
                if (kv.Value.ClassDontCheckEnabled() == cls) {
                    return true;
                }
            }

            return false;
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

    internal class OverridableCarInfo {
        [JsonIgnore] internal CarInfo? Base { get; private set; }
        [JsonProperty("Overrides")] internal CarInfo? Overrides { get; private set; }
        [JsonProperty] internal bool IsNameEnabled { get; private set; } = true;
        [JsonProperty] internal bool IsClassEnabled { get; private set; } = true;

        [JsonConstructor]
        internal OverridableCarInfo(CarInfo? @base, CarInfo? overrides = null, bool? isNameEnabled = null, bool? isClassEnabled = null) {
            this.Base = @base;
            this.Overrides = overrides;
            if (isNameEnabled != null) {
                this.IsNameEnabled = isNameEnabled.Value;
            }

            if (isClassEnabled != null) {
                this.IsClassEnabled = isClassEnabled.Value;
            }
        }

        internal OverridableCarInfo(string? name, string? manufacturer, CarClass? cls) : this(new CarInfo(name, manufacturer, cls)) { }
        internal OverridableCarInfo() : this(new CarInfo()) { }

        internal string Debug() {
            return $"CarInfo: base: {JsonConvert.SerializeObject(this.Base)}, overrides: {JsonConvert.SerializeObject(this.Overrides)}";
        }

        internal void SetOverrides(CarInfo? overrides) {
            this.Overrides = overrides;
        }

        internal void SetBase(CarInfo? @base) {
            this.Base = @base;
        }

        internal void Reset() {
            this.IsNameEnabled = true;
            this.IsClassEnabled = true;
            this.ResetName();
            this.ResetClass();
            this.ResetManufacturer();
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
            this.Overrides ??= new();
            this.Overrides.Name = name;
        }

        internal void ResetName() {
            if (this.Base?.Name == null) {
                this.IsNameEnabled = false;
            } else if (this.Overrides != null) {
                this.Overrides.Name = null;
            }

        }

        internal void DisableName() {
            this.IsNameEnabled = false;
        }

        internal void EnableName() {
            this.IsNameEnabled = true;
        }

        internal string? BaseManufacturer() {
            return this.Base?.Manufacturer;
        }

        internal string? Manufacturer() {
            return this.Overrides?.Manufacturer ?? this.Base?.Manufacturer;
        }

        internal void SetManufacturer(string manufacturer) {
            this.Overrides ??= new();
            this.Overrides.Manufacturer = manufacturer;
        }


        internal void ResetManufacturer() {
            if (this.Base?.Manufacturer == null) {
                // do nothing
            } else if (this.Overrides != null) {
                this.Overrides.Manufacturer = null;
            }
        }

        internal CarClass? BaseClass() {
            return this.Base?.Class;
        }

        internal CarClass? Class() {
            if (!this.IsClassEnabled) {
                return null;
            }

            return this.Overrides?.Class ?? this.Base?.Class;
        }

        internal CarClass? ClassDontCheckEnabled() {
            return this.Overrides?.Class ?? this.Base?.Class;
        }

        internal void SetClass(CarClass cls) {
            this.Overrides ??= new();
            this.Overrides.Class = cls;
        }

        internal void ResetClass() {
            if (this.Base?.Class == null) {
                this.IsClassEnabled = false;
            } else if (this.Overrides != null) {
                this.Overrides.Class = null;
            }
        }

        internal void DisableClass() {
            this.IsClassEnabled = false;
        }

        internal void EnableClass() {
            this.IsClassEnabled = true;
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

    internal class ClassInfos {
        private readonly Dictionary<CarClass, OverridableClassInfo> _infos;
        
        internal ClassInfos(Dictionary<CarClass, OverridableClassInfo> infos) {
            this._infos = infos;
        }

        internal OverridableClassInfo Get(CarClass cls) {
            if (!this._infos.ContainsKey(cls)) {
                var c = new OverridableClassInfo(null, null);
                c.DisableColor();
                c.DisableSameAs();

                this._infos[cls] = c;
            }

            return this._infos[cls];
        }

        internal (CarClass, OverridableClassInfo) GetFollowSameAs(CarClass cls) {
            var clsOut = cls;
            var info = this.Get(cls);
            var nextCls = info.SameAs();

            var seenClasses = new List<CarClass> { cls };

            while (nextCls != null && nextCls != clsOut) {
                clsOut = nextCls.Value;
                info = this.Get(clsOut);

                if (seenClasses.Contains(clsOut)) {
                    DynLeaderboardsPlugin.LogWarn($"Loop detected in class same as values: {string.Join(" -> ", seenClasses)} -> {clsOut}");
                    break;
                }
                seenClasses.Add(clsOut);

                nextCls = info.SameAs();
            }

            return (clsOut, info);
        }

        internal void Remove(CarClass cls) {
            if (!this._infos.ContainsKey(cls)) {
                return;
            }

            var c = this._infos[cls];
            if (c.Base != null) {
                c.Reset();
                c.DisableColor();
                c.DisableSameAs();
            } else {
                this._infos.Remove(cls);
            }
        }

        internal bool ContainsClass(CarClass cls) {
            return this._infos.ContainsKey(cls);
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
                        infos[kv.Key].SetBase(kv.Value);
                    } else {
                        var info = new OverridableClassInfo(@base: kv.Value, null);
                        infos[kv.Key] = info;
                    }
                }
            }

            return new ClassInfos(infos);
        }


        internal void WriteToJson(string path, string derivedPath) {
            File.WriteAllText(path, JsonConvert.SerializeObject(this._infos, Formatting.Indented));
        }
    }

    internal class OverridableClassInfo {
        [JsonIgnore] internal ClassInfo? Base { get; private set; }
        [JsonProperty] internal ClassInfo? Overrides { get; private set; }
        [JsonProperty] internal bool IsColorEnabled { get; private set; }
        [JsonProperty] internal bool IsSameAsEnabled { get; private set; }

        [JsonConstructor]
        internal OverridableClassInfo(ClassInfo? @base, ClassInfo? overrides, bool isColorEnabled = true, bool isSameAsEnabled = true) {
            this.Base = @base;
            this.Overrides = overrides;
            this.IsColorEnabled = isColorEnabled;
            this.IsSameAsEnabled = isSameAsEnabled;
        }

        internal void SetOverrides(ClassInfo? overrides) {
            this.Overrides = overrides;
        }

        internal void SetBase(ClassInfo? @base) {
            this.Base = @base;
        }

        internal void Reset() {
            this.IsColorEnabled = true;
            this.IsSameAsEnabled = true;
            this.ResetBackground();
            this.ResetForeground();
            this.ResetSameAs();
        }

        internal string? Foreground() {
            if (!this.IsColorEnabled) {
                return null;
            }

            return this.Overrides?.Color?.Fg ?? this.Base?.Color?.Fg ?? OverridableTextBoxColor.DEF_FG;
        }

        internal string ForegroundDontCheckEnabled() {
            return this.Overrides?.Color?.Fg ?? this.Base?.Color?.Fg ?? OverridableTextBoxColor.DEF_FG;
        }

        internal string BaseForeground() {
            return this.Base?.Color?.Fg ?? OverridableTextBoxColor.DEF_FG;
        }

        internal void SetForeground(string fg) {
            this.Overrides ??= new();
            this.Overrides.Color ??= new();
            this.Overrides.Color.Fg = fg;
        }

        internal void ResetForeground() {
            if (this.Overrides?.Color != null) {
                this.Overrides.Color.Fg = null;
            }
        }

        internal string? Background() {
            if (!this.IsColorEnabled) {
                return null;
            }

            return this.Overrides?.Color?.Bg ?? this.Base?.Color?.Bg ?? OverridableTextBoxColor.DEF_BG;
        }

        internal string BackgroundDontCheckEnabled() {
            return this.Overrides?.Color?.Bg ?? this.Base?.Color?.Bg ?? OverridableTextBoxColor.DEF_BG;
        }

        internal string BaseBackground() {
            return this.Base?.Color?.Bg ?? OverridableTextBoxColor.DEF_BG;
        }

        internal void SetBackground(string bg) {
            this.Overrides ??= new();
            this.Overrides.Color ??= new();
            this.Overrides.Color.Bg = bg;
        }

        internal void ResetBackground() {
            if (this.Overrides?.Color != null) {
                this.Overrides.Color.Bg = null;
            }
        }

        internal void DisableColor() {
            this.IsColorEnabled = false;
        }

        internal void EnableColor() {
            this.IsColorEnabled = true;
        }

        internal CarClass? BaseSameAs() {
            return this.Base?.SameAs;
        }

        internal CarClass? SameAs() {
            if (!this.IsSameAsEnabled) {
                return null;
            }

            return this.Overrides?.SameAs ?? this.Base?.SameAs;
        }

        internal CarClass? SameAsDontCheckEnabled() {
            return this.Overrides?.SameAs ?? this.Base?.SameAs;
        }

        internal void SetSameAs(CarClass? sameAs) {
            this.Overrides ??= new();
            this.Overrides.SameAs = sameAs;
        }

        internal void ResetSameAs() {
            if (this.Base?.SameAs == null) {
                this.IsSameAsEnabled = false;
            } else if (this.Overrides != null) {
                this.Overrides.SameAs = null;
            }
        }

        internal void DisableSameAs() {
            this.IsSameAsEnabled = false;
        }

        internal void EnableSameAs() {
            this.IsSameAsEnabled = true;
        }
    }

    internal class ClassInfo {
        [JsonProperty] internal TextBoxColor? Color { get; set; }
        [JsonProperty] internal CarClass? SameAs { get; set; }


        [JsonConstructor]
        internal ClassInfo(TextBoxColor? color, CarClass? sameAs) {
            this.Color = color;
            this.SameAs = sameAs;
        }

        internal ClassInfo() { }
    }

    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public TrackData? TrackData { get; private set; }
        public Session Session { get; private set; } = new();
        public Booleans Booleans { get; private set; } = new();

        public ReadOnlyCollection<CarData> OverallOrder { get; }
        public ReadOnlyCollection<CarData> ClassOrder { get; }
        public ReadOnlyCollection<CarData> CupOrder { get; }
        public ReadOnlyCollection<CarData> RelativeOnTrackAheadOrder { get; }
        public ReadOnlyCollection<CarData> RelativeOnTrackBehindOrder { get; }
        private List<CarData> _overallOrder { get; } = new();
        private List<CarData> _classOrder { get; } = new();
        private List<CarData> _cupOrder { get; } = new();
        private List<CarData> _relativeOnTrackAheadOrder { get; } = new();
        private List<CarData> _relativeOnTrackBehindOrder { get; } = new();
        public CarData? FocusedCar { get; private set; } = null;

        public int NumClassesInSession { get; private set; } = 0;
        public int NumCupsInSession { get; private set; } = 0;

        internal CarInfos CarInfos { get; private set; }

        private const string _CAR_INFOS_FILENAME = "CarInfos";
        private static string CarInfosPath() {
            return $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{_CAR_INFOS_FILENAME}.json";
        }

        private static string CarInfosBasePath() {
            return $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{_CAR_INFOS_FILENAME}.base.json";
        }

        private static CarInfos ReadCarInfos() {
            var basesPath = CarInfosBasePath();
            var path = CarInfosPath();
            return CarInfos.ReadFromJson(basePath: basesPath, path: path);
        }

        private void WriteCarInfos() {
            string path = CarInfosPath();
            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            this.CarInfos.WriteToJson(path: path, derivedPath: CarInfosBasePath());
        }

        internal ClassInfos ClassInfos { get; private set; }
        private const string _CLASS_INFOS_FILENAME = "ClassInfos";
        private static string ClassInfosPath() {
            return $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{_CLASS_INFOS_FILENAME}.json";
        }

        private static string ClassInfosBasePath() {
            return $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{_CLASS_INFOS_FILENAME}.base.json";
        }

        private static ClassInfos ReadClassInfos() {
            var basesPath = ClassInfosBasePath();
            var path = ClassInfosPath();
            return ClassInfos.ReadFromJson(basePath: basesPath, path: path);
        }

        private void WriteClassInfos() {
            string path = ClassInfosPath();
            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            this.ClassInfos.WriteToJson(path: path, derivedPath: ClassInfosBasePath());
        }

        internal TextBoxColors<CarClass> CarClassColors { get; }
        internal TextBoxColors<TeamCupCategory> TeamCupCategoryColors { get; }
        internal TextBoxColors<DriverCategory> DriverCategoryColors { get; }

        private static string TextBoxColorsPath(string fileName) {
            return $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.json";
        }

        private static TextBoxColors<K> ReadTextBoxColors<K>(string fileName) {
            var basesPath = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.base.json";
            var path = TextBoxColorsPath(fileName);
            return TextBoxColors<K>.ReadFromJson(basePath: basesPath, path: path);
        }
        private static void WriteTextBoxColors<K>(TextBoxColors<K> colors, string fileName) {
            string path = TextBoxColorsPath(fileName);
            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            colors.WriteToJson(path);
        }

        internal bool IsFirstFinished { get; private set; } = false;
        private bool _startingPositionsSet = false;

        private const string _carClassColorsJsonName = "CarClassColors";
        private const string _teamCupCategoryColorsJsonName = "TeamCupCategoryColors";
        private const string _driverCategoryColorsJsonName = "DriverCategoryColors";

        internal Values() {
            this.CarInfos = ReadCarInfos();
            this.ClassInfos = ReadClassInfos();
            this.CarClassColors = ReadTextBoxColors<CarClass>(_carClassColorsJsonName);
            this.TeamCupCategoryColors = ReadTextBoxColors<TeamCupCategory>(_teamCupCategoryColorsJsonName);
            this.DriverCategoryColors = ReadTextBoxColors<DriverCategory>(_driverCategoryColorsJsonName);

            this.OverallOrder = this._overallOrder.AsReadOnly();
            this.ClassOrder = this._classOrder.AsReadOnly();
            this.CupOrder = this._cupOrder.AsReadOnly();
            this.RelativeOnTrackAheadOrder = this._relativeOnTrackAheadOrder.AsReadOnly();
            this.RelativeOnTrackBehindOrder = this._relativeOnTrackBehindOrder.AsReadOnly();

            this.Reset();
        }

        internal void UpdateCarInfos() {
            this.CarInfos = ReadCarInfos();
        }

        internal void Reset() {
            DynLeaderboardsPlugin.LogInfo($"Values.Reset()");
            this.Session.Reset();
            this.ResetWithoutSession();
        }

        internal void ResetWithoutSession() {
            DynLeaderboardsPlugin.LogInfo($"Values.ResetWithoutSession()");
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
                    WriteTextBoxColors(this.CarClassColors, _carClassColorsJsonName);
                    WriteTextBoxColors(this.TeamCupCategoryColors, _teamCupCategoryColorsJsonName);
                    WriteTextBoxColors(this.DriverCategoryColors, _driverCategoryColorsJsonName);
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

        private int _skipCarUpdatesCount = 0;
        internal void OnDataUpdate(PluginManager _, GameData data) {
            this.Session.OnDataUpdate(data);

            if (this.Booleans.NewData.IsNewEvent || this.Session.IsNewSession || this.TrackData?.PrettyName != data.NewData.TrackName) {
                DynLeaderboardsPlugin.LogInfo($"newEvent={this.Booleans.NewData.IsNewEvent}, newSession={this.Session.IsNewSession}");
                this.ResetWithoutSession();
                this.Booleans.OnNewEvent(this.Session.SessionType);
                this.TrackData?.Dispose();
                this.TrackData = new TrackData(data);
                foreach (var car in this.OverallOrder) {
                    this.TrackData.BuildLapInterpolator(car.CarClass);
                }
                this._skipCarUpdatesCount = 0;

                DynLeaderboardsPlugin.LogInfo($"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}, len={this.TrackData.LengthMeters}");
            }

            this.TrackData?.OnDataUpdate();

            if (this.TrackData != null && this.TrackData.LengthMeters == 0) {
                // In ACC sometimes the track length is not immediately available, and is 0.
                this.TrackData?.SetLength(data);
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
                    || (DynLeaderboardsPlugin.Game.IsAMS2 && opponent.Id == "Safety Car  (AI)")
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
                    // Thus is the player manages to finish the race and exit before the first update, we would remove them.
                    // However that is practically impossible.
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
                        var currentClassBestLap = this._classBestLapCars[car.CarClass].BestLap!.Time!; // If it's in the dict, it cannot be null

                        if (car.BestLap.Time < currentClassBestLap) {
                            this._classBestLapCars[car.CarClass] = car;
                        }
                    }

                    if (!this._cupBestLapCars.ContainsKey((car.CarClass, car.TeamCupCategory))) {
                        this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)] = car;
                    } else {
                        var currentCupBestLap = this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)].BestLap!.Time!; // If it's in the dict, it cannot be null

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

            for (int i = this._overallOrder.Count - 1; i >= 0; i--) {
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

            if (!this.IsFirstFinished && this._overallOrder.Count > 0 && this.Session.SessionType == SessionType.Race) {
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
                    cupLeaderCar: this._cupLeaders[(car.CarClass, car.TeamCupCategory)], // If we get there, the leader must be present
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
                this._relativeOnTrackAheadOrder.Sort((c1, c2) => c1.RelativeSplinePositionToFocusedCar.CompareTo(c2.RelativeSplinePositionToFocusedCar));
                this._relativeOnTrackBehindOrder.Sort((c1, c2) => c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar));
            }

            this.NumClassesInSession = this._classPositions.Count;
            this.NumCupsInSession = this._cupPositions.Count;
        }

        private CarData? GetCarAheadOnTrack(CarData c) {
            var thisPos = c.SplinePosition;

            // Closest car ahead is the one with smallest positive relative spline position.
            CarData? closestCar = null;
            double relsplinepos = double.MaxValue;
            foreach (var car in this._overallOrder) {
                var carPos = car.SplinePosition;

                var pos = CarData.CalculateRelativeSplinePosition(toPos: carPos, fromPos: thisPos);
                if (pos > 0 && pos < relsplinepos) {
                    closestCar = car;
                    relsplinepos = (double)pos;
                }
            }
            return closestCar;
        }

        void SetStartingOrder() {
            // This method is called after we have checked that all cars have NewData
            this._overallOrder.Sort((a, b) => a.RawDataNew.Position.CompareTo(b.RawDataNew.Position)); // Spline position may give wrong results if cars are sitting on the grid, thus NewData.Position

            var classPositions = new Dictionary<CarClass, int>(1); // Keep track of what class position are we at the moment
            var cupPositions = new Dictionary<(CarClass, TeamCupCategory), int>(1); // Keep track of what cup position are we at the moment
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
            if (this.Session.SessionType == SessionType.Race) {
                // In race use TotalSplinePosition (splinePosition + laps) which updates real time.
                // RealtimeCarUpdate.Position only updates at the end of sector

                int cmp(CarData a, CarData b) {
                    if (a == b) {
                        return 0;
                    }

                    // Sort cars that have crossed the start line always in front of cars who haven't
                    // This affect order at race starts where the number of completed laps is 0 either
                    // side of the line but the spline position flips from 1 to 0
                    if (a.HasCrossedStartLine && !b.HasCrossedStartLine) {
                        return -1;
                    } else if (b.HasCrossedStartLine && !a.HasCrossedStartLine) {
                        return 1;
                    }

                    // Always compare by laps first
                    var alaps = a.Laps.New;
                    var blaps = b.Laps.New;
                    if (alaps != blaps) {
                        return blaps.CompareTo(alaps);
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
                            // Thus it should be behind the one who hasn't finished.
                            var aFTime = a.FinishTime == null ? long.MinValue : a.FinishTime.Value.Ticks;
                            var bFTime = b.FinishTime == null ? long.MinValue : b.FinishTime.Value.Ticks;
                            return aFTime.CompareTo(bFTime);
                        } else {
                            // Both cars have finished
                            var aFTime = a.FinishTime == null ? long.MaxValue : a.FinishTime.Value.Ticks;
                            var bFTime = b.FinishTime == null ? long.MaxValue : b.FinishTime.Value.Ticks;

                            if (aFTime == bFTime) {
                                return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
                            }
                            return aFTime.CompareTo(bFTime);
                        }
                    }

                    if (DynLeaderboardsPlugin.Game.IsAMS2) {
                        // Spline pos == 0.0 if race has not started, race state is 1 if that's the case, use games positions
                        if (gameData.NewData.GetRawDataObject() is SimHubAMS2.AMS2APIStruct rawAMS2Data) {
                            // If using AMS2 shared memory data, UDP data is not supported atm
                            if (rawAMS2Data.mRaceState < 2) {
                                return a.RawDataNew!.Position.CompareTo(b.RawDataNew!.Position);
                            }
                        }


                        // cars that didn't finish (DQ, DNS, DNF) should always be at the end
                        var aDidNotFinish = a.RawDataNew?.DidNotFinish ?? false;
                        var bDidNotFinish = b.RawDataNew?.DidNotFinish ?? false;
                        if (aDidNotFinish || bDidNotFinish) {
                            if (aDidNotFinish && bDidNotFinish) {
                                return a.RawDataNew!.Position.CompareTo(b.RawDataNew!.Position);
                            } else if (aDidNotFinish) {
                                return 1;
                            } else {
                                return -1;
                            }
                        }
                    }

                    // Keep order, make sort stable, fixes jumping, essentially keeps the cars in previous order
                    if (a.TotalSplinePosition == b.TotalSplinePosition) {
                        return a.PositionOverall.CompareTo(b.PositionOverall);
                    }
                    return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
                };

                this._overallOrder.Sort(cmp);
            } else {
                // In other sessions TotalSplinePosition doesn't make any sense, use Position
                int cmp(CarData a, CarData b) {
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

                this._overallOrder.Sort(cmp);
            }
        }

        internal void OnGameStateChanged(bool running, PluginManager _) {
            if (running) {
            } else {
                this.Reset();
            }
        }

    }
}