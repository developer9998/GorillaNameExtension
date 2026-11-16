using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GorillaNameExtension.Patches;

[HarmonyPatch(typeof(TMP_Text)), HarmonyWrapSafe]
internal class TextPropertyOverridePatch
{
    private static readonly Dictionary<TMP_Text, (Func<Color> overrideIn, Func<Color, Color> overrideOut)> behaviourOverrides = [];

    [HarmonyPatch(nameof(TMP_Text.color), MethodType.Getter), HarmonyPrefix]
    internal static bool GetPatch(TMP_Text __instance, ref Color __result)
    {
        if (behaviourOverrides.TryGetValue(__instance, out var tuple))
        {
            __result = tuple.overrideIn();
            return false;
        }

        return true;
    }

    [HarmonyPatch(nameof(TMP_Text.color), MethodType.Setter), HarmonyPrefix]
    internal static bool SetPatch(TMP_Text __instance, ref Color value)
    {
        if (behaviourOverrides.TryGetValue(__instance, out var tuple))
        {
            value = tuple.overrideOut(value);
            return false;
        }

        return true;
    }

    public static void AddOverride(TMP_Text text, Func<Color> overrideIn, Func<Color, Color> overrideOut)
    {
        if (behaviourOverrides.ContainsKey(text)) behaviourOverrides[text] = (overrideIn, overrideOut);
        else behaviourOverrides.Add(text, (overrideIn, overrideOut));
    }

    public static void RemoveOverride(TMP_Text text)
    {
        if (behaviourOverrides.ContainsKey(text)) behaviourOverrides.Remove(text);
    }
}
