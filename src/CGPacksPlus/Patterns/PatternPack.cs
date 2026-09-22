namespace CGPacksPlus.Patterns;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;


[JsonObject(MemberSerialization.OptOut)]
public class PatternPack
{

    [JsonIgnore]
    private static plog.Logger _log = new($"{Plugin.PLUGIN_SHORTNAME}.{nameof(PatternPack)}");

    private static string _patternsPath => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");

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
    [JsonIgnore]
    public string Path = "";

    public static PatternPack FromJson(string jsonData)
    {
        var result = JsonConvert.DeserializeObject<PatternPack>(jsonData);
        if (result == null)
        {
            _log.Error($"[{nameof(FromJson)}] Failed to load pattern pack from json provided json data");
            return null;
        }

        result.EnabledPatterns = result.EnabledPatterns.Distinct().ToList();
        return result;
    }

    public string ToJson() => JsonConvert.SerializeObject(this);

    public string[] ListAllPatterns()
    {
        string[] absolutePaths = Directory.GetFiles(System.IO.Path.Join(_patternsPath, Path), "*.cgp", SearchOption.TopDirectoryOnly);
        return (from path in absolutePaths select System.IO.Path.GetFileName(path)).ToArray();
    }

    public bool HasValidThumbnail =>
        File.Exists(System.IO.Path.Join(_patternsPath, Path, ThumbnailPath));
}
