# Unity 6.3 LTS → 6.6 — Current Best Practices (the 6.4 to 6.6 block is appended at the end)

**Last verified:** 2026-02-13

Modern Unity 6 patterns that may not be in the LLM's training data.
These are production-ready recommendations as of Unity 6.3 LTS.

---

## Project Setup

### Use Unity 6.3 LTS for Production
- **Supported Update releases** (6.4+): Latest features, supported only until the next one ships
- **LTS** (6.3): Production-ready, 2-year support (until Dec 2027)

### Choose the Right Render Pipeline
- **URP (Universal)**: Mobile, cross-platform, good performance ✅ Recommended for most games
- **HDRP (High Definition)**: High-end PC/console, photorealistic
- **Built-in**: Deprecated, avoid for new projects

---

## Scripting

### Use C# 9+ Features (Unity 6 Supports C# 9)

```csharp
// ✅ Record types for data
public record PlayerData(string Name, int Level, float Health);

// ✅ Init-only properties
public class Config {
    public string GameMode { get; init; }
}

// ✅ Pattern matching
var result = enemy switch {
    Boss boss => boss.Enrage(),
    Minion minion => minion.Flee(),
    _ => null
};
```

### Async/Await for Asset Loading

```csharp
// ✅ Modern async pattern
public async Task<GameObject> LoadEnemyAsync(string key) {
    var handle = Addressables.LoadAssetAsync<GameObject>(key);
    return await handle.Task;
}
```

### Use Source Generators for Serialization (Unity 6+)

```csharp
// ✅ Source-generated serialization (faster, less reflection)
[GenerateSerializer]
public partial struct PlayerStats : IComponentData {
    public int Health;
    public int Mana;
}
```

---

## DOTS/ECS (Production-Ready in Unity 6.3 LTS)

### Use ISystem (Not ComponentSystem)

```csharp
// ✅ Modern unmanaged ISystem (Burst-compatible)
public partial struct MovementSystem : ISystem {
    public void OnCreate(ref SystemState state) { }

    public void OnUpdate(ref SystemState state) {
        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>()) {
            transform.ValueRW.Position += speed.ValueRO.Value * SystemAPI.Time.DeltaTime;
        }
    }
}
```

### Use IJobEntity for Parallel Jobs

```csharp
// ✅ IJobEntity (replaces IJobForEach)
[BurstCompile]
public partial struct DamageJob : IJobEntity {
    public float DeltaTime;

    void Execute(ref Health health, in DamageOverTime dot) {
        health.Value -= dot.DamagePerSecond * DeltaTime;
    }
}

// Schedule it
var job = new DamageJob { DeltaTime = SystemAPI.Time.DeltaTime };
job.ScheduleParallel();
```

---

## Input

### Use Input System Package (Not Legacy Input)

```csharp
// ✅ Input Actions (rebindable, cross-platform)
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour {
    private PlayerControls controls;

    void Awake() {
        controls = new PlayerControls();
        controls.Gameplay.Jump.performed += ctx => Jump();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();
}
```

Create Input Actions asset in editor, generate C# class via inspector.

---

## UI

### Use UI Toolkit for Runtime UI (Production-Ready in Unity 6)

```csharp
// ✅ UI Toolkit (replaces UGUI for new projects)
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour {
    void OnEnable() {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var playButton = root.Q<Button>("play-button");
        playButton.clicked += StartGame;

        var scoreLabel = root.Q<Label>("score");
        scoreLabel.text = $"High Score: {PlayerPrefs.GetInt("HighScore")}";
    }
}
```

**UXML** (UI structure) + **USS** (styling) = HTML/CSS-like workflow.

---

## Asset Management

### Use Addressables (Not Resources)

```csharp
// ✅ Addressables (async, memory-efficient)
using UnityEngine.AddressableAssets;

public async Task SpawnEnemyAsync(string enemyKey) {
    var handle = Addressables.InstantiateAsync(enemyKey);
    var enemy = await handle.Task;

    // Cleanup: release when destroyed
    Addressables.ReleaseInstance(enemy);
}
```

**Benefits:** Async loading, remote content delivery, better memory control.

---

