# Unity 6.3 LTS → 6.6 — Deprecated APIs (the 6.4 to 6.6 spans are appended at the end)

**Last verified:** 2026-02-13

Quick lookup table for deprecated APIs and their replacements.
Format: **Don't use X** → **Use Y instead**

---

## Input

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Input.GetKey()` | `Keyboard.current[Key.X].isPressed` | New Input System |
| `Input.GetKeyDown()` | `Keyboard.current[Key.X].wasPressedThisFrame` | New Input System |
| `Input.GetMouseButton()` | `Mouse.current.leftButton.isPressed` | New Input System |
| `Input.GetAxis()` | `InputAction` callbacks | New Input System |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |

**Migration:** Install `com.unity.inputsystem` package.

---

## UI

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Canvas` (UGUI) | `UIDocument` (UI Toolkit) | UI Toolkit is now production-ready |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering, fewer draw calls |
| `Image` component | UI Toolkit `VisualElement` with background | More flexible styling |

**Migration:** UGUI still works, but UI Toolkit is recommended for new projects.

---

## DOTS/Entities

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `ComponentSystem` | `ISystem` (unmanaged) | Entities 1.0+ complete rewrite |
| `JobComponentSystem` | `ISystem` with `IJobEntity` | Burst-compatible |
| `GameObjectEntity` | Pure ECS workflow | No GameObject conversion |
| `EntityManager.CreateEntity()` (old signature) | `EntityManager.CreateEntity(EntityArchetype)` | Explicit archetype |
| `ComponentDataFromEntity<T>` | `ComponentLookup<T>` | Entities 1.0+ rename |

**Migration:** See Entities package migration guide. Major refactor required.

---

## Rendering

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CommandBuffer.DrawMesh()` | RenderGraph API | URP/HDRP render passes |
| `OnPreRender()` / `OnPostRender()` | `RenderPipelineManager` callbacks | SRP compatibility |
| `Camera.SetReplacementShader()` | Custom render pass | Not supported in SRP |

---

## Physics

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Physics.RaycastAll()` | `Physics.RaycastNonAlloc()` | Avoid GC allocations |
| `Rigidbody.velocity` (direct write) | `Rigidbody.AddForce()` | Better physics stability |

---

## Asset Loading

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Resources.Load()` | Addressables | Better memory control, async loading |
| Synchronous asset loading | `Addressables.LoadAssetAsync()` | Non-blocking |

---

## Animation

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Animation component | Animator Controller | Mecanim system |
| `Animation.Play()` | `Animator.Play()` | State machine control |

---

## Particles

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Particle System | Visual Effect Graph | GPU-accelerated, more performant |

---

## Scripting

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |

---

## Platform-Specific

### WebGL
| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| WebGL 1.0 | WebGL 2.0 or WebGPU | Unity 6+ defaults to WebGPU |

---

## Quick Migration Patterns

### Input Example
```csharp
// ❌ Deprecated
if (Input.GetKeyDown(KeyCode.Space)) {
    Jump();
}

// ✅ New Input System
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) {
    Jump();
}
```

### Asset Loading Example
```csharp
// ❌ Deprecated
var prefab = Resources.Load<GameObject>("Enemies/Goblin");

// ✅ Addressables
var handle = Addressables.LoadAssetAsync<GameObject>("Enemies/Goblin");
await handle.Task;
var prefab = handle.Result;
```

### UI Example
```csharp
// ❌ Deprecated (UGUI)
GetComponent<Text>().text = "Score: 100";

// ✅ TextMeshPro
GetComponent<TextMeshProUGUI>().text = "Score: 100";

