using System;
using System.Collections.Generic;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Track;
using KLPlugins.DynLeaderboards.Car;

using SimHub.Plugins;
using System.Linq;
using KLPlugins.DynLeaderboards.Helpers;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using KLPlugins.DynLeaderboards.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KLPlugins.DynLeaderboards {

    internal class CarInfo {

        internal class Inner {
            [JsonProperty] public string? Name { get; set; }
            [JsonProperty] public string? Manufacturer { get; set; }
            [JsonProperty] public CarClass? Class { get; set; }

            [JsonConstructor]
            internal Inner(string? name, string? manufacturer, CarClass? cls) {
                this.Name = name;
                this.Manufacturer = manufacturer;
                this.Class = cls;
            }

            internal Inner() { }
        }

        [JsonProperty("Base")]
        private Inner? _base;
        [JsonProperty("Overrides")]
        private Inner? _overrides;
        [JsonProperty] public bool IsNameEnabled { get; private set; } = true;
        [JsonProperty] public bool IsClassEnabled { get; private set; } = true;

        [JsonConstructor]
        internal CarInfo(Inner? @base, Inner? overrides = null, bool? isNameEnabled = null, bool? isClassEnabled = null) {
            this._base = @base;
            this._overrides = overrides;
            if (isNameEnabled != null) {
                this.IsNameEnabled = isNameEnabled.Value;
            }

            if (isClassEnabled != null) {
                this.IsClassEnabled = isClassEnabled.Value;
            }
        }

        internal CarInfo(string? name, string? manufacturer, CarClass? cls) : this(new Inner(name, manufacturer, cls)) { }

        public string Debug() {
            return $"CarInfo: base: {JsonConvert.SerializeObject(this._base)}, overrides: {JsonConvert.SerializeObject(this._overrides)}";
        }

        public void Reset() {
            this._overrides = null;
            this.IsNameEnabled = true;
            this.IsClassEnabled = true;
        }

        public string? BaseName() {
            return this._base?.Name;
        }

        public string? Name() {
            if (!this.IsNameEnabled) {
                return null;
            }

            return this._overrides?.Name ?? this._base?.Name;
        }

        public void SetName(string name) {
            this._overrides ??= new();
            this._overrides.Name = name;
        }

        public void ResetName() {
            if (this._overrides != null) {
                this._overrides.Name = null;
            }
        }

        public void DisableName() {
            this.IsNameEnabled = false;
        }

        public void EnableName() {
            this.IsNameEnabled = true;
        }

        public string? BaseManufacturer() {
            return this._base?.Manufacturer;
        }

        public string? Manufacturer() {
            return this._overrides?.Manufacturer ?? this._base?.Manufacturer;
        }

        public void SetManufacturer(string manufacturer) {
            this._overrides ??= new();
            this._overrides.Manufacturer = manufacturer;
        }

        public void ResetManufacturer() {
            if (this._overrides != null) {
                this._overrides.Manufacturer = null;
            }
        }

        public CarClass? BaseClass() {
            return this._base?.Class;
        }

        public CarClass? Class() {
            if (!this.IsClassEnabled) {
                return null;
            }

            return this._overrides?.Class ?? this._base?.Class;
        }

        public void SetClass(CarClass cls) {
            this._overrides ??= new();
            this._overrides.Class = cls;
        }

        public void ResetClass() {
            if (this._overrides != null) {
                this._overrides.Class = null;
            }
        }

        public void DisableClass() {
            this.IsClassEnabled = false;
        }

        public void EnableClass() {
            this.IsClassEnabled = true;
        }
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

        private Dictionary<string, CarInfo> _carInfos;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="carName">Car name returned by Opponent.CarName</param>
        /// <returns></returns>
        internal CarInfo? GetCarInfo(string carName) {
            return this._carInfos.GetValueOr(carName, null);
        }
        internal IEnumerable<KeyValuePair<string, CarInfo>> CarInfos => this._carInfos;

        private static Dictionary<string, CarInfo> ReadCarInfos() {
            var path = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\CarInfos.json";

            Dictionary<string, CarInfo>? carInfos = null;
            var dataExists = File.Exists(path);
            if (DynLeaderboardsPlugin.Game.IsAc && !dataExists) {
                DynLeaderboardsPlugin.UpdateACCarInfos();
            }

            if (File.Exists(path)) {
                carInfos = JsonConvert.DeserializeObject<Dictionary<string, CarInfo>>(File.ReadAllText(path));
            }

            carInfos ??= [];

            DynLeaderboardsPlugin.LogInfo($"CarInfos: {carInfos.Count}");


            return carInfos;
        }

        private void WriteCarInfos() {
            var path = $"{PluginSettings.PluginDataDir}\\{DynLeaderboardsPlugin.Game.Name}\\CarInfos.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(this._carInfos, Formatting.Indented));
        }

        private readonly TextBoxColors<CarClass> _carClassColors;
        internal TextBoxColor? GetCarClassColor(CarClass carClass) {
            return this._carClassColors.Get(carClass);
        }
        internal IEnumerable<KeyValuePair<CarClass, TextBoxColor>> CarClassColors => this._carClassColors.GetEnumerable();

        private readonly TextBoxColors<TeamCupCategory> _teamCupCategoryColors;
        internal TextBoxColor? GetTeamCupCategoryColor(TeamCupCategory teamCupCategory) {
            return this._teamCupCategoryColors.Get(teamCupCategory);
        }
        internal IEnumerable<KeyValuePair<TeamCupCategory, TextBoxColor>> TeamCupCategoryColors => this._teamCupCategoryColors.GetEnumerable();

        private readonly TextBoxColors<DriverCategory> _driverCategoryColors;
        internal TextBoxColor? GetDriverCategoryColor(DriverCategory teamCupCategory) {
            return this._driverCategoryColors.Get(teamCupCategory);
        }
        internal IEnumerable<KeyValuePair<DriverCategory, TextBoxColor>> DriverCategoryColors => this._driverCategoryColors.GetEnumerable();

        private static TextBoxColors<K> ReadTextBoxColors<K>(string fileName) {
            var pathEnd = $"\\{fileName}";
            var basePath = PluginSettings.PluginDataDirBase + pathEnd;
            var overridesPath = PluginSettings.PluginDataDirOverrides + pathEnd;

            TextBoxColors<K>? colors = null;
            if (File.Exists(basePath)) {
                colors = JsonConvert.DeserializeObject<TextBoxColors<K>>(File.ReadAllText(basePath));
            }

            if (File.Exists(overridesPath)) {
                var overrides = JsonConvert.DeserializeObject<TextBoxColors<K>>(File.ReadAllText(overridesPath));
                if (colors != null) {
                    if (overrides != null) {
                        colors.Merge(overrides);
                    }
                } else {
                    colors = overrides;
                }
            }

            colors ??= new();
            colors.FinalizeData();

            // DynLeaderboardsPlugin.LogInfo($"Read text box colors from '{fileName}': {colors.Debug(pretty: true)}");

            return colors;
        }

        internal bool IsFirstFinished { get; private set; } = false;
        private bool _startingPositionsSet = false;

        internal Values() {
            this._carInfos = ReadCarInfos();
            this._carClassColors = ReadTextBoxColors<CarClass>("CarClassColors.json");
            this._teamCupCategoryColors = ReadTextBoxColors<TeamCupCategory>("TeamCupCategoryColors.json");
            this._driverCategoryColors = ReadTextBoxColors<DriverCategory>("DriverCategoryColors.json");

            this.OverallOrder = this._overallOrder.AsReadOnly();
            this.ClassOrder = this._classOrder.AsReadOnly();
            this.CupOrder = this._cupOrder.AsReadOnly();
            this.RelativeOnTrackAheadOrder = this._relativeOnTrackAheadOrder.AsReadOnly();
            this.RelativeOnTrackBehindOrder = this._relativeOnTrackBehindOrder.AsReadOnly();

            this.Reset();
        }

        internal void UpdateCarInfos() {
            this._carInfos = ReadCarInfos();
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
                    DynLeaderboardsPlugin.LogInfo("Disposed");
                }

                this._isDisposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        #endregion IDisposable Support
        internal void OnDataUpdate(PluginManager _, GameData data) {
            this.Session.OnDataUpdate(data);

            if (this.Booleans.NewData.IsNewEvent || this.Session.IsNewSession || this.TrackData?.PrettyName != data.NewData.TrackName) {
                DynLeaderboardsPlugin.LogInfo($"newEvent={this.Booleans.NewData.IsNewEvent}, newSession={this.Session.IsNewSession}");
                this.ResetWithoutSession();
                this.Booleans.OnNewEvent(this.Session.SessionType);
                this.TrackData = new TrackData(data);

                DynLeaderboardsPlugin.LogInfo($"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}, len={this.TrackData.LengthMeters}");
            }

            if (this.TrackData != null && this.TrackData.LengthMeters == 0) {
                // In ACC sometimes the track length is not immediately available, and is 0.
                this.TrackData?.SetLength(data);
            }

            this.Booleans.OnDataUpdate(data, this);

            this.UpdateCars(data);
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
                if (DynLeaderboardsPlugin.Game.IsAcc && opponent.Id == "Me") {
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
                    car = new CarData(this, focusedCarId, opponent);
                    this._overallOrder.Add(car);
                } else {
                    Debug.Assert(car.Id == opponent.Id);
                    car.UpdateIndependent(this, focusedCarId, opponent);

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
                    DynLeaderboardsPlugin.LogInfo($"Removed disconnected car {car.Id}, #{car.CarNumber}");
                }
            }

            if (!this._startingPositionsSet && this.Session.IsRace && this._overallOrder.Count != 0) {
                this.SetStartingOrder();
            }
            this.SetOverallOrder();

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

        private void SetOverallOrder() {
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

    [TypeConverter(typeof(TextBoxColorTypeConverter))]
    public class TextBoxColor {
        public string Fg { get; }
        public string Bg { get; }

        [JsonConstructor]
        public TextBoxColor(string fg, string bg) {
            this.Fg = fg;
            this.Bg = bg;
        }

        public static TextBoxColor? TryNew(string? fg, string? bg) {
            if (fg == null || bg == null) {
                return null;
            }
            return new TextBoxColor(fg: fg, bg: bg);
        }
    }

    internal class TextBoxColorTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            // simhub is a special case, it means to fall back to simhub colors
            if (value is string str && str.Equals("simhub", StringComparison.OrdinalIgnoreCase)) {
                return new TextBoxColor(fg: "simhub", bg: "simhub");
            }
            throw new NotSupportedException();
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return false;
        }

        public override object? ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            throw new NotImplementedException();
        }
    }

    internal class TextBoxColors<K> {
        private Dictionary<K, TextBoxColor> _colors { get; set; }

        [JsonConstructor]
        internal TextBoxColors(Dictionary<K, TextBoxColor>? global, Dictionary<string, Dictionary<K, TextBoxColor>>? game_overrides) {
            this._colors = global ?? [];
            var overrides = game_overrides?.GetValueOr(DynLeaderboardsPlugin.Game.Name, null);
            if (overrides != null) {
                foreach (var kvp in overrides) {
                    this._colors[kvp.Key] = kvp.Value;
                }
            }
        }

        internal TextBoxColors() {
            this._colors = [];
        }

        internal TextBoxColor? Get(K key) {
            return this._colors.GetValueOr(key, null);
        }

        internal void FinalizeData() {
            this._colors = this._colors
                .Where(kvp => kvp.Value.Bg != "simhub" && kvp.Value.Fg != "simhub")
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);
        }

        internal void Merge(TextBoxColors<K> other) {
            this._colors.Merge(other._colors);
        }

        internal string Debug(bool pretty = false) {
            if (pretty) {
                return JsonConvert.SerializeObject(this._colors, Formatting.Indented);
            } else {
                return JsonConvert.SerializeObject(this._colors);
            }
        }

        // We cannot easily implement IEnumerable because it messes up deserialization from JSON.
        // However for our purposes this is enough.
        internal IEnumerable<KeyValuePair<K, TextBoxColor>> GetEnumerable() {
            return this._colors;
        }

    }
}