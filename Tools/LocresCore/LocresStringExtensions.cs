using System.Text;

namespace Helper.MemoryList;

internal static class LocresStringExtensions
{
    public static string GetStringUE(this MemoryList memoryList)
    {
        var stringLength = memoryList.GetIntValue();
        var stringValue = string.Empty;
        if (stringLength != 0)
        {
            stringValue = stringLength < 0
                ? memoryList.GetStringValue(stringLength * -2, true, -1, Encoding.Unicode)
                : memoryList.GetStringValue(stringLength);
        }

        return ReplaceBreaklines(stringValue).TrimEnd('\0');
    }

    public static void SetStringUE(
        this MemoryList memoryList,
        string stringValue,
        bool useUnicode = false,
        bool ignoreNull = true)
    {
        stringValue = ReplaceBreaklines(stringValue, back: true);

        if (string.IsNullOrEmpty(stringValue) && ignoreNull)
        {
            memoryList.InsertIntValue(0);
            return;
        }

        stringValue += '\0';

        var encoding = Encoding.Unicode;
        if (IsAscii(stringValue) && !useUnicode)
            encoding = Encoding.ASCII;

        var textBytes = encoding.GetBytes(stringValue);

        if (encoding == Encoding.ASCII)
        {
            memoryList.InsertIntValue(textBytes.Length);
            memoryList.InsertBytes(textBytes);
        }
        else
        {
            memoryList.InsertIntValue(textBytes.Length / -2);
            memoryList.InsertBytes(textBytes);
        }
    }

    private static bool IsAscii(string stringValue)
    {
        foreach (var character in stringValue)
        {
            if (character > 127)
                return false;
        }

        return true;
    }

    private static string ReplaceBreaklines(string stringValue, bool back = false)
    {
        if (!back)
        {
            stringValue = stringValue.Replace("\r\n", "<cf>");
            stringValue = stringValue.Replace("\r", "<cr>");
            stringValue = stringValue.Replace("\n", "<lf>");
        }
        else
        {
            stringValue = stringValue.Replace("<cf>", "\r\n");
            stringValue = stringValue.Replace("<cr>", "\r");
            stringValue = stringValue.Replace("<lf>", "\n");
        }

        return stringValue;
    }
}
