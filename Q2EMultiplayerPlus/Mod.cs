using Q2EMultiplayerPlus.Configuration;
using Q2EMultiplayerPlus.Models;
using Q2EMultiplayerPlus.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.Sigscan.Definitions;
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


        StartupObjects startupObjects = new();

        FillProcessAndModules(startupObjects);

        if (!_modLoader.GetController<IStartupScanner>().TryGetTarget(out startupObjects.scanner!))
            throw new Exception("Failed to get scanner");

        if (!_modLoader.GetController<IScannerFactory>().TryGetTarget(out startupObjects.scannerFactory!))
            throw new Exception("Failed to get scanner factory");

        HookMaxPlayers(startupObjects);
        HookLobbySearchIncrease(startupObjects);
    }

    private void FillProcessAndModules(StartupObjects startupObjects)
    {
        startupObjects.process = Process.GetCurrentProcess();

        // Try to get the modules
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            switch (module.ModuleName.ToUpperInvariant())
            {
                case "PLAYFABMULTIPLAYERWIN.DLL": startupObjects.playfabModule = module; break;
            }
        }

        startupObjects.mainModule = startupObjects.process.MainModule ?? throw new Exception("Failed to get main process module");

        if (startupObjects.playfabModule is null)
            throw new Exception("Failed to get playfab module");
    }

    private void HookLobbySearchIncrease(StartupObjects objects)
    {
        var playfabScanner = objects.scannerFactory.CreateScanner(objects.process, objects.playfabModule);

        // PFMultiplayerFindLobbies
        objects.scanner.AddArbitraryScan(playfabScanner, "40 53 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 78 48 C7 44 24 ?? FE FF FF FF 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 49 8B D9", result =>
        {
            try
            {
                if (!result.Found)
                    throw new Exception("Failed to find PFMultiplayerFindLobbies");


                _hook_pfMultiplayerFindLobbies = _hooks!.CreateHook<PFMultiplayerFindLobbies>(OnPFMultiplayerFindLobbies, objects.playfabModule.GetOffset(result)).Activate();
            }
            finally
            {
                playfabScanner.Dispose();
            }
        });
    }

    private void HookMaxPlayers(StartupObjects objects)
    {
        // Maxplayers limit
        objects.scanner.AddMainModuleScan("B8 10 00 00 00 44 3B F0", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov eax, 0x20",
                $"cmp r14d, eax"
            }, objects.mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        objects.scanner.AddMainModuleScan("B9 10 00 00 00 3B C1", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov rcx, 0x20",
                $"cmp rax, rcx"
            }, objects.mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        // Bot max limit
        objects.scanner.AddMainModuleScan("B9 10 00 00 00 0F 44 F9", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"mov rcx, 0x20",
                $"cmove rdi, rcx"
            }, objects.mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        // Remove cooperative player limit
        objects.scanner.AddMainModuleScan("C7 87 ?? ?? ?? ?? 04 00 00 00 66 0F 6E 8F ?? ?? ?? ??", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64"
            }, objects.mainModule.GetOffset(result), AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        objects.scanner.AddMainModuleScan("76 ?? BA 04 00 00 00 49 8B CD", result =>
        {
            _hooks!.CreateAsmHook(new[]
            {
                $"use64",
                $"cmp eax,0x1000"
            }, objects.mainModule.GetOffset(result), AsmHookBehaviour.ExecuteFirst).Activate();
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
                    Config.LobbySortByEnum.MostPlayers => "lobby/memberCount desc",
                    Config.LobbySortByEnum.LeastPlayers => "lobby/memberCount asc",
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

            if (_nativeAllocs.LobbyMaxSearchCount == nint.Zero)
                _nativeAllocs.LobbyMaxSearchCount = Marshal.AllocHGlobal(sizeof(uint));

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