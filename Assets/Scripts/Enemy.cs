using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 50f;
    private float currentHealth;
    public float contactDamage = 10f;
    [Header("Visual Feedback")]
    public Renderer modelRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;
    [Header("Simple Chase AI")]
    [SerializeField] private float speed = 3.0f;
    private Transform playerXf;


    void Start()
    {
        currentHealth = maxHealth;
        if (modelRenderer != null)
            originalColor = modelRenderer.material.color;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerXf = player.transform;
        }

    }
    // 2. Implement the TakeDamage method required by the IDamageable interface
    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.damage;
        Debug.Log($"{gameObject.name} took {damageInfo.damage} damage from {damageInfo.attacker.name}! HP: {currentHealth}/{maxHealth}");

        FlashRed();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    private void FlashRed()
    {
        if (modelRenderer != null)
        {
            modelRenderer.material.color = hitColor;
            Invoke(nameof(ResetColor), flashDuration);
        }
    }
    private void ResetColor()
    {
        if (modelRenderer != null)
        {
            modelRenderer.material.color = originalColor;
        }
    }
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!");

        // Trigger death animations, spawn loot, or spawn particles here...

        Destroy(gameObject);
    }

    void Update()
    {
        if (playerXf != null)
        {
            // Move toward player
            Vector3 direction = (playerXf.position - transform.position).normalized;
            direction.y = 0; // Remain on the ground plane

            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

    }
}
