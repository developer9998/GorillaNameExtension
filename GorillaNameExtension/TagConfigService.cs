using GorillaNameExtension.Behaviours;
using GorillaNameExtension.Models;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaNameExtension;

public static class TagConfigService
{
    private static readonly Dictionary<(Color colour, float glowAmount, float bleedAmount), TagConfig> _glowCache = [];

    private static readonly Dictionary<(Color colour, Color border, float bleedAmount), TagConfig> _borderCache = [];

    public static void SetConfig(VRRig rig, Color colour, int priority = Priority.Normal)
        => SetConfig(rig, colour, 0f, 0f, priority);

    public static void SetConfigWithGlow(VRRig rig, Color colour, float glowAmount = 0.25f, int priority = Priority.Normal)
        => SetConfig(rig, colour, glowAmount, 0f, priority);

    public static void SetConfigWithBleed(VRRig rig, Color colour, float bleedAmount = 0f, int priority = Priority.Normal)
        => SetConfig(rig, colour, 0f, bleedAmount, priority);

    public static void SetConfig(VRRig rig, Color colour, float glowAmount = 0.25f, float bleedAmount = 0f, int priority = Priority.Normal)
    {
        glowAmount = Mathf.RoundToInt(Mathf.Clamp01(glowAmount) * 100f) / 100f;
        bleedAmount = Mathf.FloorToInt(Mathf.Clamp01(bleedAmount) * 100f) / 100f;

        if (!_glowCache.TryGetValue((colour, glowAmount, bleedAmount), out TagConfig simpleColour))
        {
            simpleColour = new()
            {
                Colour = colour,
                GlowAmount = glowAmount,
                Bleed = bleedAmount
            };
            _glowCache.Add((colour, glowAmount, bleedAmount), simpleColour);
        }

        rig.GetComponent<NameExtension>()?.SetConfig(priority, simpleColour);
    }

    public static void SetConfig(VRRig rig, Color colour, Color border, int priority = Priority.Normal)
        => SetConfig(rig, colour, border, 0f, priority);

    public static void SetConfig(VRRig rig, Color colour, Color border, float bleedAmount = 0f, int priority = Priority.Normal)
    {
        bleedAmount = Mathf.FloorToInt(Mathf.Clamp01(bleedAmount) * 100f) / 100f;

        if (!_borderCache.TryGetValue((colour, border, bleedAmount), out TagConfig complexColour))
        {
            complexColour = new()
            {
                Colour = colour,
                Border = border,
                Bleed = bleedAmount
            };
            _borderCache.Add((colour, border, bleedAmount), complexColour);
        }

        rig.GetComponent<NameExtension>()?.SetConfig(priority, complexColour);
    }

    public static void RemoveConfig(VRRig rig, int priority = Priority.Normal)
    {
        rig.GetComponent<NameExtension>()?.RemoveConfig(priority);
    }
}