## Rendering

### Use RenderGraph API for Custom Passes (URP/HDRP)

```csharp
// ✅ RenderGraph API (Unity 6+)
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("My Pass", out var passData)) {
        // Setup pass
        builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
            // Execute commands
        });
    }
}
```

**Replaces:** Old `CommandBuffer.Execute()` pattern.

**This is URP's API.** `RecordRenderGraph` overrides URP's `ScriptableRenderPass`.
HDRP custom passes are a separate API: a `CustomPass` subclass, run from a Custom
Pass Volume, overrides `Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)`,
`Execute(CustomPassContext ctx)` and `Cleanup()` (HDRP 17.3, *Create a Custom
Pass in a C# script*:
https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.3/manual/Custom-Pass-Scripting.html).

---

## Performance

### Use Burst Compiler + Jobs System

```csharp
// ✅ Burst-compiled job (massive performance gain)
[BurstCompile]
struct ParticleUpdateJob : IJobParallelFor {
    public NativeArray<float3> Positions;
    public NativeArray<float3> Velocities;
    public float DeltaTime;

    public void Execute(int index) {
        Positions[index] += Velocities[index] * DeltaTime;
    }
}

// Schedule
var job = new ParticleUpdateJob {
    Positions = positions,
    Velocities = velocities,
    DeltaTime = Time.deltaTime
};
job.Schedule(positions.Length, 64).Complete();
```

**20-100x faster** than equivalent C# code.

---

### Use GPU Instancing for Repeated Objects

```csharp
// ✅ GPU Instancing (thousands of objects, minimal draw calls)
Graphics.RenderMeshInstanced(
    new RenderParams(material),
    mesh,
    0,
    matrices // NativeArray<Matrix4x4>
);
```

---

## Memory Management

### Use NativeContainers (Not Managed Arrays in Jobs)

```csharp
// ✅ NativeArray (no GC, Burst-compatible)
NativeArray<int> data = new NativeArray<int>(1000, Allocator.TempJob);
// ... use in job
data.Dispose(); // Manual cleanup required

// ✅ Or use using statement
using var data = new NativeArray<int>(1000, Allocator.TempJob);
// Auto-disposed
```

---

## Multiplayer

### Use Netcode for GameObjects (Official)

```csharp
// ✅ Unity's official netcode
using Unity.Netcode;

public class Player : NetworkBehaviour {
    private NetworkVariable<int> health = new NetworkVariable<int>(100);

    [ServerRpc]
    public void TakeDamageServerRpc(int damage) {
        health.Value -= damage;
    }
}
```

**Replaces:** UNet (deprecated), MLAPI (renamed to Netcode for GameObjects).

---

## Testing

### Use Unity Test Framework (NUnit-based)

```csharp
// ✅ Play Mode Test
[UnityTest]
public IEnumerator Player_TakesDamage_HealthDecreases() {
    var player = new GameObject().AddComponent<Player>();
    player.Health = 100;

    player.TakeDamage(25);
    yield return null; // Wait one frame

    Assert.AreEqual(75, player.Health);
}
```

---

## Command Line — Tests

Documented (Unity Manual, *Run tests from the command line*; Test Framework
command-line reference):
`<Unity editor> -runTests -batchmode -projectPath <path> -testPlatform <platform> -testResults <file>`.
`-batchmode` removes the need for user input. `-testPlatform` takes `EditMode`,
`PlayMode` (Play Mode tests in the Editor) or a player platform; with no value,
tests run in Edit Mode. `-testResults` defaults to the project root, and the
file follows NUnit's XML format: the `<test-run>` element carries
`testcasecount`, `result`, `passed` and `failed`, and each `<test-case>` a
`fullname` and a `result` of `Passed`, `Failed`, `Inconclusive` or `Skipped`.
Unity defines no common exit codes for test runs.

Observed on Unity 6000.3.23f1 — not in the docs:
- `-runTests` exits 0 when every test passes and 2 when one fails; 1 is a
  compile error (no results file is written), 3 a failed run, 4 an unknown
  `-testPlatform`. It exits by itself — no `-quit`.
