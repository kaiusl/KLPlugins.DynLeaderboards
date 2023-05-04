using System;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;

namespace KLPlugins.DynLeaderboards.Realtime {

    internal class RealtimeData {
        public RealtimeUpdate NewData { get; private set; }
        public RealtimeUpdate OldData { get; private set; }
        public TimeSpan SessionTotalTime { get; private set; } = TimeSpan.Zero;

        public bool IsNewSession { get; private set; }
        public bool IsSessionStart { get; private set; }
        public bool IsFocusedChange { get; private set; }
        public bool IsSession { get; private set; }
        public bool IsPreSession { get; private set; }
        public bool IsPostSession { get; private set; }
        public bool IsRace { get; private set; }

        internal RealtimeData(RealtimeUpdate update) {
            OldData = update;
            NewData = update;
        }

        internal void OnRealtimeUpdate(RealtimeUpdate update) {
            OldData = NewData;
            NewData = update;

            IsRace = NewData.SessionType == RaceSessionType.Race;
            IsSession = NewData.Phase == SessionPhase.Session;
            IsPreSession = !IsSession && NewData.Phase.EqualsAny(SessionPhase.Starting, SessionPhase.PreFormation, SessionPhase.FormationLap, SessionPhase.PreSession);
            IsPostSession = !IsSession && NewData.Phase.EqualsAny(SessionPhase.SessionOver, SessionPhase.PostSession, SessionPhase.ResultUI);

            IsSessionStart = OldData.Phase != SessionPhase.Session && IsSession;
            IsFocusedChange = NewData.FocusedCarIndex != OldData.FocusedCarIndex;
            IsNewSession = OldData.SessionType != NewData.SessionType
                || NewData.SessionIndex != OldData.SessionIndex
                || OldData.Phase.EqualsAny(SessionPhase.Session, SessionPhase.SessionOver, SessionPhase.PostSession, SessionPhase.ResultUI) && IsPreSession;

            if (SessionTotalTime == TimeSpan.Zero)
                SessionTotalTime = NewData.SessionRunningTime + NewData.SessionRemainingTime;
        }
    }
}