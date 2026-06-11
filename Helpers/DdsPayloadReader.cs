using System.Buffers.Binary;
using System.IO;
using System.Text;
using OutlastTrialsMod.Models;

namespace OutlastTrialsMod.Helpers;

internal static class DdsPayloadReader
{
    private const uint DdsMagic = 0x20534444; // "DDS "
    private const uint Dx10FourCC = 0x30315844; // "DX10"

    public static int GetPixelDataOffset(ReadOnlySpan<byte> dds) =>
        ResolveHeaderSize(dds, expectedRawDataSize: null);

    public static ReadOnlySpan<byte> GetPixelPayload(ReadOnlySpan<byte> dds)
    {
        var offset = GetPixelDataOffset(dds);
        if (offset >= dds.Length)
            throw new InvalidDataException("DDS pixel payload offset is outside the file.");

        return dds[offset..];
    }

    public static byte[] ExtractRawPayload(byte[] ddsBytes, int expectedRawDataSize)
    {
        if (ddsBytes.Length < expectedRawDataSize)
        {
            throw new InvalidOperationException(
                $"DDS file ({ddsBytes.Length} bytes) is smaller than expected raw payload " +
                $"({expectedRawDataSize} bytes).");
        }

        bool isDx10 = ddsBytes.Length > 88 && ddsBytes[84] == 'D' && ddsBytes[85] == 'X' && ddsBytes[86] == '1' && ddsBytes[87] == '0';
        int correctHeaderSize = isDx10 ? 148 : 128;
        if (ddsBytes.Length < correctHeaderSize + expectedRawDataSize)
        {
            throw new Exception("Ошибка: сгенерированный DDS файл слишком мал!");
        }
        var rawPayload = new byte[expectedRawDataSize];
        Array.Copy(ddsBytes, correctHeaderSize, rawPayload, 0, expectedRawDataSize);

        if (rawPayload.Length != expectedRawDataSize)
        {
            throw new InvalidOperationException(
                $"Byte mismatch! Expected {expectedRawDataSize}, got {rawPayload.Length}");
        }

        return rawPayload;
    }

    public static int ResolveHeaderSize(ReadOnlySpan<byte> dds, int? expectedRawDataSize)
    {
        if (dds.Length < 4)
            throw new InvalidDataException("DDS file is too small.");

        if (BinaryPrimitives.ReadUInt32LittleEndian(dds) != DdsMagic)
            return 0;

        if (dds.Length < 128)
            throw new InvalidDataException("DDS file is missing the standard 128-byte header.");

        if (expectedRawDataSize is int expected)
        {
            var sizeBasedHeader = dds.Length - expected;
            if (sizeBasedHeader is 128 or 148)
                return sizeBasedHeader;

            var flagBasedHeader = HasDx10Header(dds) ? 148 : 128;
            if (dds.Length - flagBasedHeader == expected)
                return flagBasedHeader;

            throw new InvalidOperationException(
                $"Cannot resolve DDS header size: file={dds.Length} bytes, expected payload={expected} bytes, " +
                $"size-based header={sizeBasedHeader}, DX10 flag header={flagBasedHeader}.");
        }

        return HasDx10Header(dds) ? 148 : 128;
    }

    public static bool HasDx10Header(ReadOnlySpan<byte> dds) =>
        dds.Length >= 88 &&
        dds[84] == (byte)'D' &&
        dds[85] == (byte)'X' &&
        dds[86] == (byte)'1' &&
        dds[87] == (byte)'0';

    public static void ValidatePayloadFormat(ReadOnlySpan<byte> dds, string expectedPixelFormat, bool isSrgb)
    {
        if (BinaryPrimitives.ReadUInt32LittleEndian(dds) != DdsMagic)
            return;

        if (dds.Length < 128)
            throw new InvalidDataException("DDS file is too small to validate pixel format.");

        var fourCc = BinaryPrimitives.ReadUInt32LittleEndian(dds.Slice(84, 4));
        string? ddsFormat;

        if (fourCc == Dx10FourCC)
        {
            if (dds.Length < 148)
                throw new InvalidDataException("DDS DX10 header is incomplete.");

            var dxgiFormat = BinaryPrimitives.ReadUInt32LittleEndian(dds.Slice(128, 4));
            ddsFormat = MapDxgiFormat(dxgiFormat);
        }
        else
        {
            ddsFormat = Encoding.ASCII.GetString(dds.Slice(84, 4)).TrimEnd('\0');
        }

        var expectedTexconv = MapPixelFormatToTexconvArgument(expectedPixelFormat, isSrgb);
        if (!FormatsMatch(ddsFormat, expectedTexconv, expectedPixelFormat))
        {
            throw new InvalidOperationException(
                $"DDS format mismatch: asset expects {expectedPixelFormat} ({expectedTexconv}), " +
                $"but texconv produced {ddsFormat ?? "unknown"}. " +
                "BC7 data in a DXT1/BC1 slot causes rainbow artifacts.");
        }
    }

