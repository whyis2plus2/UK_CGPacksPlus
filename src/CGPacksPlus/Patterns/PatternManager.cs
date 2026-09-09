namespace CGPacksPlus.Patterns;

using BepInEx;
using CGPacksPlus.Patches;
using GameConsole.pcon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PatternManager: MonoSingleton<PatternManager>
{
    public static string PatternsPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");

    private Dictionary<string, ArenaPattern> patternCache = [];
    private Dictionary<string, PatternPack> patternPackCache = [];

    /// <summary>
    /// All enabled paterns that are stored in the root directory of the patterns folder.
    /// patterns not stored in the root directory are handled by their respective pattern packs
    /// </summary>
    private Dictionary<string, ArenaPattern> _enabledPatterns = [];
    public ArenaPattern[] EnabledPatterns => _enabledPatterns.Values?.ToArray() ?? [];
    public HashSet<string> EnabledPatternsPaths => _enabledPatterns.Keys?.ToHashSet() ?? [];

    public void EnablePattern(PatternPack parent, string patternName)
    {
        if (parent == null) _enabledPatterns.TryAdd(patternName, LoadPattern(patternName));
        else
        {
            _enabledPatterns.TryAdd(Path.Join(parent.Path, patternName), LoadPattern(Path.Join(parent.Path, patternName)));            
            parent.EnabledPatterns.Add(patternName);
            File.WriteAllText(Path.Join(PatternsPath, parent.Path, "cgpack.json"), parent.ToJson());
            EndlessGrid.Instance.customPatterns = EnabledPatterns;
        }
    }

    public void DisablePattern(PatternPack parent, string patternName)
    {
        if (parent == null) _enabledPatterns.Remove(patternName);
        else
        {
            _enabledPatterns.Remove(Path.Join(parent.Path, patternName));
            parent.EnabledPatterns.Remove(patternName);
            File.WriteAllText(Path.Join(PatternsPath, parent.Path, "cgpack.json"), parent.ToJson());
            EndlessGrid.Instance.customPatterns = EnabledPatterns;
        }
    }

    public void TogglePattern(PatternPack parent, string patternName)
    {
        string key = (parent == null)? patternName : Path.Join(parent.Path, patternName);
        if (_enabledPatterns.Keys.Contains(key)) DisablePattern(parent, key);
        else EnablePattern(parent, key);
    }

    public bool GeneratePatternPreview(ArenaPattern pattern, Vector2Int offset, ref Texture2D target) =>
        CustomPatternsPatch.CustomPatternsInstance.GeneratePatternPreview(pattern, offset, ref target);

    /// <summary>
    /// Generate a preview of a pattern pack.
    /// If the pack has a valid thumbnailPath, the thumbnail will be used instead of an image being generated
    /// </summary>
    /// <param name="pack">The pattern pack in question.</param>
    /// <param name="target">The texture to be written to.</param>
    /// <returns></returns>
    public bool GeneratePackPreview(PatternPack pack, ref Texture2D target)
    {
        if (pack == null) return false;

        string[] allPatterns = pack.ListAllPatterns();

        if (pack.HasValidThumbnail)
        {
            // load the image into a new texture for ease of implementing the padding algorithm
            Texture2D image = new(1, 1);
            image.LoadImage(File.ReadAllBytes(Path.Join(PatternsPath, pack.Path, pack.ThumbnailPath)));

            if (image.width == image.height)
            {
                target = image;
                return true;
            }

            // if the image isn't square, add padding to the bottom
            // yes, that's what the following code does
            int textureSize = Math.Max(image.width, image.height);
            target.Reinitialize(textureSize, textureSize);

            List<Color> pixels = [];
            pixels.AddRange(Enumerable.Repeat(Color.black, (textureSize * textureSize) - (image.width * image.height)));
            pixels.AddRange(image.GetPixels());
            target.SetPixels(pixels.ToArray());
            return true;
        }

        target.Reinitialize(48, 48);
        target.SetPixels(Enumerable.Repeat(Color.black, 2304).ToArray());

        for (int i = 0; i < allPatterns.Length && i < 6; ++i)
        {
            ArenaPattern pattern = LoadPattern(Path.Join(pack.Path, allPatterns[i]));
            Vector2Int offset = new(i % 3, (i > 2)? 1 : 0);
            bool result = GeneratePatternPreview(pattern, offset, ref target);
            if (!result) return false;
        }

        return true;
    }

    public void SaveEnabledPatterns()
    {
        string activePatternsJson = PrefsManager.Instance.GetStringLocal("cyberGrind.enabledPatterns");
        var enabledPatternPacks = JsonUtility.FromJson<ActivePatterns>(activePatternsJson)?.enabledPatternPacks;

        ActivePatterns activePatterns = new()   
        {
            enabledPatterns = _enabledPatterns.Keys.SkipWhile((key) => key.Contains(Path.DirectorySeparatorChar)).ToArray(),
            enabledPatternPacks = enabledPatternPacks
        };

        PrefsManager.Instance.SetStringLocal("cyberGrind.enabledPatterns", JsonUtility.ToJson(activePatterns));
    }

    public void LoadEnabledPatterns()
    {
        string activePatternsJson = PrefsManager.Instance.GetStringLocal("cyberGrind.enabledPatterns");
        if (string.IsNullOrEmpty(activePatternsJson)) return;

        var activePatterns = JsonUtility.FromJson<ActivePatterns>(activePatternsJson);
        foreach (var path in activePatterns.enabledPatterns ?? [])
        {
            if (path.Contains(Path.DirectorySeparatorChar)) return;
            var pattern = LoadPattern(path);
            if (pattern) EnablePattern(null, path);
            else Plugin.Log.Warning($"{nameof(LoadEnabledPatterns)}: Failed to load pattern \"{path}\"");
        }
        
        EndlessGrid.Instance.customPatterns = EnabledPatterns;
    }
 
    public ArenaPattern LoadPattern(string relativePath) =>
        LoadPatternAbsolute(Path.Combine(PatternsPath, relativePath));

    public PatternPack LoadPack(string relativePath) =>
        LoadPackAbsolute(Path.Combine(PatternsPath, relativePath));
    
    public ArenaPattern LoadPatternAbsolute(string absolutePath)
    {
        if (!File.Exists(absolutePath)) return null;

        string relativePath = Path.GetRelativePath(PatternsPath, absolutePath);
        if (patternCache.ContainsKey(relativePath)) return patternCache[relativePath];

        var pattern = CustomPatternsPatch.CustomPatternsInstance.LoadPattern(relativePath);
        if (pattern == null)
        {
            Plugin.Log.Warning($"PatternManager.LoadPatternAbsolute: Failed to load pattern from path '{absolutePath}'");
            return null;
        }

        patternCache[relativePath] = pattern;
        return pattern;
    }

    public PatternPack LoadPackAbsolute(string absolutePath)
    {
        if (!Directory.Exists(absolutePath)) return null;

        string relativePath = Path.GetRelativePath(PatternsPath, absolutePath);
        if (patternPackCache.ContainsKey(relativePath)) return patternPackCache[relativePath];

        PatternPack result = null;

        if (!File.Exists(Path.Join(absolutePath, "cgpack.json")))
        {
            result = new();
            result.Path = result.PackName = relativePath;

            patternPackCache[relativePath] = result;
            return result;
        }

        result = PatternPack.FromJson(File.ReadAllText(Path.Join(absolutePath, "cgpack.json")));
        result.Path = relativePath;
        patternPackCache[relativePath] = result;

        // add the pack's patterns to the cache
        foreach (string patternPath in result.ListAllPatterns())
        {
            var pattern = LoadPatternAbsolute(Path.Join(absolutePath, patternPath));
            if (result.EnabledPatterns.Contains(patternPath)) EnablePattern(result, patternPath);
        }
        
        return result;
    }
}
