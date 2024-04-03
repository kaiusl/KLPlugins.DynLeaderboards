namespace KLPlugins.DynLeaderboards {

    /// <summary>
    /// Booleans to tell which game we have. Since different games have different available data then we need to do alot of check like gameName == "...".
    /// The gameName is constant in each plugin reload and thus we can set it once and simplyfy game checks alot.
    /// </summary>
    public class Game {
        public const string AcName = "AssettoCorsa";
        public const string AccName = "AssettoCorsaCompetizione";
        public const string Rf2Name = "RFactor2";
        public const string IracingName = "IRacing";
        public const string R3eName = "RRRE";
        public const string AMS2Name = "Automobilista2";
        public const string LMUName = "LMU";

        public bool IsAc { get; } = false;
        public bool IsAcc { get; } = false;
        public bool IsRf2 { get; } = false;
        public bool IsIracing { get; } = false;
        public bool IsR3e { get; } = false;
        public bool IsAMS2 { get; } = false;
        public bool IsF120XX { get; } = false;
        public bool IsLMU { get; } = false;
        public bool IsUnknown { get; } = false;
        public string Name { get; }

        internal Game(string gameName) {
            this.Name = gameName;

            if (gameName.StartsWith("F120")) {
                this.IsF120XX = true;
                return;
            }

            switch (gameName) {
                case AcName:
                    this.IsAc = true;
                    break;
                case AccName:
                    this.IsAcc = true;
                    break;
                case Rf2Name:
                    this.IsRf2 = true;
                    break;
                case IracingName:
                    this.IsIracing = true;
                    break;
                case R3eName:
                    this.IsR3e = true;
                    break;
                case AMS2Name:
                    this.IsAMS2 = true;
                    break;
                case LMUName:
                    this.IsLMU = true;
                    break;
                default:
                    this.IsUnknown = true;
                    break;
            }
        }
    }
}