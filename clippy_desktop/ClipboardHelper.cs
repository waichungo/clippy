using System;
using System.Text;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using System.Drawing;
using SkiaSharp;
using System.IO;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace Clippy
{


    public class ClipboardHelper
    {
        #region Win32

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern int GlobalSize(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13U;
        private const uint CF_BITMAP = 2U;

        #endregion

        private static byte[]? GetClipboardImage()
        {
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return null;

                IntPtr handle = GetClipboardData(CF_BITMAP);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }

                var bitmap = System.Drawing.Image.FromHbitmap(handle, IntPtr.Zero);
                using var mem = new MemoryStream();
                bitmap.Save(mem, ImageFormat.Jpeg);
                return mem.GetBuffer();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                CloseClipboard();
            }
            return null;
        }
        public static string GetImageClipsDirectory()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Clippy", "images");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private static string? GetClipboardText()
        {
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return null;
                IntPtr handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }
                IntPtr pointer = IntPtr.Zero;
                try
                {
                    pointer = GlobalLock(handle);
                    if (pointer == IntPtr.Zero)
                        return null;

                    int size = GlobalSize(handle);
                    byte[] buff = new byte[size];

                    Marshal.Copy(pointer, buff, 0, size);

                    return Encoding.Unicode.GetString(buff).TrimEnd('\0');
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                        GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static ClipItem? GetClipItem()
        {
            var systemId = Utils.GetSystemUuid();
            if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                var text = GetClipboardText();
                if (text != null)
                {
                    return new ClipItem
                    {
                        ExType = ClipType.TEXT,
                        Data = text,
                        Device= systemId
                    };
                }
            }
            else if (IsClipboardFormatAvailable(CF_BITMAP))
            {
                var bytes = GetClipboardImage();
                if (bytes != null)
                {
                    var id = Guid.NewGuid().ToString();
                    var path = Path.Combine(GetImageClipsDirectory(), id + ".jpg");
                    File.WriteAllBytes(path, bytes);
                    return new ClipItem
                    {
                        ExType = ClipType.IMAGE,
                        Data = id,
                        Device = systemId
                    };
                }
            }
            return null;


        }
    }
}
