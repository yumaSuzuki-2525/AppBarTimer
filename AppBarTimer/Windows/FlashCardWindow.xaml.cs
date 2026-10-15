using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace AppBarTimer.Windows
{
    public partial class FlashCardWindow : Window
    {
        // マウスが押し込まれた最初の位置を記憶する変数
        private Point startPoint;
        private bool isMouseDown = false;

        public FlashCardWindow()
        {
            InitializeComponent();
        }

        // ① マウスが押し込まれた瞬間（位置を記憶するだけ）
        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);
            isMouseDown = true;
        }

        // ② マウスが動いた時の判定
        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);

                // マウスが「ほんの少し（数ピクセル）」以上動いたかを計算
                if (Math.Abs(currentPoint.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // 動いた場合は「クリック」ではなく「移動」とみなしてDragMoveを開始
                    isMouseDown = false;
                    this.DragMove();
                }
            }
        }

        // ③ マウスを離した瞬間（状態をリセット）
        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
        }

        // 右クリックして離した時に閉じる
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrlキー と Vキー が同時に押されたか判定
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // クリップボードに文字（テキスト）がコピーされているか確認
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();

                    // 先ほど作ったメソッドにテキストを渡して登録！
                    MainCardControl.AddNewCard(text);

                    e.Handled = true; // 処理済みとしてマーク
                }
            }
            // ついでに、Escキーを押したらウィンドウを閉じる機能も入れておくと便利です
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

    }
}