using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

namespace CSVParser
{
    /// <summary>
    /// Позволяет скачивать и сохранять данные CSV файлов в TextAsset (только Editor) и в presistantData
    /// Процесс <see cref="CSVDownloadProcess"/> отвечает за процесс загрузки. 
    /// По окончании через коллбэк возвращается результат загрузки <see cref="CSVDownloadResult"/>
    /// В объекте результате можно запросить сохранение данных 
    /// </summary>
    public static class CSVDownloader
    {

        /// <summary>
        /// Создает кастомный процесс загрузки с ручной настройкой
        /// </summary>
        public static CSVDownloadProcess DownloadProcess(CSVDownloadInfo info) => new CSVDownloadProcess(info);

        /// <summary>
        /// Запрос CSV с дефолными параметрами
        /// </summary>
        /// <param name="progressCallback">[0-100]</param>
        public static void DownloadCSV(CSVDownloadInfo info, Action<CSVDownloadResult> completeCallback, Action<int> progressCallback = null)
        {
            new CSVDownloadProcess(info).Download(completeCallback, progressCallback);
        }

        /// <summary>
        /// Запрос CSV с дефолными параметрами (стандартный делегат web запроса)
        /// </summary>
        /// <param name="completeCallback">[Наличие ошибки], [Тексе CSV]</param>
        /// <param name="progressCallback">[0-100]</param>
        public static void DownloadCSV(CSVDownloadInfo info, Action<bool, string> completeCallback, Action<int> progressCallback = null)
        {
            new CSVDownloadProcess(info).Download(AtResultLoaded, progressCallback);

            void AtResultLoaded(CSVDownloadResult result) => completeCallback?.Invoke(result.DownloadError, result.Text);
        }

        /// <summary>
        /// Запрос обновления TextAsset с дефолными параметрами 
        /// </summary>
        /// <param name="progressCallback">[0-100]</param>
        public static void UpdateAsset(CSVDownloadInfo info, Action<int> progressCallback = null, Action atComplete = null)
        {
            new CSVDownloadProcess(info).Download(AtComplete, progressCallback);

            void AtComplete(CSVDownloadResult result)
            {
                result.RewriteAsset();
                atComplete?.Invoke();
            }
        }

        /// <summary>
        /// Запрос обновления presistantData с дефолными параметрами
        /// </summary>
        /// <param name="progressCallback">[0-100]</param>
        public static void UpdateStorage(CSVDownloadInfo info, Action<int> progressCallback = null, Action atComplete = null)
        {
            new CSVDownloadProcess(info).Download(AtComplete, progressCallback);

            void AtComplete(CSVDownloadResult result)
            {
                result.SaveiInStorage();
                atComplete?.Invoke();
            }
        }

        /// <summary>
        /// Запрос обновления нескольких TextAsset с дефолными параметрами
        /// </summary>
        public static void UpdateAllAssets(List<CSVDownloadInfo> infoList, Action atComplete = null)
        {
            Debug.Log($"{Misc.ClassKey} Начата загрузка", CSVConfig.LoadInstance());
            foreach (var info in infoList)
            {
                new CSVDownloadProcess(info).Download(AtComplete);
            }
            Debug.Log($"{Misc.ClassKey} Загрузка завершена", CSVConfig.LoadInstance());

            void AtComplete(CSVDownloadResult result)
            {
                result.RewriteAsset();
                atComplete?.Invoke();
            }
        }

        /// <summary>
        /// Запрос обновления нескольких presistantData с дефолными параметрами
        /// </summary>
        public static void UpdateAllStorages(List<CSVDownloadInfo> infoList, Action atComplete = null)
        {
            Debug.Log($"{Misc.ClassKey} Начата загрузка", CSVConfig.LoadInstance());
            foreach (var info in infoList)
            {
                new CSVDownloadProcess(info).Download(AtComplete);
            }
            Debug.Log($"{Misc.ClassKey} Загрузка завершена", CSVConfig.LoadInstance());

            void AtComplete(CSVDownloadResult result)
            {
                result.SaveiInStorage();
                atComplete?.Invoke();
            }
        }


        public static CSVDownloadProcess DownloadProcess(string key) => DownloadProcess(Misc.GetInfoByKey(key));
        public static void DownloadCSV(string key, Action<CSVDownloadResult> completeCallback, Action<int> progressCallback = null) => DownloadCSV(Misc.GetInfoByKey(key), completeCallback, progressCallback);
        public static void DownloadCSV(string key, Action<bool, string> completeCallback, Action<int> progressCallback = null) => DownloadCSV(Misc.GetInfoByKey(key), completeCallback, progressCallback);
        public static void UpdateAsset(string key, Action<int> progressCallback = null, Action atComplete = null) => UpdateAsset(Misc.GetInfoByKey(key), progressCallback, atComplete);
        public static void UpdateStorage(string key, Action<int> progressCallback = null, Action atComplete = null) => UpdateStorage(Misc.GetInfoByKey(key), progressCallback, atComplete);
        public static void UpdateAllAssets(List<string> keys, Action atComplete = null) => UpdateAllAssets(keys.Select(x => Misc.GetInfoByKey(x)).ToList(), atComplete);
        public static void UpdateAllStorages(List<string> keys, Action atComplete = null) => UpdateAllStorages(keys.Select(x => Misc.GetInfoByKey(x)).ToList(), atComplete);
        public static void UpdateAllAssets(Action atComplete = null) => UpdateAllAssets(Misc.GetCSVConfig().Items, atComplete);


        public static class Misc
        {

            static CSVConfig cachedConfig;

