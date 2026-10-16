using System;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Catcon {
    internal static class Native {
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y;public POINT(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE {public int Width,Height;public SIZE(int w,int h){Width=w;Height=h;}}
        [StructLayout(LayoutKind.Sequential,Pack=1)] public struct BLEND {public byte Operation,Flags,Alpha,Format;}
        [StructLayout(LayoutKind.Sequential)] public struct RECT {public int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)] public struct LASTINPUTINFO {public uint size,time;}
        [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern int GetWindowText(IntPtr window,StringBuilder text,int max);
        [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr window,out RECT rect);
        [DllImport("user32.dll")]public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")]public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
        [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
        [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dest,ref POINT point,ref SIZE size,IntPtr source,ref POINT origin,uint color,ref BLEND blend,uint flags);
        [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
        [DllImport("user32.dll",EntryPoint="GetWindowLong")]public static extern int GetWindowLong(IntPtr hwnd,int index);
        [DllImport("user32.dll")]public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll",EntryPoint="SetProcessDpiAwarenessContext")]public static extern bool SetDpiContext(IntPtr context);
        [DllImport("user32.dll")]public static extern uint GetGuiResources(IntPtr process,int flags);
        [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
        public static Icon CreateIcon(Bitmap image) {IntPtr h=image.GetHicon();try{return (Icon)Icon.FromHandle(h).Clone();}finally{DestroyIcon(h);}}
        public static void SetLayer(IntPtr hwnd,Bitmap bitmap,Point location) {
            IntPtr screen=GetDC(IntPtr.Zero),memory=IntPtr.Zero,hbitmap=IntPtr.Zero,previous=IntPtr.Zero;
            try {
                memory=CreateCompatibleDC(screen);hbitmap=bitmap.GetHbitmap(Color.FromArgb(0));previous=SelectObject(memory,hbitmap);
                var p=new POINT(location.X,location.Y);var origin=new POINT(0,0);var size=new SIZE(bitmap.Width,bitmap.Height);var blend=new BLEND{Alpha=255,Format=1};
                if(!UpdateLayeredWindow(hwnd,screen,ref p,ref size,memory,ref origin,0,ref blend,2))throw new Win32Exception(Marshal.GetLastWin32Error());
            } finally {if(previous!=IntPtr.Zero)SelectObject(memory,previous);if(hbitmap!=IntPtr.Zero)DeleteObject(hbitmap);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
        }
        public static double IdleSeconds() {var l=new LASTINPUTINFO{size=(uint)Marshal.SizeOf(typeof(LASTINPUTINFO))};return GetLastInputInfo(ref l)?unchecked((uint)Environment.TickCount-l.time)/1000.0:0;}
    }
}
