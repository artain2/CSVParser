using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using CSVParser.Internal;

namespace CSVParser
{
    public static class CSVMaster
    {
        #region Normal Table

        public static ParsingProcess Parsing() => new ParsingProcess();
        public static List<CSVTable> ParseMultiCSV(string csv) => new ParsingProcess().SetCSV(csv).ParseMultiTable();
        public static CSVTable ParseCSV(string csv) => new ParsingProcess().SetCSV(csv).ParseTable();
        public static CSVTable ParseCSV(string csv, TableLayout layout) => new ParsingProcess().SetCSV(csv).SetCurrentLayout(layout).ParseTable();
        public static CSVTable ParseCSV(string csv, SkipInfo skip) => new ParsingProcess().SetSkip(skip).SetCSV(csv).ParseTable();
        public static CSVTable ParseCSV(string csv, TableLayout layout, SkipInfo skip) => new ParsingProcess().SetSkip(skip).SetCurrentLayout(layout).SetCSV(csv).ParseTable();

        #endregion

        #region Localization


        public static Dictionary<string, string> ParseLocalization(string csv, string keyCol, string localeCol, bool lowercase) => ParseLocalization(csv, keyCol, localeCol, TableLayout.WithColumnNames, lowercase);

        public static  Dictionary<string, string> ParseLocalization(string csv, string keyCol, string localeCol, TableLayout layout, bool lowercase)
        {
            var container = ParseCSV(csv, layout, SkipInfo.NoRegionsOrEmpty());
            Dictionary<string, string> localizationDict = new Dictionary<string, string>();
            for (int i = 0; i < container.RowsCount; i++)
            {
                var strKey = container.GetValue(keyCol, i);
                if (string.IsNullOrEmpty(strKey))
                    continue;

                if (localizationDict.ContainsKey(strKey))
                    Debug.Log($"Localization contains key {strKey}");
                else
                    localizationDict.Add(strKey.ToLower(), container.GetValue(localeCol, i));
            }
            return localizationDict;
        }
        #endregion
    }
}
