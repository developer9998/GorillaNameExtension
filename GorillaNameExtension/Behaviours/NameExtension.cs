using GorillaNameExtension.Models;
using GorillaNameExtension.Patches;
using System;
using TMPro;
using UnityEngine;

namespace GorillaNameExtension.Behaviours;

[RequireComponent(typeof(VRRig)), DisallowMultipleComponent]
internal class NameExtension : MonoBehaviour
{
    internal NameTag Extension => _primaryExtension;

    // provided in plugin
    internal GameObject Prefab;

    private NameTag _primaryExtension;

    internal void Start()
    {
        VRRig rig = GetComponent<VRRig>();
        TMP_Text text = rig.playerText1;
        text.GetComponent<MeshRenderer>().forceRenderingOff = true;
        text.GetComponent<MeshRenderer>().enabled = false;

        GameObject instance = Instantiate(Prefab);
        instance.transform.parent = text.transform;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localEulerAngles = Vector3.zero;
        instance.transform.localScale = Vector3.one;

        _primaryExtension = instance.AddComponent<NameTag>();
        _primaryExtension.Rig = rig;
        rig.OnNameChanged += _ => _primaryExtension.UpdateName();

        TextPropertyOverridePatch.AddOverride(text, GetTagColour, SetTagColour);
    }

    public Color GetTagColour()
        => (_primaryExtension != null && _primaryExtension.Initialized) ? _primaryExtension.InnerColour : Color.white;

    internal Color SetTagColour(Color baseColour)
    {
        if (_primaryExtension == null || !_primaryExtension.Initialized) return baseColour;

        if (GetTagColour() != baseColour) _primaryExtension.UpdateBaseColour(baseColour);
        return GetTagColour();
    }

    internal void SetConfig(int priority, TagConfig colour)
    {
        try
        {
            _primaryExtension.SetConfig(priority, colour);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogFatal("Config failed to set");
            Plugin.Logger.LogError(ex);
        }
    }

    internal void RemoveConfig(int priority)
    {
        try
        {
            _primaryExtension.RemoveConfig(priority);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogFatal("Config failed to remove");
            Plugin.Logger.LogError(ex);
        }
    }
}
