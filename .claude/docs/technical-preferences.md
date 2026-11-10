# Technical Preferences

<!-- project.yaml at the repo root is the machine-readable source of truth for
     engine, specialists, naming, performance, platform, and testing.framework.
     This file is the human-readable LEGACY FALLBACK: agents and skills resolve
     each key from project.yaml first and fall back here only when the
     project.yaml key is absent. /setup-engine dual-writes both.
     Forbidden patterns and allowed libraries are NOT migrated — they live only
     in this file. Populated by /setup-engine; updated as decisions are made. -->

## Engine & Language

- **Engine**: Unity 6.6 (6000.6.6f1) — installed editor matches the pin (checked 2026-11-07)
- **Language**: C#
- **Rendering**: URP (Universal Render Pipeline)
- **Physics**: PhysX (built-in 3D physics)

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Steam, Windows)
- **Input Methods**: Keyboard/Mouse, Gamepad
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: Partial
- **Touch Support**: None
- **Platform Notes**: First-Person-Koop für 1–4 Spieler (Host-Client). Neues Input System (`com.unity.inputsystem` 1.20.0 steht in `Packages/manifest.json`). Werte aus CLAUDE.md abgeleitet (PC/Steam, First Person), per `/settings` änderbar.

## Naming Conventions

- **Classes**: PascalCase (`PlayerController`)
- **Variables**: camelCase for locals and parameters; private fields `_camelCase` (`_moveSpeed`); public fields and properties PascalCase (`MoveSpeed`)
- **Signals/Events**: PascalCase without `On` (`PhaseChanged`); methods that handle an event are named `On<Event>` (`OnPhaseChanged`). Set by the developer on 2026-11-10 to match ADR-0001 to ADR-0003.
- **Files**: PascalCase, matching the class name (`PlayerController.cs`)
- **Scenes/Prefabs**: PascalCase
- **Constants**: PascalCase (`ShrinkFactor`). Set by the developer on 2026-11-10 to match the existing code (same value as `naming.constants` in `project.yaml`).

## Performance Budgets

- **Target Framerate**: 60 fps
- **Frame Budget**: 16.6 ms
- **Draw Calls**: [TO BE CONFIGURED — measure on target hardware first; no sourced figure for URP]
- **Memory Ceiling**: [TO BE CONFIGURED — measure on target hardware first]

## Testing

- **Framework**: NUnit via Unity Test Framework — Edit Mode tests in `Assets/Tests/EditMode/`, Play Mode tests in `Assets/Tests/PlayMode/`. `com.unity.test-framework` is in `Library/PackageCache` but not listed in `Packages/manifest.json`; `/test-setup` clarifies this. No package was added by `/setup-engine`.
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- `com.unity.netcode.gameobjects` 2.13.3 — Netcode for GameObjects, host-client. Approved 2026-11-07. Stay on 2.13.x while on Unity 6.6: 3.x requires Unity 6000.7. 2.13.0 or later is needed for Enter Play Mode with domain reload disabled.
- `com.unity.multiplayer.playmode` 3.0.0 — Multiplayer Play Mode, editor-only test tool for up to four players. Approved 2026-11-07. On Unity 6.6 it is a built-in package: the Editor resolves 3.0.0 even when the manifest asks for 2.0.2 (`Packages/packages-lock.json`, `"source": "builtin"`).
- `com.unity.multiplayer.tools` 2.2.12 — Network Simulator for the ~100 ms latency tests (CLAUDE.md). Approved 2026-11-08. Unity 6.6 lists this version in its editor package manifest; it is not built in, it comes from the registry.
- `com.coplaydev.unity-mcp` — MCP for Unity, Git package from `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`, **no version pin** (lock: commit `e2fd09c71644d1d9fa1ae134526ce913cbf97536`). Added by the developer, confirmed to stay 2026-11-08.
- `com.ramenvr.aura-unity` — Git package from `https://github.com/ramenvr/AuraForUnityPlugin.git`, **no version pin**. Added by the developer, confirmed to stay 2026-11-08.
- `com.ramenvr.autopilot` — Git package from `https://github.com/ramenvr/autopilot.git?path=/plugin/unity/com.ramenvr.autopilot`, pinned to commit `9e0fbb0f2a5ae18d68d1f1f5d6ec17aceb15a1d5`. Added by the developer, confirmed to stay 2026-11-08.
- `com.unity.transport` 6.6.0 — comes with NGO. Built-in on Unity 6.6; it replaces the 2.6.0 that NGO 2.13.3 declares as a dependency. The project compiles with it (batchmode, 2026-11-08).

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP/HDRP materials)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS, UGUI Canvas, runtime UI)
- **Additional Specialists**: unity-dots-specialist (ECS, Jobs system, Burst compiler), unity-addressables-specialist (asset loading, memory management, content catalogs)
- **Routing Notes**: Invoke primary for architecture and general C# code review. Invoke DOTS specialist for any ECS/Jobs/Burst code. Invoke shader specialist for rendering and visual effects. Invoke UI specialist for all interface implementation. Invoke Addressables specialist for asset management systems. DOTS and Addressables are routing targets only — neither is a dependency of this project.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
