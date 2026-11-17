using GorillaLibrary.Extensions;
using GorillaNameExtension.Models;
using GorillaTagScripts;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

// i'll throw some funny little comments around
// no generative ai was used to write this code

namespace GorillaNameExtension.Behaviours;

internal class NameTag : MonoBehaviour
{
    public bool Initialized => _initialized;
    public Color InnerColour => _inner.color;
    public Color OuterColour => _outer.color;
    public TagConfig CurrentConfig => _latestConfig;
    public int CurrentPriority => _latestPriority;

    public VRRig Rig;

    private bool _initialized;

    private TMP_Text _outer, _inner, _inspiration;

    private Dictionary<int, TagConfig> _configuration;

    // the cache for colours that are essensially only worked on within this script
    // so think of any colours that are configured by the base game
    // here (internally) is where new behaviour for them is overwritten
    private Dictionary<Color, TagConfig> _internalColourCache;

    // outlined colour are made by adjusting the hue/saturation/value of what they're given
    // see AutoOutlineColour for what i mean. i might end up modifying it in the future, the function
    // it might just be inexpensive to do that each time but still, i'm deciding to cache it
    private readonly Dictionary<Color, Color> _glowCache = [];

    private List<PropertyInfo> _properties;

    private TagConfig _latestConfig;

    private int _latestPriority = -1;

    internal void Start()
    {
        if (gameObject.hideFlags == HideFlags.HideAndDontSave) return;

        _inspiration = transform.parent.GetComponent<TMP_Text>();
        if (_inspiration.IsObjectNull())
        {
            Destroy(gameObject);
            return;
        }

        _outer = transform.GetChild(0).GetComponent<TMP_Text>();
        _inner = transform.GetChild(1).GetComponent<TMP_Text>();

        _internalColourCache = new()
        {
            {
                Color.white,
                new()
                {
                    Colour = Color.white,
                    GlowAmount = 0f
                }
            }
        };

        Color vimColour = SubscriptionManager.SUBSCRIBER_NAME_COLOR;
        _internalColourCache.Add(vimColour, new()
        {
            Colour = vimColour,
            GlowAmount = 0.35f
        });

        _configuration = new()
        {
            {
                Priority.Last,
                _internalColourCache[Color.white]
            }
        };

        _properties = [.. typeof(TMP_Text)
            .GetProperties(AccessTools.allDeclared)
            .Where(property => property.CanWrite)
            .Where(property => property.PropertyType == typeof(string) || property.PropertyType == typeof(float) || property.PropertyType == typeof(int) || property.PropertyType == typeof(bool) || property.PropertyType.IsEnum)
            .Where(property => property.PropertyType != typeof(float) || property.Name.StartsWith("fontSize") || (property.Name.EndsWith("Spacing") && !property.Name.StartsWith("character")))];

        _initialized = true;

        UpdateName();
        UpdateColour();
    }

    internal void OnEnable()
    {
        if (!_initialized) return;
        UpdateName();
        UpdateColour();
    }

    internal void OnDisable()
    {
        if (!_initialized) return;
        var baseColour = _configuration[Priority.Last];
        _configuration.Clear();
        _configuration[Priority.Last] = baseColour;
    }

    internal void UpdateName()
    {
        if (!_initialized) return;

        foreach (var property in _properties)
        {
            var value = property.GetValue(_inspiration);
            property.SetValue(_inner, value);
            property.SetValue(_outer, value);
        }
    }

    internal void SetConfig(int priority, TagConfig config)
    {
        if (priority < 1 || priority > Priority.First) throw new IndexOutOfRangeException($"Priority of name colour should be within 1 and {Priority.First}");

        if (_configuration.ContainsKey(priority))
        {
            var oldConfig = _configuration[priority];
            _configuration[priority] = config;

            var fields = typeof(TagConfig).GetFields(AccessTools.all); // TODO: cache this
            if (fields.Any(field => field.GetValue(oldConfig) == field.GetValue(config))) UpdateColour();
        }
        else
        {
            _configuration.Add(priority, config);
            UpdateColour();
        }
    }

    internal void RemoveConfig(int priority)
    {
        if (priority < 1 || priority > Priority.First) throw new IndexOutOfRangeException($"Priority of name colour should be within 1 and {Priority.First}");

        if (_configuration.ContainsKey(priority)) _configuration.Remove(priority);

        UpdateColour();
    }

    internal void UpdateBaseColour(Color inspoColour)
    {
        // Plugin.Logger.LogMessage($"Base colour for {Rig.rigContainer.Creator.GetPlayerRef()} set to {inspoColour}");

        if (!_internalColourCache.TryGetValue(inspoColour, out TagConfig colour))
        {
            colour = new()
            {
                Colour = inspoColour,
                GlowAmount = 0f
            };
            _internalColourCache.Add(inspoColour, colour);
        }

        if (_configuration[Priority.Last] != colour)
        {
            _configuration[Priority.Last] = colour;
            UpdateColour();
        }
    }

    private void UpdateColour()
    {
        if (!_initialized) return;

        var orderedTags = _configuration.OrderByDescending(kvp => kvp.Key).ToArray();
        var latestTagWithQueue = orderedTags.First();
        TagConfig latestTag = latestTagWithQueue.Value;

        _latestConfig = latestTag;
        _latestPriority = latestTagWithQueue.Key;

        FinalColour(_latestConfig, out Color finalInner, out Color finalOuter);
        for (int i = 1; i < orderedTags.Length; i++)
        {
            var bleed = Mathf.Clamp01(orderedTags[i - 1].Value.Bleed);
            if (Mathf.Approximately(bleed, 0)) break;
            FinalColour(orderedTags[i].Value, out Color inner, out Color outer);
            finalInner = Color.Lerp(finalInner, inner, bleed);
            finalOuter = Color.Lerp(finalOuter, outer, bleed);
        }

        _inner.color = finalInner;
        _outer.color = finalOuter;
    }

    private void FinalColour(TagConfig tagColour, out Color inner, out Color outer)
    {
        inner = tagColour.Colour;
        outer = tagColour.Border ?? ColourWithGlow(tagColour.Colour, tagColour.GlowAmount);
    }

    private Color ColourWithGlow(Color colour, float amount)
    {
        if (!_glowCache.ContainsKey(colour))
        {
            Color.RGBToHSV(colour, out float H, out float S, out float V);
            _glowCache.Add(colour, Color.HSVToRGB(H, S, V * Mathf.Clamp01(amount)));
        }

        return _glowCache[colour];
    }
}
