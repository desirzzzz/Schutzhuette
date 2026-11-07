# Unity 6.3 LTS → 6.6 — Breaking Changes (the 6.4 to 6.6 spans are appended at the end)

**Last verified:** 2026-02-13

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html

---

# Appended spans: 6.3 LTS → 6.4 → 6.5 → 6.6

**Last verified:** 2026-11-07 (added by `/setup-engine` when the project pinned Unity 6000.6.6f1).
The sections above describe 2022 LTS → 6.3 LTS and were **not** re-verified on this date.
Everything below comes from pages fetched on 2026-11-07; the URLs are at the end of this
block. Items tagged **[SUMMARY]** come from the part of a "What's new" page that the fetch
tool returned only as a model-written summary, not verbatim. Verify those before relying on them.
Unity says to follow the upgrade guides in release order.

## 6.3 LTS → 6.4

Upgrade guide sections: Assets and media, Graphics, Optimization, Platforms (iOS), XR.

**Relevant to a PC / URP / 3D-physics / multiplayer project**
- **URP Compatibility Mode removed.** Custom render passes must use the Render Graph system. The scripting define `URP_COMPATIBILITY_MODE` is removed too.
- **`Object.Destroy` now calls `OnDisable` on components of ALL descendant GameObjects**, not only the object and its direct children (same as `Object.DestroyImmediate`). A manual workaround that propagated `OnDisable` deeper now causes duplicate calls and must go.
- **Asset Database artifact dependencies narrowed.** A dependent reimports only when the dependency's *import result* changes, not when its source changes. Some `ScriptedImporter` / `AssetPostprocessor` classes may stop running; add `AssetImportContext.DependsOnSourceAsset` and/or `DependsOnArtifact`.
- **SRP Core → `com.unity.modules.physics` is now a soft dependency.** The module stays enabled by default but can be disabled (local volumes then stop working). A package that uses Physics must declare `com.unity.modules.physics` itself. The `IVolume` interface is removed; custom volume types use the new `VolumeGizmoDrawer` class.
- **`LightShadowCasterMode` renamed:** `NonLightmappedOnly` → `ShadowMask`, `Everything` → `DistanceShadowMask` (scripts are auto-updated).
- **Project Auditor is built into the Editor.** Some of its APIs became internal (e.g. `CodeModuleInstructionAnalyzer`, `InstructionAnalysisContext`); custom analyzers built on them stop working.
- **`InstanceID` is being replaced by `EntityId`** (What's new page): `InstanceID` methods are marked deprecated in 6.4 and removed in 6.5 (see below).
- Build Profiles: for new projects the platform list is hidden by default (Project Settings → Editor → Build Profiles → "Hide Classic Platforms" re-enables it).
- Animator Controllers: new option "Evaluate Entry Transitions On Start" — true for new controllers, false for existing ones.

**Not relevant to this project (listed so nobody is surprised):** iOS/iPadOS/tvOS/visionOS runtime library paths changed (`UnityRuntime.framework`); HoloLens 2 support deprecated (use `com.unity.xr.openxr`); PVRTC texture compression removed.

## 6.4 → 6.5

Upgrade guide sections: 2D, Editor and workflow, Graphics, Optimization, Platforms, Programming, XR.

**Relevant to a PC / URP / 3D-physics / multiplayer project**
- **Obsolete `InstanceID` APIs now give compile errors; `EntityId` replaces them.** `int instanceID = go.GetInstanceID();` becomes `EntityId entityId = go.GetEntityId();`.
- **`EntityId` is 8 bytes.** Casting it to `int` loses data. Do not store it in `int` fields or dictionary keys typed `int`, do not use `EntityId.GetHashCode()` as a dictionary key, and do not parse a serialized `EntityId.ToString()` with `int.Parse` (6.2–6.4 stored 32 bit, 6.5 stores a 64-bit `ulong`). This matters for any save/network code that sends object identity.
- **Old shortcut properties on `GameObject` / `Component` were removed** (`rigidbody`, `collider`, `camera`, `renderer`, `AddComponent(string)` and others). Full list in `deprecated-apis.md`.
- **`ModelImporter` obsolete properties are now compile errors** (table in `deprecated-apis.md`).
- **Render Pipeline Core no longer depends on `com.unity.ugui`.** A project that relied on uGUI through URP must add `com.unity.ugui` itself. (This project's `Packages/manifest.json` lists `com.unity.ugui` 2.6.0 explicitly.)
- **Legacy Render Graph compiler removed.** URP/HDRP projects on the Render Graph API with native render pass support are not affected; only custom SRPs on the legacy API.
- **Rendering Debugger now uses UI Toolkit** instead of uGUI/IMGUI. Custom control classes based on uGUI were removed. The old `DebugState` serialization is removed in 6.6.
- **Built-In Render Pipeline is deprecated**, still supported through the whole 6.7 LTS lifecycle. This project uses URP.
- **Editor.log is project-local by default** (`ProjectName/Logs/Editor.log`). Scripts that read the old global location must change. `-useGlobalLog` or Preferences → "Use Global Editor Log" restores the old behavior. Inside Unity use `Application.consoleLogPath`.
- **Unity's own Base Class Library extensions win.** Targeting packs for `System.Text.Json`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe` are built in; user-supplied copies of these assemblies, even other versions, are dropped from the build.
- **Entities:** `Entities.ForEach`, `Job.WithCode`, `IAspect` removed (see `deprecated-apis.md`).
- Shader Graph: Reflection Probe Node deprecated (not correct on HDRP, URP Forward+ and Deferred+).
- Only for a custom `RequestLightsDelegate` (custom light baker): punctual lights rendered too bright by a factor of pi; apply a pi correction to punctual light intensity.

**Not relevant to this project:** 2D physics API renamed `LowLevelPhysics2D` → `PhysicsCore2D` (namespace `Unity.U2D.Physics`, auto-migrated); Android minimum API level 26, x86-64 removed, Gradle 9.1.0 / AGP 9.0.0, `Screen.fullScreen` no longer controls the Android navigation bar; iOS `ReplayKit` removed; VR module removed; Magic Leap support removed.

## 6.5 → 6.6 (the project's version)

Upgrade guide sections: Cameras, Editor and workflow, Graphics, Optimization, Platforms (Android), Programming, UI Toolkit.

**Relevant to a PC / URP / 3D-physics / multiplayer project**
- **`UNITY_64` and `DEVELOPMENT_BUILD` scripting symbols are deprecated and will be removed in Unity 6.8.** Diagnostics now follow a per-platform **Managed Code Variant** setting (Project Settings → Player → Other Settings). `UNITY_ENABLE_CHECKS` replaces the safety-check meaning of `DEVELOPMENT_BUILD`; `UNITY_INCLUDE_INSTRUMENTATION` replaces the profiling meaning. A default Release-variant Development Build no longer contains the checks. Set **Checked** (or **Debug** for stepping in a debugger) to keep them. Roslyn warnings: `UAC0008` (`UNITY_64`), `UAC0009` (`DEVELOPMENT_BUILD`). Code patterns are in `deprecated-apis.md`.
- **YAML serialization changed.** Word wrapping in YAML text files is off, and the **Reduce Version Control Noise** option is removed (`serializeInlineMappingsOnOneLine` is a no-op, its getter returns `true`). Expect YAML diffs unrelated to your edits in assets created by older versions; Unity suggests reserializing once with `ForceReserializeAssets`. This project is under Git, so do that in one dedicated commit.
- **Dynamic batching removed** (obsolete). Unity points to its "Choose a method for optimizing calls" manual page.
- **Cinemachine 3 is a core package** (`com.unity.cinemachine`, part of the Editor). Cinemachine 2 projects are auto-upgraded to 3 (different API and data format). Staying on 2 needs a local copy in the manifest.
- **`Dictionary<TKey,TValue>` fields marked `[SerializeField]` are now serialized.** Subclasses and wrappers of `Dictionary` stay ordinary serialized classes. Migrating an existing custom solution is manual.
- **Mesh Read/Write is now required in the Editor for Mesh Collider** (also Particle System Shape and Terrain Detail Mesh). Before, it was switched on at build time; now the build fails if it is off. (What's new page, verbatim part.)
- **Render Pipeline Core no longer depends on `com.unity.modules.terrain`.** Add the module via Package Manager → Built-in if you use Terrain through URP.
- **Rendering Debugger legacy state classes** (`DebugState`, `DebugStateBool/Int/Float/Color/Vector`, `DebugStateAttribute`, legacy `DebugUIDrawer`) are `[Obsolete(..., true)]` → compile errors. Use `ISerializedDebugDisplaySettings`.
- **Surface shader code generation uses the caching preprocessor.**
- **Progressive CPU Lightmapper is deprecated** (works, will be removed). Use Unity Compute Light Baker or Progressive GPU Lightmapper; baked results may differ.
- **UI Toolkit:** `UxmlFactory`/`UxmlTraits` removed (use `UxmlElement` / `UxmlAttribute`); `DefaultEventSystem.LegacyInputProcessor` removed, and calling `UIToolkitInputConfiguration.SetRuntimeInputBackend` is now a compile error (remove the call, it had no effect).
- **New Hierarchy window is on by default** (the legacy window can be re-enabled).
- **Performance Testing API (`com.unity.test-framework.performance`) is a core package.**
- **Play Mode no longer reloads the domain by default in new projects** [SUMMARY]. The verbatim part of the page confirms the consequence: Project Auditor got an "auto-statics cleanup" analyzer (`UAL` prefix) "for the new default where domain reload is off". This project's CLAUDE.md already requires resetting statics (see `current-best-practices.md`).
- **Unity Physics (DOTS) simulation integrity checks** are compiled in only for the Debug/Checked variants; override with `UNITY_PHYSICS_DISABLE_INTEGRITY_CHECKS`. Only relevant if the DOTS Unity Physics package is used.

**Not relevant to this project:** Android — Legacy and Round icons removed (Adaptive only), `legacy` option of `-androidChainedSignalHandlerBehavior` removed, minimum OpenGL ES raised from 3.0 to 3.1 (`PlayerSettings.openGLRequireES31` deprecated).

## NOT SOURCEABLE — gaps in the three guides

These topics matter for Schutzhütte and the pages above do not cover them. Do not fill them from memory; check the package's own docs or Unity's docs when the work starts.
- `NOT SOURCEABLE` — Netcode for GameObjects (NGO) and Multiplayer Play Mode changes in 6.4–6.6, not stated in the three upgrade guides or the three "What's new" pages. NGO is a separate package with its own documentation and version; it is not installed in this project yet (`Packages/manifest.json`, 2026-11-07).
- `NOT SOURCEABLE` — Input System package changes in 6.4–6.6 beyond: `OnMouseDown/Drag/Up` support for MonoBehaviours (6.4, verbatim), Linux IME (6.5, verbatim), gamepad rumble on Android via `Gamepad.SetMotorSpeeds` (6.6, [SUMMARY]). The project's manifest has `com.unity.inputsystem` 1.20.0; its changelog was not read.
- `NOT SOURCEABLE` — built-in 3D physics (PhysX) changes in 6.4–6.6. The pages list none. "Lists none" is not "none happened"; treat `physics: PhysX` in `project.yaml` as the engine-typical default, not a sourced fact.
- `NOT SOURCEABLE` — the C# language version of 6.4–6.6, not stated.
- The "What's new" pages for 6.5 and 6.6 were only partly returned verbatim by the fetch tool (about 40,000 characters each); the rest came as a summary. Items tagged **[SUMMARY]** may be incomplete or misnamed.
- `modules/*.md` and `plugins/*.md` in this directory still describe 6.3 and were **not** re-verified against 6.6.

**Sources (all fetched 2026-11-07):**
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity64.md
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity65.md
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66.md
- https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64
- https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65
- https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66.md
