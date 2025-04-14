using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnityHelper.ModSystem
{
    public class ModLoader
    {
        private readonly ModScanner _modScanner;
        
        public ModLoader()
        {
            _modScanner = new ModScanner();
            LoadMods();
        }

        public Dictionary<string, IMod> LoadedMods { get; } = new();

        public void LoadMods()
        {
            var modPath = Path.Combine(Application.dataPath, "../Mods");

            if (!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);

            foreach (var dll in Directory.GetFiles(modPath, "*.dll"))
            {
                if (!_modScanner.IsModSafe(dll))
                {
                    Debug.LogWarning($"Mod {Path.GetFileName(dll)} blocked due to unsafe usage.");
                    continue;
                }

                var assembly = Assembly.LoadFile(dll);
                foreach (var type in assembly.GetTypes())
                {
                    if (!typeof(IMod).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract) continue;
                    var mod = (IMod)Activator.CreateInstance(type);
                    LoadedMods.Add(mod.ModId(), mod);
                    Debug.Log($"Mod loaded: {mod.ModId()}");
                }
            }
        }

        public IMod GetMod(string modId)
        {
            if (LoadedMods.TryGetValue(modId, out var mod)) return mod;
            Debug.LogWarning($"Mod {modId} is not loaded.");
            return null;
        }
    }
}