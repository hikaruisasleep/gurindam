using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDamageable
{
    InputAction moveAction;
    InputAction dashAction;
    InputAction attackAction;

    public Rigidbody rb;


    [Header("Movement")]
    public float maxSpeed = 5.0f;
    public float acceleration = 50.0f;
    public float deceleration = 5.0f;
    public float dashForce = 35.0f;
    public float dashCooldown = 1.0f;
    public float dashDeceleration = 70.0f;
    public float dashTurnSpeed = 4.0f;
    public float dashDuration = 0.2f; // Active phasing/invincibility time
    [SerializeField] private float lastDashTime = -99f;
    [SerializeField] bool isMoving = false;
    [SerializeField] bool canDash = false;
    [SerializeField] private float dashCooldownTimer = 0f;
    private bool dashRequested = false;

    [Header("Attacking")]
    public ComboStep[] normalAttackCombo;
    public Transform spawnPoint; // Position in front of the player
    public float comboResetDelay = 1.8f;
    public float inputBufferWindow = 0.3f;
    private int currentComboIndex = 0;
    private float lastAttackTime = -100f;
    private float lastAttackInputTime = -100f;
    private Hurtbox activeHurtbox;
    private bool attackRequested = false;

    [Header("Auto-Targeting")]
    public float targetDetectionRadius = 25f;
    public LayerMask enemyLayer;
    [Range(0f, 1f)]
    public float targetConeThreshold = 0f;

    [Header("Player Health & I-Frames")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public float iframeDuration = 1.5f;
    [SerializeField] private float iframeTimer = 0f;
    public float iframeBlinkFrequency = 15f;
    private Renderer[] playerRenderers;
    
    [Header("Player Events")]
    public UnityEngine.Events.UnityEvent onTakeDamage;
    public UnityEngine.Events.UnityEvent onDeath;

    // C# event for performance-friendly UI Slider binding
    public event System.Action<float, float> onHealthChanged;

    public float HealthPercent => (maxHealth > 0f) ? currentHealth / maxHealth : 0f;

    [System.Serializable]
    public struct ComboStep
    {
        public Hurtbox[] prefabsToSpawn;
    }

    Transform mainCamera;

    void Start()
    {
        mainCamera = Camera.main.transform;
        moveAction = InputSystem.actions.FindAction("Move");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        currentHealth = maxHealth; // Initialize player health
        playerRenderers = GetComponentsInChildren<Renderer>(); // Cache all player renderers

        // Trigger initial health sync for UI
        if (onHealthChanged != null)
        {
            onHealthChanged.Invoke(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        dashCooldownTimer = Mathf.Max(0f, dashCooldown - (Time.time - lastDashTime));

        if (dashAction.WasPressedThisFrame())
        {
            dashRequested = true;
        }

        if (attackAction.WasPressedThisFrame())
        {
            attackRequested = true;
            lastAttackInputTime = Time.time;
        }

        if (attackRequested && (Time.time - lastAttackInputTime > inputBufferWindow))
        {
            attackRequested = false;
        }

        // Count down i-frame timer & handle visual blinking
        if (iframeTimer > 0f)
        {
            iframeTimer -= Time.deltaTime;
            bool isBlinkVisible = Mathf.FloorToInt(Time.time * iframeBlinkFrequency) % 2 == 0;
            SetRenderersEnabled(isBlinkVisible);
        }
        else
        {
            SetRenderersEnabled(true);
        }
    }

    void FixedUpdate()
    {
        // Ignore collisions with enemies during active dash phase
        int enemyLayerIndex = GetLayerFromMask(enemyLayer);
        bool isDashing = (Time.time - lastDashTime < dashDuration);
        Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, isDashing);

        #region movement
        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        Vector3 forward = mainCamera.forward;
        Vector3 right = mainCamera.right;
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();


        Vector3 moveInput = (forward * moveValue.y) + (right * moveValue.x);
        Vector3 targetVelocity = moveInput * maxSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        Vector3 newHorizontalVelocity;

        if (currentHorizontalVelocity.magnitude > maxSpeed)
        {
            if (moveInput.sqrMagnitude > 0.01f && activeHurtbox == null)
            {
                Vector3 inputDir = moveInput.normalized;
                float alignment = Vector3.Dot(currentHorizontalVelocity.normalized, inputDir);

                if (alignment < -0.7f)
                {
                    // Dash-cancel: Negate dash momentum completely and move in input direction immediately
                    newHorizontalVelocity = targetVelocity;
                }
                else
                {
                    // Rotate the velocity vector towards the input direction (preserves magnitude)
                    Vector3 rotatedVelocity = Vector3.RotateTowards(
                        currentHorizontalVelocity,
                        inputDir * currentHorizontalVelocity.magnitude,
                        dashTurnSpeed * Time.fixedDeltaTime,
                        0f
                    );

                    // Decelerate the magnitude down towards the walking maxSpeed
                    newHorizontalVelocity = Vector3.MoveTowards(
                        rotatedVelocity,
                        inputDir * maxSpeed,
                        dashDeceleration * Time.fixedDeltaTime
                    );
                }
            }
            else
            {
                // No input: decelerate straight to zero at the dash deceleration rate
                newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, dashDeceleration * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Standard walking physics
            float currentAccel = (currentHorizontalVelocity.sqrMagnitude > targetVelocity.sqrMagnitude) ? deceleration : acceleration;
            newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, currentAccel * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);

        if (moveInput.sqrMagnitude > 0.01f && activeHurtbox == null)
        {
            transform.rotation = Quaternion.LookRotation(moveInput);
        }

        Vector3 dashDirection = moveInput != Vector3.zero ? moveInput : transform.forward;

        isMoving = (moveInput.sqrMagnitude > 0.01f) || (currentHorizontalVelocity.sqrMagnitude > 0.005f);

        canDash = (dashCooldownTimer <= 0f) && isMoving;

        if (dashRequested)
        {
            dashRequested = false;
 
            if (canDash)
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(dashDirection.normalized * dashForce, ForceMode.Impulse);
                lastDashTime = Time.time;
            }
        }
        #endregion

        #region attacking
        if (attackRequested && activeHurtbox == null)
        {
            attackRequested = false; // Consume input
            if (normalAttackCombo == null || normalAttackCombo.Length == 0) return;

            if (Time.time - lastAttackTime > comboResetDelay)
            {
                currentComboIndex = 0;
            }

            // Auto-Targeting: Find best target in range
            Transform target = GetAutoTarget(moveInput);
            float targetDistance = Mathf.Infinity;
            if (target != null)
            {
                Vector3 attackDirection = target.position - transform.position; // Fixed: Use transform.position so direction doesn't reverse when enemy is close
                attackDirection.y = 0;
                attackDirection.Normalize();

                // Rotate player to face the target
                if (attackDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(attackDirection);
                }

                // Calculate horizontal plane distance to target
                Vector3 playerPosPlane = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 targetPosPlane = new Vector3(target.position.x, 0, target.position.z);
                targetDistance = Vector3.Distance(playerPosPlane, targetPosPlane);
            }

            ComboStep currentStep = normalAttackCombo[currentComboIndex];
            if (currentStep.prefabsToSpawn != null)
            {
                foreach (Hurtbox prefab in currentStep.prefabsToSpawn)
                {
                    if (prefab != null && spawnPoint != null)
                    {
                        Hurtbox newHurtbox = Instantiate(prefab, spawnPoint);

                        // If the target is closer than the spawnPoint's forward distance, snap the spawn position closer on both X and Z to hit the hugging enemy
                        if (target != null && targetDistance < spawnPoint.localPosition.z)
                        {
                            Vector3 localTargetPos = spawnPoint.InverseTransformPoint(target.position);
                            newHurtbox.transform.localPosition = new Vector3(
                                localTargetPos.x + newHurtbox.transform.localPosition.x,
                                newHurtbox.transform.localPosition.y,
                                localTargetPos.z + newHurtbox.transform.localPosition.z
                            );
                        }

                        //do not deparent so hurtbox moves with player
                        newHurtbox.Initialize(this.gameObject, newHurtbox.transform.forward);
                        activeHurtbox = newHurtbox;
                    }
                }
            }

            // 4. Update the timestamp of the last attack
            lastAttackTime = Time.time;

            // 5. Advance the combo index (wraps back to 0 when the combo chain finishes)
            currentComboIndex = (currentComboIndex + 1) % normalAttackCombo.Length;

        }
        #endregion
    }

    public Transform GetAutoTarget(Vector3 moveInput)
    {
        // 1. Scan for all colliders in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, targetDetectionRadius, enemyLayer);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        // Use the player's input direction if moving, otherwise use character's facing direction
        Vector3 searchDirection = moveInput != Vector3.zero ? moveInput.normalized : transform.forward;

        foreach (Collider col in hitColliders)
        {
            // Verify it's an enemy (implements IDamageable)
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            // BUG FIX: Get the root enemy transform instead of the individual collider child transform
            MonoBehaviour enemyMono = damageable as MonoBehaviour;
            Transform enemyTransform = (enemyMono != null) ? enemyMono.transform : col.transform;

            Vector3 toEnemy = enemyTransform.position - transform.position;
            toEnemy.y = 0; // Keep math on the horizontal plane
            float distance = toEnemy.magnitude;

            // Calculate alignment between our search direction and the enemy direction
            float alignment = Vector3.Dot(searchDirection, toEnemy.normalized);

            // Check if the enemy is within our targeting cone
            if (alignment >= targetConeThreshold)
            {
                // Line of sight check
                Vector3 start = transform.position + Vector3.up * 1f; // Eyes level
                Vector3 end = enemyTransform.position + Vector3.up * 1f; // Target center
                if (Physics.Linecast(start, end, LayerMask.GetMask("Obstacles"))) continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = enemyTransform;
                }
            }
        }

        // Fallback: If no enemies are in our forward cone, find the absolute closest enemy in any direction
        if (bestTarget == null)
        {
            foreach (Collider col in hitColliders)
            {
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                MonoBehaviour enemyMono = damageable as MonoBehaviour;
                Transform enemyTransform = (enemyMono != null) ? enemyMono.transform : col.transform;

                float distance = Vector3.Distance(transform.position, enemyTransform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = enemyTransform;
                }
            }
        }

        return bestTarget;
    }

    #region player damage
    private void OnCollisionEnter(Collision collision) { HandleEnemyContact(collision.gameObject); }
    private void OnCollisionStay(Collision collision) { HandleEnemyContact(collision.gameObject); }
    private void OnTriggerEnter(Collider other) { HandleEnemyContact(other.gameObject); }
    private void OnTriggerStay(Collider other) { HandleEnemyContact(other.gameObject); }

    private void HandleEnemyContact(GameObject go)
    {
        Enemy enemy = go.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Vector3 hitDir = (transform.position - go.transform.position).normalized;
            hitDir.y = 0;

            DamageInfo info = new DamageInfo
            {
                damage = enemy.contactDamage,
                knockbackForce = 5f,
                hitDirection = hitDir,
                attacker = go
            };

            TakeDamage(info);
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        bool isInvincible = (iframeTimer > 0f) || (Time.time - lastDashTime < dashDuration);
        if (isInvincible) return; // Invincible!

        currentHealth -= damageInfo.damage;
        currentHealth = Mathf.Max(0f, currentHealth); // Clamp health so it doesn't go below 0
        iframeTimer = iframeDuration;

        Debug.Log($"Player took {damageInfo.damage} damage from {damageInfo.attacker.name}! HP: {currentHealth}/{maxHealth}");

        // Trigger health changed event
        if (onHealthChanged != null)
        {
            onHealthChanged.Invoke(currentHealth, maxHealth);
        }

        // Trigger take damage visual/audio feedback event hooks (camera shake, screen flash)
        if (onTakeDamage != null)
        {
            onTakeDamage.Invoke();
        }

        // Apply knockback to player
        if (rb != null && damageInfo.knockbackForce > 0f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(damageInfo.hitDirection.normalized * damageInfo.knockbackForce, ForceMode.Impulse);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        if (onDeath != null)
        {
            onDeath.Invoke();
        }
    }

    private void SetRenderersEnabled(bool enabledState)
    {
        if (playerRenderers == null) return;
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null && playerRenderers[i].enabled != enabledState)
            {
                playerRenderers[i].enabled = enabledState;
            }
        }
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int maskValue = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & maskValue) != 0)
            {
                return i;
            }
        }
        return 0;
    }
    #endregion
}
