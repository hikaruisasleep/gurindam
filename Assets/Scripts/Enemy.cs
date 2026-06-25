using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 50f;
    [SerializeField] private float currentHealth;
    public float CurrentHealth => currentHealth;
    public float contactDamage = 10f;
    [Header("Visual Feedback")]
    public Renderer modelRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;
    [Header("Simple Chase AI")]
    [SerializeField] private float speed = 3.0f;
    private Transform playerXf;
    private Rigidbody rb;
    private float knockbackTimer = 0f;
    public float knockbackDuration = 0.2f;


    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        if (modelRenderer != null)
            originalColor = modelRenderer.material.color;
        Player player = FindFirstObjectByType<Player>();
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

        // Apply physics knockback & suspend AI movement
        if (rb != null && damageInfo.knockbackForce > 0f)
        {
            rb.linearVelocity = Vector3.zero; // Clear existing movement
            rb.AddForce(damageInfo.hitDirection.normalized * damageInfo.knockbackForce, ForceMode.Impulse);
            knockbackTimer = knockbackDuration;
        }

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
        // Decrement knockback suspension timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // Suspend movement logic while taking knockback
        if (knockbackTimer > 0f) return;

        if (playerXf != null)
        {
            Vector3 direction = (playerXf.position - transform.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                // Set Rigidbody velocity for movement (retains gravity)
                Vector3 targetVelocity = direction * speed;
                rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
}
