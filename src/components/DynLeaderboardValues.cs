using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Settings;

namespace KLPlugins.DynLeaderboards {

    public class DynLeaderboard {
        public delegate CarData? GetDynCarDelegate(int i);
        public delegate TimeSpan? DynGapDelegate(int i);
        public delegate TimeSpan? DynLapDeltaDelegate(int i);
        public delegate int? DynPositionDelegate(int i);

        public GetDynCarDelegate GetDynCar { get; private set; }
        public DynGapDelegate GetDynGapToFocused { get; private set; }
        public DynGapDelegate GetDynGapToAhead { get; private set; }
        public DynLapDeltaDelegate GetDynBestLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedLast { get; private set; }

        public DynPositionDelegate GetDynPosition { get; private set; }
        public DynPositionDelegate GetDynPositionStart { get; private set; }

        public string Name => this.Config.Name;
        public string CurrentLeaderboardName => this.Config.CurrentLeaderboardName;
        public string NextLeaderboardActionNAme => this.Config.NextLeaderboardActionName;
        public string PreviousLeaderboardActionNAme => this.Config.PreviousLeaderboardActionName;
        public int MaxPositions => this.Config.MaxPositions();
        public LeaderboardKind CurrentLeaderboard => this.Config.CurrentLeaderboard().Kind;

        internal DynLeaderboardConfig Config { get; set; }

        /// <summary>
        /// List of cars for this dynamic leaderboard in the order they are displayed.
        /// </summary>
        public ReadOnlyCollection<CarData?> Cars { get; }
        private List<CarData?> _cars { get; } = new();
        /// <summary>
        /// Focused car's index in <c>this.Cars</c>.
        /// </summary>
        public int FocusedIndex = -1;

        internal DynLeaderboard(DynLeaderboardConfig config, Values v) {
            this.Config = config;
            this.SetDynGetters(v);
            this.Cars = this._cars.AsReadOnly();
        }

        internal void OnDataUpdate(Values v) {
            this.SetCars(v);
        }

        private void SetDynGettersDefault() {
            this.GetDynCar = (i) => this._cars.ElementAtOrDefault(i);
            this.GetDynGapToFocused = (i) => null;
            this.GetDynGapToAhead = (i) => null;
            this.GetDynBestLapDeltaToFocusedBest = (i) => null;
            this.GetDynLastLapDeltaToFocusedBest = (i) => null;
            this.GetDynLastLapDeltaToFocusedLast = (i) => null;
            this.GetDynPosition = (i) => null;
            this.GetDynPositionStart = (i) => null;
        }

        private void SetDynGetters(Values v) {
            switch (this.Config.CurrentLeaderboard().Kind) {
                case LeaderboardKind.Overall:
                    this.GetDynCar = (i) => v.OverallOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionOverall;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionOverallStart;
                    break;

                case LeaderboardKind.Class:
                    this.GetDynCar = (i) => v.ClassOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToClassLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInClass;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInClassStart;
                    break;

                case LeaderboardKind.Cup:
                    this.GetDynCar = (i) => v.CupOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToCupLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInCup;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInCupStart;
                    break;

                case LeaderboardKind.RelativeOverall:
                case LeaderboardKind.PartialRelativeOverall:
                    this.GetDynCar = i => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionOverall;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionOverallStart;
                    break;

                case LeaderboardKind.RelativeClass:
                case LeaderboardKind.PartialRelativeClass:
                    this.GetDynCar = i => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInClass;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInClassStart;
                    break;

                case LeaderboardKind.RelativeCup:
                case LeaderboardKind.PartialRelativeCup:
                    this.GetDynCar = (i) => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInCup;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInCupStart;
                    break;

                case LeaderboardKind.RelativeOnTrack:
                case LeaderboardKind.RelativeOnTrackWoPit:
                    this.GetDynCar = i => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedOnTrack;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadOnTrack;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionOverall;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionOverallStart;
                    break;

                default:
                    this.SetDynGettersDefault();
                    break;
            }
        }

