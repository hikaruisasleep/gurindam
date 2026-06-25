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
    private float defaultFixedDeltaTime;

    void Start()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        // Reset TimeScale to normal in case we reloaded the scene
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        // Keep the Game Over UI hidden initially
        if (deathUIOverlay != null)
        {
            deathUIOverlay.SetActive(false);
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
            float newScale = Mathf.Lerp(startScale, targetScale, elapsed / slowDownDuration);
            Time.timeScale = newScale;

            // Scale fixedDeltaTime proportionally, clamping to prevent CPU performance spikes
            if (newScale > 0.01f)
            {
                Time.fixedDeltaTime = defaultFixedDeltaTime * newScale;
            }
            else
            {
                Time.fixedDeltaTime = defaultFixedDeltaTime * 0.01f;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Lock time scale to absolute zero and restore fixedDeltaTime
        Time.timeScale = 0f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        // Display the Game Over overlay
        if (deathUIOverlay != null)
        {
            deathUIOverlay.SetActive(true);
        }

        deathCoroutine = null;
    }
}
