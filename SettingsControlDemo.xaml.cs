using System;
using System.Windows;
using System.Windows.Controls;
using SimHub.Plugins.UI;
using SimHub.Plugins.Styles;
using System.IO;
using System.Windows.Media;
using MdXaml;

namespace KLPlugins.Leaderboard
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public LeaderboardPlugin Plugin { get; }

        public SettingsControlDemo() {
            InitializeComponent();
        }

        public SettingsControlDemo(LeaderboardPlugin plugin) : this() {
            this.Plugin = plugin;

            // Add toggles to enable/disable properties
            AddCarToggles();
            AddDriverToggles();
            AddOrderingsToggles();
            AddOtherToggles();

            // Set current values for other settings
            CurrentDriverInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.ExposedCarProperties.Includes(ExposedCarProperties.CurrentDriverInfo);
            AllDriversInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.ExposedCarProperties.Includes(ExposedCarProperties.AllDriversInfo);
            AccDataLocation_TextBox.Text = LeaderboardPlugin.Settings.AccDataLocation;
            AccDataLocation_TextBox.Background = Brushes.LightGreen;
            NumOverallPos_NumericUpDown.Value = LeaderboardPlugin.Settings.NumOverallPos;
            NumRelativePos_NumericUpDown.Value = LeaderboardPlugin.Settings.NumRelativePos;
            NumDrivers_NumericUpDown.Value = LeaderboardPlugin.Settings.NumDrivers;
            UpdateInterval_NumericUpDown.Value = LeaderboardPlugin.Settings.BroadcastDataUpdateRateMs;
            Logging_ToggleButton.IsChecked = LeaderboardPlugin.Settings.Log;

            // Add listeners to drivers toggles
            CurrentDriverInfo_ToggleButton.Checked += CurrentDriverTbChecked;
            CurrentDriverInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<ExposedCarProperties>(sender, ee, LeaderboardPlugin.Settings.RemoveExposedProperty);
            AllDriversInfo_ToggleButton.Checked += AllDriverTbChecked;
            AllDriversInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<ExposedCarProperties>(sender, ee, LeaderboardPlugin.Settings.RemoveExposedProperty);

            
            AddPluginDescription();

            PluginInfoMarkdown.Markdown = File.ReadAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\README.md"); ;

        }

        private void AddPluginDescription() {
            ExposedOrderingsInfo_TextBlock.Text = @"Here you can select all other orderings like per class or relative to the focusd car.";

        }

        private void AddCarToggles() {

            ExposedCarProperties_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            foreach (var v in (ExposedCarProperties[])Enum.GetValues(typeof(ExposedCarProperties))) {
                if (v == ExposedCarProperties.None 
                    || v == ExposedCarProperties.AllDriversInfo 
                    || v == ExposedCarProperties.CurrentDriverInfo 
                ) continue;

                // Group by similarity
                if (v == ExposedCarProperties.Laps) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Lap information";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.CarNumber) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Car and team information";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.CurrentStintTime) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Stint information";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.DistanceToLeader) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Distances";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.GapToLeader) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Gaps";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.ClassPosition) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Positions";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.IsInPitLane) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Pit information";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedCarProperties.IsFinished) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Other";
                    ExposedCarProperties_StackPanel.Children.Add(stitle);
                }


                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.ExposedCarProperties.Includes(v),
                       (sender, e) => TbChanged<ExposedCarProperties>(sender, e, LeaderboardPlugin.Settings.AddExposedProperty),
                       (sender, e) => TbChanged<ExposedCarProperties>(sender, e, LeaderboardPlugin.Settings.RemoveExposedProperty),
                       v.ToolTipText()
                   );

                ExposedCarProperties_StackPanel.Children.Add(sp);
                ExposedCarProperties_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddDriverToggles() {
            ExposedDriverProperties_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            foreach (var v in (ExposedDriverProperties[])Enum.GetValues(typeof(ExposedDriverProperties))) {
                if (v == ExposedDriverProperties.None) continue;

                if (v == ExposedDriverProperties.FirstName) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Names";
                    ExposedDriverProperties_StackPanel.Children.Add(stitle);
                } else if (v == ExposedDriverProperties.Nationality) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Other";
                    ExposedDriverProperties_StackPanel.Children.Add(stitle);
                }


                var sp = CreateToggleRow(
                    v.ToString(), 
                    v.ToString(), 
                    LeaderboardPlugin.Settings.ExposedDriverProperties.Includes(v),
                    (sender, e) => TbChanged<ExposedDriverProperties>(sender, e, LeaderboardPlugin.Settings.AddExposedDriverProperty),
                    (sender, e) => TbChanged<ExposedDriverProperties>(sender, e, LeaderboardPlugin.Settings.RemoveExposedDriverProperty),
                    v.ToolTipText()
                );
                ExposedDriverProperties_StackPanel.Children.Add(sp);
                ExposedDriverProperties_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddOrderingsToggles() {
            ExposedOrderings_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            ExposedOrderings_StackPanel.Children.Add(CreateToggleSeparator());
            foreach (var v in (ExposedOrderings[])Enum.GetValues(typeof(ExposedOrderings))) {
                if (v == ExposedOrderings.None) continue;
  
                var sp = CreateToggleRow(
                    v.ToString(), 
                    v.ToPropName(), 
                    LeaderboardPlugin.Settings.ExposedOrderings.Includes(v),
                    (sender, e) => TbChanged<ExposedOrderings>(sender, e, LeaderboardPlugin.Settings.AddExposedOrdering),
                    (sender, e) => TbChanged<ExposedOrderings>(sender, e, LeaderboardPlugin.Settings.RemoveExposedOrdering),
                    v.ToolTipText()
                );

                ExposedOrderings_StackPanel.Children.Add(sp);
                ExposedOrderings_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddOtherToggles() {
            OtherProperties_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            OtherProperties_StackPanel.Children.Add(CreateToggleSeparator());
            foreach (var v in (ExposedGeneralProperties[])Enum.GetValues(typeof(ExposedGeneralProperties))) {
                if (v == ExposedGeneralProperties.None) continue;

                var sp = CreateToggleRow(
                    v.ToString(),
                    v.ToString(),
                    LeaderboardPlugin.Settings.ExposedGeneralProperties.Includes(v),
                    (sender, e) => TbChanged<ExposedGeneralProperties>(sender, e, LeaderboardPlugin.Settings.AddExposedGeneralProperty),
                    (sender, e) => TbChanged<ExposedGeneralProperties>(sender, e, LeaderboardPlugin.Settings.RemoveExposedGeneralProperty),
                    v.ToolTipText()
                );

                OtherProperties_StackPanel.Children.Add(sp);
                OtherProperties_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private StackPanel CreateTogglesDescriptionRow() {
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            var t1 = new TextBlock();
            t1.Width = 70;

            var t = new TextBlock();
            t.Text = "Property name";
            t.Width = 250;
            var t2 = new TextBlock();
            t2.Text = "Description";
            t2.Width = 500;
            t2.TextWrapping = TextWrapping.Wrap;

            sp.Children.Add(t1);
            sp.Children.Add(t);
            sp.Children.Add(t2);
            return sp;
        }

        /// <summary>
        /// Creates row to toggle property
        /// </summary>
        private StackPanel CreateToggleRow(string name, string displayName, bool isChecked, RoutedEventHandler check, RoutedEventHandler uncheck, string tooltip) {
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            var tb = new SHToggleButton();
            tb.Name = $"{name}_toggle";
            tb.IsChecked = isChecked;
            tb.Checked += check;
            tb.Unchecked += uncheck;
            tb.ToolTip = tooltip;

            var t = new TextBlock();
            t.Text = displayName;
            t.ToolTip = tooltip;
            t.Width = 250;
            var t2 = new TextBlock();
            t2.Text = tooltip;
            t2.Width = 500;
            t2.TextWrapping = TextWrapping.Wrap;

            sp.Children.Add(tb);
            sp.Children.Add(t);
            sp.Children.Add(t2);

            return sp;
        }
        
        /// <summary>
        /// Creates separator to insert between property toggle rows.
        /// </summary>
        private Separator CreateToggleSeparator() {
            var s = new Separator();
            s.Background = Brushes.LightGray;
            s.Height = 0.75;
            s.Margin = new Thickness(25, 0, 25, 0);
            return s;
        }

        /// <summary>
        /// Called when property toggle button changes.
        /// </summary>
        /// <typeparam name="TEnum">Flags enum type.</typeparam>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="removeProp">Function that handles adding or removing property from the flags enum.</param>
        private void TbChanged<TEnum>(object sender, RoutedEventArgs e, Action<TEnum> removeProp)
            where TEnum : struct 
        {
            Control ctrl = sender as Control;
            if (ctrl != null) {
                string name = ctrl.Name.Split('_')[0];
                if (Enum.TryParse(name, out TEnum newVariant)) {
                    removeProp(newVariant);
                } else {
                    LeaderboardPlugin.LogWarn($"Found unknown setting '{name}' in Properties");
                }

                LeaderboardPlugin.LogInfo($"Checked button {name}");
            }
        }

        private void CurrentDriverTbChecked(object sender, RoutedEventArgs e) {
            // Checked to only show current driver
            LeaderboardPlugin.Settings.RemoveExposedProperty(ExposedCarProperties.AllDriversInfo);
            AllDriversInfo_ToggleButton.IsChecked = false;

            LeaderboardPlugin.Settings.AddExposedProperty(ExposedCarProperties.CurrentDriverInfo);
        }

        private void AllDriverTbChecked(object sender, RoutedEventArgs e) {
            LeaderboardPlugin.Settings.RemoveExposedProperty(ExposedCarProperties.CurrentDriverInfo);
            CurrentDriverInfo_ToggleButton.IsChecked = false;

            LeaderboardPlugin.Settings.AddExposedProperty(ExposedCarProperties.AllDriversInfo);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            //foreach (var v in (ExposedProperties[])Enum.GetValues(typeof(ExposedProperties))) {


            //    var sp = new StackPanel();
            //    sp.Orientation = Orientation.Horizontal;
            //    var tb = new SHToggleButton();
            //    tb.Name = $"{v}_toggle";
            //    tb.Checked += Tb_Checked;
            //    tb.Unchecked += Tb_Unchecked;
            //    tb.IsChecked = (LeaderboardPlugin.Settings.ExposedProperties & v) != 0;
            //    var t = new TextBlock();
            //    t.Text = $"{v}";
            //    sp.Children.Add(tb);
            //    sp.Children.Add(t);

            //    ExposedPropertiesStackPanel.Children.Add(sp);
            //}


            //NumLapsToggle.IsChecked = LeaderboardPlugin.Settings.NumberOfLaps;
        }

        private void AccDataLocation_TextChanged(object sender, TextChangedEventArgs e) {
            var success = LeaderboardPlugin.Settings.SetAccDataLocation(AccDataLocation_TextBox.Text);
            if (success) {
                AccDataLocation_TextBox.Background = Brushes.LightGreen;
            } else {
                AccDataLocation_TextBox.Background = Brushes.LightPink;
            }
            
        }

        private void NumOverallPos_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) { 
                LeaderboardPlugin.Settings.NumOverallPos = (int)e.NewValue;
            }
        }

        private void NumRelativePos_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) {
                LeaderboardPlugin.Settings.NumRelativePos = (int)e.NewValue;
            }
        }

        private void NumDrivers_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) {
                LeaderboardPlugin.Settings.NumDrivers = (int)e.NewValue;
            }
        }

        private void UpdateRate_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) { 
                LeaderboardPlugin.Settings.BroadcastDataUpdateRateMs = (int)e.NewValue;
            }
        }

        private void Logging_ToggleButton_Click(object sender, RoutedEventArgs e) {
            LeaderboardPlugin.Settings.Log = !LeaderboardPlugin.Settings.Log;
        }
    }
}
