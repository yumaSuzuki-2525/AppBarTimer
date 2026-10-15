using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AppBarTimer
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

        // Zオーダー（前面・背面）を操作するためのAPIを追加
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Win32 API の定数群
        const int ABM_NEW = 0;
        const int ABM_SETPOS = 3;

        const int ABN_FULLSCREENAPP = 2; // フルスクリーンアプリ起動の通知コード

        const int WM_APP = 0x8000;
        const int APPBAR_CALLBACK = WM_APP + 100; // 受け取るための専用メッセージID

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 最前面
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);   // 最背面

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;

        public static void SetAppBar(Window window, int edge)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;

            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            data.hWnd = hwnd;
            data.uEdge = edge;

            // OSからの通知を受け取る設定
            data.uCallbackMessage = APPBAR_CALLBACK;

            if (edge == 1) // Top
            {
                data.rc.left = 0;
                data.rc.top = 0;
                data.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                data.rc.bottom = (int)window.Height;
            }
            else if (edge == 0) // Left
            {
                data.rc.left = 0;
                data.rc.top = 0;
                data.rc.right = (int)window.Width;
                data.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
            }

            SHAppBarMessage(ABM_NEW, ref data);
            SHAppBarMessage(ABM_SETPOS, ref data);

            window.Left = data.rc.left;
            window.Top = data.rc.top;
            window.Width = data.rc.right - data.rc.left;
            window.Height = data.rc.bottom - data.rc.top;

            // ウィンドウが受け取るメッセージ（通知）を監視するフックを追加
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
        }

        // OSからの通知を受け取って処理するメソッド
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == APPBAR_CALLBACK)
            {
                int notification = wParam.ToInt32();
                if (notification == ABN_FULLSCREENAPP)
                {
                    // lParam が 0 以外なら「フルスクリーンアプリがアクティブになった」
                    bool isFullScreen = (lParam.ToInt32() != 0);

                    if (isFullScreen)
                    {
                        // 全画面アプリ起動時：AppBarを一番後ろ（底）に隠す
                        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    }
                    else
                    {
                        // 全画面アプリ終了時：AppBarを再び最前面に戻す
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    }
                }
            }
            return IntPtr.Zero;
        }
    }
}