- Tests outside `Assets/` are never compiled: the run reports
  `testcasecount="0"`, `result="Passed"` and exits 0 — a false pass.
- Without the `com.unity.test-framework` package, `-runTests` hangs rather
  than failing.
- `Unity` on `PATH` may be Unity's separate CLI, not the editor; it rejects
  `-batchmode` and `-version` with exit 2. Name the editor executable by its
  full path.
- `-batchmode -quit -projectPath . -logFile -` exits 1 on a compile error and
  0 when clean.

Sources:
- Run tests from the command line: https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/run-tests-from-command-line.html
- Test Framework command-line arguments: https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html
- NUnit test result XML format: https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html

---

## Debugging

### Use Logging Best Practices

```csharp
// ✅ Structured logging (Unity 6+)
using UnityEngine;

Debug.Log($"Player {playerName} scored {score} points");

// ✅ Conditional compilation for debug code
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.DrawRay(transform.position, direction, Color.red);
#endif
```

---

## Summary: Unity 6 Tech Stack

| Feature | Use This (2026) | Avoid This (Legacy) |
|---------|------------------|----------------------|
| **Input** | Input System package | `Input` class |
| **UI** | UI Toolkit | UGUI (Canvas) |
| **ECS** | ISystem + IJobEntity | ComponentSystem |
| **Rendering** | URP + RenderGraph | Built-in pipeline |
| **Assets** | Addressables | Resources |
| **Jobs** | Burst + IJobParallelFor | Coroutines for heavy work |
| **Multiplayer** | Netcode for GameObjects | UNet |

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/BestPracticeGuides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/index.html

---

# Appended: 6.3 LTS → 6.6

**Last verified:** 2026-11-07 (added by `/setup-engine`). The sections above are the 6.3 LTS
recommendations and were **not** re-verified on this date. Where they conflict with this block,
this block wins for this project. Gaps are listed as `NOT SOURCEABLE`; do not fill them from memory.

## Project setup (this project)

- **This project deliberately runs Unity 6.6 (6000.6.6f1), not the 6.3 LTS recommended above.** 6.6 is a Supported Update release, supported until 6.7 ships. The plan in CLAUDE.md is to move to the 6.7 LTS when it is released. The 6.6 upgrade guide says `UNITY_64` and `DEVELOPMENT_BUILD` are removed in 6.8, so new code should not use them.
- Packages resolved in `Packages/manifest.json` on 2026-11-07: `com.unity.render-pipelines.universal` 17.6.0, `com.unity.inputsystem` 1.20.0, `com.unity.ugui` 2.6.0. No Netcode or Multiplayer packages yet.

## Scripting

### Domain reload is off in new 6.6 projects [SUMMARY]
- The "What's new in 6.6" summary says new projects default to **Reload Scene only** in Enter Play Mode Settings. The verbatim part confirms the consequence: Project Auditor ships an "auto-statics cleanup" analyzer (`UAL` prefix) with attributes spelled `AutoStaticsCleanup` / `NoAutoStaticsCleanup` there (the 6.5 summary spells the second `NoAutostaticsCleanup`; use the 6.6 spelling and check it in the compiler).
- `NOT SOURCEABLE` — what those attributes do exactly. Read the Project Auditor documentation before using them.
- **Project rule (CLAUDE.md):** reset every static field in a method marked `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

### Diagnostics follow the Managed Code Variant (6.6)
```csharp
// ❌ Deprecated in 6.6, removed in 6.8
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    ValidateState();
#endif

// ✅
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
    ValidateState();
#endif

