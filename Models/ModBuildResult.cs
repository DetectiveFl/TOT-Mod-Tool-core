namespace OutlastTrialsMod.Models;

public sealed class ModBuildResult
{
    public ModBuildResult(bool succeeded, string outputPakPath, string pakFileName, string? log = null)
    {
        Succeeded = succeeded;
        OutputPakPath = outputPakPath;
        PakFileName = pakFileName;
        Log = log;
    }

    public bool Succeeded { get; }
    public string OutputPakPath { get; }
    public string PakFileName { get; }
    public string? Log { get; }
}
