using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TaskBarTimer
{
    public static class AppBarFunctions
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [DllImport("shell32.dll")]
        static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        public static void SetAppBar(Window window, int edge)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            data.hWnd = helper.Handle;
            data.uEdge = edge; // 0=Left, 1=Top, 2=Right, 3=Bottom

            // ウィンドウサイズを確保する領域として設定
            if (edge == 1) // Topの場合
            {
                data.rc.left = 0;
                data.rc.top = 0;
                data.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                data.rc.bottom = (int)window.Height;
            }

            // 新規登録 (ABM_NEW = 0)
            SHAppBarMessage(0, ref data);

            // 領域の設定 (ABM_SETPOS = 3)
            SHAppBarMessage(3, ref data);

            // ウィンドウの位置を実際に移動
            window.Left = data.rc.left;
            window.Top = data.rc.top;
            window.Width = data.rc.right - data.rc.left;
        }
    }
}