        private void SetCars(Values v) {
            if (v.FocusedCar == null) return;

            this._cars.Clear();
            switch (this.Config.CurrentLeaderboard().Kind) {
                case LeaderboardKind.Overall:
                    this.FocusedIndex = v.FocusedCar.IndexOverall;
                    break;
                case LeaderboardKind.Class:
                    this.FocusedIndex = v.FocusedCar.IndexClass;
                    break;
                case LeaderboardKind.Cup:
                    this.FocusedIndex = v.FocusedCar.IndexCup;
                    break;
                case LeaderboardKind.RelativeOverall:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumOverallRelativePos,
                        cars: v.OverallOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexOverall
                    );
                    break;
                case LeaderboardKind.RelativeClass:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumClassRelativePos,
                        cars: v.ClassOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexClass
                    );
                    break;
                case LeaderboardKind.RelativeCup:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumCupRelativePos,
                        cars: v.CupOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexCup
                    );
                    break;
                case LeaderboardKind.PartialRelativeOverall:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeOverallNumOverallPos,
                        numRelPos: this.Config.PartialRelativeOverallNumRelativePos,
                        cars: v.OverallOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexOverall
                    );
                    break;
                case LeaderboardKind.PartialRelativeClass:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeClassNumClassPos,
                        numRelPos: this.Config.PartialRelativeClassNumRelativePos,
                        cars: v.ClassOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexClass
                    );
                    break;
                case LeaderboardKind.PartialRelativeCup:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeCupNumCupPos,
                        numRelPos: this.Config.PartialRelativeCupNumRelativePos,
                        cars: v.CupOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexCup
                    );
                    break;
                case LeaderboardKind.RelativeOnTrack: {
                        var relPos = this.Config.NumOnTrackRelativePos;

                        if (v.RelativeOnTrackAheadOrder.Count < relPos) {
                            for (int i = 0; i < relPos - v.RelativeOnTrackAheadOrder.Count; i++) {
                                this._cars.Add(null);
                            }
                        }

                        foreach (var car in v.RelativeOnTrackAheadOrder.Take(relPos).Reverse()) {
                            this._cars.Add(car);
                        }

                        this._cars.Add(v.FocusedCar);
                        this.FocusedIndex = relPos;

                        foreach (var car in v.RelativeOnTrackBehindOrder.Take(relPos)) {
                            this._cars.Add(car);
                        }
                    }
                    break;

                case LeaderboardKind.RelativeOnTrackWoPit: {
                        var relPos = this.Config.NumOnTrackRelativePos;

                        var aheadCars = v.RelativeOnTrackAheadOrder
                            .Where(c => !c.IsInPitLane)
                            .Take(relPos)
                            .Reverse();
                        var aheadCount = aheadCars.Count();

                        if (aheadCount < relPos) {
                            for (int i = 0; i < relPos - aheadCount; i++) {
                                this._cars.Add(null);
                            }
                        }

                        foreach (var car in aheadCars) {
                            this._cars.Add(car);
                        }

                        this._cars.Add(v.FocusedCar);
                        this.FocusedIndex = relPos;

                        var behindCars = v.RelativeOnTrackBehindOrder
                            .Where(c => !c.IsInPitLane)
                            .Take(relPos);
                        foreach (var car in behindCars) {
                            this._cars.Add(car);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void SetCarsRelativeX(int numRelPos, ReadOnlyCollection<CarData> cars, int focusedCarIndexInCars) {
            this.FocusedIndex = numRelPos;
            int start = focusedCarIndexInCars - numRelPos;
            int end = start + numRelPos * 2 + 1;

            int i = start;
            for (; i < 0; i++) {
                this._cars.Add(null);
            }
            for (; i < end; i++) {
                this._cars.Add(cars.ElementAtOrDefault(i));
            }
        }

        private void SetCarsPartialRelativeX(int numTopPos, int numRelPos, ReadOnlyCollection<CarData> cars, int focusedCarIndexInCars) {
            // Top positions are always added
            for (int i = 0; i < numTopPos; i++) {
                var car = cars.ElementAtOrDefault(i);
                this._cars.Add(car);
                if (car != null && car.IsFocused) {
                    this.FocusedIndex = i;
                }
            }

            // Calculate relative part start and end
            var start = focusedCarIndexInCars - numTopPos;
            var end = start + numRelPos * 2 + 1;

            // if start reaches into the top positions, shift it down so it doesn't overlap
            if (start <= numTopPos) {
                var diff = numTopPos - start;
                start += diff;
                end += diff;
            }

            for (int i = start; i < end; i++) {
                var car = cars.ElementAtOrDefault(i);
                this._cars.Add(car);
                if (car != null && car.IsFocused) {
                    this.FocusedIndex = this._cars.Count - 1;
                }
            }
        }

        internal void NextLeaderboard(Values values) {
            var isSingleClass = values.NumClassesInSession < 2;
            var isSingleCup = values.NumCupsInSession == values.NumClassesInSession;
            // For loop so that we don't go into an infinite loop if all leaderboards should be excluded
            for (int i = 0; i < this.Config.Order.Count; i++) {
                if (this.Config.CurrentLeaderboardIdx == this.Config.Order.Count - 1) {
                    this.Config.CurrentLeaderboardIdx = 0;
                } else {
                    this.Config.CurrentLeaderboardIdx++;
                }

                var currentLeaderboard = this.Config.CurrentLeaderboard();
                if (
                    (isSingleClass && currentLeaderboard.RemoveIfSingleClass)
                    || (isSingleCup && currentLeaderboard.RemoveIfSingleCup)
                ) {
                    continue;
                } else {
                    break;
                }
            }

            this.OnLeaderboardChange(values);
        }

        internal void PreviousLeaderboard(Values values) {
            var isSingleClass = values.NumClassesInSession < 2;
            var isSingleCup = values.NumCupsInSession == values.NumClassesInSession;
            // For loop so that we don't go into an infinite loop if all leaderboards should be excluded
            for (int i = 0; i < this.Config.Order.Count; i++) {
                if (this.Config.CurrentLeaderboardIdx == 0) {
                    this.Config.CurrentLeaderboardIdx = this.Config.Order.Count - 1;
                } else {
                    this.Config.CurrentLeaderboardIdx--;
                }

                var currentLeaderboard = this.Config.CurrentLeaderboard();
                if (
                    (isSingleClass && currentLeaderboard.RemoveIfSingleClass)
                    || (isSingleCup && currentLeaderboard.RemoveIfSingleCup)
                ) {
                    continue;
                } else {
                    break;
                }
            }
            this.OnLeaderboardChange(values);
        }

        private void OnLeaderboardChange(Values v) {
            DynLeaderboardsPlugin.LogInfo($"OnLeaderboardChange [{this.Config.Name}]: {this.Config.CurrentLeaderboard().Kind}");
            this.SetDynGetters(v);
        }
    }
}