# Unity 6.3 → 6.6 — Networking Module Reference

**Last verified:** 2026-02-13 (sections below); 2026-11-07 (the "NGO 2.13" block at the end)
**Knowledge Gap:** Unity 6 uses Netcode for GameObjects (UNet deprecated)

> **For this project read the "NGO 2.13 for this project" block at the end first.**
> The examples below predate it: they use `[ServerRpc]`/`[ClientRpc]` and the old
> `Input` class. New code uses `[Rpc(SendTo.Server)]` and the Input System.

---

## Overview

Unity 6 networking options:
- **Netcode for GameObjects** (RECOMMENDED): Official Unity multiplayer framework
- **Mirror**: Community-driven (UNet successor)
- **Photon**: Third-party service (PUN2)
- **Custom**: Low-level sockets

**UNet (Legacy)**: Deprecated, do not use.

---

## Netcode for GameObjects

### Installation
1. `Window > Package Manager`
2. Search "Netcode for GameObjects"
3. Install `com.unity.netcode.gameobjects`

---

## Basic Setup

### NetworkManager

```csharp
// Add to scene: GameObject > Add Component > NetworkManager

// Or create custom NetworkManager:
using Unity.Netcode;

public class CustomNetworkManager : MonoBehaviour {
    void Start() {
        NetworkManager.Singleton.StartHost(); // Server + client
        // OR
        NetworkManager.Singleton.StartServer(); // Dedicated server
        // OR
        NetworkManager.Singleton.StartClient(); // Client only
    }
}
```

---

## NetworkObject (Networked GameObjects)

### Mark GameObject as Networked

1. Add `NetworkObject` component to GameObject
2. Must be in root of prefab (not nested)
3. Register prefab in `NetworkManager > NetworkPrefabs List`

### Spawn Network Objects

```csharp
using Unity.Netcode;

public class GameManager : NetworkBehaviour {
    public GameObject playerPrefab;

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ulong clientId) {
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
```

---

## NetworkBehaviour (Networked Scripts)

### NetworkBehaviour Base Class

```csharp
using Unity.Netcode;

public class Player : NetworkBehaviour {
    // Called when spawned on network
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            // Only run on owner's client
            GetComponent<Camera>().enabled = true;
        }
    }

    void Update() {
        if (!IsOwner) return; // Only owner can control

        // Handle input
        if (Input.GetKey(KeyCode.W)) {
            MoveServerRpc(Vector3.forward);
        }
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 direction) {
        // Runs on server
        transform.position += direction * Time.deltaTime;
    }
}
```

---

## Network Variables (Synchronized State)

### NetworkVariable<T>

```csharp
using Unity.Netcode;

public class Player : NetworkBehaviour {
    // ✅ Auto-synced across clients
    private NetworkVariable<int> health = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn() {
        // Subscribe to value changes
        health.OnValueChanged += OnHealthChanged;
    }

    void OnHealthChanged(int oldValue, int newValue) {
        Debug.Log($"Health changed: {oldValue} -> {newValue}");
        UpdateHealthUI(newValue);
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage) {
        // Only server can modify NetworkVariable
        health.Value -= damage;
    }
}
```

### NetworkVariable Permissions

```csharp
// Server can write, clients read-only (default)
private NetworkVariable<int> score = new NetworkVariable<int>();

// Owner can write
private NetworkVariable<int> ammo = new NetworkVariable<int>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);
```

---

## RPCs (Remote Procedure Calls)

### ServerRpc (Client → Server)

```csharp
// Client calls, server executes
[ServerRpc]
void FireWeaponServerRpc() {
    // Runs on server
    Debug.Log("Server: Weapon fired");
}

// Call from client:
if (IsOwner && Input.GetKeyDown(KeyCode.Space)) {
    FireWeaponServerRpc();
}
```

### ClientRpc (Server → All Clients)

