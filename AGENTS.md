# Gurindam Multi-Agent Orchestration & Contribution Guide

## 1. Project Context & Philosophy

**The Origin of Gurindam** is a Unity 6.2 (`6000.2.10f1`) 3D action-roguelite hack-and-slash game. 
- **Core Loop:** The Scholar navigates a fantasy realm shrouded in the dark fog of forgetfulness, wielding the *Arbiter's Brush* (melee staff combos) and *Inscribed Gurindam* (chanted text projectiles) to recover fragments of the *Gurindam Dua Belas*.
- **Death & Progression:** Defeat dissolves the protagonist into liquid ink, returning them to the *Sanctuary of the Pen* (hub area) to interact with Raja Ali Haji and the Putri Tujuh to invest *Heritage Points* into permanent upgrades.
- **Cultural Grounding:** Malay cultural heritage, classical literature, and Nusantara mythology (Cindaku, Gajah Mina, Sang Kelembai, Toyol, Durjana, Londo) are core to the game's identity.
- **Split-License Architecture:**
  - Gameplay code, systems, and tooling are licensed under **GNU GPLv3**.
  - Narrative lore, 3D models, textures, audio, and character designs are licensed under **CC BY-NC-SA 4.0**.

---

## 2. Multi-Agent Orchestration Framework

To achieve high velocity without corrupting Unity project state, agents operate within a coordinated multi-agent orchestration pipeline.

```
                   +----------------------------------+
                   |   Lead Orchestrator (Conductor)  |
                   +-----------------+----------------+
                                     |
         +---------------------------+---------------------------+
         |                           |                           |
         v                           v                           v
+------------------+       +-------------------+       +--------------------+
| Lore & Narrative |       | Technical Systems |       | Unity Asset &      |
| Lorekeeper Agent |       | & Combat Agents   |       | Inspector Guardian |
+------------------+       +-------------------+       +--------------------+
         |                           |                           |
         +---------------------------+---------------------------+
                                     |
                                     v
                   +----------------------------------+
                   |     QA & Verification Agent      |
                   +----------------------------------+
```

### Agent Roles & Specialized Personas

When decomposing tasks, the orchestrator delegates to the following specialized agent roles:

| Role | Domain / Focus | Key Responsibilities & Tools |
| :--- | :--- | :--- |
| **Lead Orchestrator** | Task lifecycle, work breakdown, agent dispatch | Coordinates dependencies, synthesizes subagent reports, enforces overall architecture and user alignment. |
| **Combat & Mechanics Agent** | `Player.cs`, `Hurtbox.cs`, hitboxes, combat math | Controls fluid combo mechanics, brush strikes, ranged text projectiles, frame timings, dodge invulnerability, and hit-stop. |
| **Enemy AI & Encounters Agent** | `Enemy.cs`, `EnemySpawner.cs`, boss logic | Encounters, state machines/behavior trees (Toyol, Durjana, Londo, Cindaku, Gajah Mina), NavMesh pathfinding, telegraph cues. |
| **Progression & Systems Agent** | Hub state, Sanctuary upgrades, save/load | Permanent progression, Heritage Points, death-to-ink transition, inventory/stats, scene transitions, persistence. |
| **Unity Asset & Inspector Guardian** | Prefabs, Scenes, `.meta` files, ScriptableObjects | Protects YAML integrity, prevents GUID breakage, validates serialized field links, audits shader/URP asset settings. |
| **Narrative & Cultural Lorekeeper** | Gurindam text, dialogue, cultural authenticity | Verifies Malay literary tone, accurate representation of Raja Ali Haji / Putri Tujuh, ensures cultural respect and CC BY-NC-SA compliance. |
| **QA & Verification Agent** | EditMode & PlayMode tests, regressions | Writes automated test fixtures, verifies boundary edge cases (pause, respawn, null refs), monitors GC per-frame allocations. |

### Model Allocation & Delegation Matrix

To optimize response latency and token efficiency:
- **Lightweight (`flash` / `flash_lite`):** File reconnaissance, broad codebase search, single-script syntax checks, documentation drafting, boilerplate test generation.
- **High-Capability (`inherit` / `pro`):** Multi-agent orchestration, complex combat state machines, boss AI design, progression serialization, and cross-system architectural refactoring.

---

## 3. Orchestration Lifecycle & Pipeline

Every non-trivial feature or refactor must traverse 5 defined stages:

