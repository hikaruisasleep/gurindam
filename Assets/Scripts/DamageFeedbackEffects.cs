using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageFeedbackEffects : MonoBehaviour
{
    [Header("UI Screen Flash")]
    public Image flashImage;
    public Color flashColor = Color.red;
    [Range(0f, 1f)]
    public float maxAlpha = 0.4f;
    public float flashDuration = 0.2f;
    private Coroutine flashCoroutine;

    [Header("Camera Shake")]
    [Tooltip("The camera transform to shake. If empty, defaults to Camera.main.")]
    public Transform cameraTransform;
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 0.2f;
    private Coroutine shakeCoroutine;
    private Vector3 originalCameraLocalPosition;

    void Start()
    {
        // 1. Auto-assign Camera if left empty
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            originalCameraLocalPosition = cameraTransform.localPosition;
        }

        // 2. Initialize Flash Image to transparent
        if (flashImage != null)
        {
            Color c = flashColor;
            c.a = 0f;
            flashImage.color = c;
            flashImage.enabled = false;
        }

        // 3. Auto-register to Player Controller's damage event
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onTakeDamage.AddListener(TriggerFeedback);
            Debug.Log("DamageFeedbackEffects: Successfully registered to Player's onTakeDamage event.");
        }
        else
        {
            Debug.LogWarning("DamageFeedbackEffects: Player not found in scene. You will need to trigger feedback manually.");
        }
    }

    void OnDestroy()
    {
        // Unregister listener to prevent memory leaks when the scene changes or object is destroyed
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onTakeDamage.RemoveListener(TriggerFeedback);
        }
    }

    // Call this method to trigger both visual and physical feedback
    public void TriggerFeedback()
    {
        TriggerFlash();
        TriggerShake();
    }

    public void TriggerFlash()
    {
        if (flashImage == null) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    public void TriggerShake()
    {
        if (cameraTransform == null) return;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            // Reset position before starting a new shake to prevent offset accumulation
            cameraTransform.localPosition = originalCameraLocalPosition;
        }
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashImage.enabled = true;
        float elapsed = 0f;
        Color startColor = flashColor;
        startColor.a = maxAlpha;
        Color endColor = flashColor;
        endColor.a = 0f;

        while (elapsed < flashDuration)
        {
            flashImage.color = Color.Lerp(startColor, endColor, elapsed / flashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        flashImage.color = endColor;
        flashImage.enabled = false;
        flashCoroutine = null;
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        // NOTE: For best results, place the Main Camera inside an empty parent GameObject (Pivot).
        // Let your camera follow script move the parent pivot, while this script shakes the localPosition of the child camera.
        while (elapsed < shakeDuration)
        {
            // Decay the magnitude over time so the shake smoothly settles down
            float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);

            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;

            cameraTransform.localPosition = new Vector3(
                originalCameraLocalPosition.x + x,
                originalCameraLocalPosition.y + y,
                originalCameraLocalPosition.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalCameraLocalPosition;
        shakeCoroutine = null;
    }
}
