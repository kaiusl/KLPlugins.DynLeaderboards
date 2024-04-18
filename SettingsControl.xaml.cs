using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

using AcTools.Utils.Helpers;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;

using MahApps.Metro.Controls;

using SimHub.Plugins.Styles;

using WoteverCommon.WPF;

using Xceed.Wpf.Toolkit;

namespace KLPlugins.DynLeaderboards.Settings {

    class CarSettingsListBoxItem : ListBoxItem {

        public string Key { get; set; }
        public OverridableCarInfo CarInfo { get; set; }

        public CarSettingsListBoxItem(string key, OverridableCarInfo car) : base() {
            this.CarInfo = car;
            this.Key = key;

            this.Content = key;
        }
    }

    public partial class SettingsControl : UserControl {
        internal DynLeaderboardsPlugin Plugin { get; }
        internal PluginSettings Settings => DynLeaderboardsPlugin.Settings;
        internal DynLeaderboardConfig CurrentDynLeaderboardSettings { get; private set; }

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
            this.UpdateInterval_NumericUpDown.Value = this.Settings.BroadcastDataUpdateRateMs;
            this.AccDataLocation_TextBox.Background = this.Settings.IsAccDataLocationValid() ? Brushes.ForestGreen : Brushes.Crimson;
            this.AcRootLocation_TextBox.Background = this.Settings.IsAcRootLocationValid() ? Brushes.ForestGreen : Brushes.Crimson;
            this.Logging_ToggleButton.IsChecked = this.Settings.Log;
            this.IncludeST21InGT2_ToggleButton.IsChecked = this.Settings.Include_ST21_In_GT2;
            this.IncludeCHLInGT2_ToggleButton.IsChecked = this.Settings.Include_CHL_In_GT2;

            this.SetCarSettingsCarsList();
            this.AddClassColors();
        }

        void SetCarSettingsCarsList() {

            SHListBox list = this.CarSettingsCarsList_SHListBox;
            list.Items.Clear();

            foreach (var c in this.Plugin.Values.CarInfos) {
                var item = new CarSettingsListBoxItem(c.Key, c.Value);
                list.Items.Add(item);
            }

            list.SelectedIndex = 0;
            list.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Content", System.ComponentModel.ListSortDirection.Ascending));

            list.SelectionChanged += (sender, _) => {
                var item = (CarSettingsListBoxItem?)((ListBox)sender).SelectedItem;
                if (item != null) {
                    this.SetCarSettingsDetails(item.Key, item.CarInfo);
                } else {
                    ((StackPanel)this.CarSettings_StackPanel).Children.Clear();
                }
            };

            var first = this.Plugin.Values.CarInfos.FirstOr(null);

            if (first != null) {
                this.SetCarSettingsDetails(first.Value.Key, first.Value.Value);
            } else {
                ((StackPanel)this.CarSettings_StackPanel).Children.Clear();
            }
        }

