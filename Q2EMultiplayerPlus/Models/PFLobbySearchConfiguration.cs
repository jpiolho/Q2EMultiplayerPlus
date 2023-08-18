using System.Runtime.InteropServices;

namespace Q2EMultiplayerPlus.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct PFLobbySearchConfiguration
{
    public nint friendsFilter;
    public nint filterString;
    public nint sortString;
    public nint clientSearchResultCount;
}
