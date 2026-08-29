using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop;

public partial class MainWindow : Window
{
    private readonly AppState _state;
    private readonly MainViewModel _vm;
    private bool _initializing = true;

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);

        _state = new AppState();
        _vm = new MainViewModel(_state);
        DataContext = _vm;

        BaseCurrencyBox.ItemsSource = Formatting.CommonCurrencies;
        BaseCurrencyBox.SelectedItem = _state.BaseCurrency;
        SetBaseCurrencyBox.ItemsSource = Formatting.CommonCurrencies;
        _initializing = false;

        Loaded += async (_, _) =>
        {
            await _state.RefreshRatesAsync();
            _vm.Refresh();
            _vm.AutoSync();
        };

        var syncTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMinutes(5) };
        syncTimer.Tick += (_, _) => _vm.AutoSync();
        syncTimer.Start();
    }

    // ----- navigation -----

    private void Nav_Dashboard(object sender, RoutedEventArgs e) => _vm.ShowDashboard();
    private void Nav_WhatIf(object sender, RoutedEventArgs e) => _vm.ShowWhatIf();

    private void Nav_Settings(object sender, RoutedEventArgs e)
    {
        SetBaseCurrencyBox.SelectedItem = _state.BaseCurrency;
        ThresholdBox.Text = _state.WorthThreshold.ToString("0.##", CultureInfo.InvariantCulture);
        BudgetBox.Text = _state.MonthlyBudget.ToString("0.##", CultureInfo.InvariantCulture);
        PathText.Text = new Core.Storage.DataStore().FilePath;
        VersionText.Text = $"SubTrackr v{Updater.CurrentVersion}";
        RatesText.Text = $"{_state.Rates.Anchor} · {_state.Rates.Date:MMM d, yyyy}";
        SyncUrlBox.Text = _state.SyncUrl;
        SyncKeyBox.Text = _state.SyncKey;
        SyncStatus.Text = "";
        _vm.ShowSettings();
    }

    // ----- add / edit / delete -----

    private void Add_Click(object sender, RoutedEventArgs e) => EditWith(new EditSubscriptionViewModel());

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is SubscriptionRowViewModel row)
            EditExisting(row);
    }

    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is SubscriptionRowViewModel row)
            EditExisting(row);
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not SubscriptionRowViewModel row) return;
        var result = MessageBox.Show(
            $"Delete \"{row.Name}\"? This can't be undone.",
            "SubTrackr", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.OK)
            _vm.Delete(row.Model.Id);
    }

    private void EditExisting(SubscriptionRowViewModel row) => EditWith(new EditSubscriptionViewModel(row.Model));

    private void EditWith(EditSubscriptionViewModel editVm)
    {
        var win = new EditSubscriptionWindow(editVm) { Owner = this };
        if (win.ShowDialog() == true && win.Result is { } sub)
            _vm.Upsert(sub);
    }

    // ----- dashboard base currency + chart -----

    private void BaseCurrency_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || BaseCurrencyBox.SelectedItem is not string code || code == _state.BaseCurrency) return;
        _state.BaseCurrency = code;
        _state.Save();
        _state.ReanchorRatesOffline();
        _vm.Refresh();
        RefreshRatesFireAndForget();
    }

    private async void RefreshRatesFireAndForget()
    {
        await _state.RefreshRatesAsync();
        _vm.Refresh();
    }

    private void ToggleView_Click(object sender, RoutedEventArgs e) => _vm.ToggleViewCommand.Execute(null);
    private void ChartDonut_Click(object sender, RoutedEventArgs e) => _vm.ChartType = ChartType.Donut;
    private void ChartBars_Click(object sender, RoutedEventArgs e) => _vm.ChartType = ChartType.Bars;
    private void ChartTrend_Click(object sender, RoutedEventArgs e) => _vm.ChartType = ChartType.Trend;

    // ----- settings page -----

    private async void Settings_Refresh_Click(object sender, RoutedEventArgs e)
    {
        RatesText.Text = "Refreshing…";
        await _state.RefreshRatesAsync();
        RatesText.Text = $"{_state.Rates.Anchor} · {_state.Rates.Date:MMM d, yyyy}";
        _vm.Refresh();
    }

    private void Settings_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = Path.GetDirectoryName(new Core.Storage.DataStore().FilePath);
        if (dir is not null && Directory.Exists(dir))
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    private async void Settings_CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        var release = await Updater.CheckAsync();
        if (release is null)
        {
            MessageBox.Show("You're on the latest version.", "SubTrackr", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"SubTrackr {release.Version} is available. Download and install now?",
                "Update available", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
        {
            await Updater.DownloadAndLaunchAsync(release);
            Application.Current.Shutdown();
        }
    }

    private void Settings_Save_Click(object sender, RoutedEventArgs e)
    {
        if (SetBaseCurrencyBox.SelectedItem is string code)
        {
            _state.BaseCurrency = code;
            BaseCurrencyBox.SelectedItem = code;
        }
        if (decimal.TryParse(ThresholdBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var t) && t >= 0)
            _state.WorthThreshold = t;
        if (decimal.TryParse(BudgetBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var b) && b >= 0)
            _state.MonthlyBudget = b;

        _state.SyncUrl = SyncUrlBox.Text;
        _state.SyncKey = SyncKeyBox.Text;
        _state.ReanchorRatesOffline();
        _state.Save();
        _vm.Refresh();
        RefreshRatesFireAndForget();
        _vm.ShowDashboard();
    }

    private async void SyncNow_Click(object sender, RoutedEventArgs e)
    {
        _state.SyncUrl = SyncUrlBox.Text;
        _state.SyncKey = SyncKeyBox.Text;
        _state.Save();

        if (!_state.SyncConfigured)
        {
            SyncStatus.Text = "Enter the project URL and anon key first.";
            return;
        }

        SyncStatus.Text = "Syncing…";
        try
        {
            var count = await _state.SyncNowAsync();
            SyncStatus.Text = $"Synced · {count} items · {DateTime.Now:HH:mm}";
            _vm.Refresh();
        }
        catch (Exception ex)
        {
            Services.Log.Error("Sync failed", ex);
            SyncStatus.Text = "Sync failed — check URL/key and connection.";
        }
    }
}
