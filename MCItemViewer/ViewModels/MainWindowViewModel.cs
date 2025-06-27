using CommunityToolkit.Mvvm.ComponentModel;
using MCToolsCommonLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MCBlockViewer.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MCToolsCommonLib.Utils;
using MCModelRenderer.MCModels;
using MCModelRenderer.Utils;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf.SharpDX;
using System.Diagnostics;

namespace MCItemViewer.ViewModels
{
    /// <summary>
    /// MainWindowViewModel クラスは、アプリケーションのメインウィンドウに関連するデータとロジックを管理します。
    /// このクラスは、設定の読み込み、バージョンや名前空間の管理、ブロックモデルの更新などを行います。
    /// </summary>
    internal partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// ツールの設定を保持するプロパティ。
        /// </summary>
        private Setting<ToolConfig> _config;

        [ObservableProperty]
        private NotifyInfo _notifyData;

        /// <summary>
        /// バニラMinecraftのパスを保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private string _vanillaMinecraftPath = "";

        /// <summary>
        /// MOD Minecraftのパスを保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private string _modMinecraftPath = "";

        /// <summary>
        /// PNG出力先のパスを保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private string _outputBasePath = "";

        /// <summary>
        /// 背景色を保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private string _backgroundColor = "#000000";

        /// <summary>
        /// Minecraftのバージョン一覧を保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private ItemBaseViewModel<string> _versionList;

        /// <summary>
        /// Minecraftの名前空間一覧を保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private ItemBaseViewModel<string> _nameSpaceList;

        /// <summary>
        /// 出力サイズ一覧を保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private ItemBaseViewModel<int> _outputSizeList;

        /// <summary>
        /// モデルローダーを保持するプロパティ。
        /// </summary>
        [ObservableProperty]
        private BlockModelLoarder _modelLoader;

        /// <summary>
        /// ブロックモデルのレンダリングを管理するプロパティ。
        /// </summary>
        [ObservableProperty]
        private ModelRender _blockRender;

        /// <summary>
        /// PNG出力が行われているかどうかを示すフラグ。
        /// </summary>
        private bool _isOutputPng = false;

        /// <summary>
        /// コンストラクタ。設定ファイルを読み込み、初期データを設定する。
        /// </summary>
        public MainWindowViewModel()
        {
            _config = new Setting<ToolConfig>(Path.Combine(Directory.GetCurrentDirectory(), "tool_settings.json"));
            _notifyData = new NotifyInfo();
            _vanillaMinecraftPath = _config.Data.VanillaMinecraftPath;
            _modMinecraftPath = _config.Data.ModMinecraftPath;
            _outputBasePath = _config.Data.OutputBasePath;
            _backgroundColor = _config.Data.BackgroundColor;
            _versionList = InitVersionList();
            _nameSpaceList = InitNameSpaceList();
            _outputSizeList = InitOutputSizeList();
            _modelLoader = InitBlockModelLoarder(_nameSpaceList.ItemList[_nameSpaceList.SelectedIndex]);
            _blockRender = new ModelRender(false);
        }

        /// <summary>
        /// フォルダ選択ダイアログを開き、選択されたパスを設定に保存。
        /// </summary>
        /// <param name="name">"OpenVanillaMinecraftPath" または "OpenModMinecraftPath" を指定</param>
        [RelayCommand]
        private void OpenFolder(string name)
        {
            string defaultPath = "";
            // 選択されたフォルダの初期パスを設定
            if (name == "OpenVanillaMinecraftPath")
            {
                defaultPath = VanillaMinecraftPath;
            }
            else if (name == "OpenModMinecraftPath")
            {
                defaultPath = ModMinecraftPath == "" ? VanillaMinecraftPath : ModMinecraftPath;
            }
            else if (name == "OpenOutputBasePath")
            {
                defaultPath = OutputBasePath == "" ? VanillaMinecraftPath : OutputBasePath;
            }

            OpenFolderDialog dlg = new OpenFolderDialog();
            dlg.Title = "フォルダを選択";
            dlg.InitialDirectory = defaultPath;
            if (dlg.ShowDialog() == false)
            {
                return;
            }

            if (name == "OpenVanillaMinecraftPath")
            {
                VanillaMinecraftPath = dlg.FolderName;
            }
            else if (name == "OpenModMinecraftPath")
            {
                ModMinecraftPath = dlg.FolderName;
            }
            else if (name == "OpenOutputBasePath")
            {
                OutputBasePath = dlg.FolderName;
            }

            _config.Data.VanillaMinecraftPath = VanillaMinecraftPath;
            _config.Data.ModMinecraftPath = ModMinecraftPath;
            _config.Data.OutputBasePath = OutputBasePath;
            _config.Save();
            return;
        }

