#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour
{
    // ── Win32 constants ─────────────────────────────────────────────────────────
    enum PROCESS_DPI_AWARENESS { Unaware = 0, System_DPI_Aware = 1, Per_Monitor_DPI_Aware = 2 }
    const uint DWMWA_NCRENDERING_POLICY = 2, DWMNCRP_DISABLE = 0;
    const int  GWL_STYLE   = -16, GWL_EXSTYLE   = -20;
    const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    const uint WS_EX_LAYERED       = 0x00080000;
    const uint WS_EX_TRANSPARENT   = 0x00000020;
    const uint WS_EX_TOOLWINDOW    = 0x00000080;
    const uint WS_EX_APPWINDOW     = 0x00040000;
    const uint LWA_COLORKEY        = 0x00000001;
    const uint MAGENTA_COLORKEY    = 0x00FF00FF;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOMOVE     = 0x0002;
    const uint SWP_NOSIZE     = 0x0001;
    const uint SWP_SHOWWINDOW = 0x0040;

    // ── Win32 imports ────────────────────────────────────────────────────────────
    [DllImport("Shcore.dll")]         static extern int    SetProcessDpiAwareness(PROCESS_DPI_AWARENESS v);
    [DllImport("dwmapi.dll")]         static extern int    DwmSetWindowAttribute(IntPtr h, uint a, ref uint v, uint s);
    [DllImport("user32.dll")]         static extern IntPtr GetWindowLongPtr(IntPtr h, int i);
    [DllImport("user32.dll")]         static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);
    [DllImport("user32.dll")]         static extern bool   SetWindowPos(IntPtr h, IntPtr hi, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")]         static extern bool   SetLayeredWindowAttributes(IntPtr h, uint key, byte a, uint f);
    [DllImport("user32.dll")]         static extern bool   SetForegroundWindow(IntPtr h);

    IntPtr _hWnd;
    EventSystem _es;
    bool _wasOverUI;

    void Awake()
    {
        _es = EventSystem.current;
        _wasOverUI = false;
        // keep running even when unfocused
        Application.runInBackground = true;
        // correct DPI on multi‐monitor setups
        SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Per_Monitor_DPI_Aware);
    }

    void Start()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return;
        AcquireWindowHandle();
        DisableDwmShadow();
        StripWindowChrome();
        ApplyBaseExStyles();
        ApplyColorKeyTransparency();
        PositionTopmost();
    }

    void Update() => ToggleClickThrough();

    // ── Helpers ─────────────────────────────────────────────────────────────────

    void AcquireWindowHandle()
    {
        // more robust than FindWindow
        _hWnd = Process.GetCurrentProcess().MainWindowHandle;
        SetForegroundWindow(_hWnd);
    }

    void DisableDwmShadow()
    {
        uint val = DWMNCRP_DISABLE;
        DwmSetWindowAttribute(_hWnd, DWMWA_NCRENDERING_POLICY, ref val, sizeof(uint));
    }

    void StripWindowChrome()
    {
        long style = GetWindowLongPtr(_hWnd, GWL_STYLE).ToInt64();
        style &= ~(long)WS_OVERLAPPEDWINDOW;
        SetWindowLongPtr(_hWnd, GWL_STYLE, new IntPtr(style));
    }

    void ApplyBaseExStyles()
    {
        long ex = GetWindowLongPtr(_hWnd, GWL_EXSTYLE).ToInt64();
        ex &= ~(long)WS_EX_TOOLWINDOW;        // show in taskbar
        ex |=  WS_EX_APPWINDOW;
        ex |=  WS_EX_LAYERED;                 // allow per-pixel alpha
        // ex &= ~(long)WS_EX_TRANSPARENT;       // start _not_ click-through
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    void ApplyColorKeyTransparency()
    {
        // fallback for alpha
        SetLayeredWindowAttributes(_hWnd, MAGENTA_COLORKEY, 0, LWA_COLORKEY);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(1, 0, 1, 1);
    }

    void PositionTopmost()
    {
        // topmost but _activatable_ (no SWP_NOACTIVATE)
        uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW;
        SetWindowPos(_hWnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
    }

    void ToggleClickThrough()
    {
        bool overUI = _es.IsPointerOverGameObject();
        if (overUI == _wasOverUI) return;
        long ex = GetWindowLongPtr(_hWnd, GWL_EXSTYLE).ToInt64();
        if (overUI)
            ex &= ~(long)WS_EX_TRANSPARENT;   // catch clicks on UI
        else
            ex |=  (long)WS_EX_TRANSPARENT;   // pass clicks through
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, new IntPtr(ex));

        // optional: refocus when re-enabling clicks
        if (overUI) SetForegroundWindow(_hWnd);
        _wasOverUI = overUI;
    }
}
#endif