### Phase 1: Reconnaissance & Discovery (Read-Only)
- Inspect existing scripts, prefabs, ScriptableObjects, and test coverage before writing code.
- Identify core dependencies, interfaces (`Assets/Scripts/Interfaces/`), and potential regression vectors.
- *Strict Rule:* Zero file modifications during this stage.

### Phase 2: Interface & Contract Specification
- Define shared C# interfaces, event delegates, or ScriptableObject schemas first.
- Establish serialized field names and inspector requirements before implementing behavioral logic.
- Ensure that multiple subagents working on parallel components have non-overlapping file scopes.

### Phase 3: Parallelized Specialized Execution
- Agents execute code updates strictly within their assigned domains.
- Never allow concurrent edits to the same `.unity` scene or `.prefab` asset.
- Adhere to Unity runtime performance constraints:
  - Zero heap allocations (`GC.Alloc`) in hot paths (`Update`, `FixedUpdate`, collision loops).
  - Cache component references in `Awake`/`Start` (`GetComponent` calls forbidden in `Update`).
  - Cache raycast/overlap allocation buffers (`NonAlloc` variants preferred).

### Phase 4: Asset & Metadata Synchronization
- Every new `.cs` script or asset must have its corresponding `.meta` file tracked and verified.
- Never delete or recreate `.meta` files arbitrarily; preserving GUIDs prevents broken inspector references in scenes and prefabs.
- If serialized fields are renamed, use `[FormerlySerializedAs("oldName")]` to prevent data loss in existing prefabs and scene instances.

### Phase 5: Verification & Gatekeeping
- Execute EditMode unit tests for pure C# logic and state changes.
- Execute PlayMode integration tests for physics, damage triggers, and spawner routines.
- Report all verified tests, edge-case validations, and any manual inspector setup needed by the user.

---

## 4. Unity-Specific Development Practices

### Serialized Fields & Component Contracts
- Make Inspector dependencies explicit using `[SerializeField] private` instead of `public` fields whenever exposure to other classes is not required.
- Defensively validate required references in `Awake()` or `Reset()` using `Debug.Assert` or conditional logging.
- Maintain serialized field names unless explicit migration via `FormerlySerializedAs` is completed and tested.

### Scene & Prefab YAML Safety
- Avoid raw text edits to `.unity` and `.prefab` files unless strictly necessary (e.g. simple GUID updates).
- When prefab changes are required, document the exact component values and hierarchy adjustments in the final report so the user can verify them in the Unity Editor.

### Editor vs. Runtime Separation
- Keep all editor-only scripts (custom inspectors, asset processors, build scripts) inside an `Editor/` folder or within editor-specific assembly definitions (`Assembly-CSharp-Editor`).
- Runtime scripts must never reference the `UnityEditor` namespace directly or via preprocessor directives unless strictly guarded by `#if UNITY_EDITOR`.

---

## 5. Domain-Specific Edge Cases to Guard

Every change to gameplay, enemy behavior, or progression must explicitly account for:
1. **Death & Respawn Lifecycle:** Defeat must cleanly reset combat states, cancel active projectile coroutines/tweens, and route to `PlayerDeathHandler.cs` without throwing missing reference exceptions.
2. **Pause & Scene Transitions:** All tweens, audio clips, and physics effects must respect `Time.timeScale` or cleanly pause/resume.
3. **Hitbox / Hurtbox Timing:** Invulnerability frames (i-frames) on dodge or hit reaction must prevent multi-damage bugs across overlapping colliders.
4. **Heritage Progression State:** Heritage Points and unlocked abilities must be idempotent and resilient against save/load interruptions.
5. **Cultural Integrity:** Gurindam text, verse recitations, and historical figure dialogues must remain culturally respectful and faithful to Malay literary tradition.

---

## 6. Testing & Quality Verification

- **EditMode Tests:** Required for calculation logic (damage multipliers, combo chains, progression cost formulas, loot tables).
- **PlayMode Tests:** Required for component integration (spawner wave cycles, hurtbox collisions, player death-ink trigger).
- **Verification Rule:** Never claim a test, compile check, or inspector validation passed unless it was actually executed. If a check cannot be run directly in the environment, report it clearly under **Manual Verification Required**.

---

## 7. Version Control & Safety Boundaries

- **Zero Destructive Actions:** Never execute `git reset --hard`, `git clean -fd`, `git checkout --`, branch deletion, or stash drops without explicit user authorization.
- **Preserve Uncommitted Work:** Always verify `git status` before beginning work. Never overwrite or discard existing user modifications.
- **Atomic Commits:** Coordinate changes so that C# code and corresponding `.meta` files are kept together in a clean, buildable state.
