using CSVParser.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSVParser
{
    /// <summary>
    /// Ячейка, содержащая параметры (заскриптованая)
    /// Параметры указываются после основного значения ячейки в фигурных скобках
    /// Ячейка может иметь несколько блоков <see cref="ScriptedBlock"/>
    /// Начало блока обозначается символом # (см. Пример1, Пример2)
    /// Ячейка унаследована от блока и может иметь собственные параметры. Такие параметры следует указывать до объявления первого блока
    /// Пример1: Shards2{#Bubble|Price:20} - шард 2ого уровня должен находиться в пузыре со стоимостью открытия = 20
    /// Пример2: Shards2{#Bubble|Price:20|#Locked|BlockVisible:TRUE} - шард 2ого уровня должен находиться в пузыре со стоимостью открытия = 20 а еще он залочен и игрок не может его видеть
    /// Пример3: Energy{Value:2|Period:24h} - Информация о некой энергии     /// 
    /// </summary>
    public class ScriptedCell : ScriptedBlock
    {
        #region Fields

        private Dictionary<string, ScriptedBlock> blocks = new Dictionary<string, ScriptedBlock>();

        public int BlocksCount => blocks.Count;
        public bool HasBlocks => blocks.Count > 0;
        public bool HasOwnValues => parametres.Count > 0;
        public List<string> BlockKeys => blocks.Keys.ToList();

        #endregion

        #region Main


        public ScriptedCell(string input, char elementsSeparator = CSVConst.CELL_PARAMS_ELEMENTS_SEPARATOR, char valueSeparator = CSVConst.VALUE_SEPARATOR)
        {
            var paramsOpenerIndex = input.IndexOf(CSVConst.CELL_PARAMS_START);
            var paramsCloseIndex = input.LastIndexOf(CSVConst.CELL_PARAMS_END);
            if (paramsOpenerIndex == -1)
            {
                name = input;
                return;
            }

            name = input.Substring(0, paramsOpenerIndex);

            // Fill blocks dictionary
            //=======================
            var kvpArrStr = input.Substring(paramsOpenerIndex + 1, paramsCloseIndex - paramsOpenerIndex - 1);
            var kvpArr = kvpArrStr.Split(CSVConst.CELL_PARAMS_ELEMENTS_SEPARATOR);
            string currentName = null;
            Dictionary<string, string> currentParametres = new Dictionary<string, string>();

            for (int i = 0; i < kvpArr.Length; i++)
            {
                // Its name of scpiped block
                if (kvpArr[i][0] == CSVConst.CELL_PARAMS_NAME)
                {
                    Insert();// Add previoius block to list if it filled
                    currentName = kvpArr[i]; // Setup new block 
                    currentParametres = new Dictionary<string, string>();
                    continue;
                }
                // Read and append key value pair
                var kvp = kvpArr[i].Split(CSVConst.VALUE_SEPARATOR);
                currentParametres.Add(kvp[0], kvp[1]);
            }

            Insert(); // Add last item

            void Insert()
            {
                if (currentName == null)
                    parametres = currentParametres;
                else
                    blocks.Add(currentName, new ScriptedBlock(currentName, currentParametres));
            }
            //=======================
        }

        public ScriptedBlock GetBlock(string blockName)
        {
            blocks.TryGetValue(blockName, out var result);
            return result;
        }

        public List<ScriptedBlock> GetAllBlocks()
        {
            return blocks.Values.ToList();
        }

        #endregion

    }

    /// <summary>
    /// Один из блоков заскритованной ячейки <see cref="ScriptedCell"/>
    /// Как правило несколько блоков используются для опционального подключения к объекту дополнительных модулей с входными параметрами
    /// Name <see cref="Name"/> Обычно указывает какой модуль нужно подключать
    /// Словарь параметров <see cref="parametres"/> устанавливает соответствие между именем поля и его значением
    /// </summary>
    public class ScriptedBlock
    {
        #region Fields

        protected string name;
        protected Dictionary<string, string> parametres;

        public string Name => name;
        public int ParametresCount => parametres.Count;
        public List<string> Keys => parametres.Keys.ToList();

        #endregion

        #region Main

        public ScriptedBlock() { }

        public ScriptedBlock(string name, Dictionary<string, string> parametres)
        {
            this.name = name;
            this.parametres = parametres;
        }

        public bool HasKey(string key) => parametres.ContainsKey(key);

        public T GetValue<T>(string name, T defaultValue = default)
        {
            if (parametres.TryGetValue(name, out var result))
                return ParseValue.Auto(result, defaultValue);
            return defaultValue;
        }
        public bool TryGetValue<T>(string name, out T result) => TryGetValue(name, default, out result);
        public bool TryGetValue<T>(string name, T defaultValue, out T result)
        {
            if (!parametres.TryGetValue(name, out var str))
            {
                result = defaultValue;
                return false;
            }
            result = GetValue<T>(name);
            return true;
        }

        public object GetObject(string name, Type type)
        {
            if (parametres.TryGetValue(name, out var result))
                return ParseValue.Object(result, type);
            return null;
        }

        public bool TryGetObject(string name, Type type, out object result)
        {
            if (!parametres.TryGetValue(name, out var str))
            {
                result = null;
                return false;
            }
            result = ParseValue.Object(str, type);
            return true;
        }

        #endregion
    }
}
