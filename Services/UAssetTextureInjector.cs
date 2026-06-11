using System.Diagnostics;

using System.IO;

using CUE4Parse.FileProvider;

using CUE4Parse.UE4.Assets;

using CUE4Parse.UE4.Assets.Exports.Texture;

using CUE4Parse.UE4.Assets.Objects;

using OutlastTrialsMod.Helpers;

using OutlastTrialsMod.Models;

using static CUE4Parse.UE4.Assets.Objects.EBulkDataFlags;



namespace OutlastTrialsMod.Services;



internal static class UAssetTextureInjector

{

    private const int UexpPackageFooterSize = 4;



    private sealed record InjectableMip(

        int Size,

        int UbulkOffset,

        bool IsSeparateFile,

        TBulkData<byte> Bulk,

        byte[]? ExistingInlineBytes);



    public static void InjectDdsIntoTextureAsset(

        string originalUassetPath,

        string ddsPath,

        TextureInjectionMetadata metadata)

    {

        if (!File.Exists(originalUassetPath))

            throw new FileNotFoundException($"Asset not found: {originalUassetPath}");



        if (!File.Exists(ddsPath))

            throw new FileNotFoundException($"DDS not found: {ddsPath}");



        var provider = Cue4ParseService.Instance.Provider

            ?? throw new InvalidOperationException("File provider is not initialized. Open the game directory first.");



        var generatedDdsBytes = File.ReadAllBytes(ddsPath);

        DdsPayloadReader.ValidatePayloadFormat(generatedDdsBytes, metadata.PixelFormat, metadata.IsSrgb);



        var package = LoadLocalPackage(originalUassetPath, provider);

        var texture = ((IPackage)package).GetExports().OfType<UTexture2D>().FirstOrDefault()

            ?? throw new InvalidOperationException($"UTexture2D export not found in asset: {originalUassetPath}");



        var basePath = Path.Combine(

            Path.GetDirectoryName(originalUassetPath) ?? string.Empty,

            Path.GetFileNameWithoutExtension(originalUassetPath));

        var uexpPath = basePath + ".uexp";

        var ubulkPath = basePath + ".ubulk";



        if (!File.Exists(uexpPath) && !File.Exists(ubulkPath))

        {

            throw new FileNotFoundException(

                $"Missing .uexp/.ubulk for texture injection: {basePath}.{{uexp|ubulk}}");

        }



        var originalUbulkBytes = File.Exists(ubulkPath) ? File.ReadAllBytes(ubulkPath) : Array.Empty<byte>();

        var injectableMips = BuildInjectableMipList(texture, originalUbulkBytes);

        if (injectableMips.Count == 0)

        {

            throw new InvalidOperationException(

                $"No injectable bulk payloads found for asset: {originalUassetPath}");

        }



        var expectedRawDataSize = injectableMips.Sum(mip => mip.Size);

        var headerSize = DdsPayloadReader.ResolveHeaderSize(generatedDdsBytes, expectedRawDataSize);

        var rawPayload = DdsPayloadReader.ExtractRawPayload(generatedDdsBytes, expectedRawDataSize);



        Debug.WriteLine(

            $"[DDS] {Path.GetFileName(ddsPath)}: header={headerSize} bytes, " +

            $"rawPayload={rawPayload.Length} bytes, expected={expectedRawDataSize} bytes");



        if (rawPayload.Length != expectedRawDataSize)

        {

            throw new InvalidOperationException(

                $"Byte mismatch! Expected {expectedRawDataSize}, got {rawPayload.Length}");

        }



        var ddsMipSizes = DdsPayloadReader.ComputeMipChainSizes(

            metadata.HeaderWidth,

            metadata.HeaderHeight,

            metadata.PixelFormat,

            metadata.MipCount);



        var separateMips = injectableMips.Where(mip => mip.IsSeparateFile).ToList();

        var inlineMips = injectableMips.Where(mip => !mip.IsSeparateFile).ToList();



        Debug.WriteLine(

            $"[Inject] {Path.GetFileName(originalUassetPath)}: separateMips={separateMips.Count}, " +

            $"inlineMips={inlineMips.Count}");



        if (separateMips.Count > 0)

        {

            if (!File.Exists(ubulkPath))

            {

                throw new FileNotFoundException($"Missing .ubulk for separate bulk injection: {ubulkPath}");

            }



            var ubulkBytes = (byte[])originalUbulkBytes.Clone();

            InjectMipSlices(rawPayload, separateMips, injectableMips, ddsMipSizes, ubulkBytes, getTargetOffset: mip => mip.UbulkOffset);

            File.WriteAllBytes(ubulkPath, ubulkBytes);

            Debug.WriteLine($"[Inject] Saved ubulk: {ubulkPath} ({ubulkBytes.Length} bytes)");

        }


    if (inlineMips.Count > 0)
    {
    if (!File.Exists(uexpPath)) throw new FileNotFoundException($"Missing .uexp: {uexpPath}");

    var inlinePayload = inlineMips.Count == injectableMips.Count
        ? rawPayload
        : ExtractInlinePayload(rawPayload, injectableMips, inlineMips, ddsMipSizes);

    int expectedInlineSize = inlineMips.Sum(m => m.Size);
    if (inlinePayload.Length != expectedInlineSize)
        throw new Exception($"Ошибка размера! Ожидали {expectedInlineSize}, получили {inlinePayload.Length}");

    var uexpBytes = File.ReadAllBytes(uexpPath);
    int originalUexpSize = uexpBytes.Length;

    int payloadCursor = 0;
    int currentSearchStartIndex = 0;

    foreach (var mip in inlineMips)
    {
        var oldPixels = mip.ExistingInlineBytes;
        if (oldPixels == null || oldPixels.Length == 0)
            throw new Exception("Отсутствуют оригинальные байты для одного из слоев!");

        int exactInjectOffset = -1;

        for (int i = currentSearchStartIndex; i <= uexpBytes.Length - oldPixels.Length; i++)
        {
            bool match = true;
            if (uexpBytes[i] == oldPixels[0] && uexpBytes[i + oldPixels.Length - 1] == oldPixels[oldPixels.Length - 1])
            {
                for (int j = 0; j < oldPixels.Length; j++)
                {
                    if (uexpBytes[i + j] != oldPixels[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    exactInjectOffset = i;
                    break;
                }
            }
        }

        if (exactInjectOffset == -1)
            throw new Exception($"Критическая ошибка: Не удалось найти слой размером {oldPixels.Length} байт!");

        Array.Copy(inlinePayload, payloadCursor, uexpBytes, exactInjectOffset, oldPixels.Length);

        payloadCursor += oldPixels.Length;
        currentSearchStartIndex = exactInjectOffset + oldPixels.Length; 
    }

    if (uexpBytes.Length != originalUexpSize)
        throw new Exception("Размер файла изменился! Сохранение отменено.");

    File.WriteAllBytes(uexpPath, uexpBytes);
    Debug.WriteLine($"[Inject] Успешно сохранен многослойный uexp: {uexpPath}");
}
}

    private static void InjectMipSlices(

        byte[] rawPayload,

        IReadOnlyList<InjectableMip> targetMips,

        IReadOnlyList<InjectableMip> allMips,

        IReadOnlyList<int> ddsMipSizes,

        byte[] targetBytes,

        Func<InjectableMip, int> getTargetOffset)

    {

        var ddsCursor = 0;

        var ddsMipIndex = 0;



        foreach (var injectableMip in allMips)

        {

            while (ddsMipIndex < ddsMipSizes.Count && ddsMipSizes[ddsMipIndex] != injectableMip.Size)

            {

                ddsCursor += ddsMipSizes[ddsMipIndex];

                ddsMipIndex++;

            }



            if (ddsMipIndex >= ddsMipSizes.Count)

            {

                throw new InvalidOperationException(

                    $"DDS mip chain does not contain a level of {injectableMip.Size} bytes.");

            }



            if (ddsCursor + injectableMip.Size > rawPayload.Length)

            {

                throw new InvalidOperationException(

                    $"DDS payload ends before mip data at offset {ddsCursor} " +

                    $"(need {injectableMip.Size} bytes).");

            }



            if (targetMips.Contains(injectableMip))

            {

                var targetOffset = getTargetOffset(injectableMip);

                if (targetOffset < 0 || targetOffset + injectableMip.Size > targetBytes.Length)

                {

                    throw new InvalidOperationException(

                        $"Injection target out of range (offset={targetOffset}, " +

                        $"size={injectableMip.Size}, buffer={targetBytes.Length}).");

                }



                Array.Copy(rawPayload, ddsCursor, targetBytes, targetOffset, injectableMip.Size);

                Debug.WriteLine(

                    $"[Inject] Wrote mip slice at target offset {targetOffset}, size={injectableMip.Size}");

            }



            ddsCursor += injectableMip.Size;

            ddsMipIndex++;

        }

    }



    private static byte[] ExtractInlinePayload(

        byte[] rawPayload,

        IReadOnlyList<InjectableMip> allMips,

        IReadOnlyList<InjectableMip> inlineMips,

        IReadOnlyList<int> ddsMipSizes)

    {

        using var stream = new MemoryStream();

        var ddsCursor = 0;

        var ddsMipIndex = 0;



        foreach (var injectableMip in allMips)

        {

            while (ddsMipIndex < ddsMipSizes.Count && ddsMipSizes[ddsMipIndex] != injectableMip.Size)

            {

                ddsCursor += ddsMipSizes[ddsMipIndex];

                ddsMipIndex++;

            }



            if (ddsMipIndex >= ddsMipSizes.Count)

            {

                throw new InvalidOperationException(

                    $"DDS mip chain does not contain a level of {injectableMip.Size} bytes.");

            }



            if (inlineMips.Contains(injectableMip))

            {

                stream.Write(rawPayload, ddsCursor, injectableMip.Size);

            }



            ddsCursor += injectableMip.Size;

            ddsMipIndex++;

        }



        return stream.ToArray();

    }



    private static List<InjectableMip> BuildInjectableMipList(UTexture2D texture, byte[] originalUbulkBytes)

    {

        var injectableMips = new List<InjectableMip>();



        foreach (var mip in texture.PlatformData.Mips)

        {

            var bulk = mip.BulkData;

            var mipSize = bulk is not null ? (int)bulk.Header.SizeOnDisk : 0;

            if (mipSize <= 0)

            {

                mipSize = DdsPayloadReader.ComputeCompressedMipSize(

                    mip.SizeX,

                    mip.SizeY,

                    texture.Format.ToString());

            }



            if (mipSize <= 0 || bulk is null || bulk.BulkDataFlags.HasFlag(BULKDATA_Unused))

                continue;



            if (bulk.BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))

            {

                var offset = (int)bulk.Header.OffsetInFile;

                if (offset < 0 || offset + mipSize > originalUbulkBytes.Length)

                    continue;



                injectableMips.Add(new InjectableMip(mipSize, offset, true, bulk, null));

                continue;

            }



            if (!TryGetInlineMipBytes(bulk, mipSize, out var existingBytes))

                continue;



            injectableMips.Add(new InjectableMip(mipSize, -1, false, bulk, existingBytes));

        }



        return injectableMips;

    }



    private static bool TryGetInlineMipBytes(TBulkData<byte> bulk, int mipSize, out byte[]? existingBytes)

    {

        existingBytes = null;



        if (bulk is FByteBulkData byteBulk)

        {

            existingBytes = byteBulk.ReadDataOnce();

            return existingBytes is { Length: > 0 };

        }



        existingBytes = bulk.Data;

        return existingBytes is { Length: > 0 };

    }



    private static IPackage LoadLocalPackage(string uassetPath, IFileProvider provider)

    {

        var basePath = Path.Combine(

            Path.GetDirectoryName(uassetPath) ?? string.Empty,

            Path.GetFileNameWithoutExtension(uassetPath));



        var uassetBytes = File.ReadAllBytes(uassetPath);

        var uexpPath = basePath + ".uexp";

        var ubulkPath = basePath + ".ubulk";

        var uptnlPath = basePath + ".uptnl";



        byte[]? uexpBytes = File.Exists(uexpPath) ? File.ReadAllBytes(uexpPath) : null;

        byte[]? ubulkBytes = File.Exists(ubulkPath) ? File.ReadAllBytes(ubulkPath) : null;

        byte[]? uptnlBytes = File.Exists(uptnlPath) ? File.ReadAllBytes(uptnlPath) : null;



        return new Package(

            Path.GetFileNameWithoutExtension(uassetPath),

            uassetBytes,

            uexpBytes,

            ubulkBytes,

            uptnlBytes,

            provider);

    }

}


