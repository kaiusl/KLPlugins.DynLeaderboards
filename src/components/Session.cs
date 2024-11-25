using System;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards;

public class Session {
    public SessionType SessionType { get; private set; } = SessionType.UNKNOWN;
    public SessionPhase SessionPhase { get; private set; } = SessionPhase.UNKNOWN;

    /// <summary>
    ///     Session start effectively means that green flag is shown. It will be true for one update.
    /// </summary>
    public bool IsSessionStart { get; private set; }

    public bool IsNewSession { get; private set; }
    public bool IsTimeLimited { get; private set; }
    public bool IsLapLimited { get; private set; }
    public bool IsRace => this.SessionType == SessionType.RACE;

    /// <summary>
    ///     Time of day in game.
    /// </summary>
    public TimeSpan TimeOfDay { get; private set; }

    public TimeSpan? MaxDriverStintTime { get; private set; }
    public TimeSpan? MaxDriverTotalDriveTime { get; private set; }

    private bool _isSessionLimitSet = false;
    private int _sessionIndex;

    internal Session() {
        this.Reset();
    }

    internal void Reset() {
        DynLeaderboardsPlugin.LogInfo("Session.Reset()");
        this.SessionType = SessionType.UNKNOWN;
        this.SessionPhase = SessionPhase.UNKNOWN;

        this.IsNewSession = true;
        this.IsSessionStart = false;
        this.IsTimeLimited = false;
        this.IsLapLimited = false;

        this.TimeOfDay = TimeSpan.Zero;
        this._isSessionLimitSet = false;

        this.MaxDriverStintTime = null;
        this.MaxDriverTotalDriveTime = null;

        this._sessionIndex = 0;
    }

    internal void OnDataUpdate(GameData data) {
        var newSessType = SessionTypeExtensions.FromShGameData(data);
        this.IsNewSession = newSessType != this.SessionType;

        if (DynLeaderboardsPlugin.Game.IsAcc) {
            var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

            if (rawDataNew.Graphics.SessionIndex != this._sessionIndex) {
                // detects multiple following sessions which are same kind
                this.IsNewSession = true;
            }

            this._sessionIndex = rawDataNew.Graphics.SessionIndex;
        }

        if (this.IsNewSession) {
            this.Reset();
        }

        this.SessionType = newSessType;
        var oldPhase = this.SessionPhase;
        this.SessionPhase = SessionPhaseExtensions.FromShGameData(data);
        this.IsSessionStart = oldPhase != SessionPhase.SESSION && this.SessionPhase == SessionPhase.SESSION;

        if (!this._isSessionLimitSet) {
            // Need to set once as at the end of the session SessionTimeLeft == 0 and this will confuse plugin
            this.IsLapLimited = data.NewData.RemainingLaps > 0;
            this.IsTimeLimited = !this.IsLapLimited;
            this._isSessionLimitSet = true;
            DynLeaderboardsPlugin.LogInfo(
                $"Session limit set: isLapLimited={this.IsLapLimited}, isTimeLimited={this.IsTimeLimited}"
            );
        }

        if (DynLeaderboardsPlugin.Game.IsAcc) {
            var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

            this._sessionIndex = rawDataNew.Graphics.SessionIndex;
            this.TimeOfDay = TimeSpan.FromSeconds(rawDataNew.Graphics.clock);

            // Set max stint times. This is only done once when we know that the session hasn't started, so that the time left shows max times.
            if (this.MaxDriverStintTime == null
                && this.IsRace
                && this.SessionPhase == SessionPhase.PRE_SESSION
                && rawDataNew.Graphics.DriverStintTimeLeft >= 0) {
                this.MaxDriverStintTime = TimeSpan.FromMilliseconds(rawDataNew.Graphics.DriverStintTimeLeft);
                var maxDriverTotalTime = rawDataNew.Graphics.DriverStintTotalTimeLeft;
                if (maxDriverTotalTime != 65_535_000) {
                    // This is max value, which means that the limit doesn't exist
                    this.MaxDriverTotalDriveTime =
                        TimeSpan.FromMilliseconds(rawDataNew.Graphics.DriverStintTotalTimeLeft);
                }
            }
        }
    }
}

