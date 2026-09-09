namespace CGPacksPlus.Patterns;

using BepInEx;
using GameConsole.pcon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
class PatternPackInfo
{
    /// <summary>
    /// The display name of the cybergrind pack
    /// The name of the folder the pack is stored in will be used instead if this is null.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// A path to a thumbnail of a pack, relative to the pack's root directory
    /// if this is null, a thumbnail will be generated for the pack at runtime
    /// </summary>
    public string ThumbnailPath = "";

    /// <summary> A list of paths to all enabled .cgp files, all relative to the pack's root directory </summary>
    public string[] EnabledPatterns = [];
}

public class PatternPack
{
    PatternPackInfo info = new();
    public static string PatternsPath => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");

    /// <summary>
    /// The path to the pattern pack (relative to the cybergrind patterns folder)
    /// </summary>
    public string Path = "";
    
    public HashSet<string> EnabledPatterns = [];

    public string PackName
    {
        get => info.Name;
        set => info.Name = value;
    }

    public string ThumbnailPath
    {
        get => info.ThumbnailPath;
        set => info.ThumbnailPath = value;
    }

    public static PatternPack FromJson(string jsonData)
    {
        var info = JsonUtility.FromJson<PatternPackInfo>(jsonData);
        if (info == null) return null;

        return new PatternPack
        {
            info = info,
            EnabledPatterns = info.EnabledPatterns.ToHashSet()
        };
    }

    public string ToJson()
    {
        info.EnabledPatterns = EnabledPatterns.ToArray();
        return JsonUtility.ToJson(info);
    }

    public string[] ListAllPatterns()
    {
        string[] absolutePaths = Directory.GetFiles(System.IO.Path.Join(PatternManager.PatternsPath, Path), "*.cgp", SearchOption.TopDirectoryOnly);
        return (from path in absolutePaths select System.IO.Path.GetFileName(path)).ToArray();
    }

    public bool HasValidThumbnail =>
        File.Exists(System.IO.Path.Join(PatternManager.PatternsPath, Path, ThumbnailPath));
}
