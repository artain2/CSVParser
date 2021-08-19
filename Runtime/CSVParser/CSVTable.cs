using CSVParser.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSVParser
{
    public class CSVTable : IHasCSVColumns
    {
        #region Fields

        protected TableHeaderValues headerValues;
        protected List<TableRow> table = new List<TableRow>();
        protected List<TableRowRange> ranges = new List<TableRowRange>();

        public TableHeaderValues HeaderValues { get => headerValues; set => headerValues = value; }
        public TableLayout HeaderInfo => headerValues.Info;
        public List<TableRow> Rows => table;
        public List<TableRowRange> Ranges => ranges;
        public int RowsCount => table.Count;
        public int Width => headerValues.Width;
        public ScriptedCell Title => headerValues.Title;
        public string Name => Title == null ? "" : Title.Name;

        #endregion

        #region Main

        public CSVTable() { }

        public CSVTable(CSVTable original, Size range)
        {
            headerValues = original.headerValues;
            table.Clear();
            for (int i = range.Start; i < range.End + 1; i++)
                table.Add(table[i]);
        }

        #endregion

        #region Regions

        public TableRow GetRow(int row) => table[row];

        public TableRowRange GetRowRange(string name)
        {
            return ranges.FirstOrDefault(x => x.Name.Contains(name));
        }

        /*  public CSVTable GetRegion(int start, int end = -1)
          {
              if (end < 0)
                  end = table.Count - 1;
              CSVTable result = new CSVTable(this, new Size(start, end));
              return result;
          }

          public CSVTable GetRegion(string regionName, string regionTag = CSVConst.RANGE_TAG) => GetRegion(start => start[0].StartsWith($"{regionTag}{regionName}"), end => end[0].StartsWith(regionTag));

          public CSVTable GetRegion(Func<string[], bool> startRegionDelegate, Func<string[], bool> endRegionDelegate)
          {
              int start = -1;
              int len = 0;
              for (int i = 0; i < table.Count; i++)
              {
                  if (start < 0)
                  {
                      if (startRegionDelegate(table[i].Values))
                          start = i + 1;
                      continue;
                  }

                  len++;
                  if (endRegionDelegate(table[i].Values))
                      break;
              }
              return GetRegion(start, start + len);
          }*/

        #endregion

        #region Get Single Value

        public string GetValue(int column, int row) => table[row][column];
        public T GetValue<T>(int column, int row) => ParseValue.Auto<T>(GetValue(column, row));
        public string GetValue(string columnName, int row) => GetValue(headerValues.GetColumnIndex(columnName), row);
        public T GetValue<T>(string columnName, int row) => ParseValue.Auto<T>(GetValue(columnName, row));
        public string GetValue(string groupName, string columnName, int row) => GetValue(headerValues.GetColumnIndex(groupName, columnName), row);
        public T GetValue<T>(string rangeName, string columnName, int row) => ParseValue.Auto<T>(GetValue(rangeName, columnName, row));
        public string GetValue(string rangeName, int columnInGroup, int row) => GetValue(headerValues.GetColumnInGroupIndex(rangeName, columnInGroup), row);
        public T GetValue<T>(string groupName, int column, int row) => ParseValue.Auto<T>(GetValue(groupName, column, row));

        #endregion

        #region Get Many Values

        public List<T> ConvertAll<T>() => SmartFormatter.FromCSVParser<T>(this);

        public Dictionary<T, D> ToDictionary<T, D>(Func<TableRow, T> keySelector, Func<TableRow, D> valueSelector)
        {
            Dictionary<T, D> result = new Dictionary<T, D>();
            foreach (var row in Rows)
                result.Add(keySelector(row), valueSelector(row));
            return result;
        }

        #endregion

        #region Misc

        void FillTableByGrid(List<string[]> grid)
        {
            table = new List<TableRow>();
            foreach (var item in grid)
                table.Add(new TableRow((this as IHasCSVColumns).GetColumnIndex, (this as IHasCSVColumns).GetColumnIndexByMapping, item));
        }

        int IHasCSVColumns.GetColumnIndex(string columnName) => headerValues.GetColumnIndex(columnName);

        int IHasCSVColumns.GetColumnIndex(string groupName, string columnName) => headerValues.GetColumnIndex(groupName, columnName);

        int IHasCSVColumns.GetColumnIndexByMapping(IList<string> mappingStack) => headerValues.GetColumnIndexByMapping(mappingStack);

        #endregion
    }

    /// <summary>
    /// Информация о таблице, необязательна но нужна для автоматизации чтения таблицы
    /// Позволяет указать:
    ///  - Строку содержащую начала значений - <see cref="startRow"/>
    ///  - Строку содержащую названия столбов (все заполнены, неуникальные) - <see cref="columnRow"/>
    ///  - Строку содержащую названия групп столбцов (все уникальные) - <see cref="groupsRow"/>
    ///  - Строки содержащие маппинг для автоматического приведения типа - <see cref="mappingSize"/>
    /// </summary>
    [Serializable]
    public struct TableLayout
    {
        #region Fields

        [SerializeField] int titleRow;
        [SerializeField] int groupsRow;
        [SerializeField] int columnRow;
        [SerializeField] int mappingStartRow;
        [SerializeField] int mappingEndRow;
        [SerializeField] int startRow;

        // Start
        public int StartIndex => startRow - 1;
        // Title
        public bool HasTitle => titleRow > 0;
        public int TitleIndex => titleRow - 1;
        // Groups
        public bool HasGroups => groupsRow > 0;
        public int GroupsIndex => groupsRow - 1;
        // Columns
        public bool HasColumns => columnRow > 0;
        public int ColumnsIndex => columnRow - 1;
        // Mapping
        public bool HasMapping => mappingStartRow > 0 && mappingEndRow >= mappingStartRow;
        public Size MappingSize => new Size(mappingStartRow - 1, mappingEndRow - 1);
        public int MappingStartIndex => mappingStartRow - 1;
        public int MappingEndIndex => mappingEndRow - 1;

        #endregion

        #region Main

        public TableLayout(int startRow) : this()
        {
            this.startRow = startRow;
        }

        public TableLayout(int startRow, int columnRow) : this(startRow)
        {
            SetColumnNamesRow(columnRow);
        }

        public TableLayout(int startRow, int columnRow, int rangeRow) : this(startRow, columnRow)
        {
            SetGroupsRow(rangeRow);
        }

        public TableLayout(int startRow, int columnRow, int rangeRow, int mappingStart, int mappingEnd) : this(startRow, columnRow, rangeRow)
        {
            SetMappingSize(mappingStart, mappingEnd);
        }

        public TableLayout SetStartRow(int startRow)
        {
            this.startRow = startRow;
            return this;
        }

        public TableLayout SetTitleRow(int titleRow)
        {
            this.titleRow = titleRow;
            return this;
        }

        public TableLayout SetColumnNamesRow(int columnRow)
        {
            this.columnRow = columnRow;
            return this;
        }

        public TableLayout SetGroupsRow(int rangeRow)
        {
            this.groupsRow = rangeRow;
            return this;
        }

        public TableLayout SetMappingSize(int start, int end)
        {
            mappingStartRow = start;
            mappingEndRow = end;
            return this;
        }

        #endregion

        #region Presets

        public static TableLayout Empty => new TableLayout(1);
        public static TableLayout WithColumnNames => new TableLayout(2, 1);
        public static TableLayout WithColumnGroups => new TableLayout(3, 2, 1);
        public static TableLayout WithSkip(int skip) => new TableLayout(skip + 1);
        public static TableLayout WithColumnNamesAndSkip(int skip) => new TableLayout(skip + 2, skip + 1);
        public static TableLayout WithColumnGroupsAndSkip(int skip) => new TableLayout(skip + 3, skip + 2, skip + 1);
        public static TableLayout WithMappingAndColumnNames(int mappingSize) => new TableLayout(mappingSize + 2).SetMappingSize(1, mappingSize).SetColumnNamesRow(mappingSize + 1);
        public static TableLayout WithColumnNamesAndMapping(int mappingSize) => new TableLayout(mappingSize + 2).SetMappingSize(2, mappingSize + 1).SetColumnNamesRow(1);
        public static TableLayout WithMappingAndColumnGroups(int mappingSize) => new TableLayout(mappingSize + 3).SetMappingSize(1, mappingSize).SetGroupsRow(mappingSize + 1).SetColumnNamesRow(mappingSize + 2);
        public static TableLayout WithColumnGroupsAndMapping(int mappingSize) => new TableLayout(mappingSize + 3).SetMappingSize(3, mappingSize + 2).SetGroupsRow(1).SetColumnNamesRow(2);
        public static TableLayout FromScriptedCell(ScriptedCell source)
        {
            TableLayout result = Empty;
            if (source.TryGetValue<int>(CSVConst.AUTO_HEADER_TITLE, out var title))
                result.SetTitleRow(title);
            if (source.TryGetValue<List<int>>(CSVConst.AUTO_HEADER_MAPPING, out var mapping))
                result.SetMappingSize(mapping[0], mapping.Count > 1 ? mapping[1] : mapping[0]);
            if (source.TryGetValue<int>(CSVConst.AUTO_HEADER_COLUMNS, out var columns))
                result.SetColumnNamesRow(columns);
            if (source.TryGetValue<int>(CSVConst.AUTO_HEADER_GROUPS, out var groups))
                result.SetGroupsRow(groups);
            if (source.TryGetValue<int>(CSVConst.AUTO_HEADER_START, out var start))
                result.SetStartRow(start);
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Строка таблицы
    /// Позволяет позволять преобразованные значения к известным типам
    /// </summary>
    public class TableRow : IHasCSVColumns
    {
        Func<string, string, int> funcColumnByName;
        Func<IList<string>, int> funcColumnByMapping;
        string[] row;

        public string[] Values => row;
        public string this[int index] => row[index];

        public TableRow(Func<string, string, int> funcColumnByName, Func<IList<string>, int> funcColumnByMapping, string[] row)
        {
            this.funcColumnByName = funcColumnByName;
            this.funcColumnByMapping = funcColumnByMapping;
            this.row = row;
        }

        public string GetValue(string columnName) => GetValue(null, columnName);
        public string GetValue(string groupName, string columnName) => row[funcColumnByName(groupName, columnName)];
        public T GetValue<T>(string columnName, T defaultValue = default) => GetValue(null, columnName, defaultValue);
        public T GetValue<T>(string groupName, string columnName, T defaultValue = default) => ParseValue.Auto(row[funcColumnByName(groupName, columnName)], defaultValue);
        public int GetColumnIndex(string columnName) => GetColumnIndex(null, columnName);
        public int GetColumnIndex(string groupName, string columnName) => funcColumnByName(groupName, columnName);
        public int GetColumnIndexByMapping(IList<string> mappingStack) => funcColumnByMapping(mappingStack);
    }

    /// <summary>
    /// Правило пропуска пустых или неподходящих строк
    /// </summary>
    public struct SkipInfo
    {
        #region Main

        public bool AllowInsertRanges { get; set; }
        public int BreackFactor { get; set; }
        public Func<string[], bool> SkipCheck { get; set; }

        public SkipInfo(int breackFactor, Func<string[], bool> skipCheck, bool insertRanges)
        {
            BreackFactor = breackFactor;
            SkipCheck = skipCheck;
            AllowInsertRanges = insertRanges;
        }
        public static SkipInfo DontSkip() => new SkipInfo(int.MaxValue, x => false, true);
        public static SkipInfo NoEmpty() => NoEmpty(int.MaxValue);
        public static SkipInfo NoEmpty(int breackFactor) => new SkipInfo(breackFactor, SimpleCheck, true);
        public static SkipInfo NoRegions() => NoRegions(int.MaxValue);
        public static SkipInfo NoRegions(int breackFactor) => new SkipInfo(breackFactor, x => false, false);
        public static SkipInfo NoRegionsOrEmpty() => NoRegionsOrEmpty(int.MaxValue);
        public static SkipInfo NoRegionsOrEmpty(int breackFactor) => new SkipInfo(breackFactor, SimpleCheck, false);
        public static SkipInfo Custom(Func<string[], bool> skipCheckFunk, bool allowRegions) => new SkipInfo(int.MaxValue, skipCheckFunk, allowRegions);

        static bool SimpleCheck(string[] row) => string.IsNullOrEmpty(row[0]);
        #endregion
    }
}