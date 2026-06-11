using System.Collections.ObjectModel;
using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.Models;

public sealed class FileTreeNode : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public FileTreeNode(string name, string fullPath, bool isFolder)
    {
        Name = name;
        FullPath = fullPath;
        IsFolder = isFolder;
        Children = new ObservableCollection<FileTreeNode>();
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsFolder { get; }
    public ObservableCollection<FileTreeNode> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
