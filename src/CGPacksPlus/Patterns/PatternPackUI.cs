namespace CGPacksPlus.Patterns;

using CGPacksPlus.Patches;
using GameConsole.pcon;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PatternPackUI
{
    public Transform CGPatternsPanelUI => 
        SceneManager
            .GetActiveScene()
            .GetRootGameObjects()
            .First((obj) => obj.name == "FirstRoom")
            .transform
            .Find("Room/Cybergrind Shop/Canvas/Background/Main Panel/Patterns");

    private PatternPack focusedPatternPack = null;
    private Transform packUI = null;
    private Transform grid = null; 
    private Transform toggleAllButton = null;
    private TMP_Text pageText = null;

    private Dictionary<string, GameObject> patternActiveIndicators = [];

    private int currentPage = 1;
    private int maxPages = 1;
    private const int MAX_ITEMS_PER_PAGE = 12;

    public PatternPackUI()
    {
        packUI = Object.Instantiate(CGPatternsPanelUI, CGPatternsPanelUI.parent);
    
        packUI.gameObject.SetActive(false);
        grid = packUI.Find("Patterns Window/Panel/Patterns/Panel/Grid");
        toggleAllButton = packUI.Find("Patterns Window/Panel").GetChild(1);

        toggleAllButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        toggleAllButton.GetComponent<ControllerPointer>().OnPressed.AddListener(ToggleAllPatterns);

        var backButton = packUI.GetChild(1); // back button
        backButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        backButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => SetFocusedPack(null));

        var refreshButton = packUI.Find("Patterns Window/Panel/Patterns/Refresh Button");
        refreshButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        refreshButton.GetComponent<ControllerPointer>().OnPressed.AddListener(BuildButtons);

        var pagesUI = packUI.Find("Patterns Window/Panel/Patterns/Pages");

        pagesUI.GetChild(0).GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        pagesUI.GetChild(0).GetComponent<ControllerPointer>().OnPressed.AddListener(PrevPage);
        pagesUI.GetChild(2).GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        pagesUI.GetChild(2).GetComponent<ControllerPointer>().OnPressed.AddListener(NextPage);

        packUI.Find("Patterns Window/Panel/Warning Text").gameObject.SetActive(false);
        packUI.Find("Patterns Window/Panel/Patterns").gameObject.SetActive(true);

        pageText = pagesUI.GetChild(1).GetComponent<TMP_Text>();
    }

    public void SetFocusedPack(PatternPack pack)
    {
        CGPatternsPanelUI.gameObject.SetActive(pack == null);

        if (pack == null)
        {
            packUI.gameObject.SetActive(false);
            CustomPatternsPatch.CustomPatternsInstance.BuildButtons();
            return;
        }

        focusedPatternPack = pack;
        patternActiveIndicators = [];
        currentPage = 1;
        maxPages = Mathf.CeilToInt((float)pack.ListAllPatterns().Length/MAX_ITEMS_PER_PAGE);

        if (pack.EnabledPatterns.Count == 0)
            toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
        else
            toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";

        // set window title to pack name
        packUI.Find("Patterns Window/Title")?.GetComponent<TextMeshProUGUI>().text = (focusedPatternPack.PackName.Length > 18)
            ?$"{focusedPatternPack.PackName[..15]}..."
            :focusedPatternPack.PackName;

        BuildButtons();
        packUI.gameObject.SetActive(true);
    }

    void BuildButtons()
    {
        if (focusedPatternPack == null) return;

        string[] allPatterns = focusedPatternPack.ListAllPatterns();

        // destroy every child of the grid, except for the pattern and pattern pack templates
        for (int i = 2; i < grid.childCount; ++i) Object.Destroy(grid.GetChild(i).gameObject);

        // load all of the patterns that are on the current page
        for (int i = (currentPage - 1) * MAX_ITEMS_PER_PAGE; i < allPatterns.Length && i < currentPage * MAX_ITEMS_PER_PAGE; ++i)
        {
            var pattern = PatternManager.Instance.LoadPattern(Path.Join(focusedPatternPack.Path, allPatterns[i]));
            if (pattern == null)
            {
                Plugin.Log.Error($"Failed to load pattern \"{Path.Join(focusedPatternPack.Path, allPatterns[i])}\"");
                return;
            }

            var patternButton = Object.Instantiate(grid.GetChild(0), grid, worldPositionStays: false).gameObject;

            Texture2D previewTexture = new(16, 16);
            bool patternPreviewSuccess = PatternManager.Instance.GeneratePatternPreview(pattern, Vector2Int.zero, ref previewTexture);
            previewTexture.Apply();

            if (!patternPreviewSuccess) {
                Plugin.Log.Warning($"Failed to generate preview for {Path.Join(focusedPatternPack.Path, allPatterns[i])}");
            }

            var previewSprite = Sprite.Create(previewTexture, new(0f, 0f, 16f, 16f), Vector2.zero, 100f);
            previewSprite.texture.filterMode = FilterMode.Point;

            patternButton.GetComponent<Image>().sprite = previewSprite;
            patternButton.SetActive(true);

            // put this in a variable so that the listener doesn't do weird stuff
            string currentPattern = allPatterns[i];
            patternButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() =>
            {
                if (focusedPatternPack.EnabledPatterns.Contains(currentPattern))
                {
                    PatternManager.Instance.DisablePattern(focusedPatternPack, currentPattern);
                    patternActiveIndicators[currentPattern].SetActive(false);
                }
                else
                {
                    PatternManager.Instance.EnablePattern(focusedPatternPack, currentPattern);
                    patternActiveIndicators[currentPattern].SetActive(true);
                }

                if (focusedPatternPack.EnabledPatterns.Count == 0)
                    toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
                else
                    toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";
            });

            patternActiveIndicators[currentPattern] = patternButton.transform.GetChild(0).gameObject;
            patternActiveIndicators[currentPattern].SetActive(focusedPatternPack.EnabledPatterns.Contains(currentPattern));
        }

        pageText.text = $"{currentPage}/{maxPages}";
        grid.GetChild(0).gameObject.SetActive(false);
    }

    void NextPage()
    {
        if (currentPage == maxPages) return;
        ++currentPage;
        BuildButtons();
    }

    void PrevPage()
    {
        if (currentPage == 1) return;
        --currentPage;
        BuildButtons();
    }

    void ToggleAllPatterns()
    {
        if (focusedPatternPack == null) return;

        if (focusedPatternPack.EnabledPatterns.Count == 0)
        {
            toggleAllButton?.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";
            foreach (var p in focusedPatternPack.ListAllPatterns()) PatternManager.Instance.EnablePattern(focusedPatternPack, p);
            foreach (var indicator in patternActiveIndicators.Values) indicator?.SetActive(true);
        }
        else
        {
            toggleAllButton?.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
            foreach (var p in focusedPatternPack.ListAllPatterns()) PatternManager.Instance.DisablePattern(focusedPatternPack, p);
            foreach (var indicator in patternActiveIndicators.Values) indicator?.SetActive(false);
        }

        if (!EndlessGrid.Instance.customPatternMode) CustomPatternsPatch.CustomPatternsInstance.Toggle();
    }
}