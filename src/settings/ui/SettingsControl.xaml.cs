using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using AcTools.Utils.Helpers;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;

using MahApps.Metro.Controls;

using MathNet.Numerics.Distributions;

using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    public class MessageDialog : SHDialogContentBase {
        public MessageDialog(string titleText, string msg) {
            this.ShowOk = true;

            var sp = new StackPanel();
            this.Content = sp;

            var title = new SHSectionTitle() {
                Text = titleText,
                Margin = new Thickness(0, 0, 0, 25)
            };

            sp.Children.Add(title);

            sp.Children.Add(new TextBlock() {
                Text = msg
            });
        }
    }


    public class ButtonMenuItem : MenuItem {
        public bool ShowDropDown {
            get => (bool)this.GetValue(ShowDropDownProperty);
            set => this.SetValue(ShowDropDownProperty, value);
        }
        public static readonly DependencyProperty ShowDropDownProperty =
                DependencyProperty.RegisterAttached("ShowDropDown", typeof(bool), typeof(ButtonMenuItem), new PropertyMetadata(false));
    }


    public class SectionTitle : UserControl {

        public string HelpPath {
            get => (string)this.GetValue(HelpPathProperty);
            set => this.SetValue(HelpPathProperty, value);
        }
        public static readonly DependencyProperty HelpPathProperty =
                DependencyProperty.RegisterAttached("HelpPath", typeof(string), typeof(SectionTitle), new PropertyMetadata("null"));
    }

    public class DocsHyperlink : Hyperlink {
        public string RelativePath {
            get => (string)this.GetValue(RelativePathProperty);
            set => this.SetValue(RelativePathProperty, value);
        }
        public static readonly DependencyProperty RelativePathProperty =
                DependencyProperty.RegisterAttached("RelativePath", typeof(string), typeof(DocsHyperlink), new PropertyMetadata(""));

        public DocsHyperlink() {
            this.RequestNavigate += (sender, e) => {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            };
        }
    }

    public class DocsHelpButton : UserControl {
        public string RelativePath {
            get => (string)this.GetValue(RelativePathProperty);
            set => this.SetValue(RelativePathProperty, value);
        }
        public static readonly DependencyProperty RelativePathProperty =
                DependencyProperty.RegisterAttached("RelativePath", typeof(string), typeof(DocsHelpButton), new PropertyMetadata(""));
    }

    public sealed class DocsPathConverter : IValueConverter {
#if DOCS_DEBUG
        public const string DOCS_ROOT = "http://127.0.0.1:8000/KLPlugins.DynLeaderboards/";
#else
        public const string DOCS_ROOT = "https://kaiusl.github.io/KLPlugins.DynLeaderboards/2.0.x/";
#endif

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                string fullPath = DOCS_ROOT + (string)value;
                return fullPath;
            } catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }

    public class SectionSeparator : Control { }

    public partial class SettingsControl : UserControl {
        internal DynLeaderboardsPlugin Plugin { get; }
        internal PluginSettings Settings => DynLeaderboardsPlugin.Settings;
        internal DynLeaderboardConfig CurrentDynLeaderboardSettings { get; private set; }
        internal CarSettingsTab CarSettingsTab { get; private set; }

        internal const double DISABLED_OPTION_OPACITY = 0.333;

        //internal SettingsControl() {
        //    InitializeComponent();
        //    DataContext = this;
        //}

        internal SettingsControl(DynLeaderboardsPlugin plugin) {
            this.InitializeComponent();
            this.DataContext = this;

            this.Plugin = plugin;

            if (this.Settings.DynLeaderboardConfigs.Count == 0) {
                this.Plugin.AddNewLeaderboard(new DynLeaderboardConfig("Dynamic"));
            }
            this.CurrentDynLeaderboardSettings = this.Settings.DynLeaderboardConfigs[0];

            foreach (var l in this.Settings.DynLeaderboardConfigs) {
                this.AddSelectDynLeaderboard_ComboBoxItem(l);
            }
            this.SelectDynLeaderboard_ComboBox.SelectedIndex = 0;

            this.AddDynLeaderboardSettings();
            this.AddOtherToggles();

            // Set current values for other settings
            this.AccDataLocation_TextBox.Text = this.Settings.AccDataLocation;
            this.AcRootLocation_TextBox.Text = this.Settings.AcRootLocation ?? "TODO";
            this.AccDataLocation_TextBox.Background = this.Settings.IsAccDataLocationValid() ? Brushes.ForestGreen : Brushes.Crimson;
            this.AcRootLocation_TextBox.Background = this.Settings.IsAcRootLocationValid() ? Brushes.ForestGreen : Brushes.Crimson;
            this.Logging_ToggleButton.IsChecked = this.Settings.Log;

            this.SetAllClassesAndManufacturers();
            this.CarSettingsTab = new CarSettingsTab(this, this.Plugin);
            this.CarSettingsTab.Build();
            new ClassSettingsTab(this, this.Plugin).Build();
            this.AddColorsTab();
        }

        internal ObservableCollection<string> AllClasses = new();
        internal ObservableCollection<string> AllManufacturers = new();

        /// <summary>
        /// Tries to add a new class but does nothing if the class already exists.
        /// </summary>
        internal void TryAddCarClass(CarClass cls) {
            var clsStr = cls.AsString();
            if (!this.AllClasses.Contains(clsStr)) {
                this.AllClasses.Add(clsStr);
            }
        }

        internal void AddCarManufacturer(string manufacturer) {
            if (!this.AllManufacturers.Contains(manufacturer)) {
                this.AllManufacturers.Add(manufacturer);
            }
        }

        internal async void DoOnConfirmation(Action action) {
            var dialogWindow = new ConfimDialog("Are you sure?", "All custom overrides will be lost.");
            var res = await dialogWindow.ShowDialogWindowAsync(this);

            switch (res) {
                case System.Windows.Forms.DialogResult.Yes:
                    action();
                    break;
                default:
                    break;

            };
        }

        void SetAllClassesAndManufacturers() {
            // Go through all cars and check for class colors. 
            // If there are new classes then trying to Values.CarClassColors.Get will add them to the dictionary.
            foreach (var c in this.Plugin.Values.CarInfos) {
                CarClass?[] classes = [c.Value.ClassDontCheckEnabled(), c.Value.BaseClass()];
                foreach (var cls in classes) {
                    if (cls != null) {
                        var info = this.Plugin.Values.ClassInfos.Get(cls.Value);
                        if (info.ReplaceWithDontCheckEnabled() != null) {
                            var _ = this.Plugin.Values.ClassInfos.Get(info.ReplaceWithDontCheckEnabled()!.Value);
                        }
                    }
                }

                string?[] manufacturers = [c.Value.Manufacturer(), c.Value.BaseManufacturer()];
                foreach (var manufacturer in manufacturers) {
                    if (manufacturer != null) {
                        this.AddCarManufacturer(manufacturer);
                    }
                }
            }

            foreach (var c in this.Plugin.Values.ClassInfos) {
                this.TryAddCarClass(c.Key);
            }
        }


        void AddColorsTab() {
            new ColorsTabSection<TeamCupCategory>(
                this,
                this.Plugin,
                "Category",
                this.Plugin.Values.TeamCupCategoryColors,
                this.ColorsTab_TeamCupCategoryColors_Menu,
                this.ColorsTab_TeamCupCategoryColors_Grid,
                this.Plugin.Values.UpdateTeamCupInfos
            ).Build(c => c == TeamCupCategory.Default);

            new ColorsTabSection<DriverCategory>(
                this,
                this.Plugin,
                "Category",
                this.Plugin.Values.DriverCategoryColors,
                this.ColorsTab_DriverCategoryColors_Menu,
                this.ColorsTab_DriverCategoryColors_Grid,
                this.Plugin.Values.UpdateDriverInfos
            ).Build(c => c == DriverCategory.Default);
        }

        #region General settings

        private void AddOtherToggles() {
            var sp = this.OtherProperties_StackPanel;

            sp.Children.Clear();

            var enableAllBtn = new SHButtonPrimary() { 
                Content = "Enable all",
                Height = 26
            };
            var disableAlLBtn = new SHButtonPrimary() {
                Content = "Disable all",
                Height = 26
            };

            var btnSp = new StackPanel() {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children = {
                    enableAllBtn,
                    disableAlLBtn
                }
            };
            sp.Children.Add(btnSp);

            sp.Children.Add(this.CreatePropertyTogglesDescriptionRow());
            sp.Children.Add(this.CreateToggleSeparator());


            List<SHToggleButton> toggles = new();
            foreach (var v in OutGeneralPropExtensions.Order()) {
                if (v == OutGeneralProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                    v.ToString(),
                    v.ToPropName(),
                    DynLeaderboardsPlugin.Settings.OutGeneralProps.Includes(v),
                    (sender, e) => DynLeaderboardsPlugin.Settings.OutGeneralProps.Combine(v),
                    (sender, e) => DynLeaderboardsPlugin.Settings.OutGeneralProps.Remove(v),
                    v.ToolTipText()
                );

                this.OtherProperties_StackPanel.Children.Add(row);
                this.OtherProperties_StackPanel.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }


            enableAllBtn.Click += (sender, e) => {
                foreach (var t in toggles) {
                    t.IsChecked = true;
                }
            };

            disableAlLBtn.Click += (sender, e) => {
                foreach (var t in toggles) {
                    t.IsChecked = false;
                }
            };
        }


        private void AccDataLocation_TextChanged(object sender, TextChangedEventArgs e) {
            var success = DynLeaderboardsPlugin.Settings.SetAccDataLocation(this.AccDataLocation_TextBox.Text);
            if (success) {
                this.AccDataLocation_TextBox.Background = Brushes.ForestGreen;
            } else {
                this.AccDataLocation_TextBox.Background = Brushes.Crimson;
            }
        }

        private void AcRootLocation_TextChanged(object sender, TextChangedEventArgs e) {
            var success = DynLeaderboardsPlugin.Settings.SetAcRootLocation(this.AcRootLocation_TextBox.Text);
            if (success) {
                this.AcRootLocation_TextBox.Background = Brushes.ForestGreen;
            } else {
                this.AcRootLocation_TextBox.Background = Brushes.Crimson;
            }
        }

        private void Logging_ToggleButton_Click(object sender, RoutedEventArgs e) {
            DynLeaderboardsPlugin.Settings.Log = !DynLeaderboardsPlugin.Settings.Log;
        }

        private void IncludeST21InGT2_ToggleButton_Click(object sender, RoutedEventArgs e) {
            DynLeaderboardsPlugin.Settings.Include_ST21_In_GT2 = !DynLeaderboardsPlugin.Settings.Include_ST21_In_GT2;
        }

        private void IncludeCHLInGT2_ToggleButton_Click(object sender, RoutedEventArgs e) {
            DynLeaderboardsPlugin.Settings.Include_CHL_In_GT2 = !DynLeaderboardsPlugin.Settings.Include_CHL_In_GT2;
        }

        private void SelectedColorChanged<T>(object _, RoutedPropertyChangedEventArgs<Color?> e, T c, Dictionary<T, string> settingsColors) {
            if (e.NewValue != null) {
                var newColor = (Color)e.NewValue;
                settingsColors[c] = newColor.ToString();
            }
        }

        // private void ClassColorPickerReset(CarClass cls) {
        //     DynLeaderboardsPlugin.Settings.CarClassColors[cls] = cls.ACCColor();
        //     this._classColorPickers[cls].SelectedColor = (Color)ColorConverter.ConvertFromString(cls.ACCColor());
        // }

        // private void TeamCupColorPickerReset(TeamCupCategory cup) {
        //     DynLeaderboardsPlugin.Settings.TeamCupCategoryColors[cup] = cup.ACCColor();
        //     this._cupColorPickers[cup].SelectedColor = (Color)ColorConverter.ConvertFromString(cup.ACCColor());
        // }

        // private void TeamCupTextColorPickerReset(TeamCupCategory cup) {
        //     DynLeaderboardsPlugin.Settings.TeamCupCategoryTextColors[cup] = cup.ACCTextColor();
        //     this._cupTextColorPickers[cup].SelectedColor = (Color)ColorConverter.ConvertFromString(cup.ACCTextColor());
        // }

        // private void DriverCategoryColorPickerReset(DriverCategory cls) {
        //     DynLeaderboardsPlugin.Settings.DriverCategoryColors[cls] = cls.GetAccColor();
        //     this._driverCategoryColorPickers[cls].SelectedColor = (Color)ColorConverter.ConvertFromString(cls.GetAccColor());
        // }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        #endregion General settings

        #region Dynamic leaderboard

        private void AddSelectDynLeaderboard_ComboBoxItem(DynLeaderboardConfig l) {
            var row = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            var nameBox = new TextBox {
                Width = 200,
                Text = l.Name
            };
            if (l.Name.Contains("CONFLICT")) {
                nameBox.Background = Brushes.LightPink;
            }

            nameBox.TextChanged += (sender, b) => {
                var caretIndex = nameBox.CaretIndex;

                // Remove any nonletter or digit characters
                char[] arr = nameBox.Text.ToCharArray();
                var arr2 = Array.FindAll(arr, (c => (char.IsLetterOrDigit(c))));
                if (arr.Length != arr2.Length) {
                    nameBox.Text = new string(arr2);
                    nameBox.CaretIndex = caretIndex - 1;
                }

                if (this.Settings.DynLeaderboardConfigs.Count(x => x.Name == nameBox.Text) > 1 || nameBox.Text.Contains("CONFLICT")) {
                    nameBox.Background = Brushes.LightPink;
                    nameBox.ToolTip = "Dynamic leaderboard with same name already exists. Please choose another, if you don't last valid name will be used.";
                } else {
                    nameBox.Background = Brushes.Transparent;
                    nameBox.ToolTip = null;
                }
            };

            nameBox.LostFocus += (a, b) => {
                if (nameBox.Background == Brushes.Transparent) {
                    l.Rename(nameBox.Text);
                } else {
                    nameBox.Text = l.Name;
                }
                nameBox.Text = l.Name;
                nameBox.Background = Brushes.Transparent;
                nameBox.ToolTip = null;
            };

            var selectText = new TextBlock {
                Text = "Select",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 0, 0, 0)
            };

            var isEnabled = new SHToggleButton {
                Name = $"{l.Name}_is_disabled_toggle",
                IsChecked = l.IsEnabled
            };
            isEnabled.Checked += (a, b) => l.IsEnabled = true;
            isEnabled.Unchecked += (a, b) => l.IsEnabled = false;

            row.Children.Add(isEnabled);
            row.Children.Add(nameBox);
            row.Children.Add(selectText);

            this.SelectDynLeaderboard_ComboBox.Items.Add(row);
        }

        /// <summary>
        /// Add settings for currently selected dynamic leaderboard.
        /// This is rerun if dynamic leaderboard selection is changed.
        /// This means that everything here should clear old items before adding new items.
        /// </summary>
        private void AddDynLeaderboardSettings() {
            // Technically we don't need to reset all setting UI items but only bindings and values.
            // But it's not critical and this is way simpler.

            this.AddDynLeaderboardToggles();
            this.AddControlsEditors();
            this.AddNumPositionsSetters();
            this.AddPropertyToggles();
        }

        private void AddControlsEditors() {
            var sp = this.DynamicLeaderboardsTab_Controls_StackPanel;
            sp.Children.Clear();

            var next = new ControlsEditor() {
                FriendlyName = "Next leaderboard",
                ActionName = $"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.NextLeaderboardActionName}",
            };

            sp.Children.Add(next);

            var prev = new ControlsEditor() {
                FriendlyName = "Previous leaderboard",
                ActionName = $"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.PreviousLeaderboardActionName}",
            };

            sp.Children.Add(prev);
        }

        /// <summary>
        /// Add all number of position row to the corresponding stack panel.
        /// </summary>
        private void AddNumPositionsSetters() {
            this.NumPositions_StackPanel.Children.Clear();

            void AddSmallTitle(string name) {
                var t = new SHSmallTitle {
                    Content = name
                };
                this.NumPositions_StackPanel.Children.Add(t);
            }

            AddSmallTitle("Overall leaderboard");

            this.NumPositions_StackPanel.Children.Add(
                this.CreateNumRow(
                    "Overall: ",
                    "Set number of overall positions exposed as properties.",
                    nameof(DynLeaderboardConfig.NumOverallPos),
                    0,
                    100,
                    1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Class: ",
                   "Set number of class positions exposed as properties. ",
                   nameof(DynLeaderboardConfig.NumClassPos),
                   0,
                   100,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Cup: ",
                   "Set number of cup positions exposed as properties. ",
                   nameof(DynLeaderboardConfig.NumCupPos),
                   0,
                   100,
                   1
                )
            );

            AddSmallTitle("Relative leaderboards");

            this.NumPositions_StackPanel.Children.Add(
                this.CreateNumRow(
                    "Overall: ",
                    "Set number of overall relative positions exposed from the focused car in one direction." +
                    " That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                    nameof(DynLeaderboardConfig.NumOverallRelativePos),
                    0,
                    50,
                    1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
                this.CreateNumRow(
                    "Class: ",
                    "Set number of class relative positions exposed from the focused car in one direction." +
                    " That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                    nameof(DynLeaderboardConfig.NumClassRelativePos),
                    0,
                    50,
                    1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
                this.CreateNumRow(
                    "Cup: ",
                    "Set number of class and cup relative positions exposed from the focused car in one direction." +
                    " That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                    nameof(DynLeaderboardConfig.NumCupRelativePos),
                    0,
                    50,
                    1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
                this.CreateNumRow(
                    "On track: ",
                    "Set number of on track relative positions exposed from the focused car in one direction. " +
                    "That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                    nameof(DynLeaderboardConfig.NumOnTrackRelativePos),
                    0,
                    50,
                    1
                )
            );

            AddSmallTitle("Partial relative leaderboards");
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Overall - top positions: ",
                   "Set number of overall positions exposed for partial relative overall leaderboard.",
                   nameof(DynLeaderboardConfig.PartialRelativeOverallNumOverallPos),
                   0,
                   100,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Overall - relative positions: ",
                   "Set number of relative positions exposed for partial relative overall " +
                   "leaderboard from the focused car in one direction. That is if it's set to 5, " +
                   "we show 5 cars ahead and 5 behind.",
                   nameof(DynLeaderboardConfig.PartialRelativeOverallNumRelativePos),
                   0,
                   50,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Class    - top positions: ",
                   "Set number of class positions exposed for partial relative class leaderboard.",
                   nameof(DynLeaderboardConfig.PartialRelativeClassNumClassPos),
                   0,
                   100,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Class    - relative positions: ",
                   "Set number of relative positions exposed for partial relative class " +
                   "leaderboard from the focused car in one direction. " +
                   "That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                   nameof(DynLeaderboardConfig.PartialRelativeClassNumRelativePos),
                   0,
                   50,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Cup      - top positions: ",
                   "Set number of cup positions exposed for partial relative class leaderboard.",
                   nameof(DynLeaderboardConfig.PartialRelativeCupNumCupPos),
                   0,
                   100,
                   1
                )
            );
            this.NumPositions_StackPanel.Children.Add(this.CreateToggleSeparator());
            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Cup      - relative positions: ",
                   "Set number of relative positions exposed for partial relative cup " +
                   "leaderboard from the focused car in one direction. " +
                   "That is if it's set to 5, we show 5 cars ahead and 5 behind.",
                   nameof(DynLeaderboardConfig.PartialRelativeCupNumRelativePos),
                   0,
                   50,
                   1
                )
            );

            AddSmallTitle("Drivers");

            this.NumPositions_StackPanel.Children.Add(
               this.CreateNumRow(
                   "Number of drivers",
                   "Set number of drivers shown per car. If set to 1 shown only current driver.",
                   nameof(DynLeaderboardConfig.NumDrivers),
                   0,
                   10,
                   1
                )
            );
        }

        /// <summary>
        /// Creates a row to set number of positions
        /// </summary>
        private StackPanel CreateNumRow(
            string name,
            string tooltip,
            string settingsPropertyName,
            int min,
            int max,
            int interval
        ) {
            var sp = new StackPanel {
                Orientation = Orientation.Horizontal,
                ToolTip = tooltip
            };

            var t = new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Width = 200,
                Margin = new Thickness(3, 3, 3, 3),
                Text = name
            };

            var num = new NumericUpDown {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 100,
                HasDecimals = false,
                Minimum = min,
                Maximum = max,
                Interval = interval,
                Margin = new Thickness(3, 3, 3, 3)
            };
            var bind = new Binding(settingsPropertyName) {
                Source = this.CurrentDynLeaderboardSettings,
                Mode = BindingMode.TwoWay
            };
            num.SetBinding(NumericUpDown.ValueProperty, bind);

            sp.Children.Add(t);
            sp.Children.Add(num);

            return sp;
        }

        class DynLeaderboardsListViewItem : StackPanel {
            internal SHToggleButton EnableToggleButton { get; private set; }
            internal TextBlock NameTextBox { get; private set; }
            internal SHToggleButton RemoveIfSingleClassToggleButton { get; private set; }
            internal SHToggleButton RemoveIfSingleCupToggleButton { get; private set; }
            internal Leaderboard Leaderboard { get; private set; }

            internal DynLeaderboardsListViewItem(Leaderboard leaderboard, bool isEnabled) {
                this.Leaderboard = leaderboard;

                var hsp = new StackPanel() {
                    Orientation = Orientation.Horizontal,
                    ToolTip = this.Leaderboard.Kind.Tooltip()
                };

                this.EnableToggleButton = new SHToggleButton {
                    Name = $"{this.Leaderboard.Kind}_toggle_listview",
                    IsChecked = isEnabled
                };

                this.NameTextBox = new TextBlock {
                    Text = this.Leaderboard.Kind.ToString(),
                    Width = 200
                };

                this.RemoveIfSingleClassToggleButton = new SHToggleButton() {
                    IsChecked = this.Leaderboard.RemoveIfSingleClass
                };
                this.RemoveIfSingleClassToggleButton.Checked += (a, b) => this.Leaderboard.RemoveIfSingleClass = true;
                this.RemoveIfSingleClassToggleButton.Unchecked += (a, b) => this.Leaderboard.RemoveIfSingleClass = false;

                this.RemoveIfSingleCupToggleButton = new SHToggleButton() {
                    IsChecked = this.Leaderboard.RemoveIfSingleCup
                };
                this.RemoveIfSingleCupToggleButton.Checked += (a, b) => this.Leaderboard.RemoveIfSingleCup = true;
                this.RemoveIfSingleCupToggleButton.Unchecked += (a, b) => this.Leaderboard.RemoveIfSingleCup = false;

                hsp.Children.Add(this.EnableToggleButton);
                hsp.Children.Add(this.NameTextBox);
                hsp.Children.Add(this.RemoveIfSingleClassToggleButton);
                hsp.Children.Add(this.RemoveIfSingleCupToggleButton);

                this.Children.Add(hsp);
                var sep = new Separator() {
                    Background = Brushes.DimGray
                };
                this.Children.Add(sep);
            }
        }

        private void AddDynLeaderboardToggles() {
            this.DynLeaderboards_ListView.Items.Clear();
            // Add currently selected leaderboards
            foreach (var l in this.CurrentDynLeaderboardSettings.Order) {
                var item = new DynLeaderboardsListViewItem(l, true);
                item.EnableToggleButton.Checked += (a, b) => this.CreateDynamicLeaderboardList();
                item.EnableToggleButton.Unchecked += (a, b) => this.CreateDynamicLeaderboardList();

                this.DynLeaderboards_ListView.Items.Add(item);
            }

            // Add all others to the end
            foreach (var l in (LeaderboardKind[])Enum.GetValues(typeof(LeaderboardKind))) {
                if (l == LeaderboardKind.None || this.CurrentDynLeaderboardSettings.Order.Contains(x => x.Kind == l)) {
                    continue;
                }

                var item = new DynLeaderboardsListViewItem(new Leaderboard(l), false);
                item.EnableToggleButton.Checked += (a, b) => this.CreateDynamicLeaderboardList();
                item.EnableToggleButton.Unchecked += (a, b) => this.CreateDynamicLeaderboardList();

                this.DynLeaderboards_ListView.Items.Add(item);
            }
        }

        /// <summary>
        /// Create list of currently selected leaderboards for currently selected dynamic leaderboard
        /// </summary>
        private void CreateDynamicLeaderboardList() {
            var selected = this.SelectDynLeaderboard_ComboBox.SelectedIndex;
            var selectedLeaderboardConfig = this.Settings.DynLeaderboardConfigs[selected];
            var currentSelectedLeaderboard = selectedLeaderboardConfig.CurrentLeaderboard().Kind;
            selectedLeaderboardConfig.CurrentLeaderboardIdx = 0;
            selectedLeaderboardConfig.Order.Clear();
            int i = 0;
            foreach (var v in this.DynLeaderboards_ListView.Items) {
                var item = (DynLeaderboardsListViewItem)v;
                if (item.EnableToggleButton.IsChecked == null || item.EnableToggleButton.IsChecked == false) {
                    continue;
                }

                selectedLeaderboardConfig.Order.Add(item.Leaderboard);
                if (item.Leaderboard.Kind == currentSelectedLeaderboard) { // Keep selected leaderboard as was, if that one was removed, set to first
                    selectedLeaderboardConfig.CurrentLeaderboardIdx = i;
                }
            }
            //  this.Plugin.SetDynamicCarGetter(this.Settings.DynLeaderboardConfigs[selected]);
        }

        private void AddPropertyToggles() {
            this.PropertiesForEachCar_StackPanel.Children.Clear();
            this.ExposedDriverProperties_StackPanel.Children.Clear();
            this.AddCarToggles();
            this.AddLapToggles();
            this.AddStintToggles();
            this.AddGapToggles();
            this.AddPosToggles();
            this.AddPitToggles();
            this.AddOtherToggles_Dynamic_leaderboards_tab();
            this.AddDriverToggles();
        }

        private class PropertyTogglesTitleRow : Border {
            internal SHButtonPrimary EnableBtn { get; private set; }
            internal SHButtonPrimary DisableBtn { get; private set; }

            internal PropertyTogglesTitleRow(string title) {
                this.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x37, 0x37, 0x37));
                this.CornerRadius = new CornerRadius(5);
                this.Margin = new Thickness(5);

                var dp = new DockPanel() { Margin = new Thickness(10, 0, 10, 0) };
                this.Child = dp;

                var titleBlock = new TextBlock() { Text = title };
                DockPanel.SetDock(titleBlock, Dock.Left);
                dp.Children.Add(titleBlock);

                this.EnableBtn = new SHButtonPrimary() { Content = "Enable all", HorizontalAlignment = HorizontalAlignment.Right, Height = 26 };
                this.DisableBtn = new SHButtonPrimary() { Content = "Disable all", HorizontalAlignment = HorizontalAlignment.Right, Height = 26 };

                DockPanel.SetDock(this.EnableBtn, Dock.Right);
                DockPanel.SetDock(this.DisableBtn, Dock.Right);

                dp.Children.Add(this.DisableBtn);
                dp.Children.Add(this.EnableBtn);
            }
        }

        private void AddCarToggles() {
            // Add Car properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Car information");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();
            foreach (var v in OutCarPropExtensions.OrderCarInformation()) {
                if (v == OutCarProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutCarProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            titleRow.EnableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = true;
                }
            };

            titleRow.DisableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = false;
                }
            };
        }

        private void AddLapToggles() {
            // Add Lap Properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Lap information");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            PropertyTogglesTitleRow AddSmallTitle(string name) {
                var t = new PropertyTogglesTitleRow(name);
                t.Margin = new Thickness(25, 5, 25, 5);
                sp.Children.Add(t);
                return t;
            }

            void AddEnableDisableButtonClicks(PropertyTogglesTitleRow titleRow, List<SHToggleButton> toggles) {
                titleRow.EnableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = true;
                    }
                };

                titleRow.DisableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = false;
                    }
                };
            }

            foreach (var v in OutLapPropExtensions.Order()) {
                if (v == OutLapProp.None) {
                    continue;
                }
                // Group by similarity
                switch (v) {
                    case OutLapProp.BestLapDeltaToOverallBest:
                        AddEnableDisableButtonClicks(titleRow, toggles);
                        toggles = new();
                        titleRow = AddSmallTitle("Best to best");
                        break;

                    case OutLapProp.LastLapDeltaToOverallBest:
                        AddEnableDisableButtonClicks(titleRow, toggles);
                        toggles = new();
                        titleRow = AddSmallTitle("Last to best");
                        break;

                    case OutLapProp.LastLapDeltaToLeaderLast:
                        AddEnableDisableButtonClicks(titleRow, toggles);
                        toggles = new();
                        titleRow = AddSmallTitle("Last to last");
                        break;

                    default:
                        break;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutLapProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutLapProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutLapProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());

                toggles.Add(toggle);
            }

            AddEnableDisableButtonClicks(titleRow, toggles);
        }

        private void AddStintToggles() {
            // Add Stint Properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Stint information");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            foreach (var v in OutStintPropExtensions.Order()) {
                if (v == OutStintProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutStintProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutStintProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutStintProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            titleRow.EnableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = true;
                }
            };

            titleRow.DisableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = false;
                }
            };
        }

        private void AddGapToggles() {
            // Add Gap Properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Gaps");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            PropertyTogglesTitleRow AddSmallTitle(string name) {
                var t = new PropertyTogglesTitleRow(name);
                t.Margin = new Thickness(25, 5, 25, 5);
                sp.Children.Add(t);
                return t;
            }

            void AddEnableDisableButtonClicks(PropertyTogglesTitleRow titleRow, List<SHToggleButton> toggles) {
                titleRow.EnableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = true;
                    }
                };

                titleRow.DisableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = false;
                    }
                };
            }

            foreach (var v in OutGapPropExtensions.Order()) {
                if (v == OutGapProp.None) {
                    continue;
                }

                if (v == OutGapProp.DynamicGapToFocused) {
                    AddEnableDisableButtonClicks(titleRow, toggles);
                    toggles = new();
                    titleRow = AddSmallTitle("Dynamic gaps");
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutGapProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutGapProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutGapProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            AddEnableDisableButtonClicks(titleRow, toggles);
        }

        private void AddPosToggles() {
            // Add Pos Properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Positions");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            foreach (var v in OutPosPropExtensions.Order()) {
                if (v == OutPosProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutPosProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPosProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPosProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            titleRow.EnableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = true;
                }
            };

            titleRow.DisableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = false;
                }
            };
        }

        private void AddPitToggles() {
            // Add Pit Properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Pit information");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            foreach (var v in OutPitPropExtensions.Order()) {
                if (v == OutPitProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutPitProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPitProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPitProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            titleRow.EnableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = true;
                }
            };

            titleRow.DisableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = false;
                }
            };
        }

        private void AddOtherToggles_Dynamic_leaderboards_tab() {
            // Add Car properties
            var sp = this.PropertiesForEachCar_StackPanel;

            var titleRow = new PropertyTogglesTitleRow("Other");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();
            foreach (var v in OutCarPropExtensions.OrderOther()) {
                if (v == OutCarProp.None) {
                    continue;
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutCarProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Remove(v),
                       v.ToolTipText()
                   );

                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            titleRow.EnableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = true;
                }
            };

            titleRow.DisableBtn.Click += (sender, e) => {
                foreach (var v in toggles) {
                    v.IsChecked = false;
                }
            };
        }

        private void AddDriverToggles() {
            var sp = this.ExposedDriverProperties_StackPanel;
            sp.Children.Add(this.CreatePropertyTogglesDescriptionRow());

            var titleRow = new PropertyTogglesTitleRow("Names");
            sp.Children.Add(titleRow);

            List<SHToggleButton> toggles = new();

            PropertyTogglesTitleRow AddSmallTitle(string name) {
                var t = new PropertyTogglesTitleRow(name);
                t.Margin = new Thickness(25, 5, 25, 5);
                sp.Children.Add(t);
                return t;
            }

            void AddEnableDisableButtonClicks(PropertyTogglesTitleRow titleRow, List<SHToggleButton> toggles) {
                titleRow.EnableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = true;
                    }
                };

                titleRow.DisableBtn.Click += (sender, e) => {
                    foreach (var v in toggles) {
                        v.IsChecked = false;
                    }
                };
            }

            foreach (var v in OutDriverPropExtensions.Order()) {
                if (v == OutDriverProp.None) {
                    continue;
                }

                if (v == OutDriverProp.Nationality) {
                    AddEnableDisableButtonClicks(titleRow, toggles);
                    toggles = new();
                    titleRow = AddSmallTitle("Other");
                }

                var (row, toggle) = this.CreatePropertyToggleRow(
                    v.ToString(),
                    v.ToPropName(),
                    this.CurrentDynLeaderboardSettings.OutDriverProps.Includes(v),
                    (sender, e) => this.CurrentDynLeaderboardSettings.OutDriverProps.Combine(v),
                    (sender, e) => this.CurrentDynLeaderboardSettings.OutDriverProps.Remove(v),
                    v.ToolTipText()
                );
                sp.Children.Add(row);
                sp.Children.Add(this.CreateToggleSeparator());
                toggles.Add(toggle);
            }

            AddEnableDisableButtonClicks(titleRow, toggles);
        }

        private StackPanel CreatePropertyTogglesDescriptionRow() {
            var sp = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            var t1 = new TextBlock {
                Width = 70
            };

            var t = new TextBlock {
                Text = "Property name",
                Width = 250
            };
            var t2 = new TextBlock {
                Text = "Description",
                MaxWidth = 750,
                TextWrapping = TextWrapping.Wrap
            };

            sp.Children.Add(t1);
            sp.Children.Add(t);
            sp.Children.Add(t2);
            return sp;
        }

        /// <summary>
        /// Creates row to toggle property
        /// </summary>
        private (StackPanel, SHToggleButton) CreatePropertyToggleRow(
            string name,
            string displayName,
            bool isChecked,
            RoutedEventHandler checkHandler,
            RoutedEventHandler uncheckHandler,
            string tooltip
        ) {
            var sp = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            var tb = new SHToggleButton {
                Name = $"{name}_toggle",
                IsChecked = isChecked
            };
            tb.Checked += checkHandler;
            tb.Unchecked += uncheckHandler;
            tb.ToolTip = tooltip;

            var t = new TextBlock {
                Text = displayName,
                ToolTip = tooltip,
                Width = 250
            };
            var t2 = new TextBlock {
                Text = tooltip,
                MaxWidth = 500,
                TextWrapping = TextWrapping.Wrap
            };

            sp.Children.Add(tb);
            sp.Children.Add(t);
            sp.Children.Add(t2);

            return (sp, tb);
        }

        /// <summary>
        /// Creates separator to insert between property toggle rows.
        /// </summary>
        private Separator CreateToggleSeparator() {
            var s = new Separator {
                Background = Brushes.DimGray,
                Height = 1,
                Margin = new Thickness(25, 0, 25, 0)
            };
            return s;
        }

        // From https://stackoverflow.com/questions/12540457/moving-an-item-up-and-down-in-a-wpf-list-box
        private void DynLeaderboard_ListView_Up(object sender, RoutedEventArgs e) {
            var selectedIndex = this.DynLeaderboards_ListView.SelectedIndex;

            if (selectedIndex > 0) {
                var itemToMoveUp = this.DynLeaderboards_ListView.Items[selectedIndex];
                this.DynLeaderboards_ListView.Items.RemoveAt(selectedIndex);
                this.DynLeaderboards_ListView.Items.Insert(selectedIndex - 1, itemToMoveUp);
                this.DynLeaderboards_ListView.SelectedIndex = selectedIndex - 1;
            }

            this.CreateDynamicLeaderboardList();
        }

        // From https://stackoverflow.com/questions/12540457/moving-an-item-up-and-down-in-a-wpf-list-box
        private void DynLeaderboard_ListView_Down(object sender, RoutedEventArgs e) {
            var selectedIndex = this.DynLeaderboards_ListView.SelectedIndex;

            if (selectedIndex + 1 < this.DynLeaderboards_ListView.Items.Count) {
                var itemToMoveDown = this.DynLeaderboards_ListView.Items[selectedIndex];
                this.DynLeaderboards_ListView.Items.RemoveAt(selectedIndex);
                this.DynLeaderboards_ListView.Items.Insert(selectedIndex + 1, itemToMoveDown);
                this.DynLeaderboards_ListView.SelectedIndex = selectedIndex + 1;
            }

            this.CreateDynamicLeaderboardList();
        }

        private void AddNewLeaderboard_Button_Click(object sender, RoutedEventArgs e) {
            var nameNum = 1;
            while (this.Settings.DynLeaderboardConfigs.Any(x => x.Name == $"Dynamic{nameNum}")) {
                nameNum++;
            }

            var cfg = new DynLeaderboardConfig($"Dynamic{nameNum}");
            this.AddSelectDynLeaderboard_ComboBoxItem(cfg);
            this.Settings.DynLeaderboardConfigs.Add(cfg);
            this.Plugin.AddNewLeaderboard(cfg);
            this.SelectDynLeaderboard_ComboBox.SelectedIndex = this.SelectDynLeaderboard_ComboBox.Items.Count - 1;

        }

        private void RemoveLeaderboard_ButtonClick(object sender, RoutedEventArgs e) {
            if (this.SelectDynLeaderboard_ComboBox.Items.Count == 1) {
                return;
            }

            int selected = this.SelectDynLeaderboard_ComboBox.SelectedIndex;
            if (selected == 0) {
                this.SelectDynLeaderboard_ComboBox.SelectedIndex++;
            } else {
                this.SelectDynLeaderboard_ComboBox.SelectedIndex--;
            }

            this.SelectDynLeaderboard_ComboBox.Items.RemoveAt(selected);
            this.Plugin.RemoveLeaderboardAt(selected);
        }

        private void SelectDynLeaderboard_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int selected = this.SelectDynLeaderboard_ComboBox.SelectedIndex;
            this.CurrentDynLeaderboardSettings = this.Settings.DynLeaderboardConfigs[selected];

            this.AddDynLeaderboardSettings();
        }

        #endregion Dynamic leaderboard

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) {
                this.Settings.BroadcastDataUpdateRateMs = (int)e.NewValue;
            }
        }
    }
}