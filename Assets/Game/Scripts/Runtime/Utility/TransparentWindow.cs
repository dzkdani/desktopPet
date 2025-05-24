#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour
{
    // Make the process DPI-aware
    enum PROCESS_DPI_AWARENESS { Unaware = 0, System_DPI_Aware = 1, Per_Monitor_DPI_Aware = 2 }
    [DllImport("Shcore.dll")] static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS v);

    // DWM shadow disable
    const uint DWMWA_NCRENDERING_POLICY = 2;
    const uint DWMNCRP_DISABLE = 0;
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref uint val, uint size);

    // Window styles
    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const uint WS_EX_TOOLWINDOW = 0x00000080;

    // Color-key constants
    const uint LWA_COLORKEY = 0x00000001;
    const uint MAGENTA_COLORKEY = 0x00FF00FF;  // BBGGRR

    // Z-order and flags
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_SHOWWINDOW = 0x0040;

    // Native imports
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    // For layered color-key
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    IntPtr hWnd;
    EventSystem eventSystem;
    bool wasOverUI;

    void Awake()
    {
        Application.runInBackground = true;
        // DPI awareness for correct monitor resolution
        SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Per_Monitor_DPI_Aware);
    }

    void Start()
    {
        // Only run on standalone Windows
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return;

        // Grab the Unity window handle
        // hWnd = FindWindow(null, Application.productName);
        hWnd = Process.GetCurrentProcess().MainWindowHandle;
        SetForegroundWindow(hWnd);

        // Disable the DWM drop shadow for pixel-perfect edges
        uint policy = DWMNCRP_DISABLE;
        DwmSetWindowAttribute(hWnd, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(uint));

        // Remove title bar and borders
        long style = GetWindowLongPtr(hWnd, GWL_STYLE).ToInt64();
        style &= ~((long)WS_OVERLAPPEDWINDOW);
        SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr(style));

        // Layered window + click-through + hide from Alt-Tab
        // Before you do the SetWindowLongPtr for exstyles, change this:
        long ex = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();

        // Remove the tool‐window bit:
        ex &= ~(long)WS_EX_TOOLWINDOW;

        // Add the app‐window bit so it appears in the taskbar:
        const uint WS_EX_APPWINDOW = 0x00040000;
        ex |= WS_EX_APPWINDOW;

        // Keep your layered & transparent bits:
        ex |= WS_EX_LAYERED;                // keep your layered/transparency support
        ex &= ~(long)WS_EX_TRANSPARENT;     // ensure we can receive mouse events
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(ex));


        // Apply magenta color-key for transparency fallback
        SetLayeredWindowAttributes(hWnd, MAGENTA_COLORKEY, 0, LWA_COLORKEY);

        // Unity must use D3D11 and "Transparent Window" in Player Settings for full alpha,
        // but this color-key covers older versions.
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(1, 0, 1, 1);

        // Maximize to work-area (stops at taskbar)
        Screen.fullScreenMode = FullScreenMode.MaximizedWindow;

        // Keep above all other windows (taskbar remains above)
        SetWindowPos(hWnd, HWND_TOPMOST,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        // Prepare for UI click-through toggling
        eventSystem = EventSystem.current;
        wasOverUI = false;
    }

    void Update()
    {
        // Toggle click-through when pointer over UI
        ToggleClickThrough();

        // catch Alt+F4 and quit
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool f4 = Input.GetKeyDown(KeyCode.F4);
        if (alt && f4)
        {
            Application.Quit();
        }
    }
    private void ToggleClickThrough()
    {
        // make sure we’ve got a window handle and an EventSystem
        if (hWnd == IntPtr.Zero || EventSystem.current == null)
            return;

        // build pointer data at the current mouse pos:
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        // raycast into ALL canvases
        var hits = new System.Collections.Generic.List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, hits);

        bool overUI = hits.Count > 0;
        if (overUI == wasOverUI) return;
   
        long ex = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
        if (overUI)
            ex &= ~(long)WS_EX_TRANSPARENT;   // catch clicks on UI
        else
            ex |= (long)WS_EX_TRANSPARENT;   // let clicks pass through
   
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(ex));
        if (overUI) SetForegroundWindow(hWnd);

        wasOverUI = overUI;

        UnityEngine.Debug.Log($"ToggleClickThrough → overUI={overUI}  WS_EX_TRANSPARENT={(ex & WS_EX_TRANSPARENT) != 0}");

    }

    void OnApplicationFocus(bool focus)
    {
        
    }

}
#endif