```csharp
// Server calls, all clients execute
[ClientRpc]
void PlayExplosionClientRpc(Vector3 position) {
    // Runs on all clients
    Instantiate(explosionPrefab, position, Quaternion.identity);
}

// Call from server:
[ServerRpc]
void ExplodeServerRpc(Vector3 position) {
    // Server logic
    DealDamageToNearbyPlayers(position);

    // Notify all clients
    PlayExplosionClientRpc(position);
}
```

### RPC Parameters

```csharp
// ✅ Supported: Primitives, structs, strings, arrays
[ServerRpc]
void SetNameServerRpc(string playerName) { }

[ClientRpc]
void UpdateScoresClientRpc(int[] scores) { }

// ❌ Not supported: MonoBehaviour, GameObject (use NetworkObjectReference)
```

---

## Network Ownership

### Check Ownership

```csharp
if (IsOwner) {
    // This client owns this NetworkObject
}

if (IsServer) {
    // Running on server
}

if (IsClient) {
    // Running on client
}

if (IsLocalPlayer) {
    // This is the local player object
}
```

### Transfer Ownership

```csharp
// Server transfers ownership
NetworkObject netObj = GetComponent<NetworkObject>();
netObj.ChangeOwnership(newOwnerClientId);
```

---

## NetworkObjectReference (Pass GameObjects in RPCs)

```csharp
using Unity.Netcode;

[ServerRpc]
void AttackTargetServerRpc(NetworkObjectReference targetRef) {
    if (targetRef.TryGet(out NetworkObject target)) {
        // Got the target object
        target.GetComponent<Health>().TakeDamage(10);
    }
}

// Call:
NetworkObject targetNetObj = target.GetComponent<NetworkObject>();
AttackTargetServerRpc(targetNetObj);
```

---

## Client-Server Architecture

### Server-Authoritative Pattern (RECOMMENDED)

```csharp
public class Player : NetworkBehaviour {
    private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();

    void Update() {
        if (IsOwner) {
            // Client: Send input to server
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            MoveServerRpc(input);
        }

        // All clients: Sync to networked position
        transform.position = position.Value;
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 input) {
        // Server: Validate and apply movement
        position.Value += input * Time.deltaTime * moveSpeed;
    }
}
```

---

## Network Transport

### Unity Transport (Default)

```csharp
// Configured in NetworkManager:
// - Transport: Unity Transport
// - Address: 127.0.0.1 (localhost) or server IP
// - Port: 7777 (default)
```

### Connection Events

```csharp
void Start() {
    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
}

void OnClientConnected(ulong clientId) {
    Debug.Log($"Client {clientId} connected");
}

void OnClientDisconnected(ulong clientId) {
    Debug.Log($"Client {clientId} disconnected");
}
```

---

## Performance Tips

### Reduce Network Traffic
- Use `NetworkVariable` for state that changes infrequently
- Batch multiple changes before syncing
- Use delta compression for large data

### Prediction & Reconciliation
- Run movement locally for responsiveness
- Reconcile with server authoritative state
- Use interpolation for smooth movement

---

## Debugging

### Network Profiler
- `Window > Analysis > Network Profiler`
- Monitor bandwidth, RPC calls, variable updates

### Network Simulator (Test Latency/Packet Loss)
- `NetworkManager > Network Simulator`
- Add artificial lag and packet loss for testing

---

## Sources
- https://docs-multiplayer.unity3d.com/netcode/current/about/
- https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/

---

# NGO 2.13 for this project (verified 2026-11-07)

**Last verified:** 2026-11-07, against the package registry, the NGO 2.13 API reference and
manual, and the Multiplayer Play Mode 2.0 manual. Where this block and the older sections
above disagree, this block wins. Gaps are marked `NOT SOURCEABLE`.

## Package choice

- **Use `com.unity.netcode.gameobjects` 2.13.3.** The newest NGO is 3.2.1 (2026-11-05), but its
  registry entry requires `"unity":"6000.7"`; this project runs 6000.6.6f1. 2.13.3 declares
  `"unity":"6000.0"` and depends on `com.unity.transport` 2.6.0 and `com.unity.nuget.mono-cecil` 1.11.4.
