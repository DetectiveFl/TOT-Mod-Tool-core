using System.IO;

using System.Windows;

using System.Windows.Input;

using System.Windows.Media;

using Microsoft.Win32;

using OutlastTrialsMod.Helpers;

using OutlastTrialsMod.Localization;



namespace OutlastTrialsMod.Views;



public partial class TextureModifyWindow : Window

{

    private readonly string _assetVirtualPath;

    private string? _loadedPngPath;



    public TextureModifyWindow(string assetVirtualPath, string displayName, ImageSource? originalImage)

    {

        _assetVirtualPath = assetVirtualPath;

        InitializeComponent();



        var loc = LocalizationManager.Instance;

        Title = loc.Format(nameof(LocalizationManager.TextureModifyTitle), displayName);

        TitleText.Text = Title;

        OriginalImage.Source = originalImage;

        OriginalPathText.Text = ModStagingPaths.TryGetRelativeVirtualPath(assetVirtualPath, out var virtualPath)
            ? virtualPath
            : assetVirtualPath;

        if (ModStagingPaths.IsWithinModifiedFilesRoot(assetVirtualPath) &&
            assetVirtualPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            _loadedPngPath = Path.GetFullPath(assetVirtualPath);
            if (originalImage is not null)
            {
                ModifiedImage.Source = originalImage;
                PlaceholderRectangle.Visibility = Visibility.Collapsed;
                ModifiedImage.Visibility = Visibility.Visible;
            }
        }

    }



    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>

        WindowChromeHelper.OnTitleBarMouseLeftButtonDown(this, e);



    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>

        WindowChromeHelper.Minimize(this);



    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>

        WindowChromeHelper.ToggleMaximize(this);



    private void CloseButton_Click(object sender, RoutedEventArgs e) =>

        WindowChromeHelper.Close(this);



    private void LoadButton_Click(object sender, RoutedEventArgs e)

    {

        var loc = LocalizationManager.Instance;

        var dialog = new OpenFileDialog

        {

            Title = loc.SelectPngTexture,

            Filter = "PNG Image (*.png)|*.png",

            DefaultExt = ".png"

        };



        if (dialog.ShowDialog() != true)

            return;



        try

        {

            var image = WpfImageHelper.FromFile(dialog.FileName);

            if (image is null)

            {

                MessageBox.Show(

                    loc.LoadPngFailed,

                    loc.Load,

                    MessageBoxButton.OK,

                    MessageBoxImage.Warning);

                return;

            }



            _loadedPngPath = dialog.FileName;

            ModifiedImage.Source = image;

            PlaceholderRectangle.Visibility = Visibility.Collapsed;

            ModifiedImage.Visibility = Visibility.Visible;

        }

        catch (Exception ex)

        {

            MessageBox.Show(

                loc.Format(nameof(LocalizationManager.LoadPngError), ex.Message),

                loc.Load,

                MessageBoxButton.OK,

                MessageBoxImage.Error);

        }

    }



    private void DoneButton_Click(object sender, RoutedEventArgs e)

    {

        var loc = LocalizationManager.Instance;



        if (string.IsNullOrWhiteSpace(_loadedPngPath) || !File.Exists(_loadedPngPath))

        {

            MessageBox.Show(

                loc.LoadPngFirst,

                loc.TextureModifyDialogTitle,

                MessageBoxButton.OK,

                MessageBoxImage.Information);

            return;

        }



        try

        {

            var targetPath = ModStagingPaths.IsWithinModifiedFilesRoot(_assetVirtualPath)
                ? Path.GetFullPath(_assetVirtualPath)
                : ModStagingPaths.GetMirroredPngPath(_assetVirtualPath);
            var targetDir = Path.GetDirectoryName(targetPath)!;
            Directory.CreateDirectory(targetDir);
            File.Copy(_loadedPngPath, targetPath, overwrite: true);



            DialogResult = true;

            Close();

        }

        catch (Exception ex)

        {

            MessageBox.Show(

                loc.Format(nameof(LocalizationManager.SaveModifyFailed), ex.Message),

                loc.TextureModifyDialogTitle,

                MessageBoxButton.OK,

                MessageBoxImage.Error);

        }

    }

}

