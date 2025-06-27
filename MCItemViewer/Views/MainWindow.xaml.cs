using MCItemViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using Wpf.Ui.Controls;
using System.Diagnostics;
using HelixToolkit.Wpf.SharpDX.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;
using MCModelRenderer.Utils;

namespace MCItemViewer.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        /// <summary>
        /// MainWindowViewModelのインスタンス
        /// </summary>
        private MainWindowViewModel _viewModel;

        /// <summary>
        /// 前回選択されていたアイテムID
        /// </summary>
        private string _prevItemID;

        /// <summary>
        /// 前回のBlockViewのサイズ
        /// </summary>
        private Size _prevBlockViewSize;

        /// <summary>
        /// 前回の背景色
        /// </summary>
        private string _prevBackgroundColor;

        /// <summary>
        /// レンダリング完了を待つためのタイマー
        /// </summary>
        private DispatcherTimer _renderCompleteTimer;

        /// <summary>
        /// MainWindowのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainWindowViewModel)DataContext;
            _prevItemID = string.Empty;
            _prevBlockViewSize = new Size(0, 0);
            _prevBackgroundColor = "";

            _renderCompleteTimer = new DispatcherTimer();
            _renderCompleteTimer.Tick += RenderCompleteTimer_Tick;
        }

        /// <summary>
        /// ウィンドウが読み込まれたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FluentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBlockModel();
            return;
        }

        /// <summary>
        /// バージョンリストの選択が変更されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if ((_viewModel == null) || (combo.Items.Count == 0))
            {
                return;
            }

            _viewModel.ChangeVersion();
            return;
        }

        /// <summary>
        /// 名前空間の選択が変更されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NameSpace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if ((_viewModel == null) || (combo.Items.Count == 0))
            {
                return;
            }

            _viewModel.ChangeNameSpace();
            return;
        }

        /// <summary>
        /// AutoSuggestBoxのテキストが変更されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = (string)args.SelectedItem;
            if (sender.Name == "SuggestItemID")
            {
                // アイテムIDが変更された場合
                UpdateBlockModel("id");
            }
            else if (sender.Name == "SuggestItemName")
            {
                // アイテム名が変更された場合
                UpdateBlockModel("name");
            }

            return;
        }

        /// <summary>
        /// AutoSuggestBox上でキーが押されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SuggestBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Enterキー以外のキーが押された場合は何もしない
            if (e.Key != Key.Enter)
            {
                return;
            }

            // Enterキーが押された場合は、アイテムを更新
            AutoSuggestBox suggestBox = (AutoSuggestBox)sender;
            if (suggestBox.Name == "SuggestItemID")
            {
                // アイテムIDが変更された場合
                UpdateBlockModel("id");
            }
            else if (suggestBox.Name == "SuggestItemName")
            {
                // アイテム名が変更された場合
                UpdateBlockModel("name");
            }

            return;
        }

        /// <summary>
        /// AutoSuggestBoxがフォーカスを失ったときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AutoSuggestBox suggestBox = (AutoSuggestBox)sender;
            if (suggestBox.Name == "SuggestItemID")
            {
                // アイテムIDで更新する
                UpdateBlockModel("id");
            }
            else if (suggestBox.Name == "SuggestItemName")
            {
                // アイテム名で更新する
                UpdateBlockModel("name");
            }

            return;
        }

        /// <summary>
        /// BackgroundColorのテキストが変更されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 色のフォーマットが正しくない場合は何もしない
           if (ColorConverter.CheckColorFormat(BackgroundColor.Text) == false)
            {
                return;
            }

            UpdateBlockModel();
            return;
        }

        /// <summary>
        /// BlockViewAreaのサイズが変更されたときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockViewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBlockModel();
            return;
        }

        /// <summary>
        /// ブロックモデルを更新
        /// </summary>
        /// <param name="changeType">変更されたプロパティの種類</param>
        private void UpdateBlockModel(string changeType = "")
        {
            // ViewModelがnullの場合は何もしない
            if (_viewModel == null)
            {
                return;
            }

            // SuggestBoxを更新
            if (changeType == "id")
            {
                string text = _viewModel.ModelLoader.GetItemName(SuggestItemID.Text);
                if (text == "")
                {
                    // アイテムIDが無効な場合は何もしない
                    return;
                }

                SuggestItemName.Text = text;
            }
            else if (changeType == "name")
            {
                string text = _viewModel.ModelLoader.GetItemID(SuggestItemName.Text);
                if (text == "")
                {
                    // アイテム名が無効な場合は何もしない
                    return;
                }

                SuggestItemID.Text = text;
            }
            else
            {
                SuggestItemID.Text = _viewModel.ModelLoader.ItemID;
                SuggestItemName.Text = _viewModel.ModelLoader.ItemName;
            }

            // サイズやアイテムIDが変更されていない場合は何もしない
            if ((_prevBlockViewSize.Width == (int)BlockViewArea.ActualWidth) &&
                (_prevBlockViewSize.Height == (int)BlockViewArea.ActualHeight) &&
                (_prevItemID == SuggestItemID.Text) &&
                (_prevBackgroundColor == BackgroundColor.Text))
            {
                return;
            }

            // BlockViewを更新
            _viewModel.UpdateBlockModel(
                SuggestItemName.Text,
                (int)BlockViewArea.ActualWidth,
                (int)BlockViewArea.ActualHeight,
                BackgroundColor.Text,
                BlockView
            );

            // 前回のアイテムIDとBlockViewのサイズを更新
            _prevItemID = _viewModel.ModelLoader.ItemID;
            _prevBlockViewSize.Width = (int)BlockViewArea.ActualWidth;
            _prevBlockViewSize.Height = (int)BlockViewArea.ActualHeight;
            _prevBackgroundColor = BackgroundColor.Text;
            return;
        }

        /// <summary>
        /// ImageRenderの描画が完了したときに呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageRender_OnRendered(object sender, EventArgs e)
        {
            // レンダリングが完了するまで複数回呼び出される可能性があるため、タイマーをリセット
            _renderCompleteTimer.Interval = TimeSpan.FromMilliseconds(100);
            _renderCompleteTimer.Start();
            return;
        }

        /// <summary>
        /// 出力タイマーのTickイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenderCompleteTimer_Tick(object? sender, EventArgs e)
        {
            // ViewModelがnullの場合は何もしない
            if (_viewModel == null)
            {
                return;
            }

            // 出力処理を実行
            var task = _viewModel.OutputPng(ImageRender, NotifyArea);
            _renderCompleteTimer.Stop();
            return;
        }
    }
}