- **Do not go below 2.13.0.** Changelog 2.13.0 (2026-06-21): "Added support for Unity's Fast
  Enter Play Mode with domain reload disabled." This project has domain reload off.
- 2.13.0 deprecated `NetworkObject.IsSceneObject` in favor of `NetworkObject.InScenePlaced`.
- 2.7.0 deprecated all `RequireOwnership` RPC fields in favor of `RpcInvokePermission`.
- Moving to 3.x is a decision for the switch to Unity 6.7 LTS.

## RPCs

- Attribute: `[Rpc(SendTo.Server)]`. Constructor `public RpcAttribute(SendTo target)`. Fields:
  `InvokePermission` (`RpcInvokePermission`, default `Everyone`), `Delivery` (`RpcDelivery`),
  `DeferLocal`, `AllowTargetOverride`.
- `RequireOwnership` is `[Obsolete]`. Use `InvokePermission = RpcInvokePermission.Owner` or
  `RpcInvokePermission.Everyone`, for example
  `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]`.
- The `ServerRpcAttribute` class itself is not `[Obsolete]` in 2.13, only its `RequireOwnership`
  field. This project uses `[Rpc(...)]` only (CLAUDE.md).
- `SendTo` values: `Authority`, `ClientsAndHost`, `Everyone`, `Me`, `NotAuthority`, `NotMe`,
  `NotOwner`, `NotServer`, `Owner`, `Server`, `SpecifiedInParams`. In client-server,
  `Authority` behaves like `Server`.
- `NOT SOURCEABLE`: whether an `[Rpc]` method name must end in `Rpc` in 2.13. Keep the suffix.

## NetworkManager (API, 2.13.3)

- `public static NetworkManager Singleton { get; }`
- `public bool StartHost()`, `public bool StartClient()`, `public bool StartServer()`
- `public void Shutdown(bool discardMessageQueue = false)`
- `public bool IsHost`, `IsServer`, `IsClient`, `IsListening { get; }`; `public ulong LocalClientId { get; }`
- `public IReadOnlyList<ulong> ConnectedClientsIds { get; }`, `ConnectedClients`, `ConnectedClientsList`
- `public event Action<ulong> OnClientConnectedCallback` and `OnClientDisconnectCallback`. The API
  remark: "It is recommended to use OnConnectionEvent instead, as this will eventually be
  deprecated." (`OnConnectionEvent`'s signature was not read: `NOT SOURCEABLE`.)
- `public string DisconnectReason { get; }`: read it after `OnClientDisconnectCallback`.
- `public event Action OnServerStarted`, `OnClientStarted`, `OnPreShutdown`, `OnTransportFailure`;
  `public event Action<bool> OnServerStopped`, `OnClientStopped` (bool = was host).
