using System;
using System.Windows;
using System.Windows.Controls;
using SimHub.Plugins.UI;
using SimHub.Plugins.Styles;
using System.IO;
using System.Windows.Media;
using MdXaml;
using System.Windows.Data;
using MahApps.Metro.Controls;
using Xceed.Wpf.Toolkit;
using KLPlugins.Leaderboard.Enums;
using System.Collections.Generic;
using KLPlugins.Leaderboard.ksBroadcastingNetwork;

namespace KLPlugins.Leaderboard
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public LeaderboardPlugin Plugin { get; }
        public PluginSettings Settings { get => LeaderboardPlugin.Settings; }

        private Dictionary<CarClass, ColorPicker> _classColorPickers = new Dictionary<CarClass, ColorPicker>(8);
        private Dictionary<TeamCupCategory, ColorPicker> _cupColorPickers = new Dictionary<TeamCupCategory, ColorPicker>(5);
        private Dictionary<TeamCupCategory, ColorPicker> _cupTextColorPickers = new Dictionary<TeamCupCategory, ColorPicker>(5);
        private Dictionary<DriverCategory, ColorPicker> _driverCategoryColorPickers = new Dictionary<DriverCategory, ColorPicker>(4);

        public SettingsControlDemo() {
            InitializeComponent();
            DataContext = this;
        }

        public SettingsControlDemo(LeaderboardPlugin plugin) : this() {
            this.Plugin = plugin;
            AddPluginDescription();
            AddToggles();
            AddColors();

            // Set current values for other settings
            CurrentDriverInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.OutDriverProps.Includes(OutDriverProp.CurrentDriverInfo);
            AllDriversInfo_ToggleButton.IsChecked = LeaderboardPlugin.Settings.OutDriverProps.Includes(OutDriverProp.AllDriversInfo);
            AccDataLocation_TextBox.Text = LeaderboardPlugin.Settings.AccDataLocation;
            AccDataLocation_TextBox.Background = Brushes.LightGreen;
            Logging_ToggleButton.IsChecked = LeaderboardPlugin.Settings.Log;

            // Add listeners to drivers toggles
            CurrentDriverInfo_ToggleButton.Checked += CurrentDriverTbChecked;
            CurrentDriverInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<OutDriverProp>(sender, ee, (o) => LeaderboardPlugin.Settings.OutDriverProps.Remove(o));
            AllDriversInfo_ToggleButton.Checked += AllDriverTbChecked;
            AllDriversInfo_ToggleButton.Unchecked += (sender, ee) => TbChanged<OutDriverProp>(sender, ee, (o) => LeaderboardPlugin.Settings.OutDriverProps.Remove(o));
        }

        #region Add ui items
        private void AddPluginDescription() {
            ExposedOrderingsInfo_TextBlock.Text = @"Here you can select all other orderings like per class or relative to the focusd car.";
            PluginInfoMarkdown.Markdown = File.ReadAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\README.md"); ;
        }

        private void AddColors() {
            AddClassColors();
            AddTeamCupColors();
            AddDriverCategoryColors();
        }

        private void AddClassColors() {

            foreach (var c in Enum.GetValues(typeof(CarClass))) {
                var cls = (CarClass)c;
                if (cls == CarClass.Unknown || cls == CarClass.Overall) continue;
                
                var sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                
                var t = new TextBlock();
                t.Text = cls.ToString() + ": ";
                t.Width = 60;
                
                var cp = new ColorPicker();
                cp.Width = 100;
                cp.Height = 25;
                cp.SelectedColor = (Color)ColorConverter.ConvertFromString(LeaderboardPlugin.Settings.CarClassColors[cls]);
                cp.SelectedColorChanged += (sender, e) => SelectedColorChanged(sender, e, cls, LeaderboardPlugin.Settings.CarClassColors);

                _classColorPickers.Add(cls, cp);

                var btn = new SHButtonPrimary();
                btn.Content = "Reset";
                btn.Click += (sender, e) => ClassColorPickerReset(cls);
                btn.Height = 25;

                sp.Children.Add(t);
                sp.Children.Add(cp);
                sp.Children.Add(btn);

                ClassColors_StackPanel.Children.Add(sp);
            }
        }

        private void AddTeamCupColors() {
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            var t = new TextBlock();
            t.Text = "Background";
            t.Width = 190;
            t.Margin = new Thickness(65, 0, 0, 0);
            var t2 = new TextBlock();
            t2.Text = "Text";
            t2.Width = 100;

            sp.Children.Add(t);
            sp.Children.Add(t2);
            TeamCupColors_StackPanel.Children.Add(sp);


            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                var cup = (TeamCupCategory)c;

                sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;

                t = new TextBlock();
                t.Text = cup.ToString() + ": ";
                t.Width = 60;

                var cp1 = new ColorPicker();
                cp1.Width = 100;
                cp1.Height = 25;
                cp1.SelectedColor = (Color)ColorConverter.ConvertFromString(LeaderboardPlugin.Settings.TeamCupCategoryColors[cup]);
                cp1.SelectedColorChanged += (sender, e) => SelectedColorChanged(sender, e, cup, LeaderboardPlugin.Settings.TeamCupCategoryColors);
                _cupColorPickers.Add(cup, cp1);

                var btn1 = new SHButtonPrimary();
                btn1.Content = "Reset";
                btn1.Click += (sender, e) => TeamCupColorPickerReset(cup);
                btn1.Height = 25;


                var cp2 = new ColorPicker();
                cp2.Margin = new Thickness(25, 0, 0, 0);
                cp2.Width = 100;
                cp2.Height = 25;
                cp2.SelectedColor = (Color)ColorConverter.ConvertFromString(LeaderboardPlugin.Settings.TeamCupCategoryTextColors[cup]);
                cp2.SelectedColorChanged += (sender, e) => SelectedColorChanged(sender, e, cup, LeaderboardPlugin.Settings.TeamCupCategoryTextColors);
                _cupTextColorPickers.Add(cup, cp2);

                var btn2 = new SHButtonPrimary();
                btn2.Content = "Reset";
                btn2.Click += (sender, e) => TeamCupTextColorPickerReset(cup);
                btn2.Height = 25;

                sp.Children.Add(t);
                sp.Children.Add(cp1);
                sp.Children.Add(btn1);
                sp.Children.Add(cp2);
                sp.Children.Add(btn2);

                TeamCupColors_StackPanel.Children.Add(sp);
            }
        }

        private void AddDriverCategoryColors() {

            foreach (var c in Enum.GetValues(typeof(DriverCategory))) {
                var cls = (DriverCategory)c;
                if (cls == DriverCategory.Error) continue;

                var sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;

                var t = new TextBlock();
                t.Text = cls.ToString() + ": ";
                t.Width = 60;

                var cp = new ColorPicker();
                cp.Width = 100;
                cp.Height = 25;
                cp.SelectedColor = (Color)ColorConverter.ConvertFromString(LeaderboardPlugin.Settings.DriverCategoryColors[cls]);
                cp.SelectedColorChanged += (sender, e) => SelectedColorChanged(sender, e, cls, LeaderboardPlugin.Settings.DriverCategoryColors);

                _driverCategoryColorPickers.Add(cls, cp);

                var btn = new SHButtonPrimary();
                btn.Content = "Reset";
                btn.Click += (sender, e) => DriverCategoryColorPickerReset(cls);
                btn.Height = 25;

                sp.Children.Add(t);
                sp.Children.Add(cp);
                sp.Children.Add(btn);

                DriverCategoryColors_StackPanel.Children.Add(sp);
            }
        }

        private void SelectedColorChanged<T>(object sender, RoutedPropertyChangedEventArgs<Color?> e, T c, Dictionary<T, string> settingsColors) {
            if (e.NewValue != null) {
                var newColor = (Color)e.NewValue;
                settingsColors[c] = newColor.ToString();
            }
        }


        private void ClassColorPickerReset(CarClass cls) {
            LeaderboardPlugin.Settings.CarClassColors[cls] = cls.GetACCColor();
            _classColorPickers[cls].SelectedColor = (Color)ColorConverter.ConvertFromString(cls.GetACCColor());
        }

        private void TeamCupColorPickerReset(TeamCupCategory cup) {
            LeaderboardPlugin.Settings.TeamCupCategoryColors[cup] = cup.GetACCColor();
            _cupColorPickers[cup].SelectedColor = (Color)ColorConverter.ConvertFromString(cup.GetACCColor());
        }

        private void TeamCupTextColorPickerReset(TeamCupCategory cup) {
            LeaderboardPlugin.Settings.TeamCupCategoryTextColors[cup] = cup.GetACCColor();
            _cupTextColorPickers[cup].SelectedColor = (Color)ColorConverter.ConvertFromString(cup.GetACCTextColor());
        }

        private void DriverCategoryColorPickerReset(DriverCategory cls) {
            LeaderboardPlugin.Settings.DriverCategoryColors[cls] = cls.GetAccColor();
            _driverCategoryColorPickers[cls].SelectedColor = (Color)ColorConverter.ConvertFromString(cls.GetAccColor());
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

            void AddSmallTitle(string name) {
                var t = new SHSmallTitle();
                t.Content = name;
                ExposedOrderings_StackPanel.Children.Add(t);
            }

            OutOrder[] order = new OutOrder[] {
                OutOrder.InClassPositions,
                OutOrder.RelativeOverallPositions,
                OutOrder.RelativeClassPositions,
                OutOrder.RelativeOnTrackPositions,
                OutOrder.PartialRelativeOverallPositions,
                OutOrder.PartialRelativeClassPositions,
                OutOrder.FocusedCarPosition,
                OutOrder.OverallBestLapPosition,
                OutOrder.InClassBestLapPosition,
            };

            foreach (var v in order) {
                if (v == OutOrder.None) continue;

                switch (v) { 
                    case OutOrder.InClassPositions:
                        AddSmallTitle("Overall leaderboards");
                        break;
                    case OutOrder.RelativeOverallPositions:
                        AddSmallTitle("Relative leaderboards");
                        break;
                    case OutOrder.PartialRelativeOverallPositions:
                        AddSmallTitle("Partial relative leaderboards");
                        break;
                    case OutOrder.FocusedCarPosition:
                        AddSmallTitle("Single positions");
                        break;
                    default:
                        break;
                }

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

        #endregion

        #region Callbacks
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


        private void AccDataLocation_TextChanged(object sender, TextChangedEventArgs e) {
            var success = LeaderboardPlugin.Settings.SetAccDataLocation(AccDataLocation_TextBox.Text);
            if (success) {
                AccDataLocation_TextBox.Background = Brushes.LightGreen;
            } else {
                AccDataLocation_TextBox.Background = Brushes.LightPink;
            }
            
        }

        private void Logging_ToggleButton_Click(object sender, RoutedEventArgs e) {
            LeaderboardPlugin.Settings.Log = !LeaderboardPlugin.Settings.Log;
        }


        #endregion

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
