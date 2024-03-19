using System;
using System.Collections.Generic;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Track;
using KLPlugins.DynLeaderboards.Car;

using SimHub.Plugins;
using System.Linq;

namespace KLPlugins.DynLeaderboards {
    /// <summary>
    /// Storage and calculation of new properties
    /// </summary>
    public class Values : IDisposable {
        public TrackData? TrackData { get; private set; }
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

            // TODO: don't create new CarData each time, update previous objects
            this.OverallOrder.Clear();
            string? playerClass = null;
            var cars = data.NewData.Opponents.Select((c, i) => (new CarData(c), i));
            foreach (var (car, i) in cars) {
                this.OverallOrder.Add(car);
                car.SetOverallPosition(i + 1);
                if (car.IsFocused) {
                    this.FocusedCar = car;
                }
            }

            this.ClassOrder.Clear();
            var classCars = this.OverallOrder
                .Where(c => c.CarClass == playerClass)
                .Select((c, i) => (c, i));
            foreach (var (car, i) in classCars) {
                this.ClassOrder.Add(car);
                car.SetClassPosition(i + 1);
            }

            // Some parts of the update require that basic data on every car has been updated
            if (this.FocusedCar != null) {
                this.RelativeOnTrackAheadOrder.Clear();
                this.RelativeOnTrackBehindOrder.Clear();
                foreach (var car in this.OverallOrder) {
                    car.UpdateDependsOnOthers(this.FocusedCar);
                    if (car.IsFocused) {
                        // nothing to do
                    } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                        this.RelativeOnTrackAheadOrder.Add(car);
                    } else {
                        this.RelativeOnTrackBehindOrder.Add(car);
                    }
                }

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