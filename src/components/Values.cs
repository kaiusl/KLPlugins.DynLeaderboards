using System;
using System.Collections.Generic;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Track;
using KLPlugins.DynLeaderboards.Car;

using SimHub.Plugins;
using System.Linq;
using KLPlugins.DynLeaderboards.Helpers;
using System.Diagnostics;

namespace KLPlugins.DynLeaderboards {
    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public TrackData? TrackData { get; private set; }
        public Session Session { get; private set; } = new();
        public Booleans Booleans { get; private set; } = new();

        public List<CarData> OverallOrder { get; } = new();
        public List<CarData> ClassOrder { get; } = new();
        public List<CarData> RelativeOnTrackAheadOrder { get; } = new();
        public List<CarData> RelativeOnTrackBehindOrder { get; } = new();
        public CarData? FocusedCar { get; private set; } = null;

        internal Values() {

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
            if (this.Booleans.NewData.IsNewEvent) {
                this.Booleans.OnNewEvent(this.Session.SessionType);
                // TODO: reset other data
            }

            this.Booleans.OnDataUpdate(data, this);
            this.Session.OnDataUpdate(data, this);
            this.UpdateCars(data);
        }

        private void UpdateCars(GameData data) {
            IEnumerable<(Opponent, int)> cars;
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                // In ACC the first car is "Me", which is weird ghost car that doesn't have any info
                cars = data.NewData.Opponents.Skip(1).WithIndex();
            } else {
                cars = data.NewData.Opponents.WithIndex();
            }

            foreach (var (opponent, i) in cars) {
                // Most common case is that the car's position hasn't changed
                var car = this.OverallOrder.ElementAtOrDefault(i);
                if (car == null || car.Id != opponent.Id) {
                    car = this.OverallOrder.Find(c => c.Id == opponent.Id);
                }

                if (car == null) {
                    car = new CarData(opponent);
                    this.OverallOrder.Add(car);
                } else {
                    Debug.Assert(car.Id == opponent.Id);
                    car.UpdateIndependent(opponent);
                }

                car.IsUpdated = true;

                if (car.IsFocused) {
                    this.FocusedCar = car;
                }
            }

            // TODO: this is temporary, use completely custom sort later
            this.OverallOrder.Sort((c1, c2) => {
                var isUpdated = c2.IsUpdated.CompareTo(c1.IsUpdated);
                // put cars that didn't receive update to the back
                if (isUpdated != 0) {
                    return isUpdated;
                } else {
                    return c1.RawDataNew.Position.CompareTo(c2.RawDataNew.Position); ;
                }
            });

            // Remove cars that didn't receive update
            var numNotUpdated = this.OverallOrder.AsEnumerable().Reverse().FirstIndex(c => c.IsUpdated);
            if (numNotUpdated > 0) {
                this.OverallOrder.RemoveRange(this.OverallOrder.Count - numNotUpdated, numNotUpdated);
            }

            this.ClassOrder.Clear();
            this.RelativeOnTrackAheadOrder.Clear();
            this.RelativeOnTrackBehindOrder.Clear();
            Dictionary<string, int> classPositions = [];
            var focusedClass = this.FocusedCar?.CarClass;
            foreach (var (car, i) in this.OverallOrder.WithIndex()) {
                car.SetOverallPosition(i + 1);

                if (car.CarClass == null) {
                    DynLeaderboardsPlugin.LogError($"Car {car.Id} has no class");
                    continue;
                }

                if (!classPositions.ContainsKey(car.CarClass)) {
                    classPositions.Add(car.CarClass, 1);
                }
                car.SetClassPosition(classPositions[car.CarClass]++);

                if (this.FocusedCar != null) {
                    car.UpdateDependsOnOthers(this.FocusedCar);
                    if (car.CarClass == focusedClass) {
                        this.ClassOrder.Add(car);
                    }

                    if (car.IsFocused) {
                        // nothing to do
                    } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                        this.RelativeOnTrackAheadOrder.Add(car);
                    } else {
                        this.RelativeOnTrackBehindOrder.Add(car);
                    }
                }
            }

            if (this.FocusedCar != null) {
                static int CmpCarByRelativeSplinePositionToFocusedCar(CarData c1, CarData c2) {
                    return c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar);
                }
                this.RelativeOnTrackAheadOrder.Sort(CmpCarByRelativeSplinePositionToFocusedCar);
                this.RelativeOnTrackBehindOrder.Sort(CmpCarByRelativeSplinePositionToFocusedCar);
            }
        }

        internal void OnGameStateChanged(bool running, PluginManager _) {
            if (running) {
            } else {
                //this.Reset();
            }
        }

    }
}