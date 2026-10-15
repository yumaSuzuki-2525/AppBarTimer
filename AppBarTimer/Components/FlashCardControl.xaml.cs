using AppBarTimer.Models;
using AppBarTimer.Utils; // FlashCardManagerを使うため
using System;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppBarTimer.Components
{
    public partial class FlashCardControl : UserControl
    {
        private SpeechSynthesizer synthesizer;

        // 現在表示しているカードのインデックス番号
        private int currentIndex = 0;

        // 自動再生用
        private int playCount = 0;
        private readonly int maxPlays = 3;
        private bool isAutoPlayMode = false;

        public FlashCardControl()
        {
            InitializeComponent();

            synthesizer = new SpeechSynthesizer();
            synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 1, new System.Globalization.CultureInfo("en-US"));
            synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

            // ★起動時にJSONからデータを読み込む
            FlashCardManager.Load();

            // 最初のカードを表示する
            UpdateUI();
        }

        // 画面のテキストや枚数表示を更新する
        private void UpdateUI()
        {
            if (FlashCardManager.Cards.Count == 0) return;

            // 現在のカード情報を取得して画面にセット
            var currentCard = FlashCardManager.Cards[currentIndex];
            EnglishText.Text = currentCard.EnglishSentence;

            // 右下の「1 / 42」のような表示を更新
            StatusText.Text = "🔊 Click to Read";

            // XAML側で名前を付けていない場合は、XAMLの右下のTextBlockに x:Name="PageText" を付けてください
            // ここでは暫定的に StatusText に情報をまとめます
            StatusText.Text = $"🔊 Click Center to Read   [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
        }

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if (FlashCardManager.Cards.Count == 0) return;

            // クリックされた横方向の位置（X座標）を取得
            double clickX = e.GetPosition(this).X;
            double width = this.ActualWidth;

            // 自動再生モード中にクリックされたら一旦キャンセル
            isAutoPlayMode = false;
            synthesizer.SpeakAsyncCancelAll();

            // 画面の「左側25%」をクリックした場合は「前のカード」へ
            if (clickX < width * 0.25)
            {
                if (currentIndex > 0)
                {
                    currentIndex--;
                    UpdateUI();
                }
            }
            // 画面の「右側25%」をクリックした場合は「次のカード」へ
            else if (clickX > width * 0.75)
            {
                if (currentIndex < FlashCardManager.Cards.Count - 1)
                {
                    currentIndex++;
                    UpdateUI();
                }
            }
            // それ以外（中央50%）をクリックした場合は「自動再生」
            else
            {
                playCount = 0;
                isAutoPlayMode = true;
                StatusText.Text = $"▶ Auto Playing... (1/{maxPlays})   [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                SpeakCurrentText();
            }
        }

        private void SpeakCurrentText()
        {
            synthesizer.SpeakAsync(EnglishText.Text);
        }

        private async void Synthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (!isAutoPlayMode) return;

            playCount++;

            if (playCount < maxPlays)
            {
                StatusText.Text = $"▶ Auto Playing... ({playCount + 1}/{maxPlays})   [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                await Task.Delay(1000);
                SpeakCurrentText();
            }
            else
            {
                // 再生が終わったら、自動で次のカードへめくる
                if (currentIndex < FlashCardManager.Cards.Count - 1)
                {
                    StatusText.Text = "⏭ Moving to Next...";
                    await Task.Delay(1500);

                    currentIndex++;
                    UpdateUI();

                    // 次のカードも続けて再生を開始する
                    playCount = 0;
                    SpeakCurrentText();
                }
                else
                {
                    StatusText.Text = $"✅ All Finished!   [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
                    isAutoPlayMode = false;
                }
            }
        }
        public void AddNewCard(string newSentence)
        {
            if (string.IsNullOrWhiteSpace(newSentence)) return;

            // 新しいカードデータを作成して追加
            var newItem = new FlashCardItem { EnglishSentence = newSentence.Trim() };
            FlashCardManager.Cards.Add(newItem);

            // JSONファイルに保存
            FlashCardManager.Save();

            // 今追加した最新のカード（一番後ろ）に表示を切り替える
            currentIndex = FlashCardManager.Cards.Count - 1;

            // UIを更新し、追加成功のメッセージを出す
            UpdateUI();
            StatusText.Text = $"✨ Added New Card!   [ {currentIndex + 1} / {FlashCardManager.Cards.Count} ]";
        }
        // 🔍「意味」検索ボタンが押されたとき
        private void SearchMeaning_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // カード本体のクリック（読み上げ等）が誤爆するのを防ぐ
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            // 検索ワードをURL用に変換し、末尾に「 意味」を強制的に付ける
            string query = Uri.EscapeDataString(EnglishText.Text + " 意味");
            string url = $"https://www.google.com/search?q={query}";

            // 標準ブラウザで開く（.NET 8では UseShellExecute = true が必須です）
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        // 🖼「画像」検索ボタンが押されたとき
        private void SearchImage_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // 同様に誤爆防止
            if (string.IsNullOrWhiteSpace(EnglishText.Text)) return;

            // tbm=isch を付けるとGoogle画像検索のタブが直接開きます
            string query = Uri.EscapeDataString(EnglishText.Text);
            string url = $"https://www.google.com/search?tbm=isch&q={query}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        // 🗑「削除」ボタンが押されたとき
        private void DeleteCard_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // 誤爆防止
            
            // そもそもカードが無い場合は何もしない
            if (FlashCardManager.Cards.Count == 0) return;

            // 現在表示しているカードをリストから削除
            FlashCardManager.Cards.RemoveAt(currentIndex);
            
            // 削除した結果をすぐにJSONファイルへ上書き保存
            FlashCardManager.Save();

            // もし削除した結果、カードが1枚も無くなってしまった場合
            if (FlashCardManager.Cards.Count == 0)
            {
                EnglishText.Text = "カードがありません。\nコピーした文章を Ctrl + V で追加してください。";
                StatusText.Text = "Empty";
                // PageText.Text = "0 / 0"; // ※XAMLにPageTextを定義している場合はこれも更新
                return;
            }

            // もし「一番最後」のカードを削除した場合、インデックスがオーバーフローしないように1つ戻す
            if (currentIndex >= FlashCardManager.Cards.Count)
            {
                currentIndex = FlashCardManager.Cards.Count - 1;
            }

            // 画面を更新して次の（または前の）カードを表示
            UpdateUI();
        }
    }
}