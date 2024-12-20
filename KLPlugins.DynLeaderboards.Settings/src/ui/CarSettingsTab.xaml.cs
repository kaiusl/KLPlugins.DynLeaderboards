﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;
#if DESIGN
using System.Collections.Generic;

using WoteverCommon.Extensions;
#endif

namespace KLPlugins.DynLeaderboards.Settings.UI;

/// <summary>
///     Interaction logic for CarSettingsTab.xaml
/// </summary>
public partial class CarSettingsTab : UserControl {
    internal CarSettingsTabViewModel _ViewModel { get; set; }

    public CarSettingsTab(SettingsControl settingsControl) {
        this.InitializeComponent();

        this._ViewModel = new CarSettingsTabViewModel(settingsControl);
        this.DataContext = this._ViewModel;

        this._ViewModel.ScrollSelectedIntoView +=
            () => {
                var selectedCar = this._ViewModel.SelectedCar;
                if (selectedCar != null) {
                    this.CarSettingsCarsList_SHListBox.ScrollIntoView(selectedCar);
                }
            };
    }
}

internal class CarSettingsTabViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? ScrollSelectedIntoView;
    internal readonly ObservableCollection<CarsListBoxItemViewModel> _CarsObservable = [];
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
                    this._selectedCar._Key,
                    this._selectedCar._Info,
                    this._settingsControl
                );
                this.SelectedCarDetailsViewModel.RemoveCar += this.RemoveSelectedCar;
                this.SelectedCarDetailsViewModel.PropertyChanged += this.OnSelectedNameChanged;
            }

            this.InvokePropertyChanged();
            this.InvokePropertyChanged(nameof(CarSettingsTabViewModel.IsSelectedNull));
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
    public bool IsAc => this._settingsControl._Game.IsAc;
    public ICommand MenuRefreshCommand { get; }

    private readonly SettingsControl _settingsControl;
    private SelectedCarDetailsViewModel? _selectedCarDetailsViewModel;

    internal CarSettingsTabViewModel(SettingsControl settingsControl) {
        this._settingsControl = settingsControl;

        foreach (var car in this._settingsControl._Settings.Infos.CarInfos) {
            var vm = new CarsListBoxItemViewModel(car.Key, car.Value);
            this._CarsObservable.Add(vm);
        }

        this.Cars = new ListCollectionView(this._CarsObservable) {
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
                Logging.LogError(msg);
            }
        }

        CommandAfterConfirmation CreateAllCarsCommand(Action<CarsListBoxItemViewModel> action) {
            return new CommandAfterConfirmation(
                () => {
                    if (this.SelectedCarDetailsViewModel != null) {
                        this.SelectedCarDetailsViewModel.PropertyChanged -= this.OnSelectedNameChanged;
                    }

                    foreach (var vm in this._CarsObservable) {
                        action(vm);
                    }

                    if (this.SelectedCarDetailsViewModel != null) {
                        this.SelectedCarDetailsViewModel.PropertyChanged += this.OnSelectedNameChanged;
                    }

                    this.Cars.Refresh();
                    this.ScrollSelectedIntoView?.Invoke();
                },
                this._settingsControl
            );
        }

        CommandAfterConfirmation CreateAllCarsCommandCannotChangeOrder(Action<CarsListBoxItemViewModel> action) {
            return new CommandAfterConfirmation(
                () => {
                    foreach (var vm in this._CarsObservable) {
                        action(vm);
                    }
                },
                this._settingsControl
            );
        }

        this.MenuResetAllCommand = CreateAllCarsCommand(vm => vm._Info.Reset(vm._Key));
        this.MenuResetAllNamesCommand = CreateAllCarsCommand(vm => vm._Info.ResetName());
        this.MenuResetAllManufacturersCommand =
            CreateAllCarsCommandCannotChangeOrder(vm => vm._Info.ResetManufacturer(vm._Key));
        this.MenuResetAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm._Info.ResetClass());

        this.MenuDisableAllCommand = CreateAllCarsCommand(
            vm => {
                vm._Info.DisableClass();
                vm._Info.DisableName();
            }
        );
        this.MenuDisableAllNamesCommand = CreateAllCarsCommand(vm => vm._Info.DisableName());
        this.MenuDisableAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm._Info.DisableClass());

        this.MenuEnableAllCommand = CreateAllCarsCommand(
            vm => {
                vm._Info.EnableClass();
                vm._Info.EnableName(vm._Key);
            }
        );
        this.MenuEnableAllNamesCommand = CreateAllCarsCommand(vm => vm._Info.EnableName(vm._Key));
        this.MenuEnableAllClassesCommand = CreateAllCarsCommandCannotChangeOrder(vm => vm._Info.EnableClass());

        this.MenuRefreshCommand = new Command(
            () => {
                var selected = this.SelectedCar;
                this._CarsObservable.Clear();
                foreach (var car in this._settingsControl._Settings.Infos.CarInfos) {
                    var vm = new CarsListBoxItemViewModel(car.Key, car.Value);
                    this._CarsObservable.Add(vm);
                }

                if (selected != null) {
                    var newSelected = this._CarsObservable.FirstOrDefault(vm => vm._Key == selected._Key);
                    this.SelectedCar = newSelected;
                }
            }
        );

        this.MenuUpdateAcBaseInfoCommand = new Command(
            () => {
                this._settingsControl._Settings.UpdateAcCarInfos();
                this._settingsControl._Settings.Infos.RereadCarInfos();
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
            this._CarsObservable.AddAll(cars);

            this._selectedCar = cars[1];
            this._selectedCarDetailsViewModel = new SelectedCarDetailsViewModel.DesignInstance();

            this.Cars = new ListCollectionView(this._CarsObservable) {
                IsLiveSorting = true,
                SortDescriptions = {
                    new SortDescription(nameof(CarsListBoxItemViewModel.Name), ListSortDirection.Ascending),
                },
            };
        }
    }
    #endif

    private void RemoveSelectedCar(string key) {
        if (this.SelectedCar == null || this.SelectedCar._Key != key) {
            var msg = $"Expected the selected car to be `{key}`. Got `{this.SelectedCar?._Key}`.";
            Debug.Fail(msg);
            Logging.LogError(msg);
            return;
        }

        this.SelectedCar.Unsubscribe();
        this._CarsObservable.Remove(this.SelectedCar);
        this._settingsControl._Settings.Infos.CarInfos.TryRemove(key);
    }

    private void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class CarsListBoxItemViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name => this._Info.Name ?? this._Key;
    public string Id => this._Key;

    internal readonly string _Key;
    internal readonly OverridableCarInfo _Info;

    internal CarsListBoxItemViewModel(string key, OverridableCarInfo info) {
        this._Key = key;
        this._Info = info;

        this._Info.PropertyChanged += this.OnInfoPropertyChanged;
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
        this._Info.PropertyChanged -= this.OnInfoPropertyChanged;
        this.PropertyChanged = null;
    }
}

