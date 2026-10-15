using System;

namespace AppBarTimer.Models
{
    public class FlashCardItem
    {
        // カードの固有ID
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 英語の文章
        public string EnglishSentence { get; set; } = "";

        // メモや日本語訳（将来の拡張用）
        public string Note { get; set; } = "";

        // 登録した日時
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //論理削除されたかどうかを判定するフラグ
        public bool IsDeleted { get; set; } = false;
    }
}