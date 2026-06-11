using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CUE4Parse.FileProvider.Objects;
using App = OutlastTrialsMod.App;
using Microsoft.Win32;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Models;
using OutlastTrialsMod.Mvvm;
using OutlastTrialsMod.Services;
using OutlastTrialsMod.Views;

namespace OutlastTrialsMod.ViewModels;

public abstract class FileBrowserViewModel : ViewModelBase
{
    private const int ThumbnailConcurrency = 4;

    private FileTreeNode? _selectedFolder;
    private GameFileItem? _selectedFile;
    private string _statusText = string.Empty;
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private CancellationTokenSource? _folderCts;
    private readonly ICollectionView _filesView;

    protected FileBrowserViewModel(bool isModTab)
    {
        IsModTab = isModTab;
        TreeRoots = new ObservableCollection<FileTreeNode>();
        FilesInFolder = new ObservableCollection<GameFileItem>();
        _filesView = CollectionViewSource.GetDefaultView(FilesInFolder);
        _filesView.Filter = FilterFileItem;

        OpenPreviewCommand = new RelayCommand(OpenPreviewFromParameter, CanOpenPreviewParameter);
        PreviewFileCommand = new RelayCommand(() => OpenPreviewForSelected(), CanPreviewOrExport);
        SaveRawCommand = new RelayCommand(p => _ = SaveRawFromParameterAsync(p), CanExportFromParameter);
        ExportConvertedCommand = new RelayCommand(p => _ = ExportConvertedFromParameterAsync(p), CanExportFromParameter);
        SaveUassetCommand = new RelayCommand(
            p => _ = SaveRawFromParameterAsync(p as GameFileItem ?? SelectedFile),
            _ => CanExportSelected());
        ExportCommand = new RelayCommand(
            p => _ = ExportConvertedFromParameterAsync(p as GameFileItem ?? SelectedFile),
            _ => CanExportSelected());
        ModifyCommand = new RelayCommand(p => _ = ModifyFromParameterAsync(p), CanModifyFromParameter);
        EditLocresCommand = new RelayCommand(ExecuteEditLocres, CanEditLocresFromParameter);
        ReplaceCommand = new RelayCommand(() => PlaceholderAction("Replace"), () => IsModTab && SelectedFile is not null);
        DeleteLocalItemCommand = new RelayCommand(_ => { }, _ => false);
    }

    public bool IsModTab { get; }
    public ObservableCollection<FileTreeNode> TreeRoots { get; }
    public ObservableCollection<GameFileItem> FilesInFolder { get; }

