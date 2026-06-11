using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Services;

public static class VirtualFileTreeBuilder
{
    public static bool IsPackageFile(GameFile file) =>
        IsPackageExtension(file.Extension);

    public static bool IsBrowsableFile(GameFile file) =>
        IsPackageFile(file) ||
        AssetTypeFilter.IsRawTextExtension(file.Extension) ||
        AssetTypeFilter.IsLocresExtension(file.Extension);

    public static bool IsPackageExtension(string extension) =>
        extension.Equals("uasset", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals("umap", StringComparison.OrdinalIgnoreCase);

    public static ObservableCollection<FileTreeNode> BuildTree(IEnumerable<GameFile> files)
    {
        var roots = new ObservableCollection<FileTreeNode>();
        var rootLookup = new Dictionary<string, FileTreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files.Where(IsBrowsableFile))
        {
            var segments = file.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) continue;

            var builder = new StringBuilder(128);
            IList<FileTreeNode> parentList = roots;
            var lookup = rootLookup;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                builder.Append(segment).Append('/');

                if (!lookup.TryGetValue(segment, out var node))
                {
                    var path = builder.ToString().TrimEnd('/');
                    node = new FileTreeNode(segment, path, isFolder: true);
                    parentList.Add(node);
                    lookup[segment] = node;
                }

                parentList = node.Children;
                lookup = GetChildLookup(node);
            }
        }

        return roots;
    }

    private static Dictionary<string, FileTreeNode> GetChildLookup(FileTreeNode node)
    {
        var dict = new Dictionary<string, FileTreeNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in node.Children)
            dict[child.Name] = child;
        return dict;
    }

    public static IList<GameFileItem> GetFilesInFolder(
        IEnumerable<GameFile> allFiles,
        string? folderPath)
    {
        IEnumerable<GameFile> filtered = allFiles.Where(IsBrowsableFile);

        if (string.IsNullOrEmpty(folderPath))
        {
            return filtered
                .Where(f => !f.Path.Contains('/', StringComparison.Ordinal))
                .Select(ToItem)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var prefix = folderPath.TrimEnd('/') + "/";
        return filtered
            .Where(f =>
            {
                if (!f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return false;
                var remainder = f.Path[prefix.Length..];
                return !remainder.Contains('/');
            })
            .Select(ToItem)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static GameFileItem ToItem(GameFile file)
    {
        var name = file.Path.Contains('/')
            ? file.Path[(file.Path.LastIndexOf('/') + 1)..]
            : file.Path;
        var ext = file.Extension;
        if (string.IsNullOrEmpty(ext) && name.Contains('.'))
            ext = Path.GetExtension(name).TrimStart('.');
        return new GameFileItem(name, file.Path, file.Size, ext);
    }
}
