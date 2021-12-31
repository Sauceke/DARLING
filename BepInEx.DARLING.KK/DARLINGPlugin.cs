using AutoVersioning;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI.MainGame;
using LitJson;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BepInEx.DARLING.KK
{

    [BepInPlugin(GUID, "D. A. R. L. I. N. G.", VersionInfo.Version)]
    internal class DARLINGPlugin : BaseUnityPlugin
    {
        private const string GUID = "Sauceke.DARLING";
        private const string defaultKeyword = "<enter keyword>";
        public static new ManualLogSource Logger { get; private set; }

        private ConfigEntry<string> customCommandsJson;

        private static List<Entry> customCommands;

        private void Start()
        {
            Logger = base.Logger;
            GameAPI.RegisterExtraBehaviour<VoiceController>(GUID);
            customCommandsJson = Config.Bind(
                section: "Custom Commands (REQUIRES RESTART!)",
                key: "Custom Commands",
                defaultValue: "[]",
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = CustomCommandsDrawer,
                        HideSettingName = true,
                        HideDefaultButton = true
                    }
                )
            );
            customCommands = JsonMapper.ToObject<List<Entry>>(customCommandsJson.Value);
        }

        private void OnDestroy()
        {
            customCommandsJson.Value = JsonMapper.ToJson(customCommands);
        }

        private void CustomCommandsDrawer(ConfigEntryBase entry)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
                for (int i = 0; i < 10; i++)
                {
                    if (customCommands.Count < i + 1)
                    {
                        customCommands.Add(new Entry(defaultKeyword, "<enter pose name>"));
                    }
                    GUILayout.BeginHorizontal();
                    {
                        customCommands[i].Key = GUILayout.TextField(customCommands[i].Key);
                        customCommands[i].Value = GUILayout.TextField(customCommands[i].Value);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }
        
        public static Dictionary<string, string> GetCustomCommands()
        {
            return customCommands
                .Where(entry => entry.Key != defaultKeyword)
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        public class Entry
        {
            public Entry() {}

            public Entry(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