public enum SessionType {
    PRACTICE,
    QUALIFYING,
    SUPERPOLE,
    RACE,
    HOTLAP,
    HOTSTINT,
    HOTLAP_SUPERPOLE,
    DRIFT,
    TIME_ATTACK,
    DRAG,
    WARMUP,
    TIME_TRIAL,
    UNKNOWN,
}

public enum SessionPhase {
    UNKNOWN = 0,
    STARTING = 1,
    PRE_FORMATION = 2,
    FORMATION_LAP = 3,
    PRE_SESSION = 4,
    SESSION = 5,
    SESSION_OVER = 6,
    POST_SESSION = 7,
    RESULT_UI = 8,
}

internal static class SessionTypeExtensions {
    internal static SessionType FromShGameData(GameData data) {
        if (DynLeaderboardsPlugin.Game.IsAcc) {
            var accData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
            return accData.Graphics.Session switch {
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.UNKNOWN,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_PRACTICE => SessionType.PRACTICE,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_QUALIFY => SessionType.QUALIFYING,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_RACE => SessionType.RACE,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_HOTLAP => SessionType.HOTLAP,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TIME_ATTACK,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRIFT => SessionType.DRIFT,
                ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRAG => SessionType.DRAG,
                (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)7 => SessionType.HOTSTINT,
                (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)8 => SessionType.HOTLAP_SUPERPOLE,
                _ => SessionType.UNKNOWN,
            };
        }

        if (DynLeaderboardsPlugin.Game.IsAc) {
            var acData = (ACSharedMemory.Reader.ACRawData)data.NewData.GetRawDataObject();
            return acData.Graphics.Session switch {
                ACSharedMemory.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.UNKNOWN,
                ACSharedMemory.AC_SESSION_TYPE.AC_PRACTICE => SessionType.PRACTICE,
                ACSharedMemory.AC_SESSION_TYPE.AC_QUALIFY => SessionType.QUALIFYING,
                ACSharedMemory.AC_SESSION_TYPE.AC_RACE => SessionType.RACE,
                ACSharedMemory.AC_SESSION_TYPE.AC_HOTLAP => SessionType.HOTLAP,
                ACSharedMemory.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TIME_ATTACK,
                ACSharedMemory.AC_SESSION_TYPE.AC_DRIFT => SessionType.DRIFT,
                ACSharedMemory.AC_SESSION_TYPE.AC_DRAG => SessionType.DRAG,
                _ => SessionType.UNKNOWN,
            };
        }

        return SessionTypeExtensions.FromString(data.NewData.SessionTypeName);
    }

    private static SessionType FromString(string s) {
        if (DynLeaderboardsPlugin.Game.IsAcc) {
            switch (s.ToLower()) {
                case "7":
                    return SessionType.HOTSTINT;
                case "8":
                    return SessionType.HOTLAP_SUPERPOLE;
            }
        }

        return s.ToLower() switch {
            "practice"
                or "open practice"
                or "offline testing" // IRacing
                or "practice 1"
                or "practice 2"
                or "practice 3"
                or "short practice" // F120xx
                => SessionType.PRACTICE,

            "qualify"
                or "open qualify"
                or "lone qualify" // IRacing
                or "qualifying 1"
                or "qualifying 2"
                or "qualifying 3"
                or "short qualifying"
                or "OSQ" // F120xx
                => SessionType.QUALIFYING,

            "race"
                or "race 1"
                or "race 2"
                or "race 3" // F120xx
                => SessionType.RACE,
            "hotlap" => SessionType.HOTLAP,
            "hotstint" => SessionType.HOTSTINT,
            "hotlapsuperpole" => SessionType.HOTLAP_SUPERPOLE,
            "drift" => SessionType.DRIFT,
            "time_attack" => SessionType.TIME_ATTACK,
            "drag" => SessionType.DRAG,
            "time_trial" => SessionType.TIME_TRIAL,
            "warmup" => SessionType.WARMUP,
            _ => SessionType.UNKNOWN,
        };
    }

