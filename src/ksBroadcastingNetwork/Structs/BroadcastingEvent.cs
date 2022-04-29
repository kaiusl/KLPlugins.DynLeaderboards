
namespace KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs {
    class BroadcastingEvent {
        public BroadcastingCarEventType Type { get; internal set; }
        public string Msg { get; internal set; }
        public double Time { get; internal set; }
        public int CarId { get; internal set; }
        public CarInfo CarData { get; internal set; }
    }
}
