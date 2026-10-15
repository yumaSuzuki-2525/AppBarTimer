using System.Windows;
using System.Windows.Input;

namespace AppBarTimer.Windows
{
    public partial class TimerFinishedWindow : Window
    {
        public TimerFinishedWindow()
        {
            InitializeComponent();
        }

        // 画面のどこかをクリックしたら閉じる
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}