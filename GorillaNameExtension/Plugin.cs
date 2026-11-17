using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GorillaLibrary;
using GorillaLibrary.Models;
using GorillaLibrary.Utilities;
using GorillaNameExtension.Behaviours;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GorillaNameExtension;

[BepInPlugin("dev.gorillanameextension", "GorillaNameExtension", "1.0.0")]
[BepInDependency("dev.gorillalibrary")]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    public static new ConfigFile Config;

    private GameObject _extensionPrefab;

    public void Awake()
    {
        Logger = base.Logger;
        Config = base.Config;

        Events.Core.OnGameInitialized += Initialize;
        Events.Rig.OnRigAdded += SetupRig;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    public void Initialize()
    {
        gameObject.AddComponent<SpecialtyManager>();

        AssetLoaderSync assetLoader = new(Assembly.GetExecutingAssembly(), "GorillaNameExtension.Content.betterfontbundle");
        _extensionPrefab = assetLoader.LoadAsset<GameObject>("BetterFontContainer");

        SetupRig(RigUtility.LocalRig, null);
    }

    public void SetupRig(VRRig rig, NetPlayer owner)
    {
        if (rig.GetComponent<NameExtension>()) return;
        var component = rig.gameObject.AddComponent<NameExtension>();
        component.Prefab = _extensionPrefab;
    }
}
