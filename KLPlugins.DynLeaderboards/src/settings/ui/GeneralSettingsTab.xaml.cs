using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
#if DESIGN
using System;
#endif

namespace KLPlugins.DynLeaderboards.Settings.UI;

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

    protected static readonly SolidColorBrush PathBgOk = new(Colors.ForestGreen);
    protected static readonly SolidColorBrush PathBgError = new(Colors.Firebrick);
    protected static readonly SolidColorBrush PathBorderOk = new(Colors.SpringGreen);
    protected static readonly SolidColorBrush PathBorderError = new(Colors.Red);

    public string? AccDataLocation {
        get => this._settings.AccDataLocation;
        set {
            this._settings.AccDataLocation = value;
            this.InvokePropertyChanged();
            this.UpdateAccDataLocationBackground();
        }
    }

    public SolidColorBrush AccDataLocationBackground { get; private set; } = GeneralSettingsTabViewModel.PathBgError;

    public SolidColorBrush AccDataLocationBorderBrush { get; private set; } =
        GeneralSettingsTabViewModel.PathBorderError;

    public string? AcRootLocation {
        get => this._settings.AcRootLocation;
        set {
            this._settings.AcRootLocation = value;
            this.InvokePropertyChanged();
            this.UpdateAcRootLocationBackground();
        }
    }

    public SolidColorBrush AcRootLocationBackground { get; set; } = GeneralSettingsTabViewModel.PathBgError;
    public SolidColorBrush AcRootLocationBorderBrush { get; set; } = GeneralSettingsTabViewModel.PathBorderError;

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
        this.UpdateAccDataLocationBackground();
        this.UpdateAcRootLocationBackground();

        foreach (var v in OutGeneralPropExtensions.Order()) {
            if (v == OutGeneralProp.NONE) {
                continue;
            }

            var vm = new PropertyViewModel<OutGeneralProp>(
                v.ToPropName(),
                v.ToolTipText(),
                v,
                this._settings.OutGeneralProps
            );
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

    private void UpdateAccDataLocationBackground() {
        if (this._settings.IsAccDataLocationValid()) {
            this.AccDataLocationBackground = GeneralSettingsTabViewModel.PathBgOk;
            this.AccDataLocationBorderBrush = GeneralSettingsTabViewModel.PathBorderOk;
        } else {
            this.AccDataLocationBackground = GeneralSettingsTabViewModel.PathBgError;
            this.AccDataLocationBorderBrush = GeneralSettingsTabViewModel.PathBorderError;
        }

        this.InvokePropertyChanged(nameof(GeneralSettingsTabViewModel.AccDataLocationBackground));
    }

    private void UpdateAcRootLocationBackground() {
        if (this._settings.IsAcRootLocationValid()) {
            this.AcRootLocationBackground = GeneralSettingsTabViewModel.PathBgOk;
            this.AcRootLocationBorderBrush = GeneralSettingsTabViewModel.PathBorderOk;
        } else {
            this.AcRootLocationBackground = GeneralSettingsTabViewModel.PathBgError;
            this.AcRootLocationBorderBrush = GeneralSettingsTabViewModel.PathBorderError;
        }

        this.InvokePropertyChanged(nameof(GeneralSettingsTabViewModel.AcRootLocationBackground));
    }
}

#if DESIGN
internal class DesignGeneralSettingsTabViewModel : GeneralSettingsTabViewModel {
    public new string? AccDataLocation { get; set; } = @"C:\Users\user\Documents\Assetto Corsa Competizione";

    // ReSharper disable once StringLiteralTypo
    public new string? AcRootLocation { get; set; } = @"C:\Program Files\SteamLibrary\steamapps\common\assettocorsa";
    public new SolidColorBrush AcRootLocationBackground { get; set; } = GeneralSettingsTabViewModel.PathBgOk;
    public new bool Log { get; set; } = true;

    public new List<PropertyViewModelBase> ExposedProperties { get; set; } =
        DesignGeneralSettingsTabViewModel.CreateProperties();

    private static List<PropertyViewModelBase> CreateProperties() {
        var list = new List<PropertyViewModelBase>();

        var vm1 = new DesignPropertyViewModel<OutGeneralProp>();
        list.Add(vm1);

        var vm2 = new DesignPropertyViewModel<OutGeneralProp> {
            Name = "Long prop name Long prop name", IsEnabled = false,
        };
        list.Add(vm2);

        var random = new Random();
        foreach (var v in OutGeneralPropExtensions.Order()) {
            if (v == OutGeneralProp.NONE) {
                continue;
            }

            var vm = new DesignPropertyViewModel<OutGeneralProp> {
                Name = v.ToPropName(), Description = v.ToolTipText(), IsEnabled = random.Next(2) == 1,
            };

            list.Add(vm);
        }

        return list;
    }
}
#endif