        /// <summary>
        /// 選択されたバージョンを変更し、モデルローダーを更新する。
        /// </summary>
        public void ChangeVersion()
        {
            // 選択されたバージョンが無効な場合は何もしない
            if (VersionList.SelectedIndex == -1)
            {
                return;
            }

            // バージョンを更新する
            _config.Data.MinecraftVersion = VersionList.ItemList[VersionList.SelectedIndex];

            // モデルローダーを更新する
            ModelLoader.Dispose();
            ModelLoader = InitBlockModelLoarder(NameSpaceList.ItemList[NameSpaceList.SelectedIndex]);

            _config.Save();
            return;
        }

        /// <summary>
        /// バージョンリストを初期化する。
        /// </summary>
        /// <returns>初期化されたバージョンリスト</returns>
        private ItemBaseViewModel<string> InitVersionList()
        {
            ItemBaseViewModel<string> versionList = new ItemBaseViewModel<string>();

            // バニラMinecraftのパスが設定されていない場合は空のリストを返す
            if (!Path.Exists(VanillaMinecraftPath))
            {
                return versionList;
            }

            // 1.13以降のバージョンを取得するためのパスを設定
            List<string> programList = CommonLib.GetMinecraftProgramFiles(VanillaMinecraftPath, "1.13");

            if (programList.Count > 0)
            {
                if (_config.Data.MinecraftVersion != "")
                {
                    versionList.Init(programList, _config.Data.MinecraftVersion);
                }
                else
                {
                    versionList.Init(programList, programList[0]);
                }
            }

            return versionList;
        }

        /// <summary>
        /// 名前空間リストを初期化する。
        /// </summary>
        /// <returns>初期化された名前空間リスト</returns>
        private ItemBaseViewModel<string> InitNameSpaceList()
        {
            List<string> nameSpaces = new List<string>();
            nameSpaces.Add("minecraft");
            ItemBaseViewModel<string> nameSpaceList = new ItemBaseViewModel<string>();
            nameSpaceList.Init(nameSpaces, nameSpaces[0]);
            return nameSpaceList;
        }

        /// <summary>
        /// 出力サイズリストを初期化する。
        /// </summary>
        /// <returns>初期化された出力サイズリスト</returns>
        private ItemBaseViewModel<int> InitOutputSizeList()
        {
            List<int> outputSizes = new List<int>();
            foreach(int size in _config.Data.OutputSizeList)
            {
                outputSizes.Add(size);
            }

            ItemBaseViewModel<int> outputSizeList = new ItemBaseViewModel<int>();
            outputSizeList.Init(outputSizes, 16);
            return outputSizeList;
        }

        /// <summary>
        /// 指定された名前空間に基づいて BlockModelLoarder を初期化する。
        /// </summary>
        /// <param name="selectedNameSpace">選択された名前空間</param>
        /// <returns>初期化された BlockModelLoarder。</returns>
        private BlockModelLoarder InitBlockModelLoarder(string selectedNameSpace)
        {
            // 選択された名前空間に基づいて、BlockModelLoarderを初期化する。
            if (_config.Data.MinecraftVersion == "")
            {
                return new BlockModelLoarder();
            }

            // 指定したバージョンのMinecraftプログラムファイルパスを取得する
            string programPath = CommonLib.GetMinecraftProgramFilePath(_config.Data.VanillaMinecraftPath, _config.Data.MinecraftVersion);
            if (programPath == "")
            {
                return new BlockModelLoarder();
            }

            // 暫定的に日本語の言語コードを使用
            BlockModelLoarder modelLoader = new BlockModelLoarder(programPath, "ja_jp", _config.Data.MinecraftVersion);

            // 前回のアイテムIDが設定されている場合、モデルをロードする
            if (modelLoader.ExistsItemName(_config.Data.PrevItemName))
            {
                modelLoader.LoadModel(_config.Data.PrevItemName);
            }
 
            return modelLoader;
        }

