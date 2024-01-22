using System.Collections.Generic;
using System.Linq;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Settings;

namespace KLPlugins.DynLeaderboards {

    internal class DynLeaderboardValues {

        public delegate CarData? GetDynCarDelegate(int i);
        public delegate int? GetFocusedCarIdxInLDynLeaderboardDelegate();
        public delegate double? DynGapDelegate(int i);
        public delegate double? DynLapDeltaDelegate(int i);
        public delegate int? DynPositionDelegate(int i);

        public GetDynCarDelegate GetDynCar { get; private set; }
        public GetFocusedCarIdxInLDynLeaderboardDelegate GetFocusedCarIdxInDynLeaderboard { get; private set; }
        public DynGapDelegate GetDynGapToFocused { get; private set; }
        public DynGapDelegate GetDynGapToAhead { get; private set; }
        public DynLapDeltaDelegate GetDynBestLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedBest { get; private set; }
        public DynLapDeltaDelegate GetDynLastLapDeltaToFocusedLast { get; private set; }

        public DynPositionDelegate GetDynPosition { get; private set; }
        public DynPositionDelegate GetDynPositionStart { get; private set; }

        public DynLeaderboardConfig Settings { get; private set; }

        private int?[]? _relativePosOnTrackCarsIdxs { get; set; }
        private int?[]? _relativePosOnTrackWoPitCarsIdxs { get; set; }
        private int?[]? _relativeOverallCarsIdxs { get; set; }
        private int?[]? _relativeClassCarsIdxs { get; set; }
        private int?[]? _relativeCupCarsIdxs { get; set; }
        private int?[]? _partialRelativeOverallCarsIdxs { get; set; }
        private int? _focusedCarPosInPartialRelativeOverallCarsIdxs { get; set; }
        private int?[]? _partialRelativeClassCarsIdxs { get; set; }
        private int?[]? _partialRelativeCupCarsIdxs { get; set; }
        private int? _focusedCarPosInPartialRelativeClassCarsIdxs { get; set; }
        private int? _focusedCarPosInPartialRelativeCupCarsIdxs { get; set; }

        internal DynLeaderboardValues(DynLeaderboardConfig settings) {
            this.Settings = settings;

            if (this.DynLeaderboardContainsAny(Leaderboard.RelativeOnTrack)) {
                this._relativePosOnTrackCarsIdxs = new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];
                this._relativePosOnTrackWoPitCarsIdxs = new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.RelativeOverall)) {
                this._relativeOverallCarsIdxs = new int?[this.Settings.NumOverallRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.PartialRelativeOverall)) {
                this._partialRelativeOverallCarsIdxs = new int?[this.Settings.PartialRelativeOverallNumOverallPos + this.Settings.PartialRelativeOverallNumRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.RelativeClass)) {
                this._relativeClassCarsIdxs = new int?[this.Settings.NumClassRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.RelativeCup)) {
                this._relativeCupCarsIdxs = new int?[this.Settings.NumCupRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.PartialRelativeClass)) {
                this._partialRelativeClassCarsIdxs = new int?[this.Settings.PartialRelativeClassNumClassPos + this.Settings.PartialRelativeClassNumRelativePos * 2 + 1];
            }

            if (this.DynLeaderboardContainsAny(Leaderboard.PartialRelativeCup)) {
                this._partialRelativeCupCarsIdxs = new int?[this.Settings.PartialRelativeCupNumCupPos + this.Settings.PartialRelativeCupNumRelativePos * 2 + 1];
            }

