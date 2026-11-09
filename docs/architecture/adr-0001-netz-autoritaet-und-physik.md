# ADR-0001: Netz-Autorität und Physik im Netz

## Status

Accepted (2026-11-09, vom Entwickler über `/architecture-decision accept ADR-0001`)

> **Wer auf `Accepted` setzen darf: der Entwickler, oder `technical-director` nach ausdrücklicher Bestätigung
> des Entwicklers.** Kein anderer Agent und kein Skill allein. Stories, die auf eine `Proposed`-ADR
> verweisen, sind `Blocked`.

## Date

2026-11-09

## Last Verified

2026-11-09

## Decision Makers

Entwickler (Projektleitung) mit Claude über `/architecture-decision`. Engine-Prüfung: `unity-specialist`
(2026-11-09: keine Blocker, sechs Hinweise eingearbeitet). TD-ADR übersprungen (Review-Modus lean).
Architektur-Review 2026-11-09 (`architecture-review-2026-11-09.md`, CONCERNS) eingearbeitet: Gutachter-Befund 1
(Bruch eines liegenden Gegenstands), W2 (eine Abmelde-Regel), W3 (Entfernen über ADR-0002), W7 („Am Körper“),
Empfehlung 4 (beide Varianten testen), Hinweis 10.

## Summary

