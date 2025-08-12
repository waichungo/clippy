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
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);



        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CopyImage(IntPtr hImage, uint uType, int cxDesired, int cyDesired, uint fuFlags); 

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        private const uint GMEM_MOVEABLE = 0x0002; // Global memory flags
        // Clipboard formats
        private const uint CF_UNICODETEXT = 13U;
        private const uint CF_BITMAP = 2U;
        const uint IMAGE_BITMAP = 0;
        const uint LR_COPYRETURNORG = 0x0004;

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
                        Device = systemId
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
        public static void SetClipboardText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                throw new Exception("Could not open clipboard.");
            }

            try
            {
                EmptyClipboard();

                // Convert string to a byte array and allocate global memory
                byte[] textBytes = Encoding.Unicode.GetBytes(text + "\0"); // Null-terminate for clipboard
                IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)textBytes.Length);
                if (hGlobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException("Failed to allocate global memory.");
                }

                IntPtr lpGlobal = GlobalLock(hGlobal);
                if (lpGlobal == IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                    throw new Exception("Failed to lock global memory.");
                }

                try
                {
                    Marshal.Copy(textBytes, 0, lpGlobal, textBytes.Length);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                // Set the clipboard data
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                {
                    GlobalFree(hGlobal); // Free memory if SetClipboardData fails
                    throw new Exception("Failed to set clipboard data.");
                }

                // The system now owns hGlobal, do not free it.
            }
            finally
            {
                CloseClipboard();
            }
        }
        public static void SetImageToClipboard(System.Drawing.Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));

            // Get HBITMAP handle from Bitmap
            IntPtr hBitmap = bmp.GetHbitmap();

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to open clipboard");

                if (!EmptyClipboard())
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to empty clipboard");

                // Copy the HBITMAP so Windows owns the handle
                IntPtr hBitmapCopy = CopyImage(hBitmap, IMAGE_BITMAP, 0, 0, LR_COPYRETURNORG);
                if (hBitmapCopy == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to copy HBITMAP");

                if (SetClipboardData(CF_BITMAP, hBitmapCopy) == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to set clipboard data");

                // After SetClipboardData, Windows owns hBitmapCopy, so we don't delete it
                CloseClipboard();
            }
            finally
            {
                // We must delete the original handle to prevent leaks
                DeleteObject(hBitmap);
            }
        }

    }
}
