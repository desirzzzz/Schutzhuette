# Schutzhütte – Master-Architektur

## Document Status

- **Version:** 1
- **Last Updated:** 2026-11-09
- **Engine:** Unity 6.6 (6000.6.6f1), URP 17.6, PhysX, Input System 1.20, Netcode for GameObjects 2.13.3
  (ClientServer, Host-Client-Modell), Unity Transport 6.6 (built-in)
- **Stufe:** `workflow: standard`. Verlangt sind Schichtenkarte und Liste der Pflicht-ADRs. Modul-Besitz,
  Datenfluss und API-Grenzen sind auf dieser Stufe nicht ausgeführt. Die Schnittstellen zwischen den Systemen
  stehen in den Abhängigkeits-Tabellen der GDDs und werden in den ADRs festgelegt.
- **Design-Grundlage:** `docs/KONZEPT.md` und `design/game-brief.md`. Ein `design/gdd/game-concept.md`
  gibt es nicht, laut CLAUDE.md stehen Konzept und Kurzfassung an diesen beiden Stellen. Dazu kommen
  `design/gdd/systems-index.md` und `production/milestones/vertical-slice.md`.
- **GDDs Covered:** `design/gdd/gegenstandsarten.md`, `design/gdd/interaktion.md`,
  `design/gdd/saison-ablauf.md`, `design/gdd/steam-lobby.md`, `design/gdd/gaeste.md` (alle Approved). Die
  6 gebauten MVP-Systeme (Netzwerk-Sitzung, Bewegung und Kamera, Tragen und Werfen, Tragen zu zweit,
  Holzofen, Geräusche) haben kein GDD. Für sie gelten die Regeln in CLAUDE.md und der Code in
  `Assets/_Project/Scripts/`.
- **ADRs Referenced:** ADR-0001 Netz-Autorität und Physik im Netz (Accepted 2026-11-09), ADR-0002
  Gegenstandsarten-Katalog und Netz-Kennung (Accepted 2026-11-09), ADR-0003 Saison-Uhr, Spielzeit und
  Meldungen (Accepted 2026-11-09).
- **TD-ARCHITECTURE:** 2026-11-09 — skipped — lean mode
- **LP-FEASIBILITY:** 2026-11-09 — skipped — lean mode

## Engine Knowledge Gap Summary

Das Sprachmodell kennt Unity sicher nur bis etwa 2022 LTS. Unity 6.0 bis 6.6 liegen dahinter (Risiko laut
`docs/engine-reference/unity/VERSION.md`: HIGH). Jede Entscheidung in einem HOCH-Bereich wird in ihrer ADR
gegen `docs/engine-reference/unity/` geprüft. Was dort als `NOT SOURCEABLE` steht, wird nicht aus dem
Gedächtnis ergänzt.

| Risiko | Bereich | Was bekannt ist, was fehlt | Betroffene Systeme |
| --- | --- | --- | --- |
| **HOCH** | Netzwerk | NGO 2.13.3: `[Rpc(SendTo.Server)]`, `RpcInvokePermission` statt `RequireOwnership`, Connection Approval, `NetworkTransform.AuthorityMode`, Parenting, `NetworkRigidbody` (aus Quelle und Doku belegt, `modules/networking.md`). **Nicht belegbar:** Signatur von `OnConnectionEvent`, ein Steam-Transport für NGO. | Netzwerk-Sitzung, Tragen, Interaktion, Saison, Gäste, Steam-Lobby |
| **HOCH** | Physik | Mesh Collider braucht Read/Write im Editor (6.6). Die Standard-Solver-Iterationen sind höher. **Nicht belegbar:** Änderungen an PhysX in 6.4 bis 6.6. | Tragen, Bruchregel, Würfe, Ragdoll |
| **HOCH** | Navigation | Das Paket AI Navigation (`com.unity.ai.navigation`) ist nicht installiert, `modules/navigation.md` beschreibt noch 6.3. | Gäste, Solo-Hüttenhund |
| **HOCH** | Scripting | Domain Reload ist aus (statische Felder zurücksetzen). `EntityId` (8 Byte) ersetzt `InstanceID`. `DEVELOPMENT_BUILD` ist veraltet, stattdessen `UNITY_ENABLE_CHECKS`. `[SerializeField] Dictionary` wird serialisiert. `Object.Destroy` ruft `OnDisable` bei allen Nachkommen auf. | alle |
| MITTEL | UI | Der Prototyp zeichnet seine Anzeigen mit IMGUI (`OnGUI`), das laut Referenz nur für den Editor gedacht ist. UI Toolkit wird empfohlen, UGUI wird weiter unterstützt. In 6.6 sind `UxmlFactory` und `UxmlTraits` entfernt. | HUD, Menüs |
| MITTEL | Animation, Rendering | Die Referenz hat noch den Stand 6.3. Dynamic Batching ist obsolet (6.6), eigene Render-Passes nur über Render Graph. | Ragdoll, Licht je Phase, Wetter |
| NIEDRIG | Input, Audio | Beide funktionieren im Prototyp. Der Sprachchat kommt als externes Paket, das noch nicht gewählt ist. | Sprachchat |

