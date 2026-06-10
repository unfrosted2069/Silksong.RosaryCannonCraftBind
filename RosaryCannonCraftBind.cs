using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RosaryCannonCraftBind;

[BepInAutoPlugin(id: "io.github.unfrosted2069.RosaryCannonCraftBind")]
[HarmonyPatch]
public partial class RosaryCannonCraftBind : BaseUnityPlugin
{
    public static ManualLogSource Log;
    private void Awake()
    {
        // Put your initialization logic here
        Harmony harmony = new(Id);
        harmony.PatchAll();
        Log = ((RosaryCannonCraftBind)this).Logger;
        Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ToolItemManager), "TryReplenishTools")]
    private static void TryReplenishTools_RosaryCannon(ToolItemManager __instance, bool doReplenish, ToolItemManager.ReplenishMethod method)
    {
        if (doReplenish && method == ToolItemManager.ReplenishMethod.QuickCraft)
        {
            ToolItem rosary_cannon = ToolItemManager.GetToolByName("Rosary Cannon");
            bool DoesHaveRosaryCannon = ToolItemManager.GetCurrentEquippedTools().Contains(rosary_cannon);
            if (DoesHaveRosaryCannon)
            {
                int amount_left = rosary_cannon.SavedData.AmountLeft;
                int storage_amount = ToolItemManager.GetToolStorageAmount(rosary_cannon);
                int missing = storage_amount - amount_left;
                if (missing <= 0)
                {
                    return;
                }

                int to_get = (missing < 60) ? missing : 60;

                CollectableItem rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Small");
                CollectableItem frayed_rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Frayed");
                if (rosary_string.CollectedAmount > 0)
                {
                    ToolItemsData.Data savedData = rosary_cannon.SavedData;

                    if (to_get > 60)
                    {
                        savedData.AmountLeft += 60;
                    }
                    else
                    {
                        savedData.AmountLeft += to_get;
                        int unloaded_rosaries = 60 - to_get;
                        if (!PlayerData.instance.HasStoredMemoryState)
                        {
                            FlingUtils.SpawnAndFling(new FlingUtils.Config
                            {
                                Prefab = GlobalSettings.Gameplay.SmallGeoPrefab,
                                AmountMin = unloaded_rosaries,
                                AmountMax = unloaded_rosaries,
                                SpeedMin = 15f,
                                SpeedMax = 25f,
                                AngleMin = -40f,
                                AngleMax = 220f
                            }, HeroController._instance.transform, Vector3.zero);
                        }
                    }

                    rosary_string.Take();
                    PlayerData.instance.SetToolData(rosary_cannon.name, savedData);
                    ToolItemManager.ReportAllBoundAttackToolsUpdated();
                    ToolItemManager.SendEquippedChangedEvent(true);
                }
                else if (frayed_rosary_string.CollectedAmount > 0)
                {
                    ToolItemsData.Data savedData = rosary_cannon.SavedData;

                    int fr = 15 + Random.Range(0, 16); // rosaries got from frayed rosary string
                    if (to_get > fr)
                    {
                        savedData.AmountLeft += fr;
                        if (!PlayerData.instance.HasStoredMemoryState)
                        {
                            FlingUtils.SpawnAndFling(new FlingUtils.Config
                            {
                                Prefab = GlobalSettings.Gameplay.SmallGeoPrefab,
                                AmountMin = 30 - fr,
                                AmountMax = 30 - fr,
                                SpeedMin = 15f,
                                SpeedMax = 25f,
                                AngleMin = -40f,
                                AngleMax = 220f
                            }, HeroController._instance.transform, Vector3.zero);
                        }
                    }
                    else
                    {
                        savedData.AmountLeft += to_get;
                        int unloaded_rosaries = fr - to_get;
                        if (!PlayerData.instance.HasStoredMemoryState)
                        {
                            FlingUtils.SpawnAndFling(new FlingUtils.Config
                            {
                                Prefab = GlobalSettings.Gameplay.SmallGeoPrefab,
                                AmountMin = unloaded_rosaries,
                                AmountMax = unloaded_rosaries,
                                SpeedMin = 15f,
                                SpeedMax = 25f,
                                AngleMin = -40f,
                                AngleMax = 220f
                            }, HeroController._instance.transform, Vector3.zero);
                        }
                    }

                    frayed_rosary_string.Take();
                    PlayerData.instance.SetToolData(rosary_cannon.name, savedData);
                    ToolItemManager.ReportAllBoundAttackToolsUpdated();
                    ToolItemManager.SendEquippedChangedEvent(true);
                }
            }
        }
    }

    public static int prememorystate_rosarystring_amount;
    public static int prememorystate_frayed_rosary_string_amount;

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), "EnteredNewMapZone")]
    private static void EnteredNewMapZone_RememberRS(GameManager __instance, GlobalEnums.MapZone previousMapZone, GlobalEnums.MapZone currentMapZone, bool forcedNotMemory)
    {
        CollectableItem rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Small");
        CollectableItem frayed_rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Frayed");
        if (!forcedNotMemory && GameManager.IsMemoryScene(currentMapZone))
        {
            if (!PlayerData._instance.HasStoredMemoryState)
            {
                prememorystate_rosarystring_amount = rosary_string.GetSavedAmount();
                prememorystate_frayed_rosary_string_amount = frayed_rosary_string.GetSavedAmount();
            }
        }
        else if (PlayerData._instance.HasStoredMemoryState && GameManager.IsMemoryScene(previousMapZone))
        {
            rosary_string.AddAmount(prememorystate_rosarystring_amount - rosary_string.GetSavedAmount());
            frayed_rosary_string.AddAmount(prememorystate_frayed_rosary_string_amount - frayed_rosary_string.GetSavedAmount());
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), "LoadedFromMenu")]
    private static void LoadedFromMenu_RememberRS(GameManager __instance)
    {
        CollectableItem rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Small");
        CollectableItem frayed_rosary_string = CollectableItemManager.GetItemByName("Rosary_Set_Frayed");
        if (PlayerData._instance.HasStoredMemoryState)
        {
            rosary_string.AddAmount(prememorystate_rosarystring_amount - rosary_string.GetSavedAmount());
            frayed_rosary_string.AddAmount(prememorystate_frayed_rosary_string_amount - frayed_rosary_string.GetSavedAmount());
        }
    }
}