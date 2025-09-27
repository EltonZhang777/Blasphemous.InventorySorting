using Blasphemous.ModdingAPI.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.InventorySorting.Extensions;

internal static class InputHandlerExtensions
{
    public static bool TryGetKeybinding(this InputHandler inputHandler, string keybindName, out KeyCode keyCode)
    {
        keyCode = KeyCode.None;
        Dictionary<string, KeyCode> keybindings = GetAllKeybindings(inputHandler);
        return keybindings != null && keybindings.TryGetValue(keybindName, out keyCode);
    }

    public static Dictionary<string, KeyCode> GetAllKeybindings(this InputHandler inputHandler)
    {
        return TraverseUtils.GetValue<Dictionary<string, KeyCode>>(inputHandler, "_keybindings");
    }
}
