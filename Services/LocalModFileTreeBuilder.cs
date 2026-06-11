using System.Collections.ObjectModel;
using System.IO;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Services;

public static class LocalModFileTreeBuilder
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "png",
        "json",
        "txt",
        "locres",
        "uasset",
        "umap"
    };

    public static bool IsAllowedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return AllowedExtensions.Contains(extension.TrimStart('.'));
    }

    public static ObservableCollection<FileTreeNode> BuildTree(string? rootPath = null)
    {
        rootPath ??= ModStagingPaths.GetModifiedFilesRoot();
        var roots = new ObservableCollection<FileTreeNode>();

        if (!Directory.Exists(rootPath))
            return roots;

        foreach (var childPath in Directory.GetDirectories(rootPath).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var childNode = new FileTreeNode(Path.GetFileName(childPath), childPath, isFolder: true)
            {
                IsExpanded = true
            };
            PopulateDirectoryChildren(childNode, childPath);
            roots.Add(childNode);
        }

        return roots;
    }

    public static IList<FileTreeNode> GetChildFolders(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        return Directory
            .GetDirectories(folderPath)
            .Select(path => new FileTreeNode(Path.GetFileName(path), path, isFolder: true))
            .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IList<GameFileItem> GetFilesInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        return Directory
            .GetFiles(folderPath)
            .Where(path => IsAllowedExtension(Path.GetExtension(path)))
            .Select(ToItem)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void PopulateDirectoryChildren(FileTreeNode parent, string directoryPath)
    {
        foreach (var childPath in Directory.GetDirectories(directoryPath).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var childNode = new FileTreeNode(Path.GetFileName(childPath), childPath, isFolder: true);
            PopulateDirectoryChildren(childNode, childPath);
            parent.Children.Add(childNode);
        }
    }

    private static GameFileItem ToItem(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var extension = Path.GetExtension(filePath).TrimStart('.');
        return new GameFileItem(fileInfo.Name, filePath, fileInfo.Length, extension);
    }
}
