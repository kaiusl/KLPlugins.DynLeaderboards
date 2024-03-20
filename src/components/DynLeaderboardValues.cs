using System.Collections.Generic;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;

using KLPlugins.DynLeaderboards.Settings;

namespace KLPlugins.DynLeaderboards {

    public class DynLeaderboard {
        public delegate CarData? GetDynCarDelegate(int i);
        public delegate double? DynGapDelegate(int i);
        public delegate double? DynLapDeltaDelegate(int i);
        public delegate int? DynPositionDelegate(int i);

        public GetDynCarDelegate GetDynCar { get; private set; }
        public DynGapDelegate GetDynGapToFocused { get; private set; }
        public DynGapDelegate GetDynGapToAhead { get; private set; }
        public DynLapDeltaDelegate GetDynBestLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedLast { get; private set; }

        public DynPositionDelegate GetDynPosition { get; private set; }
        public DynPositionDelegate GetDynPositionStart { get; private set; }

        public DynLeaderboardConfig Config { get; private set; }

        public List<CarData?> Cars { get; } = new();
        public int FocusedIndex = -1;


        internal DynLeaderboard(DynLeaderboardConfig config, Values v) {
            this.Config = config;
            this.SetDynGetters(v);
        }

        internal void OnDataUpdate(Values v) {
            this.SetCars(v);
        }

        internal void OnLeaderboardChange(Values v) {
            this.SetDynGetters(v);
        }

        private void SetDynGettersDefault() {
            this.GetDynCar = (i) => this.Cars.ElementAtOrDefault(i);
            this.GetDynGapToFocused = (i) => double.NaN;
            this.GetDynGapToAhead = (i) => double.NaN;
            this.GetDynBestLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedLast = (i) => double.NaN;
            this.GetDynPosition = (i) => null;
            this.GetDynPositionStart = (i) => null;
        }

        private void SetDynGetters(Values v) {
            switch (this.Config.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    this.GetDynCar = (i) => v.OverallOrder.ElementAtOrDefault(i);
                    // this.GetDynGapToFocused = (i) => v.GetCar(i)?.GapToLeader;
                    // this.GetDynGapToAhead = (i) => v.GetCar(i)?.GapToAhead;
                    // this.GetDynBestLapDeltaToFocusedBest = (i) => v.GetCar(i)?.BestLapDeltaToLeaderBest;
                    // this.GetDynLastLapDeltaToFocusedBest = (i) => v.GetCar(i)?.LastLapDeltaToLeaderBest;
                    // this.GetDynLastLapDeltaToFocusedLast = (i) => v.GetCar(i)?.LastLapDeltaToLeaderLast;
                    // this.GetDynPosition = (i) => v.GetCar(i)?.OverallPos;
                    // this.GetDynPositionStart = (i) => v.GetCar(i)?.StartPos;
                    break;

                case Leaderboard.Class:
                    this.GetDynCar = (i) => v.ClassOrder.ElementAtOrDefault(i);
                    // if (v.PosInClassCarsIdxs == null) {
                    //     DynLeaderboardsPlugin.LogError("Cannot calculate class positions.");
                    //     this.SetDynGettersDefault();
                    //     break;
                    // }
                    // this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToClassLeader;
                    // this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    // this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToClassLeaderBest;
                    // this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToClassLeaderBest;
                    // this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToClassLeaderLast;
                    // this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                    // this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                    break;

                // case Leaderboard.Cup:
                //     if (v.PosInCupCarsIdxs == null) {
                //         DynLeaderboardsPlugin.LogError("Cannot calculate cup positions.");
                //         this.SetDynGettersDefault();
                //         break;
                //     }
                //     this.GetDynCar = (i) => v.GetCar(i, v.PosInCupCarsIdxs);
                //     this.GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarPosInCupCarsIdxs;
                //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToCupLeader;
                //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToCupLeaderBest;
                //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToCupLeaderBest;
                //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToCupLeaderLast;
                //     this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                //     break;

                // case Leaderboard.RelativeOverall:
                //     //this._relativeOverallCarsIdxs ??= new int?[this.Settings.NumOverallRelativePos * 2 + 1];

                //     // this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                //     // this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                //     // this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     // this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     // this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     // this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                //     // this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                //     break;

                // case Leaderboard.RelativeClass:
                //     //     this._relativeClassCarsIdxs ??= new int?[this.Settings.NumClassRelativePos * 2 + 1];

                //     //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                //     //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                //     //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     //     this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                //     //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                //     break;

                // // case Leaderboard.RelativeCup:
                // //     this._relativeCupCarsIdxs ??= new int?[this.Settings.NumCupRelativePos * 2 + 1];

                // //     this.GetDynCar = (i) => v.GetCar(i, this._relativeCupCarsIdxs);
                // //     this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumCupRelativePos;
                // //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                // //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                // //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                // //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                // //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                // //     this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                // //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                // //     break;

                // case Leaderboard.PartialRelativeOverall:
                //     //     this._partialRelativeOverallCarsIdxs ??= new int?[
                //     //this.Settings.PartialRelativeOverallNumOverallPos + this.Settings.PartialRelativeOverallNumRelativePos * 2 + 1];
                //     //     this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeOverallCarsIdxs;
                //     //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                //     //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                //     //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     //     this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                //     //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                //     break;

                // case Leaderboard.PartialRelativeClass:
                //     this._partialRelativeClassCarsIdxs ??= new int?[this.Settings.PartialRelativeClassNumClassPos + this.Settings.PartialRelativeClassNumRelativePos * 2 + 1];

                //     this.GetDynCar = (i) => v.GetCar(i, this._partialRelativeClassCarsIdxs);
                //     this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeClassCarsIdxs;
                //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                //     break;

                // case Leaderboard.PartialRelativeCup:
                //     this._partialRelativeCupCarsIdxs ??= new int?[this.Settings.PartialRelativeCupNumCupPos + this.Settings.PartialRelativeCupNumRelativePos * 2 + 1];

                //     this.GetDynCar = (i) => v.GetCar(i, this._partialRelativeCupCarsIdxs);
                //     this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeCupCarsIdxs;
                //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                //     break;

                // case Leaderboard.RelativeOnTrack:
                //     this._relativePosOnTrackCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

                //     this.GetDynCar = (i) => v.GetCar(i, this._relativePosOnTrackCarsIdxs);
                //     this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumOnTrackRelativePos;
                //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedOnTrack;
                //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadOnTrack;
                //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                //     break;

                // case Leaderboard.RelativeOnTrackWoPit:
                //     this._relativePosOnTrackWoPitCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

                //     this.GetDynCar = (i) => v.GetCar(i, this._relativePosOnTrackWoPitCarsIdxs);
                //     this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumOnTrackRelativePos;
                //     this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedOnTrack;
                //     this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadOnTrack;
                //     this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                //     this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                //     this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                //     this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                //     break;

                default:
                    this.SetDynGettersDefault();
                    break;
            }
        }