    internal static string ToPrettyString(this SessionType s) {
        return s switch {
            SessionType.PRACTICE => "Practice",
            SessionType.QUALIFYING => "Qualifying",
            SessionType.RACE => "Race",
            SessionType.HOTLAP => "Hotlap",
            SessionType.HOTSTINT => "Hotstint",
            SessionType.HOTLAP_SUPERPOLE => "Superpole",
            SessionType.DRIFT => "Drift",
            SessionType.DRAG => "Drag",
            SessionType.TIME_ATTACK => "Time attack",
            SessionType.TIME_TRIAL => "Time trial",
            SessionType.WARMUP => "Warmup",
            _ => "Unknown",
        };
    }
}

internal static class SessionPhaseExtensions {
    internal static SessionPhase FromShGameData(GameData data) {
        if (DynLeaderboardsPlugin.Game.IsAcc) {
            var accData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
            if (accData.Realtime?.Phase == null) {
                return SessionPhase.UNKNOWN;
            }

            return accData.Realtime.Phase switch {
                ksBroadcastingNetwork.SessionPhase.NONE => SessionPhase.UNKNOWN,
                ksBroadcastingNetwork.SessionPhase.Starting => SessionPhase.STARTING,
                ksBroadcastingNetwork.SessionPhase.PreFormation => SessionPhase.PRE_FORMATION,
                ksBroadcastingNetwork.SessionPhase.FormationLap => SessionPhase.FORMATION_LAP,
                ksBroadcastingNetwork.SessionPhase.PreSession => SessionPhase.PRE_SESSION,
                ksBroadcastingNetwork.SessionPhase.Session => SessionPhase.SESSION,
                ksBroadcastingNetwork.SessionPhase.SessionOver => SessionPhase.SESSION_OVER,
                ksBroadcastingNetwork.SessionPhase.PostSession => SessionPhase.POST_SESSION,
                ksBroadcastingNetwork.SessionPhase.ResultUI => SessionPhase.RESULT_UI,
                _ => throw new Exception($"Unknown session phase {accData.Realtime.Phase}"),
            };
        }

        if (DynLeaderboardsPlugin.Game.IsRf2OrLmu) {
            var rf2Data = (RfactorReader.RF2.WrapV2)data.NewData.GetRawDataObject();

            var phase = rf2Data.Data.mGamePhase switch {
                0 => SessionPhase.STARTING,
                1 or 2 => SessionPhase.PRE_FORMATION,
                3 => SessionPhase.FORMATION_LAP,
                4 => SessionPhase.PRE_SESSION,
                5 => SessionPhase.SESSION,
                6 => SessionPhase.SESSION, // actually FCY or safety car
                7 => SessionPhase.SESSION, // described as session stopped, not sure what it means
                8 => SessionPhase.SESSION_OVER,
                9 =>
                    SessionPhase
                        .STARTING, // it's possible but don't know what it means exactly, but it happens at race starts
                _ => SessionPhase.UNKNOWN,
            };
            if (phase == SessionPhase.UNKNOWN) {
                DynLeaderboardsPlugin.LogWarn($"Unknown session phase {rf2Data.Data.mGamePhase}");
            }

            return phase;
        }

        if (DynLeaderboardsPlugin.Game.IsR3E) {
            var r3EData = (R3E.Data.Shared)data.NewData.GetRawDataObject();
            return r3EData.SessionPhase switch {
                -1 => SessionPhase.UNKNOWN,
                1 or 2 => SessionPhase.STARTING,
                3 => SessionPhase.FORMATION_LAP,
                4 => SessionPhase.PRE_SESSION,
                5 => SessionPhase.SESSION,
                6 => SessionPhase.SESSION_OVER, // Checkered flag shown
                _ => (SessionPhase)r3EData.SessionPhase,
            };
        }

        // TODO: Figure out how to detect these in other games
        return SessionPhase.UNKNOWN;
    }
}