using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.AccBroadcastingNetwork;
using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;
using KLPlugins.DynLeaderboards.Track;

using SimHub.Plugins;

using SimHubAMS2 = PCarsSharedMemory.AMS2.Models;

namespace KLPlugins.DynLeaderboards;

/// <summary>
///     Storage and calculation of new properties
/// </summary>
public sealed class Values : IDisposable {
    public TrackData? TrackData { get; private set; }
    public Session Session { get; } = new();
    public Booleans Booleans { get; } = new();

    public ReadOnlyCollection<CarData> OverallOrder { get; }
    public ReadOnlyCollection<CarData> ClassOrder { get; }
    public ReadOnlyCollection<CarData> CupOrder { get; }
    public ReadOnlyCollection<CarData> RelativeOnTrackAheadOrder { get; }
    public ReadOnlyCollection<CarData> RelativeOnTrackBehindOrder { get; }
    private List<CarData> _overallOrder { get; } = [];
    private List<CarData> _classOrder { get; } = [];
    private List<CarData> _cupOrder { get; } = [];
    private List<CarData> _relativeOnTrackAheadOrder { get; } = [];
    private List<CarData> _relativeOnTrackBehindOrder { get; } = [];
    public CarData? FocusedCar { get; private set; } = null;

    public int NumClassesInSession { get; private set; } = 0;
    public int NumCupsInSession { get; private set; } = 0;

    internal bool _IsFirstFinished { get; private set; } = false;
    private bool _startingPositionsSet = false;

    internal Values() {
        this.OverallOrder = this._overallOrder.AsReadOnly();
        this.ClassOrder = this._classOrder.AsReadOnly();
        this.CupOrder = this._cupOrder.AsReadOnly();
        this.RelativeOnTrackAheadOrder = this._relativeOnTrackAheadOrder.AsReadOnly();
        this.RelativeOnTrackBehindOrder = this._relativeOnTrackBehindOrder.AsReadOnly();

        this.Reset();
    }


    internal void Reset() {
        Logging.LogInfo("Values.Reset()");
        this.Session.Reset();
        this.ResetWithoutSession();
    }

    internal void ResetWithoutSession() {
        Logging.LogInfo("Values.ResetWithoutSession()");
        this.Booleans.Reset();
        this._overallOrder.Clear();
        this._classOrder.Clear();
        this._cupOrder.Clear();
        this._relativeOnTrackAheadOrder.Clear();
        this._relativeOnTrackBehindOrder.Clear();
        this.FocusedCar = null;
        this._IsFirstFinished = false;
        this._startingPositionsSet = false;
        this.NumClassesInSession = 0;
        this.NumCupsInSession = 0;
    }

    #region IDisposable Support

