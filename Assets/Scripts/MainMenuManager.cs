using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the title screen buttons (mainmenu, save-selection, settings).
///
/// Public methods such as <see cref="doButtonPlay"/> and <see cref="doButtonQuit"/>
/// are UnityEvent entry points: select the GameObject holding this component in
/// the Button's OnClick() list and pick the method from the dropdown.
///
/// Scene names are kept in constants so they can be reused for wiring buttons
/// in the Inspector (Runtime Only, Void) without hardcoding strings per button.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // Must match the assets in Assets/Scenes and the entries in
    // ProjectSettings/EditorBuildSettings.asset.
    public const string SaveSelectionSceneName = "save-selection";
    public const string SettingsSceneName = "Settings";
    public const string MainMenuSceneName = "mainmenu";
    public const string GameplaySceneName = "Arena";

    [Header("Scene Flow")]
    [Tooltip("Gameplay scene loaded by the Play button.")]
    [SerializeField] private string gameplaySceneName = GameplaySceneName;

    [Tooltip("Save-slot screen loaded by the New/Continue button. Leave empty to skip it.")]
    [SerializeField] private string saveSelectionSceneName = SaveSelectionSceneName;

    [Tooltip("Settings screen loaded by the Settings button. Leave empty to skip it.")]
    [SerializeField] private string settingsSceneName = SettingsSceneName;

    [Tooltip("Title screen returned to by the Back button. Leave empty to just close the settings/save screen.")]
    [SerializeField] private string mainMenuSceneName = MainMenuSceneName;

    /// <summary>Play button: opens the save-slot screen, or gameplay directly if none is set.</summary>
    public void doButtonPlay()
    {
        if (!string.IsNullOrWhiteSpace(saveSelectionSceneName))
        {
            LoadScene(saveSelectionSceneName);
            return;
        }

        doButtonContinue();
    }

    /// <summary>Save-slot button: starts the game and loads gameplay.</summary>
    public void doButtonContinue()
    {
        LoadScene(gameplaySceneName);
    }

    /// <summary>Settings button.</summary>
    public void doButtonSettings()
    {
        LoadScene(settingsSceneName);
    }

    /// <summary>Back button: returns to the title screen.</summary>
    public void doButtonBack()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            LoadScene(mainMenuSceneName);
            return;
        }

        doButtonQuit();
    }

    /// <summary>Quit button. Also stops Play Mode in the Editor.</summary>
    public void doButtonQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[{nameof(MainMenuManager)}] Button pressed but its target scene name is empty.", null);
            return;
        }

        // Runtime-only check: LoadScene fails at runtime for scenes missing from the build list.
        if (!Application.isEditor && !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[{nameof(MainMenuManager)}] Scene '{sceneName}' is not in the build settings. " +
                "Add it via File > Build Profiles / Build Settings.",
                null);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
