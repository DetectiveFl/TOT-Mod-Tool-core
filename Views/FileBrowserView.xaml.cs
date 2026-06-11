using System.Windows;
using System.Windows.Controls;
using OutlastTrialsMod.Models;
using OutlastTrialsMod.ViewModels;

namespace OutlastTrialsMod.Views;

public partial class FileBrowserView : System.Windows.Controls.UserControl
{
    public FileBrowserView()
    {
        InitializeComponent();
    }

    private FileBrowserViewModel? ViewModel => DataContext as FileBrowserViewModel;

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is null) return;
        ViewModel.SelectedFolder = e.NewValue as FileTreeNode;
    }
}
