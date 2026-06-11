using System.Windows.Media.Imaging;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Mvvm;
using OutlastTrialsMod.Services;

namespace OutlastTrialsMod.Models;

public sealed class GameFileItem : ViewModelBase
{
    private BitmapImage? _thumbnail;
    private bool _isThumbnailLoading;
    private string _previewStatus = string.Empty;
    private string _exportType = string.Empty;

    public GameFileItem(string name, string path, long size, string extension, bool isFolder = false)
    {
        Name = name;
        Path = path;
        Size = size;
        Extension = extension;
        IsFolder = isFolder;
        FileType = ResolveFileType(extension, isFolder);
    }

    public string Name { get; }
    public string Path { get; }
    public long Size { get; }
    public string Extension { get; }
    public bool IsFolder { get; }

    public FileType FileType { get; private set; }

    public bool IsLocalizationFile => FileType == FileType.Localization;

    public bool IsStagedFile => ModStagingPaths.IsWithinModifiedFilesRoot(Path);

    public bool ShowModifyInContextMenu =>
        !IsFolder &&
        !IsLocalizationFile &&
        (IsStagedFile
            ? Extension.Equals("png", StringComparison.OrdinalIgnoreCase) ||
              VirtualFileTreeBuilder.IsPackageExtension(Extension)
            : true);

    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Name);

    public string ExportType
    {
        get => _exportType;
        set
        {
            if (!SetProperty(ref _exportType, value))
                return;

            if (AssetTypeFilter.IsTextureExport(value))
                SetFileType(FileType.Texture);

            OnPropertyChanged(nameof(TypeSubtitle));
        }
    }

    public void SetFileType(FileType fileType)
    {
        if (FileType == fileType)
            return;

        FileType = fileType;
        OnPropertyChanged(nameof(FileType));
        OnPropertyChanged(nameof(IsLocalizationFile));
    }

    private static FileType ResolveFileType(string extension, bool isFolder)
    {
        if (isFolder)
            return FileType.Directory;

        if (AssetTypeFilter.IsLocresExtension(extension))
            return FileType.Localization;

        if (VirtualFileTreeBuilder.IsPackageExtension(extension))
            return FileType.Package;

        if (AssetTypeFilter.IsRawTextExtension(extension))
            return FileType.RawText;

        return FileType.Other;
    }

    public string TypeSubtitle =>
        !string.IsNullOrWhiteSpace(ExportType) ? ExportType : SizeDisplay;

    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        set => SetProperty(ref _thumbnail, value);
    }

    public bool IsThumbnailLoading
    {
        get => _isThumbnailLoading;
        set => SetProperty(ref _isThumbnailLoading, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        set => SetProperty(ref _previewStatus, value);
    }

    public bool HasPreviewStatus => !string.IsNullOrWhiteSpace(PreviewStatus);

    public string SizeDisplay => Size < 1024
        ? $"{Size} B"
        : Size < 1024 * 1024
            ? $"{Size / 1024.0:F1} KB"
            : $"{Size / (1024.0 * 1024.0):F1} MB";
}
