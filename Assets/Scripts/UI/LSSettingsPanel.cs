using JUTPS.CameraSystems;
using JUTPS.GameSettings;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Settings popup shared by the menu, lobby and gameplay scenes.
///
/// It never touches Time.timeScale, so the match keeps running while it is open.
/// The camera controller is suspended instead, otherwise moving the mouse to a
/// slider would spin the player's view.
///
/// Sensitivity and invert are stored by JUTPS's own JUGameSettings, audio by
/// LSAudioSettings, and the player name by PlayerProfileSO.
/// </summary>
public class LSSettingsPanel : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root object toggled on and off. Defaults to this GameObject.")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private ButtonManager closeButton;

    [Header("Profile")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private PlayerProfileSO playerProfile;

    [Header("Controls")]
    [SerializeField] private SliderManager sensitivitySlider;
    [SerializeField] private float minSensitivity = 0.2f;
    [SerializeField] private float maxSensitivity = 5f;
    [SerializeField] private SwitchManager invertVerticalSwitch;
    [SerializeField] private SwitchManager invertHorizontalSwitch;

    [Header("Audio")]
    [SerializeField] private SliderManager musicSlider;
    [SerializeField] private SwitchManager musicSwitch;
    [SerializeField] private SliderManager sfxSlider;
    [SerializeField] private SwitchManager sfxSwitch;

    [Header("Behaviour")]
    [Tooltip("Free the cursor while the panel is open so the controls are usable in game.")]
    [SerializeField] private bool releaseCursorWhileOpen = true;

    private CursorLockMode previousLockState;
    private bool previousCursorVisible;
    private JUCameraController suspendedCamera;

    private bool isOpen;
    private bool isRefreshing;

    // AI difficulty row, built in code below the Audio section.
    private const float DifficultyBlockHeight = 130f;
    private static readonly string[] DifficultyNames = { "EASY", "MEDIUM", "HARD" };
    private Image[] difficultyFills;
    private TextMeshProUGUI[] difficultyLabels;
    private Button[] difficultyButtons;
    private TextMeshProUGUI difficultyNote;

    public bool IsOpen { get { return isOpen; } }

private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        WireControls();
        WireBackdrop();
        BuildDifficultyRow();

        // LSSettingsRuntime owns applying settings at startup and per scene load,
        // because this panel is authored disabled and its Awake may never run.
        // Asking again here is harmless and covers the case where it is enabled.
        LSSettingsRuntime.RequestApply();
    }

    private void Start()
    {
        // Deliberately not in Awake. A panel left disabled in the scene only runs
        // Awake when Open() switches it on, so hiding it there would instantly undo
        // the open - which is why the first click appeared to do nothing.
        if (!isOpen)
            panelRoot.SetActive(false);
    }

    // -------------------------
    // Open / close
    // -------------------------

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    /// <summary>
    /// Keeps the popup above everything else on screen.
    ///
    /// It lives on the HUD canvas, which sorts below the lobby and result screens, so
    /// without its own sorting order it would open behind them.
    /// </summary>
    private void DrawOnTop()
    {
        if (!panelRoot.TryGetComponent(out Canvas canvas))
        {
            canvas = panelRoot.AddComponent<Canvas>();
            panelRoot.AddComponent<GraphicRaycaster>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 900;
    }

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        panelRoot.SetActive(true);
        DrawOnTop();

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[LSSettingsPanel] Panel could not be activated; is a parent object disabled?");
            return;
        }

        RefreshFromSettings();
        RefreshDifficulty();

        // MUIP widgets reset themselves in their own OnEnable, which ran during the
        // SetActive above. Refreshing again next frame makes sure our values win.
        StartCoroutine(RefreshNextFrame());

        if (releaseCursorWhileOpen)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Suspending the camera stops the view spinning while the mouse is on a
        // slider. The game itself keeps running, nothing is paused.
        suspendedCamera = FindFirstObjectByType<JUCameraController>();

        if (suspendedCamera != null)
            suspendedCamera.enabled = false;
    }

