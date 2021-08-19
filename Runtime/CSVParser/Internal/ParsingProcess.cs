using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSVParser.Internal
{

    /// <summary>
    /// Парсит строку CSV формата в двумерный массив
    /// Правила CSV формата:
    /// 1. Значения в строке разделены символом <see cref="CSVConst.DEFAULT_CELL_SEPARATOR"/> (далее - запятой)
    /// 2. Строки разделены переносом строки (\n)
    /// 3. Во всех строках одинаковое к-во ячеек
    /// 4. Если запятая и перенос строки являются частью текста ячейки, то ячейка обамляется в одинарные кавычки
    /// 5. Если кавычки являются частью текста ячейки, то каждые такие кавычки заменяются на двойные кавычки
    /// </summary>
    public class ParsingProcess
    {
        #region Fields

        bool parsingMultiTable = false;
        List<string> regionsToParse;
        TableLayout currentLayout;
        bool layoutParsed = false;
        string input;
        HeaderValuesSource currentSource;
        CSVTable currentContainer;
        int pointer = 0;
        ScriptedCell currentCustomRegion = null;
        int currentRegionStart = 0;
        SkipInfo skipInfo = SkipInfo.DontSkip();

        int CurrentWidth => currentSource.width;

        #endregion

        #region Init


        public ParsingProcess SetCSV(string input)
        {
            this.input = input;
            return this;
        }

        public ParsingProcess SetMultiTableFlag(bool muliTable)
        {
            parsingMultiTable = muliTable;
            return this;
        }

        public ParsingProcess SetRegionsToParse(params string[] regions)
        {
            regionsToParse = new List<string>();
            regionsToParse.AddRange(regions);

            for (int i = 0; i < regionsToParse.Count; i++)
                regionsToParse[i] = "##" + regionsToParse[i].TrimStart('#'); // Allows to add region names with "##" or without
            return this;
        }
        public ParsingProcess SetRegionsToParse(bool allowInsertRanges, params string[] regions)
        {
            skipInfo.AllowInsertRanges = allowInsertRanges;
            return SetRegionsToParse(regions);
        }

        public ParsingProcess SetSkip(SkipInfo info)
        {
            skipInfo = info;
            return this;
        }

        public ParsingProcess SetCurrentLayout(TableLayout layout)
        {
            currentLayout = layout;
            layoutParsed = true;
            return this;
        }


        #endregion

        #region Main

        public CSVTable ParseTable()
        {
            if (!layoutParsed && IsTableLayoutNow())
            {
                var headerScriptedCell = ParseValue.ScriptedCell(input.Substring(pointer, input.IndexOf(CSVConst.STOP_TAG)));
                SetCurrentLayout(TableLayout.FromScriptedCell(headerScriptedCell));
            }
            currentSource = new HeaderValuesSource(currentLayout, GetGridWidth());
            currentContainer = new CSVTable();

            string[] row = new string[CurrentWidth];
            int column = 0;
            int rowIndex = 0;

            while (pointer < input.Length)
            {
                var str = GetCellValue(out var eol);
                row[column] = str;
                column++;
                if (eol)
                {
                    InsertRow(row, rowIndex);
                    column = 0;
                    row = new string[CurrentWidth];
                    rowIndex++;
                    if (parsingMultiTable && IsTableLayoutNow())
                        break;
                    if (skipInfo.BreackFactor <= 0)
                        break;
                }
            }

            // Last cell was empty
            if (input[input.Length - 1] == CSVConst.DEFAULT_CELL_SEPARATOR && !parsingMultiTable)
            {
                row[CurrentWidth - 1] = "";
                InsertRow(row, rowIndex);
                TryAddCurrentCustomRegion();
            }

            currentContainer.HeaderValues = HeaderInfoBuilder.Build(currentSource);
            return currentContainer;
        }

        public List<CSVTable> ParseMultiTable()
        {
            parsingMultiTable = true;
            List<CSVTable> result = new List<CSVTable>();
            while (pointer < input.Length)
            {
                var table = ParseTable();
                Debug.Log($"Subtable {table.Name} parsed!");
                result.Add(table);
                layoutParsed = false;
            }
            return result;
        }

        #endregion

        #region Process

        private bool IsTableLayoutNow()
        {
            var len = CSVConst.AUTO_HEADER_TAG.Length + 1;
            if (pointer + len >= input.Length)
                return false;
            var tableStart = input.Substring(pointer, len).TrimStart('\"');
            return tableStart.StartsWith(CSVConst.AUTO_HEADER_TAG);
        }

        private void TryAddCurrentCustomRegion()
        {
            if (currentCustomRegion != null)
            {
                TableRowRange rangeInfo = new TableRowRange(currentCustomRegion, currentRegionStart, currentContainer.Rows.Count - 1, x => currentContainer.GetRow(x));
                currentContainer.Ranges.Add(rangeInfo);
            }
        }

        private void InsertRow(string[] toInsert, int rowIndex)
        {
            // It's Groups row
            if (currentLayout.HasGroups && rowIndex == currentLayout.GroupsIndex)
            {
                currentSource.groupsArr = toInsert;
                return;
            }
            // It's Columns row
            if (currentLayout.HasColumns && rowIndex == currentLayout.ColumnsIndex)
            {
                currentSource.namesArr = toInsert;
                return;
            }
            // It's Mapping row
            if (currentLayout.HasMapping && rowIndex >= currentLayout.MappingSize.Start && rowIndex <= currentLayout.MappingSize.End)
            {
                currentSource.mapping.Add(toInsert);
                return;
            }
            // It's Title row
            if (currentLayout.HasTitle && rowIndex == currentLayout.TitleIndex)
            {
                currentSource.title = new ScriptedCell(toInsert[0]);
                return;
            }
            // It's something else
            if (rowIndex < currentLayout.StartIndex)
                return;

            // We are in table. No regions
            if (regionsToParse == null)
            {
                InsertTableRow();
                return;
            }

            bool rowIsCustomRegion = toInsert[0].TrimStart('\"').StartsWith(CSVConst.RANGE_TAG);
            // Its normal row. Insert if we are in asked region
            if (!rowIsCustomRegion)
            {
                if (currentCustomRegion != null)
                    InsertTableRow();
                return;
            }

            // Its region tag. ignore if it not asked
            var title = new ScriptedCell(toInsert[0]);
            if (!regionsToParse.Contains(title.Name))
                return;

            // We found new region
            TryAddCurrentCustomRegion();
            currentRegionStart = currentContainer.Rows.Count;
            currentCustomRegion = title;
            if (skipInfo.AllowInsertRanges)
                InsertTableRow();


            void InsertTableRow()
            {
                if (skipInfo.SkipCheck(toInsert))
                {
                    skipInfo.BreackFactor--;
                    return;
                }
                currentContainer.Rows.Add(new TableRow((currentContainer as IHasCSVColumns).GetColumnIndex, (currentContainer as IHasCSVColumns).GetColumnIndexByMapping, toInsert));
            }
        }

        private int GetGridWidth()
        {
            bool eol = false;
            int width = 0;
            int breakFactor = 100;
            var prevPointer = pointer;

            while (!eol && pointer < input.Length)
            {
                breakFactor--;
                if (breakFactor < 0)
                    throw new StackOverflowException();
                var str = GetCellValue(out eol);
                width++;
            }
            pointer = prevPointer;
            //Last sym was empty
            if (input[input.Length - 1] == CSVConst.DEFAULT_CELL_SEPARATOR)
                width++;
            return width;
        }

        private string GetCellValue(out bool eolFlag)
        {
            char firstSym = input[pointer]; // first symbol in cell
            string result = "";

            if (TryGetEmptyCell(out eolFlag))
                return result;

            bool hasQuotes = firstSym == '"'; // cell has "quotes" flag
            result = hasQuotes ? GetQuotesCell(out eolFlag) : GetNoQuotesCell(out eolFlag);
            return result;
        }

        private bool TryGetEmptyCell(out bool eolFlag) // EMPTY CELL IN ROW
        {
            var firstSym = input[pointer];
            eolFlag = false;
            if (firstSym == CSVConst.DEFAULT_CELL_SEPARATOR)
            {
                pointer++;
                return true;
            }

            // EMPTY CELL IN THE END OF ROW
            if (firstSym == '\r' && input[pointer + 1] == '\n' || firstSym == '\n')
            {
                eolFlag = true;
                pointer += firstSym == '\n' ? 1:2;
                return true;
            }

            return false;
        }

        private string GetNoQuotesCell(out bool eolFlag) // NO "QUOTES" CELL
        {
            var prevPointer = pointer; // cell start mark
            eolFlag = false;
            pointer++;
            string result;
            //int breakFactor = 1000;

            while (pointer < input.Length)
            {
                //breakFactor--;
                //if (breakFactor < 0)
                //    throw new StackOverflowException();

                char activeChar = input[pointer];

                // Its regular sym. We need ','
                if (activeChar != CSVConst.DEFAULT_CELL_SEPARATOR && activeChar != '\r' && activeChar != '\n')
                {
                    pointer++;
                    continue;
                }

                // Its ',' so its certanly end of cell
                if (activeChar == CSVConst.DEFAULT_CELL_SEPARATOR)
                {
                    result = input.Substring(prevPointer, pointer - prevPointer);
                    pointer++;
                    return result;
                }

                // Its end of row ('\r')
                {
                    result = input.Substring(prevPointer, pointer - prevPointer);
                    pointer += activeChar == '\n' ? 1 : 2; // skip "\r\n"
                    eolFlag = true;
                    return result;
                }
            }

            // Its end of CSV
            result = input.Substring(prevPointer, pointer - prevPointer);
            eolFlag = true;
            return result;
        }

        private string GetQuotesCell(out bool eolFlag) // CELL WITH "QUOTES"
        {
            var prevPointer = pointer + 1; // cell start mark and skip first "quote"
            bool hasInnerQuotes = false;
            eolFlag = false;
            string result;

            pointer++;
            //int breakFactor = 1000;

            while (pointer < input.Length - 1)
            {
                //breakFactor--;
                //if (breakFactor<0)
                //    throw new StackOverflowException();

                char activeChar = input[pointer];

                // Its regular sym. We need quotes
                if (activeChar != '"')
                {
                    pointer++;
                    continue;
                }

                var nextChar = input[pointer + 1];
                // We found next quote
                if (nextChar == '"')
                {
                    // its double quotes
                    hasInnerQuotes = true;
                    pointer += 2; // skip this and next "quote"
                    continue;
                }

                // We end of cell
                if (nextChar == CSVConst.DEFAULT_CELL_SEPARATOR || nextChar == '\r' || nextChar == '\n') // Its certanly end of cell
                {
                    result = input.Substring(prevPointer, pointer - prevPointer);
                    if (nextChar == '\r') // Its end of row
                    {
                        eolFlag = true;
                        pointer++; // skip '\n'
                    }
                    if (nextChar == '\n') // Its end of row on mac
                    {
                        eolFlag = true;
                    }
                    if (hasInnerQuotes)
                        result = result.Replace("\"\"", "\"");
                    pointer += 2; // last sym. Skip "quotes" AND (',' or '\r')
                    return result;
                }
            }

            // Its end of CSV
            pointer++;
            result = input.Substring(prevPointer, pointer - prevPointer - 1); // Skip last "quote"
            eolFlag = true;
            if (hasInnerQuotes)
                result = result.Replace("\"\"", "\"");
            return result;
        }

        #endregion

        /// <summary>
        /// Группировка данных собираемых в процессе парсинга для создания разметки таблицы <see cref="TableHeaderValues"/>
        /// Передается в <see cref="HeaderInfoBuilder"/>
        /// </summary>
        public class HeaderValuesSource
        {
            #region Main

            public ScriptedCell title;
            public string[] namesArr = null;
            public string[] groupsArr = null;
            public List<string[]> mapping = null;
            public TableLayout info;
            public int width;

            public HeaderValuesSource(TableLayout info, int width)
            {
                this.info = info;
                this.width = width;
                if (info.HasMapping)
                    mapping = new List<string[]>();
            }

            #endregion
        }


        /// <summary>
        /// Вынесенныя тулза для создания <see cref="TableHeaderValues"/>
        /// </summary>
        public static class HeaderInfoBuilder
        {
            #region Main

            public static TableHeaderValues Build(ParsingProcess.HeaderValuesSource source) => Build(source.width, source.info, source.title, source.namesArr, source.groupsArr, source.mapping);

            public static TableHeaderValues Build(int width, TableLayout info, ScriptedCell title, string[] colmnNames, string[] groupNames, List<string[]> mappingGrid)
            {
                return new TableHeaderValues(width, info, title, FillColumns(colmnNames), FillGroups(groupNames), FillMapping(mappingGrid));
            }

            #endregion

            #region Misc

            /// <summary>
            /// Fill column names for HeaderValues by colmnNames[]
            /// </summary>
            /// <param name="colmnNames"></param>
            static List<TableColumnInfo> FillColumns(string[] colmnNames)
            {
                if (colmnNames == null)
                    return null;

                var columns = new List<TableColumnInfo>();
                for (int i = 0; i < colmnNames.Length; i++)
                    columns.Add(new TableColumnInfo(colmnNames[i], i));
                return columns;
            }

            /// <summary>
            /// Fill groups for HeaderValues by groupNames[]
            /// </summary>
            static List<TableColumnGroupInfo> FillGroups(string[] groupNames) => groupNames == null ? null : FillGroups(groupNames, new Size(0, groupNames.Length - 1));

            /// <summary>
            /// Fill groups for HeaderValues by groupNames[] in range
            /// </summary>
            static List<TableColumnGroupInfo> FillGroups(string[] groupNames, Size range)
            {
                if (groupNames == null)
                    return null;
                var groups = new List<TableColumnGroupInfo>();
                string rangeStartStr = null;
                int rangeStartInd = 0; // value does not metted it will be set in TryAddRange before usage
                for (int i = range.Start; i < range.End + 1; i++)
                    TryAddRangeAndIncriment(i);
                TryAddRange(range.End + 1);


                void TryAddRange(int rangeCurrentIndex)
                {
                    if (!string.IsNullOrEmpty(rangeStartStr))
                        groups.Add(new TableColumnGroupInfo(rangeStartStr, rangeStartInd, rangeCurrentIndex - 1));
                }

                void TryAddRangeAndIncriment(int rangeCurrentIndex)
                {
                    if (string.IsNullOrEmpty(groupNames[rangeCurrentIndex]))
                        return;
                    TryAddRange(rangeCurrentIndex);
                    rangeStartStr = groupNames[rangeCurrentIndex];
                    rangeStartInd = rangeCurrentIndex;
                }

                return groups;
            }

            /// <summary>
            /// Fill mapping for HeaderValues by mappingGrid[][]
            /// </summary>
            static Dictionary<string, MappingItem> FillMapping(List<string[]> mappingGrid)
            {
                if (mappingGrid == null)
                    return null;

                var maping = new Dictionary<string, MappingItem>();
                List<List<TableColumnGroupInfo>> columnGroupGrid = new List<List<TableColumnGroupInfo>>();
                columnGroupGrid.Add(FillGroups(mappingGrid[0]));
                int maxNesting = mappingGrid.Count;
                for (int i = 1; i < maxNesting; i++)
                    columnGroupGrid.Add(columnGroupGrid[i - 1].SelectMany(x => FillGroups(mappingGrid[i], x.Size)).ToList());

                foreach (var root in columnGroupGrid[0])
                {
                    var rootMappingItem = new MappingItem(root);
                    FillMappingItem(columnGroupGrid, rootMappingItem);
                    maping.Add(rootMappingItem.Name, rootMappingItem);
                }

                return maping;
            }

            static void FillMappingItem(List<List<TableColumnGroupInfo>> mappingGrid, MappingItem handle)
            {
                if (handle.NestingIndex == mappingGrid.Count - 1)
                    return;

                var serchRow = mappingGrid[handle.NestingIndex + 1];
                for (int i = 0; i < serchRow.Count; i++)
                {
                    if (serchRow[i].Start < handle.GroupInfo.Start)
                        continue;
                    if (serchRow[i].Start > handle.GroupInfo.End)
                        break;

                    var newNested = new MappingItem(serchRow[i], handle.NestingIndex + 1);
                    handle.NestedItems.Add(newNested.Name, newNested);
                    FillMappingItem(mappingGrid, newNested);
                }
            }

            #endregion
        }
    }


}