using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Hurtbox : MonoBehaviour
{
    [Header("Damage Stats")]
    public float damage = 10f;
    public float knockbackForce = 5f;
    public float lifetime = 0.5f;
    public float speed = 5f;

    [Header("Targeting")]
    [Tooltip("Which layers should this hurtbox damage?")]
    public LayerMask targetMask;
    private Vector3 moveDirection;
    private GameObject attacker;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        // Get or add Rigidbody at runtime to isolate collision events from the Player's Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Call this immediately after instantiating to configure the hurtbox
    public void Initialize(GameObject attacker, Vector3 direction)
    {
        this.attacker = attacker;
        this.moveDirection = direction.normalized;

        // Auto-destruct after its lifetime expires
        Destroy(gameObject, lifetime);
    }
    void Update()
    {
        // Move the hurtbox forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        // Check if the hit object is in the target mask
        if (((1 << other.gameObject.layer) & targetMask) != 0)
        {
            // Find the damageable component (either on this object or its parent)
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null && !hitTargets.Contains(damageable))
            {
                hitTargets.Add(damageable); // Prevent double-hitting this entity
                // Build the damage payload
                DamageInfo info = new DamageInfo
                {
                    damage = this.damage,
                    knockbackForce = this.knockbackForce,
                    hitDirection = this.moveDirection,
                    attacker = this.attacker
                };
                damageable.TakeDamage(info);

                // Trigger any hit VFX/SFX here...

                // If it's a projectile, you might want to destroy it on first impact:
                // Destroy(gameObject);
            }
        }
    }
}