        private void SetCars(Values v) {
            if (v.FocusedCar == null) return;

            this.Cars.Clear();
            switch (this.Config.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    this.FocusedIndex = v.FocusedCar.IndexOverall;
                    break;
                case Leaderboard.Class:
                    this.FocusedIndex = v.FocusedCar.IndexClass;
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
                case Leaderboard.RelativeOnTrack: {
                        var relPos = this.Config.NumOnTrackRelativePos;

                        if (v.RelativeOnTrackAheadOrder.Count < relPos) {
                            for (int i = 0; i < relPos - v.RelativeOnTrackAheadOrder.Count; i++) {
                                this.Cars.Add(null);
                            }
                        }

                        foreach (var car in v.RelativeOnTrackAheadOrder.Take(relPos)) {
                            this.Cars.Add(car);
                        }

                        this.Cars.Add(v.FocusedCar);
                        this.FocusedIndex = relPos;

                        foreach (var car in v.RelativeOnTrackBehindOrder.Take(relPos)) {
                            this.Cars.Add(car);
                        }
                    }
                    break;

                case Leaderboard.RelativeOnTrackWoPit: {
                        var relPos = this.Config.NumOnTrackRelativePos;

                        var aheadCars = v.RelativeOnTrackAheadOrder
                            .Where(c => !c.IsInPitLane)
                            .Take(relPos);
                        var aheadCount = aheadCars.Count();

                        if (aheadCount < relPos) {
                            for (int i = 0; i < relPos - aheadCount; i++) {
                                this.Cars.Add(null);
                            }
                        }

                        foreach (var car in aheadCars) {
                            this.Cars.Add(car);
                        }

                        this.Cars.Add(v.FocusedCar);
                        this.FocusedIndex = relPos;

                        var behindCars = v.RelativeOnTrackBehindOrder
                            .Where(c => !c.IsInPitLane)
                            .Take(relPos);
                        foreach (var car in behindCars) {
                            this.Cars.Add(car);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void SetCarsRelativeX(int numRelPos, List<CarData> cars, int focusedCarIndexInCars) {
            this.FocusedIndex = numRelPos;
            int start = focusedCarIndexInCars - numRelPos;
            int end = start + numRelPos * 2 + 1;

            int i = start;
            for (; i < 0; i++) {
                this.Cars.Add(null);
            }
            for (; i < end; i++) {
                this.Cars.Add(cars.ElementAtOrDefault(i));
            }
        }

        private void SetCarsPartialRelativeX(int numTopPos, int numRelPos, List<CarData> cars, int focusedCarIndexInCars) {
            // Top positions are always added
            for (int i = 0; i < numTopPos; i++) {
                var car = cars.ElementAtOrDefault(i);
                this.Cars.Add(car);
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
                this.Cars.Add(car);
                if (car != null && car.IsFocused) {
                    this.FocusedIndex = this.Cars.Count - 1;
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
    }
}