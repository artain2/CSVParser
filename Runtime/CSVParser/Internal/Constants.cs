using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CSVParser.Internal
{
    public static class CSVConst
    {
        #region Const

        // General
        public const char DEFAULT_CELL_SEPARATOR = ',';

        // Range and SubTables
        public const string AUTO_HEADER_TAG = "##Header";
        public const string AUTO_HEADER_TITLE = "Title";
        public const string AUTO_HEADER_MAPPING = "Mapping";
        public const string AUTO_HEADER_GROUPS = "Groups";
        public const string AUTO_HEADER_COLUMNS = "Columns";
        public const string AUTO_HEADER_START = "Start";
        public const string RANGE_TAG = "##";
        public const string STOP_TAG = "!##";

        // Cell Params
        public const char CELL_PARAMS_NAME = '#';
        public const char CELL_PARAMS_START = '{';
        public const char CELL_PARAMS_END = '}';
        public const char CELL_PARAMS_ELEMENTS_SEPARATOR = '|';

        // Special Elements
        public const char VALUE_SEPARATOR = ':';
        public const char ARRAY_START = '[';
        public const char ARRAY_END = ']';
        public const char ARRAY_SEPARATOR = ',';

        #endregion
    }
}