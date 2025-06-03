#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour
{
    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;    private const int SW_RESTORE = 9;
    private static readonly IntPtr HWND_TOPMOST = new(-1);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;    }

    [SerializeField] private bool enableTopMost = true;
    [SerializeField] private bool enableClickThrough = true;
    
    private EventSystem eventSystem;
    private bool wasOverUI = false;    private IntPtr windowHandle = IntPtr.Zero;
    private Camera mainCamera;

    private void Awake()
    {
        if (!IsWindowsPlatform())
        {
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (!enabled) return;

        InitializeWindow();
        SetupCameraTransparency();
          eventSystem = FindFirstObjectByType<EventSystem>();
    }

    private void Update()
    {
        if (!enableClickThrough || eventSystem == null) return;

        HandleUIInteraction();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && enableTopMost)
        {
            MaintainTopMostState();        }
    }

    private bool IsWindowsPlatform()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer || 
               Application.platform == RuntimePlatform.WindowsEditor;
    }

    private void InitializeWindow()
    {        try
        {
            windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            if (enableTopMost)
            {
                SetTopMost();
            }

            SetTransparentWindow();
            SetTransparentMargins();        }
        catch (Exception)
        {
        }
    }

    private void SetupCameraTransparency()
    {
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.clear;
        mainCamera.allowHDR = false;
        mainCamera.allowMSAA = false;
    }

    private void SetTopMost()
    {
        SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    private void SetTransparentWindow()
    {
        uint style = enableClickThrough ? (WS_EX_LAYERED | WS_EX_TRANSPARENT) : WS_EX_LAYERED;
        SetWindowLong(windowHandle, GWL_EXSTYLE, style);
    }

    private void SetTransparentMargins()
    {
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(windowHandle, ref margins);
    }

    private void HandleUIInteraction()
    {
        bool isOverUI = eventSystem.IsPointerOverGameObject();
        
        if (isOverUI != wasOverUI)
        {
            SetClickthrough(!isOverUI);
            wasOverUI = isOverUI;
        }
    }

    private void SetClickthrough(bool clickthrough)
    {
        if (windowHandle == IntPtr.Zero) return;

        try
        {
            uint style = clickthrough ? (WS_EX_LAYERED | WS_EX_TRANSPARENT) : WS_EX_LAYERED;
            SetWindowLong(windowHandle, GWL_EXSTYLE, style);        }
        catch (Exception)
        {
        }
    }

    private void MaintainTopMostState()
    {
        if (windowHandle == IntPtr.Zero) return;

        try
        {
            ShowWindow(windowHandle, SW_RESTORE);
            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, 
                        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);        }
        catch (Exception)        {
        }
    }

    public void SetTopMostEnabled(bool enabled)
    {
        enableTopMost = enabled;
        if (enabled && windowHandle != IntPtr.Zero)
        {
            SetTopMost();
        }
    }

    public void SetClickThroughEnabled(bool enabled)
    {
        enableClickThrough = enabled;
        if (windowHandle != IntPtr.Zero)
        {
            SetTransparentWindow();
        }
    }
}
#endif