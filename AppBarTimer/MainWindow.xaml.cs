
using System;
using System.Windows;
using System.Windows.Threading; // タイマー処理に必要
using System.Runtime.InteropServices; 
using System.Windows.Interop;         

namespace AppBarTimer
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;

            // 時計を動かすタイマーの起動（0.5秒ごとに処理を実行）
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // 時刻に合わせて針の角度（Angle）を計算する処理
        private void Timer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            // 秒針 (1秒で6度進む)
            SecondHandRotate.Angle = now.Second * 6;
            // 分針 (1分で6度進む ＋ 秒数に応じて少しずつ進める)
            MinuteHandRotate.Angle = (now.Minute * 6) + (now.Second * 0.1);
            // 時針 (1時間で30度進む ＋ 分数に応じて少しずつ進める)
            HourHandRotate.Angle = ((now.Hour % 12) * 30) + (now.Minute * 0.5);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // ① AppBarとして登録する処理（左寄せ）
            AppBarFunctions.SetAppBar(this, 0);

            // ② Alt+Tabから完全に姿を消す処理を追加
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
        }
        // 小さい時計をクリックしたときの処理
        private void SmallClock_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var largeClock = new Windows.LargeClockWindow();
            largeClock.Show(); // ShowDialogではなくShowにすることで、AppBarをロックせず独立して表示し続けられます
        }
        // 単語カードアイコンをクリックしたときの処理
        private void FlashCardIcon_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var flashCardWindow = new Windows.FlashCardWindow();
            flashCardWindow.Show();
        }
    }
}