using System.Windows.Controls;
using System.Windows.Input;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Views;

/// <summary>The dashboard page. A double-click on a row opens it for editing.</summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel && Grid.SelectedItem is SubscriptionRowViewModel row)
        {
            viewModel.EditCommand.Execute(row);
        }
    }
}
