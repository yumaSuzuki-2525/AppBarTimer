using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AppBarTimer.Models;

namespace AppBarTimer.Utils
{
    public static class FlashCardManager
    {
        private static readonly string filePath = "flashcards.json";

        // 読み込んだすべてのカードを保持するリスト
        public static List<FlashCardItem> Cards { get; private set; } = new List<FlashCardItem>();

        // JSONから読み込む処理
        public static void Load()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Cards = JsonSerializer.Deserialize<List<FlashCardItem>>(json) ?? new List<FlashCardItem>();
            }

            // 初回起動時（ファイルが無い、または空っぽの場合）はサンプルを追加する
            if (Cards.Count == 0)
            {
                Cards.Add(new FlashCardItem { EnglishSentence = "This is the first sample sentence. Click the center to read." });
                Cards.Add(new FlashCardItem { EnglishSentence = "Click the right side to go to the next card, and the left side to go back." });
                Cards.Add(new FlashCardItem { EnglishSentence = "Programming documentation often contains useful English expressions." });
                Save(); // サンプルをファイルに保存
            }
        }

        // JSONへ保存する処理
        public static void Save()
        {
            // 人間がテキストエディタで見ても読みやすい形（インデントあり）で保存する
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Cards, options);
            File.WriteAllText(filePath, json);
        }
    }
}