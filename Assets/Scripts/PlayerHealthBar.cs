using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class PlayerHealthBar : MonoBehaviour
{
    private Slider healthSlider;

    void Awake()
    {
        healthSlider = GetComponent<Slider>();
    }

    void Start()
    {
        // Find Player in scene and subscribe to the health changed event
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onHealthChanged += UpdateHealthBar;
            
            // Set initial value
            if (player.maxHealth > 0f)
            {
                healthSlider.value = player.HealthPercent;
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealthBar: Player not found in scene.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.onHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null && maxHealth > 0f)
        {
            // Set slider value (0f to 1f)
            healthSlider.value = currentHealth / maxHealth;
        }
    }
}
