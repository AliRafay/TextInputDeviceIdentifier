//using System;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Text;

//class RawInputListener
//{
//    const int RIDEV_INPUTSINK = 0x00000100;
//    const int RIM_TYPEKEYBOARD = 1;

//    [DllImport("User32.dll")]
//    static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

//    [DllImport("User32.dll")]
//    static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

//    [DllImport("User32.dll")]
//    static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

//    const uint RID_INPUT = 0x10000003;
//    const uint RIDI_DEVICENAME = 0x20000007;

//    public void Start()
//    {
//        var hwnd = Process.GetCurrentProcess().MainWindowHandle;

//        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
//        rid[0].usUsagePage = 0x01;
//        rid[0].usUsage = 0x06; // Keyboard
//        rid[0].dwFlags = RIDEV_INPUTSINK;
//        rid[0].hwndTarget = hwnd;

//        if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
//        {
//            Console.WriteLine("Failed to register raw input devices.");
//            return;
//        }

//        ComponentDispatcher.ThreadPreprocessMessage += (ref MSG msg, ref bool handled) =>
//        {
//            const int WM_INPUT = 0x00FF;
//            if (msg.message == WM_INPUT)
//            {
//                uint dwSize = 0;
//                GetRawInputData(msg.lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
//                IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);

//                try
//                {
//                    if (GetRawInputData(msg.lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
//                    {
//                        RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

//                        if (raw.header.dwType == RIM_TYPEKEYBOARD)
//                        {
//                            string deviceName = GetDeviceName(raw.header.hDevice);
//                            Console.WriteLine($"Key: {raw.keyboard.VKey} | Device: {deviceName}");
//                        }
//                    }
//                }
//                finally
//                {
//                    Marshal.FreeHGlobal(buffer);
//                }
//            }
//        };
//    }

//    private string GetDeviceName(IntPtr hDevice)
//    {
//        uint size = 0;
//        GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref size);
//        if (size <= 0) return "";

//        IntPtr pData = Marshal.AllocHGlobal((int)size);
//        GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, pData, ref size);
//        string name = Marshal.PtrToStringAnsi(pData);
//        Marshal.FreeHGlobal(pData);

//        return name;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    struct RAWINPUTDEVICE
//    {
//        public ushort usUsagePage;
//        public ushort usUsage;
//        public int dwFlags;
//        public IntPtr hwndTarget;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    struct RAWINPUTHEADER
//    {
//        public int dwType;
//        public int dwSize;
//        public IntPtr hDevice;
//        public IntPtr wParam;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    struct RAWKEYBOARD
//    {
//        public ushort MakeCode;
//        public ushort Flags;
//        public ushort Reserved;
//        public ushort VKey;
//        public uint Message;
//        public uint ExtraInformation;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    struct RAWINPUT
//    {
//        public RAWINPUTHEADER header;
//        public RAWKEYBOARD keyboard;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public struct MSG
//    {
//        public IntPtr hwnd;
//        public uint message;
//        public IntPtr wParam;
//        public IntPtr lParam;
//        public uint time;
//        public System.Drawing.Point pt;
//    }
//}
