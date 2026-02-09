using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;

namespace Blasphemous.InventorySorting.HarmonyPatches;

[HarmonyPatch(typeof(NewInventoryWidget))]
class NewInventoryWidget_TriggerOnToggleEvent_Patch
{
    [HarmonyPatch("Show")]
    [HarmonyPostfix]
    public static void TriggerOnToggleEvent(
        bool p_active)
    {
        Main.InventorySorting.eventsHandler.InventoryToggle(p_active);
    }
}