namespace KLPlugins.DynLeaderboards.Common;

/// <summary>
///     Booleans to tell which game we have. Since different games have different available data then we need to do a lot
///     of
///     check like gameName == "...".
///     The gameName is constant in each plugin reload, and thus we can set it once and simplify game checks a lot.
/// </summary>
public class Game {
    public const string AC_NAME = "AssettoCorsa";
    public const string ACC_NAME = "AssettoCorsaCompetizione";
    public const string RF2_NAME = "RFactor2";
    public const string IRACING_NAME = "IRacing";

    // ReSharper disable once InconsistentNaming
    public const string R3E_NAME = "RRRE";
    public const string AMS2_NAME = "Automobilista2";
    public const string LMU_NAME = "LMU";

    public bool IsAc { get; } = false;
    public bool IsAcc { get; } = false;
    public bool IsRf2 { get; } = false;
    public bool IsIracing { get; } = false;
    public bool IsR3E { get; } = false;
    public bool IsAms2 { get; } = false;
    public bool IsF120Xx { get; } = false;
    public bool IsLmu { get; } = false;
    public bool IsRf2OrLmu { get; } = false;
    public bool IsUnknown { get; } = false;
    public string Name { get; }

    public Game(string gameName) {
        this.Name = gameName;

        if (gameName.StartsWith("F120")) {
            this.IsF120Xx = true;
            return;
        }

        switch (gameName) {
            case Game.AC_NAME:
                this.IsAc = true;
                break;
            case Game.ACC_NAME:
                this.IsAcc = true;
                break;
            case Game.RF2_NAME:
                this.IsRf2 = true;
                break;
            case Game.IRACING_NAME:
                this.IsIracing = true;
                break;
            case Game.R3E_NAME:
                this.IsR3E = true;
                break;
            case Game.AMS2_NAME:
                this.IsAms2 = true;
                break;
            case Game.LMU_NAME:
                this.IsLmu = true;
                break;
            default:
                this.IsUnknown = true;
                break;
        }

        this.IsRf2OrLmu = this.IsRf2 || this.IsLmu;
    }
}