using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause overlay controller for gameplay scenes (Arena, Dungeons).
///
/// Drop this component on a GameObject that is always active, assign the
/// overlay root (inactive by default) and point the Buttons' OnClick() list at
/// <see cref="doButtonResume"/>, <see cref="doButtonRestart"/> and
/// <see cref="doButtonQuitToMenu"/>.
///
/// Pausing sets <see cref="Time.timeScale"/> to 0, so the overlay itself must
/// animate and read input with unscaled time.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Overlay")]
    [Tooltip("Root of the pause overlay. Left inactive on play and toggled when pausing.")]
    [SerializeField] private GameObject pauseOverlay;

    [Tooltip("Optional first button to select when the overlay opens, for gamepad navigation.")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Input")]
    [Tooltip("Optional explicit action. Leave empty to use the built-in Escape / gamepad Start polling.")]
    [SerializeField] private InputActionReference pauseAction;

    [Tooltip("Poll the Escape key and the gamepad Start button directly. Works with no Inspector wiring.")]
    [SerializeField] private bool pollPauseKeys = true;

    [Header("Time")]
    [Tooltip("Time scale applied while paused. 0 freezes gameplay completely.")]
    [SerializeField] private float pausedTimeScale = 0f;

    [Tooltip("Mute audio while paused by toggling AudioListener.pause.")]
    [SerializeField] private bool pauseAudio = true;

    [Header("Scene Flow")]
    [Tooltip("Scene loaded by the Quit button. Leave empty to quit the application instead.")]
    [SerializeField] private string mainMenuSceneName = "mainmenu";

    [Tooltip("Scene reloaded by the Restart button. Leave empty to use the active scene.")]
    [SerializeField] private string gameplaySceneName = "Arena";

    [Header("Cursor")]
    [Tooltip("Unlocks the cursor while paused and restores it on resume.")]
    [SerializeField] private bool manageCursor = true;

    private bool isPaused;
    private bool cursorWasVisible;
    private CursorLockMode previousCursorLockMode;

    /// <summary>True while the game is paused.</summary>
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPauseActionPerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPauseActionPerformed;
        }

        // Never leave the game frozen if this component is disabled mid-pause.
        if (isPaused)
        {
            Resume();
        }
    }

    private void Update()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            return; // handled by the action callback
        }

        if (!pollPauseKeys)
        {
            return;
        }

        // Poll devices directly so pausing works without any Inspector wiring.
        // Unscaled time is irrelevant here: these are per-frame edge reads.
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnPauseActionPerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    /// <summary>Pauses if running, resumes if already paused.</summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>Freezes gameplay and shows the overlay.</summary>
    public void Pause()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = pausedTimeScale;

        if (pauseAudio)
        {
            AudioListener.pause = true;
        }

        if (manageCursor)
        {
            cursorWasVisible = Cursor.visible;
            previousCursorLockMode = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(true);
        }

        if (firstSelectedButton != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    /// <summary>Restores gameplay and hides the overlay. Safe to call when not paused.</summary>
    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseAudio)
        {
            AudioListener.pause = false;
        }

        if (manageCursor)
        {
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = previousCursorLockMode;
        }

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }
    }

    /// <summary>Resume button.</summary>
    public void doButtonResume()
    {
        Resume();
    }

    /// <summary>Restart button: reloads the gameplay scene at normal time scale.</summary>
    public void doButtonRestart()
    {
        Resume();
        LoadScene(string.IsNullOrWhiteSpace(gameplaySceneName)
            ? SceneManager.GetActiveScene().name
            : gameplaySceneName);
    }

    /// <summary>Quit button: returns to the main menu, or quits the application.</summary>
    public void doButtonQuitToMenu()
    {
        Resume();

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        LoadScene(mainMenuSceneName);
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[{nameof(PauseMenuManager)}] No scene name configured for this button.", null);
            return;
        }

        // Runtime-only check: LoadScene fails for scenes missing from the build list.
        if (!Application.isEditor && !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[{nameof(PauseMenuManager)}] Scene '{sceneName}' is not in the build settings.",
                null);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
