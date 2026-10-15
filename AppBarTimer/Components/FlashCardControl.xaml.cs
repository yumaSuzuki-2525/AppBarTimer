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
        // 1. UIの描画更新（画面占有・赤線ハイライト版）
        // ==========================================
        private void UpdateUI()
        {
            if (FlashCardManager.Cards.Count == 0)
            {
                RootContainer.Visibility = Visibility.Hidden;
                return;
            }

            RootContainer.Visibility = Visibility.Visible;

            var currentCard = FlashCardManager.Cards[currentIndex];
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

            // --- 煩わしさを生む「┘」の無限増殖描画処理 ---

            GeometryGroup normalLines = new GeometryGroup();
            GeometryGroup redLine = new GeometryGroup();

            double cardW = 400; // メインカードの幅
            double cardH = 260; // メインカードの高さ

            // ★1枚追加されるごとに、右下に何ピクセルずらすか
            // （2.0px にすると、1000枚で画面が2000pxも占有されて最悪なUIになります＝最高）
            double offsetStep = 2.0;

            for (int i = 1; i <= FlashCardManager.Cards.Count; i++)
            {
                double offset = i * offsetStep;

                // 右側の縦線 ｜
                var rightLine = new LineGeometry(new Point(cardW + offset, offset), new Point(cardW + offset, cardH + offset));
                // 下側の横線 ＿
                var bottomLine = new LineGeometry(new Point(offset, cardH + offset), new Point(cardW + offset, cardH + offset));

                // 現在のページの線だけ赤色グループに追加
                if (i - 1 == currentIndex)
                {
                    redLine.Children.Add(rightLine);
                    redLine.Children.Add(bottomLine);
                }
                else
                {
                    normalLines.Children.Add(rightLine);
                    normalLines.Children.Add(bottomLine);
                }
            }

            // 計算した1000本分の線を一気に画面に適用（超高速）
            StackEdges.Data = normalLines;
            CurrentRedEdge.Data = redLine;

            // ★カードの増加に合わせて、土台のサイズ自体を物理的に拡張する
            RootContainer.Width = cardW + (FlashCardManager.Cards.Count * offsetStep);
            RootContainer.Height = cardH + (FlashCardManager.Cards.Count * offsetStep);
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
                EnglishText.Text = "カードがありません。";
                StatusText.Text = "Empty";
                RootContainer.Visibility = Visibility.Hidden;
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
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            string query = Uri.EscapeDataString(FlashCardManager.Cards[currentIndex].EnglishSentence);
            string url = $"https://translate.google.co.jp/?hl=ja&sl=en&tl=ja&text={query}";

            if (transWindow == null || !transWindow.IsLoaded)
            {
                transWindow = new TranslationWindow();
                transWindow.Show();
            }

            transWindow.NavigateTo(url);
            transWindow.Activate();
        }

        private void SearchImage_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            string query = Uri.EscapeDataString(FlashCardManager.Cards[currentIndex].EnglishSentence);
            Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?tbm=isch&q={query}", UseShellExecute = true });
        }

        private void LogicalDelete_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (FlashCardManager.Cards.Count == 0) return;

            FlashCardManager.Cards[currentIndex].IsDeleted = !FlashCardManager.Cards[currentIndex].IsDeleted;
            FlashCardManager.Save();
            UpdateUI();
        }

       
    }
}