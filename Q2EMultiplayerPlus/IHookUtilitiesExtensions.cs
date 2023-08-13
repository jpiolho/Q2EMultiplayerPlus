using Reloaded.Hooks.Definitions;

namespace QuakeReloaded.Utilities;

public static class IHookUtilitiesExtensions
{
    public static string PushAllX64(this IReloadedHooksUtilities utilities)
    {
        return @"push rax
push rbx
push rcx
push rdx
push rsi
push rdi
push rbp
push rsp
push r8
push r9
push r10
push r11
push r12
push r13
push r14
push r15";
    }

    public static string PopAllX64(this IReloadedHooksUtilities utilities)
    {
        return @"pop r15
pop r14
pop r13
pop r12
pop r11
pop r10
pop r9
pop r8
pop rsp
pop rbp
pop rdi
pop rsi
pop rdx
pop rcx
pop rbx
pop rax";
    }

    public static string PushSseCallConvRegistersX64(this IReloadedHooksUtilities utilities)
    {
        return @"sub rsp, 128
movdqu  dqword [rsp + 0], xmm0
movdqu  dqword [rsp + 16], xmm1
movdqu  dqword [rsp + 32], xmm2
movdqu  dqword [rsp + 48], xmm3
movdqu  dqword [rsp + 64], xmm4
movdqu  dqword [rsp + 80], xmm5
movdqu  dqword [rsp + 96], xmm6
movdqu  dqword [rsp + 112], xmm7";
    }

    public static string PopSseCallConvRegistersX64(this IReloadedHooksUtilities utilities)
    {
        return @"movdqu  xmm0, dqword [rsp + 0]
movdqu  xmm1, dqword [rsp + 16]
movdqu  xmm2, dqword [rsp + 32]
movdqu  xmm3, dqword [rsp + 48]
movdqu  xmm4, dqword [rsp + 64]
movdqu  xmm5, dqword [rsp + 80]
movdqu  xmm6, dqword [rsp + 96]
movdqu  xmm7, dqword [rsp + 112]
add rsp, 128";
    }
}
