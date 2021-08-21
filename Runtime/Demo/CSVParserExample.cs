using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using CSVParser.Internal;

namespace CSVParser.Example
{
    public class CSVParserExample : MonoBehaviour
    {
        const string DEFAULT_SCRIPTED_CELL = "UltraGreatSword{durability:1|weight:15|price:250|#Enchants|enchantName:fire|level:3|#Reforge|ReforgeType:Heavy|ReforgeLevel:5}";
        const int DEFULT_INT = 1;
        const float DEFULT_FLOAT = 0.2f;
        const string DEFULT_STRING = "string";
        const bool DEFULT_BOOL = true;

        [Header("Tables")]
        [SerializeField] TextAsset csvConvert;
        [SerializeField] TextAsset csvBig;
        [SerializeField] TextAsset csvCases;

        [Header("ScriptedCell")]
        [SerializeField] string scriptedCell = DEFAULT_SCRIPTED_CELL;

        [Header("Primitives")]
        [SerializeField] int someInt = DEFULT_INT;
        [SerializeField] float someFloat = DEFULT_FLOAT;
        [SerializeField] string someString = DEFULT_STRING;
        [SerializeField] bool someBool = DEFULT_BOOL;

        [ContextMenu("ParseLocalization")]
        void AssureParseLocalization()
        {
            Debug.Log($"Parsing [{csvBig.name}]. It will create dictionary [key | localized string] automatically!", csvBig);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var dict = CSVMaster.ParseLocalization(csvBig.text, "Key", "FR", true);
            sw.Stop();
            Debug.Log($"Parsed {dict.Count} localization strings for {sw.ElapsedMilliseconds} milliseconds!" +
                $"\nLast key: {dict.Last().Key} | {dict.Last().Value}" );
        }

        [ContextMenu("SmartParse")]
        void AssureSmartParse()
        {
            Debug.Log($"Parsing [{csvConvert.name}]. It will create instance of class [{nameof(AnimalInfo)}] for each row automatically!", csvConvert);
            var animals = CSVMaster.ParseCSV(csvConvert.text).ConvertAll<AnimalInfo>();
            foreach (var item in animals)
                Debug.Log(item);
        }

        [ContextMenu("RuleParse")]
        void AssureRuleParse()
        {
            Debug.Log($"Parsing [{csvConvert.name}]. It will create instance of class [{nameof(AnimalInfo)}] for each row manually!", csvConvert);
            // For parsing manually you have to select current table layout, so parser can find the columns and (optional) column groups
            // This table also has mapping info rows at start, so it must be indicated
            // Table can contains only one column names row and only one column groups row 
            var csvContainer = CSVMaster.ParseCSV(csvConvert.text, TableLayout.WithMappingAndColumnGroups(3));
            List<AnimalInfo> infos = new List<AnimalInfo>();
            for (int i = 0; i < csvContainer.RowsCount; i++)
            {
                var info = ParseAnimal(i);
                infos.Add(info);
            }

            foreach (var info in infos)
            {
                Debug.Log(info);
            }

            AnimalInfo ParseAnimal(int line)
            {
                var key = ParseAnimalKey(line);
                var speed = csvContainer.GetValue<float>("Props", "Speed", line);
                var isPredator = csvContainer.GetValue<bool>("Props", "IsPredator", line);
                var colors = csvContainer.GetValue<List<int>>("Props", "Colors", line);
                return new AnimalInfo(key, speed, isPredator, colors);
            }

            AnimalKey ParseAnimalKey(int line)
            {
                var id = csvContainer.GetValue<int>("Key", "ID", line);
                var name = csvContainer.GetValue<string>("Key", "Name", line);
                return new AnimalKey(id, name);
            }
        }

        [ContextMenu("ResetPrimitives")]
        void SetDefaultPrimitives()
        {
            someInt = DEFULT_INT;
            someString = DEFULT_STRING;
            someString = DEFULT_STRING;
            someBool = DEFULT_BOOL;
        }

