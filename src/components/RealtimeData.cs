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
            this.OldData = update;
            this.NewData = update;
        }

        internal void OnRealtimeUpdate(RealtimeUpdate update) {
            this.OldData = this.NewData;
            this.NewData = update;

            this.IsRace = this.NewData.SessionType == RaceSessionType.Race;
            this.IsSession = this.NewData.Phase == SessionPhase.Session;
            this.IsPreSession = !this.IsSession && this.NewData.Phase.EqualsAny(SessionPhase.Starting, SessionPhase.PreFormation, SessionPhase.FormationLap, SessionPhase.PreSession);
            this.IsPostSession = !this.IsSession && this.NewData.Phase.EqualsAny(SessionPhase.SessionOver, SessionPhase.PostSession, SessionPhase.ResultUI);

            this.IsSessionStart = this.OldData.Phase != SessionPhase.Session && this.IsSession;
            this.IsFocusedChange = this.NewData.FocusedCarIndex != this.OldData.FocusedCarIndex;
            this.IsNewSession = this.OldData.SessionType != this.NewData.SessionType
                || this.NewData.SessionIndex != this.OldData.SessionIndex
                || (this.OldData.Phase.EqualsAny(SessionPhase.Session, SessionPhase.SessionOver, SessionPhase.PostSession, SessionPhase.ResultUI) && this.IsPreSession);

            if (this.SessionTotalTime == TimeSpan.Zero) {
                this.SessionTotalTime = this.NewData.SessionRunningTime + this.NewData.SessionRemainingTime;
            }
        }
    }
}