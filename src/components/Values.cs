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
            }

            this.SetOverallOrder();

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

            if (!this.IsFirstFinished && this.OverallOrder.Count > 0 && this.Session.SessionType == SessionType.Race) {
                var first = this.OverallOrder.First();
                if (this.Session.IsLapLimited) {
                    this.IsFirstFinished = first.Laps == data.NewData.TotalLaps;
                } else if (this.Session.IsTimeLimited) {
                    this.IsFirstFinished = data.NewData.SessionTimeLeft.TotalSeconds <= 0 && first.IsNewLap;
                }
            }

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

                if (!classPositions.ContainsKey(car.CarClass)) {
                    classPositions.Add(car.CarClass, 1);
                }
                car.SetClassPosition(classPositions[car.CarClass]++);


                car.UpdateDependsOnOthers(this, this.FocusedCar);
                if (focusedClass != null && car.CarClass == focusedClass) {
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

            if (this.FocusedCar != null) {
                static int CmpCarByRelativeSplinePositionToFocusedCar(CarData c1, CarData c2) {
                    return c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar);
                }
                this.RelativeOnTrackAheadOrder.Sort(CmpCarByRelativeSplinePositionToFocusedCar);
                this.RelativeOnTrackBehindOrder.Sort(CmpCarByRelativeSplinePositionToFocusedCar);
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

                    // // Sort cars that have crossed the start line always in front of cars who haven't
                    // if (a.HasCrossedStartLine && !b.HasCrossedStartLine) {
                    //     return -1;
                    // } else if (b.HasCrossedStartLine && !a.HasCrossedStartLine) {
                    //     return 1;
                    // }

                    // Always compare by laps first
                    var alaps = a.Laps;
                    var blaps = b.Laps;
                    if (alaps != blaps) {
                        return blaps.CompareTo(alaps);
                    }

                    // // Keep order if one of the cars has offset lap update, could cause jumping otherwise
                    // if (a.OffsetLapUpdate != 0 || b.OffsetLapUpdate != 0) {
                    //     return a.OverallPos.CompareTo(b.OverallPos);
                    // }

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

                    // Keep order, make sort stable, fixes jumping
                    if (a.TotalSplinePosition == b.TotalSplinePosition) {
                        return a.PositionOverall.CompareTo(b.PositionOverall);
                    }
                    return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
                };

                this.OverallOrder.Sort(cmp);
            } else {
                // In other sessions TotalSplinePosition doesn't make any sense, use RealtimeCarUpdate.Position
                int cmp(CarData a, CarData b) {
                    if (a == b) {
                        return 0;
                    }

                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                this.OverallOrder.Sort(cmp);
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