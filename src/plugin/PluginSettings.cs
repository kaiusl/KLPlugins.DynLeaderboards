using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace KLPlugins.Leaderboard {
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings
    {
        public int SpeedWarningLevel = 100;
    }

    public class Settings {
        public string DataLocation { get; set; }
        public string AccDataLocation { get; set; }
        public bool Log { get; set; }
        public int NumOverallPos { get; set; }
        public int NumRelativePos { get; set; }


        public Settings() {
            DataLocation = "PluginsData\\ACCBroadcastDataPlugin";
            AccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
            Log = true;
            NumOverallPos = 30;
            NumRelativePos = 5;
        }
    }
}