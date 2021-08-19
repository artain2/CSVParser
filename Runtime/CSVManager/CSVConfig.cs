using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace CSVParser
{
    [CreateAssetMenu(fileName = "CSVConfig", menuName = "Configs/CSVConfig")]
    public class CSVConfig : ScriptableObject
    {
        #region Fields

        [SerializeField] private List<CSVDownloadInfo> items;

        public List<CSVDownloadInfo> Items => items;

        #endregion

        #region Main

        public CSVDownloadInfo GetItem(string key) => items.FirstOrDefault(x => x.Key == key);

        public static CSVConfig LoadInstance()
        {
            return Resources.Load<CSVConfig>("CSVConfig");
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void UpdateAll()
        {
            CSVDownloader.UpdateAllAssets(items);
        }


        #endregion

    }

    [Serializable]
    public class CSVDownloadInfo
    {

        #region Fields

        [SerializeField] string key;
        [SerializeField] string dowloadUrl;
        [SerializeField] string storagePath;
#if UNITY_EDITOR
        [SerializeField] TextAsset asset;
        [SerializeField] string originalUrl;

        public TextAsset Asset => asset;
        public string OriginalUrl => originalUrl;
#endif


        public string Key => key;
        public string DowloadUrl => dowloadUrl;
        public string StoragePath => storagePath;

        public CSVDownloadInfo(TextAsset asset, string originalUrl, string key, string dowloadUrl, string storagePath)
        {
#if UNITY_EDITOR
            this.asset = asset;
            this.originalUrl = originalUrl;
#endif
            this.key = key;
            this.dowloadUrl = dowloadUrl;
            this.storagePath = storagePath;
        }

        #endregion

        #region Odin

#if ODIN_INSPECTOR
        [Button]
        private void Download() => Download(null, null);

        public void Download(string path, Action<int> downloadProgress)
        {

#if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutineOwnerless(CSVDownloader.UpdateData(this, path, progressCallback: downloadProgress));
#else
            CoroutineProvider.DoCoroutine(GoogleDocsCSVDownloader.UpdateData(this, path, progressCallback: downloadProgress));
#endif
        }
#endif
        #endregion
    }
}