using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Settings;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KLPlugins.DynLeaderboards {
    class DynLeaderboardValues {
        public delegate CarData GetDynCarDelegate(int i);
        public delegate int? GetFocusedCarIdxInLDynLeaderboardDelegate();
        public delegate double? DynGapToFocusedDelegate(int i);
        public delegate double? DynGapToAheadDelegate(int i);
        public delegate double? DynBestLapDeltaToFocusedBestDelegate(int i);
        public delegate double? DynLastLapDeltaToFocusedBestDelegate(int i);
        public delegate double? DynLastLapDeltaToFocusedLastDelegate(int i);

        public GetDynCarDelegate GetDynCar { get; private set; }
        public GetFocusedCarIdxInLDynLeaderboardDelegate GetFocusedCarIdxInDynLeaderboard { get; private set; }
        public DynGapToFocusedDelegate GetDynGapToFocused { get; private set; }
        public DynGapToAheadDelegate GetDynGapToAhead { get; private set; }
        public DynBestLapDeltaToFocusedBestDelegate GetDynBestLapDeltaToFocusedBest { get; private set; }
        public DynLastLapDeltaToFocusedBestDelegate GetDynLastLapDeltaToFocusedBest { get; private set; }
        public DynLastLapDeltaToFocusedLastDelegate GetDynLastLapDeltaToFocusedLast { get; private set; }

        public DynLeaderboardConfig Settings { get; private set; }

        private int?[] _relativePosOnTrackCarsIdxs { get; set; }
        private int?[] _relativeOverallCarsIdxs { get; set; }
        private int?[] _relativeClassCarsIdxs { get; set; }
        private int?[] _partialRelativeOverallCarsIdxs { get; set; }
        private int? _focusedCarPosInPartialRelativeOverallCarsIdxs { get; set; }
        private int?[] _partialRelativeClassCarsIdxs { get; set; }
        private int? _focusedCarPosInPartialRelativeClassCarsIdxs { get; set; }

        internal DynLeaderboardValues(DynLeaderboardConfig settings) {
            Settings = settings;

            if (DynLeaderboardContainsAny(Leaderboard.RelativeOnTrack))
                _relativePosOnTrackCarsIdxs = new int?[Settings.NumOnTrackRelativePos * 2 + 1];

            if (DynLeaderboardContainsAny(Leaderboard.RelativeOverall))
                _relativeOverallCarsIdxs = new int?[Settings.NumOverallRelativePos * 2 + 1];

            if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeOverall))
                _partialRelativeOverallCarsIdxs = new int?[Settings.PartialRelativeOverallNumOverallPos + Settings.PartialRelativeOverallNumRelativePos * 2 + 1];

            if (DynLeaderboardContainsAny(Leaderboard.RelativeClass))
                _relativeClassCarsIdxs = new int?[Settings.NumClassRelativePos * 2 + 1];

            if (DynLeaderboardContainsAny(Leaderboard.PartialRelativeClass))
                _partialRelativeClassCarsIdxs = new int?[Settings.PartialRelativeClassNumClassPos + Settings.PartialRelativeClassNumRelativePos * 2 + 1];
        }

        internal void ResetPos() {
            ResetIdxs(_relativeClassCarsIdxs);
            ResetIdxs(_relativeOverallCarsIdxs);
            ResetIdxs(_relativePosOnTrackCarsIdxs);
            ResetIdxs(_partialRelativeClassCarsIdxs);
            ResetIdxs(_partialRelativeOverallCarsIdxs);

            void ResetIdxs(int?[] arr) {
                if (arr == null) return;
                for (int i = 0; i < arr.Length; i++) {
                    arr[i] = null;
                }
            }
        }

        internal void SetDynGetters(Values v) {
            switch (Settings.CurrentLeaderboard()) {
                case Leaderboard.Overall:
                    GetDynCar = (i) => v.GetCar(i);
                    GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarIdx;
                    GetDynGapToFocused = (i) => v.GetCar(i)?.GapToLeader;
                    GetDynGapToAhead = (i) => v.GetCar(i)?.GapToAhead;
                    GetDynBestLapDeltaToFocusedBest = (i) => v.GetCar(i)?.BestLapDeltaToLeaderBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => v.GetCar(i)?.LastLapDeltaToLeaderBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => v.GetCar(i)?.LastLapDeltaToLeaderLast;
                    break;

                case Leaderboard.Class:
                    GetDynCar = (i) => v.GetCar(i, v.PosInClassCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => v.FocusedCarPosInClassCarsIdxs;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToClassLeader;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAheadInClass;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToClassLeaderBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToClassLeaderBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToClassLeaderLast;
                    break;

                case Leaderboard.RelativeOverall:
                    if (_relativeOverallCarsIdxs == null) {
                        _relativeOverallCarsIdxs = new int?[Settings.NumOverallRelativePos * 2 + 1];
                    }

                    GetDynCar = (i) => v.GetCar(i, _relativeOverallCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => Settings.NumOverallRelativePos;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToFocusedTotal;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAhead;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;

                case Leaderboard.RelativeClass:
                    if (_relativeClassCarsIdxs == null) {
                        _relativeClassCarsIdxs = new int?[Settings.NumClassRelativePos * 2 + 1];
                    }

                    GetDynCar = (i) => v.GetCar(i, _relativeClassCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => Settings.NumClassRelativePos;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToFocusedTotal;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAheadInClass;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;

                case Leaderboard.PartialRelativeOverall:
                    if (_partialRelativeOverallCarsIdxs == null) {
                        _partialRelativeOverallCarsIdxs = new int?[Settings.PartialRelativeOverallNumOverallPos + Settings.PartialRelativeOverallNumRelativePos * 2 + 1];
                    }

                    GetDynCar = (i) => v.GetCar(i, _partialRelativeOverallCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => _focusedCarPosInPartialRelativeOverallCarsIdxs;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToFocusedTotal;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAhead;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;

                case Leaderboard.PartialRelativeClass:
                    if (_partialRelativeClassCarsIdxs == null) {
                        _partialRelativeClassCarsIdxs = new int?[Settings.PartialRelativeClassNumClassPos + Settings.PartialRelativeClassNumRelativePos * 2 + 1];
                    }

                    GetDynCar = (i) => v.GetCar(i, _partialRelativeClassCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => _focusedCarPosInPartialRelativeClassCarsIdxs;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToFocusedTotal;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAheadInClass;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;

                case Leaderboard.RelativeOnTrack:
                    if (_relativePosOnTrackCarsIdxs == null) {
                        _relativePosOnTrackCarsIdxs = new int?[Settings.NumOnTrackRelativePos * 2 + 1];
                    }
                    GetDynCar = (i) => v.GetCar(i, _relativePosOnTrackCarsIdxs);
                    GetFocusedCarIdxInDynLeaderboard = () => Settings.NumOnTrackRelativePos;
                    GetDynGapToFocused = (i) => GetDynCar(i)?.GapToFocusedOnTrack;
                    GetDynGapToAhead = (i) => GetDynCar(i)?.GapToAheadOnTrack;
                    GetDynBestLapDeltaToFocusedBest = (i) => GetDynCar(i)?.BestLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedBest = (i) => GetDynCar(i)?.LastLapDeltaToFocusedBest;
                    GetDynLastLapDeltaToFocusedLast = (i) => GetDynCar(i)?.LastLapDeltaToFocusedLast;
                    break;

                default:
                    SetDefault();
                    break;
            }

            void SetDefault() {
                GetDynCar = (i) => null;
                GetFocusedCarIdxInDynLeaderboard = () => null;
                GetDynGapToFocused = (i) => double.NaN;
                GetDynGapToAhead = (i) => double.NaN;
                GetDynBestLapDeltaToFocusedBest = (i) => double.NaN;
                GetDynLastLapDeltaToFocusedBest = (i) => double.NaN;
                GetDynLastLapDeltaToFocusedLast = (i) => double.NaN;
            }

        }

        internal void SetRelativeOnTrackOrder(List<CarSplinePos> _relativeSplinePositions, int focusedCarIdx) {
            Debug.Assert(_relativePosOnTrackCarsIdxs != null);

            var relPos = Settings.NumOnTrackRelativePos;
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


            ahead.CopyTo(_relativePosOnTrackCarsIdxs, relPos - ahead.Count);
            _relativePosOnTrackCarsIdxs[relPos] = focusedCarIdx;
            behind.CopyTo(_relativePosOnTrackCarsIdxs, relPos + 1);

            // Set missing positions to -1
            var startidx = relPos - ahead.Count;
            var endidx = relPos + behind.Count + 1;
            for (int i = 0; i < relPos * 2 + 1; i++) {
                if (i < startidx || i >= endidx) {
                    _relativePosOnTrackCarsIdxs[i] = null;
                }
            }
        }

        internal void SetRelativeOverallOrder(int focusedCarIdx, List<CarData> cars) {
            Debug.Assert(_relativeOverallCarsIdxs != null);

            var numRelPos = Settings.NumOverallRelativePos;
            for (int i = 0; i < numRelPos * 2 + 1; i++) {
                var idxInCars = focusedCarIdx - numRelPos + i;
                _relativeOverallCarsIdxs[i] = idxInCars < cars.Count && idxInCars >= 0 ? (int?)idxInCars : null;
            }
        }

        internal void SetPartialRelativeOverallOrder(int FocusedCarIdx, List<CarData> Cars) {
            Debug.Assert(_partialRelativeOverallCarsIdxs != null);

            var numOverallPos = Settings.PartialRelativeOverallNumOverallPos;
            var numRelPos = Settings.PartialRelativeOverallNumRelativePos;

            // TODO: Try to clean this mess up
            _focusedCarPosInPartialRelativeOverallCarsIdxs = null;
            for (int i = 0; i < numOverallPos + numRelPos * 2 + 1; i++) {
                int? idxInCars = i;
                if (i > numOverallPos - 1 && FocusedCarIdx > numOverallPos + numRelPos) {
                    idxInCars += FocusedCarIdx - numOverallPos - numRelPos;
                }
                _partialRelativeOverallCarsIdxs[i] = idxInCars < Cars.Count ? idxInCars : null;
                if (idxInCars == FocusedCarIdx) {
                    _focusedCarPosInPartialRelativeOverallCarsIdxs = i;
                }
            }
        }

        internal void SetRelativeClassOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInClassCarsIdxs) {
            Debug.Assert(_relativeClassCarsIdxs != null);

            for (int i = 0; i < Settings.NumClassRelativePos * 2 + 1; i++) {
                int? idx = Cars[(int)FocusedCarIdx].InClassPos - Settings.NumClassRelativePos + i - 1;
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                _relativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
            }
        }

        internal void SetPartialRelativeClassOrder(int FocusedCarIdx, List<CarData> Cars, int?[] PosInClassCarsIdxs) {
            Debug.Assert(_partialRelativeClassCarsIdxs != null);

            var overallPos = Settings.PartialRelativeClassNumClassPos;
            var relPos = Settings.PartialRelativeClassNumRelativePos;

            // TODO: Try to clean this mess up
            for (int i = 0; i < overallPos + relPos * 2 + 1; i++) {
                int? idx = i;
                var focusedClassPos = Cars[(int)FocusedCarIdx].InClassPos - 1;
                if (i > overallPos - 1 && focusedClassPos > overallPos + relPos) {
                    idx += focusedClassPos - overallPos - relPos;
                }
                idx = PosInClassCarsIdxs.ElementAtOrDefault((int)idx);
                _partialRelativeClassCarsIdxs[i] = idx != null && idx < Cars.Count && idx >= 0 ? idx : null;
                if (idx == FocusedCarIdx) {
                    _focusedCarPosInPartialRelativeClassCarsIdxs = i;
                }
            }
        }

        private bool DynLeaderboardContainsAny(params Leaderboard[] leaderboards) {
            foreach (var v in leaderboards) {
                if (Settings.Order.Contains(v)) {
                    return true;
                }
            }
            return false;
        }

    }
}