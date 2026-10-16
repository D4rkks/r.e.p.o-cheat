using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // Spawns any shop item, valuable or cosmetic box the way the game does it (PunManager.SpawnItem,
    // ValuableDirector.SpawnCosmeticWorldObject, the /spawn debug command): Object.Instantiate in singleplayer,
    // PhotonNetwork.InstantiateRoomObject(resourcePath) as host.
    public static class ObjectSpawner
    {
        private enum Category { Items, Valuables, Cosmetics }

        private class Entry
        {
            public string Name;
            public string Detail;
            public PrefabRef Prefab;
        }

        private static readonly FieldInfo DollarValueOverrideField =
            typeof(ValuableObject).GetField("dollarValueOverride", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly string[] CategoryNames = { "Items", "Valuables", "Cosmetic boxes" };
        private static Category category = Category.Items;
        private static readonly Dictionary<Category, List<Entry>> cache = new Dictionary<Category, List<Entry>>();
        private static string search = "";
        private static string valueText = "";
        private static int selected = -1;
        private static Vector2 scroll;

        private static List<Entry> BuildList(Category cat)
        {
            var list = new List<Entry>();
            try
            {
                switch (cat)
                {
                    case Category.Items:
                        if (StatsManager.instance == null) break;
                        foreach (var item in StatsManager.instance.itemDictionary.Values)
                        {
                            if (item == null || item.disabled || item.prefab == null || !item.prefab.IsValid()) continue;
                            list.Add(new Entry { Name = item.itemName, Detail = item.itemType.ToString(), Prefab = item.prefab });
                        }
                        break;

                    case Category.Valuables:
                        if (RunManager.instance == null) break;
                        var seen = new HashSet<string>();
                        foreach (var level in RunManager.instance.levels)
                        {
                            if (level == null || level.ValuablePresets == null) continue;
                            foreach (var preset in level.ValuablePresets)
                            {
                                if (preset == null) continue;
                                AddValuables(list, seen, preset.tiny, "tiny");
                                AddValuables(list, seen, preset.small, "small");
                                AddValuables(list, seen, preset.medium, "medium");
                                AddValuables(list, seen, preset.big, "big");
                                AddValuables(list, seen, preset.wide, "wide");
                                AddValuables(list, seen, preset.tall, "tall");
                                AddValuables(list, seen, preset.veryTall, "very tall");
                            }
                        }
                        break;

                    case Category.Cosmetics:
                        if (ValuableDirector.instance == null) break;
                        foreach (var setup in ValuableDirector.instance.cosmeticWorldObjectSetups)
                        {
                            if (setup == null || setup.prefab == null || !setup.prefab.IsValid()) continue;
                            list.Add(new Entry { Name = CleanName(setup.prefab.PrefabName), Detail = setup.rarity.ToString(), Prefab = setup.prefab });
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Hax2.Log1($"ObjectSpawner: error listing {cat}: {e.Message}");
            }
            return list.OrderBy(x => x.Name).ToList();
        }

        private static void AddValuables(List<Entry> list, HashSet<string> seen, List<PrefabRef> prefabs, string size)
        {
            if (prefabs == null) return;
            foreach (var prefab in prefabs)
            {
                if (prefab == null || !prefab.IsValid() || !seen.Add(prefab.ResourcePath)) continue;
                list.Add(new Entry { Name = CleanName(prefab.PrefabName), Detail = size, Prefab = prefab });
            }
        }

        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            foreach (var prefix in new[] { "Valuable ", "Cosmetic World Object ", "Cosmetic " })
            {
                if (name.StartsWith(prefix)) return name.Substring(prefix.Length);
            }
            return name;
        }

        private static List<Entry> Current()
        {
            // Cosmetic setups only exist in a level, so retry while the list is empty
            if (!cache.TryGetValue(category, out var list) || list.Count == 0)
            {
                list = BuildList(category);
                cache[category] = list;
            }
            return list;
        }

        public static void DrawMenu(float x, float y)
        {
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                GUI.color = (int)category == i ? Color.white : Color.gray;
                if (GUI.Button(new Rect(x + i * 182, y, 176, 28), CategoryNames[i]))
                {
                    category = (Category)i;
                    selected = -1;
                    scroll = Vector2.zero;
                }
            }
            GUI.color = Color.white;

            GUI.Label(new Rect(x, y + 38, 60, 22), "Search:");
            search = GUI.TextField(new Rect(x + 60, y + 36, 360, 24), search, 40);
            if (GUI.Button(new Rect(x + 430, y + 36, 110, 24), "Refresh list"))
            {
                cache.Clear();
                selected = -1;
            }

            var all = Current();
            var shown = string.IsNullOrEmpty(search)
                ? all
                : all.Where(e => e.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (all.Count == 0)
            {
                GUI.Label(new Rect(x, y + 70, 540, 40), category == Category.Cosmetics
                    ? "No cosmetic boxes available: only listed while in a level."
                    : "List empty: load into the game first, then press Refresh list.");
            }

            scroll = GUI.BeginScrollView(new Rect(x, y + 70, 540, 330), scroll, new Rect(0, 0, 520, shown.Count * 30), false, true);
            for (int i = 0; i < shown.Count; i++)
            {
                bool isSelected = selected >= 0 && selected < all.Count && all[selected] == shown[i];
                GUI.color = isSelected ? Color.white : Color.gray;
                if (GUI.Button(new Rect(0, i * 30, 520, 26), $"{shown[i].Name}   [{shown[i].Detail}]"))
                {
                    selected = all.IndexOf(shown[i]);
                }
            }
            GUI.color = Color.white;
            GUI.EndScrollView();

            float bottom = y + 410;
            if (category == Category.Valuables)
            {
                GUI.Label(new Rect(x, bottom + 4, 170, 22), "Value $ (empty = random):");
                valueText = GUI.TextField(new Rect(x + 175, bottom + 2, 120, 26), valueText, 9);
                valueText = new string(valueText.Where(char.IsDigit).ToArray());
            }

            Entry entry = selected >= 0 && selected < all.Count ? all[selected] : null;
            if (GUI.Button(new Rect(x + 310, bottom, 230, 30), entry != null ? $"Spawn {entry.Name}" : "Select something"))
            {
                if (entry != null) Spawn(entry);
            }

            if (SemiFunc.IsMultiplayer() && !PhotonNetwork.IsMasterClient)
            {
                GUI.Label(new Rect(x, bottom + 36, 540, 22), "<color=yellow>Not host: spawned objects may not appear for others or be removed.</color>");
            }
        }

        private static void Spawn(Entry entry)
        {
            try
            {
                GameObject player = DebugCheats.GetLocalPlayer();
                if (player == null)
                {
                    Hax2.Log1("ObjectSpawner: local player not found.");
                    return;
                }
                Vector3 position = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 1f;
                Quaternion rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

                GameObject spawned;
                if (!SemiFunc.IsMultiplayer())
                    spawned = UnityEngine.Object.Instantiate(entry.Prefab.Prefab, position, rotation);
                else if (PhotonNetwork.IsMasterClient)
                    spawned = PhotonNetwork.InstantiateRoomObject(entry.Prefab.ResourcePath, position, rotation, 0);
                else
                    spawned = PhotonNetwork.Instantiate(entry.Prefab.ResourcePath, position, rotation, 0);

                if (spawned == null)
                {
                    Hax2.Log1($"ObjectSpawner: failed to spawn {entry.Name}.");
                    return;
                }

                if (category == Category.Valuables && int.TryParse(valueText, out int value) && value > 0)
                {
                    var valuable = spawned.GetComponent<ValuableObject>();
                    if (valuable != null && DollarValueOverrideField != null) DollarValueOverrideField.SetValue(valuable, value);
                }
                Hax2.Log1($"ObjectSpawner: spawned {entry.Name} ({entry.Prefab.ResourcePath})");
            }
            catch (Exception e)
            {
                Hax2.Log1($"ObjectSpawner: error spawning {entry.Name}: {e.Message}");
            }
        }
    }
}
