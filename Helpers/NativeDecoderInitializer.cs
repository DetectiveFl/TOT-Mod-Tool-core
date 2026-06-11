using System.IO;
using CUE4Parse.Compression;
using CUE4Parse_Conversion.Textures.BC;

namespace OutlastTrialsMod.Helpers;

public static class NativeDecoderInitializer
{
    public static void Initialize()
    {
        InitializeDetex();
        InitializeOodle();
    }

    private static void InitializeDetex()
    {
        try
        {
            var detexPath = Path.Combine(AppContext.BaseDirectory, DetexHelper.DLL_NAME);
            if (!File.Exists(detexPath))
                DetexHelper.LoadDll(detexPath);

            if (File.Exists(detexPath))
                DetexHelper.Initialize(detexPath);
        }
        catch
        {
            // Detex is required for BC/DXT texture decode; failures surface when textures are opened.
        }
    }

    private static void InitializeOodle()
    {
        try
        {
            var oodlePath = Path.Combine(AppContext.BaseDirectory, OodleHelper.OODLE_NAME_OLD);
            if (File.Exists(oodlePath))
                OodleHelper.Initialize(oodlePath);
        }
        catch
        {
            // Oodle may be initialized later via download in Cue4ParseService if missing.
        }
    }
}
