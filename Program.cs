using System.Runtime.InteropServices;

class RawInputDeviceLogger
{
    private const int WS_OVERLAPPEDWINDOW = 0xcf0000;
    private const int WM_INPUT = 0x00FF;
    private const uint RIDEV_INPUTSINK = 0x100;
    private const uint RID_INPUT = 0x10000003;
    private const int RIM_TYPEKEYBOARD = 1;
    private const int SW_HIDE = 0;

    private static IntPtr BarcodeScannerDeviceId = IntPtr.Zero; // Store the device ID of the barcode scanner

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWKEYBOARD keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, int cbSizeHeader);

    [StructLayout(LayoutKind.Sequential)]
    struct WNDCLASS
    {
        public uint style;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hWnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int x;
        public int y;
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    static void Main()
    {
        var wc = new WNDCLASS
        {
            lpszClassName = "RawInputConsoleClass",
            lpfnWndProc = CustomWndProc
        };

        RegisterClassW(ref wc);
        IntPtr hwnd = CreateWindowExW(0, wc.lpszClassName, "RawInputConsole",
            WS_OVERLAPPEDWINDOW, 0, 0, 100, 100, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        ShowWindow(hwnd, SW_HIDE);
        RegisterKeyboardRawInput(hwnd);

        Console.WriteLine("Listening for raw keyboard input. Press Ctrl+C to exit.");

        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    private static void RegisterKeyboardRawInput(IntPtr hwnd)
    {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
        rid[0].usUsagePage = 0x01;
        rid[0].usUsage = 0x06;
        rid[0].dwFlags = RIDEV_INPUTSINK;
        rid[0].hwndTarget = hwnd;
        RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
    }

    private static IntPtr CustomWndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WM_INPUT)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, Marshal.SizeOf(typeof(RAWINPUTHEADER))) > 0)
                {
                    var raw = Marshal.PtrToStructure<RAWINPUT>(buffer);
                    if (raw.header.dwType == RIM_TYPEKEYBOARD)
                    {
                        string deviceName = GetDeviceName(raw.header.hDevice);
                        if (deviceName.Contains("HID#VID_E851&PID_2100")) // Filter input by specific device  
                        {
                            if ((raw.keyboard.Flags & 0x01) == 0) // Key down event  
                            {
                                if (raw.keyboard.VKey == (ushort)ConsoleKey.Enter)
                                {
                                    Console.WriteLine(); // New line for Enter key  
                                }
                                else
                                {
                                    // Print device ID only once per row unless Enter is hit  
                                    if (Console.CursorLeft == 0)
                                    {
                                        Console.Write($"DeviceId: {deviceName} | Input: ");
                                    }
                                    Console.Write((char)raw.keyboard.VKey); // Print character  
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static string GetDeviceName(IntPtr hDevice)
    {
        uint size = 0;
        GetRawInputDeviceInfo(hDevice, 0x20000007, IntPtr.Zero, ref size);
        IntPtr pData = Marshal.AllocHGlobal((int)size);

        try
        {
            if (GetRawInputDeviceInfo(hDevice, 0x20000007, pData, ref size) > 0)
            {
                return Marshal.PtrToStringAnsi(pData);
            }
            return string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(pData);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
}
