using GameReaderCommon;

using KLPlugins.DynLeaderboards.Log;

namespace KLPlugins.DynLeaderboards;

/// <summary>
///     Hold single set of boolean values
/// </summary>
public class BooleansBase {
    public bool IsInMenu { get; private set; }
    public bool EnteredMenu { get; private set; }
    public bool ExitedMenu { get; private set; }
    public bool IsNewEvent { get; private set; }

    internal BooleansBase() {
        this.Reset(SessionType.PRACTICE);
    }

    internal void Update(BooleansBase o) {
        this.IsInMenu = o.IsInMenu;
        this.EnteredMenu = o.EnteredMenu;
        this.ExitedMenu = o.ExitedMenu;
        this.IsNewEvent = o.IsNewEvent;
    }

    internal void Update(GameData data, Values v) {
        // if our last update was in menu or if somehow our last wasn't but games was
        var wasInMenu = this.IsInMenu
            || data.OldData.AirTemperature == 0 // ACC
            || data.OldData.TyrePressureFrontLeft == 0; // RF2

        this.IsInMenu = data.NewData.AirTemperature == 0 // ACC
            || data.NewData.TyrePressureFrontLeft == 0; // RF2
        this.EnteredMenu = !wasInMenu && this.IsInMenu;
        this.ExitedMenu = wasInMenu && !this.IsInMenu;
    }

    internal void Reset(SessionType sessionType) {
        this.IsInMenu = true;
        this.EnteredMenu = false;
        this.ExitedMenu = false;
        this.IsNewEvent = true;
    }

    internal void OnNewEvent(SessionType sessionType) {
        this.Reset(sessionType);
        this.IsNewEvent = false;
    }

    internal void OnSessionChange(SessionType sessionType) {
        this.Reset(sessionType);
        this.IsNewEvent = false;
    }
}

/// <summary>
///     Hold current and previous boolean values
/// </summary>
public class Booleans {
    public BooleansBase NewData { get; }
    public BooleansBase OldData { get; }

    internal Booleans() {
        this.NewData = new BooleansBase();
        this.OldData = new BooleansBase();
    }

    internal void Reset(SessionType sessionType = SessionType.PRACTICE) {
        Logging.LogInfo("Booleans.Reset()");
        this.OldData.Reset(sessionType);
        this.NewData.Reset(sessionType);
    }

    internal void OnNewEvent(SessionType sessionType) {
        this.NewData.OnNewEvent(sessionType);
        this.OldData.OnNewEvent(sessionType);
    }

    internal void OnNewSession(Values v) {
        this.NewData.OnSessionChange(v.Session.SessionType);
    }

    internal void OnDataUpdate(GameData data, Values v) {
        this.OldData.Update(this.NewData);
        this.NewData.Update(data, v);
    }
}