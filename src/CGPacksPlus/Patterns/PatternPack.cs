namespace CGPacksPlus.Patterns;

using BepInEx;
using GameConsole.pcon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[Serializable]
public class PatternPack
{
    private static string patternsPath => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");

    /// <summary>
    /// The display name of the cybergrind pack
    /// The name of the folder the pack is stored in will be used instead if this is null.
    /// </summary>
    public string DisplayName = "";

    /// <summary>
    /// A path to a thumbnail of a pack, relative to the pack's root directory
    /// if this is null, a thumbnail will be generated for the pack at runtime
    /// </summary>
    public string ThumbnailPath = "";

    /// <summary> A list of paths to all enabled .cgp files, all relative to the pack's root directory </summary>
    public List<string> EnabledPatterns = [];

    /// <summary>
    /// The path to the pattern pack (relative to the cybergrind patterns folder)
    /// </summary>
    [NonSerialized]
    public string Path = "";

    public static PatternPack FromJson(string jsonData)
    {
        var result = JsonUtility.FromJson<PatternPack>(jsonData);
        result.EnabledPatterns = result.EnabledPatterns.Distinct().ToList();
        return result;
    }

    public string ToJson() => JsonUtility.ToJson(this);

    public string[] ListAllPatterns()
    {
        string[] absolutePaths = Directory.GetFiles(System.IO.Path.Join(patternsPath, Path), "*.cgp", SearchOption.TopDirectoryOnly);
        return (from path in absolutePaths select System.IO.Path.GetFileName(path)).ToArray();
    }

    public bool HasValidThumbnail =>
        File.Exists(System.IO.Path.Join(PatternManager.PatternsPath, Path, ThumbnailPath));
}
