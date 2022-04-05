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
            AddPluginDescription();
            AddToggles();

            // Set current values for other settings
            CurrentDriverInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.OutDriverProps.Includes(OutDriverProp.CurrentDriverInfo);
            AllDriversInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.OutDriverProps.Includes(OutDriverProp.AllDriversInfo);
            AccDataLocation_TextBox.Text = LeaderboardPlugin.Settings.AccDataLocation;
            AccDataLocation_TextBox.Background = Brushes.LightGreen;
            NumOverallPos_NumericUpDown.Value = LeaderboardPlugin.Settings.NumOverallPos;
            NumRelativePos_NumericUpDown.Value = LeaderboardPlugin.Settings.NumRelativePos;
            NumDrivers_NumericUpDown.Value = LeaderboardPlugin.Settings.NumDrivers;
            UpdateInterval_NumericUpDown.Value = LeaderboardPlugin.Settings.BroadcastDataUpdateRateMs;
            Logging_ToggleButton.IsChecked = LeaderboardPlugin.Settings.Log;

            // Add listeners to drivers toggles
            CurrentDriverInfo_ToggleButton.Checked += CurrentDriverTbChecked;
            CurrentDriverInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<OutDriverProp>(sender, ee, (o) => LeaderboardPlugin.Settings.OutDriverProps.Remove(o));
            AllDriversInfo_ToggleButton.Checked += AllDriverTbChecked;
            AllDriversInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<OutDriverProp>(sender, ee, (o) => LeaderboardPlugin.Settings.OutDriverProps.Remove(o));
        }

        private void AddPluginDescription() {
            ExposedOrderingsInfo_TextBlock.Text = @"Here you can select all other orderings like per class or relative to the focusd car.";
            PluginInfoMarkdown.Markdown = File.ReadAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\README.md"); ;
        }

        private void AddToggles() {
            OutCarProps_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            AddPitToggles();
            AddPosToggles();
            AddGapToggles();
            AddDistanceToggles();
            AddStintToggles();
            AddLapToggles();
            AddCarToggles();
            AddDriverToggles();
            AddOrderingsToggles();
            AddOtherToggles();
        }

        private void AddCarToggles() {
            // Add Car properties
            StackPanel panel = OutCarProps_StackPanel;
            foreach (var v in (OutCarProp[])Enum.GetValues(typeof(OutCarProp))) {
                if (v == OutCarProp.None) continue;

                if (v == OutCarProp.IsFinished) panel = OutOtherProps_StackPanel;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutCarProps.Includes(v),
                       (sender, e) => TbChanged<OutCarProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutCarProps.Combine(o)),
                       (sender, e) => TbChanged<OutCarProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutCarProps.Remove(o)),
                       v.ToolTipText()
                   );

                panel.Children.Add(sp);
                panel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddLapToggles() {
            // Add Lap Properties
            foreach (var v in (OutLapProp[])Enum.GetValues(typeof(OutLapProp))) {
                if (v == OutLapProp.None) continue;

                void AddSmallTitle(string name) {
                    var t = new SHSmallTitle();
                    t.Content = name;
                    OutLapProps_StackPanel.Children.Add(t);
                }

                // Group by similarity
                switch (v) {
                    case OutLapProp.BestLapDeltaToOverallBest:
                        AddSmallTitle("Best lap deltas");
                        break;
                    case OutLapProp.LastLapDeltaToOverallBest:
                        AddSmallTitle("Last lap deltas");
                        break;
                    default:
                        break;
                }

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutLapProps.Includes(v),
                       (sender, e) => TbChanged<OutLapProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutLapProps.Combine(o)),
                       (sender, e) => TbChanged<OutLapProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutLapProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutLapProps_StackPanel.Children.Add(sp);
                OutLapProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddStintToggles() {
            // Add Stint Properties
            foreach (var v in (OutStintProp[])Enum.GetValues(typeof(OutStintProp))) {
                if (v == OutStintProp.None) continue;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutStintProps.Includes(v),
                       (sender, e) => TbChanged<OutStintProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutStintProps.Combine(o)),
                       (sender, e) => TbChanged<OutStintProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutStintProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutStintProps_StackPanel.Children.Add(sp);
                OutStintProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddDistanceToggles() {
            // Add Distance Properties
            foreach (var v in (OutDistanceProp[])Enum.GetValues(typeof(OutDistanceProp))) {
                if (v == OutDistanceProp.None) continue;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutDistanceProps.Includes(v),
                       (sender, e) => TbChanged<OutDistanceProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutDistanceProps.Combine(o)),
                       (sender, e) => TbChanged<OutDistanceProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutDistanceProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutDistancesProps_StackPanel.Children.Add(sp);
                OutDistancesProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddGapToggles() {
            // Add Gap Properties
            foreach (var v in (OutGapProp[])Enum.GetValues(typeof(OutGapProp))) {
                if (v == OutGapProp.None) continue;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutGapProps.Includes(v),
                       (sender, e) => TbChanged<OutGapProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutGapProps.Combine(o)),
                       (sender, e) => TbChanged<OutGapProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutGapProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutGapsProps_StackPanel.Children.Add(sp);
                OutGapsProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddPosToggles() {
            // Add Pos Properties
            foreach (var v in (OutPosProp[])Enum.GetValues(typeof(OutPosProp))) {
                if (v == OutPosProp.None) continue;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutPosProps.Includes(v),
                       (sender, e) => TbChanged<OutPosProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutPosProps.Combine(o)),
                       (sender, e) => TbChanged<OutPosProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutPosProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutPosProps_StackPanel.Children.Add(sp);
                OutPosProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddPitToggles() {
            // Add Pit Properties
            foreach (var v in (OutPitProp[])Enum.GetValues(typeof(OutPitProp))) {
                if (v == OutPitProp.None) continue;

                StackPanel sp = CreateToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       LeaderboardPlugin.Settings.OutPitProps.Includes(v),
                       (sender, e) => TbChanged<OutPitProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutPitProps.Combine(o)),
                       (sender, e) => TbChanged<OutPitProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutPitProps.Remove(o)),
                       v.ToolTipText()
                   );

                OutPitProps_StackPanel.Children.Add(sp);
                OutPitProps_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddDriverToggles() {
            ExposedDriverProperties_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            foreach (var v in (OutDriverProp[])Enum.GetValues(typeof(OutDriverProp))) {
                if (v == OutDriverProp.None || v == OutDriverProp.AllDriversInfo || v == OutDriverProp.CurrentDriverInfo) continue;

                if (v == OutDriverProp.FirstName) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Names";
                    ExposedDriverProperties_StackPanel.Children.Add(stitle);
                } else if (v == OutDriverProp.Nationality) {
                    var stitle = new SHSmallTitle();
                    stitle.Content = "Other";
                    ExposedDriverProperties_StackPanel.Children.Add(stitle);
                }


                var sp = CreateToggleRow(
                    v.ToString(), 
                    v.ToString(), 
                    LeaderboardPlugin.Settings.OutDriverProps.Includes(v),
                    (sender, e) => TbChanged<OutDriverProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutDriverProps.Combine(o)),
                    (sender, e) => TbChanged<OutDriverProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutDriverProps.Remove(o)),
                    v.ToolTipText()
                );
                ExposedDriverProperties_StackPanel.Children.Add(sp);
                ExposedDriverProperties_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddOrderingsToggles() {
            ExposedOrderings_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            ExposedOrderings_StackPanel.Children.Add(CreateToggleSeparator());
            foreach (var v in (OutOrder[])Enum.GetValues(typeof(OutOrder))) {
                if (v == OutOrder.None) continue;
  
                var sp = CreateToggleRow(
                    v.ToString(), 
                    v.ToPropName(), 
                    LeaderboardPlugin.Settings.OutOrders.Includes(v),
                    (sender, e) => TbChanged<OutOrder>(sender, e, (o) => LeaderboardPlugin.Settings.OutOrders.Combine(o)),
                    (sender, e) => TbChanged<OutOrder>(sender, e, (o) => LeaderboardPlugin.Settings.OutOrders.Remove(o)),
                    v.ToolTipText()
                );

                ExposedOrderings_StackPanel.Children.Add(sp);
                ExposedOrderings_StackPanel.Children.Add(CreateToggleSeparator());
            }
        }

        private void AddOtherToggles() {
            OtherProperties_StackPanel.Children.Add(CreateTogglesDescriptionRow());
            OtherProperties_StackPanel.Children.Add(CreateToggleSeparator());
            foreach (var v in (OutGeneralProp[])Enum.GetValues(typeof(OutGeneralProp))) {
                if (v == OutGeneralProp.None) continue;

                var sp = CreateToggleRow(
                    v.ToString(),
                    v.ToString(),
                    LeaderboardPlugin.Settings.OutGeneralProps.Includes(v),
                    (sender, e) => TbChanged<OutGeneralProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutGeneralProps.Combine(o)),
                    (sender, e) => TbChanged<OutGeneralProp>(sender, e, (o) => LeaderboardPlugin.Settings.OutGeneralProps.Remove(o)),
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
        private StackPanel CreateToggleRow(string name, string displayName, bool isChecked, RoutedEventHandler checkHandler, RoutedEventHandler uncheckHandler, string tooltip) {
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            var tb = new SHToggleButton();
            tb.Name = $"{name}_toggle";
            tb.IsChecked = isChecked;
            tb.Checked += checkHandler;
            tb.Unchecked += uncheckHandler;
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
            LeaderboardPlugin.Settings.OutDriverProps.Remove(OutDriverProp.AllDriversInfo);
            AllDriversInfo_ToggleButton.IsChecked = false;

            LeaderboardPlugin.Settings.OutDriverProps.Combine(OutDriverProp.CurrentDriverInfo);
        }

        private void AllDriverTbChecked(object sender, RoutedEventArgs e) {
            LeaderboardPlugin.Settings.OutDriverProps.Remove(OutDriverProp.CurrentDriverInfo);
            CurrentDriverInfo_ToggleButton.IsChecked = false;

            LeaderboardPlugin.Settings.OutDriverProps.Combine(OutDriverProp.AllDriversInfo);
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
