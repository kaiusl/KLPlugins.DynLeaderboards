using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs
{
    public class TrackData
    {
        public string TrackName { get; internal set; }
        public int TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        public Dictionary<string, List<string>> CameraSets { get; internal set; }
        public IEnumerable<string> HUDPages { get; internal set; }
    }
}