        private ObservableCollection<string> _carClasses = new();
        void SetCarSettingsDetails(string key, OverridableCarInfo car) {
            // Go through all cars and check for class colors. 
            // If there are new classes then trying to Values.CarClassColors.Get will add them to the dictionary.
            foreach (var c in this.Plugin.Values.CarInfos) {
                var cls = c.Value.ClassDontCheckEnabled();
                if (cls != null) {
                    var _ = this.Plugin.Values.CarClassColors.Get(cls.Value);
                }
            }

            foreach (var c in this.Plugin.Values.CarClassColors) {
                if (!this._carClasses.Contains(c.Key.AsString())) {
                    this._carClasses.Add(c.Key.AsString());
                }
            }

            this._carClasses.Sort();

            var sp = this.CarSettings_StackPanel;
            sp.Children.Clear();

            var g1 = new Grid() {
                Margin = new Thickness(0, 5, 10, 5)
            };
            g1.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g1.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            var carTitle = new SHSubSectionTitle() { Margin = new Thickness(10, 10, 10, 10), FontSize = 20, Content = key };
            Grid.SetColumn(carTitle, 0);
            Grid.SetRow(carTitle, 0);
            g1.Children.Add(carTitle);

            var allResetButton = new SHButtonPrimary() {
                Padding = new Thickness(5),
                Margin = new Thickness(5, 0, 5, 0),
                Height = 26,
                Content = "Reset"
            };
            Grid.SetColumn(allResetButton, 1);
            Grid.SetRow(allResetButton, 0);
            g1.Children.Add(allResetButton);

            sp.Children.Add(g1);

            var g2 = new Grid() {
                Margin = new Thickness(10, 5, 10, 5)
            };
            g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            const double disabledOpacity = 0.25;

            TextBlock CreateLabelTextBox(string label, bool isEnabled, int row) {
                var block = new TextBlock() {
                    Text = label,
                    Padding = new Thickness(0, 0, 10, 0),
                    IsEnabled = isEnabled,
                    Opacity = isEnabled ? 1.0 : disabledOpacity
                };
                Grid.SetRow(block, row);
                Grid.SetColumn(block, 1);

                return block;
            }

            TextBox CreateEditTextBox(string? text, bool isEnabled, int row) {
                var textBox = new TextBox() {
                    Margin = new Thickness(0, 5, 0, 5),
                    Text = text ?? "",
                    IsEnabled = isEnabled,
                    Opacity = isEnabled ? 1 : disabledOpacity
                };
                Grid.SetColumn(textBox, 2);
                Grid.SetRow(textBox, row);

                return textBox;
            }

            SHButtonSecondary CreateResetButton(bool isEnabled, int row) {
                var button = new SHButtonSecondary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5),
                    Content = "Reset",
                    IsEnabled = isEnabled,
                    Opacity = isEnabled ? 1.0 : disabledOpacity
                };
                Grid.SetColumn(button, 3);
                Grid.SetRow(button, row);