## Technical Requirements Baseline

Extracted from 5 GDDs | 29 total requirements | 0 registered, 29 provisional
(TR registry: `docs/architecture/tr-registry.yaml`, empty)

Alle IDs sind **vorläufig**. `/architecture-review` trägt sie ins Register ein und vergibt die endgültigen
IDs. Bis dahin sollte niemand sie zitieren.

| Req ID | GDD | System | Requirement | Domain |
| --- | --- | --- | --- | --- |
| TR-gegenstandsarten-001 (provisional) | gegenstandsarten.md | Gegenstandsarten | Katalog aller Arten als Datenobjekte: Masse, Trageart, Eigenschaften (brennbar, Vorratspaket, Gericht, zerbrechlich, Werkzeug, Ausrüstung, hackbar) | Core |
| TR-gegenstandsarten-002 (provisional) | gegenstandsarten.md | Gegenstandsarten | Über das Netz wird eine Art nur über ihre feste Kennung verschickt, nie als Objekt | Networking |
| TR-gegenstandsarten-003 (provisional) | gegenstandsarten.md | Gegenstandsarten | Bruchregel: Zerbrechliches zerbricht ab einer Aufprall-Geschwindigkeit, der Host entscheidet | Physics |
| TR-gegenstandsarten-004 (provisional) | gegenstandsarten.md | Gegenstandsarten | Gerichte kühlen mit `game_time` ab (`is_warm`, `warm_fraction`) | Core |
| TR-gegenstandsarten-005 (provisional) | gegenstandsarten.md | Gegenstandsarten | Würfe bis 15 m/s dürfen dünne Wände nicht durchfliegen (Open Question 3: durchgehende Kollisionsprüfung) | Physics |
| TR-gegenstandsarten-006 (provisional) | gegenstandsarten.md | Gegenstandsarten | Waren (`ware_*`) sind ein schlanker Datentyp ohne Körper, nur Bestand im Lager und Inhalt eines Pakets | Core |
| TR-interaktion-001 (provisional) | interaktion.md | Interaktion | Ein gemeinsamer Strahl liefert das Ziel für Tragen und Benutzen. Stationen melden nur Handlungen und Bedingungen | Core |
| TR-interaktion-002 (provisional) | interaktion.md | Interaktion | Jede Handlung geht als Wunsch per RPC an den Host, der Host prüft, führt aus und meldet an alle | Networking |
| TR-interaktion-003 (provisional) | interaktion.md | Interaktion | Fortschritt am Bezugsobjekt (Halten, Pausieren, Teamzuschlag zu zweit), `dt` ist vergangene `game_time` | Core |
| TR-interaktion-004 (provisional) | interaktion.md | Interaktion | Jeder Tastendruck fühlt sich auch mit Ping sofort an (Hinweis und Fortschritt am Client vorhergesagt) | Networking |
| TR-interaktion-005 (provisional) | interaktion.md | Interaktion | Ausrüstung (Stirnlampe, Funkgerät) sitzt am Körper statt in der Hand | Core |
| TR-saison-001 (provisional) | saison-ablauf.md | Saison-Ablauf | Die Uhr gehört dem Host, alle sehen dieselbe Phase zur selben Zeit (Serverzeit, Phase und Phasenstart als NetworkVariables) | Networking |
| TR-saison-002 (provisional) | saison-ablauf.md | Saison-Ablauf | Gemeinsame Spielzeit `game_time`. Sie steht in der Solo-Pause, dann halten auch Physik und Animation an | Core |
| TR-saison-003 (provisional) | saison-ablauf.md | Saison-Ablauf | Meldungen „Saison beginnt“, „Phase X beginnt“, „Nacht beginnt“, „Nacht vorbei“ (Grund `mission`, `timeout`, `abort`), „Spielzeit läuft“. Jedes System setzt sich selbst zurück | Core |
| TR-saison-004 (provisional) | saison-ablauf.md | Saison-Ablauf | Gemeinsamer Druckwert `season_pressure` für Gäste, Wetter und Ereignisse | Core |
| TR-saison-005 (provisional) | saison-ablauf.md | Saison-Ablauf | Startaufstellung: Gegenstände entfernen, Arten und Waren an festen Plätzen spawnen | Networking |
| TR-saison-006 (provisional) | saison-ablauf.md | Saison-Ablauf | Beitritt nur zwischen den Saisons (gemeinsame Freigabe) | Networking |
| TR-saison-007 (provisional) | saison-ablauf.md | Saison-Ablauf | Abbruch durch den Host: keine Bewertung, die Hüttenkasse fällt auf den Stand beim Saisonstart zurück | Core |
| TR-steam-lobby-001 (provisional) | steam-lobby.md | Steam-Lobby | Transport wählbar: Steam oder IP (Unity Transport), der IP-Weg bleibt als Ausweg | Networking |
| TR-steam-lobby-002 (provisional) | steam-lobby.md | Steam-Lobby | Steam-Anbindung mit App-ID aus einer Einstellung (480 bis zur Steam-Seite, dann die eigene) | Networking |
| TR-steam-lobby-003 (provisional) | steam-lobby.md | Steam-Lobby | Eine gemeinsame Freigabe für beide Wege: Version, höchstens 4 Spieler, Sperre während der Saison | Networking |
| TR-steam-lobby-004 (provisional) | steam-lobby.md | Steam-Lobby | Zeitgrenze 20 s für den Beitritt, Steam-Namen über den Figuren | Networking |
| TR-gaeste-001 (provisional) | gaeste.md | Gäste | Gäste sind vom Host gesteuert. Clients bekommen Position, Zustand, Bedürfnis, Geduld und Stimmung | Networking |
| TR-gaeste-002 (provisional) | gaeste.md | Gäste | Wegfindung beim Host (NavMesh, Paket AI Navigation nur nach OK) | Navigation |
| TR-gaeste-003 (provisional) | gaeste.md | Gäste | Ankunft aus Phase, `season_pressure` und Spielerzahl `n` beim Saisonstart | Core |
| TR-gaeste-004 (provisional) | gaeste.md | Gäste | Reaktion auf Kälte (Ofenwärme), Lärm (Aufprall in der Nähe) und Chaos (Treffer). Braucht Aufprall-Meldungen aus der Physik | Physics |
| TR-gaeste-005 (provisional) | gaeste.md | Gäste | Schnittstelle „Bedürfnis erfüllt“ für Stationen und Gerichte; die Gäste kennen keine Station | Core |
| TR-gaeste-006 (provisional) | gaeste.md | Gäste | Gästetypen und Bedürfnisse als Datenobjekte, mit Datenprüfung (`guest_data_valid`) | Core |
| TR-gaeste-007 (provisional) | gaeste.md | Gäste | Gäste-Logik eingefroren außerhalb von Nachmittag, Abend und Nacht | Core |

