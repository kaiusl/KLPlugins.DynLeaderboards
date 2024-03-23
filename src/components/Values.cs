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

namespace KLPlugins.DynLeaderboards {
    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public TrackData? TrackData { get; private set; }
        public Session Session { get; private set; } = new();
        public Booleans Booleans { get; private set; } = new();

        public ReadOnlyCollection<CarData> OverallOrder { get; }
        public ReadOnlyCollection<CarData> ClassOrder { get; }
        public ReadOnlyCollection<CarData> RelativeOnTrackAheadOrder { get; }
        public ReadOnlyCollection<CarData> RelativeOnTrackBehindOrder { get; }
        private List<CarData> _overallOrder { get; } = new();
        private List<CarData> _classOrder { get; } = new();
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
            return _carInfos.GetValueOr(carName, null);
        }

        private static Dictionary<string, CarInfo> ReadCarInfos() {
            var pathEnd = $"\\{DynLeaderboardsPlugin.Game.Name}\\CarInfos.json";
            var basePath = PluginSettings.PluginDataDirBase + pathEnd;
            var overridesPath = PluginSettings.PluginDataDirOverrides + pathEnd;

            Dictionary<string, CarInfo>? carInfos = null;
            var baseExists = File.Exists(basePath);
            if (DynLeaderboardsPlugin.Game.IsAc && !baseExists) {
                DynLeaderboardsPlugin.UpdateACCarInfos();
            }

            if (File.Exists(basePath)) {
                carInfos = JsonConvert.DeserializeObject<Dictionary<string, CarInfo>>(File.ReadAllText(basePath));
            }

            if (File.Exists(overridesPath)) {
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, CarInfo>>(File.ReadAllText(overridesPath));
                if (carInfos != null) {
                    if (overrides != null) {
                        foreach (var kvp in overrides) {
                            if (carInfos.ContainsKey(kvp.Key)) {
                                carInfos[kvp.Key].Merge(kvp.Value);
                            } else {
                                carInfos.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                } else {
                    carInfos = overrides;
                }
            }

            return carInfos ?? [];
        }

        private readonly TextBoxColors<CarClass> _carClassColors;
        internal TextBoxColor? GetCarClassColor(CarClass carClass) {
            return _carClassColors.Get(carClass);
        }
        internal IEnumerable<KeyValuePair<CarClass, TextBoxColor>> CarClassColors => _carClassColors.GetEnumerable();


        private readonly TextBoxColors<TeamCupCategory> _teamCupCategoryColors;
        internal TextBoxColor? GetTeamCupCategoryColor(TeamCupCategory teamCupCategory) {
            return _teamCupCategoryColors.Get(teamCupCategory);
        }
        internal IEnumerable<KeyValuePair<TeamCupCategory, TextBoxColor>> TeamCupCategoryColors => _teamCupCategoryColors.GetEnumerable();


        private readonly TextBoxColors<DriverCategory> _driverCategoryColors;
        internal TextBoxColor? GetDriverCategoryColor(DriverCategory teamCupCategory) {
            return _driverCategoryColors.Get(teamCupCategory);
        }
        internal IEnumerable<KeyValuePair<DriverCategory, TextBoxColor>> DriverCategoryColors => _driverCategoryColors.GetEnumerable();

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

            DynLeaderboardsPlugin.LogInfo($"Read text box colors from '{fileName}': {colors?.Debug(pretty: true)}");

            return colors ?? new();
        }

        internal bool IsFirstFinished { get; private set; } = false;

        internal Values() {
            _carInfos = ReadCarInfos();
            _carClassColors = ReadTextBoxColors<CarClass>("CarClassColors.json");
            _teamCupCategoryColors = ReadTextBoxColors<TeamCupCategory>("TeamCupCategoryColors.json");
            _driverCategoryColors = ReadTextBoxColors<DriverCategory>("DriverCategoryColors.json");

            this.OverallOrder = this._overallOrder.AsReadOnly();
            this.ClassOrder = this._classOrder.AsReadOnly();
            this.RelativeOnTrackAheadOrder = this._relativeOnTrackAheadOrder.AsReadOnly();
            this.RelativeOnTrackBehindOrder = this._relativeOnTrackBehindOrder.AsReadOnly();
        }

        internal void UpdateCarInfos() {
            _carInfos = ReadCarInfos();
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
            this._relativeOnTrackAheadOrder.Clear();
            this._relativeOnTrackBehindOrder.Clear();
            this.FocusedCar = null;
            this.IsFirstFinished = false;
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

            if (this.Booleans.NewData.IsNewEvent || this.Session.IsNewSession) {
                DynLeaderboardsPlugin.LogInfo($"newEvent={this.Booleans.NewData.IsNewEvent}, newSession={this.Session.IsNewSession}");
                this.ResetWithoutSession();
                this.Booleans.OnNewEvent(this.Session.SessionType);
                this.TrackData = new TrackData(data);

                DynLeaderboardsPlugin.LogInfo($"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}");
            }

            this.Booleans.OnDataUpdate(data, this);

            this.UpdateCars(data);
        }

        private void UpdateCars(GameData data) {
            IEnumerable<(Opponent, int)> cars = data.NewData.Opponents.WithIndex();

            Dictionary<CarClass, CarData> classBestLapCars = [];
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
                    car = new CarData(this, opponent);
                    this._overallOrder.Add(car);
                } else {
                    Debug.Assert(car.Id == opponent.Id);
                    car.UpdateIndependent(this, opponent);
                }

                car.IsUpdated = true;

                if (car.IsFocused) {
                    this.FocusedCar = car;
                }


                if (car.BestLap?.Time != null) {
                    if (!classBestLapCars.ContainsKey(car.CarClass)) {
                        classBestLapCars[car.CarClass] = car;
                    } else {
                        var currentClassBestLap = classBestLapCars[car.CarClass].BestLap!.Time!; // If it's in the dict, it cannot be null

                        if (car.BestLap.Time < currentClassBestLap) {
                            classBestLapCars[car.CarClass] = car;
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

            this.SetOverallOrder();

            // Remove cars that didn't receive update.
            // It's OK not to receive an update if one has already finished.
            // this.SetOverallOrder sorts such cars to the end of the list.
            var numNotUpdated = this._overallOrder
                .AsEnumerable()
                .Reverse()
                .FirstIndex(c => c.IsUpdated || c.IsFinished);
            if (numNotUpdated > 0) {
                this._overallOrder.RemoveRange(this._overallOrder.Count - numNotUpdated, numNotUpdated);
            }

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
            this._relativeOnTrackAheadOrder.Clear();
            this._relativeOnTrackBehindOrder.Clear();
            Dictionary<CarClass, int> classPositions = [];
            Dictionary<CarClass, CarData> classLeaders = [];
            Dictionary<CarClass, CarData> carAheadInClass = [];
            var focusedClass = this.FocusedCar?.CarClass;
            foreach (var (car, i) in this._overallOrder.WithIndex()) {
                car.SetOverallPosition(i + 1);

                if (!classPositions.ContainsKey(car.CarClass)) {
                    classPositions.Add(car.CarClass, 1);
                    classLeaders.Add(car.CarClass, car);
                }
                car.SetClassPosition(classPositions[car.CarClass]++);
                if (focusedClass != null && car.CarClass == focusedClass) {
                    this._classOrder.Add(car);
                }

                car.UpdateDependsOnOthers(
                    values: this,
                    overallBestLapCar: overallBestLapCar,
                    classBestLapCar: classBestLapCars.GetValueOr(car.CarClass, null),
                    cupBestLapCar: null, // TODO
                    leaderCar: this._overallOrder.First(), // If we get there, there must be at least on car
                    classLeaderCar: classLeaders[car.CarClass], // If we get there, the leader must be present
                    cupLeaderCar: null, // TODO: store all cup leader cars
                    focusedCar: this.FocusedCar,
                    carAhead: this.FocusedCar != null ? this._overallOrder.ElementAtOrDefault(this.FocusedCar.IndexOverall - 1) : null,
                    carAheadInClass: carAheadInClass.GetValueOr(car.CarClass, null),
                    carAheadInCup: null // TODO: store car ahead in each cup
                );

                if (car.IsFocused) {
                    // nothing to do
                } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                    this._relativeOnTrackAheadOrder.Add(car);
                } else {
                    this._relativeOnTrackBehindOrder.Add(car);
                }

                carAheadInClass[car.CarClass] = car;
            }

            if (this.FocusedCar != null) {
                this._relativeOnTrackAheadOrder.Sort((c1, c2) => c1.RelativeSplinePositionToFocusedCar.CompareTo(c2.RelativeSplinePositionToFocusedCar));
                this._relativeOnTrackBehindOrder.Sort((c1, c2) => c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar));
            }
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

                    // Sort cars that didn't receive an update to the end, so we can easily remove them.
                    // It's OK for car to not receive an update after they have finished,
                    // it means they left but we want to keep them around
                    if (!a.IsUpdated && !a.IsFinished && (b.IsUpdated || b.IsFinished)) {
                        return 1;
                    } else if (!b.IsUpdated && !b.IsFinished && (a.IsUpdated || a.IsFinished)) {
                        return -1;
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

                    // Need to use RawDataNew.Position because the CarData.PositionOverall is updated based of the result of this sort
                    return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
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

    internal class TextBoxColors<K> {
        private Dictionary<K, TextBoxColor> _colors { get; }

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