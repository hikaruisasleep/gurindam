using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDamageable
{
    #region variable initialization
    InputAction moveAction;
    InputAction dashAction;
    InputAction attackAction;
    InputAction altAttackAction;
    InputAction skillAction;
    InputAction ultimateAction;
    InputAction interactAction;

    public Rigidbody rb;

    [Header("Ranged Attack (Alt-Attack)")]
    public Hurtbox rangedAttackPrefab;
    public LineRenderer aimLineRenderer;
    public LineRenderer aimGhostLineRenderer;
    public float chargedAttackDuration = 0.5f;
    public float rangedAttackRange = 10f;
    public Color chargingColor = Color.red;
    public Color chargedColor = Color.green;
    public Color ghostLineColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
    [Header("Ranged Aim Movement Modifiers")]
    public float rangedAimSpeedMultiplier = 0.4f;
    public float rangedAimDecelMultiplier = 0.3f;
    [Header("Ranged Aim Camera Zoom")]
    public Unity.Cinemachine.CinemachineCamera virtualCamera;
    public float rangedAimCameraZoomSizeAdd = 1.5f;
    public float cameraZoomInSpeed = 8f;
    private float defaultCameraSize = 5f;
    [SerializeField] private float currentZoomPercent = 0f;
    [SerializeField] private bool isAimingRanged = false;
    [SerializeField] private float altAttackHoldStartTime = 0f;
    private float zoomResetStartPercent = 0f;
    private float zoomInProgress = 1f;
    private bool wasAimingRanged = false;

    [Header("Movement")]
    public float maxSpeed = 5.0f;
    public float acceleration = 50.0f;
    public float deceleration = 5.0f;
    public float dashForce = 35.0f;
    public float dashCooldown = 1.0f;
    public float dashDeceleration = 70.0f;
    public float dashTurnSpeed = 4.0f;
    [UnityEngine.Serialization.FormerlySerializedAs("dashDuration")]
    public float dashInvincibilityDuration = 0.2f; // Active phasing/invincibility time
    [SerializeField] private float lastDashTime = -99f;
    [SerializeField] bool isMoving = false;
    [SerializeField] bool canDash = false;
    [SerializeField] private float dashCooldownTimer = 0f;
    private bool dashRequested = false;
    [Header("Ground & Stair Suspension")]
    [SerializeField] private bool autoAdjustCollider = true;
    [SerializeField] private float stepClearance = 0.35f;
    [SerializeField] private float rideHeight = 1.0f;
    [SerializeField] private float suspensionStrength = 250f;
    [SerializeField] private float suspensionDamper = 30f;
    [SerializeField] private float probeRadius = 0.25f;
    [SerializeField] private float maxStepDown = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] float airborneGravityMultiplier = 2f;

    [Header("Double Dash Options")]
    public bool doubleDash = false;
    [SerializeField] private int dashesUsed = 0;
    [SerializeField] private float firstDashTime = -99f;
    [SerializeField] private float cooldownStartTime = -99f;

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
    public LayerMask ObjectPhasingLayer;
    [Range(0f, 1f)]
    public float targetConeThreshold = 0f;
    public float targetDistanceTolerance = 2.0f;

    [Header("Player Health & I-Frames")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public float iframeDuration = 1.5f;
    [SerializeField] private float iframeTimer = 0f;
    public float iframeBlinkFrequency = 15f;
    private Renderer[] playerRenderers;

    [Header("Player State")]
    [SerializeField] private bool controlsEnabled = true;

    [Header("Player Events")]
    public UnityEngine.Events.UnityEvent onTakeDamage;
    public UnityEngine.Events.UnityEvent onDeath;

    // C# event for performance-friendly UI Slider binding
    public event System.Action<float, float> onHealthChanged;

    [Header("Animation")]
    public Animator animator;

    [Header("Animation States")]
    [SerializeField] private bool _isWalking = false;
    [SerializeField] private bool _isDashing = false;
    [SerializeField] private bool _isMeleeAttack1 = false;
    [SerializeField] private bool _isMeleeAttack2 = false;
    [SerializeField] private bool _isMeleeAttack3 = false;
    [SerializeField] private bool _isChargingAttack = false;
    [SerializeField] private bool _chargeAttackRelease = false;
    [SerializeField] private int _meleeAttackStage = 0;

    public bool isWalking { get => _isWalking; private set => _isWalking = value; }
    public bool isDashing { get => _isDashing; private set => _isDashing = value; }
    public bool isMeleeAttack1 { get => _isMeleeAttack1; private set => _isMeleeAttack1 = value; }
    public bool isMeleeAttack2 { get => _isMeleeAttack2; private set => _isMeleeAttack2 = value; }
    public bool isMeleeAttack3 { get => _isMeleeAttack3; private set => _isMeleeAttack3 = value; }
    public bool isChargingAttack { get => _isChargingAttack; private set => _isChargingAttack = value; }
    public bool chargeAttackRelease { get => _chargeAttackRelease; private set => _chargeAttackRelease = value; }
    public int meleeAttackStage { get => _meleeAttackStage; private set => _meleeAttackStage = value; }
    public bool isMeleeAttack => _isMeleeAttack1 || _isMeleeAttack2 || _isMeleeAttack3;

    [Header("Animation Events")]
    public UnityEngine.Events.UnityEvent<bool> onWalkingStateChanged;
    public UnityEngine.Events.UnityEvent<bool> onDashingStateChanged;
    public UnityEngine.Events.UnityEvent onDashStart;
    public UnityEngine.Events.UnityEvent onMeleeAttack1;
    public UnityEngine.Events.UnityEvent onMeleeAttack2;
    public UnityEngine.Events.UnityEvent onMeleeAttack3;
    public UnityEngine.Events.UnityEvent<int> onMeleeAttack;
    public UnityEngine.Events.UnityEvent onMeleeAttackEnd;
    public UnityEngine.Events.UnityEvent<bool> onChargingAttackStateChanged;
    public UnityEngine.Events.UnityEvent onChargeAttackRelease;

    private readonly System.Collections.Generic.HashSet<int> animatorParamHashes = new System.Collections.Generic.HashSet<int>();
    static readonly int AnimIsWalking = Animator.StringToHash("isWalking");
    static readonly int AnimIsDashing = Animator.StringToHash("isDashing");
    static readonly int AnimDashTrigger = Animator.StringToHash("dash");
    static readonly int AnimIsMeleeAttack = Animator.StringToHash("isMeleeAttack");
    static readonly int AnimIsMeleeAttack1 = Animator.StringToHash("isMeleeAttack1");
    static readonly int AnimIsMeleeAttack2 = Animator.StringToHash("isMeleeAttack2");
    static readonly int AnimIsMeleeAttack3 = Animator.StringToHash("isMeleeAttack3");
    static readonly int AnimMeleeAttack1 = Animator.StringToHash("meleeAttack1");
    static readonly int AnimMeleeAttack2 = Animator.StringToHash("meleeAttack2");
    static readonly int AnimMeleeAttack3 = Animator.StringToHash("meleeAttack3");
    static readonly int AnimMeleeAttackStage = Animator.StringToHash("meleeAttackStage");
    static readonly int AnimIsChargingAttack = Animator.StringToHash("isChargingAttack");
    static readonly int AnimChargeAttackRelease = Animator.StringToHash("chargeAttackRelease");

    public float HealthPercent => (maxHealth > 0f) ? currentHealth / maxHealth : 0f;

    [System.Serializable]
    public struct ComboStep
    {
        public Hurtbox[] prefabsToSpawn;
    }

    Transform mainCamera;
    #endregion

    void Start()
    {
        mainCamera = Camera.main.transform;
        moveAction = InputSystem.actions.FindAction("Move");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        altAttackAction = InputSystem.actions.FindAction("AltAttack");
        skillAction = InputSystem.actions.FindAction("Skill");
        ultimateAction = InputSystem.actions.FindAction("Ultimate");
        interactAction = InputSystem.actions.FindAction("Interact");
        currentHealth = maxHealth; // Initialize player health
        playerRenderers = GetComponentsInChildren<Renderer>(); // Cache all player renderers
        controlsEnabled = true;

        if (virtualCamera != null)
        {
            defaultCameraSize = virtualCamera.Lens.OrthographicSize;
        }

        // Trigger initial health sync for UI
        if (onHealthChanged != null)
        {
            onHealthChanged.Invoke(currentHealth, maxHealth);
        }

        // hide aim line on game start
        aimLineRenderer.enabled = false;
        aimLineRenderer.positionCount = 0;

        if (groundLayer.value == 0)
        {
            groundLayer = ~LayerMask.GetMask("Player", "Ignore Raycast");
        }

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null && autoAdjustCollider)
        {
            // Elevate the bottom of the capsule collider by stepClearance, keeping head height identical.
            // This ensures step risers and floor unevenness never physically collide with the capsule.
            col.height = Mathf.Max(col.radius * 2f, col.height - stepClearance);
            col.center += new Vector3(0f, stepClearance * 0.5f, 0f);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                animatorParamHashes.Add(p.nameHash);
            }
        }
    }

    void Update()
    {
        dashCooldownTimer = Mathf.Max(0f, dashCooldown - (Time.time - cooldownStartTime));

        // Reset dashes count if cooldown timer finished
        int maxDashes = doubleDash ? 2 : 1;
        if (dashesUsed >= maxDashes && dashCooldownTimer <= 0f)
        {
            dashesUsed = 0;
        }
        // Or if doubleDash is enabled, we only did one dash, and the cooldown duration has passed since then
        else if (doubleDash && dashesUsed == 1 && Time.time - firstDashTime >= dashCooldown)
        {
            dashesUsed = 0;
        }

        if (controlsEnabled && dashAction.WasPressedThisFrame())
        {
            dashRequested = true;
        }

        if (controlsEnabled && attackAction.WasPressedThisFrame())
        {
            attackRequested = true;
            lastAttackInputTime = Time.time;
        }

        // Reset combo if the delay has exceeded since the last attack input
        if (Time.time - lastAttackInputTime > comboResetDelay)
        {
            currentComboIndex = 0;
        }

        if (attackRequested && (Time.time - lastAttackInputTime > inputBufferWindow))
        {
            attackRequested = false;
        }

        if (controlsEnabled && skillAction != null && skillAction.WasPressedThisFrame())
        {
            Debug.Log("Skill Cast pressed!");
        }
        if (controlsEnabled && ultimateAction != null && ultimateAction.WasPressedThisFrame())
        {
            Debug.Log("Ultimate Cast pressed!");
        }
        if (controlsEnabled && interactAction != null && interactAction.WasPressedThisFrame())
        {
            Debug.Log("Interact Action pressed!");
        }

        if (controlsEnabled && altAttackAction != null && altAttackAction.WasPressedThisFrame())
        {
            isAimingRanged = true;
            altAttackHoldStartTime = Time.time;
            isChargingAttack = true;
            chargeAttackRelease = false;
            onChargingAttackStateChanged?.Invoke(true);
            SetAnimBool(AnimIsChargingAttack, true);
            SetAnimBool(AnimChargeAttackRelease, false);
            ResetAnimTrigger(AnimChargeAttackRelease);

            if (aimLineRenderer != null)
            {
                aimLineRenderer.positionCount = 2;
                aimLineRenderer.enabled = true;
            }
            if (aimGhostLineRenderer != null)
            {
                aimGhostLineRenderer.positionCount = 2;
                aimGhostLineRenderer.enabled = true;
            }
        }

        if (isAimingRanged && altAttackAction != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 playerFootPos = transform.position;
            Vector3 aimDir = mouseWorldPos - playerFootPos;
            aimDir.y = 0;
            aimDir.Normalize();
            if (aimDir == Vector3.zero)
            {
                aimDir = transform.forward;
            }

            Vector3 targetAimPos = playerFootPos + aimDir * rangedAttackRange;
            float chargePercent = chargedAttackDuration > 0f ? Mathf.Clamp01((Time.time - altAttackHoldStartTime) / chargedAttackDuration) : 1f;
            bool isCharged = chargePercent >= 1.0f;

            if (aimGhostLineRenderer != null)
            {
                aimGhostLineRenderer.SetPosition(0, playerFootPos);
                aimGhostLineRenderer.SetPosition(1, targetAimPos);

                Color currentGhostColor = isCharged ? chargedColor : ghostLineColor;
                aimGhostLineRenderer.startColor = currentGhostColor;
                aimGhostLineRenderer.endColor = currentGhostColor;
            }

            if (aimLineRenderer != null)
            {
                Vector3 currentTargetPos = Vector3.Lerp(playerFootPos, targetAimPos, chargePercent);
                Vector3 yOffset = Vector3.up * 0.01f;
                aimLineRenderer.SetPosition(0, playerFootPos + yOffset);
                aimLineRenderer.SetPosition(1, currentTargetPos + yOffset);

                Color currentColor = isCharged ? chargedColor : chargingColor;
                aimLineRenderer.startColor = currentColor;
                aimLineRenderer.endColor = currentColor;
            }

            // Face the aiming direction
            transform.rotation = Quaternion.LookRotation(aimDir);

            if (!altAttackAction.IsPressed()) // Released
            {
                isAimingRanged = false;
                if (isChargingAttack)
                {
                    isChargingAttack = false;
                    onChargingAttackStateChanged?.Invoke(false);
                    SetAnimBool(AnimIsChargingAttack, false);
                }

                if (aimLineRenderer != null)
                {
                    aimLineRenderer.enabled = false;
                    aimLineRenderer.positionCount = 0;
                }
                if (aimGhostLineRenderer != null)
                {
                    aimGhostLineRenderer.enabled = false;
                    aimGhostLineRenderer.positionCount = 0;
                }

                if (Time.time - altAttackHoldStartTime >= chargedAttackDuration)
                {
                    FireRangedAttack(targetAimPos);
                    chargeAttackRelease = true;
                    onChargeAttackRelease?.Invoke();
                    SetAnimTrigger(AnimChargeAttackRelease);
                    SetAnimBool(AnimChargeAttackRelease, true);
                }
                else
                {
                    chargeAttackRelease = false;
                    SetAnimBool(AnimChargeAttackRelease, false);
                    ResetAnimTrigger(AnimChargeAttackRelease);
                    Debug.Log($"Ranged attack cancelled: charged for {Time.time - altAttackHoldStartTime:F2}s (minimum is {chargedAttackDuration}s).");
                }
            }
        }

        // Zooming out: Coupled with the charge duration (chargedAttackDuration)
        if (isAimingRanged)
        {
            currentZoomPercent = chargedAttackDuration > 0f ? Mathf.Clamp01((Time.time - altAttackHoldStartTime) / chargedAttackDuration) : 1f;
            wasAimingRanged = true;
        }
        // Zooming in (reset): Decoupled from charge duration for a fast and snappy snapback using cubic ease-out
        else
        {
            if (wasAimingRanged)
            {
                wasAimingRanged = false;
                zoomResetStartPercent = currentZoomPercent;
                zoomInProgress = 0f;
            }

            if (zoomInProgress < 1f)
            {
                zoomInProgress = Mathf.Min(1f, zoomInProgress + cameraZoomInSpeed * Time.deltaTime);
                float t = zoomInProgress;
                float easedProgress = 1f - (1f - t) * (1f - t) * (1f - t); // Cubic Ease-Out
                currentZoomPercent = Mathf.Lerp(zoomResetStartPercent, 0f, easedProgress);
            }
            else
            {
                currentZoomPercent = 0f;
            }
        }

        if (virtualCamera != null)
        {
            float targetSize = Mathf.Lerp(defaultCameraSize, defaultCameraSize + rangedAimCameraZoomSizeAdd, currentZoomPercent);
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = targetSize;
            virtualCamera.Lens = lens;
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
        // Ride-Height Virtual Spring Suspension (Stairs & Ground Hugging)
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;
        float lookaheadDist = Mathf.Min(speed * Time.fixedDeltaTime, 0.4f);
        Vector3 forwardOffset = speed > 0.1f ? horizontalVel.normalized * lookaheadDist : Vector3.zero;
        Vector3 probeOrigin = transform.position + forwardOffset;

        float targetDist = rideHeight - probeRadius;
        // Scale maximum downward reach dynamically with horizontal speed so fast movement down slopes/stairs stays grounded
        float dynamicStepDown = maxStepDown + speed * Time.fixedDeltaTime * 1.5f;
        float maxDist = targetDist + dynamicStepDown;

        bool isGrounded = Physics.SphereCast(
            probeOrigin, probeRadius, Vector3.down, out RaycastHit groundHit,
            maxDist, groundLayer, QueryTriggerInteraction.Ignore);

        bool isDashActive = Time.time - lastDashTime < dashInvincibilityDuration;

        if (isGrounded)
        {
            float currentDist = groundHit.distance > 0f ? groundHit.distance : 0f;
            float error = targetDist - currentDist;
            float vy = rb.linearVelocity.y;

            // Critically damped spring force + gravity compensation to prevent sag
            float springForce = (error * suspensionStrength) - (vy * suspensionDamper) - Physics.gravity.y;
            rb.AddForce(Vector3.up * springForce, ForceMode.Acceleration);

            // Step/stair assists: only active when moving horizontally
            if (speed > 0.5f)
            {
                if (error > 0.02f)
                {
                    // Ascending: lift capsule to glide over upcoming step/slope
                    float neededClimbVy = error / Time.fixedDeltaTime;
                    if (rb.linearVelocity.y < neededClimbVy)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, neededClimbVy, rb.linearVelocity.z);
                    }
                }
                else if (error < -0.06f)
                {
                    // Descending: smoothly pull downward to hug descending slope/stairs and eliminate coyote time
                    float neededDescendVy = Mathf.Clamp(error / (Time.fixedDeltaTime * 1.5f), -35f, 0f);
                    if (rb.linearVelocity.y > neededDescendVy)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, neededDescendVy, rb.linearVelocity.z);
                    }
                }
            }

            // Once at or above ride height during a dash, suppress upward velocity so slope impacts cannot launch
            if (isDashActive && error <= 0f && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }
        else
        {
            if (airborneGravityMultiplier > 1f)
            {
                // Extra gravity when genuinely airborne for snappy, responsive falls
                rb.AddForce(Physics.gravity * (airborneGravityMultiplier - 1f), ForceMode.Acceleration);
            }

            // If airborne during a dash, suppress any upward launch velocity
            if (isDashActive && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }

        // Ignore collisions with enemies during active dash phase
        int enemyLayerIndex = GetLayerFromMask(enemyLayer);
        int ObjectPhasingIndex = GetLayerFromMask(ObjectPhasingLayer);
        Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, isDashActive);
        Physics.IgnoreLayerCollision(gameObject.layer, ObjectPhasingIndex, isDashActive);

        #region movement
        Vector2 moveValue = controlsEnabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        Vector3 forward = mainCamera.forward;
        Vector3 right = mainCamera.right;
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 moveInput = (forward * moveValue.y) + (right * moveValue.x);
        float activeMaxSpeed = isAimingRanged ? (maxSpeed * rangedAimSpeedMultiplier) : maxSpeed;
        Vector3 targetVelocity = moveInput * activeMaxSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        Vector3 newHorizontalVelocity;

        if (currentHorizontalVelocity.magnitude > activeMaxSpeed)
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
                        inputDir * activeMaxSpeed,
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
            float activeDecel = isAimingRanged ? (deceleration * rangedAimDecelMultiplier) : deceleration;
            float currentAccel = (currentHorizontalVelocity.sqrMagnitude > targetVelocity.sqrMagnitude) ? activeDecel : acceleration;
            newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, currentAccel * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);

        if (moveInput.sqrMagnitude > 0.01f && activeHurtbox == null && !isAimingRanged)
        {
            transform.rotation = Quaternion.LookRotation(moveInput);
        }

        Vector3 dashDirection = moveInput != Vector3.zero ? moveInput : transform.forward;

        isMoving = (moveInput.sqrMagnitude > 0.01f) || (currentHorizontalVelocity.sqrMagnitude > 0.005f);

        bool dashingNow = Time.time - lastDashTime < dashInvincibilityDuration;
        bool walkingNow = isMoving && !dashingNow;

        if (walkingNow != isWalking)
        {
            isWalking = walkingNow;
            onWalkingStateChanged?.Invoke(isWalking);
            SetAnimBool(AnimIsWalking, isWalking);
        }

        if (dashingNow != isDashing)
        {
            isDashing = dashingNow;
            onDashingStateChanged?.Invoke(isDashing);
            SetAnimBool(AnimIsDashing, isDashing);
        }

        canDash = (dashCooldownTimer <= 0f) && isMoving;

        if (dashRequested)
        {
            dashRequested = false;

            if (canDash)
            {
                // Cancel ranged aiming on dash
                if (isAimingRanged)
                {
                    isAimingRanged = false;
                    chargeAttackRelease = false;
                    SetAnimBool(AnimChargeAttackRelease, false);
                    ResetAnimTrigger(AnimChargeAttackRelease);

                    if (isChargingAttack)
                    {
                        isChargingAttack = false;
                        onChargingAttackStateChanged?.Invoke(false);
                        SetAnimBool(AnimIsChargingAttack, false);
                    }

                    if (aimLineRenderer != null)
                    {
                        aimLineRenderer.enabled = false;
                        aimLineRenderer.positionCount = 0;
                    }
                    if (aimGhostLineRenderer != null)
                    {
                        aimGhostLineRenderer.enabled = false;
                        aimGhostLineRenderer.positionCount = 0;
                    }
                    Debug.Log("Ranged attack cancelled: player dashed.");
                }

                rb.linearVelocity = Vector3.zero;
                rb.AddForce(dashDirection.normalized * dashForce, ForceMode.Impulse);
                lastDashTime = Time.time;
                onDashStart?.Invoke();
                SetAnimTrigger(AnimDashTrigger);

                dashesUsed++;
                if (dashesUsed == 1)
                {
                    firstDashTime = Time.time;
                }

                int maxDashes = doubleDash ? 2 : 1;
                if (dashesUsed >= maxDashes)
                {
                    cooldownStartTime = Time.time;
                }
            }
        }
        #endregion

        #region attacking
        if (activeHurtbox == null && meleeAttackStage != 0)
        {
            meleeAttackStage = 0;
            isMeleeAttack1 = false;
            isMeleeAttack2 = false;
            isMeleeAttack3 = false;
            onMeleeAttackEnd?.Invoke();
            SetAnimBool(AnimIsMeleeAttack, false);
            SetAnimBool(AnimIsMeleeAttack1, false);
            SetAnimBool(AnimIsMeleeAttack2, false);
            SetAnimBool(AnimIsMeleeAttack3, false);
            SetAnimInt(AnimMeleeAttackStage, 0);
        }

        if (attackRequested && activeHurtbox == null)
        {
            attackRequested = false; // Consume input
            if (normalAttackCombo == null || normalAttackCombo.Length == 0) return;

            int attackStage = currentComboIndex + 1; // 1, 2, or 3
            meleeAttackStage = attackStage;
            isMeleeAttack1 = (attackStage == 1);
            isMeleeAttack2 = (attackStage == 2);
            isMeleeAttack3 = (attackStage == 3);

            onMeleeAttack?.Invoke(attackStage);
            if (attackStage == 1) onMeleeAttack1?.Invoke();
            else if (attackStage == 2) onMeleeAttack2?.Invoke();
            else if (attackStage == 3) onMeleeAttack3?.Invoke();

            SetAnimBool(AnimIsMeleeAttack, true);
            SetAnimBool(AnimIsMeleeAttack1, isMeleeAttack1);
            SetAnimBool(AnimIsMeleeAttack2, isMeleeAttack2);
            SetAnimBool(AnimIsMeleeAttack3, isMeleeAttack3);
            SetAnimInt(AnimMeleeAttackStage, meleeAttackStage);
            SetAnimTrigger(attackStage == 1 ? AnimMeleeAttack1 : (attackStage == 2 ? AnimMeleeAttack2 : AnimMeleeAttack3));

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
        float bestDistance = Mathf.Infinity;

        // Use the mouse direction relative to the player as the search direction
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 searchDirection = mouseWorldPos - transform.position;
        searchDirection.y = 0;
        searchDirection.Normalize();
        if (searchDirection == Vector3.zero)
        {
            searchDirection = transform.forward;
        }

        foreach (Collider col in hitColliders)
        {
            // Verify it's an enemy (implements IDamageable and has Enemy component)
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            Transform enemyTransform = enemy.transform;

            Vector3 toEnemy = enemyTransform.position - transform.position;
            toEnemy.y = 0; // Keep math on the horizontal plane
            float distance = toEnemy.magnitude;

            // Line of sight check
            Vector3 start = transform.position + Vector3.up * 1f; // Eyes level
            Vector3 end = enemyTransform.position + Vector3.up * 1f; // Target center
            if (Physics.Linecast(start, end, LayerMask.GetMask("Obstacles"))) continue;

            // Calculate alignment between our search direction and the enemy direction
            float alignment = Vector3.Dot(searchDirection, toEnemy.normalized);
            bool inCone = alignment >= targetConeThreshold;

            if (bestTarget == null)
            {
                bestTarget = enemyTransform;
                bestDistance = distance;
                continue;
            }

            Enemy bestEnemy = bestTarget.GetComponent<Enemy>();
            Vector3 toBest = bestTarget.position - transform.position;
            toBest.y = 0;
            float bestAlignment = Vector3.Dot(searchDirection, toBest.normalized);
            bool bestInCone = bestAlignment >= targetConeThreshold;

            // 1. Aim direction cone (highest priority)
            if (inCone != bestInCone)
            {
                if (inCone)
                {
                    bestTarget = enemyTransform;
                    bestDistance = distance;
                }
                continue;
            }

            // 2. Distance check with tolerance for health prioritization
            float distanceDifference = distance - bestDistance;
            if (Mathf.Abs(distanceDifference) < targetDistanceTolerance)
            {
                // 3. Enemy health (lowest priority)
                if (enemy.CurrentHealth < bestEnemy.CurrentHealth)
                {
                    bestTarget = enemyTransform;
                    bestDistance = distance;
                }
            }
            else
            {
                if (distance < bestDistance)
                {
                    bestTarget = enemyTransform;
                    bestDistance = distance;
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
        bool isInvincible = (iframeTimer > 0f) || (Time.time - lastDashTime < dashInvincibilityDuration);
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

    public void SetControlsEnabled(bool enabledState)
    {
        controlsEnabled = enabledState;
        if (!enabledState)
        {
            dashRequested = false;
            attackRequested = false;
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        SetControlsEnabled(false);
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

    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position + transform.forward;
        Vector2 screenMousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(screenMousePos);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return transform.position + transform.forward;
    }

    private void FireRangedAttack(Vector3 targetPosition)
    {
        if (rangedAttackPrefab == null) return;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 fireDir = targetPosition - spawnPos;
        fireDir.y = 0;
        fireDir.Normalize();

        if (fireDir == Vector3.zero)
        {
            fireDir = transform.forward;
        }

        Hurtbox projectile = Instantiate(rangedAttackPrefab, spawnPos, Quaternion.LookRotation(fireDir));
        projectile.Initialize(gameObject, fireDir);

        Debug.Log("Fired Ranged Attack (Inscribed Gurindam) towards: " + targetPosition);
    }
    #endregion

    void LateUpdate()
    {
        // Reset single-frame triggers
        chargeAttackRelease = false;
        SetAnimBool(AnimChargeAttackRelease, false);
    }

    private void SetAnimBool(int hash, bool value)
    {
        if (animator != null && animatorParamHashes.Contains(hash))
        {
            animator.SetBool(hash, value);
        }
    }

    private void SetAnimTrigger(int hash)
    {
        if (animator != null && animatorParamHashes.Contains(hash))
        {
            animator.SetTrigger(hash);
        }
    }

    private void ResetAnimTrigger(int hash)
    {
        if (animator != null && animatorParamHashes.Contains(hash))
        {
            animator.ResetTrigger(hash);
        }
    }

    private void SetAnimInt(int hash, int value)
    {
        if (animator != null && animatorParamHashes.Contains(hash))
        {
            animator.SetInteger(hash, value);
        }
    }
}
