using Bridgemate.Models;
using Bridgemate.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Bridgemate.ViewModels;

public class MainViewModel : INotifyPropertyChanged {

    private readonly SimulationService _simulationService;
    private readonly NavigationStore _navigationStore;

    private int _dealCount = 10;
    private bool _isSimulating;
    private string? _errorMessage;

    public ObservableCollection<DealResult> Deals { get; } = [];

    public ICommand SimulateCommand { get; }
    public ICommand OpenDealCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;


    public int DealCount {
        get => _dealCount;
        set { _dealCount = value; OnPropertyChanged(); }
    }

    public bool IsSimulating {
        get => _isSimulating;
        set {
            _isSimulating = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSimulate));
        }
    }

    public bool CanSimulate => !_isSimulating;

    public string? ErrorMessage {
        get => _errorMessage;
        set {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);


    public MainViewModel(SimulationService simulationService, NavigationStore navigationStore) {
        _simulationService = simulationService;
        _navigationStore = navigationStore;

        SimulateCommand = new Command(
            execute: async () => await RunSimulationAsync(),
            canExecute: () => !_isSimulating);

        OpenDealCommand = new Command<DealResult>(
            execute: async deal => {
                if (deal != null) {
                    await Shell.Current.GoToAsync($"dealpage?deal={deal.DealNumber}");
                }
            });
    }


    private async Task RunSimulationAsync() {
        if (_isSimulating) return;

        ErrorMessage = null;
        IsSimulating = true;
        Deals.Clear();
        ((Command)SimulateCommand).ChangeCanExecute();

        try {
            var count = Math.Clamp(DealCount, 1, 500);
            var results = await Task.Run(() => _simulationService.Simulate(count));

            _navigationStore.Deals = results;

            foreach (var deal in results) {
                Deals.Add(deal);
            }
        } catch (Exception ex) {
            ErrorMessage = $"Simulation failed: {ex.Message}";
        } finally {
            IsSimulating = false;
            ((Command)SimulateCommand).ChangeCanExecute();
        }
    }


    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