            this.GetDynCar = (i) => null;
            this.GetFocusedCarIdxInDynLeaderboard = () => null;
            this.GetDynGapToFocused = (i) => double.NaN;
            this.GetDynGapToAhead = (i) => double.NaN;
            this.GetDynBestLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedLast = (i) => double.NaN;
            this.GetDynPosition = (i) => null;
            this.GetDynPositionStart = (i) => null;
        }

        internal void ResetPos() {
            ResetIdxs(this._relativeClassCarsIdxs);
            ResetIdxs(this._relativeOverallCarsIdxs);
            ResetIdxs(this._relativePosOnTrackCarsIdxs);
            ResetIdxs(this._relativePosOnTrackWoPitCarsIdxs);
            ResetIdxs(this._partialRelativeClassCarsIdxs);
            ResetIdxs(this._partialRelativeOverallCarsIdxs);

            static void ResetIdxs(int?[]? arr) {
                if (arr == null) {
                    return;
                }

                for (int i = 0; i < arr.Length; i++) {
                    arr[i] = null;
                }
            }
        }

        internal void SetDynGetters(Values v) {
            switch (this.Settings.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    this.GetDynCar = (i) => v.GetCar(i);
                    this.GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarIdx;
                    this.GetDynGapToFocused = (i) => v.GetCar(i)?.GapToLeader;
                    this.GetDynGapToAhead = (i) => v.GetCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => v.GetCar(i)?.BestLapDeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => v.GetCar(i)?.LastLapDeltaToLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => v.GetCar(i)?.LastLapDeltaToLeaderLast;
                    this.GetDynPosition = (i) => v.GetCar(i)?.OverallPos;
                    this.GetDynPositionStart = (i) => v.GetCar(i)?.StartPos;
                    break;

                case Leaderboard.Class:
                    if (v.PosInClassCarsIdxs == null) {
                        DynLeaderboardsPlugin.LogError("Cannot calculate class positions.");
                        this.SetDynGettersDefault();
                        break;
                    }
                    this.GetDynCar = (i) => v.GetCar(i, v.PosInClassCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarPosInClassCarsIdxs;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToClassLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToClassLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToClassLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                    break;

                case Leaderboard.Cup:
                    if (v.PosInCupCarsIdxs == null) {
                        DynLeaderboardsPlugin.LogError("Cannot calculate cup positions.");
                        this.SetDynGettersDefault();
                        break;
                    }
                    this.GetDynCar = (i) => v.GetCar(i, v.PosInCupCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarPosInCupCarsIdxs;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToCupLeader;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToCupLeaderBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToCupLeaderLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                    break;

                case Leaderboard.RelativeOverall:
                    this._relativeOverallCarsIdxs ??= new int?[this.Settings.NumOverallRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._relativeOverallCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumOverallRelativePos;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                    break;

                case Leaderboard.RelativeClass:
                    this._relativeClassCarsIdxs ??= new int?[this.Settings.NumClassRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._relativeClassCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumClassRelativePos;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                    break;

                case Leaderboard.RelativeCup:
                    this._relativeCupCarsIdxs ??= new int?[this.Settings.NumCupRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._relativeCupCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumCupRelativePos;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                    break;

                case Leaderboard.PartialRelativeOverall:
                    this._partialRelativeOverallCarsIdxs ??= new int?[this.Settings.PartialRelativeOverallNumOverallPos + this.Settings.PartialRelativeOverallNumRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._partialRelativeOverallCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeOverallCarsIdxs;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAhead;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                    break;

                case Leaderboard.PartialRelativeClass:
                    this._partialRelativeClassCarsIdxs ??= new int?[this.Settings.PartialRelativeClassNumClassPos + this.Settings.PartialRelativeClassNumRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._partialRelativeClassCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeClassCarsIdxs;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInClass;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InClassPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInClass;
                    break;

                case Leaderboard.PartialRelativeCup:
                    this._partialRelativeCupCarsIdxs ??= new int?[this.Settings.PartialRelativeCupNumCupPos + this.Settings.PartialRelativeCupNumRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._partialRelativeCupCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this._focusedCarPosInPartialRelativeCupCarsIdxs;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedTotal;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadInCup;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.InCupPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPosInCup;
                    break;

                case Leaderboard.RelativeOnTrack:
                    this._relativePosOnTrackCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._relativePosOnTrackCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumOnTrackRelativePos;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedOnTrack;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadOnTrack;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                    break;

                case Leaderboard.RelativeOnTrackWoPit:
                    this._relativePosOnTrackWoPitCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

                    this.GetDynCar = (i) => v.GetCar(i, this._relativePosOnTrackWoPitCarsIdxs);
                    this.GetFocusedCarIdxInDynLeaderboard = () => this.Settings.NumOnTrackRelativePos;
                    this.GetDynGapToFocused = (i) => this.GetDynCar(i)?.GapToFocusedOnTrack;
                    this.GetDynGapToAhead = (i) => this.GetDynCar(i)?.GapToAheadOnTrack;
                    this.GetDynBestLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedBest = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    this.GetDynLastLapDeltaToFocusedLast = (i) => this.GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    this.GetDynPosition = (i) => this.GetDynCar(i)?.OverallPos;
                    this.GetDynPositionStart = (i) => this.GetDynCar(i)?.StartPos;
                    break;

                default:
                    this.SetDynGettersDefault();
                    break;
            }
        }

        void SetDynGettersDefault() {
            this.GetDynCar = (i) => null;
            this.GetFocusedCarIdxInDynLeaderboard = () => null;
            this.GetDynGapToFocused = (i) => double.NaN;
            this.GetDynGapToAhead = (i) => double.NaN;
            this.GetDynBestLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedBest = (i) => double.NaN;
            this.GetDynLastLapDeltaToFocusedLast = (i) => double.NaN;
            this.GetDynPosition = (i) => null;
            this.GetDynPositionStart = (i) => null;
        }

        internal void SetRelativeOnTrackOrder(List<CarSplinePos> _relativeSplinePositions, int focusedCarIdx) {
            this._relativePosOnTrackCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

            var relPos = this.Settings.NumOnTrackRelativePos;
            var ahead = _relativeSplinePositions
                .Where(x => x.SplinePos > 0)
                .Take(relPos)
                .Reverse()
                .ToList()
                .ConvertAll(x => (int?)x.CarIdx);
            var behind = _relativeSplinePositions
                .Where(x => x.SplinePos < 0)
                .Reverse()
                .Take(relPos)
                .ToList()
                .ConvertAll(x => (int?)x.CarIdx);

            ahead.CopyTo(this._relativePosOnTrackCarsIdxs, relPos - ahead.Count);
            this._relativePosOnTrackCarsIdxs[relPos] = focusedCarIdx;
            behind.CopyTo(this._relativePosOnTrackCarsIdxs, relPos + 1);

            // Set missing positions to -1
            var startidx = relPos - ahead.Count;
            var endidx = relPos + behind.Count + 1;
            for (int i = 0; i < relPos * 2 + 1; i++) {
                if (i < startidx || i >= endidx) {
                    this._relativePosOnTrackCarsIdxs[i] = null;
                }
            }
        }

        internal void SetRelativeOnTrackWoPitOrder(List<CarSplinePos> _relativeSplinePositions, int focusedCarIdx, List<CarData> cars, bool isRace) {
            this._relativePosOnTrackWoPitCarsIdxs ??= new int?[this.Settings.NumOnTrackRelativePos * 2 + 1];

            var relPos = this.Settings.NumOnTrackRelativePos;
            var ahead = _relativeSplinePositions
                .Where(x => {
                    var car = cars[x.CarIdx];
                    var isInPit = car.NewData?.IsInPitlane ?? true;
                    var showInPits = isRace && car.RelativeOnTrackLapDiff == 0;
                    return x.SplinePos > 0 && (!isInPit || showInPits);
                })
                .Take(relPos)
                .Reverse()
                .ToList()
                .ConvertAll(x => (int?)x.CarIdx);
            var behind = _relativeSplinePositions
                .Where(x => {
                    var car = cars[x.CarIdx];
                    var isInPit = car.NewData?.IsInPitlane ?? true;
                    var showInPits = isRace && car.RelativeOnTrackLapDiff == 0;
                    return x.SplinePos < 0 && (!isInPit || showInPits);
                })
                .Reverse()
                .Take(relPos)
                .ToList()
                .ConvertAll(x => (int?)x.CarIdx);

            ahead.CopyTo(this._relativePosOnTrackWoPitCarsIdxs, relPos - ahead.Count);
            this._relativePosOnTrackWoPitCarsIdxs[relPos] = focusedCarIdx;
            behind.CopyTo(this._relativePosOnTrackWoPitCarsIdxs, relPos + 1);

            // Set missing positions to -1
            var startidx = relPos - ahead.Count;
            var endidx = relPos + behind.Count + 1;
            for (int i = 0; i < relPos * 2 + 1; i++) {
                if (i < startidx || i >= endidx) {
                    this._relativePosOnTrackWoPitCarsIdxs[i] = null;
                }
            }
        }

        internal void SetRelativeOverallOrder(int focusedCarIdx, List<CarData> cars) {
            this._relativeOverallCarsIdxs ??= new int?[this.Settings.NumOverallRelativePos * 2 + 1];

            var numRelPos = this.Settings.NumOverallRelativePos;
            for (int i = 0; i < numRelPos * 2 + 1; i++) {
                var idxInCars = focusedCarIdx - numRelPos + i;
                this._relativeOverallCarsIdxs[i] = idxInCars < cars.Count && idxInCars >= 0 ? (int?)idxInCars : null;
            }
        }

        internal void SetPartialRelativeOverallOrder(int FocusedCarIdx, List<CarData> Cars) {
            this._partialRelativeOverallCarsIdxs ??= new int?[this.Settings.PartialRelativeOverallNumOverallPos + this.Settings.PartialRelativeOverallNumRelativePos * 2 + 1];

            var numOverallPos = this.Settings.PartialRelativeOverallNumOverallPos;
            var numRelPos = this.Settings.PartialRelativeOverallNumRelativePos;

            // TODO: Try to clean this mess up
            this._focusedCarPosInPartialRelativeOverallCarsIdxs = null;
            for (int i = 0; i < numOverallPos + numRelPos * 2 + 1; i++) {
                int? idxInCars = i;
                if (i > numOverallPos - 1 && FocusedCarIdx > numOverallPos + numRelPos) {
                    idxInCars += FocusedCarIdx - numOverallPos - numRelPos;
                }
                this._partialRelativeOverallCarsIdxs[i] = idxInCars < Cars.Count ? idxInCars : null;
                if (idxInCars == FocusedCarIdx) {
                    this._focusedCarPosInPartialRelativeOverallCarsIdxs = i;
                }
            }
        }

        internal void SetRelativeClassOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInClassCarsIdxs) {
            this._relativeClassCarsIdxs ??= new int?[this.Settings.NumClassRelativePos * 2 + 1];

            for (int i = 0; i < this.Settings.NumClassRelativePos * 2 + 1; i++) {
                int? idx = Cars[(int)FocusedCarIdx].InClassPos - this.Settings.NumClassRelativePos + i - 1;
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                this._relativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
            }
        }

        internal void SetPartialRelativeClassOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInClassCarsIdxs) {
            this._partialRelativeClassCarsIdxs ??= new int?[this.Settings.PartialRelativeClassNumClassPos + this.Settings.PartialRelativeClassNumRelativePos * 2 + 1];

            var overallPos = this.Settings.PartialRelativeClassNumClassPos;
            var relPos = this.Settings.PartialRelativeClassNumRelativePos;

            // TODO: Try to clean this mess up
            for (int i = 0; i < overallPos + relPos * 2 + 1; i++) {
                int? idx = i;
                var focusedClassPos = Cars[(int)FocusedCarIdx].InClassPos - 1;
                if (i > overallPos - 1 && focusedClassPos > overallPos + relPos) {
                    idx += focusedClassPos - overallPos - relPos;
                }
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                this._partialRelativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
                if (idx == FocusedCarIdx) {
                    this._focusedCarPosInPartialRelativeClassCarsIdxs = i;
                }
            }
        }

        internal void SetRelativeCupOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInCupCarsIdxs) {
            this._relativeCupCarsIdxs ??= new int?[this.Settings.NumCupRelativePos * 2 + 1];

            for (int i = 0; i < this.Settings.NumCupRelativePos * 2 + 1; i++) {
                int? idx = Cars[(int)FocusedCarIdx].InCupPos - this.Settings.NumCupRelativePos + i - 1;
                idx = PosInCupCarsIdxs.ElementAtOrDefault((int)idx);
                this._relativeCupCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
            }
        }

        internal void SetPartialRelativeCupOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInCupCarsIdxs) {
            this._partialRelativeCupCarsIdxs ??= new int?[this.Settings.PartialRelativeCupNumCupPos + this.Settings.PartialRelativeCupNumRelativePos * 2 + 1];

            var overallPos = this.Settings.PartialRelativeCupNumCupPos;
            var relPos = this.Settings.PartialRelativeCupNumRelativePos;

            // TODO: Try to clean this mess up
            for (int i = 0; i < overallPos + relPos * 2 + 1; i++) {
                int? idx = i;
                var focusedCupPos = Cars[(int)FocusedCarIdx].InCupPos - 1;
                if (i > overallPos - 1 && focusedCupPos > overallPos + relPos) {
                    idx += focusedCupPos - overallPos - relPos;
                }
                idx = PosInCupCarsIdxs.ElementAtOrDefault((int)idx);
                this._partialRelativeCupCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
                if (idx == FocusedCarIdx) {
                    this._focusedCarPosInPartialRelativeCupCarsIdxs = i;
                }
            }
        }

        private bool DynLeaderboardContainsAny(params Leaderboard[] leaderboards) {
            foreach (var v in leaderboards) {
                if (this.Settings.Order.Contains(v)) {
                    return true;
                }
            }
            return false;
        }
    }
}