// ✅ A log that must not ship in non-development builds
if (Debug.isDebugBuild) Debug.Log("...");
```
Set **Managed Code Variant = Checked** (Project Settings → Player → Other Settings) for builds that should keep the checks; the default **Release** variant strips them.

### `Dictionary` fields serialize (6.6)
`[SerializeField] private Dictionary<string, int> _counts;` is now serialized by Unity in MonoBehaviours, ScriptableObjects and `[Serializable]` types [SUMMARY for the exact places; the upgrade guide confirms the field case]. Wrappers and subclasses of `Dictionary` still work as ordinary serialized classes.

### Object identity is `EntityId` (6.5)
Use `EntityId` (8 bytes), never `int`, for object identity. Never put it in an `int` field, never use its hash code as a key, never round-trip it through `int.Parse`. (Advice, not from the guides: for identity that travels between machines, use the netcode layer's own ids rather than `EntityId` values.)

### Destroying objects (6.4)
`Object.Destroy` now calls `OnDisable` on components of all descendants. Do not write helper code that calls `OnDisable` manually on children.

## Rendering (URP)

- Custom render passes use the **Render Graph** API; URP Compatibility Mode is gone (6.4).
- Dynamic batching is obsolete (6.6). The 6.4 Rendering Statistics window shows draw calls saved by the SRP Batcher, GPU Resident Drawer, `BatchRendererGroup` and GPU instancing; use it to check batching. (Advice, not from the guides: rely on the SRP Batcher and GPU Resident Drawer instead of dynamic batching.)
- URP: `additionalLightsShadowResolutionTier` on `UniversalAdditionalLightData` changes point/spot shadow resolution at run time (6.5, verbatim).
- Reading depth from GPU memory through an input attachment is available in URP on DirectX 12 and Vulkan only (6.6, verbatim).
- Lightmapping: prefer Unity Compute Light Baker or Progressive GPU Lightmapper; the Progressive CPU Lightmapper is deprecated.

## Physics

- **Mesh Collider needs Mesh Read/Write enabled in the Editor** (6.6, verbatim). A build fails if it is off.
- `NOT SOURCEABLE` — built-in PhysX changes in 6.4–6.6 (none listed in the guides; that is not proof of none). Rigidbody tuning for heavy, wobbly objects must be checked in the Editor, not taken from this file.
- Not for this project unless DOTS is adopted: the Unity Physics package is a core package since 6.5 with a Direct Solver option [SUMMARY].

## Input

- New Input System only (`com.unity.inputsystem` 1.20.0). 6.4 adds `OnMouseDown`, `OnMouseDrag`, `OnMouseUp` for MonoBehaviours when the Input System is active (verbatim).
- `NOT SOURCEABLE` — other Input System changes in 6.4–6.6.

## Multiplayer

- `NOT SOURCEABLE` — Netcode for GameObjects and Multiplayer Play Mode in 6.4–6.6. Neither is installed yet. Read the package documentation for the exact installed version when the NGO step starts, and add its version to this file then.
- Unity's own Base Class Library targeting packs (`System.Text.Json`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`) override same-named assemblies that a plug-in ships (6.5). Keep this in mind when adding third-party networking or Steam libraries.

## Editor, workflow and version control

- **YAML format change in 6.6:** word wrapping is off and "Reduce Version Control Noise" is removed. Assets created by older versions show unrelated YAML diffs; reserialize once (`AssetDatabase.ForceReserializeAssets`) in a separate commit.
- The new Hierarchy window is the default (6.6).
- Editor.log is `ProjectName/Logs/Editor.log` by default (6.5). Tools that read the old global log must be changed, or use `-useGlobalLog`.
- Cinemachine is a core package at version 3 (6.6). Do not add Cinemachine 2 examples.
- Project Auditor is built into the Editor (6.4).

## Command line (verified against the 6.6 Editor command-line reference)

- Batchmode cannot open a project that is already open in an Editor: "only a single instance of Unity can run at a time". Close the Editor before `commands.test`, `commands.smoke` or `commands.build`.
- `-buildTarget` selects the active build target the Editor launches in; valid values include `win64`, `win`, `linux64`, `osxuniversal`, `android`, `ios`, `webgl`. `-buildWindows64Player <path.exe>` builds a 64-bit Windows player. The page does not say whether these flags switch the active platform.
- `-runTests` with `-testPlatform EditMode|PlayMode` and `-testResults <path>`. **Do not combine `-runTests` with `-quit`:** the Editor then quits before the tests complete.
- `-logFile -` writes the log to the console. `-version` prints the Editor version without launching it.

**Sources (all fetched 2026-11-07):**
- the six pages listed at the end of the "Appended spans" block in `breaking-changes.md`
- https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/command-line-arguments/editor
- https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/reference-command-line
