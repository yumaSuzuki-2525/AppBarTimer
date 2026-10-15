using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppBarTimer.Windows
{
    public partial class LargeClockWindow : Window
    {
        private DispatcherTimer timer;

        public LargeClockWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            SecondHandRotate.Angle = now.Second * 6;
            MinuteHandRotate.Angle = (now.Minute * 6) + (now.Second * 0.1);
            HourHandRotate.Angle = ((now.Hour % 12) * 30) + (now.Minute * 0.5);
        }

        // ★ウィジェットのように、マウスの左ドラッグで自由に画面上を動かせるようにする
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // 右クリックしたらこの時計ウィジェットを閉じる
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        // 既存のメソッド（ドラッグ移動、右クリックで閉じるなど）の下にこれを追加
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // ホイールを上に回すと大きく、下に回すと小さくなる（1回のカチッで20ピクセルずつ）
            double step = e.Delta > 0 ? 20 : -20;
            double newSize = this.Width + step;

            // 大きくなりすぎ・小さくなりすぎを防ぐ（最小100px、最大800pxに制限）
            if (newSize >= 100 && newSize <= 800)
            {
                this.Width = newSize;
                this.Height = newSize;
            }
        }
    }
}