## System Layer Map

Die Schichten folgen der Dependency Map in `design/gdd/systems-index.md`. Eine Schicht nutzt nur Schichten
darunter. Nach oben meldet sie über C#-Events, zum Beispiel ruft der Saison-Ablauf „Saison beginnt“ aus,
und jedes System setzt sich selbst zurück. So kennt die Uhr keine Gäste und der Katalog kein Tragen.

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ PRESENTATION   HUD, Menüs, Geräusche                                             │
│                zeigen nur an; lesen NetworkVariables und Events, ändern nichts   │
├──────────────────────────────────────────────────────────────────────────────────┤
│ FEATURE        Tragen zu zweit*, Holzofen und Wärme*, Holz hacken, Lager und      │
│                Vorräte, Kochen und Gerichte, Materialseilbahn, Gäste und          │
│                Bedürfnisse (mit Wegfindung), Versorgung, Wetter und Eskalation,   │
│                Ereignisse, Nachteinsatz, Solo-Hüttenhund, Figuren-Animation und   │
│                Ragdoll, Bewertung, Hüttenkasse und Upgrades, Sprachchat           │
├──────────────────────────────────────────────────────────────────────────────────┤
│ CORE           Bewegung und Kamera*, Tragen und Werfen*, Interaktion,             │
│                Saison-Ablauf und Spielphasen, Steam-Lobby und Einladen            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ FOUNDATION     Netzwerk-Sitzung* (Transport IP/Steam, Freigabe, Spawn, Version,   │
│                Updater), Gegenstandsarten (Katalog und Kennung)                   │
├──────────────────────────────────────────────────────────────────────────────────┤
│ PLATFORM       Unity 6.6 (URP, PhysX, Input System, Audio), NGO 2.13.3 und Unity  │
│                Transport, später Facepunch.Steamworks und ein Sprachchat-Paket    │
└──────────────────────────────────────────────────────────────────────────────────┘
* gebaut (Prototyp)
```

Die Platform-Schicht ist kein eigener Code. Ein Adapter, der eine Plattform einbindet (zum Beispiel der
Steam-Transport), gehört zur Netzwerk-Sitzung in Foundation. Den Ablauf von Lobby und Einladung regelt die
Steam-Lobby in Core.

### Netz-Rolle je Schicht

| Schicht | Wer ändert den Zustand | Wie Clients ihn sehen |
| --- | --- | --- |
| Foundation | Host: Freigabe, Spawn, Kennungen | Connection Approval, NetworkObjects, Kennung als Netzwerkwert |
| Core | Host. Ausnahme: der eigene Spielerkörper ist owner-autoritativ | NetworkVariables (Phase, Uhr), NetworkTransform, Wünsche per `[Rpc(SendTo.Server)]` |
| Feature | nur der Host | NetworkVariables und NetworkTransform/NetworkRigidbody. Client-Vorhersage ist nur optisch |
| Presentation | niemand | liest nur |

### Code-Aufteilung (entschieden 2026-11-09)

Heute liegt aller Code in `Assembly-CSharp`. Darauf kann ein Test-Projekt nicht verweisen. Deshalb gibt es
künftig vier Assemblies:

| Assembly | Ordner | Inhalt |
| --- | --- | --- |
| `Schutzhuette.Runtime` | `Assets/_Project/Scripts/` | der ganze Spiel-Code, Schichten über Ordner und Namespaces |
| `Schutzhuette.Editor` | `Assets/_Project/Scripts/Editor/` | Editor-Skripte (Builder, Build-Setup), nur Plattform Editor |
| `Schutzhuette.Tests.EditMode` | `Assets/Tests/EditMode/` | EditMode-Tests, verweist auf Runtime |
| `Schutzhuette.Tests.PlayMode` | `Assets/Tests/PlayMode/` | PlayMode-Tests, verweist auf Runtime |

Eine Assembly je Schicht wurde verworfen: Der Compiler würde Aufrufe nach oben zwar verbieten, aber jeder
Kreis (zum Beispiel Tragen ↔ Interaktion) und jede Paket-Referenz müsste einzeln verdrahtet werden. Die
Schichtenregel prüft deshalb das Code-Review. `Scripts/Editor/` **muss** eine eigene Editor-Assembly
bekommen, sonst landen die Editor-Skripte in `Schutzhuette.Runtime`, und der Build bricht ab. Die genauen
Paket-Referenzen (Netcode, Input System, Multiplayer Tools) legt `/test-setup` fest. Dieser Umbau wird im
Editor geprüft (Solo und mit zwei Spielern in Multiplayer Play Mode).

## Module Ownership

Bei `workflow: standard` nicht ausgeführt. Wem welche Daten gehören, steht in den Abhängigkeits-Tabellen
der GDDs und wird in den ADRs 0001 bis 0006 festgelegt.

## Data Flow

Bei `workflow: standard` nicht ausgeführt. Der wichtigste Fluss, die Meldungen der Saison an alle anderen
Systeme, wird in ADR-0003 festgelegt. Speichern gibt es im Vertical Slice nicht: Die Hüttenkasse gilt nur
innerhalb einer Spielsitzung, „Spielstand speichern“ kommt in der Alpha.

## API Boundaries

Bei `workflow: standard` nicht ausgeführt. Die Schnittstellen „Bedürfnis erfüllt“ (Gäste), „Einsatz
beendet“ (Saison) und der gemeinsame Strahl (Interaktion) sind in den GDDs beschrieben. Ihre Code-Form legen
die ADRs fest.

## ADR Audit

Drei ADRs, alle Accepted (2026-11-09): ADR-0001 und ADR-0002 (Foundation), ADR-0003 (Core).
Engine-Angaben einheitlich (Unity 6000.6.6f1, NGO 2.13.3), Abhängigkeiten kreisfrei (0002 → 0001,
0003 → 0001 und 0002). Das Architektur-Review vom 2026-11-09
(`docs/architecture/architecture-review-2026-11-09.md`, CONCERNS) fand keine Konflikte zwischen den ADRs;
seine Befunde sind eingearbeitet (Commit a189fed).

**Traceability (Stand des Reviews, vor der Einarbeitung):** 33 Anforderungen (29 aus dieser Fassung, 4 vom
Review neu vergeben), davon 16 gedeckt, 8 teilweise, 9 Lücken. Die Lücken liegen in Core und Feature und
gehören zu ADR-0004 bis ADR-0006 (unten). Die Zahlen sind seit der Einarbeitung und der Annahme nicht neu
gezählt; das macht ein neues `/architecture-review` in einer frischen Sitzung.

## Required ADRs

**Vor dem ersten Code (Foundation und Core, Plan-Schritt 1):** erledigt, alle drei geschrieben und
Accepted (2026-11-09).

1. **ADR-0001 Netz-Autorität und Physik im Netz.** Hält den Ist-Stand des Prototyps fest: Host entscheidet;
   der eigene Spielerkörper ist owner-autoritativ; Tragen per Parenting mit Client-Vorhersage; ruhende
   Dinge schlafen oder sind kinematisch; der Host simuliert Fallendes; Wände beim Tragen über
   `CarryCollision`. Neu: die Bruchregel beim Host, die durchgehende Kollisionsprüfung für schnelle Würfe
   und die Aufprall-Meldungen für Lärm und Chaos. Klärt `OnConnectionEvent` (QQ-06).
   → deckt: TR-gegenstandsarten-003, TR-gegenstandsarten-005, TR-gaeste-004 und die gebauten MVP-Systeme
2. **ADR-0002 Gegenstandsarten-Katalog und Netz-Kennung.** Arten als ScriptableObjects, eine feste
   Kennung als Netzwerkwert, Waren als eigener schlanker Typ, Datenprüfung beim Laden, Werte wie die
   Brenndauer wandern aus den Komponenten in den Katalog.
   → deckt: TR-gegenstandsarten-001, -002, -004, -006, TR-gaeste-006
3. **ADR-0003 Saison-Uhr, Spielzeit und Meldungen zwischen Systemen.** Serverzeit und NetworkVariables für
   Phase und Phasenstart, `game_time` mit Solo-Pause (QQ-05), Meldungen als C#-Events beim Host plus
   NetworkVariable-Änderungen bei den Clients, Startaufstellung, Zurücksetzen, Abbruch.
   → deckt: TR-saison-001 bis -007, TR-interaktion-003 (dt), TR-gaeste-007

**Vor dem jeweiligen Plan-Schritt (`production/milestones/vertical-slice.md`):**

4. **ADR-0004 Interaktion übers Netz** (vor Schritt 4): RPC-Form, wem der Fortschritt gehört, Vorhersage
   von Hinweis und Fortschritt, Log-Format `[Interact]`.
   → deckt: TR-interaktion-001, -002, -004, -005
5. **ADR-0005 Gäste-Wegfindung im Netz** (vor Schritt 5): NavMesh beim Host, Paket AI Navigation erst nach
   dem OK des Entwicklers, Übertragung an die Clients.
   → deckt: TR-gaeste-001, -002, -003, -005
6. **ADR-0006 Steam-Transport für NGO mit IP-Ausweg** (vor Schritt 6): Welche Bibliothek, ob sie mit
   NGO 2.13 und Unity 6.6 läuft (QQ-03), eine Freigabe für beide Wege.
   → deckt: TR-steam-lobby-001 bis -004, TR-saison-006

**Später (die GDDs fehlen noch):**

7. **ADR-0007 UI-Technik und Szenenablauf** (vor Schritt 15): Ersatz für IMGUI (QQ-04), Hauptmenü →
   Hütte.
8. **ADR-0008 Sprachchat-Paket** (vor Schritt 16): Vivox oder Dissonance, Funkgerät-Effekt.
9. **ADR-0009 Ragdoll im Netz** (vor Schritt 17): nur Zustand und Wurzelposition übertragen oder mehr.

**Kann bis zum Bau warten:** Licht- und Stimmungswechsel je Phase, Details der Geräusche, Log-Formate
einzelner Systeme.

## Architecture Principles

1. **Der Host entscheidet.** Spielzustand (Gegenstände, Ofen, Gäste, Wetter, Uhr) ändert nur der Host.
   Clients schicken Wünsche per `[Rpc(SendTo.Server)]`, der Host prüft und führt aus. Einzige Ausnahme ist
   der eigene Spielerkörper: Er ist owner-autoritativ, damit die eigene Bewegung nicht um den Ping
   hinterherhinkt (Entscheidung 2026-11-08). Client-Vorhersage ist immer nur optisch.
2. **Physik im Netz sparsam.** Was ruht, schläft oder ist kinematisch. Was getragen wird, folgt dem Träger
   (Parenting). Nur Fallendes und Rutschendes simuliert der Host, die Clients bekommen es über
   `NetworkTransform` und `NetworkRigidbody`.
3. **Werte in Datenobjekten, übers Netz nur Kennungen.** Gerichte, Gäste, Gegenstände, Ereignisse und
   Upgrades sind ScriptableObjects. Über das Netz gehen ihre Kennungen, nie die Objekte.
4. **Kleine Komponenten, Meldungen nach oben.** Nach unten wird aufgerufen, nach oben nur über Events
   gemeldet. Singletons gibt es nur für echte globale Dienste. Weil Domain Reload aus ist, wird jedes
   statische Feld in einer Methode mit
   `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` zurückgesetzt.
5. **Solo ist Host ohne Clients, und Regeln sind testbar.** Es gibt keinen eigenen Solo-Pfad.
   Rechenregeln stehen als reine statische Funktionen im Code (wie `Stove.Step` und
   `HeavyCarryable.ComputeCarryPose`), damit EditMode-Tests sie ohne Szene prüfen können.

## Open Questions

| ID | Frage | Priorität | Wird geklärt in |
| --- | --- | --- | --- |
| QQ-01 | Budget für Draw Calls und Speicher. Gesetzt sind nur 60 fps und 16,6 ms (`.claude/docs/technical-preferences.md`). | Mittel | Messung mit dem Profiler in Plan-Schritt 5, dann in `project.yaml` |
| QQ-02 | Netz-Bandbreite bei 4 Spielern, vollen Gästen und fallenden Dingen | Mittel | Network Profiler in Plan-Schritt 5 und 10 |
| QQ-03 | Welches Steam-Transport-Paket passt zu NGO 2.13 und Unity 6.6? | Hoch | ADR-0006 |
| QQ-04 | UI-Technik als Ersatz für IMGUI (UI Toolkit oder UGUI) | Mittel | ADR-0007 |
| QQ-05 | ~~Hält die Solo-Pause Physik und Animation über `Time.timeScale` oder über einen eigenen Wert an?~~ **Beantwortet 2026-11-09:** über `Time.timeScale = 0`, nur ohne Clients; nur `SeasonClock` schreibt den Wert und setzt ihn an drei Stellen auf 1 zurück; NGO rechnet unskaliert weiter; das Menü schaltet die Gameplay-Eingaben aus | Hoch | ADR-0003 (Accepted) |
| QQ-06 | ~~`OnConnectionEvent` statt `OnClientConnectedCallback` (NGO kündigt die Ablösung an, die Signatur ist nicht belegt)~~ **Beantwortet 2026-11-09:** Neuer Code nutzt nur `OnConnectionEvent`, abonniert vor `StartHost()`/`StartClient()`, Host-Logik wertet nur `ClientConnected`/`ClientDisconnected` aus; `ConnectionHud` und `PlayerSpawnApproval` wechseln im Plan-Schritt „Steam-Lobby“. Signatur aus dem NGO-2.13.3-Quelltext belegt | Niedrig | ADR-0001 (Accepted) |
