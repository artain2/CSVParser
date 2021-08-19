using CSVParser.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSVParser
{

    /// <summary>
    /// Содержит функции для парсинга общих типов:
    ///  - Примитивы: int. float, double bool, string, <see cref="CSVParser.ScriptedCell"/>
    ///  - Списки примитивов
    ///  - Списки с указанием веса для каждого элемента, для каждого примитива
    /// </summary>
    static class ParseValue
    {
        #region Universal

        /// <summary>
        /// Приводит строку к одному из доступных типов <see cref="SupportedTypes"/> или Enum
        /// </summary>
        public static T Auto<T>(string str, T defaultValue = default) => (T)Object(str, typeof(T), defaultValue);

        public static object Object(string str, Type type, object defaultValue = null)
        {
            if (type.IsEnum)
                return Enum.IsDefined(type, str) ? Enum.Parse(type, str) : defaultValue;


            if (!SupportedTypes.TryGetCSVType(type, out var supprotedType))
                throw new Exception($"Type {type.Name} not supported. Use ParseValue.IsTypeSupported and decomposite class first.");

            switch (supprotedType)
            {
                case CSVType.Int:
                    return Int(str, Convert.ToInt32(defaultValue));
                case CSVType.Float:
                    return Float(str, Convert.ToSingle(defaultValue));
                case CSVType.Double:
                    return Double(str, Convert.ToDouble(defaultValue));
                case CSVType.Bool:
                    return Bool(str, Convert.ToBoolean(defaultValue));
                case CSVType.String:
                    return String(str, Convert.ToString(defaultValue));
                case CSVType.Time:
                    return Time(str, (TimeSpan?)defaultValue);
                case CSVType.ScriptedCell:
                    return ScriptedCell(str);
                case CSVType.IntList:
                    return IntList(str, Convert.ToInt32(defaultValue));
                case CSVType.FloatList:
                    return FloatList(str, Convert.ToSingle(defaultValue));
                case CSVType.DoubleList:
                    return DoubleList(str, Convert.ToDouble(defaultValue));
                case CSVType.BoolList:
                    return BoolList(str, Convert.ToBoolean(defaultValue));
                case CSVType.StringList:
                    return StringList(str);
                case CSVType.TimeList:
                    return TimeList(str);
                case CSVType.ScriptedCellList:
                    return ScriptedCellList(str);
                case CSVType.IntWeightList:
                    return IntWeightList(str, Convert.ToInt32(defaultValue));
                case CSVType.FloatWeightList:
                    return FloatWeightList(str, Convert.ToSingle(defaultValue));
                case CSVType.DoubleWeightList:
                    return DoubleWeightList(str, Convert.ToDouble(defaultValue));
                case CSVType.BoolWeightList:
                    return BoolWeightList(str, Convert.ToBoolean(defaultValue));
                case CSVType.StringWeightList:
                    return StringWeightList(str);
                case CSVType.TimeWeightList:
                    return TimeWeightList(str, (TimeSpan?)defaultValue);
                case CSVType.ScriptedCellWeightList:
                    return ScriptedCellWeightList(str);
                default:
                    return defaultValue;
            }
        }

        #endregion

        #region Primitives

        public static int Int(string str, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            if (int.TryParse(str, out int result))
                return result;
            return defaultValue;
        }

        public static float Float(string str, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            if (float.TryParse(str, out float result))
                return result;
            else if (float.TryParse(str.Replace('.', ','), out result))
                return result;
            return defaultValue;
        }

        public static double Double(string str, double defaultValue = 0d)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            if (double.TryParse(str, out double result))
                return result;
            else if (double.TryParse(str.Replace('.', ','), out result))
                return result;
            return defaultValue;
        }

        public static string String(string str, string defaultValue = "")
        {
            return str;
        }

        public static bool Bool(string str, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            return str.ToUpper() == "TRUE" || str == "1";
        }

        public static TimeSpan Time(string str, TimeSpan? defaultValue = null)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue ?? new TimeSpan();
            TimeSpan result = new TimeSpan();
            string value = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsDigit(str[i]))
                {
                    value += str[i];
                    continue;
                }

                if (str[i] == 'd')
                    result += TimeSpan.FromDays(int.Parse(value));
                else if (str[i] == 'h')
                    result += TimeSpan.FromHours(int.Parse(value));
                else if (str[i] == 'm')
                    result += TimeSpan.FromMinutes(int.Parse(value));
                else if (str[i] == 's')
                    result += TimeSpan.FromSeconds(int.Parse(value));
                else
                {
                    result = defaultValue ?? new TimeSpan();
                    break;
                }
                value = "";
            }

            return result;
        }

        public static ScriptedCell ScriptedCell(string str) => new ScriptedCell(str);

        #endregion

        #region Lists

        public static List<int> IntList(string str, int defaultValue = 0) => GenericList(str, x => Int(x, defaultValue));

        public static List<float> FloatList(string str, float defaultValue = 0f) => GenericList(str, x => Float(x, defaultValue));

        public static List<double> DoubleList(string str, double defaultValue = 0f) => GenericList(str, x => Double(x, defaultValue));

        public static List<bool> BoolList(string str, bool defaultValue = false) => GenericList(str, x => Bool(x, defaultValue));

        public static List<string> StringList(string str) => GenericList(str, x => x);

        public static List<string> TimeList(string str) => GenericList(str, x => x);

        public static List<TimeSpan> ScriptedCellList(string str) => GenericList(str, x => Time(x));

        static List<T> GenericList<T>(string str, Func<string, T> itemParseDelegate)
        {
            if (string.IsNullOrEmpty(str))
                return new List<T>();
            return str.Trim(CSVConst.ARRAY_START, CSVConst.ARRAY_END).Split(CSVConst.ARRAY_SEPARATOR).Select(itemParseDelegate).ToList();
        }

        #endregion

        #region Weight Lists

        public static List<KeyValuePair<int, float>> IntWeightList(string str, int defaultValue = 0) => GenericWeightList(str, x => Int(x, defaultValue));

        public static List<KeyValuePair<float, float>> FloatWeightList(string str, float defaultValue = 0f) => GenericWeightList(str, x => Float(x, defaultValue));

        public static List<KeyValuePair<double, float>> DoubleWeightList(string str, double defaultValue = 0f) => GenericWeightList(str, x => Double(x, defaultValue));

        public static List<KeyValuePair<bool, float>> BoolWeightList(string str, bool defaultValue = false) => GenericWeightList(str, x => Bool(x, defaultValue));

        public static List<KeyValuePair<string, float>> StringWeightList(string str) => GenericWeightList(str, x => x);

        public static List<KeyValuePair<TimeSpan, float>> TimeWeightList(string str, TimeSpan? defaultValue = null) => GenericWeightList(str, x => Time(x, defaultValue));

        public static List<KeyValuePair<ScriptedCell, float>> ScriptedCellWeightList(string str) => GenericWeightList(str, x => ScriptedCell(x));

        static List<KeyValuePair<T, float>> GenericWeightList<T>(string str, Func<string, T> valueParseDelegate)
        {
            var result = new List<KeyValuePair<T, float>>();
            var kvpArr = StringList(str);
            foreach (var kvpStr in kvpArr)
            {
                bool cellScripted = kvpStr.Contains(CSVConst.CELL_PARAMS_START) && kvpStr.Contains(CSVConst.CELL_PARAMS_END);
                int scriptingEnd = kvpStr.IndexOf(CSVConst.CELL_PARAMS_END);
                int valueSymPos = kvpStr.IndexOf(CSVConst.VALUE_SEPARATOR);
                bool notNestedSeparator = !cellScripted || cellScripted && valueSymPos > scriptingEnd;
                bool hasValue = valueSymPos > 0 && notNestedSeparator;
                string itemKey = hasValue ? kvpStr.Substring(0, valueSymPos) : kvpStr;
                float itemValue = hasValue ? Float(kvpStr.Substring(valueSymPos+1)) : 0f;
                var node = new KeyValuePair<T, float>(valueParseDelegate(itemKey), itemValue);
                result.Add(node);
            }
            return result;
        }



        #endregion
    }
}