namespace CGPacksPlus.Patterns;

using CGPacksPlus.HelperExtensions;
using CGPacksPlus.Patches;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Object;

public class PatternPackUI {
    private const int _MAX_ITEMS_PER_PAGE = 12;
    private PatternPack _focusedPatternPack = null;
    private Transform _packUI = null;
    private Transform _grid = null;
    private Transform _toggleAllButton = null;
    private TMP_Text _pageText = null;
    private Dictionary<string, GameObject> _patternActiveIndicators = [];
    private int _currentPage = 1;
    private int _maxPages = 1;
    private static plog.Logger _log = new($"{Plugin.PLUGIN_SHORTNAME}.{nameof(PatternPackUI)}");

    public Transform CGPatternsPanelUI =>
        SceneManager
            .GetActiveScene()
            .GetRootGameObjects()
            .First((obj) => obj.name == "FirstRoom")
            .transform
            .Find("Room/Cybergrind Shop/Canvas/Background/Main Panel/Patterns");

    public PatternPackUI() {
        _packUI = Instantiate(CGPatternsPanelUI, CGPatternsPanelUI.parent);

        _packUI.gameObject.SetActive(false);
        _grid = _packUI.Find("Patterns Window/Panel/Patterns/Panel/Grid");
        _toggleAllButton = _packUI.Find("Patterns Window/Panel").GetChild(1);

        _toggleAllButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        _toggleAllButton.GetComponent<ControllerPointer>().OnPressed.AddListener(ToggleAllPatterns);

        var backButton = _packUI.GetChild(1); // back button
        backButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        backButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => SetFocusedPack(null));

        var refreshButton = _packUI.Find("Patterns Window/Panel/Patterns/Refresh Button");
        refreshButton.GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        refreshButton.GetComponent<ControllerPointer>().OnPressed.AddListener(BuildButtons);

        var pagesUI = _packUI.Find("Patterns Window/Panel/Patterns/Pages");

        pagesUI.GetChild(0 /* Prev Page Button */).GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        pagesUI.GetChild(0 /* Prev Page Button */).GetComponent<ControllerPointer>().OnPressed.AddListener(PrevPage);
        pagesUI.GetChild(2 /* Next Page Button */).GetComponent<ControllerPointer>().OnPressed.RemoveAllListeners();
        pagesUI.GetChild(2 /* Next Page Button */).GetComponent<ControllerPointer>().OnPressed.AddListener(NextPage);

        _packUI.Find("Patterns Window/Panel/Warning Text").gameObject.SetActive(false);
        _packUI.Find("Patterns Window/Panel/Patterns").gameObject.SetActive(true);

        // make it so that pressing any of the other buttons also closes the pattern pack ui
        for (int i = 0; i < 4; ++i) {
            CGPatternsPanelUI.parent
                .Find("Main Menu/Buttons")
                .GetChild(i)
                .GetComponent<ControllerPointer>()
                .OnPressed
                .AddListener(() => _packUI.gameObject.SetActive(false));
        }