Alle Gameplay-Systeme brauchen eine feste Regel, wer im Netz was entscheidet und wie Physik übertragen wird.
Diese ADR schreibt den gebauten Stand des Prototyps fest (Host entscheidet, eigener Spielerkörper
owner-autoritativ, Tragen per Parenting mit rein optischer Client-Vorhersage) und ergänzt drei Regeln:
Zerbrechen entscheidet der Host, Geworfenes bekommt eine durchgehende Kollisionsprüfung, solange es fällt,
und jeder Aufprall geht beim Host an einen Meldedienst `ImpactEvents`.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.6f1 (PhysX), Netcode for GameObjects 2.13.3, Unity Transport 6.6.0 (eingebaut), Multiplayer Tools 2.2.12 |
| **Domain** | Networking (mit Physics) |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH (Unity 6.6 und NGO 2.13 liegen nach dem Wissensstand des Modells, `VERSION.md`) |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md` (Block „NGO 2.13 for this project“), `modules/physics.md`, `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md`; NGO 2.13.3 Quelltext in `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/` (`Core/NetworkManager.cs`, `Core/NetworkObject.cs`, `Connection/NetworkConnectionManager.cs`, `Serialization/NetworkObjectReference.cs`, `Spawning/NetworkSpawnManager.cs`, `Messaging/MessageDelivery.cs`, `Components/NetworkRigidBodyBase.cs`); `docs/GRAUBOX.md` (Wandstärken); `ProjectSettings/TimeManager.asset` (Physik-Takt 0,02 s). `modules/physics.md` ist Stand 6.3 und sagt nichts zu durchgehender Kollisionsprüfung bei kinematischen Körpern. |
| **Post-Cutoff APIs Used** | `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` (NGO ≥ 2.7), `NetworkTransform.AuthorityMode` und `SwitchTransformSpaceWhenParented`, `NetworkRigidbody.AutoUpdateKinematicState` und `SetIsKinematic`, `NetworkObject.TrySetParent`/`TryRemoveParent`, `NetworkManager.OnConnectionEvent` mit `ConnectionEventData` (aus dem 2.13.3-Quelltext gelesen), `Rigidbody.linearVelocity` (kompiliert im Projekt) |
| **Verification Required** | (1) Umschalten der Kollisionsprüfung: keine Konsolen-Warnung bei Host und Client (auch nicht beim Bruch, wenn NGO den Körper beim `Despawn` kinematisch setzt), eine Tasse mit 15 m/s fliegt nicht durch eine 0,2 m dicke Innenwand. Dieselben Fälle einmal mit dauerhaft „ContinuousSpeculative“ wiederholen; zeigt „ContinuousDynamic“ Warnungen oder Durchflüge, Speculative aber nicht, gilt der Dauerwert (Rollback plan). (2) Ein Wurf mit 8–15 m/s trifft den Collider eines Gastes (Plan-Schritt 5). (3) Log-Prüfung: `OnConnectionEvent` meldet beim Host `ClientConnected` für den eigenen Client (laut Quelltext ja). |

**Aus dem NGO-2.13.3-Quelltext belegt** (Spezialisten-Prüfung 2026-11-09, Stichproben nachgelesen):
- `NetworkObject.Despawn(true)` aus einem Physik-Callback ist unbedenklich: Es läuft synchron durch, das
  `Destroy` kommt danach (`Spawning/NetworkSpawnManager.cs`). Das Projekt macht es schon so aus
  `OnTriggerEnter` (`Stove`).
- RPC und Despawn im selben Frame kommen in Sendereihenfolge an: Alle NGO-Nachrichten laufen als
  `ReliableFragmentedSequenced` (`Messaging/MessageDelivery.cs`) in dieselbe Warteschlange je Client.
- `NetworkObjectReference` wirft eine `ArgumentException`, wenn das Objekt nicht (mehr) gespawnt ist
  (`Serialization/NetworkObjectReference.cs`); `Despawn` setzt `IsSpawned` sofort auf `false`.
- NGO setzt `Rigidbody.collisionDetectionMode` nirgends; `NetworkRigidbody` schaltet nur `isKinematic`
  (beim Spawn, Besitzerwechsel und Despawn) und die Interpolation. Der Modus ist lokal und wird nicht
  übertragen.
- `StartHost()` löst am Ende `ClientConnected` für den eigenen Client aus (`Core/NetworkManager.cs`). Beim
  Host kommt bei jedem Beitritt zusätzlich `PeerConnected`.

**Im Architektur-Review belegt** (`unity-specialist`, 2026-11-09, Stichproben nachgelesen):
- `NetworkRigidbody.AutoSetKinematicOnDespawn` ist standardmäßig an: NGO setzt beim Despawn selbst
  `isKinematic` (`Components/NetworkRigidBodyBase.cs` 46).
- Ein direktes `Destroy` auf ein gespawntes Objekt löst beim Host das Despawn aus (`Core/NetworkObject.cs`
  1827–1832); `OnNetworkDespawn` läuft also auch dann.

**NOT SOURCEABLE** (nicht in `docs/engine-reference/` und nicht im Quelltext belegt, deshalb in „Verification
Required“):
- Änderungen an PhysX in Unity 6.4–6.6 (`breaking-changes.md` nennt keine, das ist kein Beleg für keine).
- Wie sich `CollisionDetectionMode.ContinuousDynamic` bei einem Körper verhält, der kinematisch wird oder
  es ist. Bekannt aus älteren Unity-Versionen: Kinematische Körper unterstützen nur „Speculative“.
  Verification 1 testet deshalb beide Varianten.
- In welcher Reihenfolge zwei Körper bei einem Zusammenstoß ihren `OnCollisionEnter` bekommen. Die
  Entscheidung nimmt an, dass sie nicht festgelegt ist (Entscheidung 4); Validation Criteria, PlayMode.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (erste ADR; `docs/architecture/` enthält keine weitere ADR) |
| **Enables** | ADR-0002 (Katalog liefert Zerbrechlichkeit, Bruchgeschwindigkeit, Masse, Klanggruppe und die Art-Kennung für die Scherben-RPC), ADR-0003 (Meldungen zwischen Systemen übernehmen das Muster von `ImpactEvents`), ADR-0004 (Interaktion nutzt dieselbe RPC-Form), ADR-0005 (Gäste hören Lärm und Treffer am Meldedienst), ADR-0009 (Ragdoll: Sturz des Trägers versetzt Getragenes in „Fällt“) |
| **Blocks** | Plan-Schritt „Gegenstandsarten“ (Zerbrechen, Würfe) und Plan-Schritt „Erster Gast und Tee“ (Lärm, Chaos) in `production/milestones/vertical-slice.md`. Epics und Stories gibt es noch keine. |
| **Ordering Note** | Den Typ der Art-Kennung in `PlayBreakRpc` legt ADR-0002 fest. ADR-0003 übernimmt `ImpactEvents` als Muster oder ersetzt es ausdrücklich. |

## Context

### Problem Statement

Der Prototyp hat das Netz-Verhalten Schritt für Schritt gebaut und in `docs/engine-reference/unity/modules/
networking.md` nachgetragen, aber nirgends als verbindliche Regel festgehalten. Der Vertical Slice bringt
Systeme, die genau daran hängen: Geschirr zerbricht, Würfe werden schneller, und Gäste reagieren auf Lärm
und Treffer. Ohne feste Regel würde jedes System Autorität, Physik und Meldewege neu erfinden. Die Frage muss
vor dem ersten Code des Vertical Slice (Plan-Schritt 1) geklärt sein.

### Current State

Gebaut und im Editor getestet (MPPM, 100 ms simulierter Ping, 2026-11-08; Freundestest 0.1.4 am 2026-11-09):
- **Host-Client mit NGO 2.13.3**, Topologie ClientServer, Connection Approval setzt Startplätze.
- **Spielerkörper owner-autoritativ:** `NetworkTransform.AuthorityMode = Owner`, nur Position und
  Y-Drehung. Alles andere ist server-autoritativ.
- **Tragen per Parenting:** Der Host hängt den Gegenstand an die Spielfigur (`TrySetParent`), er ist dann
  kinematisch und sein Collider aus. Clients sehen eine nicht vernetzte Kopie (`Carryable.CreateLocalGhost`,
  `LocalGhost`), bis der Host fertig ist. Beim Ablegen schickt der Client seine eigene Pose, der Host nimmt
  sie innerhalb von 4 m an.
- **Zu zweit tragen:** Zwei `NetworkVariable<NetworkObjectReference>` für die Halter, der Host setzt die
  Pose jedes `LateUpdate` (`HeavyCarryable.ComputeCarryPose`), jeder Besitzer begrenzt seine eigene
  Bewegung (`IMovementModifier`). Wände beim Tragen prüft `CarryCollision` vor jedem Schritt.
- **Ofen:** Host schreibt `NetworkVariable<float>` für Brennstoff und Wärme (`Stove.Step`, rein statisch).
- **Physik:** Clients haben kinematische Gegenstände (`NetworkRigidbody.AutoUpdateKinematicState`), der Host
  rechnet Fallendes und Rutschendes, Ruhendes schläft. Kollisionsprüfung überall „Discrete“.
- **Aufpralle:** `ImpactSound` misst beim Host im eigenen `OnCollisionEnter` (erster Kontaktpunkt) und
  schickt das Geräusch per RPC an alle.
- **Verbindungen:** `ConnectionHud` und `PlayerSpawnApproval` nutzen `OnClientConnectedCallback` und
  `OnClientDisconnectCallback`. NGO empfiehlt `OnConnectionEvent`, weil die alten „eventually deprecated“
  werden.

Was fehlt: Zerbrechen, Schutz gegen Durchfliegen von Wänden (Innenwände 0,2 m; bei 15 m/s und 0,02 s
Physik-Takt sind es 0,3 m pro Schritt) und ein Meldeweg für Aufpralle an andere Systeme.

### Constraints

- CLAUDE.md Architektur-Regeln 1–3 und 6: Multiplayer ab Tag 1, der Host entscheidet, Physik sparsam,
  Solo = Host ohne Clients.
- Koop für 1–4 Spieler unter Freunden: Schutz gegen Schummeln ist kein Ziel.
- NGO bleibt auf 2.13.3, solange das Projekt auf Unity 6.6 ist (3.x braucht 6.7).
- Domain Reload ist aus: statische Felder müssen beim Start zurückgesetzt werden.
- Entwickler mit etwas Unity-Erfahrung: lieber wenige, gut erklärte Muster als viele.

### Requirements

- Zerbrechen nach `breaks_on_impact` (`design/gdd/gegenstandsarten.md`, Formel 2), nur beim Host.
- Würfe bis 15 m/s dürfen dünne Wände nicht durchfliegen (GDD Gegenstandsarten, Open Question 3).
- Gäste erfahren Lärm (Aufprall in der Nähe, zerbrochenes Geschirr) und Treffer (`design/gdd/gaeste.md`,
  Formel 5).
- Der Client muss beim Tragen und Werfen weiter flüssig spielen (100 ms Ping).
- Keine neue Daueraufgabe im Netz für Ruhendes.

## Decision

**Grundsatz:** Der Host entscheidet über allen Spielzustand. Einzige Ausnahme ist der eigene Spielerkörper:
Er ist owner-autoritativ, damit die eigene Bewegung nicht um den Ping hinterherhinkt. Client-Vorhersage ist
immer nur optisch: Eine Kopie darf sich bewegen, aufprallen und klingen, aber nie Spielzustand ändern.

**1. Autorität und Wünsche.** Clients ändern nichts selbst. Sie schicken Wünsche per
`[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` von ihrer eigenen Spielfigur. Der Host
prüft (zum Beispiel die Ablege-Pose innerhalb von 4 m) und führt aus. NetworkVariables schreibt nur der Host.
Über das Netz gehen Kennungen (`NetworkObjectReference`, `NetworkObjectId`, Art-Kennung nach ADR-0002), nie
`EntityId` oder `InstanceID`.

**2. Physik im Netz.** Gegenstände haben einen server-autoritativen `NetworkTransform` mit `NetworkRigidbody`
(`AutoUpdateKinematicState` an, `UseRigidBodyForMotion` aus, `SwitchTransformSpaceWhenParented` an). Bei
Clients sind sie immer kinematisch. Beim Host gibt es drei Zustände:
- **Liegt:** Rigidbody aktiv, schläft in Ruhe ein, Kollisionsprüfung „Discrete“.
- **Getragen:** Kind der Spielfigur, kinematisch, Collider aus. Wände prüft der Träger mit `CarryCollision`.
- **Fällt:** nach dem Loslassen, nach einem Treffer durch einen bewegten Körper oder beim Sturz des Trägers.
  Der Host rechnet die Physik voll. Der Zustand „Fällt“ ist nur beim Host bekannt und geht nicht ins Netz.
  Ein liegender Gegenstand erkennt den Treffer selbst (Entscheidung 4).

Ausrüstung, die ein Spieler „am Körper“ trägt (GDD Gegenstandsarten, Kriterium 34), legt ADR-0004 als eigenen
Zustand fest. Bis dahin gilt: Sie wechselt nie in „Fällt“, auch nicht beim Sturz des Trägers.

**3. Durchgehende Kollisionsprüfung nur im Fall.** Solange ein Gegenstand beim Host „fällt“ und nicht
kinematisch ist, gilt `collisionDetectionMode = ContinuousDynamic`, sonst „Discrete“. Das gleicht eine einzige
Stelle am Gegenstand beim Host ab (jeden Physikschritt aus Zustand und `isKinematic`), nicht jede Stelle, die
den Zustand ändert. So ist auch der Fall abgedeckt, dass NGO einen Getragenen beim Verbindungsabbruch des
Trägers selbst loslässt. Wo der eigene Code einen Körper kinematisch macht (`AttachTo`) oder entfernt (Bruch),
stellt er vorher auf „Discrete“. An NGO-eigenen Stellen (Despawn, Besitzerwechsel) lässt sich das nicht
erzwingen; ob dort Warnungen entstehen, prüft Verification 1. Die Vorhersage-Kopie beim Client bekommt beim
Erzeugen ebenfalls „ContinuousDynamic“, damit sie nicht dort durch die Wand fliegt, wo die echte abprallt.
Verification 1 testet dieselben Fälle auch mit dauerhaft „ContinuousSpeculative“ (ohne Umschalten). Zeigt
„ContinuousDynamic“ Warnungen oder Durchflüge und der Dauerwert nicht, wechselt das Projekt auf den Dauerwert;
die Abgleichstelle entfällt dann.

**4. Aufprall messen und melden.** Jeder Gegenstand hat beim Host eine Komponente `ImpactReporter`. Sie ist die
einzige Stelle, die `OnCollisionEnter` für Spiel-Logik auswertet. Sie misst `impact_speed` als größten
senkrechten Anteil der Relativgeschwindigkeit über alle Kontaktpunkte (GDD Formel 2), prüft die Bruchregel
und meldet das Ergebnis einmal an den Meldedienst `ImpactEvents`. Jeder beteiligte Gegenstand meldet seinen
eigenen Aufprall. Doppelte Wirkung verhindern die Sperrzeiten der Zuhörer (zum Beispiel `noise_lockout` je
Gast).

Liegt der Gegenstand und trifft ihn ein Körper mit nicht kinematischem Rigidbody (`collision.rigidbody`),
schaltet sein `ImpactReporter` ihn im selben Callback zuerst auf „Fällt“ und prüft erst danach die
Bruchregel. Grund: Die Bruchregel verlangt „Fällt“, und welcher der beiden Körper seinen Callback zuerst
bekommt, ist nicht festgelegt. Ohne diese Reihenfolge zerbräche eine liegende Tasse nie, wenn eine geworfene
Kiste sie trifft (GDD Gegenstandsarten, Kriterium 15). Schläft der Körper wieder ein, ist er wieder „Liegt“.

**5. Zerbrechen.** Die Bruchregel ist eine reine statische Funktion (`BreakRule.BreaksOnImpact`). Zerbricht ein
Gegenstand, sperrt sich `ImpactReporter` gegen weitere Kontakte im selben Physikschritt, meldet den Aufprall
mit `Broke = true`, stellt auf „Discrete“ und entfernt den Gegenstand danach per
`NetworkObject.Despawn(true)`. Im Bau bündelt ADR-0002 die beiden letzten Schritte in der Host-Funktion
„Entfernen“; der Bruch ruft sie auf, statt `Despawn` selbst. Die Reihenfolge Melden → Despawn ist wichtig: NGO stellt die Nachrichten in
Sendereihenfolge zu, die Scherben-RPC kommt also vor dem Entfernen an. Scherben-Effekt und
Scherben-Klang schickt nicht der Gegenstand selbst, sondern ein dauerhaftes Objekt `ImpactEffects`
(Presentation), das am Meldedienst zuhört und `PlayBreakRpc` an alle sendet. Die Vorhersage-Kopie zerbricht
nie selbst; verschwindet das Original, entfernt der Client auch die Kopie (heute schon in `PlayerCarry`).

**6. Geräusche.** `ImpactEffects` übernimmt auch die normalen Aufprall-Geräusche: Es hört am Meldedienst zu,
wählt bei zwei zusammenstoßenden Gegenständen den schwereren und sendet `PlayImpactRpc` mit einer
`NetworkObjectReference`, aber nur, wenn der Gegenstand noch gespawnt ist (sonst wirft NGO). Der Client
verträgt, dass `TryGet` fehlschlägt. Ein Client, bei dem der Gegenstand gerade ausgeblendet ist, weil seine
Kopie fliegt, überspringt die RPC (wie heute). Der eigene `OnCollisionEnter` in `ImpactSound` entfällt.

**7. Verbindungs-Meldungen (QQ-06).** Neuer Code nutzt nur `NetworkManager.OnConnectionEvent` und abonniert
es vor `StartHost()`/`StartClient()`, sonst fehlt die Meldung über den eigenen Client. Host-Logik wertet nur
`ClientConnected` und `ClientDisconnected` aus, nicht `PeerConnected`/`PeerDisconnected`. `ConnectionHud` und
`PlayerSpawnApproval` wechseln im Plan-Schritt „Steam-Lobby“, wenn sie ohnehin umgebaut werden.

### Architecture Diagram

```
 Client (Besitzer)                      Host                                     alle Rechner
 ─────────────────                      ────                                     ────────────
 Spielerkörper ── NetworkTransform ───► (owner-autoritativ, Host übernimmt) ───► sehen ihn interpoliert
 E / Wurf ── [Rpc(SendTo.Server)] ────► prüft, TrySetParent / Detach
 Kopie (LocalGhost, optisch)                │
                                            ▼  Zustand Fällt: ContinuousDynamic
                                     Rigidbody (PhysX, nur hier aktiv)
                                            │ OnCollisionEnter
                                            ▼
                                     ImpactReporter ── BreakRule (statisch)
                                            │ ImpactReport
                                            ▼
                                     ImpactEvents.Reported (C#-Event, nur Host)
                       ┌────────────────────┼──────────────────────┐
                       ▼                    ▼                      ▼
                 ImpactEffects        Gäste (Lärm, Chaos)    weitere Zuhörer
                 PlayImpactRpc /      ändern Laune als        (Bewertung, Hund …)
                 PlayBreakRpc ──────────────────────────────────────────────► Klang, Scherben
                                     danach bei Broke: Despawn ─────────────► Gegenstand weg
```

### Key Interfaces

Vorschlag; Namen und Ort legt der Bau in Plan-Schritt „Gegenstandsarten“ fest. Ort: Foundation,
`Assets/_Project/Scripts/Items/`, Namespace `Schutzhuette.Items` (gleicher Ort wie der Katalog aus
ADR-0002). Nicht `Schutzhuette.Physics`: Der Name würde im eigenen Code `UnityEngine.Physics` verdecken.

```csharp
namespace Schutzhuette.Items
{
    /// <summary>Ein Aufprall, wie ihn der Host gemessen hat. Nur beim Host, geht nicht ins Netz.</summary>
    public readonly struct ImpactReport
    {
        public readonly NetworkObject Item;   // der aufprallende Gegenstand
        public readonly Vector3 Point;        // Kontaktpunkt mit dem größten senkrechten Anteil
        public readonly float ImpactSpeed;    // m/s, größter |dot(relativeVelocity, normal)| aller Kontakte
        public readonly float Mass;           // kg, aus dem Katalog (bis ADR-0002 gebaut ist: Rigidbody)
        public readonly bool WasFalling;      // Zustand „Fällt“ beim Host
        public readonly bool Broke;           // Bruchregel hat ausgelöst
        public readonly Collider Other;       // was getroffen wurde, z. B. ein Gast
    }

    /// <summary>Meldedienst für Aufpralle. Nur der Host löst aus, nur Host-Code hört zu.</summary>
    public static class ImpactEvents
    {
        public static event Action<ImpactReport> Reported;

        /// <summary>Nur ImpactReporter ruft das auf. Ruft jeden Zuhörer einzeln in try/catch auf
        /// (Debug.LogException), damit ein Fehler weder andere Zuhörer noch das Despawn verhindert.</summary>
        public static void Raise(in ImpactReport report);

        // Domain Reload ist aus: Abonnenten vom letzten Play nicht behalten.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Reported = null;
    }

    /// <summary>Bruchregel aus GDD Gegenstandsarten, Formel 2. Rein, ohne Szene testbar.</summary>
    public static class BreakRule
    {
        public static float ImpactSpeed(Vector3 relativeVelocity, Vector3 contactNormal);
        public static bool BreaksOnImpact(bool isFragile, bool isFalling, float impactSpeed, float breakSpeed);
    }
}

// Presentation, dauerhaftes NetworkObject (vom Host beim Start erzeugt):
public class ImpactEffects : NetworkBehaviour
{
    [Rpc(SendTo.ClientsAndHost)] void PlayImpactRpc(NetworkObjectReference item, Vector3 point, float speed);
    [Rpc(SendTo.ClientsAndHost)] void PlayBreakRpc(Vector3 point, /* Art-Kennung, Typ aus ADR-0002 */ ...);
}

// Verbindungen (Signatur aus dem NGO-2.13.3-Quelltext):
// public event Action<NetworkManager, ConnectionEventData> OnConnectionEvent
// ConnectionEventData { ConnectionEvent EventType; ulong ClientId; NativeArray<ulong> PeerClientIds; }
// ConnectionEvent: ClientConnected, PeerConnected, ClientDisconnected, PeerDisconnected
```

### Implementation Guidelines

- Spielzustand **muss** beim Host geändert werden. Clients **dürfen nie** NetworkVariables schreiben oder
  Gegenstände selbst bewegen; sie schicken Wünsche per `[Rpc(SendTo.Server, InvokePermission =
  RpcInvokePermission.Owner)]`. RPC-Namen enden auf `Rpc`.
- Der Host **muss** jeden Wunsch prüfen (Abstand, Zustand, wer hält was), bevor er ihn ausführt.
- Client-Vorhersage **muss** rein optisch bleiben: Kopien sind nicht vernetzt, ändern keinen Spielzustand
  und lösen weder Bruchregel noch Meldedienst aus.
- `OnCollisionEnter` für Spiel-Logik **darf nur** `ImpactReporter` beim Host auswerten (`IsServer`).
  Andere Systeme **müssen** am Meldedienst `ImpactEvents` zuhören.
- Trifft ein nicht kinematischer Körper einen liegenden Gegenstand, **muss** dessen `ImpactReporter` ihn im
  selben Callback zuerst auf „Fällt“ schalten und erst danach die Bruchregel prüfen.
- Kontaktpunkte **müssen** ohne Allokation gelesen werden (`contactCount` und `GetContact(i)`), nie über
  `collision.contacts`. Den `Collision`-Parameter **darf** niemand über den Callback hinaus speichern (Unity
  verwendet das Objekt wieder); was ein Zuhörer braucht, steht im `ImpactReport`.
- Ein zerbrochener Gegenstand **muss** genau einmal gemeldet und entfernt werden (Sperre gegen weitere
  Kontakte im selben Physikschritt).
- Effekte zu einem Gegenstand, der gleich entfernt wird, **müssen** von einem dauerhaften Objekt gesendet
  werden, nie vom entfernten Gegenstand selbst.
- Die Kollisionsprüfung **muss** an einer einzigen Stelle je Gegenstand beim Host abgeglichen werden:
  „ContinuousDynamic“ nur, wenn der Gegenstand fällt und nicht kinematisch ist, sonst „Discrete“. Eigener
  Code, der einen Körper kinematisch macht oder entfernt, **muss** vorher auf „Discrete“ stellen.
- Eine `NetworkObjectReference` **darf nur** aus einem gespawnten Objekt gebaut werden (`IsSpawned` prüfen);
  wer eine Referenz empfängt, **muss** ein fehlschlagendes `TryGet` vertragen.
- `ImpactEvents.Raise` **muss** jeden Zuhörer einzeln absichern, damit ein Fehler das Despawn nicht verhindert.
  Dafür darf es `GetInvocationList()` nutzen; die kleine Allokation je Aufprall ist bei wenigen Aufprallen pro
  Sekunde vertretbar. Zeigt der Profiler sie als Problem, hält `ImpactEvents` die Zuhörer in einer eigenen
  Liste.
- `Rigidbody.linearVelocity` **darf nur** beim Loslassen (Startgeschwindigkeit) und beim Aufheben (auf null)
  geschrieben werden. Laufende Kräfte gehen über `AddForce`.
- An- und Abmelden an einem Meldedienst (`ImpactEvents` hier, `SeasonEvents` aus ADR-0003) folgt einer Regel:
  NetworkBehaviours **müssen** sich in `OnNetworkSpawn` anmelden (nur bei `IsServer`) und in
  `OnNetworkDespawn` abmelden; das läuft auch, wenn jemand das Objekt direkt zerstört. Reine MonoBehaviours
  **müssen** sich in `OnEnable` an- und in `OnDisable` abmelden. NetworkBehaviours **dürfen** `OnDestroy` nur
  als `override` mit `base.OnDestroy()` haben: Ein eigenes `private void OnDestroy()` verdeckt
  `NetworkBehaviour.OnDestroy`, und NGO gibt dann die NetworkVariables nicht frei.
- Jedes statische Feld **muss** in einer Methode mit
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` zurückgesetzt werden.
- Rechenregeln (Bruch, Pose, Ofen) **müssen** reine statische Funktionen sein, damit EditMode-Tests sie ohne
  Szene prüfen.
- Neuer Code **muss** `OnConnectionEvent` statt `OnClientConnectedCallback`/`OnClientDisconnectCallback`
  nutzen und es vor `StartHost()`/`StartClient()` abonnieren. Host-Logik **darf nur** `ClientConnected` und
  `ClientDisconnected` auswerten. `PeerClientIds` ist nur beim eigenen `ClientConnected` eines Clients gefüllt
  (vorher `IsCreated` prüfen) und **darf nie** gespeichert werden: NGO gibt das Array direkt nach dem Aufruf
  frei (`Allocator.Temp`); wer die Liste braucht, kopiert sie.
- Objekt-Identität über das Netz **muss** über `NetworkObjectReference` oder `NetworkObjectId` laufen, nie
  über `EntityId` oder `InstanceID`.

## Alternatives Considered

### Alternative 1: Alles beim Server, auch der Spielerkörper
- **Description**: Der Client schickt nur Eingaben, der Host bewegt alle Körper. Damit es flüssig wirkt,
  sagt der Client die eigene Bewegung voraus und gleicht sie mit dem Host ab (Rückabgleich).
- **Pros**: Eine einzige Autorität, kein Vertrauen in Client-Positionen.
- **Cons**: Rückabgleich für einen Character Controller ist aufwendig und fehleranfällig; ohne ihn hinkt die
  eigene Bewegung um den Ping nach. Schummeln ist im Koop kein Problem.
- **Rejection Reason**: Viel Aufwand für einen Schutz, den das Spiel nicht braucht. Der Ist-Stand ist mit
  100 ms Ping und im Freundestest flüssig.

### Alternative 2: Distributed Authority (wer wirft, rechnet)
- **Description**: NGO-Topologie `DistributedAuthority`: Der Spieler, der einen Gegenstand hält oder wirft,
  besitzt und simuliert ihn.
- **Pros**: Würfe wären für den Werfer ohne Vorhersage-Kopie flüssig.
- **Cons**: Zerbrechen, Treffer auf Gäste und Tragen zu zweit hätten mehrere Entscheider. Connection
  Approval wird mit dieser Topologie nicht unterstützt (`modules/networking.md`). Großer Umbau.
- **Rejection Reason**: Widerspricht „der Host entscheidet“ und würde gebaute, getestete Systeme ersetzen.

### Teilentscheidungen (je kurz)
- **Kollisionsprüfung immer „ContinuousSpeculative“:** einfach, auch für kinematische Körper erlaubt, spart
  die Abgleichstelle, kann aber Geister-Kontakte vor Kanten erzeugen. Verworfen zugunsten „nur im Fall“;
  bleibt der getestete Ausweg (Verification 1, Architektur-Review Empfehlung 4).
- **Eigener Vorausblick per SphereCast:** volle Kontrolle, aber mehr Code und kein Aufprall-Ereignis von der
  Physik. Verworfen.
- **Event je Gegenstand oder Gäste suchen selbst (OverlapSphere):** Gäste müssten jeden Gegenstand kennen
  oder würden kurze Aufpralle verpassen. Verworfen zugunsten eines Meldedienstes.
- **Zustand „zerbrochen“ als NetworkVariable, oder RPC vom Gegenstand vor dem Despawn:** mehr Zustand
  beziehungsweise ungesicherte Reihenfolge. Verworfen zugunsten des Dauer-Objekts.
- **`OnConnectionEvent` sofort überall oder gar nicht:** Umbau ohne Gewinn beziehungsweise Umbau unter
  Zeitdruck, wenn NGO die alten Callbacks abschaltet. Verworfen zugunsten „neu ja, alt beim Umbau“.

## Consequences

### Positive
- Jedes neue System weiß, wo es entscheiden darf und wie Clients davon erfahren: beim Host, über
  NetworkVariables, NetworkTransform oder RPCs.
- Ein Messpunkt für Aufpralle: Zerbrechen, Geräusche und Gäste rechnen mit demselben `impact_speed`.
- Die durchgehende Kollisionsprüfung kostet nur, solange etwas fliegt.
- Bruchregel und Messung sind ohne Szene testbar (EditMode).

### Negative
- Ein statischer Meldedienst ist eine globale Abhängigkeit; Tests müssen Abonnenten selbst aufräumen.
- Der Client sieht einen Bruch um seinen Ping verspätet (etwa 100 ms), seine Kopie prallt vorher ab.
- `ImpactSound` muss umgebaut werden; der heutige, getestete Weg ändert sich.
- Owner-autoritative Körper lassen Schummeln zu (bewusst hingenommen, Koop unter Freunden).

### Neutral
- Der Zustand „Fällt“ ist nur beim Host bekannt; Clients brauchen ihn nicht.

## Risks
- **„ContinuousDynamic“ verhält sich in Unity 6.6 anders als erwartet** (Warnungen, Kosten, Wechsel mit
  kinematisch). Gegenmittel: Test in Plan-Schritt „Gegenstandsarten“; Ausweg „Continuous“ (nur gegen feste
  Geometrie) oder „ContinuousSpeculative“.
- **Schnelle Würfe treffen Gäste nicht** (Gäste sind keine feste Geometrie). Gegenmittel: Prüfung in Plan-
  Schritt 5; Ausweg: Gäste mit kinematischem Rigidbody, oder ein Treffer-Test per SphereCast nur für Gäste.
- **Vergessenes Abmelden vom Meldedienst** führt zu Aufrufen auf zerstörte Objekte. Gegenmittel: eine
  Abmelde-Regel für alle Meldedienste (Guidelines), Zurücksetzen bei `SubsystemRegistration`, Code-Review.
- **Eine liegende Tasse zerbricht nicht, wenn eine Kiste sie trifft**, weil sie im Callback noch „Liegt“.
  Gegenmittel: Wechsel auf „Fällt“ vor der Bruchregel im selben Callback (Entscheidung 4); PlayMode-Test.
- **Kopie und Original laufen auseinander** (Kopie fliegt weiter, Original zerbricht). Gegenmittel: Die
  Kopie verschwindet mit dem Original (gebaut).
- **Viele gleichzeitige Aufpralle** (umfallender Stapel) erzeugen viele RPCs. Gegenmittel: Sperrzeit je
  Gegenstand für Geräusche (`ImpactCooldown`, gebaut); Messung mit dem Network Profiler (QQ-02).
- **Beim Client verschwindet das Original zu früh:** `NetworkTransform` zeigt Positionen um einige Ticks
  verzögert (Interpolation), das Despawn wirkt sofort. Ein Wurf eines anderen Spielers kann beim Client vor
  der Wand verschwinden. Gegenmittel: Scherben am Ort des Hosts zeigen; wenn es im Test stört, das Original
  beim Client erst nach der Interpolationszeit ausblenden. (Trainingswissen, im Test prüfen.)
- **Host-RPC läuft mitten im Physik-Callback:** Beim Host führt NGO die eigene RPC sofort aus. Gegenmittel,
  falls Effekte dort stören: `DeferLocal = true` am `[Rpc]`-Attribut.
- **Ein Zuhörer wirft eine Exception** und verhindert das Despawn. Gegenmittel: `Raise` sichert jeden Zuhörer
  einzeln ab (siehe Guidelines).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| gegenstandsarten.md | TR-gegenstandsarten-003 (vorläufig): Bruchregel `breaks_on_impact`, der Host entscheidet (Formel 2) | `BreakRule` als reine Funktion, nur `ImpactReporter` beim Host wertet aus; ein getroffener liegender Gegenstand wechselt vor der Prüfung auf „Fällt“; Despawn plus Scherben vom Dauer-Objekt (Entscheidung 4, 5) |
| gegenstandsarten.md | TR-gegenstandsarten-005 (vorläufig): Würfe bis 15 m/s durchfliegen keine dünnen Wände (Open Question 3) | „ContinuousDynamic“ im Zustand „Fällt“, auch für die Vorhersage-Kopie (Entscheidung 3) |
| gegenstandsarten.md | Edge Cases: mehrere Kontakte im selben Physikschritt → einmal entfernt, ein Scherben-Geräusch; die Kopie zerbricht nie selbst | Sperre nach dem Bruch; Kopie löst nichts aus (Entscheidung 5, Guidelines) |
| gaeste.md | TR-gaeste-004 (vorläufig): Reaktion auf Lärm (Aufprall in der Nähe, zerbrochenes Geschirr) und Chaos (Treffer), Formel 5 | `ImpactEvents.Reported` liefert Ort, `ImpactSpeed`, Masse, `WasFalling`, `Broke` und den getroffenen Collider |
| Gebaute MVP-Systeme | Bewegung, Tragen und Werfen, Tragen zu zweit, Holzofen (Prototyp, ohne TR-ID) | Schreibt den getesteten Stand als Regel fest (Grundsatz, Entscheidung 1, 2) |

Die TR-IDs sind vorläufig; `/architecture-review` vergibt die festen.

## Performance Implications
- **CPU**: Durchgehende Kollisionsprüfung nur für fallende Gegenstände (selten mehr als eine Handvoll
  gleichzeitig). Ein `OnCollisionEnter` je Kontakt beim Host, ohne Allokation. Bei Clients rechnet keine
  Physik für Gegenstände.
- **Memory**: vernachlässigbar (eine Komponente je Gegenstand, ein Dauer-Objekt).
- **Load Time**: keine.
- **Network**: keine neue NetworkVariable. Je Bruch eine RPC plus Despawn. Aufprall-Geräusche wie heute mit
  Sperrzeit je Gegenstand. Ruhendes sendet nichts.

## Migration Plan

Der Ist-Stand bleibt; diese ADR hält ihn fest. Neu gebaut wird in drei Plan-Schritten:
1. **„Gegenstandsarten“:** `ImpactReporter`, `BreakRule`, `ImpactEvents` und `ImpactEffects` anlegen;
   `ImpactSound` verliert seinen eigenen `OnCollisionEnter` und wird zu einem Teil von `ImpactEffects`;
   eine Abgleichstelle für die Kollisionsprüfung je Gegenstand beim Host (deckt `Carryable.Detach`, `AttachTo`,
   Einschlafen und das Loslassen durch NGO in `OnNetworkObjectParentChanged` ab); Kopie mit „ContinuousDynamic“;
   Wechsel „Liegt → Fällt“ bei einem Treffer im `ImpactReporter`; Verification 1 mit beiden Varianten.
2. **„Erster Gast und Tee“:** Gäste hören an `ImpactEvents` zu (Lärm, Chaos).
3. **„Steam-Lobby“:** `ConnectionHud` und `PlayerSpawnApproval` wechseln auf `OnConnectionEvent`.

**Rollback plan**: Das Umschalten der Kollisionsprüfung sitzt an einer Stelle und lässt sich auf „Discrete“
(oder „ContinuousSpeculative“ ohne Umschalten) zurückstellen. Der Meldedienst ist ein Zusatz; ohne ihn bleibt `ImpactSound` wie heute.

## Validation Criteria
- **EditMode:** `BreakRule.BreaksOnImpact` mit den Beispielen aus GDD Formel 2 (4,2 / 6,1 / 8,0 m/s bei
  7,0) und `ImpactSpeed` (Wurf 40° schräg = 6,1 m/s).
- **PlayMode, nur Host:** Eine Tasse mit 15 m/s gegen eine 0,2 m dicke Innenwand fliegt nicht hindurch; eine
  Tasse zerbricht bei mehreren Kontakten im selben Schritt genau einmal, `ImpactEvents.Reported` meldet genau
  einmal `Broke = true`. GDD Gegenstandsarten, Kriterium 15: Eine liegende Tasse zerbricht, wenn eine Kiste
  mit 8 m/s sie trifft, und bleibt heil, wenn die Kiste aus Haltehöhe (5,01 m/s) darauf fällt.
- **Editor, MPPM mit 100 ms Ping:** Bruch bei Host und Client sichtbar, genau ein Scherben-Klang je Rechner,
  keine Fehler oder Warnungen zur Kollisionsprüfung in beiden Logs; Tragen und Werfen fühlen sich für den
  Client so an wie in Prototyp 0.1.4.
- **Freundestest:** GDD Gegenstandsarten, Abnahmekriterium 28 (Wurf gegen die Wand beim Client).

## Related
- `docs/architecture/architecture.md` (Architektur-Prinzipien 1, 2, 4, 5; QQ-06 hiermit beantwortet)
- `design/gdd/gegenstandsarten.md` (Formel 2, Edge Cases, Open Question 3)
- `design/gdd/gaeste.md` (Formel 5, Edge Cases zu Treffern)
- `docs/engine-reference/unity/modules/networking.md` (Ist-Stand im Detail)
- Code: `Assets/_Project/Scripts/Carrying/` (`Carryable`, `PlayerCarry`, `HeavyCarryable`, `CarryCollision`,
  `LocalGhost`), `Assets/_Project/Scripts/Audio/ImpactSound.cs`, `Assets/_Project/Scripts/Net/`
- Nachfolgende ADRs: ADR-0002 (Katalog, Art-Kennung, Host-Funktion „Entfernen“), ADR-0003 (Meldungen),
  ADR-0004 (Zustand „Am Körper“), ADR-0005 (Gäste), ADR-0009 (Ragdoll)
- `docs/architecture/architecture-review-2026-11-09.md` (Befunde, die diese Fassung einarbeitet)
