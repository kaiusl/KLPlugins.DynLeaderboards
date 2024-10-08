﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    public partial class GeneralSettingsTab : UserControl {
        private readonly GeneralSettingsTabViewModel _viewModel;

        internal GeneralSettingsTab(PluginSettings settings) {
            this.InitializeComponent();

            this._viewModel = new GeneralSettingsTabViewModel(settings);
            this.DataContext = this._viewModel;
        }
    }

    internal class GeneralSettingsTabViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected static readonly SolidColorBrush PATH_BG_OK = new(Colors.ForestGreen);
        protected static readonly SolidColorBrush PATH_BG_ERROR = new(Colors.Firebrick);
        protected static readonly SolidColorBrush PATH_BORDER_OK = new(Colors.SpringGreen);
        protected static readonly SolidColorBrush PATH_BORDER_ERROR = new(Colors.Red);

        public string? ACCDataLocation {
            get => this._settings.AccDataLocation;
            set {
                this._settings.AccDataLocation = value;
                this.InvokePropertyChanged();
                this.UpdateACCDataLocationBackground();
            }
        }
        public SolidColorBrush ACCDataLocationBackground { get; private set; } = PATH_BG_ERROR;
        public SolidColorBrush ACCDataLocationBorderBrush { get; private set; } = PATH_BORDER_ERROR;

        public string? ACRootLocation {
            get => this._settings.AcRootLocation;
            set {
                this._settings.AcRootLocation = value;
                this.InvokePropertyChanged();
                this.UpdateACRootLocationBackground();
            }
        }
        public SolidColorBrush ACRootLocationBackground { get; set; } = PATH_BG_ERROR;
        public SolidColorBrush ACRootLocationBorderBrush { get; set; } = PATH_BORDER_ERROR;

        public bool Log {
            get => this._settings.Log;
            set {
                this._settings.Log = value;
                this.InvokePropertyChanged();
            }
        }


        private List<PropertyViewModelBase> _exposedProperties { get; } = [];
        public ListCollectionView ExposedProperties { get; }

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
                this._exposedProperties.Add(vm);
            }

            this.ExposedPropertiesEnableSelectedCommand = new SelectedPropertiesCommand(p => p.IsEnabled = true);
            this.ExposedPropertiesDisableSelectedCommand = new SelectedPropertiesCommand(p => p.IsEnabled = false);

            this.ExposedProperties = new ListCollectionView(this._exposedProperties);
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
                this.ACCDataLocationBorderBrush = PATH_BORDER_OK;
            } else {
                this.ACCDataLocationBackground = PATH_BG_ERROR;
                this.ACCDataLocationBorderBrush = PATH_BORDER_ERROR;
            }
            this.InvokePropertyChanged(nameof(this.ACCDataLocationBackground));
        }

        private void UpdateACRootLocationBackground() {
            if (this._settings.IsAcRootLocationValid()) {
                this.ACRootLocationBackground = PATH_BG_OK;
                this.ACRootLocationBorderBrush = PATH_BORDER_OK;
            } else {
                this.ACRootLocationBackground = PATH_BG_ERROR;
                this.ACRootLocationBorderBrush = PATH_BORDER_ERROR;
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
