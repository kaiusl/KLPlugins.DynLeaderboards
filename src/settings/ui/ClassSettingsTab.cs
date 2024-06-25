
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
using System;
using SimHub.Plugins.UI;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    internal class ClassSettingsTab {
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
                    this._detailsStackPanel.Children.Clear();
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

            void RebuildItems() {
                var selected = this.SelectedClass();
                this.BuildItems();
                if (selected != null) {
                    this.TrySelectClass(selected.Key);
                }
                this._plugin.Values.UpdateClassInfos();
            }

            resetMenu.Items.Add(resetMenuResetAll);
            resetMenuResetAll.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.Reset();
                    }
                    RebuildItems();
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
                    RebuildItems();
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
                    RebuildItems();
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
                    RebuildItems();
                });
            };

            var resetMenuResetReplaceWith = new MenuItem() {
                Header = "Reset all \"replace with\" values",
            };
            resetMenu.Items.Add(resetMenuResetReplaceWith);
            resetMenuResetReplaceWith.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.ResetReplaceWith();
                    }
                    RebuildItems();
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
                        c.Value.DisableReplaceWith();
                    }
                    RebuildItems();
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
                    RebuildItems();
                });
            };

            var disableAllReplaceWith = new MenuItem() {
                Header = "Disable all \"replace with\" values",
            };
            disableMenu.Items.Add(disableAllReplaceWith);
            disableAllReplaceWith.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.DisableReplaceWith();
                    }
                    RebuildItems();
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
                        c.Value.EnableReplaceWith();
                    }
                    RebuildItems();
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
                    RebuildItems();
                });
            };

            var enableAllReplaceWith = new MenuItem() {
                Header = "Enable all \"replace with\" values",
            };
            enableMenu.Items.Add(enableAllReplaceWith);
            enableAllReplaceWith.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.ClassInfos) {
                        c.Value.EnableReplaceWith();
                    }
                    RebuildItems();
                });
            };

            var addNewClassBtn = new ButtonMenuItem() {
                Header = "Add new class"
            };
            addNewClassBtn.Click += async (sender, e) => {
                var dialogWindow = new AddNewClassDialog();
                var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

                switch (res) {
                    case System.Windows.Forms.DialogResult.OK:
                        DynLeaderboardsPlugin.LogInfo($"Add new class `{dialogWindow.ClassName.Text}`");
                        var clsName = dialogWindow.ClassName.Text;
                        if (clsName != null && clsName != "") {
                            var cls = new CarClass(clsName);
                            this.TryAddCarClass(cls);
                            var newItem = this._carClassesListBoxItems.First(a => a.Key == cls);
                            if (newItem != null) {
                                this._classesList.SelectedItem = newItem;
                                this._classesList.ScrollIntoView(newItem);
                            }
                        }

                        break;
                    default:
                        break;

                };
            };
            this._menu.Items.Add(addNewClassBtn);

            var refreshBtn = new ButtonMenuItem() {
                Header = "Refresh"
            };
            refreshBtn.ToolTip = "Refresh classes list. This will check if new classes have been added and will add them here for customization.";
            refreshBtn.Click += (_, _) => RebuildItems();
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
                this._detailsStackPanel.Children.Clear();
            }
        }

        ClassSettingsListBoxItem? SelectedClass() {
            return (ClassSettingsListBoxItem?)this._classesList.SelectedItem;
        }

        static TextBlock CreateLabelTextBox(string label, bool isEnabled = true) {
            var block = new TextBlock() {
                Text = label,
                Padding = new Thickness(0, 0, 10, 0),
                IsEnabled = isEnabled,
                Opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            return block;
        }

        /// <summary>
        /// Tries to add a new class but does nothing if the class already exists.
        /// </summary>
        void TryAddCarClass(CarClass cls) {
            var info = this._plugin.Values.ClassInfos.Get(cls);
            this._settingsControl.TryAddCarClass(cls);
            var clsText = cls.AsString();
            if (!this._carClassesListBoxItems.Contains(a => a.Key.AsString() == clsText)) {
                this._carClassesListBoxItems.Add(new ClassSettingsListBoxItem(cls, info));
            }
        }

        void TrySelectClass(CarClass cls) {
            var newItem = this._carClassesListBoxItems.FirstOrDefault(a => a.Key == cls);
            if (newItem != null) {
                this._classesList.SelectedItem = newItem;
                this._classesList.ScrollIntoView(newItem);
            }
        }

        void BuildDetails(ClassSettingsListBoxItem listItem) {
            var key = listItem.Key;
            var clsInfo = listItem.ClassInfo;

            this._detailsStackPanel.Children.Clear();

            var currentBgColor = WindowsMediaColorExtensions.FromHex(clsInfo.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG);
            var currentFgColor = WindowsMediaColorExtensions.FromHex(clsInfo.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG);

            var titleRow = new Grid() {
                Margin = new Thickness(0, 5, 10, 5)
            };
            this._detailsStackPanel.Children.Add(titleRow);

            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
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

            SHButtonPrimary CreateTitleRowButton(string label, int column) {
                var btn = new SHButtonPrimary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5, 0, 5, 0),
                    Height = 26,
                    Content = label
                };
                Grid.SetColumn(btn, column);
                Grid.SetRow(btn, 0);

                return btn;
            }

            var duplicateBtn = CreateTitleRowButton("Duplicate", 2);
            duplicateBtn.ToolTip = "Duplicate this class with new name.";
            titleRow.Children.Add(duplicateBtn);
            duplicateBtn.Click += async (_, _) => {
                var dialogWindow = new DuplicateClassDialog(key);
                var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

                switch (res) {
                    case System.Windows.Forms.DialogResult.OK:
                        var clsName = dialogWindow.NewClassName.Text;

                        if (clsName == null || clsName == "") {
                            var dialog = new MessageDialog("ERROR: Empty new class name", "A new class name cannot be empty. Failed to duplicate selected class.");
                            var _ = await dialog.ShowDialogWindowAsync(this._settingsControl);
                            return;
                        }

                        var cls = new CarClass(clsName);
                        var item = this.SelectedClass();

                        if (item == null) {
                            // shouldn't happen because what did we want to duplicate? but just as safety check
                            DynLeaderboardsPlugin.LogError("Failed to duplicate class because no class was selected");
                            return;
                        }
                        if (this._plugin.Values.ClassInfos.ContainsClass(cls)) {
                            var dialog = new MessageDialog("ERROR: Class already exists", "Class `" + cls.AsString() + "` already exists. Failed to duplicate selected class.");
                            var _ = await dialog.ShowDialogWindowAsync(this._settingsControl);
                            return;
                        }

                        this._plugin.Values.ClassInfos.Duplicate(old: key, @new: cls);
                        this.TryAddCarClass(cls);
                        this.TrySelectClass(cls);

                        break;
                    default:
                        break;

                };
            };

            var disableAllBtn = CreateTitleRowButton("Disable", 3);
            disableAllBtn.ToolTip = "Disable all custom properties for this class. As a result SimHub's provided data will be used.";
            titleRow.Children.Add(disableAllBtn);

            var resetAllBtn = CreateTitleRowButton("Reset", 4);
            resetAllBtn.ToolTip = "Reset all custom properties for this class to the plugin defaults.";
            titleRow.Children.Add(resetAllBtn);

            var deleteBtn = CreateTitleRowButton("Remove", 5);
            if (clsInfo.HasBase() || key == CarClass.Default || this._plugin.Values.CarInfos.ContainsClass(key)) {
                deleteBtn.IsEnabled = false;
                deleteBtn.Opacity = 0.5;
                string reason;
                if (key == CarClass.Default) {
                    reason = "Default class cannot be deleted.";
                } else if (clsInfo.HasBase()) {
                    reason = "This class has base data and cannot be deleted.";
                } else {
                    reason = "This class is used by some cars and cannot be deleted.";
                }
                deleteBtn.ToolTip = $"{reason} Use `Disable` button to revert back to SimHub provided data or `Reset` to revert back to plugin defaults.";
            } else {
                deleteBtn.ToolTip = "Remove this class completely.";
            }

            titleRow.Children.Add(deleteBtn);
            ToolTipService.SetShowOnDisabled(deleteBtn, true);

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
            settingsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            TextBlock CreateLabelTextBox(string label, bool isEnabled, int row) {
                var block = ClassSettingsTab.CreateLabelTextBox(label, isEnabled);
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

            var isEnabled = clsInfo.IsColorEnabled;
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
                clsInfo.SetBackground(color.ToString());
                this._plugin.Values.UpdateClassInfos();
            };
            colorsStackpanel.Children.Add(bgColorPicker);

            var bgColorResetButton = new SHButtonSecondary() {
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ResetButton"),
            };
            void ResetBgColor() {
                var baseColor = clsInfo.BaseBackground();
                if (baseColor == null) {
                    bgColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_BG);
                } else {
                    bgColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(baseColor);
                }
                clsInfo.ResetBackground();
                colorToggle.IsChecked = clsInfo.IsColorEnabled;

            }
            bgColorResetButton.Click += (sender, b) => {
                ResetBgColor();
                this._plugin.Values.UpdateClassInfos();
            };
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
                clsInfo.SetForeground(color.ToString());
                this._plugin.Values.UpdateClassInfos();
            };
            colorsStackpanel.Children.Add(textColorPicker);

            var textColorResetButton = new SHButtonSecondary() {
                Style = (Style)this._settingsControl.FindResource("ColorGrid_ResetButton"),
            };
            void ResetTextColor() {
                var baseColor = clsInfo.BaseForeground();
                if (baseColor == null) {
                    textColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(OverridableTextBoxColor.DEF_FG);
                } else {
                    textColorPicker.SelectedColor = WindowsMediaColorExtensions.FromHex(baseColor);
                }
                clsInfo.ResetForeground();
                colorToggle.IsChecked = clsInfo.IsColorEnabled;
            }
            textColorResetButton.Click += (sender, b) => {
                ResetTextColor();
                this._plugin.Values.UpdateClassInfos();
            };
            colorsStackpanel.Children.Add(textColorResetButton);

            colorToggle.Checked += (sender, b) => {
                clsInfo.EnableColor();
                colorLabel.IsEnabled = true;
                colorLabel.Opacity = 1;
                colorsStackpanel.IsEnabled = true;
                colorsStackpanel.Opacity = 1;
                this._plugin.Values.UpdateClassInfos();
            };
            colorToggle.Unchecked += (sender, b) => {
                clsInfo.DisableColor();
                colorLabel.IsEnabled = false;
                colorLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                colorsStackpanel.IsEnabled = false;
                colorsStackpanel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this._plugin.Values.UpdateClassInfos();
            };

            var colorResetButton = CreateResetButton(row);

            void ResetColors() {
                ResetBgColor();
                ResetTextColor();

                clsInfo.ResetColors();
                colorToggle.IsChecked = clsInfo.IsColorEnabled;
            }

            colorResetButton.Click += (sender, b) => {
                ResetColors();
                this._plugin.Values.UpdateClassInfos();
            };
            settingsGrid.Children.Add(colorResetButton);

            //// Short name row

            isEnabled = true;
            opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;
            row = 1;

            var shortNameLabel = CreateLabelTextBox("Short name", isEnabled, row);
            settingsGrid.Children.Add(shortNameLabel);

            var shortNameTextBox = new TextBox() {
                VerticalContentAlignment = VerticalAlignment.Center,
                Text = clsInfo.ShortName() ?? key.AsString(),
            };
            shortNameTextBox.TextChanged += (sender, b) => {
                clsInfo.SetShortName(shortNameTextBox.Text);
                this._plugin.Values.UpdateClassInfos();
            };
            Grid.SetColumn(shortNameTextBox, 2);
            Grid.SetRow(shortNameTextBox, row);
            settingsGrid.Children.Add(shortNameTextBox);

            void ResetShortName() {
                var clsStr = clsInfo.BaseShortName() ?? key.AsString();
                shortNameTextBox.Text = clsStr;
                clsInfo.ResetShortName();
            }

            var shortNameResetButton = CreateResetButton(row);
            shortNameResetButton.Click += (sender, b) => {
                ResetShortName();
                this._plugin.Values.UpdateClassInfos();
            };
            settingsGrid.Children.Add(shortNameResetButton);

            //// Replace with row

            isEnabled = clsInfo.IsReplaceWithEnabled;
            opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;
            row = 2;

            var replaceWithToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this class' \"replace with\" override. If disabled, the plugin will use this class."
            );
            settingsGrid.Children.Add(replaceWithToggle);

            var replaceWithLabel = CreateLabelTextBox("Replace with", isEnabled, row);
            settingsGrid.Children.Add(replaceWithLabel);

            var clsStr = clsInfo.ReplaceWithDontCheckEnabled()?.AsString() ?? CarClass.Default.AsString();
            this.TryAddCarClass(new CarClass(clsStr));

            var replaceWithComboBox = new ComboBox() {
                IsReadOnly = false,
                IsEditable = true,
                ItemsSource = new ListCollectionView(this._settingsControl.AllClasses) {
                    IsLiveSorting = true,
                    CustomSort = new CaseInsensitiveComparer(CultureInfo.InvariantCulture)
                },
                IsEnabled = isEnabled,
                SelectedItem = clsStr,
                Opacity = opacity,
                ShouldPreserveUserEnteredPrefix = true,
                IsTextSearchCaseSensitive = true,
            };

            Grid.SetColumn(replaceWithComboBox, 2);
            Grid.SetRow(replaceWithComboBox, row);

            void ResetReplaceWith() {
                var clsStr = clsInfo.BaseReplaceWith()?.AsString() ?? CarClass.Default.AsString();
                this._settingsControl.TryAddCarClass(new CarClass(clsStr));
                replaceWithComboBox.SelectedItem = clsStr;
                clsInfo.ResetReplaceWith();
                replaceWithToggle.IsChecked = clsInfo.IsReplaceWithEnabled;
            }

            replaceWithComboBox.LostFocus += (sender, b) => {
                var clsText = (string?)replaceWithComboBox.Text;

                if (clsText == null || clsText == "") {
                    // "" is not a valid class name
                    ResetShortName();
                } else {
                    var cls = new CarClass(clsText);
                    this.TryAddCarClass(cls);
                    clsInfo.SetReplaceWith(new CarClass(clsText));
                }

                replaceWithToggle.IsChecked = clsInfo.IsReplaceWithEnabled;
                this._plugin.Values.UpdateClassInfos();
            };
            settingsGrid.Children.Add(replaceWithComboBox);

            var replaceWithResetButton = CreateResetButton(row);

            replaceWithResetButton.Click += (sender, b) => {
                ResetReplaceWith();
                this._plugin.Values.UpdateClassInfos();
            };
            settingsGrid.Children.Add(replaceWithResetButton);

            replaceWithToggle.Checked += (sender, b) => {
                clsInfo.EnableReplaceWith();
                replaceWithLabel.IsEnabled = true;
                replaceWithLabel.Opacity = 1;
                replaceWithComboBox.IsEnabled = true;
                replaceWithComboBox.Opacity = 1;

                replaceWithComboBox.SelectedItem = clsInfo.ReplaceWithDontCheckEnabled()?.AsString();
                this._plugin.Values.UpdateClassInfos();
            };
            replaceWithToggle.Unchecked += (sender, b) => {
                clsInfo.DisableReplaceWith();
                replaceWithLabel.IsEnabled = false;
                replaceWithLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                replaceWithComboBox.IsEnabled = false;
                replaceWithComboBox.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this._plugin.Values.UpdateClassInfos();
            };

            disableAllBtn.Click += (sender, b) => {
                colorToggle.IsChecked = false;
                replaceWithToggle.IsChecked = false;
                this._plugin.Values.UpdateClassInfos();
            };

            void ResetAll() {
                ResetColors();
                ResetShortName();
                this._plugin.Values.UpdateClassInfos();
            }

            resetAllBtn.Click += (sender, b) => ResetAll();

            if (deleteBtn.IsEnabled) {
                deleteBtn.Click += (sender, e) => {
                    this._plugin.Values.ClassInfos.Remove(key);
                    this._carClassesListBoxItems.Remove(this.SelectedClass()!);
                };
            }
        }

        // Nested classes

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

            public void SetKey(CarClass key) {
                this.Key = key;
                this.ClassText.Text = this.Key.AsString();
            }
        }

        private class ClassSettingsListBoxItemComparer : IComparer {
            public int Compare(object x, object y) {
                var xKey = ((ClassSettingsListBoxItem)x).Key;
                var yKey = ((ClassSettingsListBoxItem)y).Key;
                return string.Compare(xKey.AsString(), yKey.AsString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private class AddNewClassDialog : SHDialogContentBase {

            public TextBox ClassName { get; set; }

            public AddNewClassDialog() {
                this.ShowOk = true;
                this.ShowCancel = true;

                var sp = new StackPanel();
                this.Content = sp;

                var title = new SHSectionTitle() {
                    Text = "Add new class",
                    Margin = new Thickness(0, 0, 0, 25)
                };

                sp.Children.Add(title);

                var grid = new Grid();
                sp.Children.Add(grid);

                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                var nameLabel = CreateLabelTextBox("Name");
                Grid.SetColumn(nameLabel, 0);
                Grid.SetRow(nameLabel, 0);
                grid.Children.Add(nameLabel);

                this.ClassName = new TextBox();
                Grid.SetColumn(this.ClassName, 1);
                Grid.SetRow(this.ClassName, 0);
                grid.Children.Add(this.ClassName);

            }
        }

        private class DuplicateClassDialog : SHDialogContentBase {

            public TextBox NewClassName { get; set; }

            public DuplicateClassDialog(CarClass cls) {
                this.ShowOk = true;
                this.ShowCancel = true;

                var sp = new StackPanel();
                this.Content = sp;

                var title = new SHSectionTitle() {
                    Text = $"Rename `{cls.AsString()}` class",
                    Margin = new Thickness(0, 0, 0, 25)
                };

                sp.Children.Add(title);

                var grid = new Grid();
                sp.Children.Add(grid);

                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                var nameLabel = CreateLabelTextBox("New came");
                Grid.SetColumn(nameLabel, 0);
                Grid.SetRow(nameLabel, 0);
                grid.Children.Add(nameLabel);

                this.NewClassName = new TextBox();
                Grid.SetColumn(this.NewClassName, 1);
                Grid.SetRow(this.NewClassName, 0);
                grid.Children.Add(this.NewClassName);

            }
        }
    }
}
