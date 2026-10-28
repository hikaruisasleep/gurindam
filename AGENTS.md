# Gurindam Contribution Guide

## Project context

The Origin of Gurindam is a Unity 6.2 (`6000.2.10f1`) 3D action-roguelite hack-and-slash game. Gameplay code is primarily in `Assets/Scripts`; game scenes are in `Assets/Scenes`. The project uses C# and Unity's Test Framework.

The game combines fast combat, ink-based weapons and projectiles, permanent Sanctuary progression, and culturally grounded narrative content. Treat existing gameplay values, progression, and lore as intentional unless a task explicitly authorizes changing them.

## Engineering principles

- Prioritize correctness, clarity, and maintainability over clever abstractions.
- Prefer the simplest design that satisfies the gameplay requirements.
- Keep changes focused on the requested task. Do not perform unrelated refactors or add unnecessary dependencies.
- Reuse the existing architecture and established component boundaries where possible. Do not introduce a new system when a small extension of an existing one is sufficient.
- Do not silently alter gameplay rules, balance values, player-facing behavior, cultural content, or narrative logic.

## Before making changes

1. Read the relevant scripts, scenes, prefabs, ScriptableObjects, tests, and project settings before proposing an implementation.
2. Identify the existing behavior, dependencies, and edge cases that must not regress. For gameplay changes, explicitly consider death/respawn, pause and scene transitions, damage and invulnerability timing, null or missing references, repeated input/events, and save/progression state when applicable.
3. Explain the proposed approach before making a significant change, including affected systems, preserved behavior, and any trade-offs.
4. Ask for clarification before implementation when a gameplay rule, balance decision, expected player experience, or cultural/narrative requirement is ambiguous. Do not fill in such gaps by changing rules on your own.

## Unity-specific practices

- Preserve serialized field names and component contracts unless a migration is deliberately included and verified.
- Make Inspector dependencies explicit and validate required references defensively where appropriate.
- Avoid hand-editing generated Unity files, package lock files, or scene/prefab YAML unless that is necessary for the requested task and the resulting asset changes can be verified in Unity.
- Keep runtime code separate from editor-only code and place editor code in an `Editor` folder or editor-only assembly as appropriate.
- Avoid per-frame allocations and unnecessary work in `Update`, `FixedUpdate`, and other hot paths, but favor readable code over premature optimization.

## Tests and verification

- Add or update regression tests for behavior changes. Use Unity EditMode tests for isolated logic and PlayMode tests for scene, component, physics, animation, or input integration.
- Run the relevant tests and project checks after changes. Include at least the affected test suite and a compile/editor check when available.
- Never claim a test or check passed unless it was actually run. Report checks that could not be performed and why.
- In the final report, state the implementation summary, tests/checks run and their results, assumptions, known limitations, and remaining risks.

## Version control and safety

- Never commit, push, reset, delete, or otherwise perform destructive actions without explicit user approval.
- Preserve unrelated uncommitted work. Do not overwrite user changes.
