using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;

using SimHub.Plugins.Styles;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    internal class CarSettingsTab {
        private class CarSettingsListBoxItem : ListBoxItem {
            public string Key { get; set; }
            public OverridableCarInfo CarInfo { get; set; }

            public CarSettingsListBoxItem(string key, OverridableCarInfo car) : base() {
                this.CarInfo = car;
                this.Key = key;

                this.Content = key;
            }
        }

        SettingsControl _settingsControl { get; set; }
        DynLeaderboardsPlugin _plugin { get; set; }
        SHListBox _carsList { get; set; }
        StackPanel _detailsStackPanel { get; set; }
        Menu _menu { get; set; }
        readonly ObservableCollection<CarSettingsListBoxItem> _carsListBoxItems = [];


        private class CarSettingsListBoxItemComparer : IComparer {
            public int Compare(object x, object y) {
                var xKey = ((CarSettingsListBoxItem)x).Key;
                var yKey = ((CarSettingsListBoxItem)y).Key;
                return string.Compare(xKey, yKey, StringComparison.OrdinalIgnoreCase);
            }
        }
        internal CarSettingsTab(SettingsControl settingsControl, DynLeaderboardsPlugin plugin) {
            this._settingsControl = settingsControl;
            this._plugin = plugin;

            this._carsList = this._settingsControl.CarSettingsCarsList_SHListBox;
            this._carsList.Items.Clear();
            this._carsList.ItemsSource = new ListCollectionView(this._carsListBoxItems) {
                IsLiveSorting = true,
                CustomSort = new CarSettingsListBoxItemComparer()
            };
            this._carsList.SelectionChanged += (sender, _) => {
                var item = (CarSettingsListBoxItem?)((ListBox)sender).SelectedItem;
                if (item != null) {
                    this.BuildDetails(item);
                } else {
                    ((StackPanel)this._settingsControl.CarSettings_StackPanel).Children.Clear();
                }
            };

            this._detailsStackPanel = this._settingsControl.CarSettings_StackPanel;
            this._menu = this._settingsControl.CarSettingsTab_Menu;
        }

        void TrySelectCar(string car) {
            var newItem = this._carsListBoxItems.FirstOrDefault(a => a.Key == car);
            if (newItem != null) {
                this._carsList.SelectedItem = newItem;
                this._carsList.ScrollIntoView(newItem);
            }
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
                var selected = this.GetSelectedCar();
                this.BuildItems();
                if (selected != null) {
                    this.TrySelectCar(selected.Key);
                }
                this._plugin.Values.UpdateCarInfos();
            }

            resetMenu.Items.Add(resetMenuResetAll);
            resetMenuResetAll.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.Reset(c.Key);
                    }
                    RebuildItems();
                });
            };

            var resetMenuResetNames = new MenuItem() {
                Header = "Reset all names",
            };
            resetMenu.Items.Add(resetMenuResetNames);
            resetMenuResetNames.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.ResetName();
                    }
                    RebuildItems();
                });
            };

            var resetMenuResetManufacturers = new MenuItem() {
                Header = "Reset all manufacturers"
            };
            resetMenu.Items.Add(resetMenuResetManufacturers);
            resetMenuResetManufacturers.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.ResetManufacturer(c.Key);
                    }
                    RebuildItems();
                });
            };

            var resetMenuResetClasses = new MenuItem() {
                Header = "Reset all classes",
            };
            resetMenu.Items.Add(resetMenuResetClasses);
            resetMenuResetClasses.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.ResetClass();
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
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.DisableName();
                        c.Value.DisableClass();
                    }
                    RebuildItems();
                });
            };

            var disableAllNames = new MenuItem() {
                Header = "Disable all names",
            };
            disableMenu.Items.Add(disableAllNames);
            disableAllNames.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.DisableName();
                    }
                    RebuildItems();
                });
            };

            var disableAllClasses = new MenuItem() {
                Header = "Disable all classes",
            };
            disableMenu.Items.Add(disableAllClasses);
            disableAllClasses.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.DisableClass();
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
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.EnableName(c.Key);
                        c.Value.EnableClass();
                    }
                    RebuildItems();
                });
            };

            var enableAllNames = new MenuItem() {
                Header = "Enable all names",
            };
            enableMenu.Items.Add(enableAllNames);
            enableAllNames.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.EnableName();
                    }
                    RebuildItems();
                });
            };

            var enableAllClasses = new MenuItem() {
                Header = "Enable all classes",
            };
            enableMenu.Items.Add(enableAllClasses);
            enableAllClasses.Click += (sender, e) => {
                this._settingsControl.DoOnConfirmation(() => {
                    foreach (var c in this._plugin.Values.CarInfos) {
                        c.Value.EnableClass();
                    }
                    RebuildItems();
                });
            };

            if (DynLeaderboardsPlugin.Game.IsAc) {
                var updateACCarsBtn = new ButtonMenuItem() {
                    Header = "Update base info"
                };
                updateACCarsBtn.ToolTip = """
                    Read the car UI info directly from ACs car files and update this plugins look up files with that data. 
                    That data is used to get car class, manufacturer and more.
                    Note that this overwrites the base data, all user overrides will still work as expected.
                    """;
                updateACCarsBtn.Click += (_, _) => {
                    DynLeaderboardsPlugin.UpdateACCarInfos();
                    this._plugin.Values.RereadCarInfos();
                    RebuildItems();
                };
                this._menu.Items.Add(updateACCarsBtn);
            }

            var refreshBtn = new ButtonMenuItem() {
                Header = "Refresh"
            };
            refreshBtn.ToolTip = "Refresh cars list. This will check if new cars have been added and will add them here for customization.";
            refreshBtn.Click += (_, _) => RebuildItems();
            this._menu.Items.Add(refreshBtn);
        }

        CarSettingsListBoxItem? GetSelectedCar() {
            return this._carsList.SelectedItem as CarSettingsListBoxItem;
        }

        void BuildItems() {
            // Go through all cars and check for class colors. 
            // If there are new classes then trying to Values.CarClassColors.Get will add them to the dictionary.
            this._carsListBoxItems.Clear();
            foreach (var c in this._plugin.Values.CarInfos) {
                var item = new CarSettingsListBoxItem(c.Key, c.Value);
                this._carsListBoxItems.Add(item);
            }

            this._carsList.SelectedIndex = 0;
            var first = this.GetSelectedCar();
            if (first != null) {
                this.BuildDetails(first);
            } else {
                this._detailsStackPanel.Children.Clear();
            }
        }

        internal void RebuildCurrentDetails() {
            var item = this.GetSelectedCar();
            if (item != null) {
                this.BuildDetails(item);
            }
        }

        void BuildDetails(CarSettingsListBoxItem listItem) {
            var key = listItem.Key;
            var carInfo = listItem.CarInfo;

            this._detailsStackPanel.Children.Clear();

            var titleRow = new Grid() {
                Margin = new Thickness(0, 5, 10, 5)
            };
            this._detailsStackPanel.Children.Add(titleRow);
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            var carTitle = new SHSectionTitle() {
                Margin = new Thickness(10, 10, 10, 10),
                FontSize = 20,
                Text = key
            };
            Grid.SetColumn(carTitle, 0);
            Grid.SetRow(carTitle, 0);
            titleRow.Children.Add(carTitle);

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

            var disableAllBtn = CreateTitleRowButton("Disable", 1);
            titleRow.Children.Add(disableAllBtn);

            var resetAllBtn = CreateTitleRowButton("Reset", 2);
            titleRow.Children.Add(resetAllBtn);

            var deleteBtn = CreateTitleRowButton("Remove", 3);
            if (carInfo.Base != null) {
                deleteBtn.IsEnabled = false;
                deleteBtn.Opacity = 0.5;
                deleteBtn.ToolTip = "This car has base data and cannot be deleted. Use `Disable` button to revert back to SimHub provided data or `Reset` to revert back to plugin defaults.";
            }
            titleRow.Children.Add(deleteBtn);
            ToolTipService.SetShowOnDisabled(deleteBtn, true);


            var settingsGrid = new Grid() {
                Margin = new Thickness(10, 5, 10, 5)
            };
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

            TextBox CreateEditTextBox(string? text, bool isEnabled, int row) {
                var textBox = new TextBox() {
                    Margin = new Thickness(0, 5, 0, 5),
                    Text = text ?? "",
                    IsEnabled = isEnabled,
                    Opacity = isEnabled ? 1 : SettingsControl.DISABLED_OPTION_OPACITY
                };
                Grid.SetColumn(textBox, 2);
                Grid.SetRow(textBox, row);

                return textBox;
            }

            SHButtonSecondary CreateResetButton(int row) {
                var button = new SHButtonSecondary() {
                    Padding = new Thickness(5),
                    Margin = new Thickness(5),
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


            // Name row

            var isEnabled = carInfo.IsNameEnabled;
            var row = 0;

            var nameToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this car name override. If disabled, the plugin will use the name provided by SimHub."
            );
            settingsGrid.Children.Add(nameToggle);

            var nameLabel = CreateLabelTextBox("Name", isEnabled, row);
            settingsGrid.Children.Add(nameLabel);

            var nameTextBox = CreateEditTextBox(carInfo.NameDontCheckEnabled(), isEnabled, row);
            nameTextBox.TextChanged += (sender, b) => {
                carInfo.SetName(nameTextBox.Text);
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(nameTextBox);

            var nameResetButton = CreateResetButton(row);
            void ResetName() {
                // Set the text before resetting, because it will trigger the TextChanged event and calls car.SetName
                nameTextBox.Text = carInfo.BaseName();
                carInfo.ResetName();
                nameToggle.IsChecked = carInfo.IsNameEnabled;
            }
            nameResetButton.Click += (sender, b) => {
                ResetName();
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(nameResetButton);

            nameToggle.Checked += (sender, b) => {
                carInfo.EnableName(key);
                nameLabel.IsEnabled = true;
                nameLabel.Opacity = 1;
                nameTextBox.IsEnabled = true;
                nameTextBox.Opacity = 1;

                nameTextBox.Text = carInfo.NameDontCheckEnabled();
                this._plugin.Values.UpdateCarInfos();
            };
            nameToggle.Unchecked += (sender, b) => {
                carInfo.DisableName();
                nameLabel.IsEnabled = false;
                nameLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                nameTextBox.IsEnabled = false;
                nameTextBox.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this._plugin.Values.UpdateCarInfos();
            };


            // Manufacturer row

            row = 1;
            isEnabled = true;

            var manufacturerToggle = CreateToggle(
                true,
                row,
                "Manufacturer name cannot be disabled as there is no SimHub data to fall back to. You can use `Reset` button to revert back to the default value."
            );
            manufacturerToggle.IsEnabled = false;
            manufacturerToggle.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
            ToolTipService.SetShowOnDisabled(manufacturerToggle, true);
            settingsGrid.Children.Add(manufacturerToggle);

            var manufacturerLabel = CreateLabelTextBox("Manufacturer", isEnabled, row);
            settingsGrid.Children.Add(manufacturerLabel);

            var manufacturersView = new ListCollectionView(this._settingsControl.AllManufacturers) {
                IsLiveSorting = true,
                CustomSort = new CaseInsensitiveComparer(CultureInfo.InvariantCulture)
            };

            var manufacturer = carInfo.Manufacturer();
            if (manufacturer != null) {
                this._settingsControl.AddCarManufacturer(manufacturer);
            }

            var manufacturerComboBox = new ComboBox() {
                IsReadOnly = false,
                IsEditable = true,
                ItemsSource = manufacturersView,
                SelectedItem = manufacturer,
                IsEnabled = isEnabled,
                Opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY,
                ShouldPreserveUserEnteredPrefix = true,
                IsTextSearchCaseSensitive = true
            };

            Grid.SetColumn(manufacturerComboBox, 2);
            Grid.SetRow(manufacturerComboBox, row);
            manufacturerComboBox.LostFocus += (sender, b) => {
                var manufacturer = (string?)manufacturerComboBox.Text;

                if (manufacturer != null) {
                    this._settingsControl.AddCarManufacturer(manufacturer);
                    carInfo.SetManufacturer(manufacturer);
                }
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(manufacturerComboBox);

            var manufacturerResetButton = CreateResetButton(row);
            void ResetManufacturer() {
                carInfo.ResetManufacturer(key);
                var manufacturer = carInfo.Manufacturer();
                if (manufacturer != null) {
                    this._settingsControl.AddCarManufacturer(manufacturer);
                }
                manufacturerComboBox.SelectedItem = manufacturer;
            }
            manufacturerResetButton.Click += (sender, b) => {
                ResetManufacturer();
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(manufacturerResetButton);

            this._detailsStackPanel.Children.Add(settingsGrid);


            // Class row

            isEnabled = carInfo.IsClassEnabled;
            row = 2;

            var classToggle = CreateToggle(
                isEnabled,
                row,
                "Enable this car class override. If disabled, the plugin will use the class provided by SimHub."
            );
            settingsGrid.Children.Add(classToggle);

            var classLabel = CreateLabelTextBox("Class", isEnabled, row);
            settingsGrid.Children.Add(classLabel);

            var classesView = new ListCollectionView(this._settingsControl.AllClasses) {
                IsLiveSorting = true,
                CustomSort = new CaseInsensitiveComparer(CultureInfo.InvariantCulture)
            };

            var clsStr = carInfo.ClassDontCheckEnabled()?.AsString() ?? CarClass.Default.AsString();
            this._settingsControl.TryAddCarClass(new CarClass(clsStr));

            var classComboBox = new ComboBox() {
                IsReadOnly = false,
                IsEditable = true,
                ItemsSource = classesView,
                SelectedItem = clsStr,
                IsEnabled = isEnabled,
                Opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY,
                ShouldPreserveUserEnteredPrefix = true,
                IsTextSearchCaseSensitive = true
            };

            Grid.SetColumn(classComboBox, 2);
            Grid.SetRow(classComboBox, row);
            classComboBox.LostFocus += (sender, b) => {
                var cls = (string?)classComboBox.Text;

                if (cls == null || cls == "") {
                    // "" is not a valid class name
                    ResetClass();
                } else {
                    var cls2 = new CarClass(cls);
                    this._settingsControl.TryAddCarClass(cls2);
                    carInfo.SetClass(cls2);
                }

                classToggle.IsChecked = carInfo.IsClassEnabled;
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(classComboBox);

            var classResetButton = CreateResetButton(row);
            void ResetClass() {
                classComboBox.SelectedItem = carInfo.BaseClass()?.AsString() ?? CarClass.Default.AsString();
                carInfo.ResetClass();
                classToggle.IsChecked = carInfo.IsClassEnabled;
            }
            classResetButton.Click += (sender, b) => {
                ResetClass();
                this._plugin.Values.UpdateCarInfos();
            };
            settingsGrid.Children.Add(classResetButton);

            classToggle.Checked += (sender, b) => {
                carInfo.EnableClass();
                classLabel.IsEnabled = true;
                classLabel.Opacity = 1;
                classComboBox.IsEnabled = true;
                classComboBox.Opacity = 1;

                classComboBox.SelectedItem = carInfo.ClassDontCheckEnabled()?.AsString();
                this._plugin.Values.UpdateCarInfos();
            };
            classToggle.Unchecked += (sender, b) => {
                carInfo.DisableClass();
                classLabel.IsEnabled = false;
                classLabel.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                classComboBox.IsEnabled = false;
                classComboBox.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this._plugin.Values.UpdateCarInfos();
            };

            disableAllBtn.Click += (sender, b) => {
                nameToggle.IsChecked = false;
                classToggle.IsChecked = false;
                this._plugin.Values.UpdateCarInfos();
            };

            void ResetAll() {
                ResetName();
                ResetManufacturer();
                ResetClass();
            }

            resetAllBtn.Click += (sender, b) => {
                ResetAll();
                this._plugin.Values.UpdateCarInfos();
            };

            if (deleteBtn.IsEnabled) {
                deleteBtn.Click += (sender, e) => {
                    this._plugin.Values.CarInfos.Remove(key);
                    this._carsListBoxItems.Remove(this.GetSelectedCar()!);
                };
            }
        }
    }
}
