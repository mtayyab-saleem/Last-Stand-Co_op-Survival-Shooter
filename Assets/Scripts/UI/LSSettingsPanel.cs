using JUTPS.CameraSystems;
using JUTPS.GameSettings;
using Michsky.MUIP;
using TMPro;
using UnityEngine;

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

    public bool IsOpen { get { return isOpen; } }

private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        WireControls();
        WireBackdrop();

        // This panel lives in every scene, so this is also where saved settings get
        // re-applied after a scene load: the new scene's camera and audio sources
        // pick up whatever the player chose somewhere else.
        JUGameSettings.ApplySettings();
        LSAudioSettings.Apply();
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

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        panelRoot.SetActive(true);

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[LSSettingsPanel] Panel could not be activated; is a parent object disabled?");
            return;
        }

        RefreshFromSettings();

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

        // Push the freshly saved values at the camera in this scene.
        JUGameSettings.ApplySettings();
        LSAudioSettings.Apply();
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
