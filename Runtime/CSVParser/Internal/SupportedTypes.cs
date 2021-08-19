using System;
using System.Collections;
using System.Collections.Generic;

namespace CSVParser.Internal
{

    /// <summary>
    /// Поддерживаемые типы данных для парсина
    /// </summary>
    public static class SupportedTypes
    {
        #region Main

        public static CSVType GetCSVType(Type type) => SupportedTypesDict[type];
        public static bool TryGetCSVType(Type type, out CSVType csvType) => SupportedTypesDict.TryGetValue(type, out csvType);
        public static bool IsTypeSupported(Type type) => type.IsEnum || SupportedTypesDict.ContainsKey(type);

        #endregion

        #region Supported Types

        private static readonly Dictionary<Type, CSVType> SupportedTypesDict = new Dictionary<Type, CSVType>() {
            // Primitives =================================================================================
            { typeof(int), CSVType.Int },
            { typeof(float), CSVType.Float },
            { typeof(double), CSVType.Double },
            { typeof(bool), CSVType.Bool },
            { typeof(string), CSVType.String },
            { typeof(TimeSpan), CSVType.Time },
            { typeof(ScriptedCell), CSVType.ScriptedCell },
            // Lists ======================================================================================
            { typeof(List<int>), CSVType.IntList },
            { typeof(List<float>), CSVType.FloatList },
            { typeof(List<double>), CSVType.DoubleList },
            { typeof(List<bool>), CSVType.BoolList },
            { typeof(List<string>), CSVType.StringList},
            { typeof(List<TimeSpan>), CSVType.TimeList},
            { typeof(List<ScriptedCell>), CSVType.ScriptedCellList},
            // WeightLists ================================================================================
            { typeof(List<KeyValuePair<int, float>>), CSVType.IntWeightList },
            { typeof(List<KeyValuePair<float, float>>), CSVType.FloatWeightList },
            { typeof(List<KeyValuePair<double, float>>), CSVType.DoubleWeightList },
            { typeof(List<KeyValuePair<bool, float>>), CSVType.BoolWeightList},
            { typeof(List<KeyValuePair<string, float>>), CSVType.StringWeightList},
            { typeof(List<KeyValuePair<TimeSpan, float>>), CSVType.TimeWeightList},
            { typeof(List<KeyValuePair<ScriptedCell, float>>), CSVType.ScriptedCellWeightList},
            //=============================================================================================
        };

        #endregion
    }

    /// <summary>
    /// Доступные для парсинга значения
    /// </summary>
    public enum CSVType
    {
        #region Values
        // Primitives =====================
        Int,
        Float,
        Double,
        Bool,
        String,
        Time,
        ScriptedCell,
        // Lists ==========================
        IntList,
        FloatList,
        DoubleList,
        BoolList,
        StringList,
        TimeList,
        ScriptedCellList,
        // WeightLists ====================
        IntWeightList,
        FloatWeightList,
        DoubleWeightList,
        BoolWeightList,
        StringWeightList,
        TimeWeightList,
        ScriptedCellWeightList,
        //=================================
        #endregion
    }

}