- `public const ulong ServerClientId = 0`; `DisconnectClient(ulong clientId, string reason = null)`.
- `NetworkConfig`: `ConnectionApproval` (bool), `PlayerPrefab` (GameObject: "When set, NetworkManager
  will automatically create and spawn the assigned player prefab"), `NetworkTopology`
  (`NetworkTopologyTypes.ClientServer` or `DistributedAuthority`). Defaults: not stated.
  This project uses **ClientServer**.
- Getting-started manual: "Netcode for GameObjects won't spawn a player object if you don't have
  any prefab set as the PlayerPrefab." Enable Scene Management is on by default.

## Connection approval

- `public Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> ConnectionApprovalCallback { get; set; }`.
  Set it before `StartHost()`, and tick **Connection Approval** in the NetworkManager inspector
  ("enables connection approval when this is checked and the NetworkManager.ConnectionApprovalCallback is assigned").
- Request (struct): `ulong ClientNetworkId`, `byte[] Payload`.
- Response: `bool Approved`, `bool CreatePlayerObject`, `bool Pending`, `uint? PlayerPrefabHash`
  (null = default player prefab), `Vector3? Position`, `Quaternion? Rotation`, `string Reason`
  (sent to a rejected client; it reads it from `NetworkManager.DisconnectReason`).
- Null `Position`: the API page says the prefab position is used, a manual sample comment says
  `Vector3.zero`. They conflict, so always set `Position` and `Rotation` explicitly.
- Not supported with the distributed authority topology.
- The docs do not say whether the callback also runs for the host's own client. **Observed in
  this project (Editor test, 2026-11-08): it does.** `Logs/Editor.log` shows
  `[Spawn] Client 0 angenommen, Spawnpunkt 1.` for the host and `Client 1 ... Spawnpunkt 2.` for
  the joining client, and both capsules appeared at the assigned positions in both windows.

## Unity Transport (as shipped with NGO 2.13)

- `SetConnectionData(string ipv4Address, ushort port, string listenAddress = null)`, plus an
  overload with a leading `bool forceOverrideCommandLineArgs` and one taking `NetworkEndpoint`s.
  `ipv4Address` "can be an IPv6 address or a domain name". Ignored when using Unity Relay.
- **`DebugSimulator` and `SetDebugSimulatorParameters` are `[Obsolete]` and have no effect:**
  "Use Network Simulator from the Multiplayer Tools package." The ~100 ms latency test from
  CLAUDE.md therefore needs the Multiplayer Tools package.
- `NOT SOURCEABLE`: Network Simulator setup steps and the Multiplayer Tools version for 6000.6.
  Check before the carrying step.

## Observed in this project (batchmode runs, 2026-11-08)

- **On Unity 6000.6.6f1, `com.unity.multiplayer.playmode` and `com.unity.transport` are built-in
  packages.** With `"com.unity.multiplayer.playmode": "2.0.2"` in the manifest the Editor resolved
  **3.0.0** (`"source": "builtin"` in `Packages/packages-lock.json`); the manifest now says 3.0.0.
  NGO 2.13.3 declares `com.unity.transport` 2.6.0, but the Editor resolved the built-in **6.6.0**.
  The project compiles with that combination; runtime behavior is checked by the play test.
- MPPM 3.0.0's changelog entry (2026-02-11) is empty. Its bundled manual
  (`Library/PackageCache/com.unity.multiplayer.playmode@*/Documentation~`) uses the same menu
  paths as 2.0: **Window > Play Mode > Scenarios** and **Window > Play Mode > Active Scenario**.
- **A network prefab saved with `PrefabUtility.SaveAsPrefabAsset` from a scene object gets
  `GlobalObjectIdHash: 0`.** NGO computes the hash in `NetworkObject.OnValidate` when the prefab
  asset is loaded, but the first save does not contain it. At run time NGO logs "Detected
  NetworkObject GlobalObjectIdHash value of 0!" and does not spawn it. Fix: load the asset,
  `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets()` (see
  `PrototypeSceneBuilder.RefreshPrefabHashFromCommandLine`). Source: NGO 2.13.3
  `Runtime/Core/NetworkObject.cs` in `Library/PackageCache`.
- NGO created `Assets/DefaultNetworkPrefabs.asset` itself and added the Player prefab to it. The
  NetworkManager in `Prototype.unity` references that list.

## NetworkTransform in NGO 2.13.3 (read from the installed source, 2026-11-08)

Source: `Library/PackageCache/com.unity.netcode.gameobjects@*/Runtime/Components/NetworkTransform.cs`,
namespace `Unity.Netcode.Components`.
- `public AuthorityModes AuthorityMode;` with `enum AuthorityModes { Server, Owner }`. Server is
  the default ("If server (the default), then only server-side adjustments to the transform will
  be synchronized"). Owner: "only the owner-side adjustments to the transform will be synchronized
  with both the server and other clients." In NGO 2.x there is no `ClientNetworkTransform` subclass
  to write; set the field instead.
- Public bool fields: `SyncPositionX/Y/Z`, `SyncRotAngleX/Y/Z`, `SyncScaleX/Y/Z` (all default
  `true`), `Interpolate` (default `true`), `InLocalSpace`, `UseQuaternionSynchronization`,
  `UseHalfFloatPrecision`, `SlerpPosition`. `Teleport(Vector3, Quaternion, Vector3)` exists.
- **Project decision (2026-11-08): the player's own body is owner-authoritative** (AuthorityMode
  Owner, position plus Y rotation only). Everything that is game state (items, oven, guests,
  weather) stays server-authoritative per CLAUDE.md rule 2. Reason: with server authority and no
  prediction, the client's own movement would lag by the round trip.
- Tested in the Editor with MPPM (2026-11-08): both players walk and turn, each sees the other.
  Not yet tested with simulated latency.

## Carrying via NetworkObject parenting (NGO 2.13.3, read from source, 2026-11-08)

How `Carryable` / `PlayerCarry` work, and the NGO facts they rely on:
- `NetworkObject.TrySetParent(NetworkObject parent, bool worldPositionStays = true)` and
  `TryRemoveParent(bool worldPositionStays = true)`, server-side. `AutoObjectParentSync` defaults
  to `true`. `NetworkBehaviour.OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)`
  is the override that reacts to a parent change.
- `NetworkTransform.SwitchTransformSpaceWhenParented` (default `false`): "When parented:
  Automatically transitions into local space ... When de-parented: Automatically transitions into
  world space". Only works when the paired `NetworkRigidbody` does **not** use
  `UseRigidBodyForMotion`. The crate prefab sets it to `true` and leaves `UseRigidBodyForMotion` off.
- `NetworkRigidbody.AutoUpdateKinematicState` (default `true`) calls
  `SetIsKinematic(!m_IsAuthority)` on spawn and ownership changes, so clients' crates are always
  kinematic. On the host, `Carryable` switches kinematic on while carried and off on drop.
- `NetworkObject.Spawn(bool destroyWithScene = false)`; `NetworkObjectReference` has implicit
  conversions from `NetworkObject` and `TryGet(out NetworkObject, NetworkManager = null)`.
- When a parent NetworkObject is despawned, NGO moves its first-generation children to the root
  (`TryRemoveParentCachedWorldPositionStays` in `NetworkSpawnManager`). A crate held by a player
  who disconnects therefore drops; `Carryable` makes it non-kinematic again in the parent callback.
- Why parenting: the crate becomes a child of the carrier's owner-authoritative body, so on the
  carrier's own machine it follows with zero latency; everyone else sees it move with the
  interpolated body. Pick-up and drop go through `[Rpc(SendTo.Server, InvokePermission =
  RpcInvokePermission.Owner)]`; on drop the client sends its own crate pose and the host accepts it
  when it is within 4 m of the host's view of the carrier.
- Known limit: the carried crate's collider is off, so it passes through walls while carried.
- Tested in the Editor with MPPM (2026-11-08, host in the Player 2 window): crates spawn, pick-up,
  drop and throw work, no errors in either Editor log.

## Simulated latency: Multiplayer Tools 2.2.12 Network Simulator (2026-11-08)

Sources: the package's bundled `Documentation~/network-simulator.md` and its runtime source in
`Library/PackageCache/com.unity.multiplayer.tools@*`, plus NGO's `UnityTransport.cs` and Unity
Transport's `SimulatorUtility.cs`.
- Add the `NetworkSimulator` component (namespace `Unity.Multiplayer.Tools.NetworkSimulator.Runtime`)
  to an object that persists across scenes; this project uses the NetworkManager. Custom presets
  are `NetworkSimulatorPresetAsset` ScriptableObjects (`PacketDelayMs`, `PacketJitterMs`,
  `PacketLossInterval`, `PacketLossPercent`); the component stores one in its serialized
  `m_PresetAsset` field (Inspector: Connection Preset = Custom). Requires Unity Transport >= 2.0.0
  and NGO >= 1.1.0; it works with the built-in Transport 6.6.0 here.
- NGO only builds the simulator pipeline stage when `UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED`
  is defined, which `UnityTransport.cs` does in the Editor and in DEBUG builds unless
  `UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP` is set.
- NGO calls `WithSimulatorStageParameters` without a mode, so `ApplyMode.AllPackets` applies: each
  instance delays both sent and received packets ("we handle both directions"). With the simulator
  on host and client (both load the same scene in MPPM), round trip = 4 x PacketDelayMs. The project
  preset `Assets/_Project/Data/Verzoegerung100ms.asset` uses 25 ms delay and 5 ms jitter.
- **Measured (Editor test, 2026-11-08):** the client's HUD showed a ping averaging about 100 ms
  (`UnityTransport.GetCurrentRtt(NetworkManager.ServerClientId)`). Carrying works at that latency;
  the user described it as slightly sluggish for the client.
- Pitfall when setting it by editor script: `EditorSceneManager.OpenScene` unloads unused assets,
  so load the preset asset after opening the scene, or the reference is saved as null.

## Client-side prediction for pick-up, drop and throw (project code, 2026-11-08)

Not an NGO feature; this is how `PlayerCarry` hides the round trip on clients. The host stays
authoritative (CLAUDE.md rule 2); prediction is purely visual on the owner's machine.
- **Grab:** on E the client sends `PickUpRpc`, hides the real crate locally
  (`Carryable.SetLocallyHidden`) and shows a non-networked copy (`Carryable.CreateLocalGhost`) at the
  hold point. It swaps back once the host has parented the real crate to the player and the crate
  reached the hold point (or 0.4 s after parenting), or reveals it again if someone else took it or
  nothing happened within 0.75 s.
- **Release (drop/throw):** the client sends `DropRpc` with its own crate pose and velocity, hides
  the real crate and lets a local copy with its own Rigidbody fly. The copy ignores collisions with
  the player's CharacterController and with the real crate (`Physics.IgnoreCollision`, re-applied
  every frame because enabling a collider resets it). It swaps back when the copy rests and the real
  crate is within 0.3 m, or after 2.5 s.
