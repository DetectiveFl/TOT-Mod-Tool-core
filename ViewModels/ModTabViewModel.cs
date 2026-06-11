using System.IO;
using System.Windows;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Models;
using OutlastTrialsMod.Mvvm;
using OutlastTrialsMod.Services;
using OutlastTrialsMod.Views;

namespace OutlastTrialsMod.ViewModels;

public sealed class ModTabViewModel : FileBrowserViewModel
{
    private const int ThumbnailConcurrency = 4;

    public Action? ModifiedFilesChangedCallback { get; set; }

    public ModTabViewModel() : base(isModTab: true)
    {
        DeleteLocalItemCommand = new RelayCommand(p => _ = DeleteLocalItemAsync(p), CanDeleteLocalItem);
    }

    protected override async Task NotifyModificationSavedAsync()
    {
        await base.NotifyModificationSavedAsync().ConfigureAwait(true);
        ModifiedFilesChangedCallback?.Invoke();
    }

    protected override IEnumerable<CUE4Parse.FileProvider.Objects.GameFile> GetSourceFiles() =>
        Enumerable.Empty<CUE4Parse.FileProvider.Objects.GameFile>();

    public override async Task LoadTreeAsync()
    {
        IsLoading = true;
        StatusText = "Scanning modified files...";

        try
        {
            var tree = await Task.Run(() => LocalModFileTreeBuilder.BuildTree()).ConfigureAwait(true);

            TreeRoots.Clear();
            foreach (var root in tree)
                TreeRoots.Add(root);

            SelectedFolder = TreeRoots.FirstOrDefault();
            StatusText = TreeRoots.Count > 0
                ? $"{TreeRoots.Count} modified folders"
                : "No modified files yet.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            MessageBox.Show(
                ex.Message,
                "Mod Tab",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
            ModifiedFilesChangedCallback?.Invoke();
        }
    }

    protected override async Task RefreshFileListAsync()
    {
        CancelFolderWork();
        var token = BeginFolderWork();

        FilesInFolder.Clear();
        SelectedFile = null;

        if (SelectedFolder is null)
        {
            StatusText = "No folder selected.";
            return;
        }

        IList<FileTreeNode> folderNodes;
        IList<GameFileItem> items;

        try
        {
            folderNodes = await Task.Run(() =>
                LocalModFileTreeBuilder.GetChildFolders(SelectedFolder.FullPath)).ConfigureAwait(true);
            items = await Task.Run(() =>
                LocalModFileTreeBuilder.GetFilesInFolder(SelectedFolder.FullPath)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to list files: {ex.Message}";
            return;
        }

        if (token.IsCancellationRequested)
            return;

        foreach (var folder in folderNodes)
            FilesInFolder.Add(new GameFileItem(folder.Name, folder.FullPath, 0, string.Empty, isFolder: true));

        foreach (var item in items)
        {
            item.ExportType = item.Extension.ToUpperInvariant();
            FilesInFolder.Add(item);
        }

        RefreshFilesView();

        StatusText = $"{folderNodes.Count} folders, {items.Count} modified files";
        await LoadLocalThumbnailsAsync(items, token).ConfigureAwait(true);

        if (!token.IsCancellationRequested)
        {
            var assetCount = FilesInFolder.Count(i => !i.IsFolder);
            StatusText = folderNodes.Count > 0
                ? $"{folderNodes.Count} folders, {assetCount} files"
                : $"{assetCount} files";
        }
    }

    protected override async Task OpenPreviewWindowAsync(GameFileItem item)
    {
        try
        {
            if (item.Extension.Equals("png", StringComparison.OrdinalIgnoreCase))
            {
                var image = WpfImageHelper.FromFile(item.Path);
                if (image is null)
                {
                    MessageBox.Show(
                        "Could not load the PNG preview.",
                        "Preview",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var window = new ImagePreviewWindow(item.DisplayName, image);
                window.Show();
                StatusText = $"Preview opened: {item.DisplayName}";
                return;
            }

            if (item.Extension.Equals("json", StringComparison.OrdinalIgnoreCase) ||
                item.Extension.Equals("txt", StringComparison.OrdinalIgnoreCase))
            {
                var text = await Task.Run(() => File.ReadAllText(item.Path)).ConfigureAwait(true);
                var window = new TextPreviewWindow(item.DisplayName, text);
                window.Show();
                StatusText = $"Preview opened: {item.DisplayName}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Preview Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadLocalThumbnailsAsync(IEnumerable<GameFileItem> items, CancellationToken token)
    {
        using var semaphore = new SemaphoreSlim(ThumbnailConcurrency);

        var tasks = items
            .Where(item => item.Extension.Equals("png", StringComparison.OrdinalIgnoreCase))
            .Select(item => LoadLocalThumbnailAsync(item, semaphore, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static bool CanDeleteLocalItem(object? parameter)
    {
        var path = ResolveItemPath(parameter);
        return path is not null &&
               ModStagingPaths.IsWithinModifiedFilesRoot(path) &&
               (Directory.Exists(path) || File.Exists(path));
    }

    private async Task DeleteLocalItemAsync(object? parameter)
    {
        var path = ResolveItemPath(parameter);
        if (path is null || !ModStagingPaths.IsWithinModifiedFilesRoot(path))
            return;

        var loc = LocalizationManager.Instance;
        if (MessageBox.Show(
                loc.DeleteConfirm,
                loc.Delete,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
                else if (File.Exists(path))
                    File.Delete(path);
                else
                    throw new FileNotFoundException(loc.PathNotFound, path);
            }).ConfigureAwait(true);

            await LoadTreeAsync().ConfigureAwait(true);
            StatusText = loc.Deleted;
            ModifiedFilesChangedCallback?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                loc.Format(nameof(LocalizationManager.DeleteFailed), ex.Message),
                loc.Delete,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string? ResolveItemPath(object? parameter) =>
        parameter switch
        {
            FileTreeNode node => node.FullPath,
            GameFileItem item => item.Path,
            string path => path,
            _ => null
        };

    private async Task LoadLocalThumbnailAsync(
        GameFileItem item,
        SemaphoreSlim semaphore,
        CancellationToken token)
    {
        await Application.Current.Dispatcher.InvokeAsync(() => item.IsThumbnailLoading = true);

        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (token.IsCancellationRequested)
                return;

            var thumbnail = await Task.Run(() => WpfImageHelper.FromFile(item.Path), token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                item.Thumbnail = thumbnail;
                item.IsThumbnailLoading = false;
            });
        }
        catch (OperationCanceledException)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => item.IsThumbnailLoading = false);
        }
        catch
        {
            await Application.Current.Dispatcher.InvokeAsync(() => item.IsThumbnailLoading = false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
