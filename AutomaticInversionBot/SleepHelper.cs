using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutomaticInversionBot
{
    class SleepHelper
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;        // Specifies the size, in bytes, of the structure. 
                                        // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
            public Int32 flags;         // Specifies the cursor state. This parameter can be one of the following values:
                                        // 0                 The cursor is hidden.
                                        // 0x00000001        The cursor is showing.
            public IntPtr hCursor;      // Handle to the cursor. 
            public POINT ptScreenPos;   // A POINT structure that receives the screen coordinates of the cursor. 
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder sb = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, sb, nChars) > 0)
            {
                return sb.ToString();
            }
            return null;
        }

        public static Color GetColorAt(int x, int y)
        {
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, x, y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        //Waits, until a child window (dialog or sort of) appears. Then another action can be performed. Child handle is returned
        public static IntPtr WaitForWindow(int interval, string windowTitle, int loops)
        {
            while (!GetActiveWindowTitle().Contains(windowTitle) && loops-- > 0)
            {
                Thread.Sleep(interval);
            }
            return GetForegroundWindow();
        }
        public static IntPtr WaitForWindow(int interval, string[] windowTitles, int loops)
        {
            bool containsTitle = false;
            while (!containsTitle && loops-- > 0)
            {
                foreach(string s in windowTitles)
                {
                    if(GetActiveWindowTitle().Contains(s))
                    {
                        containsTitle = false;
                    }
                }
                Thread.Sleep(interval);
            }
            return GetForegroundWindow();
        }

        //Waits, until a certain pixel has a certain color. Then another action can be performed
        public static void WaitForColorOnPixel(int interval, int x, int y, Color color)
        {
            while(!color.Equals(GetColorAt(x, y)))
            {
                Thread.Sleep(interval);
            }
        }

        //Wais, until a certain process is started and returns it
        public static Process WaitForProcess(int inverval, string name)
        {
            Process[] p = Process.GetProcessesByName(name);
            while(p.Count() == 0)
            {
                Thread.Sleep(inverval);
                p = Process.GetProcessesByName(name);
            }
            return p.First();
        }

        public static void WaitForCursor(int interval, Cursor c)
        {
            var h = c.Handle;
            CURSORINFO pci;
            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            GetCursorInfo(out pci);

            while (pci.hCursor != h) {
                Thread.Sleep(interval);
                pci = new CURSORINFO();
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                GetCursorInfo(out pci);
            }
        }

        public static bool CheckForWindow(string[] windowTitles)
        {
            string active = GetActiveWindowTitle();
            foreach (string s in windowTitles)
            {
                if (active.Contains(s))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