                return button;
            }

            SHToggleButton CreateToggle(bool isEnabled, int row, string tooltip) {
                var toggle = new SHToggleButton() {
                    IsChecked = isEnabled,
                    ToolTip = tooltip
                };
                Grid.SetColumn(toggle, 0);
                Grid.SetRow(toggle, row);
                return toggle;
            }


            // Name row

            var isEnabled = car.IsNameEnabled;
            var row = 0;

            var nameToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this car name override. If disabled, the plugin will use the name provided by SimHub."
            );
            g2.Children.Add(nameToggle);

            var nameLabel = CreateLabelTextBox("Name", isEnabled, row);
            g2.Children.Add(nameLabel);

            var nameTextBox = CreateEditTextBox(car.NameDontCheckEnabled(), isEnabled, row);
            nameTextBox.TextChanged += (sender, b) => car.SetName(nameTextBox.Text);
            g2.Children.Add(nameTextBox);

            var nameResetButton = CreateResetButton(isEnabled, row);
            nameResetButton.Click += (sender, b) => {
                // Set the text before resetting, because it will trigger the TextChanged event and calls car.SetName
                nameTextBox.Text = car.BaseName();
                car.ResetName();
            };
            g2.Children.Add(nameResetButton);

            nameToggle.Checked += (sender, b) => {
                car.EnableName();
                nameLabel.IsEnabled = true;
                nameLabel.Opacity = 1;
                nameTextBox.IsEnabled = true;
                nameTextBox.Opacity = 1;
                nameResetButton.IsEnabled = true;
                nameResetButton.Opacity = 1;
            };
            nameToggle.Unchecked += (sender, b) => {
                car.DisableName();
                nameLabel.IsEnabled = false;
                nameLabel.Opacity = disabledOpacity;
                nameTextBox.IsEnabled = false;
                nameTextBox.Opacity = disabledOpacity;
                nameResetButton.IsEnabled = false;
                nameResetButton.Opacity = disabledOpacity;
            };


            // Manufacturer row

            row = 1;
            isEnabled = true;

            var manufacturerLabel = CreateLabelTextBox("Manufacturer", isEnabled, row);
            g2.Children.Add(manufacturerLabel);

            var manufacturerTextBox = CreateEditTextBox(car.Manufacturer(), isEnabled, row);
            manufacturerTextBox.TextChanged += (sender, b) => car.SetManufacturer(manufacturerTextBox.Text);
            g2.Children.Add(manufacturerTextBox);

            var manufacturerResetButton = CreateResetButton(isEnabled, row);
            manufacturerResetButton.Click += (sender, b) => {
                manufacturerTextBox.Text = car.BaseManufacturer(); // Must be set before resetting
                car.ResetManufacturer();
            };
            g2.Children.Add(manufacturerResetButton);

            sp.Children.Add(g2);


            // Class row

            isEnabled = car.IsClassEnabled;
            row = 2;

            var classToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this car class override. If disabled, the plugin will use the class provided by SimHub."
            );
            g2.Children.Add(classToggle);

            var classLabel = CreateLabelTextBox("Class", isEnabled, row);
            g2.Children.Add(classLabel);

            var classComboBox = new ComboBox() {
                IsReadOnly = false,
                IsEditable = true,
                ItemsSource = this._carClasses,
                SelectedItem = car.ClassDontCheckEnabled()?.AsString(),
                IsEnabled = isEnabled,
                Opacity = isEnabled ? 1.0 : disabledOpacity
            };

            Grid.SetColumn(classComboBox, 2);
            Grid.SetRow(classComboBox, row);
            classComboBox.LostFocus += (sender, b) => {
                var cls = (string?)classComboBox.Text;

                DynLeaderboardsPlugin.LogInfo("Selected class: " + cls);
                if (cls != null && cls != "") {
                    if (!this._carClasses.Contains(cls)) {
                        this._carClasses.Add(cls);
                        this._carClasses.Sort();
                    }
                    car.SetClass(new CarClass(cls));
                } else {
                    car.ResetClass();
                }
            };
            g2.Children.Add(classComboBox);

            var classResetButton = CreateResetButton(isEnabled, row);
            classResetButton.Click += (sender, b) => {
                classComboBox.SelectedItem = car.BaseClass()?.AsString(); // Must be set before resetting
                car.ResetClass();
            };
            g2.Children.Add(classResetButton);

            classToggle.Checked += (sender, b) => {
                car.EnableClass();
                classLabel.IsEnabled = true;
                classLabel.Opacity = 1;
                classComboBox.IsEnabled = true;
                classComboBox.Opacity = 1;
                classResetButton.IsEnabled = true;
                classResetButton.Opacity = 1;
            };
            classToggle.Unchecked += (sender, b) => {
                car.DisableClass();
                classLabel.IsEnabled = false;
                classLabel.Opacity = disabledOpacity;
                classComboBox.IsEnabled = false;
                classComboBox.Opacity = disabledOpacity;
                classResetButton.IsEnabled = false;
                classResetButton.Opacity = disabledOpacity;
            };


            allResetButton.Click += (sender, b) => {
                nameTextBox.Text = car.BaseName();
                manufacturerTextBox.Text = car.BaseManufacturer();
                classComboBox.SelectedItem = car.BaseClass()?.AsString();
                // Reset after setting text, because it will trigger the TextChanged event and calls car.Set which will set overrides
                car.Reset();
                classToggle.IsChecked = car.IsClassEnabled;
                nameToggle.IsChecked = car.IsNameEnabled;
            };
        }

        private void AddClassColors() {
            StackPanel sp = this.Colors_StackPanel;
            sp.Children.Clear();

            // Go through all cars and check for class colors. 
            // If there are new classes then trying to Values.CarClassColors.Get will add them to the dictionary.
            foreach (var car in this.Plugin.Values.CarInfos) {
                var cls = car.Value.ClassDontCheckEnabled();
                if (cls != null) {
                    var _ = this.Plugin.Values.CarClassColors.Get(cls.Value);
                }
            }

            var refreshButton = new SHButtonPrimary() {
                Content = "Refresh",
                ToolTip = "Refresh colors. This will check if new classes or categories have been added and will add them here for customization.",
                MaxWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            refreshButton.Click += (sender, e) => {
                this.AddClassColors();
            };
            sp.Children.Add(refreshButton);

            Grid CreateColorsGrid(string kind) {
                var grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), MinWidth = 75 });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Labels row

                TextBlock CreateLabel(string label, int col) {
                    var t = new TextBlock() {
                        Opacity = 0.75,
                        Margin = new Thickness(10, 0, 10, 5),
                        Text = label
                    };
                    Grid.SetColumn(t, col);
                    Grid.SetRow(t, 0);

                    return t;
                }
                grid.Children.Add(CreateLabel("Enabled", 0));
                grid.Children.Add(CreateLabel(kind, 1));
                grid.Children.Add(CreateLabel("Background", 2));
                grid.Children.Add(CreateLabel("Foreground", 4));

                return grid;
            }

            void CreateRow(Grid grid, int row, string name, OverridableTextBoxColor colors) {
                const float disabledOpacity = 0.25f;
                var isEnabled = colors.IsEnabled;
                var opacity = isEnabled ? 1.0 : disabledOpacity;

                var enabledToggle = new SHToggleButton() {
                    Margin = new Thickness(10, 0, 0, 0),
                    IsChecked = isEnabled
                };
                Grid.SetColumn(enabledToggle, 0);
                Grid.SetRow(enabledToggle, row);
                grid.Children.Add(enabledToggle);

                const string DEF_FOREGROUND = "#FFFFFF";
                const string DEF_BACKGROUND = "#000000";

                var currentBgColor = WindowsMediaColorExtensions.FromHex(colors.BackgroundDontCheckEnabled() ?? DEF_BACKGROUND);
                var currentFgColor = WindowsMediaColorExtensions.FromHex(colors.ForegroundDontCheckEnabled() ?? DEF_FOREGROUND);

                var classBox = new Border() {
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(currentBgColor),
                    Height = 25,
                    Margin = new Thickness(0, 0, 15, 0),
                    Opacity = opacity,
                    IsEnabled = isEnabled
                };
                Grid.SetColumn(classBox, 1);
                Grid.SetRow(classBox, row);

                var classText = new TextBlock() {
                    Padding = new Thickness(0, 0, 5, 0),
                    Foreground = new SolidColorBrush(currentFgColor),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = name
                };
                classBox.Child = classText;

                grid.Children.Add(classBox);


                var bgPicker = new ColorPicker() {
                    Width = 100,
                    Height = 20,
                    Margin = new Thickness(5, 0, 5, 0),
                    SelectedColor = currentBgColor,
                    Opacity = opacity,
                    IsEnabled = isEnabled
                };
                Grid.SetColumn(bgPicker, 2);
                Grid.SetRow(bgPicker, row);
                bgPicker.SelectedColorChanged += (sender, e) => {
                    colors.SetBackground(bgPicker.SelectedColor.ToString());
                    classBox.Background = new SolidColorBrush(bgPicker.SelectedColor.Value);
                };
                grid.Children.Add(bgPicker);

                var bgResetButton = new SHButtonSecondary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5, 5, 15, 5),
                    Content = "Reset",
                    Opacity = opacity,
                    IsEnabled = isEnabled
                };
                Grid.SetColumn(bgResetButton, 3);
                Grid.SetRow(bgResetButton, row);
                bgResetButton.Click += (sender, e) => {
                    bgPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(colors.BaseBackground() ?? DEF_BACKGROUND);
                    colors.ResetBackground();
                };
                grid.Children.Add(bgResetButton);

                var fgPicker = new ColorPicker() {
                    Width = 100,
                    Height = 20,
                    Margin = new Thickness(5, 0, 5, 0),
                    SelectedColor = currentFgColor,
                    Opacity = opacity,
                    IsEnabled = isEnabled
                };
                Grid.SetColumn(fgPicker, 4);
                Grid.SetRow(fgPicker, row);
                fgPicker.SelectedColorChanged += (sender, e) => {
                    colors.SetForeground(fgPicker.SelectedColor.ToString());
                    classText.Foreground = new SolidColorBrush(fgPicker.SelectedColor.Value);
                };
                grid.Children.Add(fgPicker);

                var fgResetButton = new SHButtonSecondary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5, 5, 15, 5),
                    Content = "Reset",
                    Opacity = opacity,
                    IsEnabled = isEnabled
                };
                Grid.SetColumn(fgResetButton, 5);
                Grid.SetRow(fgResetButton, row);
                fgResetButton.Click += (sender, e) => {
                    fgPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(colors.BaseForeground() ?? DEF_FOREGROUND);
                    colors.ResetForeground();
                };
                grid.Children.Add(fgResetButton);

                enabledToggle.Checked += (sender, e) => {
                    colors.Enable();
                    classBox.IsEnabled = true;
                    classBox.Opacity = 1.0;
                    bgPicker.IsEnabled = true;
                    bgPicker.Opacity = 1.0;
                    bgResetButton.IsEnabled = true;
                    bgResetButton.Opacity = 1.0;
                    fgPicker.IsEnabled = true;
                    fgPicker.Opacity = 1.0;
                    fgResetButton.IsEnabled = true;
                    fgResetButton.Opacity = 1.0;
                };

                enabledToggle.Unchecked += (sender, e) => {
                    var opacity = disabledOpacity;
                    colors.Disable();
                    classBox.IsEnabled = false;
                    classBox.Opacity = opacity;
                    bgPicker.IsEnabled = false;
                    bgPicker.Opacity = opacity;
                    bgResetButton.IsEnabled = false;
                    bgResetButton.Opacity = opacity;
                    fgPicker.IsEnabled = false;
                    fgPicker.Opacity = opacity;
                    fgResetButton.IsEnabled = false;
                    fgResetButton.Opacity = opacity;
                };
            }


            void AddColors<K>(string title, string kind, TextBoxColors<K> colors, Func<K, string> keyToString) {
                sp.Children.Add(new SHSectionTitle() { Text = title });

                var gameSpecificGrid = CreateColorsGrid(kind);
                sp.Children.Add(gameSpecificGrid);

                foreach (var (cls, i) in colors.WithIndex() ?? []) {
                    gameSpecificGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    CreateRow(gameSpecificGrid, i + 1, keyToString(cls.Key), cls.Value);
                }
            }

            AddColors("Car class colors", "Class", this.Plugin.Values.CarClassColors, k => k.AsString());
            sp.Children.Add(new SHSectionSeparator());
            AddColors("Team cup category colors", "Category", this.Plugin.Values.TeamCupCategoryColors, k => k.AsString());
            sp.Children.Add(new SHSectionSeparator());
            AddColors("Driver category colors", "Category", this.Plugin.Values.DriverCategoryColors, k => k.AsString());


        }

        #region General settings

        private void AddOtherToggles() {
            this.OtherProperties_StackPanel.Children.Clear();
            this.OtherProperties_StackPanel.Children.Add(this.CreatePropertyTogglesDescriptionRow());
            this.OtherProperties_StackPanel.Children.Add(this.CreateToggleSeparator());
            foreach (var v in OutGeneralPropExtensions.Order()) {
                if (v == OutGeneralProp.None) {
                    continue;
                }

                var sp = this.CreatePropertyToggleRow(
                    v.ToString(),
                    v.ToString(),
                    DynLeaderboardsPlugin.Settings.OutGeneralProps.Includes(v),
                    (sender, e) => DynLeaderboardsPlugin.Settings.OutGeneralProps.Combine(v),
                    (sender, e) => DynLeaderboardsPlugin.Settings.OutGeneralProps.Remove(v),
                    v.ToolTipText()
                );

                this.OtherProperties_StackPanel.Children.Add(sp);
                this.OtherProperties_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
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
                    this.EnablePropertiesDescription_TextBlock.Text = $"Enable/disable properties for currently selected dynamic leaderboard. Each property can be accessed as \"DynLeaderboardsPlugin.{nameBox.Text}.<pos>.<property name>\"";
                    this.DynLeaderboardPropertyAccess_TextBlock.Text = $"Properties for this dynamic leaderboard are accessible as \"DynLeaderboardsPlugin.{nameBox.Text}.<pos>.<property name>\", for example \"DynLeaderboardsPlugin.{nameBox.Text}.5.Car.Number\"";
                    this.ExposedDriverProps_TextBlock.Text = $"Properties for each driver car be accessed as \"DynLeaderboardsPlugin.{nameBox.Text}.<pos>.Driver.<driver number>.<property name>\", for example \"DynLeaderboardsPlugin.{nameBox.Text}.5.Driver.1.FirstName\"";
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

            this.EnablePropertiesDescription_TextBlock.Text = $"Enable/disable properties for currently selected dynamic leaderboard. Each properties car be accessed as \"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.Name}.5.<property name>\"";
            this.DynLeaderboardPropertyAccess_TextBlock.Text = "The toggle button in front of each leaderboard allows to disable calculations of given leaderboard. " +
                "This can be useful if you have many leaderboards but only use some of them at a time. " +
                "You can disable the ones not used at the moment in order to not waste resources. " +
                $"\n\nProperties for each leaderboard will be accessible as \"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.Name}.<pos>.<property name>\"" +
                $"for example \"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.Name}.5.Car.Number";
            this.ExposedDriverProps_TextBlock.Text = $"Properties for each driver car be accessed as \"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.Name}.<pos>.Driver.<driver number>.<property name>\", for example \"DynLeaderboardsPlugin.{this.CurrentDynLeaderboardSettings.Name}.5.Driver.1.FirstName\"";

            this.AddDynLeaderboardToggles();
            this.AddNumPositionsSetters();
            this.AddPropertyToggles();
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
                Source = CurrentDynLeaderboardSettings,
                Mode = BindingMode.TwoWay
            };
            num.SetBinding(NumericUpDown.ValueProperty, bind);

            sp.Children.Add(t);
            sp.Children.Add(num);

            return sp;
        }

        private void AddDynLeaderboardToggles() {
            this.DynLeaderboards_ListView.Items.Clear();
            // Add currently selected leaderboards
            foreach (var l in this.CurrentDynLeaderboardSettings.Order) {
                var sp = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    ToolTip = l.Tooltip()
                };

                var tb = new SHToggleButton {
                    Name = $"{l}_toggle_listview",
                    IsChecked = true
                };
                tb.Checked += (a, b) => this.CreateDynamicLeaderboardList();
                tb.Unchecked += (a, b) => this.CreateDynamicLeaderboardList();

                var t = new TextBlock {
                    Text = l.ToString()
                };

                sp.Children.Add(tb);
                sp.Children.Add(t);

                this.DynLeaderboards_ListView.Items.Add(sp);
            }

            // Add all others to the end
            foreach (var l in (Leaderboard[])Enum.GetValues(typeof(Leaderboard))) {
                if (l == Leaderboard.None || this.CurrentDynLeaderboardSettings.Order.Contains(l)) {
                    continue;
                }

                var sp = new StackPanel {
                    Orientation = Orientation.Horizontal
                };

                var tb = new SHToggleButton {
                    Name = $"{l}_toggle_listview",
                    IsChecked = false
                };
                tb.Checked += (a, b) => this.CreateDynamicLeaderboardList();
                tb.Unchecked += (a, b) => this.CreateDynamicLeaderboardList();
                //tb.ToolTip = tooltip;

                var t = new TextBlock {
                    Text = l.ToString()
                };

                sp.Children.Add(tb);
                sp.Children.Add(t);

                this.DynLeaderboards_ListView.Items.Add(sp);
            }
        }

        /// <summary>
        /// Create list of currently selected leaderboards for currently selected dynamic leaderboard
        /// </summary>
        private void CreateDynamicLeaderboardList() {
            var selected = this.SelectDynLeaderboard_ComboBox.SelectedIndex;
            var currentSelectedLeaderboard = this.Settings.DynLeaderboardConfigs[selected].CurrentLeaderboard();
            this.Settings.DynLeaderboardConfigs[selected].CurrentLeaderboardIdx = 0;
            this.Settings.DynLeaderboardConfigs[selected].Order.Clear();
            int i = 0;
            foreach (var v in this.DynLeaderboards_ListView.Items) {
                var sp = (StackPanel)v;
                var tb = (SHToggleButton)sp.Children[0];
                var txt = (TextBlock)sp.Children[1];
                if (tb.IsChecked == null || tb.IsChecked == false) {
                    continue;
                }

                if (Enum.TryParse(txt.Text, out Leaderboard variant)) {
                    this.Settings.DynLeaderboardConfigs[selected].Order.Add(variant);
                    if (variant == currentSelectedLeaderboard) { // Keep selected leaderboard as was, if that one was removed, set to first
                        this.Settings.DynLeaderboardConfigs[selected].CurrentLeaderboardIdx = i;
                    }

                    i++;
                }
            }
            //  this.Plugin.SetDynamicCarGetter(this.Settings.DynLeaderboardConfigs[selected]);
        }

        private void AddPropertyToggles() {
            this.OutCarProps_StackPanel.Children.Add(this.CreatePropertyTogglesDescriptionRow());
            this.AddPitToggles();
            this.AddPosToggles();
            this.AddGapToggles();
            this.AddStintToggles();
            this.AddLapToggles();
            this.AddCarToggles();
            this.AddDriverToggles();
        }

        private void AddCarToggles() {
            // Add Car properties
            this.OutCarProps_StackPanel.Children.Clear();
            this.OutOtherProps_StackPanel.Children.Clear();

            StackPanel panel = this.OutCarProps_StackPanel;
            foreach (var v in OutCarPropExtensions.Order()) {
                if (v == OutCarProp.None) {
                    continue;
                }

                if (v == OutCarProp.IsFinished) {
                    panel = this.OutOtherProps_StackPanel;
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutCarProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutCarProps.Remove(v),
                       v.ToolTipText()
                   );

                panel.Children.Add(sp);
                panel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddLapToggles() {
            // Add Lap Properties
            this.OutLapProps_StackPanel.Children.Clear();

            void AddSmallTitle(string name) {
                var t = new SHSmallTitle {
                    Content = name,
                    Margin = new Thickness(25, 0, 0, 0)
                };
                this.OutLapProps_StackPanel.Children.Add(t);
            }

            foreach (var v in OutLapPropExtensions.Order()) {
                if (v == OutLapProp.None) {
                    continue;
                }
                // Group by similarity
                switch (v) {
                    case OutLapProp.BestLapDeltaToOverallBest:
                        AddSmallTitle("Best to best");
                        break;

                    case OutLapProp.LastLapDeltaToOverallBest:
                        AddSmallTitle("Last to best");
                        break;

                    case OutLapProp.LastLapDeltaToLeaderLast:
                        AddSmallTitle("Last to last");
                        break;

                    default:
                        break;
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutLapProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutLapProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutLapProps.Remove(v),
                       v.ToolTipText()
                   );

                this.OutLapProps_StackPanel.Children.Add(sp);
                this.OutLapProps_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddStintToggles() {
            this.OutStintProps_StackPanel.Children.Clear();
            // Add Stint Properties
            foreach (var v in OutStintPropExtensions.Order()) {
                if (v == OutStintProp.None) {
                    continue;
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutStintProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutStintProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutStintProps.Remove(v),
                       v.ToolTipText()
                   );

                this.OutStintProps_StackPanel.Children.Add(sp);
                this.OutStintProps_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddGapToggles() {
            this.OutGapsProps_StackPanel.Children.Clear();
            // Add Gap Properties
            void AddSmallTitle(string name) {
                var t = new SHSmallTitle {
                    Content = name,
                    Margin = new Thickness(25, 0, 0, 0)
                };
                this.OutGapsProps_StackPanel.Children.Add(t);
            }

            foreach (var v in OutGapPropExtensions.Order()) {
                if (v == OutGapProp.None) {
                    continue;
                }

                if (v == OutGapProp.DynamicGapToFocused) {
                    AddSmallTitle("Dynamic gaps");
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutGapProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutGapProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutGapProps.Remove(v),
                       v.ToolTipText()
                   );

                this.OutGapsProps_StackPanel.Children.Add(sp);
                this.OutGapsProps_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddPosToggles() {
            this.OutPosProps_StackPanel.Children.Clear();
            // Add Pos Properties
            foreach (var v in OutPosPropExtensions.Order()) {
                if (v == OutPosProp.None) {
                    continue;
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutPosProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPosProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPosProps.Remove(v),
                       v.ToolTipText()
                   );

                this.OutPosProps_StackPanel.Children.Add(sp);
                this.OutPosProps_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddPitToggles() {
            this.OutPitProps_StackPanel.Children.Clear();
            // Add Pit Properties
            foreach (var v in OutPitPropExtensions.Order()) {
                if (v == OutPitProp.None) {
                    continue;
                }

                StackPanel sp = this.CreatePropertyToggleRow(
                       v.ToString(),
                       v.ToPropName(),
                       this.CurrentDynLeaderboardSettings.OutPitProps.Includes(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPitProps.Combine(v),
                       (sender, e) => this.CurrentDynLeaderboardSettings.OutPitProps.Remove(v),
                       v.ToolTipText()
                   );

                this.OutPitProps_StackPanel.Children.Add(sp);
                this.OutPitProps_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
        }

        private void AddDriverToggles() {
            this.ExposedDriverProperties_StackPanel.Children.Clear();
            this.ExposedDriverProperties_StackPanel.Children.Add(this.CreatePropertyTogglesDescriptionRow());
            foreach (var v in OutDriverPropExtensions.Order()) {
                if (v == OutDriverProp.None) {
                    continue;
                }

                if (v == OutDriverProp.FirstName) {
                    var stitle = new SHSmallTitle {
                        Content = "Names"
                    };
                    this.ExposedDriverProperties_StackPanel.Children.Add(stitle);
                } else if (v == OutDriverProp.Nationality) {
                    var stitle = new SHSmallTitle {
                        Content = "Other"
                    };
                    this.ExposedDriverProperties_StackPanel.Children.Add(stitle);
                }

                var sp = this.CreatePropertyToggleRow(
                    v.ToString(),
                    v.ToPropName(),
                    this.CurrentDynLeaderboardSettings.OutDriverProps.Includes(v),
                    (sender, e) => this.CurrentDynLeaderboardSettings.OutDriverProps.Combine(v),
                    (sender, e) => this.CurrentDynLeaderboardSettings.OutDriverProps.Remove(v),
                    v.ToolTipText()
                );
                this.ExposedDriverProperties_StackPanel.Children.Add(sp);
                this.ExposedDriverProperties_StackPanel.Children.Add(this.CreateToggleSeparator());
            }
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
        private StackPanel CreatePropertyToggleRow(
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

            return sp;
        }

        /// <summary>
        /// Creates separator to insert between property toggle rows.
        /// </summary>
        private Separator CreateToggleSeparator() {
            var s = new Separator {
                Background = Brushes.LightGray,
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

        private void UpdateACCarInfos_Button_Click(object sender, RoutedEventArgs e) {
            DynLeaderboardsPlugin.UpdateACCarInfos();
            this.Plugin.Values.UpdateCarInfos();
        }

        private void CarSettingsRefresh_Button_Click(object sender, RoutedEventArgs e) {
            this.SetCarSettingsCarsList();
        }
    }
}