public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        // Read the widgets back before hiding. Live listeners already push most
        // changes, but this makes saving on close unconditional rather than
        // dependent on every MUIP event having fired.
        CommitFromWidgets();

        panelRoot.SetActive(false);

        PlayerPrefs.Save();

        if (releaseCursorWhileOpen)
        {
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
        }

        if (suspendedCamera != null)
        {
            suspendedCamera.enabled = true;
            suspendedCamera = null;
        }
    }

    // -------------------------
    // AI difficulty
    // -------------------------

    /// <summary>
    /// "AI DIFFICULTY" with Easy / Medium / Hard, under Audio. The window grows to fit
    /// and the close button moves down. Built here so the panel needs no new wiring.
    /// </summary>
    private void BuildDifficultyRow()
    {
        var window = panelRoot.transform.Find("Window") as RectTransform;
        Transform audioTitle = window != null ? window.Find("Text_AUDIO") : null;

        if (audioTitle == null)
        {
            Debug.LogWarning("[LSSettingsPanel] Window/Text_AUDIO not found; the AI difficulty row was not added.");
            return;
        }

        window.sizeDelta += new Vector2(0f, DifficultyBlockHeight);

        foreach (string below in new[] { "Divider2", "CloseButton" })
        {
            if (window.Find(below) is RectTransform moved)
                moved.anchoredPosition -= new Vector2(0f, DifficultyBlockHeight);
        }

        // Section title in the same style as PROFILE / CONTROLS / AUDIO.
        GameObject title = Instantiate(audioTitle.gameObject, window);
        title.name = "Text_AI DIFFICULTY";
        ((RectTransform)title.transform).anchoredPosition = new Vector2(56f, -676f);
        TMP_Text titleText = title.GetComponent<TMP_Text>();
        titleText.text = "AI DIFFICULTY";
        TMP_FontAsset font = titleText.font;

        difficultyFills = new Image[3];
        difficultyLabels = new TextMeshProUGUI[3];
        difficultyButtons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            int level = i;

            RectTransform border = LSUITheme.Panel("Difficulty_" + DifficultyNames[i], window, LSUITheme.BoxBorder, true);
            TopLeft(border, new Vector2(56f + i * 262f, -718f), new Vector2(244f, 46f));

            RectTransform inner = LSUITheme.Panel("Fill", border, LSUITheme.BoxInner);
            LSUITheme.Stretch(inner);
            inner.sizeDelta = new Vector2(-4f, -4f);
            difficultyFills[i] = inner.GetComponent<Image>();

            difficultyLabels[i] = LSUITheme.Label("Label", border, font, DifficultyNames[i], 22f, 4f,
                                                  LSUITheme.Text, TextAlignmentOptions.Center);
            LSUITheme.Stretch((RectTransform)difficultyLabels[i].transform);

            difficultyButtons[i] = border.gameObject.AddComponent<Button>();
            difficultyButtons[i].transition = Selectable.Transition.None;
            difficultyButtons[i].onClick.AddListener(() => SelectDifficulty((BotDifficulty)level));
        }

        difficultyNote = LSUITheme.Label("Note", window, font, string.Empty, 16f, 2f,
                                         LSUITheme.Muted, TextAlignmentOptions.Left);
        TopLeft((RectTransform)difficultyNote.transform, new Vector2(56f, -774f), new Vector2(768f, 26f));

        RefreshDifficulty();
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        LSUITheme.Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
    }

    private void SelectDifficulty(BotDifficulty difficulty)
    {
        if (MatchRunning())
            return;

        LSBotDifficulty.Current = difficulty;
        RefreshDifficulty();
    }

    // Bots are planned when the host starts the match; from then on the choice is fixed.
    private static bool MatchRunning()
    {
        return SceneManager.GetActiveScene().name == "GameScene";
    }

    private void RefreshDifficulty()
    {
        if (difficultyButtons == null)
            return;

        bool locked = MatchRunning();
        int selected = (int)LSBotDifficulty.Current;

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool on = i == selected;
            difficultyButtons[i].interactable = !locked;
            difficultyFills[i].color = on ? LSUITheme.ButtonGreen : LSUITheme.BoxInner;
            difficultyLabels[i].color = on ? LSUITheme.Text : LSUITheme.Muted;

            // Dimmed while locked, the chosen one still readable.
            difficultyLabels[i].alpha = locked && !on ? 0.4f : 1f;
        }

        difficultyNote.text = locked
            ? "LOCKED DURING A MATCH  ·  THE HOST'S DIFFICULTY IS IN USE"
            : "SET BEFORE THE MATCH  ·  IN A MATCH THE HOST'S DIFFICULTY IS USED";
    }

    /// <summary>
    /// Writes whatever the widgets currently show into the saved settings.
    /// </summary>
    private void CommitFromWidgets()
    {
        if (isRefreshing) return;

        if (sensitivitySlider != null && sensitivitySlider.mainSlider != null)
            JUGameSettings.CameraSensibility = sensitivitySlider.mainSlider.value;

        if (musicSlider != null && musicSlider.mainSlider != null)
            LSAudioSettings.MusicVolume = musicSlider.mainSlider.value / 100f;

        if (sfxSlider != null && sfxSlider.mainSlider != null)
            LSAudioSettings.SfxVolume = sfxSlider.mainSlider.value / 100f;

        if (invertVerticalSwitch != null) JUGameSettings.CameraInvertVertical = invertVerticalSwitch.isOn;
        if (invertHorizontalSwitch != null) JUGameSettings.CameraInvertHorizontal = invertHorizontalSwitch.isOn;
        if (musicSwitch != null) LSAudioSettings.MusicEnabled = musicSwitch.isOn;
        if (sfxSwitch != null) LSAudioSettings.SfxEnabled = sfxSwitch.isOn;

        // Push the freshly saved values at the camera and mixer in this scene.
        LSSettingsRuntime.RequestApply();
    }