- **Fast sequences:** releasing while a grab is still pending reuses the grab copy's pose; the host
  receives `PickUpRpc` then `DropRpc` in order. A just-thrown crate (its copy or the hidden real
  crate) can be targeted and picked up again immediately.
- The host predicts nothing: its own RPCs run locally at once.
- **Lesson from testing:** an Editor that is open while scripts change on disk may not recompile on
  its own (here it kept running a 09:27 `Assembly-CSharp.dll` after 09:49 edits). Check the DLL time
  in `Library/ScriptAssemblies/` or a version marker in the HUD before trusting a test; Ctrl+R
  forces the refresh.
- **Tested (Editor, MPPM, 100 ms simulated ping, 2026-11-08):** the user reported carrying as the
  client as "much better", and fast pick-up/throw sequences as clean after the fixes. No errors in
  either Editor log.

## Two-person carrying (`HeavyCarryable`, `PlayerHeavyCarry`, 2026-11-08)

- **Holders** are two `NetworkVariable<NetworkObjectReference>` (one per end), written only by the
  host. A reference, not a client ID: NGO's `SpawnManager.GetPlayerNetworkObject(clientId)` refuses on
  clients in client-server mode ("Only the server can find player objects from other clients"),
  while `NetworkObjectReference.TryGet` resolves through `SpawnManager.SpawnedObjects`, which clients
  have. The default reference (id 0) resolves to null; NGO's id counter starts at 1.
