using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using KLPlugins.DynLeaderboards.Car;
#if DESIGN
using System.Collections.Generic;

using WoteverCommon.Extensions;
#endif

namespace KLPlugins.DynLeaderboards.Settings.UI;

/// <summary>
///     Interaction logic for CarSettingsTab.xaml
/// </summary>
public partial class CarSettingsTab : UserControl {
    internal CarSettingsTabViewModel ViewModel { get; set; }

    public CarSettingsTab(DynLeaderboardsPlugin plugin, SettingsControl settingsControl) {
        this.InitializeComponent();

        this.ViewModel = new CarSettingsTabViewModel(plugin, settingsControl);
        this.DataContext = this.ViewModel;

        this.ViewModel.ScrollSelectedIntoView +=
            () => {
                var selectedCar = this.ViewModel.SelectedCar;
                if (selectedCar != null) {
                    this.CarSettingsCarsList_SHListBox.ScrollIntoView(selectedCar);
                }
            };
    }
}

internal class CarSettingsTabViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? ScrollSelectedIntoView;
    internal readonly ObservableCollection<CarsListBoxItemViewModel> CarsObservable = [];
    public ListCollectionView Cars { get; set; }

    private CarsListBoxItemViewModel? _selectedCar;

    public CarsListBoxItemViewModel? SelectedCar {
        get => this._selectedCar;
        set {
            this._selectedCar = value;

            this.SelectedCarDetailsViewModel?.Unsubscribe();
            if (this._selectedCar == null) {
                this.SelectedCarDetailsViewModel = null;
            } else {
                this.SelectedCarDetailsViewModel = new SelectedCarDetailsViewModel(
                    this._selectedCar.Key,
                    this._selectedCar.Info,
                    this._settingsControl
                );
                this.SelectedCarDetailsViewModel.RemoveCar += this.RemoveSelectedCar;
                this.SelectedCarDetailsViewModel.PropertyChanged += this.OnSelectedNameChanged;
            }

            this.InvokePropertyChanged();
            this.InvokePropertyChanged(nameof(this.IsSelectedNull));
            this.ScrollSelectedIntoView?.Invoke();
        }
    }

    private void OnSelectedNameChanged(object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == "Name") {
            this.Cars.EditItem(this.SelectedCar);
            this.Cars.CommitEdit();
            this.ScrollSelectedIntoView?.Invoke();
        }
    }

    public SelectedCarDetailsViewModel? SelectedCarDetailsViewModel {
        get => this._selectedCarDetailsViewModel;
        private set {
            this._selectedCarDetailsViewModel = value;
            this.InvokePropertyChanged();
        }
    }

    public bool IsSelectedNull => this.SelectedCar == null;

    public ICommand MenuResetAllCommand { get; }
    public ICommand MenuResetAllNamesCommand { get; }
    public ICommand MenuResetAllManufacturersCommand { get; }
    public ICommand MenuResetAllClassesCommand { get; }

    public ICommand MenuDisableAllCommand { get; }
    public ICommand MenuDisableAllNamesCommand { get; }
    public ICommand MenuDisableAllClassesCommand { get; }

    public ICommand MenuEnableAllCommand { get; }
    public ICommand MenuEnableAllNamesCommand { get; }
    public ICommand MenuEnableAllClassesCommand { get; }

    public ICommand MenuUpdateAcBaseInfoCommand { get; }
    public bool IsAc => DynLeaderboardsPlugin.Game.IsAc;
    public ICommand MenuRefreshCommand { get; }

    private readonly SettingsControl _settingsControl;
    private SelectedCarDetailsViewModel? _selectedCarDetailsViewModel;

    internal CarSettingsTabViewModel(DynLeaderboardsPlugin plugin, SettingsControl settingsControl) {
        this._settingsControl = settingsControl;

        foreach (var car in plugin.Values.CarInfos) {
            var vm = new CarsListBoxItemViewModel(car.Key, car.Value);
            this.CarsObservable.Add(vm);
        }

        this.Cars = new ListCollectionView(this.CarsObservable) {
            IsLiveSorting = true,
            SortDescriptions = {
                new SortDescription(nameof(CarsListBoxItemViewModel.Name), ListSortDirection.Ascending),
            },
        };

        if (!this.Cars.IsEmpty) {
            var first = this.Cars.GetItemAt(0);
            if (first is CarsListBoxItemViewModel firstVm) {
                this.SelectedCar = firstVm;
            } else {
                var msg = $"Expected the list element to be `CarsListBoxItemViewModel`. Got `{first?.GetType()}`.";
                Debug.Fail(msg);
                DynLeaderboardsPlugin.LogError(msg);
            }
        }

        Command CreateAllCarsCommand(Action<CarsListBoxItemViewModel> action) {
            return new Command(
                () => {
                    if (this.SelectedCarDetailsViewModel != null) {
                        this.SelectedCarDetailsViewModel.PropertyChanged -= this.OnSelectedNameChanged;
                    }

                    foreach (var vm in this.CarsObservable) {
                        action(vm);
                    }

                    if (this.SelectedCarDetailsViewModel != null) {
                        this.SelectedCarDetailsViewModel.PropertyChanged += this.OnSelectedNameChanged;
                    }

                    this.Cars.Refresh();
                    this.ScrollSelectedIntoView?.Invoke();
                }
            );
        }

        Command CreateAllCarsCommandCannotChangeOrder(Action<CarsListBoxItemViewModel> action) {
            return new Command(
                () => {
                    foreach (var vm in this.CarsObservable) {
                        action(vm);
                    }
                }
            );
        }

        this.MenuResetAllCommand = CreateAllCarsCommand(vm => vm.Info.Reset(vm.Key));
        this.MenuResetAllNamesCommand = CreateAllCarsCommand(vm => vm.Info.ResetName());
        this.MenuResetAllManufacturersCommand =
            CreateAllCarsCommandCannotChangeOrder(vm => vm.Info.ResetManufacturer(vm.Key));
        this.MenuResetAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm.Info.ResetClass());

        this.MenuDisableAllCommand = CreateAllCarsCommand(
            vm => {
                vm.Info.DisableClass();
                vm.Info.DisableName();
            }
        );
        this.MenuDisableAllNamesCommand = CreateAllCarsCommand(vm => vm.Info.DisableName());
        this.MenuDisableAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm.Info.DisableClass());

        this.MenuEnableAllCommand = CreateAllCarsCommand(
            vm => {
                vm.Info.EnableClass();
                vm.Info.EnableName(vm.Key);
            }
        );
        this.MenuEnableAllNamesCommand = CreateAllCarsCommand(vm => vm.Info.EnableName(vm.Key));
        this.MenuEnableAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm.Info.EnableClass());

        this.MenuRefreshCommand = new Command(
            () => {
                var selected = this.SelectedCar;
                this.CarsObservable.Clear();
                foreach (var car in plugin.Values.CarInfos) {
                    var vm = new CarsListBoxItemViewModel(car.Key, car.Value);
                    this.CarsObservable.Add(vm);
                }

                if (selected != null) {
                    var newSelected = this.CarsObservable.FirstOrDefault(vm => vm.Key == selected.Key);
                    this.SelectedCar = newSelected;
                }
            }
        );

        this.MenuUpdateAcBaseInfoCommand = new Command(
            () => {
                DynLeaderboardsPlugin.UpdateAcCarInfos();
                plugin.Values.RereadCarInfos();
                this.MenuRefreshCommand.Execute(null);
            }
        );
    }

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal CarSettingsTabViewModel() { }
    #pragma warning restore CS8618, CS9264

    internal class DesignInstance : CarSettingsTabViewModel {
        public DesignInstance() {
            List<CarsListBoxItemViewModel> cars = [
                new CarsListBoxItemViewModel.DesignInstance {
                    Name = "Audi R8 LMS GT3 Evo", Id = "audi_r8_lms_gt3_evo",
                },
                new CarsListBoxItemViewModel.DesignInstance { Name = "BMW M2 CS", Id = "BMW_M2_CS" },
                new CarsListBoxItemViewModel.DesignInstance {
                    Name = "Alfa Romeo Giulia Quadrifoglio", Id = "alfa_giulia_quadrifoglio",
                },
            ];
            this.CarsObservable.AddAll(cars);

            this._selectedCar = cars[1];
            this._selectedCarDetailsViewModel = new SelectedCarDetailsViewModel.DesignInstance();

            this.Cars = new ListCollectionView(this.CarsObservable) {
                IsLiveSorting = true,
                SortDescriptions = {
                    new SortDescription(nameof(CarsListBoxItemViewModel.Name), ListSortDirection.Ascending),
                },
            };
        }
    }
    #endif

    private void RemoveSelectedCar(string key) {
        if (this.SelectedCar == null || this.SelectedCar.Key != key) {
            var msg = $"Expected the selected car to be `{key}`. Got `{this.SelectedCar?.Key}`.";
            Debug.Fail(msg);
            DynLeaderboardsPlugin.LogError(msg);
            return;
        }

        this.SelectedCar.Unsubscribe();
        this.CarsObservable.Remove(this.SelectedCar);
        this._settingsControl.Plugin.Values.CarInfos.TryRemove(key);
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class CarsListBoxItemViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name => this.Info.Name() ?? this.Key;
    public string Id => this.Key;

    internal readonly string Key;
    internal readonly OverridableCarInfo Info;

    internal CarsListBoxItemViewModel(string key, OverridableCarInfo info) {
        this.Key = key;
        this.Info = info;

        this.Info.PropertyChanged += this.OnInfoPropertyChanged;
    }

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal CarsListBoxItemViewModel() { }
    #pragma warning restore CS8618, CS9264
    internal class DesignInstance : CarsListBoxItemViewModel {
        public new string Name { get; set; } = "a";
        public new string Id { get; set; } = "b";
    }
    #endif

    private void OnInfoPropertyChanged(object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(OverridableCarInfo.Name)) {
            this.PropertyChanged?.Invoke(this, e);
        }
    }

    internal void Unsubscribe() {
        this.Info.PropertyChanged -= this.OnInfoPropertyChanged;
        this.PropertyChanged = null;
    }
}

