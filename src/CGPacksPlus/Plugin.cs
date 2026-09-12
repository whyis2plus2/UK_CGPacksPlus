namespace CGPacksPlus;

using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static BepInEx.BepInDependency;

[BepInPlugin(PLUGIN_FULLNAME, PLUGIN_SHORTNAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_FULLNAME = "whyis2plus2.ULTRAKILL.CGPacksPlus";
    public const string PLUGIN_SHORTNAME = "CGPacksPlus";
    public const string PLUGIN_VERSION = "0.0.1";

    /// <summary> We need to have an instance of this in order to do patches </summary>
    private readonly Harmony harmony = new(PLUGIN_SHORTNAME);

    void Awake()
    {
        harmony.PatchAll(typeof(Patches.CustomPatternsPatch));
        Logger.LogInfo($"Loaded {PLUGIN_SHORTNAME}");
    }
}
