namespace CGPacksPlus.Patches;

using CGPacksPlus.HelperExtensions;
using CGPacksPlus.Patterns;

using HarmonyLib;

using System.Collections.Generic;
using System.IO;

using TMPro;

using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

public static class CustomPatternsPatch {
    private static PatternPackUI _packUI = null;
    private static PatternManager _pm = null;
    private static Dictionary<string, GameObject> _patternPackActiveIndicators => CustomPatternsInstance.patternPackActiveIndicators;
    private static Dictionary<string, GameObject> _patternActiveIndicators => CustomPatternsInstance.patternActiveIndicators;

    public static CustomPatterns CustomPatternsInstance = null;

    private static int _maxItemsPerPage => CustomPatternsInstance.maxItemsPerPage;
    private static string _patternsPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");


    [HarmonyPrefix]
    [HarmonyPatch(typeof(CustomPatterns), "Awake")]
    public static void Prefix_CustomPatterns_Awake(ref CustomPatterns __instance) {
        CustomPatternsInstance = __instance;
        _pm = new();
        _packUI = new();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CustomPatterns), "SaveEnabledPatterns")]
    public static bool Prefix_CustomPatterns_SaveEnabledPatterns(ref CustomPatterns __instance) {
        _pm.SaveEnabledPatterns();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CustomPatterns), "LoadEnabledPatterns")]
    public static bool Prefix_CustomPatterns_LoadEnabledPatterns(ref CustomPatterns __instance) {
        _pm.LoadEnabledPatterns();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CustomPatterns), "BuildButtons")]
    public static bool Prefix_CustomPatterns_BuildButtons(ref CustomPatterns __instance, ref Transform ___grid) {
        for (int i = 2; i < ___grid.childCount; ++i) GameObject.Destroy(___grid.GetChild(i).gameObject);

        List<GridTile> allPacksAndPatterns = [];

        Directory.GetDirectories(_patternsPath, "*", SearchOption.TopDirectoryOnly).ForEach(
            dir => allPacksAndPatterns.Add(new() { folder = true, path = Path.GetFileName(dir) })
        );

        Directory.GetFiles(_patternsPath, "*.cgp", SearchOption.TopDirectoryOnly).ForEach(
            dir => allPacksAndPatterns.Add(new() { folder = false, path = Path.GetFileName(dir) })
        );

        __instance.maxPages = Mathf.CeilToInt((float)allPacksAndPatterns.Count / _maxItemsPerPage);
        int currentPage = __instance.currentPage;
        for (int i = (currentPage - 1) * _maxItemsPerPage; i < allPacksAndPatterns.Count && i < currentPage * _maxItemsPerPage; ++i) {
            GridTile current = allPacksAndPatterns[i];

            if (current.folder) {
                PatternPack pack = _pm.LoadPack(current.path);

                var thumbnailTexture = new Texture2D(48, 48);

                _pm.GeneratePackPreview(pack, ref thumbnailTexture);
                thumbnailTexture.Apply();

                bool flipTexture = pack.HasValidThumbnail; // ensures that thumbnails loaded from a file aren't mirrored, they like to do that for some reason
                var thumbnailSprite = Sprite.Create(
                    thumbnailTexture,
                    new Rect(0f, 0f, thumbnailTexture.width, (flipTexture ? -1 : 1) * thumbnailTexture.height),
                    Vector2.zero,
                    100f
                );
                thumbnailSprite.texture.filterMode = FilterMode.Point;

                var packButton = Instantiate(__instance.packButtonTemplate, __instance.grid, worldPositionStays: false);
                packButton.GetComponentInChildren<TMP_Text>(includeInactive: true).text = pack.DisplayName;
                packButton.GetComponent<Image>().sprite = thumbnailSprite;
                packButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => _packUI.SetFocusedPack(pack));
                packButton.SetActive(true);

                _patternActiveIndicators[current.path] = packButton.transform.GetChild(0).gameObject;
                _patternActiveIndicators[current.path].SetActive(pack.EnabledPatterns.Count == pack.ListAllPatterns().Length);
                continue;
            }

            var pattern = _pm.LoadPattern(current.path);
            var patternButton = Instantiate(___grid.GetChild(0), ___grid, worldPositionStays: false).gameObject;

            Texture2D previewTexture = new(16, 16);
            _pm.GeneratePatternPreview(pattern, Vector2Int.zero, ref previewTexture);
            previewTexture.Apply();

            var previewSprite = Sprite.Create(previewTexture, new(0f, 0f, 16f, 16f), Vector2.zero, 100f);
            previewSprite.texture.filterMode = FilterMode.Point;
            patternButton.GetComponent<Image>().sprite = previewSprite;

            patternButton.gameObject.SetActive(true);

            // put this in a variable so the listener doesn't freak out
            string key = current.path;
            patternButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => {
                _pm.TogglePattern(null, key);
                _patternActiveIndicators[key].gameObject.SetActive(!_patternActiveIndicators[key].gameObject.activeSelf);
                EndlessGrid.Instance.customPatterns = _pm.EnabledPatterns;
                _pm.SaveEnabledPatterns();
            });

            _patternActiveIndicators[key] = patternButton.transform.GetChild(0).gameObject;
            _patternActiveIndicators[key].SetActive(_pm.EnabledPatternsPaths.Contains(key));
        }

        __instance.pageText.text = $"{currentPage}/{__instance.maxPages}";
        ___grid.GetChild(0).gameObject.SetActive(false);

        return false;
    }
}
