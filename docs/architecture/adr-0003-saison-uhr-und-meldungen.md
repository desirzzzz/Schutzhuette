# ADR-0003: Saison-Uhr, Spielzeit und Meldungen zwischen Systemen

## Status

Accepted (2026-11-09, vom Entwickler über `/architecture-decision accept ADR-0003`)

> **Wer auf `Accepted` setzen darf: der Entwickler, oder `technical-director` nach ausdrücklicher Bestätigung
> des Entwicklers.** Kein anderer Agent und kein Skill allein. Stories, die auf eine `Proposed`-ADR
> verweisen, sind `Blocked`.

## Date

2026-11-09

## Last Verified

2026-11-09

## Decision Makers

Entwickler (Projektleitung) mit Claude über `/architecture-decision`. Engine-Prüfung: `unity-specialist`
(2026-11-09: keine Blocker; Hinweise zu `OnValueChanged` beim Host, Startabgleich, Teleport und
`timeScale`-Fallen eingearbeitet, die wichtigsten im Quelltext nachgelesen). TD-ADR übersprungen
(Review-Modus lean). Architektur-Review 2026-11-09 (`architecture-review-2026-11-09.md`, CONCERNS)
eingearbeitet: Gutachter-Befund 2 und W4 (Client-Spielzeit immer `max`), W1 (`JoinRules` in Foundation), W2
(eine Abmelde-Regel), W3 (Entfernen über ADR-0002), W5 (`ClientJoined`), Empfehlungen 3 (Eingaben bei offenem
Menü aus) und 7 (`Teleport`), Hinweise 8, 9 und 11.

## Summary

