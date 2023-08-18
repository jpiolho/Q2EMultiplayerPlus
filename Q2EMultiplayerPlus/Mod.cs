using Q2EMultiplayerPlus.Configuration;
using Q2EMultiplayerPlus.Models;
using Q2EMultiplayerPlus.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace Q2EMultiplayerPlus;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;


    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;


    private delegate uint PFMultiplayerFindLobbies(nint handle, nint searchingEntity, nint searchConfiguration, nint asyncContext);
    private IHook<PFMultiplayerFindLobbies> _hook_pfMultiplayerFindLobbies;

    private NativeAllocs _nativeAllocs;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        _nativeAllocs = new();


        if (_hooks is null)
            throw new Exception("Hooks is null");

        // Try to get the modules
        ProcessModule? mainModule, playfabModule = null;
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            switch (module.ModuleName)
            {
                case "PlayFabMultiplayerWin.dll": playfabModule = module; break;
            }
        }
        mainModule = Process.GetCurrentProcess().MainModule;

        if (mainModule == null)
            throw new Exception("Failed to find main module");
        if (playfabModule == null)
            throw new Exception("Failed to find playfab module");

        if (!_modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner))
            throw new Exception("Failed to get scanner");

        HookMaxPlayers(mainModule, scanner);
        HookLobbySearchIncrease(playfabModule, scanner);
    }

    private void HookLobbySearchIncrease(ProcessModule playfabModule, IStartupScanner scanner)
    {
        var playfabScanner = new Scanner(Process.GetCurrentProcess(), playfabModule);

        _nativeAllocs.LobbyMaxSearchCount = Marshal.AllocHGlobal(sizeof(uint));
        _nativeAllocs.LobbySearchFilterBuffer = Marshal.AllocHGlobal(256);

        Marshal.WriteInt32(_nativeAllocs.LobbyMaxSearchCount, 50);

        // PFMultiplayerFindLobbies
        scanner.AddArbitraryScan(playfabScanner, "40 53 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 78 48 C7 44 24 ?? FE FF FF FF 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 49 8B D9", result =>
        {
            try
            {
                if (!result.Found)
                    throw new Exception("Failed to find PFMultiplayerFindLobbies");


                _hook_pfMultiplayerFindLobbies = _hooks!.CreateHook<PFMultiplayerFindLobbies>(OnPFMultiplayerFindLobbies, playfabModule.GetOffset(result)).Activate();
            }
            finally
            {
                playfabScanner.Dispose();
            }
        });
    }

    private void HookMaxPlayers(ProcessModule mainModule, IStartupScanner scanner)
    {
        // Maxplayers limit
        scanner.AddMainModuleScan("B8 10 00 00 00 44 3B F0", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov eax, 0x20",
                $"cmp r14d, eax"
            }, mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        scanner.AddMainModuleScan("B9 10 00 00 00 3B C1", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov rcx, 0x20",
                $"cmp rax, rcx"
            }, mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        // Bot max limit
        scanner.AddMainModuleScan("B9 10 00 00 00 0F 44 F9", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov rcx, 0x20",
                $"cmove rdi, rcx"
            }, mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        // Remove cooperative player limit
        scanner.AddMainModuleScan("C7 87 ?? ?? ?? ?? 04 00 00 00 66 0F 6E 8F ?? ?? ?? ??", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64"
            }, mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        scanner.AddMainModuleScan("76 ?? BA 04 00 00 00 49 8B CD", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"cmp eax,0x1000"
            }, mainModule.GetOffset(result), AsmHookBehaviour.ExecuteFirst).Activate();
        });

    }

    private uint OnPFMultiplayerFindLobbies(nint handle, nint searchingEntity, nint searchConfigurationPointer, nint asyncContext)
    {
        unsafe
        {

            var searchConfiguration = (PFLobbySearchConfiguration*)searchConfigurationPointer;

            // Do lobby filtering query
            string? filterString = null;
            if (_configuration.LobbyFilterByGameMode != Config.LobbyFilterByGameModeEnum.Disabled)
            {
                var gamemodeFilter = _configuration.LobbyFilterByGameMode switch
                {
                    Config.LobbyFilterByGameModeEnum.Deathmatch => "$m_deathmatch",
                    Config.LobbyFilterByGameModeEnum.Cooperative => "$m_coop",
                    Config.LobbyFilterByGameModeEnum.CaptureTheFlag => "$m_ctf",
                    Config.LobbyFilterByGameModeEnum.TeamDeathmatch => "$m_teamplay",
                    _ => throw new Exception($"Unsupported filter: {_configuration.LobbyFilterByGameMode}")
                };

                // string_key1 eq 'sublist\crossplay'
                if (filterString is null)
                    filterString = Marshal.PtrToStringAnsi(searchConfiguration->filterString);

                filterString += $" and string_key5 eq 'gamemode\\{gamemodeFilter}'";
            }

            if (!string.IsNullOrEmpty(_configuration.LobbyAdvancedFilter))
            {
                // string_key1 eq 'sublist\crossplay'
                if (filterString is null)
                    filterString = Marshal.PtrToStringAnsi(searchConfiguration->filterString);

                filterString += $" and {_configuration.LobbyAdvancedFilter}";
            }

            if (filterString != null)
            {
                if (_nativeAllocs.LobbySearchFilterBuffer != nint.Zero)
                    Marshal.FreeHGlobal(_nativeAllocs.LobbySearchFilterBuffer);

                _nativeAllocs.LobbySearchFilterBuffer = Marshal.StringToHGlobalAnsi(filterString);
                searchConfiguration->filterString = _nativeAllocs.LobbySearchFilterBuffer;
            }

            // Do lobby sorting query
            string? sortString = null;
            if (_configuration.LobbySortBy != Config.LobbySortByEnum.Disabled)
            {
                sortString = _configuration.LobbySortBy switch
                {
                    Config.LobbySortByEnum.PlayerCount => "lobby/memberCount desc",
                    _ => throw new Exception($"Unsupported sort: {_configuration.LobbySortBy}")
                };
            }

            if (!string.IsNullOrEmpty(_configuration.LobbyAdvancedSort))
            {
                if (!string.IsNullOrEmpty(sortString))
                    sortString += ",";
                sortString += _configuration.LobbyAdvancedSort;
            }

            if (!string.IsNullOrEmpty(sortString))
            {
                _nativeAllocs.LobbySearchSortBuffer = Marshal.StringToHGlobalAnsi(sortString);
                searchConfiguration->sortString = _nativeAllocs.LobbySearchSortBuffer;
            }


            Marshal.WriteInt32(_nativeAllocs.LobbyMaxSearchCount, Math.Clamp(_configuration.LobbyMaxResultCount, 1, 50));
            searchConfiguration->clientSearchResultCount = _nativeAllocs.LobbyMaxSearchCount;
        }


        return _hook_pfMultiplayerFindLobbies.OriginalFunction.Invoke(handle, searchingEntity, searchConfigurationPointer, asyncContext);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}