using System;

using GameReaderCommon;

namespace KLPlugins.DynLeaderboards {
    public class Session {
        public SessionType SessionType { get; private set; } = SessionType.Unknown;
        public SessionPhase SessionPhase { get; private set; } = SessionPhase.Unknown;
        /// <summary>
        /// Session start effectively means that green flag is shown. It will be true for one update.
        /// </summary>
        public bool IsSessionStart { get; private set; }
        public bool IsNewSession { get; private set; }
        public bool IsTimeLimited { get; private set; }
        public bool IsLapLimited { get; private set; }
        public bool IsRace => this.SessionType == SessionType.Race;
        /// <summary>
        /// Time of day in game.
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
            this.SessionType = SessionType.Unknown;
            this.SessionPhase = SessionPhase.Unknown;

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
            var newSessType = SessionTypeExtensions.FromSHGameData(data);
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
            this.SessionPhase = SessionPhaseExtensions.FromSHGameData(data);
            this.IsSessionStart = oldPhase != SessionPhase.Session && this.SessionPhase == SessionPhase.Session;

            if (!this._isSessionLimitSet) {
                // Need to set once as at the end of the session SessionTimeLeft == 0 and this will confuse plugin
                this.IsLapLimited = data.NewData.RemainingLaps > 0;
                this.IsTimeLimited = !this.IsLapLimited;
                this._isSessionLimitSet = true;
                DynLeaderboardsPlugin.LogInfo($"Session limit set: isLapLimited={this.IsLapLimited}, isTimeLimited={this.IsTimeLimited}");
            }

            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var rawDataNew = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();

                this._sessionIndex = rawDataNew.Graphics.SessionIndex;
                this.TimeOfDay = TimeSpan.FromSeconds(rawDataNew.Graphics.clock);

