using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2EMultiplayerPlus.Models;

internal class NativeAllocs
{
    public nint LobbyMaxSearchCount { get; set; }
    public nint LobbySearchFilterBuffer { get; set; }
    public nint LobbySearchSortBuffer { get; set; }
}
