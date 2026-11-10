# Story 007: Erscheinen und Entfernen über ItemSpawning, Ware am Vorratspaket

> **Epic**: Gegenstandsarten
> **Status**: Complete (2026-11-10: Tests grün, Entwickler hat die Exe getestet und den Commit freigegeben). Offen für den Freundestest: die Client-Seite von Kriterium 32 und Verification 1
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: 2026-11-10

## Context

Gegenstände erscheinen und verschwinden ab jetzt nur noch über zwei Host-Funktionen. Ein Vorratspaket ohne
feste Ware bekommt seine Ware beim Erscheinen mit, und der Client sieht sie sofort. Das ist die erste Stelle,
an der eine Kennung als Text übers Netz geht.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: `ItemSpawning.Spawn` prüft die Art, ruft `Instantiate`, setzt die Startwerte und ruft `Spawn()`; `ItemSpawning.Remove` stellt auf „Discrete“ und ruft `Despawn(true)`. Werte je Gegenstand sind NetworkVariables, die nur der Host schreibt; der Startwert kommt vor `Spawn()` in ein normales Feld und in `OnNetworkSpawn` in die NetworkVariable.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: `NetworkVariable<FixedString32Bytes>` braucht keinen eigenen Serialisierer (NGO-2.13.3-Quelltext). Zu prüfen (ADR-0002, Verification 1): Der Client sieht die Ware-Kennung schon in seinem `OnNetworkSpawn`, ohne die Warnung „NetworkVariable is written to, but doesn't know its NetworkBehaviour yet“ beim Host. Ob sich Kriterium 32 mit Host und Client in einem PlayMode-Test prüfen lässt (Test-Hilfen von NGO 2.13.3), ist nicht belegt; sonst gilt die Log-Prüfung im Freundestest.

**Control Manifest Rules (this layer)**:
- Required: Erscheinen nur über `ItemSpawning.Spawn` (Host); eine ungültige Art erscheint nicht. Wer ein Vorratspaket ohne feste Ware erzeugt, nennt die Ware.
- Required: Verschwinden nur über `ItemSpawning.Remove` (Host): erst `Discrete`, dann `NetworkObject.Despawn(true)`.
- Required: Startwert vor `Spawn()` in ein normales Feld, in `OnNetworkSpawn` (Host) in die NetworkVariable; Clients verarbeiten spätere Änderungen über `OnValueChanged`.
- Forbidden: Nie eine NetworkVariable vor `Spawn()` direkt schreiben, nie den Startwert erst danach. Nie `Despawn` oder `Destroy` für Gegenstände direkt. Nie ScriptableObjects, Asset-Pfade oder Listen-Positionen über das Netz.
- Guardrail: Ware-Kennung (etwa 10–15 Byte) nur einmal in der Spawn-Nachricht; keine Daten für die Art selbst.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 32 [PlayMode]:** GIVEN der Host lässt eine Vorratskiste mit `ware_speck` erscheinen WHEN ein Client sie sieht THEN lesen Host und Client am Gegenstand dieselbe Ware `ware_speck`. *(Stand 2026-11-10: Host-Seite durch `HostSpawnTests` belegt. Client-Seite offen, Prüfung im Freundestest über die Log-Zeilen `[Items] … Ware … (Host)` und `(Client)`.)*
- [x] **Edge Case Paket ohne Ware [PlayMode]:** Wenn ein Paket ohne feste Ware ohne Angabe erscheinen soll: Fehler beim Host, das Paket erscheint nicht.
- [x] **Edge Case ungültige Art [PlayMode]:** Eine ungültige Art (Masse ≤ 0, kein Prefab, unbekannte Ware) erscheint nicht in der Welt; im Log steht ein Fehler.
- [ ] **Startwert (ADR-0002, Verification 1) [Log]:** Beim Erscheinen einer Vorratskiste mit Ware gibt es beim Host keine Warnung zur NetworkVariable, und der Client liest die Ware schon in seinem `OnNetworkSpawn`. *(Stand 2026-11-10: Beim Host keine Warnung, durch `HostSpawnTests` belegt. Dass der Client die Ware schon in seinem `OnNetworkSpawn` liest, ist offen bis zum Freundestest.)*

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- `ItemSpawning` (statisch, nur Host): `Spawn(ItemKind kind, Vector3 position, Quaternion rotation, WareKind ware = null, double? cookedTime = null)` liefert `null` bei ungültiger Art; `Remove(NetworkObject item)`.
- `SupplyPackState : NetworkBehaviour` nur an Vorratspaketen ohne feste Ware: `NetworkVariable<FixedString32Bytes> WareId` als Feld-Initialisierer, `SetInitialWare` vor `Spawn()`.
- `CrateSpawner` und alle anderen Stellen, die heute Gegenstände erzeugen oder entfernen, wechseln auf `ItemSpawning`. Der Ofen wechselt in Story 008.
- Der Client schlägt die Kennung über `CatalogService.Current.TryGetWare` nach und verträgt einen Fehlschlag (Fehler im Log, keine Exception).
- `DishState` (Kochzeitpunkt) kommt erst mit der Saison-Uhr; der Parameter `cookedTime` darf bis dahin ungenutzt bleiben.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 008: Ofen und Tragen lesen den Katalog
- Story 010: Entfernen beim Zerbrechen
- Die Startaufstellung als Datenobjekt (`StartLayout`): Epic Saison-Ablauf (ADR-0003)

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/ItemSpawning.cs`
- `Assets/_Project/Scripts/Items/SupplyPackState.cs`
- `Assets/_Project/Scripts/Carrying/CrateSpawner.cs`
- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/Tests/PlayMode/Items/ItemSpawningTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/Tests/PlayMode/Items/ItemSpawningTests.cs` OR playtest doc (Log-Zeilen von Host und Client aus dem Freundestest für Kriterium 32)

**Status**: [x] Erstellt (2026-11-10). EditMode 79 von 79 (neu: `ItemSpawningTests` 11, ein Test in `CatalogValidatorTests`), PlayMode 5 von 5 (neu: `Assets/Tests/PlayMode/Items/HostSpawnTests.cs` startet in der echten Szene einen Host). Der Host-Test belegt Kriterium 32 für die Host-Seite und Verification 1 für den Host (keine Warnung von NGO). Gegenprobe: Ohne die Zeile, die die Ware in den Netzwert überträgt, schlägt der Host-Test fehl. Offen für den Freundestest: Der Client liest dieselbe Ware (Log-Zeilen `[Items] … Ware … (Client)`). Abweichung von der ADR-Signatur: `ItemSpawning.Spawn` hat noch keinen Parameter für den Kochzeitpunkt; er kommt mit `DishState`.

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-006-laufzeit-katalog-und-masse.md`
- Unlocks: `production/epics/gegenstandsarten/story-008-tragen-und-ofen-lesen-den-katalog.md`, `production/epics/gegenstandsarten/story-010-aufprall-messpunkt-und-zerbrechen.md`
