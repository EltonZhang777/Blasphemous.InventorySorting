using BepInEx;
using System;
using System.Linq;

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

    public static T GetNextEnumValue<T>(T currentValue, int stepLength = 1) where T : Enum
    {
        T[] values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        int currentIndex = Array.IndexOf(values, currentValue);

        // Calculate the new index, handling out-of-bounds cases
        int newIndex = (currentIndex + stepLength) % values.Length;
        if (newIndex < 0)
        {
            newIndex += values.Length;
        }

        return values[newIndex];
    }
}
