using System.Windows.Controls;

namespace KLPlugins.Leaderboard
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public LeaderboardPlugin Plugin { get; }

        public SettingsControlDemo()
        {
            InitializeComponent();
        }

        public SettingsControlDemo(LeaderboardPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }


    }
}
