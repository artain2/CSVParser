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

        [SerializeField] TextAsset csvConvert;
        [SerializeField] TextAsset csvBig;
        [SerializeField] TextAsset csvCases;

        #region Localization

        [ContextMenu("ParseLocalization")]
        void ParseLocalization()
        {
            var dict = CSVMaster.ParseLocalization(csvBig.text, "Key", "FR", true);
            Debug.Log(dict.Count);
        }

        #endregion

        #region AssureRuleParse

        [ContextMenu("SmartParse")]
        void SmartParse()
        {
            var animals = CSVMaster.ParseCSV(csvConvert.text).ConvertAll<AnimalInfo>();
            foreach (var item in animals)
                Debug.Log(item);
        }

        #endregion

        #region AssureRuleParse

        [ContextMenu("RuleParse")]
        void AssureRuleParse()
        {
            var csvContainer = CSVMaster.ParseCSV(csvConvert.text, TableLayout.WithMappingAndColumnGroups(3));
            List<AnimalInfo> infos = new List<AnimalInfo>();
            for (int i = 0; i < csvContainer.RowsCount; i++)
            {
                var info = ParseAnimal(i);
                infos.Add(info);
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

        #endregion

        #region AssureAutoParse

        //[SerializeField] int v1;
        //[SerializeField] float v2;
        //[SerializeField] bool v3;
        //[SerializeField] string v4;

        [ContextMenu("AutoParse")]
        void AssureAutoParse()
        {
            string vInt = "1";
            string vFloat = "2.2";
            string vBool = "true";
            string vString = "asd";
            //vInt = v1.ToString();
            //vFloat = v2.ToString();
            //vBool = v3.ToString();
            //vString = v4.ToString();

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

        #endregion

        #region AssureScriptedCell
        //[SerializeField] string input;
        [ContextMenu("ScriptedCell")]
        void AssureScriptedCell(string str)
        {
            var cell = new ScriptedCell(str);
            Debug.Log($"Cell >>> {cell.Name}");
            var blocks = cell.GetAllBlocks();
            foreach (var block in blocks)
            {
                Debug.Log($"Block >>> {block.Name}");
                foreach (var key in block.Keys)
                {
                    Debug.Log($"{key} > {block.GetValue<string>(key)}");
                }
            }
        }
        #endregion

        #region AssureTableParse

        //[SerializeField] TextAsset csv;
        [ContextMenu("TableParse")]
        void AssureTableParse(string csv)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result1 = CSVParser.CSVMaster.ParseCSV(csv, TableLayout.WithMappingAndColumnGroups(3));
            sw.Stop();
            Debug.Log($"CSVParser >>> {sw.Elapsed.TotalSeconds}");
            sw.Restart();

            //var result2 = ParsingUtils.Split(csv);
            //sw.Stop();
            //Debug.Log($"ParsingUtils >>> {sw.Elapsed.TotalSeconds}");
            //sw.Restart();

            Debug.Log($"CSVReader >>> {sw.Elapsed.TotalSeconds}");
        }

        #endregion
    }


}