- **Pose:** with both ends held, the host makes the chest kinematic and sets it every `LateUpdate`
  from both carriers' positions (`HeavyCarryable.ComputeCarryPose`, a pure static function): each
  hand sits `reach` in front of its carrier towards the other one, the chest lies centred between
  the hands. Its server-authoritative NetworkTransform distributes the result. With fewer than two
  holders it is a normal host-simulated rigid body and falls.
- **Limits:** movement stays owner-authoritative, so each owner limits its own move through
  `IMovementModifier` (`PlayerMovement` asks every modifier for a speed factor and a constrained
  delta): carriers keep their horizontal distance between 2.6 m and 3.6 m and walk at 60 %; a single
  holder cannot get further than 1.2 m from the held end. The partner's position is the
  interpolated, delayed one.
- Each owner ignores collisions between its CharacterController and the chest while holding it.
- **Prediction for the chest (clients only, 2026-11-08):** while both ends are held, a client
  carrier hides the real chest locally and shows a copy (`LocalGhost`) posed every `LateUpdate`
  with the same `ComputePose` the host uses, but from its own current position and the partner as
  it currently sees them. The copy goes away as soon as the client no longer sees two holders. Tested
  in the Editor (MPPM, 100 ms): the client's end stays in its hands. On release the whole chest falls
  flat; keeping the remaining carrier's end in hand is noted in `docs/IDEEN.md`.