internal class SelectedCarDetailsViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? RemoveCar;

    public string Name {
        get => this._info.Name() ?? this.Id;
        set => this._info.SetName(value);
    }

    public string Id { get; }

    public bool IsNameEnabled {
        get => this._info.IsNameEnabled;
        set {
            if (value) {
                this._info.EnableName();
            } else {
                this._info.DisableName();
            }
        }
    }

    public string Manufacturer {
        get => this._info.Manufacturer() ?? "";
        set {
            this._info.SetManufacturer(value);
            this._settingsControl.TryAddCarManufacturer(value);
        }
    }

    public string Class {
        get => (this._info.Class() ?? CarClass.Default).AsString();
        set {
            var cls = new CarClass(value);
            this._info.SetClass(cls);
            this._settingsControl.TryAddCarClass(cls);
        }
    }

    public bool IsClassEnabled {
        get => this._info.IsClassEnabled;
        set {
            if (value) {
                this._info.EnableClass();
            } else {
                this._info.DisableClass();
            }
        }
    }

    public bool CanBeRemoved => this._settingsControl.Plugin.Values.CarInfos.CanBeRemoved(this.Id);

    public ListCollectionView AllClasses { get; }
    public ListCollectionView AllManufacturers { get; }

    public ICommand ResetAllCommand { get; }
    public ICommand DisableAllCommand { get; }
    public ICommand RemoveCommand { get; }

    public ICommand ResetNameCommand { get; }
    public ICommand ResetManufacturerCommand { get; }
    public ICommand ResetClassCommand { get; }

    private readonly OverridableCarInfo _info;
    private readonly SettingsControl _settingsControl;

    internal SelectedCarDetailsViewModel(string key, OverridableCarInfo info, SettingsControl settingsControl) {
        this.Id = key;
        this._info = info;
        this._settingsControl = settingsControl;

        this.AllClasses = new ListCollectionView(settingsControl.AllClasses) {
            IsLiveSorting = true, SortDescriptions = { new SortDescription() },
        };

        this.AllManufacturers = new ListCollectionView(settingsControl.AllManufacturers) {
            IsLiveSorting = true, SortDescriptions = { new SortDescription() },
        };

        this.ResetAllCommand = new Command(() => this._info.Reset(this.Id));
        this.DisableAllCommand = new Command(
            () => {
                this._info.DisableClass();
                this._info.DisableName();
            }
        );
        this.ResetNameCommand = new Command(() => this._info.ResetName());
        this.ResetManufacturerCommand = new Command(() => this._info.ResetManufacturer(this.Id));
        this.ResetClassCommand = new Command(() => this._info.ResetClass());
        this.RemoveCommand = new Command(() => this.RemoveCar?.Invoke(this.Id));

        this._info.PropertyChanged += this.OnInfoPropertyChanged;
    }

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal SelectedCarDetailsViewModel() { }
    #pragma warning restore CS8618, CS9264

    internal class DesignInstance : SelectedCarDetailsViewModel {
        public new string Name { get; set; } = "Audi R8 LMS GT3 Evo";
        public new string Id { get; set; } = "audi_r8_lms_gt3_evo";
        public new bool IsNameEnabled { get; set; } = false;
        public new string Manufacturer { get; set; } = "Audi";
        public new string Class { get; set; } = "GT3";
        public new bool IsClassEnabled { get; set; } = true;
    }
    #endif

    internal void Unsubscribe() {
        this._info.PropertyChanged -= this.OnInfoPropertyChanged;
        this.PropertyChanged = null;
        this.RemoveCar = null;
    }

    private void OnInfoPropertyChanged(object sender, PropertyChangedEventArgs e) {
        this.PropertyChanged?.Invoke(this, e);
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}