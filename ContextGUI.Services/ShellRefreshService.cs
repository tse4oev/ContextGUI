using System;
using System.Runtime.InteropServices;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Uses SHChangeNotify to refresh Explorer.
/// </summary>
public sealed class ShellRefreshService : IShellRefreshService
{
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfFlush = 0x1000;

    public bool TryRefresh()
    {
        try
        {
            SHChangeNotify(ShcneAssocChanged, ShcnfFlush, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
