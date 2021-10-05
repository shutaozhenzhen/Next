﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using KBEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SkySwordKill.Next
{
    public static class ModManager
    {
        #region 字段

        public static List<ModConfig> modConfigs = new List<ModConfig>();
        
        #endregion

        #region 属性

        public static Lazy<string> pluginDir =
            new Lazy<string>(() => BepInEx.Utility.CombinePaths(
                BepInEx.Paths.PluginPath, "Next"));

        public static Lazy<string> baseDataDir =
            new Lazy<string>(() => BepInEx.Utility.CombinePaths(
                pluginDir.Value, "Base"));

        public static Lazy<FieldInfo[]> dataField =
            new Lazy<FieldInfo[]>(() => typeof(jsonData).GetFields());

        

        #endregion

        #region 回调方法

        #endregion

        #region 公共方法

        public static void GenerateBaseData()
        {
            Main.LogInfo($"正在生成Base文件。");
            string dirPath = baseDataDir.Value;
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(dirPath);
            jsonData jsonInstance = jsonData.instance;
            foreach (var fieldInfo in dataField.Value)
            {
                if (fieldInfo.Name.StartsWith("_"))
                    continue;

                var value = fieldInfo.GetValue(jsonInstance);

                if (value is JSONObject jsonObject)
                {
                    string filePath = Utility.CombinePaths(dirPath, $"{fieldInfo.Name}.json");
                    File.WriteAllText(filePath, ConvertJson(jsonObject.Print(true)));
                }
                else if (value is JObject jObject)
                {
                    string filePath = Utility.CombinePaths(dirPath, $"{fieldInfo.Name}.json");
                    File.WriteAllText(filePath, jObject.ToString(Formatting.Indented));
                }
                else if (value is jsonData.YSDictionary<string, JSONObject> dicData)
                {
                    string dirPathForData = Utility.CombinePaths(dirPath, fieldInfo.Name);
                    if (!Directory.Exists(dirPathForData))
                        Directory.CreateDirectory(dirPathForData);
                    foreach (var kvp in dicData)
                    {
                        string filePath = Utility.CombinePaths(dirPathForData, $"{kvp.Key}.json");
                        File.WriteAllText(filePath, ConvertJson(kvp.Value.Print(true)));
                    }
                }
                else if (value is JSONObject[] jsonObjects)
                {
                    string dirPathForData = Utility.CombinePaths(dirPath, fieldInfo.Name);
                    if (!Directory.Exists(dirPathForData))
                        Directory.CreateDirectory(dirPathForData);
                    for (int i = 0; i < jsonObjects.Length; i++)
                    {
                        if (jsonObjects[i] == null)
                            continue;
                        string filePath = Utility.CombinePaths(dirPathForData, $"{i}.json");
                        File.WriteAllText(filePath, ConvertJson(jsonObjects[i].Print(true)));
                    }
                }
            }
        }

        public static void LoadAllMod()
        {
            modConfigs.Clear();
            Main.LogInfo($"===================" + "正在读取Mod列表" + "=====================");
            var home = Directory.CreateDirectory(pluginDir.Value);
            jsonData jsonInstance = jsonData.instance;
            foreach (var dir in home.GetDirectories("mod*"))
            {
                try
                {
                    LoadModPatch(jsonInstance, dir.FullName);
                }
                catch (Exception e)
                {
                    Main.LogError($"加载mod出错！{dir.FullName}");
                    Main.LogError(e);
                }
            }

            foreach (JSONObject jsonobject in jsonInstance._BuffJsonData.list)
            {
                var key = (int)jsonobject["buffid"].n;
                if (!jsonInstance.Buff.ContainsKey(key))
                    jsonInstance.Buff.Add(key, new Buff(key));
            }
        }

        public static void LoadModPatch(jsonData jsonInstance, string dir)
        {
            Main.LogInfo($"===================" + "开始载入Mod数据" + "=====================");
            Main.LogInfo($"加载Mod数据：{Path.GetFileNameWithoutExtension(dir)}");
            var modConfig = GetModConfig(dir);
            modConfig.Path = dir;
            Main.logIndent = 1;
            Main.LogInfo($"Mod名称：{modConfig.Name}");
            Main.LogInfo($"Mod作者：{modConfig.Author}");
            Main.LogInfo($"Mod版本：{modConfig.Version}");
            Main.LogInfo($"Mod描述：{modConfig.Description}");
            modConfigs.Add(modConfig);
            try
            {
                // 载入Mod Patch数据
                foreach (var fieldInfo in dataField.Value)
                {
                    if (fieldInfo.Name.StartsWith("_"))
                        continue;

                    var value = fieldInfo.GetValue(jsonInstance);

                    // 普通数据
                    if (value is JSONObject jsonObject)
                    {
                        string filePath = Utility.CombinePaths(dir, $"{fieldInfo.Name}.json");
                        PatchJsonObject(filePath, jsonObject);
                    }
                    else if (value is JObject jObject)
                    {
                        string filePath = Utility.CombinePaths(dir, $"{fieldInfo.Name}.json");
                        PatchJObject(filePath, jObject);
                    }
                    else if (value is jsonData.YSDictionary<string, JSONObject> dicData)
                    {
                        string dirPathForData = Utility.CombinePaths(dir, fieldInfo.Name);
                        JSONObject toJsonObject =
                            typeof(jsonData).GetField($"_{fieldInfo.Name}").GetValue(jsonInstance) as JSONObject;

                        PatchDicData(dirPathForData, dicData, toJsonObject);
                    }
                    // 功能函数配置数据
                    else if (value is JSONObject[] jsonObjects)
                    {
                        string dirPathForData = Utility.CombinePaths(dir, fieldInfo.Name);
                        PatchJsonObjectArray(dirPathForData, jsonObjects);
                    }
                }
                // 载入Mod Dialog数据
                LoadDialogEventData(dir);
                LoadDialogTriggerData(dir);
            }
            catch (Exception)
            {
                modConfig.Success = false;
                throw;
            }
            modConfig.Success = true;
            Main.logIndent = 0;
            Main.LogInfo($"===================" + "载入Mod数据完成" + "=====================");
        }

        private static ModConfig GetModConfig(string dir)
        {
            try
            {
                string filePath = Utility.CombinePaths(dir, $"modConfig.json");
                if (File.Exists(filePath))
                {
                    return JObject.Parse(File.ReadAllText(filePath)).ToObject<ModConfig>();
                }
                else
                {
                    Main.LogWarning("Mod配置不存在！");
                }
            }
            catch (Exception)
            {
                Main.LogWarning("Mod配置读取错误！");
            }

            return new ModConfig();
        }

        private static void PatchJsonObjectArray(string dirPathForData, JSONObject[] jsonObjects)
        {
            if (!Directory.Exists(dirPathForData))
                return;
            for (int i = 0; i < jsonObjects.Length; i++)
            {
                if (jsonObjects[i] == null)
                    continue;
                string filePath = Utility.CombinePaths(dirPathForData, $"{i}.json");
                PatchJsonObject(filePath, jsonObjects[i], $"{Path.GetFileNameWithoutExtension(dirPathForData)}/");
            }
        }

        private static void PatchJsonObject(string filePath, JSONObject jsonObject, string dirName = "")
        {
            if (File.Exists(filePath))
            {
                string data = File.ReadAllText(filePath);
                var jsonData = JSONObject.Create(data);
                foreach (var key in jsonData.keys)
                {
                    jsonObject.TryAddOrReplace(key, jsonData.GetField(key).Copy());
                }

                Main.LogInfo($"载入 {dirName}{Path.GetFileNameWithoutExtension(filePath)}.json");
            }
        }

        private static void PatchJObject(string filePath, JObject jObject)
        {
            if (File.Exists(filePath))
            {
                string data = File.ReadAllText(filePath);
                var jsonData = JObject.Parse(data);
                foreach (var property in jsonData.Properties())
                {
                    if (jObject.ContainsKey(property.Name))
                        jObject.Remove(property.Name);
                    jObject.Add(property.Name, property.Value.DeepClone());
                }

                Main.LogInfo($"载入 {Path.GetFileNameWithoutExtension(filePath)}.json");
            }
        }

        private static void PatchDicData(string dirPathForData, jsonData.YSDictionary<string, JSONObject> dicData,
            JSONObject toJsonObject)
        {
            if (!Directory.Exists(dirPathForData))
                return;
            foreach (var filePath in Directory.GetFiles(dirPathForData))
            {
                string data = File.ReadAllText(filePath);
                var jsonData = JSONObject.Create(data);
                var key = Path.GetFileNameWithoutExtension(filePath);
                dicData[key] = jsonData;
                toJsonObject.TryAddOrReplace(key, jsonData);
                Main.LogInfo($"载入 {Path.GetFileNameWithoutExtension(dirPathForData)}/" +
                             $"{Path.GetFileNameWithoutExtension(filePath)}.json [{key}]");
            }
        }
        
        public static void LoadDialogEventData(string dirPath)
        {
            var dirName = "DialogEvent";
            var tagDir = Path.Combine(dirPath, dirName);
            if(!Directory.Exists(tagDir))
                return;
            foreach (var filePath in Directory.GetFiles(tagDir))
            {
                string json = File.ReadAllText(filePath);
                JArray.Parse(json).ToObject<List<DialogEventData>>()?.ForEach(TryAddEventData);
                Main.LogInfo($"载入 {dirName}/{Path.GetFileNameWithoutExtension(filePath)}.json");
            }
        }
        
        public static void LoadDialogTriggerData(string dirPath)
        {
            var dirName = "DialogTrigger";
            var tagDir = Path.Combine(dirPath, dirName);
            if(!Directory.Exists(tagDir))
                return;
            foreach (var filePath in Directory.GetFiles(tagDir))
            {
                string json = File.ReadAllText(filePath);
                JArray.Parse(json).ToObject<List<DialogTriggerData>>()?.ForEach(TryAddTriggerData);
                Main.LogInfo($"载入 {dirName}/{Path.GetFileNameWithoutExtension(filePath)}.json");
            }
        }

        public static string ConvertJson(string json)
        {
            Regex reg = new Regex(@"(?i)\\[uU]([0-9a-f]{4})");
            string convertSrt = reg.Replace(json,
                delegate(Match m) { return ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString(); });
            return convertSrt;
        }

        public static void TryAddOrReplace(this JSONObject jsonObject, string key, JSONObject value)
        {
            var index = jsonObject.keys.IndexOf(key);
            if (index <= -1)
            {
                jsonObject.AddField(key, value.Copy());
            }
            else
            {
                jsonObject.list[index] = value.Copy();
            }
        }
        
        public static void TryAddEventData(DialogEventData dialogEventData)
        {
            DialogAnalysis.dialogDataDic[dialogEventData.id] = dialogEventData;
        }
        
        public static void TryAddTriggerData(DialogTriggerData dialogTriggerData)
        {
            DialogAnalysis.dialogTriggerDataDic[dialogTriggerData.id] = dialogTriggerData;
        }

        #endregion

        #region 私有方法

        #endregion
    }
}