using BepInEx;

namespace Blasphemous.InventorySorting
{
    [BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
    [BepInDependency("Blasphemous.ModdingAPI", "0.1.0")]
    internal class Main : BaseUnityPlugin
    {
        public static InventorySorting InventorySorting { get; private set; }

        private void Start()
        {
            InventorySorting = new InventorySorting();
        }
    }
}
