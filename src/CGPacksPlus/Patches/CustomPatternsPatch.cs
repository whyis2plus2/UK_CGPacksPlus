namespace CGPacksPlus.Patches;

using CGPacksPlus.Patterns;
using GameConsole.pcon;
using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CustomPatternsPatch
{
	private static PatternPackUI packUI = null;
	private static PatternManager pm = null;
	private static Dictionary<string, GameObject> PatternPackActiveIndicators => CustomPatternsInstance.patternPackActiveIndicators;
	private static Dictionary<string, GameObject> PatternActiveIndicators => CustomPatternsInstance.patternActiveIndicators;

	public static CustomPatterns CustomPatternsInstance = null;

	private static int maxItemsPerPage => CustomPatternsInstance.maxItemsPerPage;
    private static string patternsPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Patterns");
	

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CustomPatterns), "Awake")]
	public static void Prefix_CustomPatterns_Awake(ref CustomPatterns __instance)
	{
		CustomPatternsInstance = __instance;
		pm = new();
		packUI = new();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CustomPatterns), "SaveEnabledPatterns")]
	public static bool Prefix_CustomPatterns_SaveEnabledPatterns(ref CustomPatterns __instance)
	{
		pm.SaveEnabledPatterns();
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CustomPatterns), "LoadEnabledPatterns")]
	public static bool Prefix_CustomPatterns_LoadEnabledPatterns(ref CustomPatterns __instance)
	{
		pm.LoadEnabledPatterns();
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CustomPatterns), "BuildButtons")]
	public static bool Prefix_CustomPatterns_BuildButtons(ref CustomPatterns __instance, ref Transform ___grid)
	{
        for (int i = 2; i < ___grid.childCount; ++i) GameObject.Destroy(___grid.GetChild(i).gameObject);

		List<GridTile> allPacksAndPatterns = [];

		foreach (string dir in Directory.GetDirectories(patternsPath, "*", SearchOption.TopDirectoryOnly))
			allPacksAndPatterns.Add(new(){folder = true, path = Path.GetFileName(dir)});

		foreach (string dir in Directory.GetFiles(patternsPath, "*.cgp", SearchOption.TopDirectoryOnly))
			allPacksAndPatterns.Add(new(){folder = false, path = Path.GetFileName(dir)});

		__instance.maxPages = Mathf.CeilToInt((float)allPacksAndPatterns.Count/maxItemsPerPage);
		int currentPage = __instance.currentPage;
		for (int i = (currentPage - 1) * maxItemsPerPage; i < allPacksAndPatterns.Count && i < currentPage * maxItemsPerPage; ++i)
		{
			GridTile current = allPacksAndPatterns[i];

			if (current.folder)
			{
				PatternPack pack = pm.LoadPack(current.path);

				var thumbnailTexture = new Texture2D(48, 48);

				pm.GeneratePackPreview(pack, ref thumbnailTexture);
				thumbnailTexture.Apply();

				bool flipTexture = pack.HasValidThumbnail; // ensures that thumbnails loaded from a file aren't mirrored, they like to do that for some reason
				var thumbnailSprite = Sprite.Create(
					thumbnailTexture,
					new Rect(0f, 0f, thumbnailTexture.width, (flipTexture? -1 : 1) * thumbnailTexture.height),
					Vector2.zero,
					100f
				);
				thumbnailSprite.texture.filterMode = FilterMode.Point;

				var packButton = GameObject.Instantiate(__instance.packButtonTemplate, __instance.grid, worldPositionStays: false);
				packButton.GetComponentInChildren<TMP_Text>(includeInactive: true).text = pack.PackName;
				packButton.GetComponent<Image>().sprite = thumbnailSprite;
				packButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => packUI.SetFocusedPack(pack));
				packButton.SetActive(true);

				PatternActiveIndicators[current.path] = packButton.transform.GetChild(0).gameObject;
				PatternActiveIndicators[current.path].SetActive(pack.EnabledPatterns.Count == pack.ListAllPatterns().Length);
				continue;
			}

			var pattern = pm.LoadPattern(current.path);
            var patternButton = GameObject.Instantiate(___grid.GetChild(0), ___grid, worldPositionStays: false).gameObject;
			
			Texture2D previewTexture = new(16, 16);
			pm.GeneratePatternPreview(pattern, Vector2Int.zero, ref previewTexture);
			previewTexture.Apply();

			var previewSprite = Sprite.Create(previewTexture, new(0f, 0f, 16f, 16f), Vector2.zero, 100f);
            previewSprite.texture.filterMode = FilterMode.Point;
			patternButton.GetComponent<Image>().sprite = previewSprite;

			patternButton.gameObject.SetActive(true);

			// put this in a variable so the listener doesn't freak out
			string key = current.path;
            patternButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() =>
            {
				pm.TogglePattern(null, key);
				CustomPatternsInstance.patternActiveIndicators[key].gameObject.SetActive(!CustomPatternsInstance.patternActiveIndicators[key].gameObject.activeSelf);
                EndlessGrid.Instance.customPatterns = pm.EnabledPatterns;
				pm.SaveEnabledPatterns();
            });

			__instance.patternActiveIndicators[key] = patternButton.transform.GetChild(0).gameObject;
			__instance.patternActiveIndicators[key].SetActive(pm.EnabledPatternsPaths.Contains(key));
		}

		__instance.pageText.text = $"{currentPage}/{__instance.maxPages}";
        ___grid.GetChild(0).gameObject.SetActive(false);

		return false;
	}
}