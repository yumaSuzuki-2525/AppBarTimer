using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AppBarTimer.Utils
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

            // 領域の計算（Left と Top 両方に対応）
            if (edge == 1) // Topの場合
            {
                data.rc.left = 0;
                data.rc.top = 0;
                data.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                data.rc.bottom = (int)window.Height;
            }
            else if (edge == 0) // Leftの場合
            {
                data.rc.left = 0;
                data.rc.top = 0;
                data.rc.right = (int)window.Width;
                data.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
            }

            SHAppBarMessage(0, ref data); // 新規登録
            SHAppBarMessage(3, ref data); // 領域の設定

            // ウィンドウの位置とサイズを実際に適用
            window.Left = data.rc.left;
            window.Top = data.rc.top;
            window.Width = data.rc.right - data.rc.left;
            window.Height = data.rc.bottom - data.rc.top; // 左寄せ時は高さが画面一杯になるため追記
        }
    }
}