    ~Values() {
        this.Dispose(false);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed = false;

    private void Dispose(bool disposing) {
        if (!this._isDisposed) {
            if (disposing) {
                this.TrackData?.Dispose();
                Logging.LogInfo("Disposed");
            }

            this._isDisposed = true;
        }
    }

    public void Dispose() {
        this.Dispose(true);
    }

    #endregion IDisposable Support

    internal void UpdateClassInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateClassInfos(this);
        }
    }

    internal void UpdateCarInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateCarInfos(this);
        }
    }

    internal void UpdateTeamCupInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateTeamCupInfos(this);
        }
    }

    internal void UpdateDriverInfos() {
        foreach (var car in this.OverallOrder) {
            car.UpdateDriverInfos(this);
        }
    }

    private int _skipCarUpdatesCount = 0;

    internal void OnDataUpdate(PluginManager _, GameData data) {
        this.Session.OnDataUpdate(data);

        if (this.Booleans.NewData.IsNewEvent
            || this.Session.IsNewSession
            || this.TrackData?.PrettyName != data._NewData.TrackName) {
            Logging.LogInfo($"newEvent={this.Booleans.NewData.IsNewEvent}, newSession={this.Session.IsNewSession}");
            this.ResetWithoutSession();
            this.Booleans.OnNewEvent(this.Session.SessionType);
            if (this.TrackData == null || this.TrackData.PrettyName != data._NewData.TrackName) {
                this.TrackData?.Dispose();
                this.TrackData = new TrackData(data);
                Logging.LogInfo(
                    $"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}, len={this.TrackData.LengthMeters}"
                );
            }

            foreach (var car in this.OverallOrder) {
                this.TrackData.BuildLapInterpolator(car.CarClass);
            }

            this._skipCarUpdatesCount = 0;
        }

        if (this.TrackData == null) {
            this.TrackData = new TrackData(data);
            foreach (var car in this.OverallOrder) {
                this.TrackData.BuildLapInterpolator(car.CarClass);
            }

            Logging.LogInfo(
                $"Track set to: id={this.TrackData.Id}, name={this.TrackData.PrettyName}, len={this.TrackData.LengthMeters}"
            );
        }

        this.TrackData.OnDataUpdate();

        if (this.TrackData.LengthMeters == 0)
            // In ACC sometimes the track length is not immediately available, and is 0.
        {
            this.TrackData.SetLength(data);
        }

        this.Booleans.OnDataUpdate(data, this);

        // Skip car updates for few updated after new session so that everything from the SimHub's side would be reset
        // Atm this is important in AMS2, so that old session data doesn't leak into new session
        if (this._skipCarUpdatesCount > 100) {
            this.UpdateCars(data);
        } else {
            this._skipCarUpdatesCount++;
        }
    }

    // Temporary dicts used in UpdateCars. Don't allocate new one at each update, just clear them.
    private readonly Dictionary<CarClass, CarData> _classBestLapCars = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _cupBestLapCars = [];
    private readonly Dictionary<CarClass, int> _classPositions = [];
    private readonly Dictionary<CarClass, CarData> _classLeaders = [];
    private readonly Dictionary<CarClass, CarData> _carAheadInClass = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), int> _cupPositions = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _cupLeaders = [];
    private readonly Dictionary<(CarClass, TeamCupCategory), CarData> _carAheadInCup = [];
    private const double _MISSING_CAR_TOLERANCE_SECONDS = 10;

    private void UpdateCars(GameData data) {
        this._classBestLapCars.Clear();
        this._cupBestLapCars.Clear();
        this._classPositions.Clear();
        this._classLeaders.Clear();
        this._carAheadInClass.Clear();
        this._cupPositions.Clear();
        this._cupLeaders.Clear();
        this._carAheadInCup.Clear();

        IEnumerable<(Opponent, int)> cars = data._NewData.Opponents.WithIndex();

        string? focusedCarId = null;
        if (DynLeaderboardsPlugin._Game.IsAcc) {
            var realtime = data._NewData.GetRawDataObject() switch {
                ACSharedMemory.ACC.Reader.ACCRawData rawDataNew => rawDataNew.Realtime,
                AccBroadcastingRawData d => d._RealtimeUpdate,
                var d => throw new Exception($"Unknown data type for ACC `{d?.GetType()}`"),
            };
            focusedCarId = realtime?.FocusedCarIndex.ToString();
        }

        CarData? overallBestLapCar = null;
        foreach (var (opponent, i) in cars) {
            if ((DynLeaderboardsPlugin._Game.IsAcc && opponent.Id == "Me")
                || (DynLeaderboardsPlugin._Game.IsAms2 && opponent.Id == "Safety Car  (AI)")
            ) {
                continue;
            }

            // Most common case is that the car's position hasn't changed
            var car = this._overallOrder.ElementAtOrDefault(i);
            if (car == null || car._Id != opponent.Id) {
                car = this._overallOrder.Find(c => c._Id == opponent.Id);
            }

            if (car == null) {
                if (!opponent.IsConnected || (DynLeaderboardsPlugin._Game.IsAcc && opponent.Coordinates == null)) {
                    continue;
                }

                car = new CarData(this, focusedCarId, opponent, data);
                this._overallOrder.Add(car);
                this.TrackData?.BuildLapInterpolator(car.CarClass);
            } else {
                Debug.Assert(car._Id == opponent.Id);
                car.UpdateIndependent(this, focusedCarId, opponent, data);

                // Note: car.IsFinished is actually updated in car.UpdateDependsOnOthers.
                // Thus, if the player manages to finish the race and exit before the first update, we would remove them.
                // However, that is practically impossible.
                if (!car.IsFinished
                    && (DateTime.Now - car._LastUpdateTime).TotalSeconds > Values._MISSING_CAR_TOLERANCE_SECONDS) {
                    continue;
                }
            }

            if (car.IsFocused) {
                this.FocusedCar = car;
            }

            if (car.BestLap?.Time != null) {
                if (!this._classBestLapCars.ContainsKey(car.CarClass)) {
                    this._classBestLapCars[car.CarClass] = car;
                } else {
                    var currentClassBestLap =
                        this._classBestLapCars[car.CarClass].BestLap!.Time!; // If it's in the dict, it cannot be null

                    if (car.BestLap.Time < currentClassBestLap) {
                        this._classBestLapCars[car.CarClass] = car;
                    }
                }

                if (!this._cupBestLapCars.ContainsKey((car.CarClass, car.TeamCupCategory))) {
                    this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)] = car;
                } else {
                    var currentCupBestLap = this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)].BestLap!
                        .Time!; // If it's in the dict, it cannot be null

                    if (car.BestLap.Time < currentCupBestLap) {
                        this._cupBestLapCars[(car.CarClass, car.TeamCupCategory)] = car;
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

        for (var i = this._overallOrder.Count - 1; i >= 0; i--) {
            var car = this._overallOrder[i];
            if (!car.IsFinished
                && (DateTime.Now - car._LastUpdateTime).TotalSeconds > Values._MISSING_CAR_TOLERANCE_SECONDS) {
                this._overallOrder.RemoveAt(i);
                Logging.LogInfo($"Removed disconnected car {car._Id}, #{car.CarNumberAsString}");
            }
        }

        if (!this._startingPositionsSet && this.Session.IsRace && this._overallOrder.Count != 0) {
            this.SetStartingOrder();
        }

        this.SetOverallOrder(data);

        if (!this._IsFirstFinished && this._overallOrder.Count > 0 && this.Session.SessionType == SessionType.RACE) {
            var first = this._overallOrder.First();
            if (this.Session.IsLapLimited) {
                this._IsFirstFinished = first.Laps.New == data._NewData.TotalLaps;
            } else if (this.Session.IsTimeLimited) {
                this._IsFirstFinished = data._NewData.SessionTimeLeft.TotalSeconds <= 0 && first.IsNewLap;
            }

            if (this._IsFirstFinished) {
                Logging.LogInfo($"First finished: id={this._overallOrder.First()._Id}");
            }
        }

        this._classOrder.Clear();
        this._cupOrder.Clear();
        this._relativeOnTrackAheadOrder.Clear();
        this._relativeOnTrackBehindOrder.Clear();
        var focusedClass = this.FocusedCar?.CarClass;
        var focusedCup = this.FocusedCar?.TeamCupCategory;
        foreach (var (car, i) in this._overallOrder.WithIndex()) {
            //DynLeaderboardsPlugin.LogInfo($"Car [{car.Id}, #{car.CarNumber}] missed update: {car.MissedUpdates}");
            car._IsUpdated = false;

            if (!this._classPositions.ContainsKey(car.CarClass)) {
                this._classPositions.Add(car.CarClass, 1);
                this._classLeaders.Add(car.CarClass, car);
            }

            if (!this._cupPositions.ContainsKey((car.CarClass, car.TeamCupCategory))) {
                this._cupPositions.Add((car.CarClass, car.TeamCupCategory), 1);
                this._cupLeaders.Add((car.CarClass, car.TeamCupCategory), car);
            }

            if (focusedClass != null && car.CarClass == focusedClass) {
                this._classOrder.Add(car);
                if (focusedCup != null && car.TeamCupCategory == focusedCup) {
                    this._cupOrder.Add(car);
                }
            }

            car.UpdateDependsOnOthers(
                values: this,
                overallBestLapCar: overallBestLapCar,
                classBestLapCar: this._classBestLapCars.GetValueOr(car.CarClass, null),
                cupBestLapCar: this._cupBestLapCars.GetValueOr((car.CarClass, car.TeamCupCategory), null),
                leaderCar: this._overallOrder.First(), // If we get there, there must be at least on car
                classLeaderCar: this._classLeaders[car.CarClass], // If we get there, the leader must be present
                cupLeaderCar: this._cupLeaders
                    [(car.CarClass, car.TeamCupCategory)], // If we get there, the leader must be present
                focusedCar: this.FocusedCar,
                carAhead: i > 0 ? this._overallOrder[i - 1] : null,
                carAheadInClass: this._carAheadInClass.GetValueOr(car.CarClass, null),
                carAheadInCup: this._carAheadInCup.GetValueOr((car.CarClass, car.TeamCupCategory), null),
                carAheadOnTrack: this.GetCarAheadOnTrack(car),
                overallPosition: i + 1,
                classPosition: this._classPositions[car.CarClass]++,
                cupPosition: this._cupPositions[(car.CarClass, car.TeamCupCategory)]++
            );

            if (car.IsFocused) {
                // nothing to do
            } else if (car.RelativeSplinePositionToFocusedCar > 0) {
                this._relativeOnTrackAheadOrder.Add(car);
            } else {
                this._relativeOnTrackBehindOrder.Add(car);
            }

            this._carAheadInClass[car.CarClass] = car;
            this._carAheadInCup[(car.CarClass, car.TeamCupCategory)] = car;
        }

        if (this.FocusedCar != null) {
            this._relativeOnTrackAheadOrder.Sort(
                (c1, c2) => c1.RelativeSplinePositionToFocusedCar.CompareTo(c2.RelativeSplinePositionToFocusedCar)
            );
            this._relativeOnTrackBehindOrder.Sort(
                (c1, c2) => c2.RelativeSplinePositionToFocusedCar.CompareTo(c1.RelativeSplinePositionToFocusedCar)
            );
        }

        this.NumClassesInSession = this._classPositions.Count;
        this.NumCupsInSession = this._cupPositions.Count;
    }

    private CarData? GetCarAheadOnTrack(CarData c) {
        var thisPos = c.SplinePosition;

        // Closest car ahead is the one with the smallest positive relative spline position.
        CarData? closestCar = null;
        var relSplinePos = double.MaxValue;
        foreach (var car in this._overallOrder) {
            var carPos = car.SplinePosition;

            var pos = CarData.CalculateRelativeSplinePosition(toPos: carPos, fromPos: thisPos);
            if (pos > 0 && pos < relSplinePos) {
                closestCar = car;
                relSplinePos = pos;
            }
        }

        return closestCar;
    }

    private void SetStartingOrder() {
        // This method is called after we have checked that all cars have NewData
        this._overallOrder.Sort(
            (a, b) => a._RawDataNew.Position.CompareTo(b._RawDataNew.Position)
        ); // Spline position may give wrong results if cars are sitting on the grid, thus NewData.Position

        var classPositions = new Dictionary<CarClass, int>(1); // Keep track of what class position are we at the moment
        var cupPositions =
            new Dictionary<(CarClass, TeamCupCategory), int>(1); // Keep track of what cup position are we at the moment
        foreach (var (car, i) in this._overallOrder.WithIndex()) {
            var thisClass = car.CarClass;
            var thisCup = car.TeamCupCategory;
            if (!classPositions.ContainsKey(thisClass)) {
                classPositions.Add(thisClass, 0);
            }

            if (!cupPositions.ContainsKey((thisClass, thisCup))) {
                cupPositions.Add((thisClass, thisCup), 0);
            }

            var classPos = ++classPositions[thisClass];
            var cupPos = ++cupPositions[(thisClass, thisCup)];
            car.SetStartingPositions(i + 1, classPos, cupPos);
        }

        this._startingPositionsSet = true;
    }

    private void SetOverallOrder(GameData gameData) {
        // Sort cars in overall position order
        if (this.Session.SessionType == SessionType.RACE) {
            // In race use TotalSplinePosition (splinePosition + laps) which updates real time.
            // RealtimeCarUpdate.Position only updates at the end of sector

            int Cmp(CarData a, CarData b) {
                if (a == b) {
                    return 0;
                }

                // Sort cars that have crossed the start line always in front of cars who haven't
                // This affect order at race starts where the number of completed laps is 0 either
                // side of the line but the spline position flips from 1 to 0
                if (a._HasCrossedStartLine && !b._HasCrossedStartLine) {
                    return -1;
                }

                if (b._HasCrossedStartLine && !a._HasCrossedStartLine) {
                    return 1;
                }

                // Always compare by laps first
                var aLaps = a.Laps.New;
                var bLaps = b.Laps.New;
                if (aLaps != bLaps) {
                    return bLaps.CompareTo(aLaps);
                }

                // Keep order if one of the cars has offset lap update, could cause jumping otherwise
                if (a._OffsetLapUpdate != 0 || b._OffsetLapUpdate != 0) {
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
                        // Thus, it should be behind the one who hasn't finished.
                        var aFTime = a._FinishTime?.Ticks ?? long.MinValue;
                        var bFTime = b._FinishTime?.Ticks ?? long.MinValue;
                        return aFTime.CompareTo(bFTime);
                    } else {
                        // Both cars have finished
                        var aFTime = a._FinishTime?.Ticks ?? long.MaxValue;
                        var bFTime = b._FinishTime?.Ticks ?? long.MaxValue;

                        if (aFTime == bFTime) {
                            return a._RawDataNew.Position.CompareTo(b._RawDataNew.Position);
                        }

                        return aFTime.CompareTo(bFTime);
                    }
                }

                if (DynLeaderboardsPlugin._Game.IsAms2) {
                    // Spline pos == 0.0 if race has not started, race state is 1 if that's the case, use games positions
                    // If using AMS2 shared memory data, UDP data is not supported atm
                    if (gameData._NewData.GetRawDataObject() is SimHubAMS2.AMS2APIStruct { mRaceState: < 2 }) {
                        return a._RawDataNew.Position.CompareTo(b._RawDataNew.Position);
                    }

                    // cars that didn't finish (DQ, DNS, DNF) should always be at the end
                    var aDidNotFinish = a._RawDataNew.DidNotFinish ?? false;
                    var bDidNotFinish = b._RawDataNew.DidNotFinish ?? false;
                    if (aDidNotFinish || bDidNotFinish) {
                        if (aDidNotFinish && bDidNotFinish) {
                            return a._RawDataNew.Position.CompareTo(b._RawDataNew.Position);
                        }

                        if (aDidNotFinish) {
                            return 1;
                        }

                        return -1;
                    }
                }

                // Keep order, make sort stable, fixes jumping, essentially keeps the cars in previous order
                if (a.TotalSplinePosition == b.TotalSplinePosition) {
                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                return b.TotalSplinePosition.CompareTo(a.TotalSplinePosition);
            }

            this._overallOrder.Sort(Cmp);
        } else {
            // In other sessions TotalSplinePosition doesn't make any sense, use Position
            int Cmp(CarData a, CarData b) {
                if (a == b) {
                    return 0;
                }

                var aPos = a._RawDataNew.Position;
                var bPos = b._RawDataNew.Position;
                if (aPos == bPos)
                    // if aPos == bPos, one cad probably left but maybe not. 
                    // Use old position to keep the order stable and not cause flickering.
                {
                    return a.PositionOverall.CompareTo(b.PositionOverall);
                }

                // Need to use RawDataNew.Position because the CarData.PositionOverall is updated based of the result of this sort
                return aPos.CompareTo(bPos);
            }

            this._overallOrder.Sort(Cmp);
        }
    }

    internal void OnGameStateChanged(bool running, PluginManager _) {
        if (running) { } else {
            this.Reset();
            // dispose track data on session end, so that we save the interpolators data after session,
            // where we have lots of time
            this.TrackData?.SaveData();
        }
    }
}