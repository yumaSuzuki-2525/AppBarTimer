using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading; // タイマー用
using System.Windows.Media;     // 色の変更用
using System.Windows.Shapes;    // Rectangle(図形)の操作用
using AppBarTimer.Windows;

namespace AppBarTimer.Components
{
    public partial class TimerControl : UserControl
    {
        private DispatcherTimer countdownTimer;
        private int totalSeconds = 0;
        private int remainingSeconds = 0;

        public TimerControl()
        {
            InitializeComponent();

            // 1秒に1回動くタイマーを準備
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void TimerArea_Click(object sender, MouseButtonEventArgs e)
        {
            // 現在タイマーが動いているかを記憶し、一旦ストップする
            bool wasRunning = countdownTimer.IsEnabled;
            countdownTimer.Stop();

            // 現在の残り時間を「分」と「秒」に変換
            int currentM = remainingSeconds / 60;
            int currentS = remainingSeconds % 60;

            // ダイアログに現在の時間を渡して開く
            var dialog = new TimerSettingWindow(currentM, currentS);

            if (dialog.ShowDialog() == true)
            {
                // 「設定する」が押された場合：新しい時間で上書き
                totalSeconds = (dialog.Minutes * 60) + dialog.Seconds;
                remainingSeconds = totalSeconds;

                UpdateUI(); // 画面を更新

                if (totalSeconds > 0)
                {
                    countdownTimer.Start();
                }
            }
            else
            {
                // 「×ボタン」等でキャンセルされた場合：元々動いていたなら再開する
                if (wasRunning && remainingSeconds > 0)
                {
                    countdownTimer.Start();
                }
            }
        }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            remainingSeconds--;
            UpdateUI();

            // 0秒になった時の処理
            if (remainingSeconds <= 0)
            {
                countdownTimer.Stop();

                // 終了通知ダイアログを表示する
                var finishDialog = new TimerFinishedWindow();
                finishDialog.Show(); // ShowDialogではなくShowを使うと、AppBarを固まらせずに表示できます
            }
        }

        // 数字とゲージを更新する処理
        private void UpdateUI()
        {
            // 残り秒数を「〇分〇秒」に計算し直して、画面のテキストを書き換え
            int m = remainingSeconds / 60;
            int s = remainingSeconds % 60;
            MinText.Text = m.ToString("D2"); // "D2"で必ず2桁(00)になるようにする
            SecText.Text = s.ToString("D2");

            // 10個のゲージを割合に応じて黒くする処理
            if (GaugePanel.Children.Count > 0 && totalSeconds > 0)
            {
                // 残り時間の割合（0.0 ～ 1.0）
                double ratio = (double)remainingSeconds / totalSeconds;

                // 黒くすべきブロックの数（10個中何個か）を計算
                int activeBlocks = (int)Math.Ceiling(ratio * GaugePanel.Children.Count);

                for (int i = 0; i < GaugePanel.Children.Count; i++)
                {
                    var rect = (Rectangle)GaugePanel.Children[i];

                    // 下から積み上げるように色を付けるため、上から数えて「黒にしない数」を超えたら黒にする
                    if (i >= GaugePanel.Children.Count - activeBlocks)
                    {
                        rect.Fill = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        // 消化した時間はグレー（背景色になじむ色）にする
                        rect.Fill = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    }
                }
            }
        }
    }
}