using Reloaded.Memory.Sigscan.Definitions.Structs;
using System.Diagnostics;

namespace Q2EMultiplayerPlus;

public static class ProcessModuleExtensions
{
    public static nint GetOffset(this ProcessModule module, int offset) => module.BaseAddress + offset;
    public static nint GetOffset(this ProcessModule module, PatternScanResult result) => module.BaseAddress + result.Offset;
}