private System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        if (isOpen) RefreshFromSettings();
    }


    // -------------------------
    // Wiring
    // -------------------------

private void WireControls()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.RemoveListener(OnPlayerNameChanged);
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }

        // MUIP has its own PlayerPrefs saving. It is switched off here so these
        // settings have exactly one owner and cannot drift out of sync.
        // Listening on the underlying Slider rather than MUIP's own SliderEvent,
        // because that event does not fire reliably for every drag.
        WireSlider(sensitivitySlider, minSensitivity, maxSensitivity, OnSensitivityChanged);
        WireSlider(musicSlider, 0f, 100f, OnMusicVolumeChanged);
        WireSlider(sfxSlider, 0f, 100f, OnSfxVolumeChanged);

        WireSwitch(invertVerticalSwitch, OnInvertVerticalChanged);
        WireSwitch(invertHorizontalSwitch, OnInvertHorizontalChanged);
        WireSwitch(musicSwitch, OnMusicEnabledChanged);
        WireSwitch(sfxSwitch, OnSfxEnabledChanged);
    }

    private void WireSlider(SliderManager target, float min, float max, UnityEngine.Events.UnityAction<float> handler)
    {
        if (target == null)
            return;

        target.enableSaving = false;
        target.minValue = min;
        target.maxValue = max;

        if (target.mainSlider != null)
        {
            target.mainSlider.minValue = min;
            target.mainSlider.maxValue = max;
            target.mainSlider.onValueChanged.RemoveListener(handler);
            target.mainSlider.onValueChanged.AddListener(handler);
        }
    }

    private void WireSwitch(SwitchManager target, UnityEngine.Events.UnityAction<bool> handler)
    {
        if (target == null)
            return;

        target.saveValue = false;
        target.invokeAtStart = false;
        target.onValueChanged.AddListener(handler);
    }

    /// <summary>
    /// Pushes the saved values back into the widgets. isRefreshing stops the
    /// widget callbacks from writing the same value straight back out again.
    /// </summary>
    public void RefreshFromSettings()
    {
        // try/finally: if any widget throws mid-refresh, isRefreshing must still be
        // cleared, or every later change is silently swallowed and nothing saves.
        isRefreshing = true;
        try { RefreshFromSettingsInternal(); }
        finally { isRefreshing = false; }
    }

    private void RefreshFromSettingsInternal()
    {
        if (playerNameInput != null)
        {
            if (playerProfile == null)
                playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();

            playerProfile.LoadProfile();

            string savedName = Capitalise(playerProfile.playerName);
            if (string.IsNullOrWhiteSpace(savedName)) savedName = "Player";

            playerNameInput.SetTextWithoutNotify(savedName);
            playerNameInput.ForceLabelUpdate();
        }

        SetSlider(sensitivitySlider, JUGameSettings.CameraSensibility);

        // Volume sliders read 0-100 for a clean label; settings store 0-1.
        SetSlider(musicSlider, LSAudioSettings.MusicVolume * 100f);
        SetSlider(sfxSlider, LSAudioSettings.SfxVolume * 100f);

        SetSwitch(invertVerticalSwitch, JUGameSettings.CameraInvertVertical);
        SetSwitch(invertHorizontalSwitch, JUGameSettings.CameraInvertHorizontal);
        SetSwitch(musicSwitch, LSAudioSettings.MusicEnabled);
        SetSwitch(sfxSwitch, LSAudioSettings.SfxEnabled);
    }

    private void SetSlider(SliderManager slider, float value)
    {
        if (slider == null || slider.mainSlider == null)
            return;

        slider.mainSlider.SetValueWithoutNotify(value);
        slider.UpdateUI();
    }

    private void SetSwitch(SwitchManager target, bool value)
    {
        if (target == null)
            return;

        target.isOn = value;

        // MUIP resolves its animator in Start(), which has not run yet on the frame
        // the panel is first shown. Without this, UpdateUI() dereferences null and
        // the whole refresh aborts halfway through.
        if (target.switchAnimator == null)
            target.switchAnimator = target.GetComponent<Animator>();

        if (target.switchAnimator != null && target.switchAnimator.gameObject.activeInHierarchy)
            target.UpdateUI();
    }

    // -------------------------
    // Handlers
    // -------------------------

    private void OnPlayerNameChanged(string newName)
    {
        if (isRefreshing || string.IsNullOrWhiteSpace(newName))
            return;

        newName = Capitalise(newName);

        if (playerProfile == null)
            playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();

        playerProfile.playerName = newName;
        playerProfile.SaveProfile();

        // Show the tidied version back to the player.
        if (playerNameInput != null)
        {
            playerNameInput.SetTextWithoutNotify(newName);
            playerNameInput.ForceLabelUpdate();
        }

        // Push the new name to the server straight away when connected.
        if (LSPlayer.LocalInstance != null)
            LSPlayer.LocalInstance.CmdSetPlayerName(newName);
    }

    /// <summary>Trims and upper-cases the first letter, e.g. "tayyab" -> "Tayyab".</summary>
    private static string Capitalise(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        value = value.Trim();
        return char.ToUpper(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Clicking the dimmed area behind the window closes the popup, which also
    /// commits the settings because Close() saves.
    /// </summary>
    private void WireBackdrop()
    {
        Transform dim = panelRoot.transform.Find("Dim");
        if (dim == null) return;

        UnityEngine.UI.Button backdrop = dim.GetComponent<UnityEngine.UI.Button>();
        if (backdrop == null) backdrop = dim.gameObject.AddComponent<UnityEngine.UI.Button>();

        backdrop.transition = UnityEngine.UI.Selectable.Transition.None;
        backdrop.targetGraphic = dim.GetComponent<UnityEngine.UI.Image>();
        backdrop.onClick.RemoveAllListeners();
        backdrop.onClick.AddListener(Close);
    }

    private void OnSensitivityChanged(float value)
    {
        if (isRefreshing) return;
        JUGameSettings.CameraSensibility = value;
    }

    private void OnInvertVerticalChanged(bool value)
    {
        if (isRefreshing) return;
        JUGameSettings.CameraInvertVertical = value;
    }

    private void OnInvertHorizontalChanged(bool value)
    {
        if (isRefreshing) return;
        JUGameSettings.CameraInvertHorizontal = value;
    }

private void OnMusicVolumeChanged(float value)
    {
        if (isRefreshing) return;
        LSAudioSettings.MusicVolume = value / 100f;
    }

private void OnSfxVolumeChanged(float value)
    {
        if (isRefreshing) return;
        LSAudioSettings.SfxVolume = value / 100f;
    }

    private void OnMusicEnabledChanged(bool value)
    {
        if (isRefreshing) return;
        LSAudioSettings.MusicEnabled = value;
    }

    private void OnSfxEnabledChanged(bool value)
    {
        if (isRefreshing) return;
        LSAudioSettings.SfxEnabled = value;
    }
}
