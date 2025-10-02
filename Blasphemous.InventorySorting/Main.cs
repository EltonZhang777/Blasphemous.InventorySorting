//global using BlasLocManager = Framework.Managers.LocalizationManager;
global using BlasCollectibleItem = Framework.Inventory.CollectibleItem;
using BepInEx;

namespace Blasphemous.InventorySorting;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.0")]
internal class Main : BaseUnityPlugin
{
    public static InventorySorting InventorySorting { get; private set; }

    private void Start()
    {
        InventorySorting = new InventorySorting();
    }
}