            public static CSVDownloadInfo GetInfoByKey(string key)
            {
                var info = GetCSVConfig().GetItem(key);
                if (info == null)
                {
                    Debug.LogError($"No key [{key}] in CSVConfig", cachedConfig);
                }
                return info;
            }

            public static CSVConfig GetCSVConfig()
            {
                if (cachedConfig == null)
                {
                    cachedConfig = CSVConfig.LoadInstance();
                }
                return cachedConfig;
            }

            public static string ClassKey { get; private set; } = $"[{nameof(CSVDownloader)}]: ";

            public static void StartCrt(IEnumerator crt, MonoBehaviour host = null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(crt);
                    return;
                }
#endif
                host.StartCoroutine(crt);
            }
        }
    }

    /// <summary>
    /// Настраиваемый процесс скачивания CSV
    /// После передачи всех параметров - вызываем <see cref="Download"/> и ждем результат в <see cref="completeCallback"/>
    /// </summary>
    public class CSVDownloadProcess
    {
        #region Fields

        public CSVDownloadInfo Info { get; set; }
        public bool AllowLogs { get; set; }
        public bool DownloadComplete { get; private set; }


        Action<int> progressCallback = null;
        Action<CSVDownloadResult> completeCallback = null;

        #endregion Fields

        #region Init

        public CSVDownloadProcess(CSVDownloadInfo info)
        {
            Info = info;
        }

        public CSVDownloadProcess SetLogs(bool allow)
        {
            AllowLogs = allow;
            return this;
        }

        #endregion Init

        #region Main

        public void Download(Action<CSVDownloadResult> completeCallback, Action<int> progressCallback = null, MonoBehaviour host = null)
        {
            if (!CheckSource())
                return;

            DownloadComplete = false;
            this.completeCallback = completeCallback;
            this.progressCallback = progressCallback;
            CSVDownloader.Misc.StartCrt(CRT_Download(), host);
        }

        private bool CheckSource()
        {
#if UNITY_EDITOR
            if (Info.Asset == null)
            {
                Debug.LogError($"{CSVDownloader.Misc.ClassKey} Не прокинут ассет в [{Info.Key}]", CSVConfig.LoadInstance());
                return false;
            }
#endif
            if (string.IsNullOrEmpty(Info.Key))
            {
                Debug.LogError($"{CSVDownloader.Misc.ClassKey} Пустой ключ", CSVConfig.LoadInstance());
                return false;
            }

            if (string.IsNullOrEmpty(Info.DowloadUrl))
            {
                Debug.LogError($"{CSVDownloader.Misc.ClassKey} Не указана ссылка в [{Info.Key}]", CSVConfig.LoadInstance());
                return false;
            }
            return true;
        }

        private IEnumerator CRT_Download()
        {
            UnityWebRequest www = new UnityWebRequest(Info.DowloadUrl);
            www.downloadHandler = new DownloadHandlerBuffer();

            if (AllowLogs)
                Debug.Log($"{CSVDownloader.Misc.ClassKey} Начата загрузка [{Info.Key}]", CSVConfig.LoadInstance());

            if (progressCallback == null)
            {
                yield return www.SendWebRequest();
            }
            else
            {
                www.SendWebRequest();
                while (!www.isDone)
                {
                    progressCallback.Invoke((int)(www.downloadProgress * 100));
                    yield return null;
                }
            }

            CSVDownloadResult result = null;
            if (www.isNetworkError)
                result = new CSVDownloadResult(Info, www.error, true);
            else
                result = new CSVDownloadResult(Info, www.downloadHandler.text, false);

            if (AllowLogs)
                Debug.Log($"{CSVDownloader.Misc.ClassKey} Загрузка завершена [{Info.Key}]", CSVConfig.LoadInstance());

            DownloadComplete = true;

            if (progressCallback != null)
                progressCallback.Invoke(100);

            if (completeCallback != null)
                completeCallback(result);
        }

        #endregion Main
    }

    /// <summary>
    /// Результат загрузки CSV файла по ссылке
    /// Может по запросу сохранить в presistentData <see cref="SaveiInStorage"/>
    /// Или обновить TextAsset <see cref="RewriteAsset"/> (Editor only)
    /// </summary>
    public class CSVDownloadResult
    {
        #region Fields

        public CSVDownloadInfo Info { get; private set; }
        public string Text { get; private set; }
        public bool DownloadError { get; private set; }

        #endregion

        #region Main

        public CSVDownloadResult(CSVDownloadInfo info, string result, bool downloadError)
        {
            Info = info;
            Text = result;
            DownloadError = downloadError;
        }

        /// <summary>
        /// Перезаписывает TextAsset указанный в <see cref=" Info"/> (EditorOnly)
        /// </summary>
        public CSVDownloadResult RewriteAsset()
        {
#if UNITY_EDITOR

            if (Info.Asset == null)
            {
                Debug.LogError($"{CSVDownloader.Misc.ClassKey} [{Info.Key}] Не получилось сохранить файл, он не указан в конфиге!");
                return this;
            }

            var filePath = AssetDatabase.GetAssetPath(Info.Asset);
            File.WriteAllText(filePath, Text);
            EditorUtility.SetDirty(Info.Asset);
            AssetDatabase.Refresh();
#endif
            return this;
        }

        /// <summary>
        /// Перезаписывает или создает данные из CSV в presistentData по пути указанному в <see cref=" Info"/> 
        /// </summary>
        public CSVDownloadResult SaveiInStorage()
        {
            var presistPath = $"{Application.persistentDataPath}/{Info.StoragePath}";
            if (!Directory.Exists(presistPath))
                Directory.CreateDirectory(presistPath);

            var filePath = $"{presistPath}/{Info.Key}";
            File.WriteAllText(filePath, Text);
            return this;
        }

        #endregion
    }
}