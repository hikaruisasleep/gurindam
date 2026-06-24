using System.Collections;
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Death Settings")]
    [Tooltip("How long (in seconds) the game takes to slow down to a complete freeze on death")]
    public float slowDownDuration = 1.5f;

    [Tooltip("The Canvas/Panel UI overlay to enable when the game freezes")]
    public GameObject deathUIOverlay;

    private Coroutine deathCoroutine;

    void Start()
    {
        // Reset TimeScale to normal in case we reloaded the scene
        Time.timeScale = 1f;

        // Auto-register to Player's onDeath UnityEvent
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onDeath.AddListener(OnPlayerDeath);
        }
        else
        {
            Debug.LogWarning("PlayerDeathHandler: Player not found in scene. Please link to Player.onDeath manually.");
        }

        // Keep the Game Over UI hidden initially
        if (deathUIOverlay != null)
        {
            deathUIOverlay.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Clean up the event listener on destroy to prevent memory leaks
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onDeath.RemoveListener(OnPlayerDeath);
        }
    }

    public void OnPlayerDeath()
    {
        if (deathCoroutine == null)
        {
            deathCoroutine = StartCoroutine(DeathSequenceRoutine());
        }
    }

    private IEnumerator DeathSequenceRoutine()
    {
        float elapsed = 0f;
        float startScale = Time.timeScale;
        float targetScale = 0f;

        // CRITICAL: We must use Time.unscaledDeltaTime here because as Time.timeScale 
        // approaches 0, standard Time.deltaTime approaches 0 and would freeze this coroutine.
        while (elapsed < slowDownDuration)
        {
            Time.timeScale = Mathf.Lerp(startScale, targetScale, elapsed / slowDownDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Lock time scale to absolute zero
        Time.timeScale = 0f;

        // Display the Game Over overlay
        if (deathUIOverlay != null)
        {
            deathUIOverlay.SetActive(true);
        }

        deathCoroutine = null;
    }
}
