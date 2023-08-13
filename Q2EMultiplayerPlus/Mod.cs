using Q2EMultiplayerPlus.Template;
using QuakeReloaded.Utilities;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace Q2EMultiplayerPlus
{
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
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _modConfig = context.ModConfig;


            if (_hooks is null)
                throw new Exception("Hooks is null");

            var mainModule = Process.GetCurrentProcess().MainModule!;
            if (!_modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner))
                throw new Exception("Failed to get scanner");

            HookMaxPlayers(mainModule, scanner);
        }

        private void HookMaxPlayers(ProcessModule mainModule, IStartupScanner scanner)
        {
            // Maxplayers limit
            scanner.AddMainModuleScan("B8 10 00 00 00 44 3B F0", result =>
            {
                var offset = mainModule.BaseAddress + result.Offset;

                _hooks!.CreateAsmHook(new[]
                {
                    $"use64",
                    $"mov eax, 0x20",
                    $"cmp r14d, eax"
                }, offset, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            scanner.AddMainModuleScan("B9 10 00 00 00 3B C1", result =>
            {
                var offset = mainModule.BaseAddress + result.Offset;

                _hooks!.CreateAsmHook(new[]
                {
                    $"use64",
                    $"mov rcx, 0x20",
                    $"cmp rax, rcx"
                }, offset, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            // Bot max limit
            scanner.AddMainModuleScan("B9 10 00 00 00 0F 44 F9", result =>
            {
                var offset = mainModule.BaseAddress + result.Offset;

                _hooks!.CreateAsmHook(new[]
                {
                    $"use64",
                    $"mov rcx, 0x20",
                    $"cmove rdi, rcx"
                }, offset, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
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


        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}