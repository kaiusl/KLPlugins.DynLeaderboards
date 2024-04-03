using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using GameReaderCommon;

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
        public int MaxPositions => this.Config.MaxPositions();
        public Leaderboard CurrentLeaderboard => this.Config.CurrentLeaderboard();

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
            switch (this.Config.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    this.GetDynCar = (i) => v.OverallOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionOverall;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionOverallStart;
                    break;

                case Leaderboard.Class:
                    this.GetDynCar = (i) => v.ClassOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToClassLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInClass;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInClassStart;
                    break;

                case Leaderboard.Cup:
                    this.GetDynCar = (i) => v.CupOrder.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToCupLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInCup;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInCupStart;
                    break;

                case Leaderboard.RelativeOverall:
                case Leaderboard.PartialRelativeOverall:
                    this.GetDynCar = i => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionOverall;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionOverallStart;
                    break;

                case Leaderboard.RelativeClass:
                case Leaderboard.PartialRelativeClass:
                    this.GetDynCar = i => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInClass;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInClassStart;
                    break;

                case Leaderboard.RelativeCup:
                case Leaderboard.PartialRelativeCup:
                    this.GetDynCar = (i) => this._cars.ElementAtOrDefault(i);
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLap?.DeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.PositionInCup;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.PositionInCupStart;
                    break;

                case Leaderboard.RelativeOnTrack:
                case Leaderboard.RelativeOnTrackWoPit:
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
            switch (this.Config.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    this.FocusedIndex = v.FocusedCar.IndexOverall;
                    break;
                case Leaderboard.Class:
                    this.FocusedIndex = v.FocusedCar.IndexClass;
                    break;
                case Leaderboard.Cup:
                    this.FocusedIndex = v.FocusedCar.IndexCup;
                    break;
                case Leaderboard.RelativeOverall:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumOverallRelativePos,
                        cars: v.OverallOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexOverall
                    );
                    break;
                case Leaderboard.RelativeClass:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumClassRelativePos,
                        cars: v.ClassOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexClass
                    );
                    break;
                case Leaderboard.RelativeCup:
                    this.SetCarsRelativeX(
                        numRelPos: this.Config.NumCupRelativePos,
                        cars: v.CupOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexCup
                    );
                    break;
                case Leaderboard.PartialRelativeOverall:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeOverallNumOverallPos,
                        numRelPos: this.Config.PartialRelativeOverallNumRelativePos,
                        cars: v.OverallOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexOverall
                    );
                    break;
                case Leaderboard.PartialRelativeClass:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeClassNumClassPos,
                        numRelPos: this.Config.PartialRelativeClassNumRelativePos,
                        cars: v.ClassOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexClass
                    );
                    break;
                case Leaderboard.PartialRelativeCup:
                    this.SetCarsPartialRelativeX(
                        numTopPos: this.Config.PartialRelativeCupNumCupPos,
                        numRelPos: this.Config.PartialRelativeCupNumRelativePos,
                        cars: v.CupOrder,
                        focusedCarIndexInCars: v.FocusedCar.IndexCup
                    );
                    break;
                case Leaderboard.RelativeOnTrack: {
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

                case Leaderboard.RelativeOnTrackWoPit: {
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

        private bool DynLeaderboardContainsAny(params Leaderboard[] leaderboards) {
            foreach (var v in leaderboards) {
                if (this.Config.Order.Contains(v)) {
                    return true;
                }
            }
            return false;
        }

        internal void NextLeaderboard(Values values) {
            if (this.Config.CurrentLeaderboardIdx == this.Config.Order.Count - 1) {
                this.Config.CurrentLeaderboardIdx = 0;
            } else {
                this.Config.CurrentLeaderboardIdx++;
            }
            this.OnLeaderboardChange(values);
        }

        internal void PreviousLeaderboard(Values values) {
            if (this.Config.CurrentLeaderboardIdx == 0) {
                this.Config.CurrentLeaderboardIdx = this.Config.Order.Count - 1;
            } else {
                this.Config.CurrentLeaderboardIdx--;
            }
            this.OnLeaderboardChange(values);
        }

        private void OnLeaderboardChange(Values v) {
            DynLeaderboardsPlugin.LogInfo($"OnLeaderboardChange [{this.Config.Name}]: {this.Config.CurrentLeaderboard()}");
            this.SetDynGetters(v);
        }
    }
}