                // Set max stint times. This is only done once when we know that the session hasn't started, so that the time left shows max times.
                if (this.MaxDriverStintTime == null && this.IsRace && this.SessionPhase == SessionPhase.PreSession && rawDataNew.Graphics.DriverStintTimeLeft >= 0) {
                    this.MaxDriverStintTime = TimeSpan.FromMilliseconds(rawDataNew.Graphics.DriverStintTimeLeft);
                    var maxDriverTotalTime = rawDataNew.Graphics.DriverStintTotalTimeLeft;
                    if (maxDriverTotalTime != 65_535_000) { // This is max value, which means that the limit doesn't exist
                        this.MaxDriverTotalDriveTime = TimeSpan.FromMilliseconds(rawDataNew.Graphics.DriverStintTotalTimeLeft);
                    }
                }
            }
        }
    }

    public enum SessionType {
        Practice,
        Qualifying,
        Superpole,
        Race,
        Hotlap,
        Hotstint,
        HotlapSuperpole,
        Drift,
        TimeAttack,
        Drag,
        Warmup,
        TimeTrial,
        Unknown
    }


    public enum SessionPhase {
        Unknown = 0,
        Starting = 1,
        PreFormation = 2,
        FormationLap = 3,
        PreSession = 4,
        Session = 5,
        SessionOver = 6,
        PostSession = 7,
        ResultUI = 8
    };

    internal static class SessionTypeExtensions {
        internal static SessionType FromSHGameData(GameData data) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                return accData.Graphics.Session switch {
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.Unknown,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_PRACTICE => SessionType.Practice,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_QUALIFY => SessionType.Qualifying,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_RACE => SessionType.Race,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_HOTLAP => SessionType.Hotlap,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TimeAttack,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRIFT => SessionType.Drift,
                    ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE.AC_DRAG => SessionType.Drag,
                    (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)7 => SessionType.Hotstint,
                    (ACSharedMemory.ACC.MMFModels.AC_SESSION_TYPE)8 => SessionType.HotlapSuperpole,
                    _ => SessionType.Unknown,
                };
            } else if (DynLeaderboardsPlugin.Game.IsAc) {
                var acData = (ACSharedMemory.Reader.ACRawData)data.NewData.GetRawDataObject();
                return acData.Graphics.Session switch {
                    ACSharedMemory.AC_SESSION_TYPE.AC_UNKNOWN => SessionType.Unknown,
                    ACSharedMemory.AC_SESSION_TYPE.AC_PRACTICE => SessionType.Practice,
                    ACSharedMemory.AC_SESSION_TYPE.AC_QUALIFY => SessionType.Qualifying,
                    ACSharedMemory.AC_SESSION_TYPE.AC_RACE => SessionType.Race,
                    ACSharedMemory.AC_SESSION_TYPE.AC_HOTLAP => SessionType.Hotlap,
                    ACSharedMemory.AC_SESSION_TYPE.AC_TIME_ATTACK => SessionType.TimeAttack,
                    ACSharedMemory.AC_SESSION_TYPE.AC_DRIFT => SessionType.Drift,
                    ACSharedMemory.AC_SESSION_TYPE.AC_DRAG => SessionType.Drag,
                    _ => SessionType.Unknown,
                };
            }

            return FromString(data.NewData.SessionTypeName);
        }


        private static SessionType FromString(string s) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                switch (s.ToLower()) {
                    case "7":
                        return SessionType.Hotstint;
                    case "8":
                        return SessionType.HotlapSuperpole;
                    default:
                        break;
                }
            }


            return s.ToLower() switch {
                "practice"
                or "open practice" or "offline testing" // IRacing
                or "practice 1" or "practice 2" or "practice 3" or "short practice" // F120xx
                => SessionType.Practice,

                "qualify"
                or "open qualify" or "lone qualify" // IRacing
                or "qualifying 1" or "qualifying 2" or "qualifying 3" or "short qualifying" or "OSQ" // F120xx
                => SessionType.Qualifying,

                "race"
                or "race 1" or "race 2" or "race 3" // F120xx
                => SessionType.Race,
                "hotlap" => SessionType.Hotlap,
                "hotstint" => SessionType.Hotstint,
                "hotlapsuperpole" => SessionType.HotlapSuperpole,
                "drift" => SessionType.Drift,
                "time_attack" => SessionType.TimeAttack,
                "drag" => SessionType.Drag,
                "time_trial" => SessionType.TimeTrial,
                "warmup" => SessionType.Warmup,
                _ => SessionType.Unknown,
            };
        }

        internal static string ToPrettyString(this SessionType s) {
            return s switch {
                SessionType.Practice => "Practice",
                SessionType.Qualifying => "Qualifying",
                SessionType.Race => "Race",
                SessionType.Hotlap => "Hotlap",
                SessionType.Hotstint => "Hotstint",
                SessionType.HotlapSuperpole => "Superpole",
                SessionType.Drift => "Drift",
                SessionType.Drag => "Drag",
                SessionType.TimeAttack => "Time attack",
                SessionType.TimeTrial => "Time trial",
                SessionType.Warmup => "Warmup",
                _ => "Unknown",
            };
        }
    }

    internal static class SessionPhaseExtensions {
        internal static SessionPhase FromSHGameData(GameData data) {
            if (DynLeaderboardsPlugin.Game.IsAcc) {
                var accData = (ACSharedMemory.ACC.Reader.ACCRawData)data.NewData.GetRawDataObject();
                if (accData.Realtime?.Phase == null) {
                    return SessionPhase.Unknown;
                }
                return accData.Realtime.Phase switch {
                    ksBroadcastingNetwork.SessionPhase.NONE => SessionPhase.Unknown,
                    ksBroadcastingNetwork.SessionPhase.Starting => SessionPhase.Starting,
                    ksBroadcastingNetwork.SessionPhase.PreFormation => SessionPhase.PreFormation,
                    ksBroadcastingNetwork.SessionPhase.FormationLap => SessionPhase.FormationLap,
                    ksBroadcastingNetwork.SessionPhase.PreSession => SessionPhase.PreSession,
                    ksBroadcastingNetwork.SessionPhase.Session => SessionPhase.Session,
                    ksBroadcastingNetwork.SessionPhase.SessionOver => SessionPhase.SessionOver,
                    ksBroadcastingNetwork.SessionPhase.PostSession => SessionPhase.PostSession,
                    ksBroadcastingNetwork.SessionPhase.ResultUI => SessionPhase.ResultUI,
                    _ => throw new System.Exception($"Unknown session phase {accData.Realtime.Phase}"),
                };
            } else if (DynLeaderboardsPlugin.Game.IsRf2) {
                var rf2Data = (RfactorReader.RF2.WrapV2)data.NewData.GetRawDataObject();

                var phase = rf2Data.Data.mGamePhase switch {
                    0 => SessionPhase.Starting,
                    1 or 2 => SessionPhase.PreFormation,
                    3 => SessionPhase.FormationLap,
                    4 => SessionPhase.PreSession,
                    5 => SessionPhase.Session,
                    6 => SessionPhase.Session, // actually FCY or safety car
                    7 => SessionPhase.Session, // described as session stopped, not sure what it means
                    8 => SessionPhase.SessionOver,
                    9 => SessionPhase.Unknown, // it's possible but don't know that it means
                    _ => SessionPhase.Unknown
                };
                if (phase == SessionPhase.Unknown) {
                    DynLeaderboardsPlugin.LogWarn($"Unknown session phase {rf2Data.Data.mGamePhase}");
                }
                return phase;
            } else {
                // TODO: Figure out how to detect these in other games
                return SessionPhase.Unknown;
            }
        }
    }
}