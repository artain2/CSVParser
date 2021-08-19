using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace CSVParser.Internal
{
    /* public class CSVAppendableContainer : CSVContainer
     {
         public void SetHeaderValues(TableHeaderValues headerValues)
         {
             this.headerValues = headerValues;
         }
     }*/


    public interface IHasCSVColumns
    {
        int GetColumnIndex(string columnName);
        int GetColumnIndex(string groupName, string columnName);
        int GetColumnIndexByMapping(IList<string> mappingStack);
    }

    /// <summary>
    /// Сгруппированные значения заголовка таблицы
    /// Содержит:
    ///  - Названия и позиции столбцов - <see cref="columns"/>
    ///  - Названия и диапазон групп столбцов - <see cref="groups"/>
    ///  - Начало и конец региона маппинга -
    /// </summary>
    public class TableHeaderValues
    {
        #region Fields

        public int Width { get; private set; }
        public List<TableColumnInfo> Columns { get; private set; }
        public List<TableColumnGroupInfo> Groups { get; private set; }
        public Dictionary<string, MappingItem> Mapping { get; private set; }
        public TableLayout Info { get; private set; }
        public Size FullSize => new Size(0, Width);
        public ScriptedCell Title { get; private set; }


        #endregion

        #region Main

        public TableHeaderValues(int width, TableLayout info, ScriptedCell title, List<TableColumnInfo> columns, List<TableColumnGroupInfo> groups, Dictionary<string, MappingItem> mapping)
        {
            Info = info;
            Width = width;
            Columns = columns;
            Groups = groups;
            Mapping = mapping;
            Title = title;
        }

        public int GetColumnIndex(CSVTagAttribute tagAttribute) => tagAttribute.ColumnGroup == null ? GetColumnIndex(tagAttribute.ColumnName) : GetColumnIndex(tagAttribute.ColumnGroup, tagAttribute.ColumnName);

        public int GetColumnIndex(string columnName) => GetColumnIndex(null, columnName);

        public string GetColumnName(int columnIndex)
        {
            return Columns[columnIndex].Title;
        }

        public Size GetColumnGroupSize(string columnGroupName)
        {
            return columnGroupName == null ? FullSize : Groups.FirstOrDefault(x => x.Title == columnGroupName).Size;
        }

        public int GetColumnIndex(string columnGroupName, string columnName)
        {
            if (columnGroupName == null)
                return Columns.FirstOrDefault(x => x.Title == columnName).Column;

            var size = columnGroupName == null ? FullSize : GetColumnGroupSize(columnGroupName);
            for (int i = size.Start; i < size.End + 1; i++)
                if (Columns[i].Title == columnName)
                    return Columns[i].Column;
            throw new Exception($"Cant find column name [{columnName}] in group [{columnGroupName} {size}]");
        }

        public string GetColumnInGroupName(string columnGroupName, int columnInGroupIndex)
        {
            return Columns[GetColumnInGroupIndex(columnGroupName, columnInGroupIndex)].Title;
        }

        public int GetColumnInGroupIndex(string columnGroupName, int columnInGroupIndex)
        {
            var size = columnGroupName == null ? FullSize : GetColumnGroupSize(columnGroupName);
            return size.Start + columnInGroupIndex;
        }

        public int GetColumnIndexByMapping(Stack<string> mappingStack)
        {
            if (mappingStack.Count == 1)
                return Mapping[mappingStack.Peek()].Column;
            return GetColumnIndexByMapping(mappingStack.ToArray());
        }

        public int GetColumnIndexByMapping(IList<string> mappingStack)
        {
            MappingItem currentItem = Mapping[mappingStack[mappingStack.Count - 1]];
            for (int i = mappingStack.Count - 2; i >= 0; i--)
                currentItem = currentItem.NestedItems[mappingStack[i]];
            return currentItem.Column;
        }

        #endregion
    }

    /// <summary>
    /// Группа строк. Строки находятся в диапазоне между регионами обозначенными ##
    /// </summary>
    public class TableRowRange
    {
        #region Fields

        public ScriptedCell Title { get; private set; }
        public int Start { get; private set; }
        public int End { get; private set; }

        Func<int, TableRow> getRowFunc;

        public string Name => Title.Name;

        #endregion

        #region Main

        public TableRowRange(ScriptedCell title, int start, int end, Func<int, TableRow> getRowFunc)
        {
            Title = title;
            Start = start;
            End = end;
            this.getRowFunc = getRowFunc;
        }

        public List<TableRow> GetRows()
        {
            List<TableRow> result = new List<TableRow>();
            for (int i = Start; i < End + 1; i++)
                result.Add(getRowFunc(i));
            return result;
        }

        public void ForEach(Action<TableRow> action)
        {
            for (int i = Start; i < End + 1; i++)
                action(getRowFunc(i));
        } 

        #endregion
    }

    /// <summary>
    /// Регионы столбцов 
    /// Обычно указываются как объединенные ячейки на первой строке
    /// Используются как пространства имен
    /// </summary>
    public struct TableColumnGroupInfo
    {
        #region Fields

        string title;
        Size size;

        public string Title => title;
        public int Start => size.Start;
        public int End => size.End;
        public int Length => size.Length;
        public Size Size => size;

        #endregion

        #region Main

        public TableColumnGroupInfo(string title, int start, int end)
        {
            this.title = title;
            this.size = new Size(start, end);
        }

        #endregion

        #region Overrides

        public static bool operator ==(TableColumnGroupInfo a, TableColumnGroupInfo b) => a.title == b.title && a.size == b.size;
        public static bool operator !=(TableColumnGroupInfo a, TableColumnGroupInfo b) => !(a == b);
        public override bool Equals(object obj) => obj is TableColumnGroupInfo info && title == info.title && EqualityComparer<Size>.Default.Equals(size, info.size);
        public override int GetHashCode() => 973329613 * (-1521134295 + EqualityComparer<string>.Default.GetHashCode(title)) * (-1521134295 + size.GetHashCode());
        public override string ToString() => $"{title} {size}";

        #endregion
    }

    /// <summary>
    /// Потому что <see cref="Vector2Int"/> для слабаков!
    /// </summary>
    public struct Size
    {
        #region Fields

        int start;
        int end;

        public int Start => start;
        public int End => end;
        public int Length => end - start + 1;

        #endregion

        #region Main

        public Size(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        #endregion

        #region Overrides

        public static bool operator ==(Size a, Size b) => a.start == b.start && a.end == b.end;
        public static bool operator !=(Size a, Size b) => !(a == b);
        public override bool Equals(object obj) => obj is Size size && start == size.start && end == size.end;
        public override int GetHashCode() => 1075529825 * (-1521134295 + start.GetHashCode()) * (-1521134295 + end.GetHashCode());
        public override string ToString() => $"({start}:{end})";

        #endregion
    }

    /// <summary>
    /// Информация о столбце таблицы
    /// Позволяет кэшировать имя и номер столбца
    /// </summary>
    public struct TableColumnInfo
    {
        #region Fields

        string title;
        int column;

        public string Title => title;
        public int Column => column;

        #endregion

        #region Main

        public TableColumnInfo(string title, int column)
        {
            this.title = title;
            this.column = column;
        }

        #endregion

        #region Overrides

        public static bool operator ==(TableColumnInfo a, TableColumnInfo b) => a.title == b.title && a.column == b.column;
        public static bool operator !=(TableColumnInfo a, TableColumnInfo b) => !(a == b);
        public override bool Equals(object obj) => obj is TableColumnInfo size && title == size.title && column == size.column;
        public override int GetHashCode() => 1075529825 * (-1521134295 + title.GetHashCode()) * (-1521134295 + column.GetHashCode());
        public override string ToString() => $"({title}:{column})";

        #endregion
    }

    /// <summary>
    /// Информация о маппинге поля
    /// Должен содержать маппинг всех вложенных полей
    /// Может быть вложен в другой MappingItem
    /// </summary>
    public struct MappingItem
    {
        #region Fields

        TableColumnGroupInfo groupInfo;
        int nestingIndex;
        Dictionary<string, MappingItem> nestedItems;

        public string Name => groupInfo.Title;
        public int Column => groupInfo.Start;
        public int NestingIndex => nestingIndex;
        public TableColumnGroupInfo GroupInfo => groupInfo;
        public Dictionary<string, MappingItem> NestedItems => nestedItems;

        #endregion

        #region Main

        public MappingItem(TableColumnGroupInfo groupInfo) : this(groupInfo, 0) { }

        public MappingItem(TableColumnGroupInfo groupInfo, int nestingIndex) : this(groupInfo, nestingIndex, new Dictionary<string, MappingItem>()) { }

        public MappingItem(TableColumnGroupInfo groupInfo, int nestingIndex, Dictionary<string, MappingItem> nestedItems)
        {
            this.groupInfo = groupInfo;
            this.nestingIndex = nestingIndex;
            this.nestedItems = nestedItems;
        }

        #endregion

        #region Overrides

        public override string ToString() => $"{groupInfo} [Nesting: {nestingIndex}] [Childs: {nestedItems.Count}]";

        #endregion
    }
}