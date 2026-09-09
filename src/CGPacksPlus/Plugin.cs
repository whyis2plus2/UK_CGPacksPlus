namespace CGPacksPlus;

using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static BepInEx.BepInDependency;

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity;
using UnityEngine.AddressableAssets;
using GameConsole;

[BepInPlugin(PLUGIN_FULLNAME, PLUGIN_SHORTNAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_FULLNAME = "whyis2plus2.ULTRAKILL.CGPacksPlus";
    public const string PLUGIN_SHORTNAME = "CGPacksPlus";
    public const string PLUGIN_VERSION = "0.0.1";

    /// <summary> The current instance of the plugin, accessable by all parts of the code </summary>
    public static Plugin Instance;

    /// <summary> Public version of the Logger so that the rest of the mod can acess it </summary>
    public static plog.Logger Log;

    /// <summary> We need to have an instance of this in order to do patches </summary>
    public readonly Harmony harmony = new(PLUGIN_FULLNAME);

    void Awake()
    {
        Instance = this;
        Log = new(PLUGIN_SHORTNAME);

        harmony.PatchAll(typeof(Patches.CustomPatternsPatch));
        Logger.LogInfo($"Loaded {PLUGIN_SHORTNAME}");
    }
}