        _pageText = pagesUI.GetChild(1).GetComponent<TMP_Text>();
    }

    public void SetFocusedPack(PatternPack pack) {
        CGPatternsPanelUI.gameObject.SetActive(pack == null);

        if (pack == null) {
            _packUI.gameObject.SetActive(false);
            CustomPatternsPatch.CustomPatternsInstance.BuildButtons();
            return;
        }

        _focusedPatternPack = pack;
        _patternActiveIndicators = [];
        _currentPage = 1;
        _maxPages = Mathf.CeilToInt((float)pack.ListAllPatterns().Length / _MAX_ITEMS_PER_PAGE);

        if (pack.EnabledPatterns.Count == 0)
            _toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
        else
            _toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";

        // set window title to pack name
        _packUI.Find("Patterns Window/Title")?.GetComponent<TextMeshProUGUI>().text = (_focusedPatternPack.DisplayName.Length > 18)
            ? $"{_focusedPatternPack.DisplayName[..15]}..."
            : _focusedPatternPack.DisplayName;

        BuildButtons();
        _packUI.gameObject.SetActive(true);
    }

    void BuildButtons() {
        if (_focusedPatternPack == null) return;

        string[] allPatterns = _focusedPatternPack.ListAllPatterns();

        // destroy every child of the grid, except for the pattern and pattern pack templates
        for (int i = 2; i < _grid.childCount; ++i) Object.Destroy(_grid.GetChild(i).gameObject);

        // load all of the patterns that are on the current page
        for (int i = (_currentPage - 1) * _MAX_ITEMS_PER_PAGE; i < allPatterns.Length && i < _currentPage * _MAX_ITEMS_PER_PAGE; ++i) {
            var pattern = PatternManager.Instance.LoadPattern(Path.Join(_focusedPatternPack.Path, allPatterns[i]));
            if (pattern == null) {
                _log.Error($"Failed to load pattern \"{Path.Join(_focusedPatternPack.Path, allPatterns[i])}\"");
                return;
            }

            var patternButton = Instantiate(_grid.GetChild(0), _grid, worldPositionStays: false).gameObject;

            Texture2D previewTexture = new(16, 16);
            bool patternPreviewSuccess = PatternManager.Instance.GeneratePatternPreview(pattern, Vector2Int.zero, ref previewTexture);
            previewTexture.Apply();

            if (!patternPreviewSuccess) {
                _log.Warning($"Failed to generate preview for {Path.Join(_focusedPatternPack.Path, allPatterns[i])}");
            }

            var previewSprite = Sprite.Create(previewTexture, new(0f, 0f, 16f, 16f), Vector2.zero, 100f);
            previewSprite.texture.filterMode = FilterMode.Point;

            patternButton.GetComponent<Image>().sprite = previewSprite;
            patternButton.SetActive(true);

            // put this in a variable so that the listener doesn't do weird stuff
            string currentPattern = allPatterns[i];
            patternButton.GetComponent<ControllerPointer>().OnPressed.AddListener(() => {
                if (_focusedPatternPack.EnabledPatterns.Contains(currentPattern)) {
                    PatternManager.Instance.DisablePattern(_focusedPatternPack, currentPattern);
                    _patternActiveIndicators[currentPattern].SetActive(false);
                } else {
                    PatternManager.Instance.EnablePattern(_focusedPatternPack, currentPattern);
                    _patternActiveIndicators[currentPattern].SetActive(true);
                }

                if (_focusedPatternPack.EnabledPatterns.Count == 0)
                    _toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
                else
                    _toggleAllButton.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";
            });

            _patternActiveIndicators[currentPattern] = patternButton.transform.GetChild(0).gameObject;
            _patternActiveIndicators[currentPattern].SetActive(_focusedPatternPack.EnabledPatterns.Contains(currentPattern));
        }

        _pageText.text = $"{_currentPage}/{_maxPages}";
        _grid.GetChild(0).gameObject.SetActive(false);
    }

    void NextPage() {
        if (_currentPage == _maxPages) return;
        ++_currentPage;
        BuildButtons();
    }

    void PrevPage() {
        if (_currentPage == 1) return;
        --_currentPage;
        BuildButtons();
    }

    void ToggleAllPatterns() {
        if (_focusedPatternPack == null) return;

        if (_focusedPatternPack.EnabledPatterns.Count == 0) {
            _toggleAllButton?.GetComponentInChildren<TextMeshProUGUI>().text = "Disable All";
            _focusedPatternPack.ListAllPatterns().ForEach(p => PatternManager.Instance.EnablePattern(_focusedPatternPack, p));
            _patternActiveIndicators.Values.ForEach(indicator => indicator?.SetActive(true));
        } else {
            _toggleAllButton?.GetComponentInChildren<TextMeshProUGUI>().text = "Enable All";
            _focusedPatternPack.ListAllPatterns().ForEach(p => PatternManager.Instance.DisablePattern(_focusedPatternPack, p));
            _patternActiveIndicators.Values.ForEach(indicator => indicator?.SetActive(false));
        }

        if (!EndlessGrid.Instance.customPatternMode) CustomPatternsPatch.CustomPatternsInstance.Toggle();
    }
}