internal class SelectedCarDetailsViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? RemoveCar;

    public ClassPreviewViewModel ClassPreviewViewModel {
        get => this._classPreviewViewModel;
        private set {
            this._classPreviewViewModel = value;
            this.InvokePropertyChanged();
        }
    }

    public string Name {
        get => this._info.Name ?? this.Id;
        set => this._info.SetName(value);
    }

    public string Id { get; }

    public bool IsNameEnabled {
        get => this._info._IsNameEnabled;
        set {
            if (value) {
                this._info.EnableName();
            } else {
                this._info.DisableName();
            }
        }
    }

    public string Manufacturer {
        get => this._info.Manufacturer ?? "";
        set {
            this._info.SetManufacturer(value);
            this._settingsControl.TryAddCarManufacturer(value);
        }
    }

    public string Class {
        get => this._info.Class.AsString();
        set {
            var oldClass = this._info.Class;
            var cls = new CarClass(value);
            this._info.SetClass(cls);
            this._settingsControl.TryAddCarClass(cls);

            // HACK:
            // Problem is that when a car's class changed, we want to update the CanBeRemove property on Class settings tab.
            // So we somehow have to trigger an update on SelectedClassViewModels.CanBeRemoved property.
            // 
            // ClassInfos.Manager doesn't itself have a property CanBeRemoved, but ClassSettingsTab's SelectedClassViewModels does,
            // and it forwards all property change notifications from ClassInfos.Manager. 
            // Thus, below will trigger an update of SelectedClassViewModels.CanBeRemoved property. 
            this._settingsControl._ClassesManager.GetOrAdd(oldClass)
                .InvokePropertyChanged(nameof(SelectedClassViewModel.CanBeRemoved));
            var newClsManager = this._settingsControl._ClassesManager.GetOrAddFollowReplaceWith(cls);
            newClsManager.InvokePropertyChanged(nameof(SelectedClassViewModel.CanBeRemoved));

            this.ClassPreviewViewModel = new ClassPreviewViewModel(newClsManager);
        }
    }

    public bool IsClassEnabled {
        get => this._info._IsClassEnabled;
        set {
            if (value) {
                this._info.EnableClass();
            } else {
                this._info.DisableClass();
            }

            this._settingsControl._ClassesManager.GetOrAdd(this._info._ClassDontCheckEnabled)
                .InvokePropertyChanged(nameof(SelectedClassViewModel.CanBeRemoved));

            var cls = this._info.Class;
            var newClsManager = this._settingsControl._ClassesManager.GetOrAddFollowReplaceWith(cls);
            this.ClassPreviewViewModel = new ClassPreviewViewModel(newClsManager);
        }
    }

    public bool CanBeRemoved => this._settingsControl._Settings.Infos.CarInfos.CanBeRemoved(this.Id);

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
    private ClassPreviewViewModel _classPreviewViewModel;

    internal SelectedCarDetailsViewModel(string key, OverridableCarInfo info, SettingsControl settingsControl) {
        this.Id = key;
        this._info = info;
        this._settingsControl = settingsControl;
        var cls = info.Class;
        var clsManager = settingsControl._ClassesManager.GetOrAddFollowReplaceWith(cls);
        this.ClassPreviewViewModel = new ClassPreviewViewModel(clsManager);

        this.AllClasses = new ListCollectionView(settingsControl._AllClasses) {
            IsLiveSorting = true, SortDescriptions = { new SortDescription() },
        };

        this.AllManufacturers = new ListCollectionView(settingsControl._AllManufacturers) {
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
        this.ResetClassCommand = new Command(
            () => {
                this._info.ResetClass();
                var cls2 = this._info.Class;
                var clsManager2 = settingsControl._ClassesManager.GetOrAddFollowReplaceWith(cls2);
                this.ClassPreviewViewModel = new ClassPreviewViewModel(clsManager2);
            }
        );
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

        public new ClassPreviewViewModel ClassPreviewViewModel { get; set; } = new DesignClassPreviewViewModel();
    }
    #endif

    internal void Unsubscribe() {
        this._info.PropertyChanged -= this.OnInfoPropertyChanged;
        this.PropertyChanged = null;
        this.RemoveCar = null;
    }

    private void OnInfoPropertyChanged(object sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(OverridableCarInfo.Name):
                this.InvokePropertyChanged(nameof(this.Name));
                break;
            case nameof(OverridableCarInfo.Class):
                var cls2 = this._info.Class;
                var clsManager2 = this._settingsControl._ClassesManager.GetOrAddFollowReplaceWith(cls2);
                this.ClassPreviewViewModel = new ClassPreviewViewModel(clsManager2);
                this.InvokePropertyChanged(nameof(this.Class));
                break;
            case nameof(OverridableCarInfo.Manufacturer):
                this.InvokePropertyChanged(nameof(this.Manufacturer));
                break;
            case nameof(OverridableCarInfo._IsClassEnabled):
                this.InvokePropertyChanged(nameof(this.IsClassEnabled));
                break;
            case nameof(OverridableCarInfo._IsNameEnabled):
                this.InvokePropertyChanged(nameof(this.IsNameEnabled));
                break;
        }
    }

    private void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}