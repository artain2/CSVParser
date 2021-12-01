using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using CSVParser;

#if JARVIS
using DrawerTools;

namespace Jarvis
{
    public class CSVPanel : JarvisPanel
    {
        #region Fields

        private List<FileDownload> lines;
        private CSVConfig csvConfig;
        private Editor csvConfigEditor;
        private DTToolbar tabs;
        private DTButton reload;
        private DTButton selectConfig;
        private DTButton downloadAll;

        public static readonly string[] TabTypes = new string[] { "Main", "Config" };

        #endregion Fields

        #region DT

        public CSVPanel(IDTPanel parent) : base(parent)
        {
            //Tooltip = "CSV таблицы";
            tabs = new DTToolbar(callback: null, TabTypes);
            reload = new DTButton(FontIconType.Restart, Reload).SetWidth(20) as DTButton;
            selectConfig = new DTButton(FontIconType.ZoomIn, () => DT.ShowAsset(CSVConfig.LoadInstance())).SetWidth(20) as DTButton;
            downloadAll = new DTButton(FontIconType.InBox, () => lines.ForEach(x => x.UpdateAsset())).SetWidth(20) as DTButton;
            Reload();
        }

        protected override void AtDraw()
        {
            DTScope.DrawHorizontal(tabs, downloadAll, selectConfig, reload);

            // Основная
            if (tabs.Value == 0)
            {
                if (lines == null || lines.Count == 0)
                {
                    DT.Label("Нет конфигов");
                    return;
                }

                foreach (var item in lines)
                {
                    item.Draw();
                }
            }
            // Конфиг
            else if (tabs.Value == 1)
            {
                if (csvConfig == null || csvConfigEditor == null)
                {
                    DT.Label("Не найден файл конфиг");
                    return;
                }
                csvConfigEditor.OnInspectorGUI();
            }
        }

        #endregion DT

        #region Misc

        private void Reload()
        {
            // var configs = FindConfigs();
            // if (configs.Length > 0)
            // csvConfig = configs[0];

            csvConfig = CSVConfig.LoadInstance();

            if (csvConfig)
            {
                csvConfigEditor = Editor.CreateEditor(csvConfig);
                lines = csvConfig.Items.Select(x => new FileDownload(x)).OrderBy(x => x.AssetName).ToList();
            }
        }

        private CSVConfig[] FindConfigs()
        {
            var result = new List<CSVConfig>();
            var files = DTAssets.FindAssetsByType<CSVConfig>();
            foreach (var file in files)
            {
                if (file) result.Add(file);
            }
            return result.ToArray();
        }

        #endregion Misc

        private class FileDownload : DTDrawable
        {
            #region Fields

            public string AssetName => info.Key;
            public string URL => info.DowloadUrl;
            public bool DownloadInProcess { get; private set; }

            private CSVDownloadInfo info;
            private DTLabel labelName;
            private DTButton buttonUrl;
            private DTButton buttonDownload;
            private DTButton buttonSelectFile;

            #endregion Fields

            #region DT

            public FileDownload(CSVDownloadInfo info)
            {
                this.info = info;

                labelName = new DTLabel(AssetName);
                buttonSelectFile = new DTButton("*", AtSelectFile).SetWidth(20f) as DTButton;
                buttonUrl = new DTButton("URL", OpenUrl).SetWidth(40f) as DTButton;
                buttonDownload = new DTButton("Download", UpdateAsset).SetWidth(120f) as DTButton;

                void AtSelectFile()
                {
                    DT.ShowAsset(info.Asset);
                }

                void OpenUrl()
                {
                    Application.OpenURL(info.OriginalUrl);
                }

                void CopyLink()
                {
                    EditorGUIUtility.systemCopyBuffer = info.DowloadUrl;
                }
            }

            protected override void AtDraw()
            {
                DTScope.DrawHorizontal(labelName, buttonSelectFile, buttonUrl, buttonDownload);
            }

            #endregion DT

            #region Main

            public void UpdateAsset()
            {
                if (DownloadInProcess)
                    return;

                DownloadInProcess = true;

                CSVDownloader.UpdateAsset(info, AtProgress, AtComplete);

                void AtProgress(int progress)
                {
                    buttonDownload.SetName($"{progress}/100");
                }

                void AtComplete()
                {
                    buttonDownload.SetName("Download");
                    buttonDownload.Enabled = true;
                    DownloadInProcess = false;
                }
            }

            #endregion Main
        }
    }

}

#endif // JARVIS