using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Diagnostics;

namespace Q2EMultiplayerPlus.Models;

internal class StartupObjects
{
    public IStartupScanner scanner = null!;
    public IScannerFactory scannerFactory = null!;
    public Process process = null!;
    public ProcessModule mainModule = null!;
    public ProcessModule playfabModule = null!;
}