    public ICollectionView FilesView => _filesView;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value))
                return;

            _filesView.Refresh();
        }
    }

    public FileTreeNode? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (!SetProperty(ref _selectedFolder, value)) return;
            _ = RefreshFileListAsync();
        }
    }

    public GameFileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
                RaiseCommandCanExecute();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand OpenPreviewCommand { get; }
    public ICommand PreviewFileCommand { get; }
    public ICommand SaveRawCommand { get; }
    public ICommand ExportConvertedCommand { get; }
    public ICommand SaveUassetCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ModifyCommand { get; }
    public ICommand EditLocresCommand { get; }
    public ICommand ReplaceCommand { get; }
    public ICommand DeleteLocalItemCommand { get; protected set; }

    public Func<Task>? ModTabRefreshCallback { get; set; }

    protected abstract IEnumerable<GameFile> GetSourceFiles();

    protected void RefreshFilesView() => _filesView.Refresh();

    private bool FilterFileItem(object item)
    {
        if (item is not GameFileItem fileItem)
            return false;

        if (string.IsNullOrWhiteSpace(_searchQuery))
            return true;

        var query = _searchQuery.Trim();
        return fileItem.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               fileItem.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public void CancelPendingWork()
    {
        _folderCts?.Cancel();
        _folderCts?.Dispose();
        _folderCts = null;
    }

    public void ReleaseUiFileHandles()
    {
        CancelPendingWork();

        foreach (var item in FilesInFolder)
        {
            item.Thumbnail = null;
            item.IsThumbnailLoading = false;
        }
    }

    private static bool IsPackageItem(GameFileItem item) =>
        VirtualFileTreeBuilder.IsPackageExtension(item.Extension);

    private bool CanPreviewOrExport() =>
        !IsModTab && SelectedFile is not null && CanExportItem(SelectedFile);

    private bool CanExportSelected() =>
        !IsModTab && SelectedFile is not null && CanExportItem(SelectedFile);

    private bool CanExportFromParameter(object? parameter) =>
        !IsModTab && parameter is GameFileItem item && CanExportItem(item);

    private static bool CanExportItem(GameFileItem item) =>
        !item.IsFolder && (AssetTypeFilter.IsRawTextExtension(item.Extension) || IsPackageItem(item));

    private bool CanModifyFromParameter(object? parameter) =>
        parameter is GameFileItem item &&
        !item.IsFolder &&
        item.FileType != FileType.Localization &&
        (IsModTab ? item.ShowModifyInContextMenu : !IsModTab);

    private bool CanEditLocresFromParameter(object? parameter) =>
        parameter is GameFileItem item &&
        !item.IsFolder &&
        item.FileType == FileType.Localization &&
        (IsModTab ? item.IsStagedFile : !IsModTab);

    private async void ExecuteEditLocres(object? param)
    {
        if (param is not GameFileItem item)
            return;

        var loc = LocalizationManager.Instance;

        try
        {
            if (!item.IsStagedFile &&
                Cue4ParseService.Instance.Provider is null &&
                !File.Exists(ModStagingPaths.GetMirroredLocresPath(item.Path)))
            {
                MessageBox.Show(
                    loc.LocresLoadFailed,
                    loc.Edit,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var editorPath = item.Path;
            var window = new LocresEditorWindow(editorPath)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                StatusText = $"Localization saved: {item.DisplayName}";
                await NotifyModificationSavedAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                loc.Format(nameof(LocalizationManager.LocresOpenFailed), ex.Message),
                loc.Edit,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task ModifyFromParameterAsync(object? parameter)
    {
        if (parameter is not GameFileItem item || item.IsFolder)
            return;

        if (IsModTab && item.IsStagedFile)
        {
            await ModifyStagedFileAsync(item).ConfigureAwait(true);
            return;
        }

        if (!IsPackageItem(item))
        {
            var loc = LocalizationManager.Instance;
            MessageBox.Show(
                loc.ModifyPackagesOnly,
                loc.Modify,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var exportType = item.ExportType;
            if (string.IsNullOrWhiteSpace(exportType) ||
                exportType.Equals(item.Extension, StringComparison.OrdinalIgnoreCase))
            {
                exportType = await Cue4ParseService.Instance
                    .TryResolveExportTypeAsync(item.Path)
                    .ConfigureAwait(true);
            }

            if (!AssetTypeFilter.IsTextureExport(exportType))
            {
                var loc = LocalizationManager.Instance;
                MessageBox.Show(
                    loc.ModifyTexture2DOnly,
                    loc.Modify,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            StatusText = $"Preparing texture editor for {item.DisplayName}...";
            var originalImage = item.Thumbnail;

            if (originalImage is null)
            {
                var decodeResult = await Cue4ParseService.Instance
                    .TryDecodeTextureFullAsync(item.Path)
                    .ConfigureAwait(true);
                originalImage = decodeResult.Preview;
            }

            if (originalImage is null)
            {
                var loc = LocalizationManager.Instance;
                MessageBox.Show(
                    loc.ModifyDecodeFailed,
                    loc.Modify,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusText = "Failed to open texture editor.";
                return;
            }

            var window = new TextureModifyWindow(item.Path, item.DisplayName, originalImage)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                StatusText = $"Texture mod staged for {item.DisplayName}";
                await NotifyModificationSavedAsync().ConfigureAwait(true);
            }
            else
            {
                StatusText = "Texture modification cancelled.";
            }
        }
        catch (Exception ex)
        {
            var loc = LocalizationManager.Instance;
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                loc.Modify,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText = loc.TextureModifyError;
        }
    }

    private async Task ModifyStagedFileAsync(GameFileItem item)
    {
        var loc = LocalizationManager.Instance;

        try
        {
            if (item.Extension.Equals("png", StringComparison.OrdinalIgnoreCase))
            {
                var image = item.Thumbnail ?? WpfImageHelper.FromFile(item.Path);
                if (image is null)
                {
                    MessageBox.Show(
                        loc.ModifyDecodeFailed,
                        loc.Modify,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var window = new TextureModifyWindow(item.Path, item.DisplayName, image)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    StatusText = $"Texture updated: {item.DisplayName}";
                    await NotifyModificationSavedAsync().ConfigureAwait(true);
                }

                return;
            }

            if (IsPackageItem(item) &&
                ModStagingPaths.TryGetRelativeVirtualPath(item.Path, out var virtualPath))
            {
                var pngPath = ModStagingPaths.GetMirroredPngPath(virtualPath);
                if (!File.Exists(pngPath))
                {
                    MessageBox.Show(
                        loc.ModifyDecodeFailed,
                        loc.Modify,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var image = WpfImageHelper.FromFile(pngPath);
                if (image is null)
                {
                    MessageBox.Show(
                        loc.ModifyDecodeFailed,
                        loc.Modify,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var window = new TextureModifyWindow(virtualPath, item.DisplayName, image)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    StatusText = $"Texture updated: {item.DisplayName}";
                    await NotifyModificationSavedAsync().ConfigureAwait(true);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                loc.Modify,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText = loc.TextureModifyError;
        }
    }

    protected virtual async Task NotifyModificationSavedAsync()
    {
        if (IsModTab)
            await RefreshFileListAsync().ConfigureAwait(true);
        else if (ModTabRefreshCallback is not null)
            await ModTabRefreshCallback().ConfigureAwait(true);
    }

    private bool CanOpenPreviewParameter(object? parameter) =>
        parameter is GameFileItem item &&
        (item.IsFolder || (IsModTab ? CanPreviewModItem(item) : CanPreviewItem(item)));

    private static bool CanPreviewModItem(GameFileItem item) =>
        !item.IsFolder && LocalModFileTreeBuilder.IsAllowedExtension(item.Extension);

    private static bool CanPreviewItem(GameFileItem item) =>
        AssetTypeFilter.IsRawTextExtension(item.Extension) || IsPackageItem(item);

    private void OpenPreviewFromParameter(object? parameter)
    {
        if (parameter is not GameFileItem item) return;

        if (item.IsFolder)
        {
            var node = SelectedFolder?.Children.FirstOrDefault(c =>
                c.FullPath.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
            if (node is not null)
                SelectedFolder = node;
            return;
        }

        OpenPreviewForItem(item);
    }

    public virtual async Task LoadTreeAsync()
    {
        IsLoading = true;
        StatusText = "Building file tree...";

        try
        {
            var files = await Task.Run(() =>
                GetSourceFiles().Where(VirtualFileTreeBuilder.IsBrowsableFile).ToList()).ConfigureAwait(true);

            var tree = await Task.Run(() => VirtualFileTreeBuilder.BuildTree(files)).ConfigureAwait(true);

            TreeRoots.Clear();
            foreach (var root in tree)
                TreeRoots.Add(root);

            if (TreeRoots.Count > 0)
                SelectedFolder = TreeRoots[0];

            StatusText = $"{files.Count} files indexed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                "File Browser",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void CancelFolderWork()
    {
        _folderCts?.Cancel();
        _folderCts?.Dispose();
        _folderCts = null;
    }

    protected CancellationToken BeginFolderWork()
    {
        CancelFolderWork();
        _folderCts = new CancellationTokenSource();
        return _folderCts.Token;
    }

    protected virtual async Task RefreshFileListAsync()
    {
        var token = BeginFolderWork();

        FilesInFolder.Clear();
        SelectedFile = null;

        if (SelectedFolder is null)
        {
            StatusText = "No folder selected.";
            return;
        }

        IList<GameFileItem> folderItems = SelectedFolder.Children
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new GameFileItem(c.Name, c.FullPath, 0, string.Empty, isFolder: true))
            .ToList();

        IList<GameFileItem> items;
        try
        {
            items = await Task.Run(() =>
                VirtualFileTreeBuilder.GetFilesInFolder(GetSourceFiles(), SelectedFolder.FullPath)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to list files: {ex.Message}";
            return;
        }

        if (token.IsCancellationRequested) return;

        foreach (var folder in folderItems)
            FilesInFolder.Add(folder);

        foreach (var item in items)
        {
            item.PreviewStatus = string.Empty;
            item.ExportType = item.FileType == FileType.Localization
                ? "LOCRES"
                : AssetTypeFilter.IsRawTextExtension(item.Extension)
                    ? item.Extension.ToUpperInvariant()
                    : string.Empty;
            FilesInFolder.Add(item);
        }

        RefreshFilesView();

        StatusText = $"{folderItems.Count} folders, {items.Count} assets — loading thumbnails...";
        await LoadThumbnailsAsync(items, token).ConfigureAwait(true);

        if (!token.IsCancellationRequested)
        {
            var assetCount = FilesInFolder.Count(i => !i.IsFolder);
            StatusText = folderItems.Count > 0
                ? $"{folderItems.Count} folders, {assetCount} assets"
                : $"{assetCount} assets";
        }
    }

    private async Task LoadThumbnailsAsync(IEnumerable<GameFileItem> items, CancellationToken token)
    {
        using var semaphore = new SemaphoreSlim(ThumbnailConcurrency);

        var tasks = items
            .Where(item => IsPackageItem(item) && !AssetTypeFilter.IsRawTextExtension(item.Extension))
            .Select(item => LoadThumbnailForItemAsync(item, semaphore, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task LoadThumbnailForItemAsync(
        GameFileItem item,
        SemaphoreSlim semaphore,
        CancellationToken token)
    {
        await Application.Current.Dispatcher.InvokeAsync(() => item.IsThumbnailLoading = true);

        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (Cue4ParseService.Instance.Provider is null)
                return;

            var exportType = await Cue4ParseService.Instance
                .TryResolveExportTypeAsync(item.Path, token)
                .ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            if (AssetTypeFilter.ShouldHide(exportType))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilesInFolder.Remove(item);
                    item.IsThumbnailLoading = false;
                });
                return;
            }

            if (!string.IsNullOrEmpty(exportType))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    item.ExportType = exportType);
            }

            if (!AssetTypeFilter.IsTextureExport(exportType))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    item.IsThumbnailLoading = false);
                return;
            }

            var result = await Cue4ParseService.Instance
                .TryDecodeTextureThumbnailAsync(item.Path, token)
                .ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            if (AssetTypeFilter.ShouldHide(result.ExportType, result.ClassName))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilesInFolder.Remove(item);
                    item.IsThumbnailLoading = false;
                });
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                item.Thumbnail = result.Preview;
                item.ExportType = result.ExportType ?? string.Empty;
                item.PreviewStatus = result.CardStatus;
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

    public void OpenPreviewForItem(GameFileItem item)
    {
        if (item.IsFolder)
            return;

        if (IsModTab)
        {
            if (!CanPreviewModItem(item))
                return;
        }
        else if (!CanPreviewItem(item))
        {
            return;
        }

        _ = OpenPreviewWindowAsync(item);
    }

    public void OpenPreviewForSelected()
    {
        if (SelectedFile is not null)
            OpenPreviewForItem(SelectedFile);
    }

    protected virtual async Task OpenPreviewWindowAsync(GameFileItem item)
    {
        try
        {
            if (AssetTypeFilter.IsRawTextExtension(item.Extension))
            {
                await OpenTextPreviewWindowAsync(item, rawText: true).ConfigureAwait(true);
                return;
            }

            if (!IsPackageItem(item))
                return;

            var exportType = item.ExportType;
            if (string.IsNullOrWhiteSpace(exportType) || exportType.Equals(item.Extension, StringComparison.OrdinalIgnoreCase))
            {
                exportType = await Cue4ParseService.Instance
                    .TryResolveExportTypeAsync(item.Path)
                    .ConfigureAwait(true);
            }

            if (AssetTypeFilter.ShouldHide(exportType))
                return;

            if (AssetTypeFilter.IsTextureExport(exportType))
            {
                await OpenImagePreviewWindowAsync(item).ConfigureAwait(true);
                return;
            }

            await OpenTextPreviewWindowAsync(item, rawText: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                "Preview Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task OpenTextPreviewWindowAsync(GameFileItem item, bool rawText)
    {
        StatusText = rawText
            ? $"Loading text for {item.DisplayName}..."
            : $"Serializing {item.DisplayName}...";

        var result = rawText
            ? await Cue4ParseService.Instance.TryReadRawTextAsync(item.Path).ConfigureAwait(true)
            : await Cue4ParseService.Instance.TrySerializeAssetAsync(item.Path).ConfigureAwait(true);

        if (!result.Succeeded)
        {
            var message = result.ErrorMessage ?? "Failed to load text preview.";
            item.PreviewStatus = message;
            MessageBox.Show(
                message,
                "Text Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            StatusText = message;
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new TextPreviewWindow(item.DisplayName, result.Text!);
            window.Show();
        });

        StatusText = $"Text preview opened: {item.DisplayName}";
    }

    private async Task OpenImagePreviewWindowAsync(GameFileItem item)
    {
        StatusText = $"Loading texture preview for {item.DisplayName}...";
        var result = await Cue4ParseService.Instance
            .TryDecodeTextureFullAsync(item.Path)
            .ConfigureAwait(true);

        if (result.Preview is null)
        {
            item.PreviewStatus = result.PreviewHint;
            MessageBox.Show(
                result.PreviewHint,
                "Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            StatusText = result.PreviewHint;
            return;
        }

        var window = new ImagePreviewWindow(item.DisplayName, result.Preview);
        window.Show();
        StatusText = $"Preview opened: {item.DisplayName}";
    }

    private async Task SaveRawFromParameterAsync(object? parameter)
    {
        if (parameter is not GameFileItem item || !CanExportItem(item))
            return;

        var outputDirectory = PromptForOutputDirectory("Select folder to save raw asset");
        if (outputDirectory is null)
            return;

        try
        {
            StatusText = $"Saving raw asset: {item.DisplayName}...";
            var result = await Cue4ParseService.Instance
                .SaveRawAssetAsync(item.Path, outputDirectory)
                .ConfigureAwait(true);

            var loc = LocalizationManager.Instance;
            StatusText = result.Succeeded ? "Raw export complete" : "Raw export failed";
            MessageBox.Show(
                result.Message,
                loc.SaveOriginalTitle,
                MessageBoxButton.OK,
                result.Succeeded ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            var loc = LocalizationManager.Instance;
            StatusText = "Raw export failed";
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                loc.SaveOriginalTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task ExportConvertedFromParameterAsync(object? parameter)
    {
        if (parameter is not GameFileItem item || !CanExportItem(item))
            return;

        var outputDirectory = PromptForOutputDirectory("Select folder for converted export");
        if (outputDirectory is null)
            return;

        try
        {
            StatusText = $"Exporting converted asset: {item.DisplayName}...";
            var result = await Cue4ParseService.Instance
                .ExportConvertedAsync(item, outputDirectory, item.ExportType)
                .ConfigureAwait(true);

            var loc = LocalizationManager.Instance;
            StatusText = result.Succeeded ? "Converted export complete" : "Converted export failed";
            MessageBox.Show(
                result.Message,
                loc.ExportTitle,
                MessageBoxButton.OK,
                result.Succeeded ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            var loc = LocalizationManager.Instance;
            StatusText = "Converted export failed";
            MessageBox.Show(
                App.FormatExceptionMessage(ex),
                loc.ExportTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string? PromptForOutputDirectory(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName)
            ? dialog.FolderName
            : null;
    }

    private void PlaceholderAction(string action)
    {
        if (SelectedFile is null) return;
        MessageBox.Show($"{action}: {SelectedFile.Path}", action, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RaiseCommandCanExecute()
    {
        (PreviewFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveRawCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportConvertedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveUassetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ModifyCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditLocresCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ReplaceCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
