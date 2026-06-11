using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutlastTrialsMod.Helpers;
using OutlastTrialsMod.Localization;
using OutlastTrialsMod.Models;
using OutlastTrialsMod.Services;
using UE4localizationsTool.Core.locres;

namespace OutlastTrialsMod.Views;

public partial class LocresEditorWindow : Window
{
    private readonly string _savePath;
    private readonly LocresFile _locresFile;
    private readonly ObservableCollection<LocresEntryRow> _entries = [];

    public LocresEditorWindow(string filePath)
    {
        if (ModStagingPaths.IsWithinModifiedFilesRoot(filePath))
        {
            _savePath = Path.GetFullPath(filePath);
        }
        else
        {
            _savePath = ModStagingPaths.GetMirroredLocresPath(filePath);
        }

        var sourcePath = LocresStagingService.ResolveSourcePath(filePath)
            ?? throw new FileNotFoundException(
                LocalizationManager.Instance.LocresLoadFailed,
                filePath);

        _locresFile = new LocresFile(sourcePath);
        InitializeComponent();

        var loc = LocalizationManager.Instance;
        var displayName = Path.GetFileName(
            ModStagingPaths.TryGetRelativeVirtualPath(_savePath, out var virtualPath)
                ? virtualPath
                : filePath.Replace('/', Path.DirectorySeparatorChar));
        Title = loc.Format(nameof(LocalizationManager.LocresEditorTitle), displayName);
        TitleText.Text = Title;

        foreach (var nameSpace in _locresFile)
        {
            foreach (var table in nameSpace)
            {
                _entries.Add(new LocresEntryRow(nameSpace.Name, table.key, table.Value));
            }
        }

        EntriesGrid.ItemsSource = _entries;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        WindowChromeHelper.Close(this);

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        EntriesGrid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
        EntriesGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);

        try
        {
            ApplyEditsToLocresFile();

            var targetDirectory = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrEmpty(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            _locresFile.Save(_savePath);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            var loc = LocalizationManager.Instance;
            MessageBox.Show(
                loc.Format(nameof(LocalizationManager.LocresSaveFailed), ex.Message),
                loc.Save,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplyEditsToLocresFile()
    {
        foreach (var row in _entries)
        {
            if (string.IsNullOrEmpty(row.Namespace))
            {
                foreach (var nameSpace in _locresFile)
                {
                    if (nameSpace.ContainsKey(row.Key))
                    {
                        nameSpace[row.Key].Value = row.Value;
                        break;
                    }
                }

                continue;
            }

            if (_locresFile.Any(ns => ns.Name == row.Namespace))
                _locresFile[row.Namespace][row.Key].Value = row.Value;
        }
    }
}
