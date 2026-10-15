using System;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AppBarTimer.Models;
using AppBarTimer.Utils;
using AppBarTimer.Windows;

namespace AppBarTimer.Components
{
    public partial class FlashCardControl : UserControl
    {
        private SpeechSynthesizer synthesizer;
        private int currentIndex = 0;
        private int playCount = 0;
        private readonly int maxPlays = 3;
        private bool isAutoPlayMode = false;
        private Random rnd = new Random();
        private static TranslationWindow? transWindow = null;

        public FlashCardControl()
        {
            InitializeComponent();

            synthesizer = new SpeechSynthesizer();
            synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 1, new System.Globalization.CultureInfo("en-US"));
            synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

            FlashCardManager.Load();

            // 起動時は常に最後のカード（最新）を表示
            if (FlashCardManager.Cards.Count > 0)
            {
                currentIndex = FlashCardManager.Cards.Count - 1;
            }
            UpdateUI();
        }

        // ==========================================
        // 1. UIの描画更新（完全不透明・角丸なし版）
        // ==========================================
        private void UpdateUI()
        {
            if (FlashCardManager.Cards.Count == 0)
            {
                // バグ修正：RootContainerごと消すのではなく、後ろの束だけを隠す
                BackgroundStack.Visibility = Visibility.Hidden;
                CurrentRedStack.Visibility = Visibility.Hidden;
                ForegroundStack.Visibility = Visibility.Hidden;

                DateText.Text = ""; // 0枚の時は日付も空にする
                return;
            }

            // カードがある場合は束を表示する
            BackgroundStack.Visibility = Visibility.Visible;
            CurrentRedStack.Visibility = Visibility.Visible;
            ForegroundStack.Visibility = Visibility.Visible;

            var currentCard = FlashCardManager.Cards[currentIndex];

            // 追加：日付を「yyyy/MM/dd HH:mm」の形式で左上に表示
            DateText.Text = currentCard.CreatedAt.ToString("yyyy/MM/dd HH:mm");

            if (currentCard.IsDeleted)
            {
                CardBackground.Background = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                EnglishText.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                EnglishText.Text = "【削除済み】\n" + currentCard.EnglishSentence;
            }
            else
            {
                CardBackground.Background = new SolidColorBrush(Color.FromRgb(249, 246, 240));
                EnglishText.Foreground = new SolidColorBrush(Color.FromRgb(58, 53, 50));
                EnglishText.Text = currentCard.EnglishSentence;
            }

            StatusText.Text = $"🔊 Center to Read  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";

            // --- 煩わしさを生む「透過しない束」の無限増殖描画処理 ---

            // 塗りつぶしモードを Nonzero にすることで、隙間なく完全に透過を防ぎます
            GeometryGroup bgGroup = new GeometryGroup { FillRule = FillRule.Nonzero };
            GeometryGroup redGroup = new GeometryGroup { FillRule = FillRule.Nonzero };
            GeometryGroup fgGroup = new GeometryGroup { FillRule = FillRule.Nonzero };

            double cardW = 420; // カード幅
            double cardH = 500; // カード高さ
            double offsetStep = 2.0; // 1枚あたり右下へずれる量

            int totalCards = FlashCardManager.Cards.Count;
            int targetI = currentIndex + 1;

            // 奥のカードから手前のカードに向かって四角形を重ねて描画する
            for (int i = totalCards; i >= 1; i--)
            {
                double offset = i * offsetStep;
                // 線ではなく「背景色が塗られた完全な四角形」をずらしながら配置する
                var rect = new RectangleGeometry(new Rect(offset, offset, cardW, cardH));

                if (i > targetI) bgGroup.Children.Add(rect);
                else if (i == targetI) redGroup.Children.Add(rect);
                else fgGroup.Children.Add(rect);
            }

            BackgroundStack.Data = bgGroup;
            CurrentRedStack.Data = redGroup;
            ForegroundStack.Data = fgGroup;

            // カードの増加に合わせて、土台のサイズを物理的に拡張する
            RootContainer.Width = cardW + (totalCards * offsetStep);
            RootContainer.Height = cardH + (totalCards * offsetStep);
        }

        // ==========================================
        // イベント名変更（UserControl_xxx から CardBackground_xxx へ）
        // ==========================================
        private void CardBackground_MouseMove(object sender, MouseEventArgs e)
        {
            // 判定基準を一番手前のメインカード（CardBackground）に限定
            double clickX = e.GetPosition(CardBackground).X;
            double width = CardBackground.ActualWidth;

            if (clickX < width * 0.25) HoverActionText.Text = "◀ 前へ";
            else if (clickX > width * 0.75) HoverActionText.Text = "次へ ▶";
            else HoverActionText.Text = isAutoPlayMode ? "⏹ 停止" : "▶ 自動再生";
        }

        private void CardBackground_MouseLeave(object sender, MouseEventArgs e)
        {
            HoverActionText.Text = "";
        }

