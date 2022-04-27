namespace KLPlugins.DynLeaderboards {


    /// <summary>
    /// Booleans to tell which game we have. Since different games have different available data then we need to do alot of check like gameName == "...".
    /// The gameName is constant in each plugin reload and thus we can set it once and simplyfy game checks alot.
    /// </summary>
    class Game {
        public const string AcName = "AssettoCorsa";
        public const string AccName = "AssettoCorsaCompetizione";
        public const string Rf2Name = "RFactor2";
        public const string IracingName = "IRacing";
        public const string R3eName = "RRRE";

        public bool IsAc { get => _isAc; }
        public bool IsAcc { get => _isAcc; }
        public bool IsRf2 { get => _isRf2; }
        public bool IsIracing { get => _isIracing; }
        public bool IsR3e { get => _isR3e; }
        public bool IsUnknown { get => _isUnknown; }
        public string Name { get => _name; }

        private readonly bool _isAc = false;
        private readonly bool _isAcc = false;
        private readonly bool _isRf2 = false;
        private readonly bool _isIracing = false;
        private readonly bool _isR3e = false;
        private readonly bool _isUnknown = false;
        private readonly string _name;

        public Game(string gameName) {
            _name = gameName;
            switch (gameName) {
                case AcName:
                    _isAc = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to AC");
                    break;
                case AccName:
                    _isAcc = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to ACC");
                    break;
                case Rf2Name:
                    _isRf2 = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to RF2");
                    break;
                case IracingName:
                    _isIracing = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to IRacing");
                    break;
                case R3eName:
                    _isR3e = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to R3E");
                    break;
                default:
                    _isUnknown = true;
                    DynLeaderboardsPlugin.LogInfo("Game set to Unknown");
                    break;
            }
        }
    }
}