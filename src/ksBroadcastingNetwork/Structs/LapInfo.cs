using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs
{
    public class LapInfo
    {
        public double? Laptime { get; internal set; }
        public List<double?> Splits { get; } = new List<double?>();
        public ushort CarIndex { get; internal set; }
        public ushort DriverIndex { get; internal set; }
        public bool IsInvalid { get; internal set; }
        public bool IsValidForBest { get; internal set; }
        public LapType Type { get; internal set; }

        public override string ToString()
        {
            return $"{Laptime, 5}|{string.Join("|", Splits)}";
        }
    }
}