        private void CardBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (FlashCardManager.Cards.Count == 0) return;

            // 判定基準を一番手前のメインカードに限定
            double clickX = e.GetPosition(CardBackground).X;
            double width = CardBackground.ActualWidth;

            if (clickX < width * 0.25)
            {
                isAutoPlayMode = false;
                synthesizer.SpeakAsyncCancelAll();
                if (currentIndex > 0) { currentIndex--; UpdateUI(); }
            }
            else if (clickX > width * 0.75)
            {
                isAutoPlayMode = false;
                synthesizer.SpeakAsyncCancelAll();
                if (currentIndex < FlashCardManager.Cards.Count - 1) { currentIndex++; UpdateUI(); }
            }
            else
            {
                if (isAutoPlayMode || synthesizer.State == SynthesizerState.Speaking)
                {
                    isAutoPlayMode = false;
                    synthesizer.SpeakAsyncCancelAll();
                    StatusText.Text = $"⏹ Stopped  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                }
                else
                {
                    playCount = 0;
                    isAutoPlayMode = true;
                    StatusText.Text = $"▶ Auto Playing... (1/{maxPlays})  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                    SpeakCurrentText();
                }
            }
        }

        // 💥「完全削除」（物理削除）
        private void PhysicalDelete_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (FlashCardManager.Cards.Count == 0) return;

            FlashCardManager.Cards.RemoveAt(currentIndex);
            FlashCardManager.Save();

            if (FlashCardManager.Cards.Count == 0)
            {
                EnglishText.Text = "カードがありません。\nCtrl + V で追加してください。";
                StatusText.Text = "Empty";
                DateText.Text = ""; // 日付を消す

                // ★バグ修正：RootContainerではなく束だけを隠す
                BackgroundStack.Visibility = Visibility.Hidden;
                CurrentRedStack.Visibility = Visibility.Hidden;
                ForegroundStack.Visibility = Visibility.Hidden;

                return;
            }

