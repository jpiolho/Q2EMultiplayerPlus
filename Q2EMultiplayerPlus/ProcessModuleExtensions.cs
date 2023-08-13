using Reloaded.Memory.Sigscan.Definitions.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2EMultiplayerPlus;

public static class ProcessModuleExtensions
{
    public static nint GetOffset(this ProcessModule module, int offset) => module.BaseAddress + offset;
    public static nint GetOffset(this ProcessModule module, PatternScanResult result) => module.BaseAddress + result.Offset;
}
