namespace KLPlugins.DynLeaderboards {

    /// <summary>
    /// Booleans to tell which game we have. Since different games have different available data then we need to do alot of check like gameName == "...".
    /// The gameName is constant in each plugin reload and thus we can set it once and simplyfy game checks alot.
    /// </summary>
    internal class Game {
        public const string AcName = "AssettoCorsa";
        public const string AccName = "AssettoCorsaCompetizione";
        public const string Rf2Name = "RFactor2";
        public const string IracingName = "IRacing";
        public const string R3eName = "RRRE";

        public bool IsAc { get; } = false;
        public bool IsAcc { get; } = false;
        public bool IsRf2 { get; } = false;
        public bool IsIracing { get; } = false;
        public bool IsR3e { get; } = false;
        public bool IsUnknown { get; } = false;
        public string Name { get; }

        public Game(string gameName) {
            this.Name = gameName;
            switch (gameName) {
                case AcName:
                    this.IsAc = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to AC");
                    break;

                case AccName:
                    this.IsAcc = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to ACC");
                    break;

                case Rf2Name:
                    this.IsRf2 = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to RF2");
                    break;

                case IracingName:
                    this.IsIracing = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to IRacing");
                    break;

                case R3eName:
                    this.IsR3e = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to R3E");
                    break;

                default:
                    this.IsUnknown = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to Unknown");
                    break;
            }
        }
    }
}