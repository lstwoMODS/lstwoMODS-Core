using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace lstwoMODS_Core.Keybinds
{
    public class KeybindManager : MonoBehaviour
    {
        public static Keybinder KeybinderToUpdate
        {
            get
            {
                return _keybinderToUpdate;
            }
            set
            {
                if(_keybinderToUpdate != null && _keybinderToUpdate != value && _keybinderToUpdate.keybinds.Any(keybind => keybind.isCapturing))
                {
                    foreach(var keybind in _keybinderToUpdate.keybinds.Where(keybind => keybind.isCapturing))
                    {
                        keybind.StopDetectKeybind();
                    }
                }

                _keybinderToUpdate = value;
            }
        }

        private static List<Keybinder.Keybind> Keybinds = new List<Keybinder.Keybind>();
        private static Keybinder _keybinderToUpdate;

        void Update()
        {
            if (_keybinderToUpdate != null)
            {
                foreach (var keybind in _keybinderToUpdate.keybinds.Where(keybind => keybind.isCapturing))
                {
                    keybind.DetectKeybinds();
                }
            }

            if (Plugin.UiBase == null || Plugin.UiBase.Enabled)
            {
                return;
            }

            foreach (var keybind in Keybinds)
            {
                if(keybind.IsPressed())
                {
                    keybind.OnPressed();
                }
            }
        }

        public static Keybinder.Keybind AddKeybind(Keybinder.Keybind keybind)
        {
            Keybinds.Add(keybind);

            return keybind;
        }

        public static void RemoveKeybind(Keybinder.Keybind keybind)
        {
            Keybinds.Remove(keybind);
        }

        public static void SaveAllKeybinds()
        {
            ConvertAllKeybindsToSerializable();

            var json = JsonConvert.SerializeObject(serializableKeybinders, Formatting.None);
            var folderPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lstwoMODS";

            if(!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = $@"{folderPath}\keybinds.json";
            File.WriteAllText(filePath, json);
        }

        public static void LoadAllKeybinds()
        {
            var folderPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lstwoMODS";
            var filePath = $@"{folderPath}\keybinds.json";

            if(!Directory.Exists(folderPath) || !File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            serializableKeybinders = JsonConvert.DeserializeObject<Dictionary<string, SerializableKeybind[]>>(json);
        }

        private static void ConvertAllKeybindsToSerializable()
        {
            serializableKeybinders.Clear();

            foreach(var keybind in Keybinds)
            {
                var keybinderID = keybind.keybinder.keybinderID;
                var serializableKeybind = new SerializableKeybind()
                {
                    primaryKey = keybind.primaryKey,
                    secondaryKeys = keybind.secondaryKeys.ToArray(),
                    data = keybind.SerializeData()
                };

                if(!serializableKeybinders.TryGetValue(keybinderID, out var serializableKeybinds))
                {
                    serializableKeybinds = new SerializableKeybind[1] { serializableKeybind };
                    serializableKeybinders.Add(keybinderID, serializableKeybinds);
                    continue;
                }

                var keybindsList = serializableKeybinds.ToList();
                keybindsList.Add(serializableKeybind);
                serializableKeybinders[keybinderID] = keybindsList.ToArray();
            }
        }

        public static Dictionary<string, SerializableKeybind[]> serializableKeybinders = new();

        [Serializable]
        public class SerializableKeybind
        {
            public KeyCode? primaryKey;
            public KeyCode[] secondaryKeys;
            public string[] data;
        }
    }
}
