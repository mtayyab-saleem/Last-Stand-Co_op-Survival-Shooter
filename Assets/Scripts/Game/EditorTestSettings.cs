using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Testing switches for playing inside the Unity editor. Lives in Resources so it is one
/// asset for every scene: select it with Tools > Last Stand > Editor Test Settings.
///
/// Nothing here affects a phone build: JUTPS's JUGameManager decides touch controls from
/// the device there, and this asset is only read in the editor.
/// </summary>
[CreateAssetMenu(menuName = "Last Stand/Editor Test Settings")]
public class EditorTestSettings : ScriptableObject
{
    [Tooltip("Unity editor only. On: the touch controls are hidden and the game is played with keyboard and mouse - WASD move, Shift run, Space jump, C crouch, R reload, mouse look, left click fire, right click aim. The cursor stays visible so UI buttons can be clicked. Off: the touch controls, as on a phone.")]
    public bool keyboardAndMouseInEditor = true;

#if UNITY_EDITOR
    private const string ResourceName = "EditorTestSettings";

    private static EditorTestSettings active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        active = Resources.Load<EditorTestSettings>(ResourceName);

        if (active == null)
            return;

        // Every scene brings its own JUGameManager (the lobby and game HUDs each have one).
        SceneManager.sceneLoaded += (scene, mode) => active.Apply();

        GameObject runner = new GameObject("[EditorTestSettings]");
        runner.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(runner);
        runner.AddComponent<FreeCursor>();
    }

    // Ticking the box during play mode takes effect straight away.
    private void OnValidate()
    {
        if (Application.isPlaying)
            Apply();
    }

    /// <summary>
    /// JUGameManager copies SimulateMobileDevice into IsMobileControls every frame, and the
    /// JUTPS MobileRig shows the touch controls and blocks keyboard and mouse input from it.
    /// </summary>
    private void Apply()
    {
        JUTPS.CameraSystems.TPSCameraController.RotateWithFreeCursor = keyboardAndMouseInEditor;

        System.Reflection.FieldInfo simulateMobile = typeof(JUTPS.JUGameManager).GetField(
            "SimulateMobileDevice",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (simulateMobile == null)
            return;

        foreach (JUTPS.JUGameManager manager in FindObjectsByType<JUTPS.JUGameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            simulateMobile.SetValue(manager, !keyboardAndMouseInEditor);
    }

    /// <summary>
    /// JUTPS locks and hides the cursor (camera start, pause menu, inventory), which leaves
    /// no pointer to click UI with. In keyboard and mouse mode the cursor is kept free every
    /// frame; the camera still turns with the mouse (TPSCameraController.RotateWithFreeCursor)
    /// and the local player may fire while the cursor shows.
    /// </summary>
    private class FreeCursor : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (active == null || !active.keyboardAndMouseInEditor)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            LSPlayer local = LSPlayer.LocalInstance;

            if (local != null && local.TryGetComponent(out JUTPS.JUCharacterController character))
                character.BlockFireModeOnCursorVisible = false;
        }
    }

    [UnityEditor.MenuItem("Tools/Last Stand/Editor Test Settings")]
    private static void SelectAsset()
    {
        UnityEditor.Selection.activeObject = Resources.Load<EditorTestSettings>(ResourceName);
    }
#endif
}
