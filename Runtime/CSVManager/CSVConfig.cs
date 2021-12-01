using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CSVParser
{
    [CreateAssetMenu(fileName = "CSVConfig", menuName = "Configs/CSVConfig")]
    public class CSVConfig : ScriptableObject
    {
        #region Fields

        [SerializeField] private string tableUrl;
        [SerializeField] private string configDownloadUrl;
        [SerializeField] private List<CSVDownloadInfo> items;

        public string TableUrl => tableUrl;
        public string ConfigDownloadUrl => configDownloadUrl;
        public List<CSVDownloadInfo> Items => items;

        #endregion Fields

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

        #endregion Main

    }

    [Serializable]
    public class CSVDownloadInfo
    {

        #region Fields

        [SerializeField] private string key;
        [SerializeField] private string dowloadUrl;
        [SerializeField] private string storagePath;
#if UNITY_EDITOR
        [SerializeField] private TextAsset asset;
        [SerializeField] private string originalUrl;

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

        #endregion Fields

        #region Odin

#if ODIN_INSPECTOR
        [Button]
        private void Download() 
        {

#if UNITY_EDITOR
            CSVDownloader.UpdateAsset(this);
#else
            CSVDownloader.UpdateStorage(this);
#endif
        }
#endif
        #endregion Odin
    }
}