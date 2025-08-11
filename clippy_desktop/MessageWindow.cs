using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Clippy
{
    public sealed class MessageWindow : IDisposable
    {
        // Public event for clipboard changes (optional)
        public event EventHandler? ClipboardUpdated;

        // HWND_MESSAGE constant
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        // The handle to our message-only window
        public IntPtr Hwnd { get; private set; } = IntPtr.Zero;

        // Stored delegate to prevent GC from collecting the WndProc delegate
        private readonly WndProcDelegate _wndProcDelegate;

        // Registered class atom (returned from RegisterClassEx)
        private ushort _atom;

        // Class name (unique)
        private readonly string _className;

        // WM message constants we care about
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public MessageWindow()
        {
            // Give the class a unique name to avoid collisions
            _className = "MsgOnlyWndClass_" + Guid.NewGuid().ToString("N");
            _wndProcDelegate = WndProc;

            RegisterAndCreateWindow();
        }

        private void RegisterAndCreateWindow()
        {
            // Fill WNDCLASSEX
            var wndClass = new WNDCLASSEX()
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = GetModuleHandle(null),
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = _className,
                hIconSm = IntPtr.Zero
            };

            _atom = RegisterClassEx(ref wndClass);
            if (_atom == 0)
                ThrowLastWin32Error("RegisterClassEx failed");

            // Create message-only window by passing HWND_MESSAGE as parent and class name
            Hwnd = CreateWindowEx(
                0,
                _className,
                string.Empty,
                0,
                0, 0, 0, 0,
                HWND_MESSAGE,
                IntPtr.Zero,
                wndClass.hInstance,
                IntPtr.Zero);

            if (Hwnd == IntPtr.Zero)
                ThrowLastWin32Error("CreateWindowEx failed");

            // Optionally register for clipboard notifications. If you want clipboard events:
            if (!AddClipboardFormatListener(Hwnd))
                ThrowLastWin32Error("AddClipboardFormatListener failed");
        }

        // WndProc signature
        private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                ClipboardUpdated?.Invoke(this, EventArgs.Empty);
            }

            // Default handling - call DefWindowProc for default behaviour
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            // Unregister clipboard listener
            if (Hwnd != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(Hwnd);
            }

            // Destroy the window
            if (Hwnd != IntPtr.Zero)
            {
                DestroyWindow(Hwnd);
                Hwnd = IntPtr.Zero;
            }

            // Unregister class if registered
            if (_atom != 0)
            {
                UnregisterClass(_className, GetModuleHandle(null));
                _atom = 0;
            }
        }

        // Helpers
        private static void ThrowLastWin32Error(string message)
        {
            var err = Marshal.GetLastWin32Error();
            throw new Win32Exception(err, message + ": " + err);
        }

        #region PInvoke Declarations

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc; // function pointer
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool UnregisterClass([MarshalAs(UnmanagedType.LPWStr)] string lpClassName, IntPtr hInstance);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Clipboard notification helpers
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        #endregion
    }
}