// ✅ UI Toolkit
rootVisualElement.Q<Label>("score-label").text = "Score: 100";
```

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/deprecated-features.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Migration.html

---

# Appended spans: 6.3 LTS → 6.4 → 6.5 → 6.6

**Last verified:** 2026-11-07 (added by `/setup-engine`). Sections above are the 2022 LTS → 6.3 LTS
tables and were **not** re-verified on this date. Source pages and the list of gaps
(`NOT SOURCEABLE`) are at the end of the matching block in `breaking-changes.md`.
Format: **Don't use X** → **Use Y instead**.

## Removed in 6.5 (compile errors in 6.5 and later)

### Identity: `InstanceID` → `EntityId`

| Removed | Replacement | Notes |
|---------|-------------|-------|
| `go.GetInstanceID()` and other `InstanceID` APIs | `go.GetEntityId()` | Error messages point to the replacement. |
| `int` as the type of an object identity | `EntityId` | `EntityId` is 8 bytes; a cast to `int` loses data. |
| `EntityId.GetHashCode()` as a dictionary key | use `EntityId` itself as the key | Stated in the 6.5 guide. |
| `EntityId.FromULong(int.Parse(s))` on a stored `ToString()` | parse as `ulong` | 6.2–6.4 stored 32 bit, 6.5 stores 64 bit. |

### `GameObject` / `Component` shortcut properties

Every property below existed on both `GameObject` and `Component`. Use `GetComponent<T>()`.

| Removed | Replacement |
|---------|-------------|
| `.rigidbody` | `GetComponent<Rigidbody>()` |
| `.rigidbody2D` | `GetComponent<Rigidbody2D>()` |
| `.collider` | `GetComponent<Collider>()` |
| `.collider2d` | `GetComponent<Collider2D>()` |
| `.camera` | `GetComponent<Camera>()` |
| `.light` | `GetComponent<Light>()` |
| `.renderer` | `GetComponent<Renderer>()` |
| `.audio` | `GetComponent<AudioSource>()` |
| `.animation` | `GetComponent<Animation>()` |
| `.constantForce` | `GetComponent<ConstantForce>()` |
| `.hingeJoint` | `GetComponent<HingeJoint>()` |
| `.particleSystem` | `GetComponent<ParticleSystem>()` |
| `.networkView` | `GetComponent<NetworkView>()` |
| `GameObject.AddComponent(string)` | `GameObject.AddComponent<T>()` |
| `GameObject.SampleAnimation(AnimationClip, float)` | `AnimationClip.SampleAnimation(GameObject, float)` |
| `GameObject.PlayAnimation(Object)` / `StopAnimation()` | `animation.Play()` / `animation.Stop()` |

### `ModelImporter`

| Removed | Replacement |
|---------|-------------|
| `isFileScaleUsed` | `useFileScale` |
| `normalImportMode` | `importNormals` |
| `optimizeMesh` | `optimizeMeshPolygons` and/or `optimizeMeshVertices` |
| `resampleRotations` | `resampleCurves` |
| `splitTangentsAcrossSeams` | `tangentImportMode` |
| `tangentImportMode` | `importTangents` |

### Entities (only if the DOTS packages are used)

| Removed | Replacement |
|---------|-------------|
| `Entities.ForEach` | `IJobEntity` and `SystemAPI.Query` |
| `Job.WithCode` | `IJob` |
| `IAspect` | `Component` and `EntityQuery` APIs |

Guide: https://docs.unity3d.com/Packages/com.unity.entities@6.5/manual/upgrade-guide.html

## Removed or deprecated in 6.4

| Deprecated / removed | Replacement | Notes |
|----------------------|-------------|-------|
| URP Compatibility Mode, define `URP_COMPATIBILITY_MODE` | Render Graph system for custom render passes | Removed in 6.4. |
| `IVolume` interface | `VolumeGizmoDrawer` class | For custom volume types. |
| `LightShadowCasterMode.NonLightmappedOnly` | `LightShadowCasterMode.ShadowMask` | Auto-updated. |
| `LightShadowCasterMode.Everything` | `LightShadowCasterMode.DistanceShadowMask` | Auto-updated. |
| manual `OnDisable` propagation to deep descendants | nothing, `Object.Destroy` does it now | Otherwise duplicate calls. |

## Deprecated or removed in 6.5 / 6.6

| Deprecated / removed | Replacement | Notes |
|----------------------|-------------|-------|
| Built-In Render Pipeline | URP | Deprecated in 6.5, supported through the 6.7 LTS lifecycle. |
| Shader Graph Reflection Probe Node | URP Forward/Deferred: Custom Function Node with `Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);` | Inputs `ViewDir` (Vector3), `Normal` (Vector3), `LOD` (Float). |
| Dynamic batching | see Unity manual "Choose a method for optimizing calls" | Deprecated in 6.5, obsolete in 6.6. No replacement API is named. |
| Progressive CPU Lightmapper | Unity Compute Light Baker or Progressive GPU Lightmapper | Still works, will be removed. |
| `UxmlFactory`, `UxmlTraits` | `UxmlElement`, `UxmlAttribute` | Removed in 6.6. |
| `UIToolkitInputConfiguration.SetRuntimeInputBackend`, `DefaultEventSystem.LegacyInputProcessor` | nothing, remove the call | Compile error in 6.6; it had no effect. |
| Rendering Debugger `DebugState*`, `DebugStateAttribute`, legacy `DebugUIDrawer` | `ISerializedDebugDisplaySettings` | Compile error in 6.6. |
| `BuildOptions.ForceEnableAssertions` | `PlayerSettings.SetManagedCodeVariant(namedBuildTarget, ManagedCodeVariant.Checked)` | `[Obsolete]`. |
| `CoreBuildData.developmentBuild` | `CoreBuildData.useDiagnosticChecks` | `[Obsolete]`, always returns `false`. |
| Cinemachine 2 | Cinemachine 3 (`com.unity.cinemachine`, core package) | Auto-upgraded; different API. |

### Scripting symbols (deprecated in 6.6, removed in 6.8)

| Deprecated | Replacement |
|------------|-------------|
| `UNITY_64` | a runtime check, e.g. `IntPtr.Size == 4` instead of `#if !UNITY_64 && UNITY_ANDROID` |
| `DEVELOPMENT_BUILD` (safety checks, assertions) | `UNITY_ENABLE_CHECKS` |
| `DEVELOPMENT_BUILD` (profiling, diagnostic logging) | `UNITY_INCLUDE_INSTRUMENTATION` |
| `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` | `#if UNITY_EDITOR \|\| UNITY_ENABLE_CHECKS` |
| `DEVELOPMENT_BUILD` to keep a log out of non-development builds | `if (Debug.isDebugBuild)` at run time |

Package authors who must also support older Unity gate on `UNITY_6000_6_OR_NEWER`.

## Not relevant to this project (PC only)

Android: `PlayerSettings.openGLRequireES31` (deprecated, getter always `true`), `PlayerSettings.Android.startInFullscreen` (obsolete), `AndroidAssetPacks.RequestToUseMobileDataAsync` (use `ShowConfirmationDialogAsync`). iOS: `ReplayKit` removed. XR: VR module and Magic Leap removed, HoloLens 2 deprecated. 2D physics: `LowLevelPhysics2D` → `PhysicsCore2D`.

**Sources (all fetched 2026-11-07):** see the end of the "Appended spans" block in `breaking-changes.md`.
