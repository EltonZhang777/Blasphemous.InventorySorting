//global using BlasLocManager = Framework.Managers.LocalizationManager;
global using BlasCollectibleItem = Framework.Inventory.CollectibleItem;
using BepInEx;
using Blasphemous.ModdingAPI;

namespace Blasphemous.InventorySorting;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "3.0.0")]
[BepInDependency("Blasphemous.Framework.UI", "0.2.0")]
internal class Main : BaseUnityPlugin
{
    public static InventorySorting InventorySorting { get; private set; }

    private void Start()
    {
        InventorySorting = new InventorySorting();
    }

    internal static string Localize(string key)
    {
        return InventorySorting.LocalizationHandler.Localize(key);
    }

    internal static void LogIfDebug(string message)
    {
#if DEBUG
        ModLog.Warn($"[DEBUG] {message}");
#endif
    }
}
