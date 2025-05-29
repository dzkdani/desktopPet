#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour
{
    //penting
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("Dwmapi.dll")] private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    const int SW_RESTORE = 9;



    private struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    private EventSystem eventSystem;
    private bool wasOverUI = false;


    private void Start()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer)
        {
            Debug.LogWarning("TransparentWindow script is only supported on Windows platforms.");
            return;
        }

        // Setup came ra transparency
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0, 0, 0, 0);
        Camera.main.allowHDR = false;
        Camera.main.allowMSAA = false;

        // Get window handle and setup window
        IntPtr hWnd = GetActiveWindow();
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);

        // Set transparent margins //penting
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        // Get reference to EventSystem
        eventSystem = EventSystem.current;
    }

    private void Update()
    {
        // Check if mouse is over any UI element
        bool isOverUI = eventSystem.IsPointerOverGameObject();

        // Only update if state changed
        if (isOverUI != wasOverUI)
        {
            SetClickthrough(!isOverUI);
            wasOverUI = isOverUI;
        }
    }
    // void MakeTopMost()
    // {
    //     IntPtr hWnd = GetActiveWindow();
    //     SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
    // }
    // void  OnApplicationFocus(bool hasFocus)
    // {
    //     if (hasFocus) MakeTopMost();
    // }

    //penting
    private void SetClickthrough(bool clickthrough)
    {
        IntPtr hWnd = GetActiveWindow();
        if (clickthrough)
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            IntPtr hWnd = GetActiveWindow();
            // When losing focus, ensure window stays visible
            ShowWindow(hWnd, SW_RESTORE);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }                                                                                                                                                                                                         
}  
#endif                                      