Die Saison braucht eine Uhr, die alle Spieler gleich sehen, eine gemeinsame Spielzeit `game_time`, die in
der Solo-Pause steht, und Meldungen wie „Saison beginnt“ an Ofen, Lager, Gäste und Kasse. Entschieden ist:
Der Host führt die Saison in einer reinen C#-Klasse und überträgt nur einen gemeinsamen Wert (Phase,
Phasenstart, Pausensumme, Saisonzähler); alle Rechner rechnen Spielzeit, Fortschritt und Druck selbst aus der
NGO-Serverzeit. Die Pause hält Physik und Animation über `Time.timeScale = 0` an, Meldungen gehen wie bei
ADR-0001 über einen statischen Meldedienst nur beim Host.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.6f1, Netcode for GameObjects 2.13.3 |
| **Domain** | Core (Zeit, Spielablauf) mit Networking |
| **Layer** | Core |
| **Knowledge Risk** | HIGH (Unity 6.6 und NGO 2.13 liegen nach dem Wissensstand des Modells, `VERSION.md`) |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md` (Block „NGO 2.13 for this project“), `breaking-changes.md` und `current-best-practices.md` (6.6: Domain Reload aus), `deprecated-apis.md`; NGO 2.13.3 Quelltext in `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea`: `Runtime/Timing/NetworkTimeSystem.cs`, `NetworkTickSystem.cs`, `RealTimeProvider.cs`, `NetworkTime.cs`, `Runtime/Core/NetworkManager.cs`, `Runtime/NetworkVariable/NetworkVariable.cs`, `Runtime/Messaging/Messages/NetworkVariableDeltaMessage.cs`, `Runtime/NetworkVariable/Serialization/TypedILPPInitializers.cs`, `Runtime/NetworkVariable/Collections/NetworkList.cs`, `Runtime/Messaging/RpcTargets/OwnerRpcTarget.cs`; `Assets/_Project/Scripts/Net/PlayerSpawnApproval.cs`, `Hut/Stove.cs`; `docs/registry/architecture.yaml` (Regeln aus ADR-0001 und ADR-0002) |
| **Post-Cutoff APIs Used** | `NetworkManager.ServerTime` (`NetworkTime.Time`, double); `NetworkVariable<T>` mit eigenem `unmanaged` struct (`INetworkSerializable`, `IEquatable<T>`); `NetworkList<ulong>`; `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` und `[Rpc(SendTo.Owner)]`; `NetworkManager.OnConnectionEvent` (ADR-0001) |
| **Verification Required** | (1) Solo mit Testprofil: Esc-Menü 20 s offen im Nachmittag, während eine geworfene Kiste fliegt → sie hängt in der Luft und fliegt danach weiter, `ServerTime` im Log läuft weiter, `game_time` steht, keine NaN-Werte in Bewegung oder Kamera (GDD Saison-Ablauf, Kriterium 28). (2) Play bei offenem Menü (Pause) beenden, neu starten → `Time.timeScale` ist im ersten Bild 1. (3) Mit Network Simulator (100 ms): Beim Client wird `game_time` in keinem Bild kleiner als im Bild davor (Log-Prüfung), und `|gt − start| ≤ 0,5 s` bei `phase-received` (Kriterium 37). (4) Beim Client gibt es keine Log-Zeile, in der die Phase neu ist, der Phasenstart aber noch alt, und `PhaseChanged` feuert beim Host genau einmal pro Wechsel. (5) Der struct `SeasonSnapshot` in einer NetworkVariable kompiliert und kommt beim Client an. (6) Ein Client, der in der Bewertung beitritt, sieht Phase und Saison-Nr. richtig, auch wenn andere Objekte vor `SeasonClock` bei ihm erscheinen. (7) Beim Saisonstart springt der Spieler des Freundes beim Host an den Startpunkt, ohne quer durch die Hütte zu gleiten (Kriterium 40). (8) Solo mit offenem Menü: Maus und Tasten bewegen weder Blick noch Figur, E und Werfen tun nichts; nach dem Schließen geht alles wieder. |

**Aus dem NGO-2.13.3-Quelltext belegt** (2026-11-09 gelesen):
- Die Netzzeit läuft mit `Time.unscaledDeltaTime` (`RealTimeProvider.UnscaledDeltaTime`, aufgerufen in
  `NetworkTimeSystem.UpdateTime`). `Time.timeScale = 0` hält `ServerTime` also **nicht** an.
- `NetworkManager.ServerTime` wird jedes Bild in `NetworkUpdateStage.PreUpdate` neu gesetzt
  (`NetworkManager.cs`, `NetworkTickSystem.UpdateTick`); `.Time` ist ein `double` in Sekunden.
- Beim Host entsteht mit jedem Start ein neues Zeitsystem ohne Puffer und ohne harten Sprung
  (`NetworkTimeSystem.ServerTimeSystem()` = `new NetworkTimeSystem(0, 0, double.MaxValue)`); die Serverzeit
  beginnt bei 0.
- Beim Client wird die geschätzte Serverzeit mit 1 % Tempo nachgeführt (`k_DefaultAdjustmentRatio = 0.01`),
  liegt um einen Puffer (Standard 0,05 s) hinter dem Host und **springt**, wenn die Abweichung größer als
  0,2 s ist (`k_HardResetThresholdSeconds`, `Advance`). Der Sprung kann rückwärts gehen.
- `OnValueChanged` feuert in `NetworkVariable.ReadDelta`, also für jede NetworkVariable einzeln, während die
  später gelesenen Variablen derselben Nachricht noch ihren alten Wert haben
  (`NetworkVariableDeltaMessage.Handle`).
- Eigene `unmanaged` structs mit `INetworkSerializable` gehen in eine NetworkVariable
  (`InitializeSerializer_UnmanagedINetworkSerializable<T>`), Gleichheit über `IEquatable<T>`
  (`InitializeEqualityChecker_UnmanagedIEquatable<T>`). `NetworkList<T>` verlangt `unmanaged, IEquatable<T>`.
- `[Rpc(SendTo.Owner)]` auf einem Objekt, das dem Host selbst gehört, wird lokal ausgeführt
  (`OwnerRpcTarget.Send`).
- Der Setter von `NetworkVariable.Value` ruft `OnValueChanged` sofort selbst auf, also auch beim Host
  (`NetworkVariable.cs`, Setter von `Value`). Der erste Abgleich beim Spawn (`ReadField`) ruft es **nicht**
  auf.
- `NetworkBehaviour.OnDestroy` gibt alle NetworkVariables und NetworkLists frei (`networkVar.Dispose()`);
  wer `OnDestroy` überschreibt, muss `base.OnDestroy()` aufrufen.
- `NetworkTransform.Teleport(pos, rot, scale)` setzt die Werte sofort, ohne Interpolation, und wirft eine
  Exception, wenn die nicht autoritative Seite es aufruft (`NetworkTransform.cs`, `Teleport` prüft
  `CanCommitToTransform`). `SetState(…, teleportDisabled: false)` landet in derselben internen Methode, weicht
  auf der nicht autoritativen Seite aber aus: Beim Host schickt es den Wunsch per RPC an den Besitzer weiter
  (`SetStateClientRpc`), bei einem fremden Client loggt es nur einen Fehler.
- `serializer.SerializeValue(ref Phase)` für ein enum mit `byte` als Basistyp hat eine eigene Überladung
  `where T : unmanaged, Enum` (`Serialization/BufferSerializer.cs`) und braucht 1 Byte (vom
  `unity-specialist` gelesen).

**Im Architektur-Review belegt** (`unity-specialist`, 2026-11-09, Stichproben nachgelesen):
- Die Zeit des Clients läuft erst, wenn er verbunden ist (`Timing/NetworkTimeSystem.cs` 155), und
  `ConnectionApproved` setzt sie zurück, bevor die Objekte erscheinen
  (`Messaging/Messages/ConnectionApprovedMessage.cs` 294–296). Nach `OnNetworkSpawn` von `SeasonClock` gibt es
  also keinen „großen Rücksprung vor dem ersten Zeitabgleich“ mehr.
- Die Interpolation von `NetworkTransform` rechnet mit skalierter Zeit (`Components/NetworkTransform.cs` 4254,
  `RealTimeProvider.DeltaTime`). `timeScale = 0` würde bei Clients Bewegungen einfrieren.
- `NetworkVariable<SeasonSnapshot>` braucht kein `[GenerateSerializationForType]`, solange der Wert ein Feld
  des NetworkBehaviours ist (`Editor/CodeGen/NetworkBehaviourILPP.cs` 1406–1429).

**NOT SOURCEABLE** (nicht in `docs/engine-reference/` oder im Quelltext belegt, deshalb in „Verification
Required“):
- Was `Time.timeScale = 0` alles anhält (`FixedUpdate` und Physik, `Time.deltaTime` = 0, `Time.time` steht,
  Animator im Modus „Normal“, Partikel) und was weiterläuft (`Update`, `Time.unscaledDeltaTime`, Audio,
  `WaitForSecondsRealtime`). Trainingswissen aus älteren Unity-Versionen; Prüfung (1).
- Ob Unity `Time.timeScale` beim Beenden von Play zurücksetzt, wenn Domain Reload aus ist. Die Entscheidung
  setzt es deshalb selbst zurück; Prüfung (2).
- In welcher Reihenfolge NGO die schon vorhandenen Objekte bei einem neuen Client erzeugt. Die Entscheidung
  verlangt deshalb, dass `SeasonClock.Current == null` erlaubt ist; Prüfung (6).

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Host entscheidet, Meldedienst-Muster `ImpactEvents`, `OnConnectionEvent`, RPC-Form; muss vor der Umsetzung `Accepted` sein), ADR-0002 (Startaufstellung erzeugt Gegenstände über den Katalog und die Host-Funktionen zum Erscheinen und Entfernen (`ItemSpawning`); Startwerte von NetworkVariables in `OnNetworkSpawn`; muss vor der Umsetzung `Accepted` sein) |
| **Enables** | ADR-0004 (Interaktion rechnet `dt` mit `game_time`), ADR-0005 (Gäste hören auf Phasen-Meldungen und lesen `season_pressure`), ADR-0006 (Steam-Transport übernimmt die Beitrittsregel „nur zwischen den Saisons“), ADR-0007 (UI liest `SeasonClock` für HUD und Menüs) |
| **Blocks** | Plan-Schritt „Saison“ in `production/milestones/vertical-slice.md`; das Abkühlen von Gerichten (ADR-0002, Ordering Note). Epics und Stories gibt es noch keine. |
| **Ordering Note** | `CrateSpawner` wird durch die Startaufstellung ersetzt, sobald der Katalog aus ADR-0002 gebaut ist. Den Ofen erst auf `game_time` umstellen, wenn `SeasonClock` existiert. |

## Context

### Problem Statement

Das GDD Saison-Ablauf beschreibt eine Saison mit Phasen, eine Spielzeit `game_time` als **einzige Zeitbasis**
für Spielregeln (Abkühlen, Ofen, Fortschritt von Handlungen) und Meldungen an alle anderen Systeme. Drei
weitere GDDs rechnen schon mit `game_time` oder hören auf Phasen. Offen sind: wie die Uhr übers Netz kommt,
wie die Solo-Pause Physik und Animation anhält (QQ-05), wie die Meldungen aussehen und wie Startaufstellung,
Beitrittssperre und Abbruch technisch laufen. Das muss vor dem ersten Code feststehen, weil sonst jedes
System seine eigene Zeit benutzt.

### Current State

- Es gibt keine Saison und keine Spielzeit. Regeln rechnen mit `Time.deltaTime` (`Stove.cs`: Brennzeit pro
  Bild) und `Time.time` (Abkühlzeiten von Geräuschen, Zeitgrenzen beim Greifen).
- `CrateSpawner` legt Kisten, Holz und Truhe beim Host und legt nach einer Wartezeit neu nach.
- `PlayerSpawnApproval` prüft beim Beitritt Version und freie Plätze (`ConnectionApprovalCallback`,
  `response.Reason`) und weist Startpunkte zu.
- Es gibt kein Esc-Menü mit Pause; Esc gibt nur den Mauszeiger frei.

### Constraints

- CLAUDE.md Architektur-Regeln 1–6: Multiplayer ab Tag 1, Host entscheidet, Daten in ScriptableObjects,
  kleine Komponenten, Solo = Host ohne Clients. Domain Reload ist aus.
- Register: Meldungen nach dem Muster `impact_report` (ADR-0001 verlangt, dass ADR-0003 es übernimmt oder
  ausdrücklich ersetzt); Wünsche der Clients als RPC von der eigenen Spielfigur (`client_request`);
  `OnConnectionEvent` (`connection_events`); Startwerte in `OnNetworkSpawn`
  (`network_variable_initial_value`); verboten: `client_writes_game_state`.
- Schichten (architecture.md): Saison ist Core. Nach unten wird aufgerufen, nach oben nur gemeldet. GDD
  Saison-Ablauf, Kriterium 33: Die Saison-Skripte verweisen auf keine Klasse aus `Hut/`, `Guests/` oder
  `Weather/`.
- Der eigene Spielerkörper ist owner-autoritativ (ADR-0001). `SetState` beim Host würde den Wunsch zwar per
  RPC an den Besitzer weiterreichen, aber dessen aktiver `CharacterController` überschreibt eine von außen
  gesetzte Position. Der Besitzer muss sich also selbst versetzen.
- Schichten (architecture.md): Foundation (Netz-Sitzung, `Net/`) darf nichts aus Core kennen. Die
  Beitrittsregel braucht aber „läuft eine Saison?“.
- Kein neues Paket.

### Requirements

- Alle sehen dieselbe Phase zur selben Zeit; übertragen werden nur Phase, Phasenstart, Pausensumme,
  Saisonzähler und Bereit-Liste (GDD Kriterium 39, TR-saison-001).
- `game_time` nach Formel 3a: läuft wie die Serverzeit, steht in der Solo-Pause, springt nie und läuft nie
  rückwärts (TR-saison-002).
- Meldungen „Saison beginnt“, „Phase X beginnt“, „Nacht vorbei“ mit Grund, dazu jederzeit lesbar: Phase,
  Fortschritt, Druck, `game_time`, „Spielzeit läuft“ (TR-saison-003, -004).
- Startaufstellung nach Plätzen, Beitritt nur zwischen den Saisons, Abbruch mit Rückfall der Kasse
  (TR-saison-005, -006, -007).
- `dt` von Handlungen ist vergangene `game_time` (TR-interaktion-003); Gäste frieren außerhalb von
  Nachmittag, Abend und Nacht ein (TR-gaeste-007).
- Alle Formeln und der Ablauf als reine Funktionen, testbar in EditMode mit übergebener Serverzeit
  (`Step(double serverTime)`, GDD Acceptance Criteria).

## Decision

**1. Ablauf in einer reinen C#-Klasse beim Host.** `SeasonDirector` ist eine normale C#-Klasse (kein
MonoBehaviour). Sie hält Zustand, Phasenstart, Pausensumme, Saisonzähler und die Bestätigungen und wird nur
beim Host benutzt. Sie bekommt die Serverzeit als Parameter (`Step(double serverTime)`,
`MissionFinished(double serverTime)` usw.) und meldet nach außen über eine Schnittstelle `ISeasonOutput`
(Gegenstände entfernen, Startaufstellung legen, Meldungen, Log). So laufen alle Ablauf-Tests aus dem GDD
(Kriterien 1, 10, 13, 14, 19, 31) in EditMode mit Stubs, ohne Szene und ohne Netz. Die Formeln 1–7 stehen
rein statisch in `SeasonMath`.

**2. Übers Netz geht ein gemeinsamer Wert.** Die Komponente `SeasonClock` (NetworkBehaviour, Core) sitzt an
einem dauerhaften NetworkObject, das der Host beim Start erzeugt, wie `ImpactEffects` (ADR-0001). Sie hält
genau zwei Netzwerkwerte:
- `NetworkVariable<SeasonSnapshot>`: Phase, Phasenstart (in `game_time`), Pausensumme und Saisonzähler in
  einem struct. Grund (NGO-Quelltext): `OnValueChanged` feuert für jede NetworkVariable einzeln; in getrennten
  Variablen sähe der Client kurz die neue Phase mit dem alten Start. Der struct ändert sich etwa sechsmal pro
  Saison.
- `NetworkList<ulong>` mit den Spielern, die bestätigt haben: in der Vorbereitung „Bereit“, in der Bewertung
  „Weiter“. Bei jedem Phasenwechsel wird die Liste geleert.

Fortschritt, Druck und verstrichene Zeit werden **nie** übertragen; jeder Rechner rechnet sie mit
`SeasonMath` selbst. Nur der Host schreibt beide Werte; den Startwert schreibt er in `OnNetworkSpawn`
(ADR-0002).

**3. Spielzeit aus der NGO-Serverzeit.** Grundlage ist `NetworkManager.ServerTime.Time`, nicht
`LocalTime` (eine Vorausschätzung für Eingaben) und nicht `Time.time`.
- Beim Host: `game_time = (is_paused ? pause_time : server_time) − paused_total` (GDD Formel 3a).
  `pause_time` und `is_paused` kennt nur der Host.
- Beim Client: `game_time = max(game_time des letzten Bildes, server_time − paused_total)`. Der Client ist nie
  in Pause (Pause geht nur ohne Clients). Das `max` fängt die Rücksprünge der NGO-Zeitschätzung ab: Statt
  rückwärts zu laufen, steht die Spielzeit dann kurz still. So ist `dt` nie negativ und `game_time` läuft
  nie rückwärts (GDD Formel 3a). Das `max` gilt **immer**, ohne Ausnahme für große Sprünge. Grund
  (NGO-Quelltext, Architektur-Review): Die Client-Zeit läuft erst nach dem Verbinden und wird zurückgesetzt,
  bevor Objekte erscheinen; ein großer Rücksprung vor dem ersten Abgleich kommt nach `OnNetworkSpawn` nicht
  mehr vor. Einen Rücksprung über 0,5 s loggt der Client nur (`[Season] time-jump`), er übernimmt ihn nicht.
  Den gemerkten Wert setzt er **nur** in `OnNetworkSpawn` zurück, weil die Serverzeit bei jedem Host-Start
  bei 0 beginnt.
- `SeasonClock.PhaseElapsed` liefert nie weniger als 0 (die Formel aus dem GDD darf beim Client kurz negativ
  werden, die Anzeige nicht).
- `SeasonClock.Current.GameTime` ist der einzige Zugang. Reine Funktionen bekommen die Spielzeit als
  Parameter (wie `DishRules.IsWarm` aus ADR-0002).

**4. Solo-Pause über `Time.timeScale` (beantwortet QQ-05).** Pause gilt, wenn das Menü des Hosts offen ist,
der Host allein ist und die Phase Nachmittag, Abend oder Nacht ist (GDD Regel 11). Dann setzt `SeasonClock`
`Time.timeScale = 0`, danach wieder 1. Das hält Physik, Spielerbewegung, Animator und Partikel an einer Stelle
an; NGO und das Menü rechnen mit unskalierter Zeit und laufen weiter. Nur `SeasonClock` schreibt
`Time.timeScale`. Es setzt den Wert in `OnNetworkSpawn`, `OnNetworkDespawn` und in einer
`SubsystemRegistration`-Methode auf 1, weil Domain Reload aus ist. „Allein“ erfährt der Host über
`OnConnectionEvent` (ADR-0001): Geht der letzte Client, während das Menü offen ist, beginnt die Pause in
diesem Moment. Dass die Pause nur ohne Clients gilt, ist auch technisch wichtig: Die Interpolation von
`NetworkTransform` rechnet mit skalierter Zeit (NGO-Quelltext), bei Clients würde `timeScale = 0` also
Bewegungen einfrieren. In der Pause ist `Time.deltaTime` 0; Code, der durch `Time.deltaTime` teilt, muss das
abfangen (der heutige Code tut es mit `Mathf.Max(Time.deltaTime, 0.0001f)`).

`Time.timeScale` hält keine Eingaben an: Mausdrehung liefe weiter, und die eigenen RPCs des Hosts (Aufheben,
Werfen) laufen sofort lokal. Deshalb schaltet das Menü beim Öffnen die Gameplay-Action-Maps des Input Systems
aus (Bewegung, Blick, Interaktion) und beim Schließen wieder an, mit und ohne Pause. Das Menü selbst nutzt
eine eigene Action-Map, die an bleibt.

**5. Meldungen über einen Meldedienst beim Host (übernimmt das Muster aus ADR-0001).** Die statische Klasse
`SeasonEvents` meldet nur beim Host an Spiel-Logik: `SeasonStarted`, `PhaseBegan`, `NightOver` (Grund
`Mission`, `Timeout`, `Abort`) und neu `SeasonAborted`. Jeder Zuhörer wird einzeln in try/catch aufgerufen;
die Abonnenten werden bei `SubsystemRegistration` gelöscht. Reihenfolgen:
- Start: Gegenstände entfernen → `SeasonStarted` (Systeme setzen sich zurück) → Startaufstellung legen →
  `PhaseBegan(Afternoon)`.
- Abbruch: in der Nacht `NightOver(Abort)` → `SeasonAborted` → `PhaseBegan(Preparation)`.
- Mehrere Phasenwechsel in einem Schritt (nach einem Hänger) melden jede Phase einzeln in der richtigen
  Reihenfolge (GDD Kriterium 14).

Anzeigen auf allen Rechnern (HUD, Licht, Musik) hören nicht am Meldedienst, sondern lesen
`SeasonClock.Current` und das Event `SeasonClock.PhaseChanged`, das nur für Darstellung da ist. Es hat genau
eine Quelle: `SeasonClock` abonniert in `OnNetworkSpawn` auf **jedem** Rechner `OnValueChanged` des
Snapshots und löst darin `PhaseChanged` aus, wenn sich die Phase geändert hat. Beim Host feuert das schon
beim Setzen des Werts (NGO-Quelltext); ein zusätzlicher Aufruf von Hand würde doppelt melden. Weil der erste
Abgleich beim Spawn `OnValueChanged` nicht auslöst, übernimmt `SeasonClock` den Startwert in
`OnNetworkSpawn` einmal selbst. Schreibt der Host mehrere Werte in einem Bild (mehrere Phasenwechsel nach
einem Hänger), kommt beim Client nur der letzte an; Anzeigen dürfen also keine Phase voraussetzen.

**6. Wünsche und Aufrufe.** Clients schicken „Bereit“ und „Weiter“ per
`[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` von ihrer eigenen Spielfigur
(Komponente `PlayerSeasonRequests`); der Host gibt sie mit der Absender-Kennung an `SeasonClock` weiter.
„Weiter zu den Upgrades, wenn alle verbundenen Spieler ‚Weiter‘ gedrückt haben“ (GDD Regel 8) braucht die
Liste der Verbundenen: `SeasonClock` übergibt beim Spawn die schon verbundenen Kennungen
(`NetworkManager.ConnectedClientsIds`) an `SeasonDirector.ClientJoined` und danach jedes `ClientConnected` und
`ClientDisconnected` aus `OnConnectionEvent` (ADR-0001). Grund für den ersten Schritt: Das Uhr-Objekt
erscheint erst nach `StartHost()` und verpasst die Meldung über den eigenen Client.
Starten, Abbrechen, „Weiter“ des Hosts, „Nächste Saison“ und das Menü sind Host-Aufrufe an `SeasonClock`.
Der Nachteinsatz ruft nur `SeasonClock.MissionFinished()` auf und kennt sonst nichts von der Uhr.

**7. Startaufstellung, Beitritt, Abbruch.**
- Die Startaufstellung ist ein Datenobjekt `StartLayout` mit Einträgen „Platz-Kennung → Art (und Ware)“. In
  der Szene stehen Marker `ItemSlot` mit derselben Kennung. Beim Start entfernt der Host jeden Gegenstand
  (jedes gespawnte Objekt mit `Item`, auch getragene) mit `ItemSpawning.Remove` und erzeugt die neuen mit
  `ItemSpawning.Spawn` (beide ADR-0002). Ein unbekannter Eintrag lässt den Platz leer und loggt einen
  Fehler. Eine reine Prüffunktion vergleicht Aufstellung, Marker-Kennungen und Katalog.
- Spieler stellt der Host über `[Rpc(SendTo.Owner)]` an ihren Startpunkt; der Besitzer versetzt seinen
  Körper selbst (owner-autoritativ, ADR-0001): `CharacterController` aus, Position und Drehung setzen,
  `NetworkTransform.Teleport(pos, rot, transform.localScale)` (springt, statt zu gleiten; wirft auf der
  falschen Seite eine Exception, statt still auszuweichen), Fallgeschwindigkeit auf 0, `CharacterController`
  wieder an. Ein aktiver `CharacterController` würde eine direkt gesetzte Position sonst überschreiben.
  Startpunkte sind die aus `PlayerSpawnApproval`.
- Beitritt: Eine reine Funktion `JoinRules.Decide` prüft in der Reihenfolge Version → Saison läuft → voll;
  `PlayerSpawnApproval` ruft sie im `ConnectionApprovalCallback` auf und gibt den Grund über
  `response.Reason` zurück. Beide liegen in Foundation (`Net/`, Namespace `Schutzhuette.Net`), damit die
  Netz-Sitzung nichts aus Core kennt (Schichtenregel). `JoinRules` kennt deshalb keine Phase, nur den Wert
  „Saison läuft“ (`bool`, wahr in Nachmittag, Abend und Nacht; GDD Regel 10). Diesen Wert hält
  `PlayerSpawnApproval` (`SeasonRunning`); `SeasonClock` setzt ihn beim Host bei jedem Phasenwechsel von oben
  (Core ruft Foundation, also nach unten). Hält der Server an,
  fällt er auf `false`. Dieselbe Regel nutzt später der Steam-Transport (ADR-0006).
- Solange ein angenommener Spieler noch nicht erschienen ist, lehnt der Director „Saison starten“ ab
  (`start-blocked reason=join-pending`). Die Zahl wartender Beitritte liest `SeasonClock` bei
  `PlayerSpawnApproval` (`PendingJoins`) und übergibt sie an `TryStart`.
- Abbruch: Die Hüttenkasse merkt sich ihren Stand bei `SeasonStarted` und setzt ihn bei `SeasonAborted`
  zurück. Der Saisonzähler steigt erst bei „Nächste Saison“, ein Abbruch ändert ihn nicht.

### Architecture Diagram

```
 Host                                                                        Client
 ────                                                                        ──────
 Nachteinsatz ──MissionFinished()──┐        PlayerSeasonRequests ◄──RPC── eigene Spielfigur
 Menü/HUD (Host) ──Start/Abbruch/──┤             (Bereit, Weiter)            („Bereit“, „Weiter“)
                    Menü offen     ▼                │
                              SeasonClock ◄─────────┘
                    (NetworkBehaviour, Core)
                     │  Update: Step(ServerTime)
                     ▼
               SeasonDirector (reines C#) ── SeasonMath (Formeln 1–7)
                     │ ISeasonOutput
       ┌─────────────┼──────────────────────────┐
       ▼             ▼                          ▼
 Gegenstände    SeasonEvents (statisch,    Time.timeScale (nur Pause)
 entfernen /    nur Host): SeasonStarted,
 Startauf-      PhaseBegan, NightOver,
 stellung       SeasonAborted
 (ADR-0002)          │
                     ▼
       Ofen, Lager, Gäste, Kasse, Einsatz (setzen sich selbst zurück)

 SeasonClock schreibt:  NetworkVariable<SeasonSnapshot> ─────────────► OnValueChanged → PhaseChanged
                        NetworkList<ulong> (bestätigt) ──────────────► Bereit-/Weiter-Anzeige
 SeasonClock setzt (Host, nach unten): PlayerSpawnApproval.SeasonRunning (Foundation, Net/) → JoinRules
 Alle Rechner lesen:    GameTime, Phase, PhaseElapsed, PhaseProgress, Pressure, IsGameTimeRunning
                        (aus ServerTime + SeasonSnapshot, beim Client immer max gegen Rücksprung)
```

### Key Interfaces

Vorschlag; die genauen Namen legt der Bau fest. Ort: `Assets/_Project/Scripts/Core/`, Namespace
`Schutzhuette.Core`.

```csharp
namespace Schutzhuette.Core
{
    public enum SeasonPhase : byte { Preparation, Afternoon, Evening, Night, Rating, Upgrades }
    public enum NightEndReason : byte { Mission, Timeout, Abort }

    /// <summary>Der einzige Netzwerkwert der Uhr. Kommt immer vollständig an.</summary>
    public struct SeasonSnapshot : INetworkSerializable, IEquatable<SeasonSnapshot>
    {
        public SeasonPhase Phase;
        public int SeasonNumber;
        public double PhaseStartTime;   // in game_time
        public double PausedTotal;      // Summe aller abgeschlossenen Pausen der Sitzung
        // NetworkSerialize: alle vier Felder, lesen und schreiben in derselben Reihenfolge.
        // Equals: alle vier Felder, sonst erkennt NGO eine Änderung nicht.
    }

    /// <summary>Saison-Profil (Datenobjekt): Phasenlängen, night_min/max, p[0..3]. Spiel- und Testprofil.</summary>
    public class SeasonProfile : ScriptableObject { /* Felder mit [Min], nicht [Range] (GDD) */ }

    /// <summary>Formeln 1–7 aus GDD Saison-Ablauf. Rein, ohne Szene testbar.</summary>
    public static class SeasonMath
    {
        public static double GameTime(double serverTime, bool isPaused, double pauseTime, double pausedTotal);
        public static double PhaseElapsed(double gameTime, double phaseStartTime);
        public static float PhaseProgress(double phaseElapsed, float referenceLength);
        public static float ReferenceLength(SeasonPhase phase, SeasonProfile profile);
        public static float Pressure(SeasonPhase phase, float phaseProgress, SeasonProfile profile);
        public static bool PhaseDataValid(SeasonProfile profile, out string error);
        public static bool SeasonLengthInTarget(SeasonProfile profile, out string hint);
    }

    /// <summary>Was der Director nach außen braucht. Produktion: SeasonClock. Tests: Stub.</summary>
    public interface ISeasonOutput
    {
        void RemoveAllItems();
        void LayStartLayout();                 // Gegenstände und Spieler an ihre Plätze
        void SeasonStarted(int seasonNumber);
        void PhaseBegan(SeasonPhase phase, double startGameTime);
        void NightOver(NightEndReason reason);
        void SeasonAborted();
        void Log(string line);                 // [Season]-Zeilen aus dem GDD
    }

    /// <summary>Ablauf der Saison. Nur beim Host. Bekommt die Serverzeit immer übergeben.</summary>
    public sealed class SeasonDirector
    {
        public SeasonDirector(SeasonProfile profile, ISeasonOutput output);
        public SeasonSnapshot Snapshot { get; }
        public IReadOnlyCollection<ulong> Confirmed { get; }
        public bool IsPaused { get; }
        public double GameTime(double serverTime);

        public void Step(double serverTime);
        public bool TryStart(double serverTime, int pendingJoins);   // false: start-blocked
        public void Abort(double serverTime);
        public void MissionFinished(double serverTime);              // nur in der Nacht wirksam
        public void Confirm(ulong clientId, bool value);             // Bereit / Weiter
        public void HostContinue(double serverTime);                 // Bewertung → Upgrades
        public void NextSeason(double serverTime);                   // Upgrades → Vorbereitung
        public void SetPauseInputs(bool menuOpen, bool hostAlone, double serverTime);
        public void ClientJoined(ulong clientId);                    // zählt ab jetzt bei „alle verbunden“ mit
        public void ClientLeft(ulong clientId, double serverTime);
    }

    /// <summary>Meldedienst. Nur der Host löst aus, nur Host-Code hört zu (Muster aus ADR-0001).</summary>
    public static class SeasonEvents
    {
        public static event Action<int> SeasonStarted;                // nach dem Entfernen, vor der Aufstellung
        public static event Action<SeasonPhase, double> PhaseBegan;   // Phase, Beginn in game_time
        public static event Action<NightEndReason> NightOver;
        public static event Action SeasonAborted;                     // vor PhaseBegan(Preparation)

        // Raise-Methoden nur für SeasonClock; jeder Zuhörer einzeln in try/catch (Debug.LogException).

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { /* alle vier Events = null */ }
    }

    /// <summary>Die Uhr im Netz. Dauerhaftes NetworkObject, vom Host beim Start erzeugt.</summary>
    public class SeasonClock : NetworkBehaviour, ISeasonOutput
    {
        public static SeasonClock Current { get; private set; }   // null, solange nicht gespawnt

        // Lesen, alle Rechner:
        public SeasonPhase Phase { get; }
        public int SeasonNumber { get; }
        public double GameTime { get; }           // Host: Formel 3a; Client: nie kleiner als im Bild davor
        public bool IsGameTimeRunning { get; }    // false nur in der Solo-Pause
        public double PhaseElapsed { get; }
        public float PhaseProgress { get; }
        public float Pressure { get; }
        public IReadOnlyList<ulong> Confirmed { get; }
        public event Action<SeasonPhase> PhaseChanged;   // alle Rechner, nur für Anzeige

        // Nur Host:
        public void RequestStart();
        public void Abort();
        public void HostContinue();
        public void NextSeason();
        public void MissionFinished();
        public void SetMenuOpen(bool open);
        public void ReceiveConfirm(ulong clientId, bool value);   // von PlayerSeasonRequests

        // Müssen vor dem Spawn existieren (NGO initialisiert die Variablen vor OnNetworkSpawn),
        // also als Feld-Initialisierer. Freigeben tut NGO in NetworkBehaviour.OnDestroy.
        private readonly NetworkVariable<SeasonSnapshot> _snapshot = new();
        private readonly NetworkList<ulong> _confirmed = new();

        // OnNetworkSpawn: Current setzen, _snapshot.OnValueChanged abonnieren (alle Rechner),
        //   Startwert einmal selbst übernehmen, gemerkte Spielzeit zurücksetzen (nur hier), timeScale = 1.
        //   Nur Host: ConnectedClientsIds an Director.ClientJoined, OnConnectionEvent abonnieren,
        //   PlayerSpawnApproval.SeasonRunning setzen.
        // OnNetworkDespawn: abmelden, Current = null, timeScale = 1.
        // OnDestroy nur als override mit base.OnDestroy() (sonst gibt NGO die NetworkVariables nicht frei).

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Current = null; Time.timeScale = 1f; }
    }

    /// <summary>Startaufstellung (Datenobjekt): Platz-Kennung → Art, optional Ware.</summary>
    public class StartLayout : ScriptableObject { /* IReadOnlyList<Entry>: SlotId, ItemKind, WareKind */ }

    /// <summary>Marker in der Szene.</summary>
    public class ItemSlot : MonoBehaviour { public string SlotId { get; } }
}

// Foundation, Assets/_Project/Scripts/Net/ (kennt nichts aus Core):
namespace Schutzhuette.Net
{
    /// <summary>Beitrittsregel (GDD Regel 10, Kriterium 26). Rein.</summary>
    public static class JoinRules
    {
        public enum Result { Approved, VersionMismatch, SeasonRunning, Full }
        public static Result Decide(bool versionMatches, bool seasonRunning, int connectedPlayers, int slots);
    }

    // Bestehende Komponente am NetworkManager, erweitert:
    // public class PlayerSpawnApproval : MonoBehaviour
    // {
    //     public bool SeasonRunning { get; set; }   // setzt SeasonClock (Host); false beim Server-Stopp
    //     public int PendingJoins { get; }          // angenommen, aber noch nicht erschienen
    // }
}

// An der Spielfigur (Player-Prefab):
public class PlayerSeasonRequests : NetworkBehaviour
{
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ConfirmRpc(bool value);                   // Host: SeasonClock.ReceiveConfirm(OwnerClientId, value)

    [Rpc(SendTo.Owner)]
    public void MoveToStartRpc(Vector3 position, Quaternion rotation);
    // Besitzer: CharacterController aus, Position setzen,
    // NetworkTransform.Teleport(position, rotation, transform.localScale), Fallgeschwindigkeit 0,
    // CharacterController an.
}
```

### Implementation Guidelines

- Spielregeln mit Zeit **müssen** mit `game_time` rechnen (`SeasonClock.Current.GameTime` oder als
  Parameter). Sie **dürfen nie** `Time.time`, `Time.deltaTime`, `Time.unscaledTime` oder
  `NetworkManager.ServerTime` direkt benutzen. Darstellung (Schwanken, Flackern, Kamera, Schritte) **darf**
  weiter `Time.deltaTime` nutzen.
- Gespeicherte Zeitpunkte (Kochzeitpunkt, Beginn einer Handlung) **müssen** `double` in `game_time` sein.
- Nur `SeasonClock` **darf** `Time.timeScale` schreiben. Tests, die eine Pause auslösen, **müssen**
  `Time.timeScale` im TearDown auf 1 setzen.
- Code, der durch `Time.deltaTime` teilt (Geschwindigkeit aus Positionsdifferenz, Glätten), **muss** den Wert
  0 abfangen.
- `PhaseChanged` **darf nur** aus `OnValueChanged` des Snapshots ausgelöst werden, nie zusätzlich von Hand.
  NetworkVariables und NetworkLists **müssen** als Feld-Initialisierer angelegt werden.
- Nur der Host **darf** `SeasonSnapshot` und die Bestätigungsliste schreiben. Fortschritt, Druck und
  verstrichene Zeit **dürfen nie** als NetworkVariable oder RPC übertragen werden (GDD Kriterium 39).
- Spiel-Logik **muss** an `SeasonEvents` zuhören und **darf** dort nur beim Host etwas tun. Anzeigen
  **müssen** `SeasonClock.Current` und `PhaseChanged` nutzen. An- und Abmelden folgt der Regel aus ADR-0001
  (Register `event_bus_subscription`): NetworkBehaviours in `OnNetworkSpawn` (nur `IsServer`) und
  `OnNetworkDespawn`, reine MonoBehaviours in `OnEnable` und `OnDisable`.
- Die Spielzeit beim Client **muss** mit `max` gegen das letzte Bild laufen, ohne Ausnahme; der gemerkte Wert
  **darf nur** in `OnNetworkSpawn` zurückgesetzt werden.
- Das Menü **muss** beim Öffnen die Gameplay-Action-Maps (Bewegung, Blick, Interaktion) ausschalten und beim
  Schließen wieder an, mit und ohne Pause.
- Foundation-Code (`Net/`) **darf nie** `SeasonClock`, `SeasonPhase` oder anderes aus Core kennen. Core
  setzt Werte in Foundation von oben (`PlayerSpawnApproval.SeasonRunning`).
- Jedes System **muss** sich bei `SeasonStarted` selbst zurücksetzen. Der Saison-Code **darf nie** Klassen
  aus `Hut/`, `Guests/`, `Weather/` oder anderen Feature-Ordnern kennen (GDD Kriterium 33).
- Code, der `SeasonClock.Current` liest, **muss** `null` vertragen (Objekt noch nicht gespawnt) und dann
  keine Zeitregel ausführen.
- Gegenstände **müssen** über `ItemSpawning.Remove` (ADR-0002) entfernt werden, nie per `Despawn` direkt.
  Wer einen Gegenstand hält oder auf ihn verweist, **muss** damit umgehen, dass der Host ihn entfernt
  (Startaufstellung, Zerbrechen aus ADR-0001).
- Phasenlängen und Druckwerte **müssen** im `SeasonProfile` stehen; in den Saison-Skripten **dürfen** 600,
  720, 480 oder 1080 nicht als feste Zahlen stehen (GDD Kriterium 18).
- Der Director **muss** beim Wechsel nach Zeit `neuer Start = alter Start + Länge` rechnen, nicht die Zeit des
  Bildes, in dem er den Wechsel bemerkt (GDD Formel 3).

## Alternatives Considered

### Alternative 1: Host überträgt fertige Spielzeit und Fortschritt (B)

- **Description**: Der Host schreibt `game_time`, `phase_progress` und `season_pressure` in NetworkVariables;
  Clients lesen nur.
- **Pros**: Clients rechnen nichts; kein Rücksprung-Schutz nötig.
- **Cons**: Werte ändern sich jedes Bild, also Netzverkehr in jedem Tick für alle Clients; beim Client ruckeln
  sie im Tick-Takt; das GDD (Kriterium 39) verbietet genau das.
- **Rejection Reason**: Mehr Verkehr und schlechteres Bild für Werte, die jeder Rechner aus der Serverzeit
  selbst bekommt.

### Alternative 2: Eigene Uhr je Rechner mit Abgleich per RPC (C)

- **Description**: Jeder Rechner zählt seine eigene Zeit; der Host schickt regelmäßig einen Abgleich.
- **Pros**: Unabhängig von NGO-Interna.
- **Cons**: Baut nach, was NGO schon tut (Zeitabgleich mit RTT, `NetworkTimeSystem`); eigene Fehler bei
  Drift und Verzögerung.
- **Rejection Reason**: Kein Nutzen gegenüber `NetworkManager.ServerTime`.

### Teilentscheidungen (je kurz)

- **Pause über eigenen Wert** (`Physics.simulationMode = Script`, jedes System prüft „Spielzeit läuft“):
  mehr Code, und jedes vergessene System läuft in der Pause weiter. Verworfen zugunsten von
  `Time.timeScale = 0`.
- **Meldungen auf allen Rechnern:** Jeder Zuhörer müsste selbst prüfen, ob er Host ist; vergisst er es,
  setzt ein Client Spielzustand zurück. Verworfen.
- **Instanz-Events am Uhr-Objekt:** Zuhörer müssten das Objekt erst finden; weicht ohne Grund vom Muster aus
  ADR-0001 ab. Verworfen.
- **Einzelne NetworkVariables für Phase und Start:** laut Quelltext kurz inkonsistent beim Client. Verworfen.
- **Grund an der Phasen-Meldung statt `SeasonAborted`:** Jeder Zuhörer der Vorbereitung müsste den Grund
  beachten. Verworfen.
- **`NetworkManager.LocalTime`:** eine Vorausschätzung des Clients für seine Eingaben, liegt vor dem Host.
  Für eine gemeinsame Uhr falsch. Verworfen.
- **Ticks statt Sekunden:** hängt an der Tick-Rate; die GDD-Formeln rechnen in Sekunden. Verworfen.

## Consequences

### Positive

- Eine Zeitbasis für alle Regeln; die Pause hält alles an, auch Abkühlen und Ofen.
- Fast kein Netzverkehr: ein kleiner struct etwa sechsmal pro Saison, dazu die Bestätigungen.
- Ablauf und Formeln sind ohne Szene testbar; der Director kennt weder Netz noch Feature-Systeme.
- Dasselbe Meldemuster wie bei Aufprallen (ADR-0001); neue Systeme hängen sich an, ohne die Saison zu ändern.

### Negative

- Ein statischer Meldedienst mehr; Tests müssen Abonnenten aufräumen.
- Beim Client kann die Spielzeit nach einem Zeitsprung von NGO kurz stillstehen (höchstens so lange, wie der
  Rücksprung war).
- Bestehender Code muss umgestellt werden (Ofen, später alles mit Zeitregeln); `Time.time` in Spielregeln
  wird zur Regelverletzung.

### Neutral

- Geräusche laufen in der Pause weiter (`Time.timeScale` hält Audio nicht an). Ob sie verstummen, entscheidet
  das Geräusche-System anhand von `IsGameTimeRunning`.

## Risks

- **Rücksprung der NGO-Zeit beim Client** (ab 0,2 s Abweichung). Gegenmittel: `max` in Entscheidung 3, immer
  und ohne Ausnahme; große Sprünge nur im Log; Verification 3.
- **`Time.timeScale` bleibt nach Play auf 0** (Domain Reload aus). Gegenmittel: Zurücksetzen an drei Stellen
  (Entscheidung 4); Verification 2.
- **Ein System rechnet doch mit `Time.time`** und läuft in der Pause weiter. Gegenmittel: Regel oben, Suche
  nach `Time.time`/`Time.deltaTime` in Regel-Code bei der Code-Prüfung.
- **Vergessenes Abmelden von `SeasonEvents`** führt zu Aufrufen auf zerstörte Objekte. Gegenmittel: eine
  Abmelde-Regel für alle Meldedienste (ADR-0001, Guidelines), `ResetStatics`.
- **Der Host hebt in der Pause auf oder wirft**, weil seine eigenen RPCs sofort lokal laufen und
  `timeScale` keine Eingaben anhält. Gegenmittel: Das Menü schaltet die Gameplay-Action-Maps aus
  (Entscheidung 4); Verification 8.
- **Ein Spieler, der in der Bewertung beitritt, hält „alle haben Weiter gedrückt“ auf oder wird übersehen.**
  Gegenmittel: `ClientJoined` aus `ConnectedClientsIds` und `OnConnectionEvent` (Entscheidung 6); EditMode-Test
  im Director.
- **Hänger beim Host** (großes `unscaledDeltaTime`) lässt `game_time` springen. Gegenmittel: Der Director
  meldet mehrere Phasenwechsel einzeln (Kriterium 14); der Ofen verbrennt die vergangene Zeit, das ist
  gewollt.
- **`SeasonClock` erscheint beim neuen Client später als andere Objekte.** Gegenmittel: `Current == null`
  ist erlaubt (Guideline); Verification 6.
- **Teleport an den Startpunkt kommt erst nach einer Netzrunde an**; kurz stehen Spieler noch am alten Ort.
  Gegenmittel: GDD Kriterium 40 prüft 1 s nach dem Start.
- **Andere sehen den Spieler quer durch die Hütte gleiten**, wenn der Besitzer nur `transform.position` setzt.
  Gegenmittel: `NetworkTransform.Teleport` beim Besitzer (Entscheidung 7); Verification 7.
- **Doppelte `PhaseChanged`-Meldung beim Host**, wenn jemand sie zusätzlich von Hand auslöst. Gegenmittel:
  eine Quelle (Entscheidung 5, Guideline); Verification 4.
- **NaN in Bewegung oder Kamera in der Pause** durch Teilen durch `Time.deltaTime = 0`. Gegenmittel:
  Guideline; Verification 1. Der heutige Code fängt es an den bekannten Stellen schon ab.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| saison-ablauf.md | TR-saison-001 (vorläufig): Die Uhr gehört dem Host, alle sehen dieselbe Phase zur selben Zeit | `SeasonDirector` nur beim Host; `SeasonSnapshot` als ein Wert; alle rechnen aus `ServerTime` (Entscheidung 1–3) |
| saison-ablauf.md | TR-saison-002 (vorläufig): `game_time` steht in der Solo-Pause, Physik und Animation halten an | Formel 3a beim Host, `max` beim Client; `Time.timeScale = 0` (Entscheidung 3, 4) |
| saison-ablauf.md | TR-saison-003 (vorläufig): Meldungen „Saison beginnt“, „Phase X beginnt“, „Nacht vorbei“ mit Grund, „Spielzeit läuft“; jedes System setzt sich selbst zurück | `SeasonEvents` mit festen Reihenfolgen; `IsGameTimeRunning` lesbar (Entscheidung 5) |
| saison-ablauf.md | TR-saison-004 (vorläufig): gemeinsamer Druck `season_pressure` | `SeasonMath.Pressure`, auf jedem Rechner gerechnet, `SeasonClock.Pressure` (Entscheidung 2) |
| saison-ablauf.md | TR-saison-005 (vorläufig): Startaufstellung | `StartLayout` + `ItemSlot`, Entfernen vor `SeasonStarted`, Spieler per Owner-RPC (Entscheidung 7) |
| saison-ablauf.md | TR-saison-006 (vorläufig): Beitritt nur zwischen den Saisons | `JoinRules.Decide` (Foundation, mit dem Wert „Saison läuft“, den `SeasonClock` von oben setzt) im `ConnectionApprovalCallback`, Start gesperrt bei wartendem Beitritt (Entscheidung 7) |
| saison-ablauf.md | TR-saison-007 (vorläufig): Abbruch ohne Bewertung, Kasse fällt zurück | `SeasonAborted`; Kasse merkt sich ihren Stand bei `SeasonStarted` (Entscheidung 5, 7) |
| saison-ablauf.md | Formeln 1–7, Kriterien 1–45 (Messweg `[Season]`-Log, `Step(double serverTime)`) | `SeasonMath`, `SeasonDirector` mit übergebener Serverzeit, `ISeasonOutput.Log` |
| interaktion.md | TR-interaktion-003 (vorläufig): `dt` des Fortschritts ist vergangene `game_time` | `GameTime` nie rückwärts, `dt ≥ 0`; in der Pause 0 (Entscheidung 3) |
| gaeste.md | TR-gaeste-007 (vorläufig): Gäste-Logik eingefroren außerhalb von Nachmittag, Abend und Nacht | Gäste hören `PhaseBegan` und lesen `Phase` (Entscheidung 5) |
| gegenstandsarten.md | TR-gegenstandsarten-004 (vorläufig): Gerichte kühlen mit `game_time` ab | `DishState.CookedTime` (ADR-0002) und `GameTime` aus dieser ADR |

## Performance Implications

- **CPU**: ein `Step` pro Bild beim Host (wenige Vergleiche), ein paar Rechnungen pro Bild auf jedem Rechner.
  Vernachlässigbar.
- **Memory**: ein struct, eine kleine Liste, ein Datenobjekt pro Profil.
- **Load Time**: keine.
- **Network**: `SeasonSnapshot` (etwa 21 Byte) etwa sechsmal pro Saison; die Bestätigungsliste nur in
  Vorbereitung und Bewertung. Kein Verkehr pro Bild.

## Migration Plan

Im Plan-Schritt „Saison“, nach `/test-setup` (Test-Gerüst) und dem Katalog aus ADR-0002:

1. `SeasonMath`, `SeasonProfile` (Spiel- und Testprofil per Editor-Skript), `JoinRules` (in `Net/`) und
   EditMode-Tests nach GDD Abschnitt B, C, D.
2. `SeasonDirector` mit `ISeasonOutput`-Stub und EditMode-Tests nach Abschnitt A, E, F.
3. `SeasonEvents`, `SeasonClock`, `PlayerSeasonRequests`; das NetworkObject erzeugt der Host beim Start
   (Prefab per Editor-Skript, `GlobalObjectIdHash` neu erzeugen, in die Netzwerk-Prefab-Liste).
   `SeasonSnapshot` liegt in derselben Spiel-Assembly wie `SeasonClock` (architecture.md: eine
   Spiel-Assembly `Schutzhuette.Runtime`; sie muss auf `Unity.Netcode.Runtime` verweisen).
4. `StartLayout`, `ItemSlot`-Marker in der Hütte (per Editor-Skript), Startaufstellung über die Host-Funktion
   aus ADR-0002; `CrateSpawner` entfernen (GDD Kriterium 7).
5. `PlayerSpawnApproval` bekommt `SeasonRunning` und `PendingJoins` und ruft `JoinRules.Decide` auf;
   `SeasonClock` setzt `SeasonRunning` bei jedem Phasenwechsel.
6. Ofen auf `game_time` umstellen (`Stove.Step` bekommt `dt` aus der Spielzeit statt `Time.deltaTime`) und
   bei `SeasonStarted` zurücksetzen.
7. Test-Menü (Esc) mit Pause, Debug-Anzeige und Debug-Befehl „Einsatz beendet“ nach GDD; es schaltet beim
   Öffnen die Gameplay-Action-Maps aus.

## Validation Criteria

- Alle EditMode-Tests aus GDD Saison-Ablauf, Abschnitte A–F, laufen grün.
- Verification Required (1)–(8) bestanden; (7) im Freundestest.
- Freundestest mit Testprofil: Kriterien 36–45 (Abschnitt G) erfüllt.
- In `Assets/_Project/Scripts/` stehen in Regel-Code keine `Time.time`/`Time.deltaTime` mehr (Code-Prüfung),
  und nur `SeasonClock` schreibt `Time.timeScale`.

## Related

- ADR-0001 Netz-Autorität und Physik im Netz (Meldedienst-Muster, `OnConnectionEvent`, RPC-Form,
  owner-autoritativer Körper)
- ADR-0002 Gegenstandsarten-Katalog und Netz-Kennung (Katalog, `ItemSpawning` zum Erscheinen und
  Entfernen, Startwerte in `OnNetworkSpawn`, Abkühlen mit `game_time`)
- `design/gdd/saison-ablauf.md`, `design/gdd/interaktion.md`, `design/gdd/gaeste.md`,
  `design/gdd/gegenstandsarten.md`
- `docs/architecture/architecture.md` (QQ-05 wird hier beantwortet)
- `docs/architecture/architecture-review-2026-11-09.md` (Befunde, die diese Fassung einarbeitet)
