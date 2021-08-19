using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace CSVParser.Internal
{
    /// <summary>
    /// Десерриализует строки или перечни строк (<see cref="TableRow"/>)
    /// Для серриализации таблицы должно выполняться одно из условий:
    ///     - Таблица имеет маппинг или информация о маппинге передана в <see cref="ParsingProcess"/>
    ///     - Названия столбцов совпадают с названиями полей (без вложенных типов)
    ///     - Названия столбцов совпадают с <see cref="CSVTagAttribute"/> поля (без вложенных типов)
    /// </summary>
    public class SmartFormatter
    {
        public static List<T> FromCSVParser<T>(CSVTable parser)
        {
            List<T> list = new List<T>();
            MemberInfo[] mi = FormatterServices.GetSerializableMembers(typeof(T));

            for (int i = 0; i < parser.RowsCount; i++)
                list.Add((T)ParseCSVGridObject(parser.GetRow(i), mi, typeof(T), null));
            // list.Add((T)ParseCSVGridObject(parser, mi, i, typeof(T), null));

            return list;
        }

        public static T FromCSVRow<T>(TableRow row)
        {
            MemberInfo[] mi = FormatterServices.GetSerializableMembers(typeof(T));
            var result = ParseCSVGridObject(row, mi, typeof(T), null);
            return (T)result;
        }

        static object ParseCSVGridObject(TableRow row, MemberInfo[] mi, Type type, Stack<string> mappingStack)
        {
            List<object> dataOfFields = new List<object>();
            var newItem = FormatterServices.GetSafeUninitializedObject(type);
            foreach (var memberInfo in mi)
            {
                if (mappingStack == null)
                    mappingStack = new Stack<string>();
                mappingStack.Push(memberInfo.Name);
                var parsed = ParseCSVGridMember(row, memberInfo as FieldInfo, mappingStack);
                dataOfFields.Add(parsed);
            }
            FormatterServices.PopulateObjectMembers(newItem, mi, dataOfFields.ToArray());
            if (mappingStack != null && mappingStack.Count > 0)
                mappingStack?.Pop();
            return newItem;
        }

        static object ParseCSVGridMember(TableRow row, FieldInfo fieldInfo, Stack<string> mappingStack)
        {
            MethodInfo parseMethod = null;
            bool typeSupported = SupportedTypes.IsTypeSupported(fieldInfo.FieldType);
            if (!typeSupported && !TryGetParsingMethod(fieldInfo.FieldType, out parseMethod))
            {
                MemberInfo[] mi = FormatterServices.GetSerializableMembers(fieldInfo.FieldType);
                return ParseCSVGridObject(row, mi, fieldInfo.FieldType, mappingStack);
            }

            int col = GetMemberColumn(row, fieldInfo, mappingStack);
            mappingStack.Pop();

            if (typeSupported)
                return ParseValue.Object(row[col], fieldInfo.FieldType);
            if (parseMethod != null)
                return parseMethod.Invoke(null, new object[] { row[col] });
            throw new Exception($"Cant parse {fieldInfo.FieldType} for value {row[col]}. It must be supported type, or have CSVParsingMethod");
        }

        static bool TryGetParsingMethod(Type type, out MethodInfo parseMethod)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            parseMethod = methods.FirstOrDefault(x => x.GetCustomAttribute<CSVParsingMethod>() != null);
            return parseMethod != null;
        }

        static int GetMemberColumn(TableRow row, FieldInfo fieldInfo, Stack<string> mappingStack)
        {
            var indexByMapping = row.GetColumnIndexByMapping(mappingStack.ToArray());
            if (indexByMapping >= 0)
                return indexByMapping;

            var csvTag = fieldInfo.GetCustomAttribute<CSVTagAttribute>();
            if (csvTag != null)
                row.GetColumnIndex(csvTag.ColumnGroup, csvTag.ColumnName);

            return row.GetColumnIndex(fieldInfo.Name);
        }
    }
}

namespace CSVParser
{
    /// <summary>
    /// Позволяет указать из какого столбца таблицы брать значение для поля
    /// Используется для таблиц без маппинга
    /// </summary>
    public class CSVTagAttribute : Attribute
    {
        private string columnName;
        private string columnGroup = null;
        public string ColumnName => columnName;
        public string ColumnGroup => columnGroup;

        public CSVTagAttribute(string columnName)
        {
            this.columnName = columnName;
        }

        public CSVTagAttribute(string columnName, string columnGroup) : this(columnName)
        {
            this.columnGroup = columnGroup;
        }
    }

    public class CSVParsingMethod : Attribute { }
}