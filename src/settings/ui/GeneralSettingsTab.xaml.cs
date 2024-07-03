using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Helpers;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    public partial class GeneralSettingsTab : UserControl {
        private readonly GeneralSettingsTabViewModel _viewModel;

        internal GeneralSettingsTab(PluginSettings settings) {
            this.InitializeComponent();

            this._viewModel = new GeneralSettingsTabViewModel(settings);
            this.DataContext = this._viewModel;
        }

        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            // DataGrid doesn't expose SelectedItems/Rows etc property to us as bindable. 
            // Manually keep track of which rows are selected.
            this._viewModel.OnExposedPropertiesSelectedCellsChanged(sender, e);
        }
    }

    internal class GeneralSettingsTabViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected static readonly SolidColorBrush PATH_BG_OK = new(Colors.ForestGreen);
        protected static readonly SolidColorBrush PATH_BG_ERROR = new(Colors.Firebrick);

        public string? ACCDataLocation {
            get => this._settings.AccDataLocation;
            set {
                this._settings.AccDataLocation = value;
                this.InvokePropertyChanged();
                this.UpdateACCDataLocationBackground();
            }
        }
        public SolidColorBrush ACCDataLocationBackground { get; private set; } = PATH_BG_ERROR;

        public string? ACRootLocation {
            get => this._settings.AcRootLocation;
            set {
                this._settings.AcRootLocation = value;
                this.InvokePropertyChanged();
                this.UpdateACRootLocationBackground();
            }
        }
        public SolidColorBrush ACRootLocationBackground { get; set; } = PATH_BG_ERROR;

        public bool Log {
            get => this._settings.Log;
            set {
                this._settings.Log = value;
                this.InvokePropertyChanged();
            }
        }

        public List<PropertyViewModelBase> ExposedProperties { get; } = [];

        public ICommand ExposedPropertiesEnableSelectedCommand { get; }
        public ICommand ExposedPropertiesDisableSelectedCommand { get; }

        private readonly PluginSettings _settings;

#if DESIGN
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal GeneralSettingsTabViewModel() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif

        internal GeneralSettingsTabViewModel(PluginSettings settings) {
            this._settings = settings;
            this.UpdateACCDataLocationBackground();
            this.UpdateACRootLocationBackground();

            foreach (var v in OutGeneralPropExtensions.Order()) {
                if (v == OutGeneralProp.None) {
                    continue;
                }

                var vm = new PropertyViewModel<OutGeneralProp>(v.ToPropName(), v.ToolTipText(), v, this._settings.OutGeneralProps);
                this.ExposedProperties.Add(vm);
            }

            this.ExposedPropertiesEnableSelectedCommand = new Command(() => {
                var items = this.ExposedProperties.Where(v => v.IsRowSelected);
                foreach (var it in items) {
                    it.IsEnabled = true;
                }
            });

            this.ExposedPropertiesDisableSelectedCommand = new Command(() => {
                var items = this.ExposedProperties.Where(v => v.IsRowSelected);
                foreach (var v in items) {
                    v.IsEnabled = false;
                }
            });
        }

        internal void OnExposedPropertiesSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            // one item is returned for every cell, we have three columns so we get everything three times,
            // so skip duplicates 
            PropertyViewModelBase? prev = null;
            foreach (var it in e.AddedCells) {
                if (it.Item is not PropertyViewModelBase vm || prev == vm) {
                    continue;
                }

                vm.IsRowSelected = true;
                prev = vm;
            }

            prev = null;
            foreach (var it in e.RemovedCells) {
                if (it.Item is not PropertyViewModelBase vm || prev == vm) {
                    continue;
                }
                vm.IsRowSelected = false;
                prev = vm;
            }
        }

        private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
            if (propertyName == null) {
                return;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateACCDataLocationBackground() {
            if (this._settings.IsAccDataLocationValid()) {
                this.ACCDataLocationBackground = PATH_BG_OK;
            } else {
                this.ACCDataLocationBackground = PATH_BG_ERROR;
            }
            this.InvokePropertyChanged(nameof(this.ACCDataLocationBackground));
        }

        private void UpdateACRootLocationBackground() {
            if (this._settings.IsAcRootLocationValid()) {
                this.ACRootLocationBackground = PATH_BG_OK;
            } else {
                this.ACRootLocationBackground = PATH_BG_ERROR;
            }
            this.InvokePropertyChanged(nameof(this.ACRootLocationBackground));
        }
    }

#if DESIGN
    internal class DesignGeneralSettingsTabViewModel : GeneralSettingsTabViewModel {
        public new string? ACCDataLocation { get; set; } = @"C:\Users\user\Documents\Assetto Corsa Competizione";
        public new string? ACRootLocation { get; set; } = @"C:\Program Files\SteamLibrary\steamapps\common\assettocorsa";
        public new SolidColorBrush ACRootLocationBackground { get; set; } = PATH_BG_OK;
        public new bool Log { get; set; } = true;
        public new List<PropertyViewModelBase> ExposedProperties { get; set; } = CreateProperties();

        private static List<PropertyViewModelBase> CreateProperties() {
            var list = new List<PropertyViewModelBase>();

            var vm1 = new DesignPropertyViewModel<OutGeneralProp>();
            list.Add(vm1);

            var vm2 = new DesignPropertyViewModel<OutGeneralProp>() {
                Name = "Long prop name Long prop name",
                IsEnabled = false
            };
            list.Add(vm2);

            var random = new Random();
            foreach (var v in OutGeneralPropExtensions.Order()) {
                if (v == OutGeneralProp.None) {
                    continue;
                }

                var vm = new DesignPropertyViewModel<OutGeneralProp>() {
                    Name = v.ToPropName(),
                    Description = v.ToolTipText(),
                    IsEnabled = random.Next(2) == 1
                };

                list.Add(vm);
            }

            return list;
        }
    }
#endif
}
