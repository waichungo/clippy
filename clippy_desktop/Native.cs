using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Clippy
{
    public class Native
    {
        public static int SW_SHOW = 5;
        public static int SW_NORMAL = 1;
        public static uint MB_OK = 0;
        public static uint YesNo = 4;
        public static uint MB_TOPMOST = 0x00040000;
        public static uint MB_YESNO = 0x00000004;
        public static int IDYES = 6;
        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOPMOST = 0x0008;
        public static IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);


        public static int MessageBox(String text, String caption = "", uint type = 0)
        {
            return MessageBox(IntPtr.Zero, text, caption, type);
        }
    }
}