        /// <summary>
        /// 選択された名前空間を変更し、モデルローダーを更新する。
        /// </summary>
        public void ChangeNameSpace()
        {
            ModelLoader = InitBlockModelLoarder(NameSpaceList.ItemList[NameSpaceList.SelectedIndex]);
            return;
        }

        /// <summary>
        /// 指定されたアイテムIDに基づいてブロックモデルを更新し、表示する。
        /// </summary>
        /// <param name="itemName">アイテム名</param>
        /// <param name="width">レンダリング幅</param>
        /// <param name="height">レンダリング高さ</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="viewport">表示するViewport3DX</param>
        public void UpdateBlockModel(string itemName, int width, int height, string backgroundColor, in Viewport3DX viewport)
        {
            if (itemName == "")
            {
                return;
            }

            // 前回のアイテム名と異なる場合、モデルを再読み込みする
            if (_config.Data.PrevItemName != itemName)
            {
                ModelLoader.LoadModel(itemName);
                _config.Data.PrevItemID = ModelLoader.ItemID;
                _config.Data.PrevItemName = ModelLoader.ItemName;
                _config.Save();
            }

            // 背景色が異なる場合、設定を更新する
            if (_config.Data.BackgroundColor != backgroundColor)
            {
                _config.Data.BackgroundColor = backgroundColor;
                _config.Save();
            }

            // ブロックモデルを更新する
            BlockRender.Resize(width, height);
            BlockRender.SetModel(ModelLoader.Model);
            BlockRender.Render(viewport, backgroundColor);
            return;
        }

        /// <summary>
        /// PNG出力コマンドを実行する。
        /// </summary>
        [RelayCommand]
        public void BeginOutputPng(Viewport3DX viewport)
        {
            // ブロックモデルを更新する
            string itemName = ModelLoader.ItemName;
            int size = OutputSizeList.ItemList[OutputSizeList.SelectedIndex];
            viewport.Width = size;
            viewport.Height = size;
            _isOutputPng = true;
            UpdateBlockModel(ModelLoader.ItemName, size, size, BackgroundColor, viewport);
            return;
        }

        /// <summary>
        /// PNGを出力するメソッド。
        /// </summary>
        /// <param name="viewport"></param>
        public async Task OutputPng(Viewport3DX viewport, StackPanel panel)
        {
            // モデルが読み込まれていない場合は何もしない
            if (((ModelLoader.Model.Elements.Count == 0) && (ModelLoader.Model.Textures.Count == 0)) || (!_isOutputPng))
            {
                return;
            }

            // PNGを出力する
            string outputFileName = BlockRender.Output(viewport, OutputBasePath);
            _isOutputPng = false;

            // 出力結果を通知する
            if (outputFileName == "")
            {
                await Notify(panel, "PNGの出力に失敗しました。", "#EA9999");
            }
            else
            {
                await Notify(panel, $"PNGを出力しました。: {outputFileName}", "#B7E1CD");
            }

            return;
        }

        /// <summary>
        /// バニラMinecraftのパスが変更されたときに呼び出されるイベントハンドラ。
        /// </summary>
        /// <param name="value">変更されたパス</param>
        partial void OnVanillaMinecraftPathChanged(string value)
        {
            // 設定を保存
            _config.Data.VanillaMinecraftPath = value;
            _config.Save();

            // バージョンリストとモデルローダーを更新
            VersionList = InitVersionList();
            ModelLoader = InitBlockModelLoarder(NameSpaceList.ItemList[NameSpaceList.SelectedIndex]);
            return;
        }

        /// <summary>
        /// 指定されたパネルに通知メッセージを表示する。
        /// </summary>
        /// <param name="panel">パネル</param>
        /// <param name="message">通知メッセージ</param>
        /// <param name="color">文字色</param>
        /// <returns></returns>
        private async Task Notify(object panel, string message, string color)
        {
            StackPanel stackPanel = (StackPanel)panel;
            stackPanel.Visibility = System.Windows.Visibility.Visible;
            NotifyData = new NotifyInfo(message, color);
            await Task.Delay(3000);

            NotifyData = new NotifyInfo();
            stackPanel.Visibility = System.Windows.Visibility.Collapsed;
            return;
        }
    }
}
