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

        public bool IsFirstFinished { get; private set; } = false;

        internal Values() {
        }

        internal void Reset() {
            DynLeaderboardsPlugin.LogInfo($"Values.Reset()");
            this.Session.Reset();
            this.ResetWithoutSession();
        }

        internal void ResetWithoutSession() {
            DynLeaderboardsPlugin.LogInfo($"Values.ResetWithoutSession()");
            this.Booleans.Reset();
            this.OverallOrder.Clear();
            this.ClassOrder.Clear();
            this.RelativeOnTrackAheadOrder.Clear();
            this.RelativeOnTrackBehindOrder.Clear();
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
            }

            this.Booleans.OnDataUpdate(data, this);

            this.UpdateCars(data);
        }

        private void UpdateCars(GameData data) {
            IEnumerable<(Opponent, int)> cars = data.NewData.Opponents.WithIndex();

            Dictionary<string, CarData> classBestLapCars = [];
            CarData? overallBestLapCar = null;
            foreach (var (opponent, i) in cars) {
                if (DynLeaderboardsPlugin.Game.IsAcc && opponent.Id == "Me") {
                    continue;
                }

                // Most common case is that the car's position hasn't changed
                var car = this.OverallOrder.ElementAtOrDefault(i);
                if (car == null || car.Id != opponent.Id) {
                    car = this.OverallOrder.Find(c => c.Id == opponent.Id);
                }

                if (car == null) {
                    car = new CarData(this, opponent);
                    this.OverallOrder.Add(car);
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
            var numNotUpdated = this.OverallOrder
                .AsEnumerable()
                .Reverse()
                .FirstIndex(c => c.IsUpdated || c.IsFinished);
            if (numNotUpdated > 0) {
                this.OverallOrder.RemoveRange(this.OverallOrder.Count - numNotUpdated, numNotUpdated);
            }

            if (!this.IsFirstFinished && this.OverallOrder.Count > 0 && this.Session.SessionType == SessionType.Race) {
                var first = this.OverallOrder.First();
                if (this.Session.IsLapLimited) {
                    this.IsFirstFinished = first.Laps.New == data.NewData.TotalLaps;
                } else if (this.Session.IsTimeLimited) {
                    this.IsFirstFinished = data.NewData.SessionTimeLeft.TotalSeconds <= 0 && first.IsNewLap;
                }

                if (this.IsFirstFinished) {
                    DynLeaderboardsPlugin.LogInfo($"First finished: id={this.OverallOrder.First().Id}");
                }
            }

            this.ClassOrder.Clear();
            this.RelativeOnTrackAheadOrder.Clear();
            this.RelativeOnTrackBehindOrder.Clear();
            Dictionary<string, int> classPositions = [];
            Dictionary<string, CarData> classLeaders = [];
            Dictionary<string, CarData> carAheadInClass = [];
            var focusedClass = this.FocusedCar?.CarClass;
            foreach (var (car, i) in this.OverallOrder.WithIndex()) {
                car.SetOverallPosition(i + 1);

                if (!classPositions.ContainsKey(car.CarClass)) {
                    classPositions.Add(car.CarClass, 1);
                    classLeaders.Add(car.CarClass, car);
                }
                car.SetClassPosition(classPositions[car.CarClass]++);
                if (focusedClass != null && car.CarClass == focusedClass) {
                    this.ClassOrder.Add(car);
                }

                car.UpdateDependsOnOthers(
                    values: this,
                    overallBestLapCar: overallBestLapCar,
                    classBestLapCar: classBestLapCars.GetValueOr(car.CarClass, null),
                    cupBestLapCar: null, // TODO
                    leaderCar: this.OverallOrder.First(), // If we get there, there must be at least on car
                    classLeaderCar: classLeaders[car.CarClass], // If we get there, the leader must be present
                    cupLeaderCar: null, // TODO: store all cup leader cars
                    focusedCar: this.FocusedCar,
                    carAhead: this.FocusedCar != null ? this.OverallOrder.ElementAtOrDefault(this.FocusedCar.IndexOverall - 1) : null,
                    carAheadInClass: carAheadInClass.GetValueOr(car.CarClass, null),
                    carAheadInCup: null // TODO: store car ahead in each cup
                );

                if (car.IsFocused) {
                    // nothing to do
                } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                    this.RelativeOnTrackAheadOrder.Add(car);
                } else {
                    this.RelativeOnTrackBehindOrder.Add(car);
                }

                carAheadInClass[car.CarClass] = car;
            }

            if (this.FocusedCar != null) {
                this.RelativeOnTrackAheadOrder.Sort((c1, c2) => c1.RelativeSplinePositionToFocusedCar.CompareTo(c2.RelativeSplinePositionToFocusedCar));
                this.RelativeOnTrackBehindOrder.Sort((c1, c2) => c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar));
            }
        }

        void SetOverallOrder() {
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
                            var aFTime = a.FinishTime == null ? long.MinValue : (long)a.FinishTime!;
                            var bFTime = b.FinishTime == null ? long.MinValue : (long)b.FinishTime!;
                            return aFTime.CompareTo(bFTime);
                        } else {
                            // Both cars have finished
                            var aFTime = a.FinishTime == null ? long.MaxValue : (long)a.FinishTime;
                            var bFTime = b.FinishTime == null ? long.MaxValue : (long)b.FinishTime;
                            return aFTime.CompareTo(bFTime);
                        }
                    }

                    // Keep order, make sort stable, fixes jumping, essentially keeps the cars in previous order
                    if (a.TotalSplinePosition == b.TotalSplinePosition) {
                        return a.PositionOverall.CompareTo(b.PositionOverall);
                    }
                    return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
                };

                this.OverallOrder.Sort(cmp);
            } else {
                // In other sessions TotalSplinePosition doesn't make any sense, use Position
                int cmp(CarData a, CarData b) {
                    if (a == b) {
                        return 0;
                    }

                    // Need to use RawDataNew.Position because the CarData.PositionOverall is updated based of the result of this sort
                    return a.RawDataNew.Position.CompareTo(b.RawDataNew.Position);
                }

                this.OverallOrder.Sort(cmp);
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