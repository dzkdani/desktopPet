#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickThroughViaHitTest : MonoBehaviour
{
    const int    GWL_WNDPROC   = -4;
    const int    GWL_EXSTYLE   = -20;
    const uint   WS_EX_LAYERED = 0x00080000;
    const int    WM_NCHITTEST  = 0x0084;
    const int    HTCLIENT      = 1;
    const int    HTTRANSPARENT = -1;
    const uint   LWA_ALPHA     = 0x00000002;

    [DllImport("user32.dll", EntryPoint="SetWindowLongPtr", SetLastError=true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtr", SetLastError=true)]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    static extern IntPtr CallWindowProc(IntPtr prevProc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("Shcore.dll")]
    static extern int SetProcessDpiAwareness(int awareness);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate _newProc;
    private IntPtr _oldProc, _hWnd;

    void Awake()
    {
        Application.runInBackground = true;           // keep ticking when unfocused
        SetProcessDpiAwareness(2);                    // per-monitor DPI
    }

    void Start()
    {
        _hWnd = Process.GetCurrentProcess().MainWindowHandle;

        // allow per-pixel alpha
        long ex = GetWindowLongPtr(_hWnd, GWL_EXSTYLE).ToInt64();
        ex |= WS_EX_LAYERED;
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, new IntPtr(ex));

        // use camera’s alpha = 0 for transparency
        SetLayeredWindowAttributes(_hWnd, 0, 255, LWA_ALPHA);

        // hook into hit-test
        _newProc = CustomWndProc;
        _oldProc = SetWindowLongPtr(_hWnd, GWL_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(_newProc));
    }

    void OnDestroy()
    {
        if (_oldProc != IntPtr.Zero)
            SetWindowLongPtr(_hWnd, GWL_WNDPROC, _oldProc);
    }

    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCHITTEST)
        {
            // extract mouse coords
            int x = (short)(lParam.ToInt64() & 0xFFFF);
            int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
            // UI raycast
            var evt = new PointerEventData(EventSystem.current) { position = new Vector2(x, y) };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(evt, hits);
            return (hits.Count > 0)
                ? new IntPtr(HTCLIENT)      // click goes to Unity UI
                : new IntPtr(HTTRANSPARENT);// click falls through desktop
        }
        return CallWindowProc(_oldProc, hWnd, msg, wParam, lParam);
    }
}
#endif
