using System;
using System.Windows;

namespace AppBarTimer.Windows
{
    public partial class TranslationWindow : Window
    {
        public TranslationWindow()
        {
            InitializeComponent();
        }

        // WebView2専用の安全なページ遷移メソッド
        public async void NavigateTo(string url)
        {
            // エンジンの準備が整うまで待機
            await Browser.EnsureCoreWebView2Async();

            // 準備ができたらURLを開く
            Browser.CoreWebView2.Navigate(url);
        }
    }
}