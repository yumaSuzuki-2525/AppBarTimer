using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppBarTimer.Windows
{
    public partial class TimerSettingWindow : Window
    {
        public int Minutes { get; private set; }
        public int Seconds { get; private set; }

        // 初期値（現在の残り時間）を受け取るように書き換え
        public TimerSettingWindow(int initialMin, int initialSec)
        {
            InitializeComponent();
            // 渡された時間をテキストボックスにセットする
            MinBox.Text = initialMin.ToString();
            SecBox.Text = initialSec.ToString();
        }

        // キーボード入力時に「数字以外」を弾く処理
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // カーソルが外れた時（空っぽなら "0" にする処理）
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "0";
            }
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MinBox.Text, out int m) && int.TryParse(SecBox.Text, out int s))
            {
                // 上限の制限（分は999、秒は60まで）
                if (m > 999) m = 999;
                if (s > 60) s = 60;

                Minutes = m;
                Seconds = s;
                this.DialogResult = true;
            }
        }
    }
}