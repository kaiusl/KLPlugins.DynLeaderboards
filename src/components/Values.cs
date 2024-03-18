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
        public List<CarData> OverallOrder { get; } = new();
        public List<CarData> ClassOrder { get; } = new();
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
                foreach (var car in this.OverallOrder) {
                    car.UpdateDependsOnOthers(this.FocusedCar);
                }
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