using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using MCToolsCommonLib.Common;

namespace MCBlockViewer.Models
{
    /// <summary>
    /// ウィンドウの設定データを保持するクラス
    /// </summary>
    public class WindowSettingData
    {
        /// <summary>
        /// ウィンドウの位置を保持するプロパティ
        /// </summary>
        public Point WindowPos { get; set; }

        /// <summary>
        /// ウィンドウのサイズを保持するプロパティ
        /// </summary>
        public Size WindowSize { get; set; }

        /// <summary>
        /// コンストラクタ。デフォルトのウィンドウ位置とサイズを設定する。
        /// </summary>
        public WindowSettingData()
        {
            WindowPos = new Point(100, 100);
            WindowSize = new Size(1000, 500);
        }
    }

    /// <summary>
    /// ツールの設定データを保持するクラス
    /// </summary>
    public class ToolConfig
    {
        /// <summary>
        /// Minecraftのバニラパスを保持するプロパティ
        /// </summary>
        public string VanillaMinecraftPath { get; set; }

        /// <summary>
        /// MinecraftのMODパスを保持するプロパティ
        /// </summary>
        public string ModMinecraftPath { get; set; }

        /// <summary>
        /// Minecraftのバージョンを保持するプロパティ
        /// </summary>
        public string MinecraftVersion { get; set; }

        /// <summary>
        /// 前回のアイテムIDを保持するプロパティ
        /// </summary>
        public string PrevItemID { get; set; }

        /// <summary>
        /// 前回のアイテム名を保持するプロパティ
        /// </summary>
        public string PrevItemName { get; set; }

        /// <summary>
        /// 出力先のパスを保持するプロパティ
        /// </summary>
        public string OutputBasePath { get; set; }

        /// <summary>
        /// 背景色を保持するプロパティ
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// 出力サイズのリストを保持するプロパティ
        /// </summary>
        public List<int> OutputSizeList { get; set; }

        /// <summary>
        /// コンストラクタ。デフォルトの設定を初期化する。
        /// </summary>
        public ToolConfig()
        {
            VanillaMinecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            ModMinecraftPath = "";

            List<string> programList = CommonLib.GetMinecraftProgramFiles(VanillaMinecraftPath, "1.13");
            MinecraftVersion = programList.Count > 0 ? programList[0] : "";
            PrevItemID = "";
            PrevItemName = "";
            OutputBasePath = Directory.GetCurrentDirectory();
            BackgroundColor = "#000000";
            OutputSizeList = new List<int> { 16, 32, 64, 128, 256, 512, 1024 };

            //WindowSetting = new WindowSettingData();
        }
    }
}