- **Walls (2026-11-08):** carried things are kinematic or have their collider off, so physics cannot
  stop them. Instead the owner checks before each step whether the carried box would overlap fixed
  geometry (`CarryCollision.HitsEnvironment`: `Physics.OverlapBox` at 95 % size; "fixed" = no
  attached Rigidbody, not a CharacterController, not part of a player). Blocked steps are cut back
  to their x- or z-part so carriers slide along walls (`CarryCollision.Slide`). A single crate also
  blocks turning (`IMovementModifier.ConstrainYaw`, used by `PlayerLook`). The chest check uses the
  partner's delayed position. If the box already overlaps, the check lets the player move, so nobody
  gets stuck. Tested in the Editor (MPPM, 100 ms): crate and chest stop at walls and door frames.
- Tested in the Editor with MPPM and 100 ms simulated ping (2026-11-08): grab, carry together,
  limits and drop work, no errors in the logs. Alone with two windows the carriers can only move
  alternately; the real feel needs a test with two people.

## Multiplayer Play Mode 2.0.2 (docs read for 2.0; the project resolves 3.0.0, see above)

- `com.unity.multiplayer.playmode` 2.0.2 (2026-03-12). Minimum editor 6000.3.0b10 (changelog 2.0.1);
  2.0.0: "This version only supports Unity 6.3 and beyond."
- Up to four editor players (the main Editor plus up to three additional Editor instances), plus
  up to four local builds.
- 2.0 works with **Play Mode Scenarios**: **Window > Play Mode > Scenarios** (or the Play Mode
  dropdown > **Configure play mode scenarios**) > **+**. In the scenario keep **Editor** ticked and
  click **+** under **Additional Editor Instances**. Entering Play mode launches all configured
  instances; each additional Editor is a separate process with its own window. **Window > Play
  Mode > Active Scenario** shows them; Ctrl+F9 to Ctrl+F12 focus Player 1 to 4.
- Roles: the docs recommend Multiplayer Roles from the Dedicated Server package and advise
  against tags. For the prototype the role is picked with the HUD buttons instead.
- Limits: in an additional Editor you cannot create or change GameObjects, and the Package
  Manager does not work. If instances go out of sync, close the Editor and delete `Library/VP`.
- `NOT SOURCEABLE`: MPPM behavior with domain reload disabled.

**Sources (all fetched 2026-11-07):**
- https://packages.unity.com/com.unity.netcode.gameobjects
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.2/changelog/CHANGELOG.html (contains the 2.x entries)
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkManager.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkManager.ConnectionApprovalResponse.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkManager.ConnectionApprovalRequest.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkConfig.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkTopologyTypes.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.Transports.UTP.UnityTransport.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.RpcAttribute.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.SendTo.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.ServerRpcAttribute.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/basics/connection-approval.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/components/core/networkmanager.html
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/tutorials/get-started-with-ngo.html
- https://packages.unity.com/com.unity.multiplayer.playmode
- https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html and its pages install.html, play-mode-scenario/play-mode-scenario-create.html, play-mode-scenario/play-mode-scenario-configurations.html, instance-types/main-and-additional-editor-instances.html, faq.html
- https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/changelog/CHANGELOG.html