    public static IReadOnlyList<int> ComputeMipChainSizes(
        int width,
        int height,
        string pixelFormat,
        int maxMipCount = int.MaxValue)
    {
        var sizes = new List<int>();
        var mipWidth = width;
        var mipHeight = height;

        while (mipWidth > 0 && mipHeight > 0 && sizes.Count < maxMipCount)
        {
            sizes.Add(ComputeMipSize(mipWidth, mipHeight, pixelFormat));
            if (mipWidth == 1 && mipHeight == 1)
                break;

            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }

        return sizes;
    }

    public static int ComputeCompressedMipSize(int sizeX, int sizeY, string pixelFormat) =>
        ComputeMipSize(sizeX, sizeY, pixelFormat);

    public static int ComputeMipSize(int sizeX, int sizeY, string pixelFormat)
    {
        if (sizeX <= 0 || sizeY <= 0)
            return 0;

        var bytesPerPixel = TextureInjectionMetadata.GetBytesPerPixel(pixelFormat);
        if (bytesPerPixel >= 4)
            return (int)(sizeX * sizeY * bytesPerPixel);

        var blockBytes = bytesPerPixel <= 0.5 ? 8 : 16;
        var blocksWide = Math.Max(1, (sizeX + 3) / 4);
        var blocksHigh = Math.Max(1, (sizeY + 3) / 4);
        return blocksWide * blocksHigh * blockBytes;
    }

    public static string MapPixelFormatToTexconvArgument(string pixelFormat, bool isSrgb)
    {
        if (pixelFormat.Contains("BC7", StringComparison.OrdinalIgnoreCase))
            return isSrgb ? "BC7_UNORM_SRGB" : "BC7_UNORM";

        if (pixelFormat.Contains("DXT1", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC1", StringComparison.OrdinalIgnoreCase))
        {
            return isSrgb ? "BC1_UNORM_SRGB" : "BC1_UNORM";
        }

        if (pixelFormat.Contains("DXT5", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("BC3", StringComparison.OrdinalIgnoreCase))
        {
            return isSrgb ? "BC3_UNORM_SRGB" : "BC3_UNORM";
        }

        if (pixelFormat.Contains("BC4", StringComparison.OrdinalIgnoreCase))
            return "BC4_UNORM";

        if (pixelFormat.Contains("BC5", StringComparison.OrdinalIgnoreCase))
            return "BC5_UNORM";

        if (pixelFormat.Contains("B8G8R8A8", StringComparison.OrdinalIgnoreCase))
            return isSrgb ? "BGRA8_UNORM_SRGB" : "BGRA8_UNORM";

        throw new InvalidOperationException(
            $"Unsupported texture format for injection: {pixelFormat}");
    }

    private static string? MapDxgiFormat(uint dxgiFormat) => dxgiFormat switch
    {
        71 => "BC1_UNORM",
        72 => "BC1_UNORM_SRGB",
        77 => "BC3_UNORM",
        78 => "BC3_UNORM_SRGB",
        87 => "BGRA8_UNORM",
        91 => "BGRA8_UNORM_SRGB",
        80 => "BC4_UNORM",
        83 => "BC5_UNORM",
        98 => "BC7_UNORM",
        99 => "BC7_UNORM_SRGB",
        _ => $"DXGI_{dxgiFormat}"
    };

    private static bool FormatsMatch(string? ddsFormat, string expectedTexconv, string expectedPixelFormat)
    {
        if (string.IsNullOrWhiteSpace(ddsFormat))
            return true;

        if (ddsFormat.Equals(expectedTexconv, StringComparison.OrdinalIgnoreCase))
            return true;

        if (NormalizeTexconvFormat(ddsFormat).Equals(
                NormalizeTexconvFormat(expectedTexconv),
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ddsFormat.Contains("DXT1", StringComparison.OrdinalIgnoreCase) &&
            expectedPixelFormat.Contains("DXT1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ddsFormat.Contains("DXT5", StringComparison.OrdinalIgnoreCase) &&
            (expectedPixelFormat.Contains("DXT5", StringComparison.OrdinalIgnoreCase) ||
             expectedPixelFormat.Contains("BC3", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return ddsFormat.Contains(expectedTexconv, StringComparison.OrdinalIgnoreCase) ||
               expectedTexconv.Contains(ddsFormat, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTexconvFormat(string format) =>
        format.Replace("_SRGB", string.Empty, StringComparison.OrdinalIgnoreCase);
}
