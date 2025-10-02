using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Blasphemous.InventorySorting.Extensions;

internal static class MiscExtensions
{
    internal static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= list.Count || newIndex < 0 || newIndex >= list.Count)
            throw new ArgumentOutOfRangeException();

        if (oldIndex == newIndex)
            return;

        var item = list[oldIndex];
        list.RemoveAt(oldIndex);
        //if (newIndex > oldIndex) 
        //    newIndex--; 
        list.Insert(newIndex, item);
    }

    public static T GetNextEnumValue<T>(this T currentValue, int stepLength = 1) where T : Enum
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

    public static string ReplaceWords(this string str, Dictionary<string, string> targetsToReplacements)
    {
        string pattern = string.Join("|", targetsToReplacements.Keys.ToArray());
        str = Regex.Replace(str, pattern, match =>
        {
            return targetsToReplacements[match.Value];
        });
        return str;
    }
}
