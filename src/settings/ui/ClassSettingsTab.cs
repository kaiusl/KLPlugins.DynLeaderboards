
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.Car;

using SimHub.Plugins.Styles;

using Xceed.Wpf.Toolkit;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    internal class ClassSettingsTab {
        private class ClassSettingsListBoxItem : ListBoxItem {

            public CarClass Key { get; set; }
            public OverridableClassInfo ClassInfo { get; set; }
            public Border Border { get; set; }
            public TextBlock ClassText { get; set; }

            public ClassSettingsListBoxItem(CarClass key, OverridableClassInfo cls) : base() {
                this.ClassInfo = cls;
                this.Key = key;

                this.ClassText = new TextBlock() {
                    Text = this.Key.AsString(),
                    Foreground = new SolidColorBrush(WindowsMediaColorExtensions.FromHex(this.ClassInfo.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG)),
                    // Margin = new Thickness(15, 5, 10, 5),
                    Padding = new Thickness(5, 0, 5, 0),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                this.Border = new Border() {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(5),
                    Height = 25,
                    Margin = new Thickness(5, 0, 5, 0),
                    Child = this.ClassText,
                    Background = new SolidColorBrush(WindowsMediaColorExtensions.FromHex(this.ClassInfo.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG)),
                };

                this.Content = this.Border;
            }
        }

        private class ClassSettingsListBoxItemComparer : IComparer {
            public int Compare(object x, object y) {
                var xKey = ((ClassSettingsListBoxItem)x).Key;
                var yKey = ((ClassSettingsListBoxItem)y).Key;
                return xKey.CompareTo(yKey);
            }
        }

        DockPanel _dockPanel { get; set; }
        Menu _menu { get; set; }
        ListBox _classesList { get; set; }
        StackPanel _detailsStackPanel { get; set; }
        SettingsControl _settingsControl { get; set; }
        DynLeaderboardsPlugin _plugin { get; set; }
        readonly ObservableCollection<ClassSettingsListBoxItem> _carClassesListBoxItems = [];



        internal ClassSettingsTab(SettingsControl settingsControl, DynLeaderboardsPlugin plugin) {
            this._settingsControl = settingsControl;
            this._plugin = plugin;

            this._dockPanel = this._settingsControl.ClassSettings_DockPanel;
            this._menu = this._settingsControl.ClassSettingsTab_Menu;
            this._classesList = this._settingsControl.ClassSettingsClassesList_SHListBox;
            this._detailsStackPanel = this._settingsControl.ClassSettings_StackPanel;

            this._classesList.Items.Clear();
            this._classesList.ItemsSource = new ListCollectionView(this._carClassesListBoxItems) {
                IsLiveSorting = true,
                CustomSort = new ClassSettingsListBoxItemComparer()
            };
            this._classesList.SelectionChanged += (sender, _) => {
                var item = (ClassSettingsListBoxItem?)((ListBox)sender).SelectedItem;
                if (item != null) {
                    this.BuildDetails(item);
                } else {
                    ((StackPanel)this._detailsStackPanel).Children.Clear();
                }
            };
        }

        internal void Build() {
            this.BuildMenu();
            this.BuildItems();
        }

        void BuildMenu() {
            var resetMenu = new ButtonMenuItem() {
                Header = "Reset",
                ShowDropDown = true,
            };
            this._menu.Items.Add(resetMenu);

            var resetMenuResetAll = new MenuItem() {
                Header = "Reset all",
            };

            resetMenu.Items.Add(resetMenuResetAll);
            resetMenuResetAll.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.Reset();
                    }
                    this.BuildItems();
                });
            };

            var resetMenuResetColors = new MenuItem() {
                Header = "Reset all colors",
            };
            resetMenu.Items.Add(resetMenuResetColors);
            resetMenuResetColors.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.ResetColors();
                    }
                    this.BuildItems();
                });
            };

            var resetMenuResetBackgroundColors = new MenuItem() {
                Header = "Reset all background colors",
            };
            resetMenu.Items.Add(resetMenuResetBackgroundColors);
            resetMenuResetBackgroundColors.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.ResetBackground();
                    }
                    this.BuildItems();
                });
            };

            var resetMenuResetForegroundColors = new MenuItem() {
                Header = "Reset all text colors"
            };
            resetMenu.Items.Add(resetMenuResetForegroundColors);
            resetMenuResetForegroundColors.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.ResetForeground();
                    }
                    this.BuildItems();
                });
            };

            var resetMenuResetSameAs = new MenuItem() {
                Header = "Reset all same as values",
            };
            resetMenu.Items.Add(resetMenuResetSameAs);
            resetMenuResetSameAs.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.ResetSameAs();
                    }
                    this.BuildItems();
                });
            };

            var disableMenu = new ButtonMenuItem() {
                Header = "Disable",
                ShowDropDown = true,
            };
            this._menu.Items.Add(disableMenu);

            var disableAll = new MenuItem() {
                Header = "Disable all",
            };
            disableMenu.Items.Add(disableAll);
            disableAll.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.DisableColor();
                        c.Value.DisableSameAs();
                    }
                    this.BuildItems();
                });
            };

            var disableAllColors = new MenuItem() {
                Header = "Disable all colors",
            };
            disableMenu.Items.Add(disableAllColors);
            disableAllColors.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.DisableColor();
                    }
                    this.BuildItems();
                });
            };

            var disableAllSameAs = new MenuItem() {
                Header = "Disable all same as values",
            };
            disableMenu.Items.Add(disableAllSameAs);
            disableAllSameAs.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.DisableSameAs();
                    }
                    this.BuildItems();
                });
            };

            var enableMenu = new ButtonMenuItem() {
                Header = "Enable",
                ShowDropDown = true,
            };
            this._menu.Items.Add(enableMenu);

            var enableAll = new MenuItem() {
                Header = "Enable all",
            };
            enableMenu.Items.Add(enableAll);
            enableAll.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.EnableColor();
                        c.Value.EnableSameAs();
                    }
                    this.BuildItems();
                });
            };

            var enableAllColors = new MenuItem() {
                Header = "Enable all colors",
            };
            enableMenu.Items.Add(enableAllColors);
            enableAllColors.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.EnableColor();
                    }
                    this.BuildItems();
                });
            };

            var enableAllSameAs = new MenuItem() {
                Header = "Enable all same as values",
            };
            enableMenu.Items.Add(enableAllSameAs);
            enableAllSameAs.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.EnableSameAs();
                    }
                    this.BuildItems();
                });
            };

            var deletaAllBtn = new ButtonMenuItem() {
                Header = "Remove all"
            };
            deletaAllBtn.ToolTip = "Remove all classes from the settings file. Note that if the class has base data it will be reset and disabled, but not completely deleted.";
            deletaAllBtn.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    var classes = this._plugin.Values.ClassInfos.Select(kv => kv.Key).ToList();
                    foreach (var c in classes) {
                        this._plugin.Values.ClassInfos.Remove(c);
                    }
                    this.BuildItems();
                });
            };
            this._menu.Items.Add(deletaAllBtn);

            var refreshBtn = new ButtonMenuItem() {
                Header = "Refresh"
            };
            refreshBtn.ToolTip = "Refresh classes list. This will check if new classes have been added and will add them here for customization.";
            refreshBtn.Click += (_, _) => this.BuildItems();
            this._menu.Items.Add(refreshBtn);
        }


        void BuildItems() {
            this._carClassesListBoxItems.Clear();
            foreach (var c in this._plugin.Values.ClassInfos) {
                var item = new ClassSettingsListBoxItem(c.Key, c.Value);
                this._carClassesListBoxItems.Add(item);
            }

            this._classesList.SelectedIndex = 0;
            var first = (ClassSettingsListBoxItem)this._classesList.SelectedItem;
            if (first != null) {
                this.BuildDetails(first);
            } else {
                ((StackPanel)this._detailsStackPanel).Children.Clear();
            }
        }

        void BuildDetails(ClassSettingsListBoxItem listItem) {
            var key = listItem.Key;
            var cls = listItem.ClassInfo;

            this._detailsStackPanel.Children.Clear();

            var currentBgColor = WindowsMediaColorExtensions.FromHex(cls.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG);
            var currentFgColor = WindowsMediaColorExtensions.FromHex(cls.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG);

            var titleRow = new Grid() {
                Margin = new Thickness(0, 5, 10, 5)
            };
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            var titleText = new TextBlock() {
                Foreground = new SolidColorBrush(currentFgColor),
                FontSize = 20,
                Margin = new Thickness(20, 5, 15, 5),
                Style = (Style)this._settingsControl.FindResource("ColorGrid_LabelText"),
                Text = key.AsString()
            };
            var titleBox = new Border() {
                MinHeight = 35,
                Margin = new Thickness(10, 10, 10, 10),
                Background = new SolidColorBrush(currentBgColor),
                Style = (Style)this._settingsControl.FindResource("ColorGrid_LabelBorder"),
                Child = titleText
            };
            Grid.SetColumn(titleBox, 0);
            Grid.SetRow(titleBox, 0);
            titleRow.Children.Add(titleBox);

            var allResetButton = new SHButtonPrimary() {
                Padding = new Thickness(5),
                Margin = new Thickness(5, 0, 5, 0),
                Height = 26,
                Content = "Reset"
            };
            Grid.SetColumn(allResetButton, 2);
            Grid.SetRow(allResetButton, 0);
            titleRow.Children.Add(allResetButton);
            this._detailsStackPanel.Children.Add(titleRow);

            var deleteButton = new SHButtonPrimary() {
                Padding = new Thickness(5),
                Margin = new Thickness(5, 0, 5, 0),
                Height = 26,
                Content = "Remove",
                ToolTip = "Removes the selected car. Note that if the car has base data, it will only be reset and disabled but not completely deleted."
            };
            Grid.SetColumn(deleteButton, 3);
            Grid.SetRow(deleteButton, 0);
            titleRow.Children.Add(deleteButton);

            var settingsGrid = new Grid() {
                Margin = new Thickness(10, 5, 10, 5)
            };
            this._detailsStackPanel.Children.Add(settingsGrid);
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            settingsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            settingsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            settingsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            settingsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            TextBlock CreateLabelTextBox(string label, bool isEnabled, int row) {
                var block = new TextBlock() {
                    Text = label,
                    Padding = new Thickness(0, 0, 10, 0),
                    IsEnabled = isEnabled,
                    Opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY
                };
                Grid.SetRow(block, row);
                Grid.SetColumn(block, 1);

                return block;
            }

            SHButtonSecondary CreateResetButton(int row) {
                var button = new SHButtonSecondary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5),
                    Height = 26,
                    Content = "Reset"
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


            //// Color row

            var isEnabled = cls.IsColorEnabled;
            var opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;
            var row = 0;

            var colorToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this class' color override. If disabled, the plugin will use the colors provided by SimHub."
            );
            settingsGrid.Children.Add(colorToggle);

            var colorLabel = CreateLabelTextBox("Color", isEnabled, row);
            settingsGrid.Children.Add(colorLabel);

            var colorsStackpanel = new StackPanel() {
                Margin = new Thickness(0, 5, 0, 5),
                Orientation = Orientation.Horizontal,
                IsEnabled = isEnabled,
                Opacity = opacity
            };
            Grid.SetColumn(colorsStackpanel, 2);
            Grid.SetRow(colorsStackpanel, row);
            settingsGrid.Children.Add(colorsStackpanel);

            colorsStackpanel.Children.Add(new TextBlock() {
                Padding = new Thickness(0, 0, 0, 0),
                Text = "Background"
            });

            var bgColorPicker = new ColorPicker() {
                SelectedColor = currentBgColor,
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ColorPicker"),
            };
            bgColorPicker.SelectedColorChanged += (sender, e) => {
                var color = bgColorPicker.SelectedColor ?? WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_BG);
                titleBox.Background = new SolidColorBrush(color);
                listItem.Border.Background = new SolidColorBrush(color);
                cls.SetBackground(color.ToString());
            };
            colorsStackpanel.Children.Add(bgColorPicker);

            var bgColorResetButton = new SHButtonSecondary() {
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ResetButton"),
            };
            void ResetBgColor() {
                var baseColor = cls.BaseBackground();
                if (baseColor == null) {
                    bgColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_BG);
                } else {
                    bgColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(baseColor);
                }
                cls.ResetBackground();
                colorToggle.IsChecked = cls.IsColorEnabled;
            }
            bgColorResetButton.Click += (sender, b) => ResetBgColor();
            colorsStackpanel.Children.Add(bgColorResetButton);

            colorsStackpanel.Children.Add(new TextBlock() {
                Padding = new Thickness(25, 0, 0, 0),
                Text = "Text"
            });

            var textColorPicker = new ColorPicker() {
                SelectedColor = currentFgColor,
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ColorPicker"),
            };
            textColorPicker.SelectedColorChanged += (sender, e) => {
                var color = textColorPicker.SelectedColor ?? WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_FG);
                titleText.Foreground = new SolidColorBrush(color);
                listItem.ClassText.Foreground = new SolidColorBrush(color);
                cls.SetForeground(color.ToString());
            };
            colorsStackpanel.Children.Add(textColorPicker);

            var textColorResetButton = new SHButtonSecondary() {
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ResetButton"),
            };
            void ResetTextColor() {
                var baseColor = cls.BaseForeground();
                if (baseColor == null) {
                    textColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_FG);
                } else {
                    textColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(baseColor);
                }
                cls.ResetForeground();
                colorToggle.IsChecked = cls.IsColorEnabled;
            }
            textColorResetButton.Click += (sender, b) => ResetTextColor();
            colorsStackpanel.Children.Add(textColorResetButton);

            colorToggle.Checked += (sender, b) => {
                cls.EnableColor();
                colorLabel.IsEnabled = true;
                colorLabel.Opacity = 1;
                colorsStackpanel.IsEnabled = true;
                colorsStackpanel.Opacity = 1;
            };
            colorToggle.Unchecked += (sender, b) => {
                cls.DisableColor();
                colorLabel.IsEnabled = false;
                colorLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                colorsStackpanel.IsEnabled = false;
                colorsStackpanel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
            };

            var colorResetButton = CreateResetButton(row);

            void ResetColors() {
                ResetBgColor();
                ResetTextColor();

                cls.ResetColors();
                colorToggle.IsChecked = cls.IsColorEnabled;
            }

            colorResetButton.Click += (sender, b) => ResetColors();
            settingsGrid.Children.Add(colorResetButton);

            //// Same as row

            isEnabled = cls.IsSameAsEnabled;
            opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;
            row = 1;

            var sameAsToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this class' same as override. If disabled, the plugin will use this class."
            );
            settingsGrid.Children.Add(sameAsToggle);

            var sameAsLabel = CreateLabelTextBox("Same as", isEnabled, row);
            settingsGrid.Children.Add(sameAsLabel);

            var sameAsComboBox = new ComboBox() {
                IsReadOnly = false,
                IsEditable = true,
                ItemsSource = new ListCollectionView(this._settingsControl.AllClasses) {
                    IsLiveSorting = true,
                    CustomSort = new CaseInsensitiveComparer(CultureInfo.InvariantCulture)
                },
                IsEnabled = isEnabled,
                SelectedItem = cls.SameAsDontCheckEnabled()?.AsString(),
                Opacity = opacity,
                ShouldPreserveUserEnteredPrefix = true,
                IsTextSearchCaseSensitive = true,
            };

            Grid.SetColumn(sameAsComboBox, 2);
            Grid.SetRow(sameAsComboBox, row);

            void ResetSameAs() {
                var clsTxt = cls.BaseSameAs()?.AsString();
                if (clsTxt == null) {
                    sameAsComboBox.SelectedItem = CarClass.Default.AsString(); // Must be set before resetting
                } else {
                    sameAsComboBox.SelectedItem = clsTxt; // Must be set before resetting
                }
                cls.ResetSameAs();

                sameAsToggle.IsChecked = cls.IsSameAsEnabled;
            }

            sameAsComboBox.LostFocus += (sender, b) => {
                var clsText = (string?)sameAsComboBox.Text;

                if (clsText == null || clsText == "") {
                    // "" is not a valid class name
                    ResetSameAs();
                } else {
                    if (!this._settingsControl.AllClasses.Contains(clsText)) {
                        this._settingsControl.AllClasses.Add(clsText);
                    }
                    if (!this._carClassesListBoxItems.Contains(a => a.Key.AsString() == clsText)) {
                        var cls = new CarClass(clsText);
                        this._carClassesListBoxItems.Add(new ClassSettingsListBoxItem(cls, this._plugin.Values.ClassInfos.Get(cls)));
                    }
                    cls.SetSameAs(new CarClass(clsText));
                }

                sameAsToggle.IsChecked = cls.IsSameAsEnabled;
            };
            settingsGrid.Children.Add(sameAsComboBox);

            var sameAsResetButton = CreateResetButton(row);

            sameAsResetButton.Click += (sender, b) => ResetSameAs();
            settingsGrid.Children.Add(sameAsResetButton);

            sameAsToggle.Checked += (sender, b) => {
                cls.EnableSameAs();
                sameAsLabel.IsEnabled = true;
                sameAsLabel.Opacity = 1;
                sameAsComboBox.IsEnabled = true;
                sameAsComboBox.Opacity = 1;
            };
            sameAsToggle.Unchecked += (sender, b) => {
                cls.DisableSameAs();
                sameAsLabel.IsEnabled = false;
                sameAsLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                sameAsComboBox.IsEnabled = false;
                sameAsComboBox.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
            };

            void ResetAll() {
                ResetColors();
                ResetSameAs();
            }

            allResetButton.Click += (sender, b) => ResetAll();

            deleteButton.Click += (sender, e) => {
                this._plugin.Values.ClassInfos.Remove(key);

                if (!this._plugin.Values.ClassInfos.ContainsClass(key)) {
                    // class was removed from backing data, remove from ui too
                    this._carClassesListBoxItems.Remove(this._classesList.SelectedItem as ClassSettingsListBoxItem);
                } else {
                    // class wasn't removed, had base data. Reset and disable all.
                    ResetAll();
                    sameAsToggle.IsChecked = false;
                    colorToggle.IsChecked = false;
                }
            };
        }
    }
}