            if (currentIndex >= FlashCardManager.Cards.Count) currentIndex = FlashCardManager.Cards.Count - 1;
            UpdateUI();
        }
        // ==========================================
        // 2. ホイールスクロールでの移動
        // ==========================================
        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (FlashCardManager.Cards.Count == 0) return;

            // 再生中なら止める
            isAutoPlayMode = false;
            synthesizer.SpeakAsyncCancelAll();

            if (e.Delta < 0) // 下にスクロール（手前に引く）＝ 次のカードへ
            {
                if (currentIndex < FlashCardManager.Cards.Count - 1) { currentIndex++; UpdateUI(); }
            }
            else // 上にスクロール（奥に押す）＝ 前のカードへ
            {
                if (currentIndex > 0) { currentIndex--; UpdateUI(); }
            }
        }

        // ==========================================
        // 3. ホバー時のナビゲーション表示
        // ==========================================
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            double clickX = e.GetPosition(this).X;
            double width = this.ActualWidth;

            if (clickX < width * 0.25) HoverActionText.Text = "◀ 前へ";
            else if (clickX > width * 0.75) HoverActionText.Text = "次へ ▶";
            else HoverActionText.Text = isAutoPlayMode ? "⏹ 停止" : "▶ 自動再生";
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            HoverActionText.Text = "";
        }

        // ==========================================
        // 4. クリック操作
        // ==========================================
        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if (FlashCardManager.Cards.Count == 0) return;

            double clickX = e.GetPosition(this).X;
            double width = this.ActualWidth;

            if (clickX < width * 0.25)
            {
                isAutoPlayMode = false;
                synthesizer.SpeakAsyncCancelAll();
                if (currentIndex > 0) { currentIndex--; UpdateUI(); }
            }
            else if (clickX > width * 0.75)
            {
                isAutoPlayMode = false;
                synthesizer.SpeakAsyncCancelAll();
                if (currentIndex < FlashCardManager.Cards.Count - 1) { currentIndex++; UpdateUI(); }
            }
            else
            {
                if (isAutoPlayMode || synthesizer.State == SynthesizerState.Speaking)
                {
                    isAutoPlayMode = false;
                    synthesizer.SpeakAsyncCancelAll();
                    StatusText.Text = $"⏹ Stopped  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                }
                else
                {
                    playCount = 0;
                    isAutoPlayMode = true;
                    StatusText.Text = $"▶ Auto Playing... (1/{maxPlays})  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                    SpeakCurrentText();
                }
            }
        }

        // ==========================================
        // 5. 音声再生
        // ==========================================
        private void SpeakCurrentText()
        {
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.Rate = rnd.Next(-2, 3);
            var textToRead = FlashCardManager.Cards[currentIndex].EnglishSentence;
            synthesizer.SpeakAsync(textToRead);
        }

        private async void Synthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (!isAutoPlayMode) return;
            playCount++;

            if (playCount < maxPlays)
            {
                StatusText.Text = $"▶ Auto Playing... ({playCount + 1}/{maxPlays})  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                await Task.Delay(1000);
                SpeakCurrentText();
            }
            else
            {
                if (currentIndex < FlashCardManager.Cards.Count - 1)
                {
                    StatusText.Text = $"⏭ Moving to Next...  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                    await Task.Delay(1500);
                    currentIndex++;
                    UpdateUI();

                    playCount = 0;
                    SpeakCurrentText();
                }
                else
                {
                    StatusText.Text = $"✅ All Finished!  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                    isAutoPlayMode = false;
                }
            }
        }

        // ==========================================
        // 6. 新規登録（一番後ろに追加し、そこへ移動）
        // ==========================================
        public void AddNewCard(string newSentence)
        {
            if (string.IsNullOrWhiteSpace(newSentence)) return;

            // 一番後ろ（リストの末尾）に追加
            FlashCardManager.Cards.Add(new FlashCardItem { EnglishSentence = newSentence.Trim() });
            FlashCardManager.Save();

            // 追加した最新のカード（一番後ろ）に移動
            currentIndex = FlashCardManager.Cards.Count - 1;
            UpdateUI();

            StatusText.Text = $"✨ Added!  [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
        }

        // ==========================================
        // 7. 各種ボタン処理
        // ==========================================
        private void SearchTranslate_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            // カードが0件の場合は何もしない
            if (FlashCardManager.Cards.Count == 0) return;
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            string query = Uri.EscapeDataString(FlashCardManager.Cards[currentIndex].EnglishSentence);
            string url = $"https://translate.google.co.jp/?hl=ja&sl=en&tl=ja&text={query}";

            if (transWindow == null || !transWindow.IsLoaded)
            {
                transWindow = new TranslationWindow();
                // 画面の左側に配置する
                var workArea = SystemParameters.WorkArea;
                transWindow.Left = workArea.Left + 20; // 左から20px
                transWindow.Top = workArea.Top + 20;   // 上から20px
                transWindow.Show();
            }

            transWindow.NavigateTo(url);


            // transWindow.Activate(); を削除し、単語カードを前面に戻す
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Activate();
            }
        }

        //private void SearchImage_Click(object sender, MouseButtonEventArgs e)
        //{
        //    e.Handled = true;
        //    if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

        //    string query = Uri.EscapeDataString(FlashCardManager.Cards[currentIndex].EnglishSentence);
        //    Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?tbm=isch&q={query}", UseShellExecute = true });
        //}
        // 🖼「画像」検索ボタン（専用ウィンドウを上書きして開く）
        private void SearchImage_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (FlashCardManager.Cards.Count == 0) return;
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            string query = Uri.EscapeDataString(FlashCardManager.Cards[currentIndex].EnglishSentence);
            // 画像検索のURL
            string url = $"https://www.google.com/search?tbm=isch&q={query}";

            // ウィンドウが無ければ作る、あれば使い回す
            if (transWindow == null || !transWindow.IsLoaded)
            {
                transWindow = new TranslationWindow();
                // ★追加：画面の左側に配置する
                var workArea = SystemParameters.WorkArea;
                transWindow.Left = workArea.Left + 20; // 左から20px
                transWindow.Top = workArea.Top + 20;   // 上から20px
            }

            transWindow.NavigateTo(url);
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Activate();
            }
        }

        private void LogicalDelete_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (FlashCardManager.Cards.Count == 0) return;

            FlashCardManager.Cards[currentIndex].IsDeleted = !FlashCardManager.Cards[currentIndex].IsDeleted;
            FlashCardManager.Save();
            UpdateUI();
        }

        // ==========================================
        // 束の部分をクリックしたときのページジャンプ処理
        // ==========================================
        private void RootContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (FlashCardManager.Cards.Count == 0) return;

            // クリックされた座標（X, Y）を取得
            Point pos = e.GetPosition(RootContainer);

            double cardW = 420; // メインカードの幅
            double cardH = 320; // メインカードの高さ
            double offsetStep = 2.0; // 1枚あたりのズレ幅

            // メインカードの領域外（右側か下側 ＝ 束の部分）をクリックしたか判定
            if (pos.X > cardW || pos.Y > cardH)
            {
                // メインカードから何ピクセルはみ出た場所をクリックしたか計算
                // （右方向と下方向、より大きくはみ出ている方を基準にする）
                double delta = Math.Max(pos.X - cardW, pos.Y - cardH);

                // はみ出たピクセル量 ÷ 1枚のズレ(2.0px) で、何枚目の位置かを逆算！
                int targetIndex = (int)Math.Round(delta / offsetStep);

                // 計算結果が 0 ～ 最後のページ の範囲に収まるように安全対策
                targetIndex = Math.Max(0, Math.Min(targetIndex, FlashCardManager.Cards.Count - 1));

                // 算出したページにジャンプ！
                currentIndex = targetIndex;

                // 自動再生中なら安全のために止める
                isAutoPlayMode = false;
                synthesizer.SpeakAsyncCancelAll();

                UpdateUI();
            }
        }
    }
}