        [ContextMenu("PrimitivesParse")]
        void AssurePrimitivesParse()
        {
            string vInt = someInt.ToString();
            string vFloat = someFloat.ToString().Replace(',', '.');
            string vBool = someBool.ToString();
            string vString = someString;

            Debug.Log("Int >>>" + ParseValue.Auto<int>(vInt));
            Debug.Log("Float >>>" + ParseValue.Auto<float>(vFloat));
            Debug.Log("Bool >>>" + ParseValue.Auto<bool>(vBool));
            Debug.Log("String >>>" + ParseValue.Auto<string>(vString));
            Debug.Log("Int List >>>" + string.Join(" > ", ParseValue.Auto<List<int>>($"[{vInt},{vInt},{vInt}]")));
            Debug.Log("Float List >>>" + string.Join(" > ", ParseValue.Auto<List<float>>($"[{vFloat},{vFloat},{vFloat}]")));
            Debug.Log("Bool List >>>" + string.Join(" > ", ParseValue.Auto<List<bool>>($"[{vBool},{vBool},{vBool}]")));
            Debug.Log("String List >>>" + string.Join(" > ", ParseValue.Auto<List<string>>($"[{vString},{vString},{vString}]")));
            Debug.Log("Int List (no braces) >>>" + string.Join(" > ", ParseValue.Auto<List<int>>($"{vInt},{vInt},{vInt}")));
            Debug.Log("Int List (no braces, single) >>>" + string.Join(" > ", ParseValue.Auto<List<int>>($"{vInt}")));
            Debug.Log("Weight List (int, str, bool) >>>" + string.Join(" > ", ParseValue.Auto<List<KeyValuePair<string, float>>>($"[{vInt}:{vFloat},{vString}:{vFloat},{vBool}:{vFloat}]")));
            Debug.Log("Weight List (no weight) >>>" + string.Join(" > ", ParseValue.Auto<List<KeyValuePair<string, float>>>($"[{vInt},{vString},{vBool}]")));
            Debug.Log("Weight List (single) >>>" + string.Join(" > ", ParseValue.Auto<List<KeyValuePair<string, float>>>($"{vInt}:{vFloat}")));
            Debug.Log("Weight List (single, no weight) >>>" + string.Join(" > ", ParseValue.Auto<List<KeyValuePair<string, float>>>($"{vInt}")));
        }

        [ContextMenu("ResetScriptedCell")]
        void SetDefaultScriptedCell()
        {
            scriptedCell = DEFAULT_SCRIPTED_CELL;
        }

        [ContextMenu("ScriptedCell")]
        void AssureScriptedCell()
        {
            var cell = new ScriptedCell(scriptedCell);
            Debug.Log($"Cell >>> {cell.Name}");
            if (cell.HasOwnValues)
            {
                Debug.Log($"Cell >>> Own Values");
            }
            foreach (var key in cell.Keys)
            {
                Debug.Log($"Own Values >>> {key} = {cell.GetValue<string>(key)}");
            }

            var blocks = cell.GetAllBlocks();
            foreach (var block in blocks)
            {
                Debug.Log($"Block >>> {block.Name}");
                foreach (var key in block.Keys)
                {
                    Debug.Log($"{block.Name} >>> {key} = {block.GetValue<string>(key)}");
                }
            }
        }

        [ContextMenu("MainCsvParsingCases")]
        void AssureMainCsvParsingCases()
        {
            Debug.Log($"Parsing [{csvCases.name}]. It will assure main CSV parsing cases!", csvCases);
            var table = CSVMaster.ParseCSV(csvCases.text, TableLayout.WithColumnNames);
            foreach (var item in table.Rows)
            {
                string log = "";
                log += item.GetValue("ID") + "|";
                log += item.GetValue("Name") + "|";
                log += item.GetValue("Value");
                Debug.Log(log);
            }
        }
    }


}