using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OutlastTrialsMod.Helpers;

namespace OutlastTrialsMod.Converters;

public sealed class AssetTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AssetTypeStyle.GetIconGeometry(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
