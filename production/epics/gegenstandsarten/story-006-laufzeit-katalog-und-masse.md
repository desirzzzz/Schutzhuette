# Story 006: Laufzeit-Katalog, Komponente Item und Masse aus dem Katalog

> **Epic**: Gegenstandsarten
> **Status**: Complete (2026-11-10: Tests grün, Entwickler hat die Exe getestet und den Commit freigegeben). Offen: der Handtest zu Kriterium 8 im Spiel
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: 2026-11-10

## Context

Der Katalog kommt ins laufende Spiel: Ein Dienst am `NetworkManager`-Objekt hält ihn und schlägt Kennungen
nach. Jedes Gegenstands-Prefab bekommt die Komponente `Item` und weiß damit, welche Art es ist. Die Masse des
Rigidbody kommt ab jetzt aus dem Katalog, auf jedem Rechner.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-001`, `TR-gegenstandsarten-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: `CatalogService` sitzt am Objekt des `NetworkManager`, stellt sich als `CatalogService.Current` bereit und baut die Nachschlagetabellen bei jedem Start neu. Jedes Prefab trägt `Item` mit Verweis auf seine `ItemKind`; in `Awake` übernimmt der Rigidbody die Masse aus dem Katalog.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Domain Reload ist aus: `CatalogService.Current` wird bei `SubsystemRegistration` zurückgesetzt, und die Tabellen entstehen bei jedem Start neu. Unity speichert den Kreisverweis Art → Prefab → Art ohne Probleme (im Architektur-Review bestätigt). `FixedString32Bytes` kommt aus `com.unity.collections`.

**Control Manifest Rules (this layer)**:
- Required: Zugriff zur Laufzeit über `CatalogService.Current`; Nachschlagetabellen bei jedem Start neu bauen (`TryGetKind`, `TryGetWare`, `TryGetNeed`).
- Required: In `Awake` übernimmt der Rigidbody auf jedem Rechner die Masse aus dem Katalog.
- Required: Unbekannte Kennung beim Client: Fehler im Log, der Gegenstand wird nicht angezeigt.
- Required: Jedes statische Feld wird bei `SubsystemRegistration` zurückgesetzt.
- Forbidden: Nie Laufzeit-Tabellen im `ItemCatalog`. Nie `CatalogService.Current` in Prüfregeln, Editor-Code oder Tests. Nie eine Art an Kennung oder Namen erkennen. Nie still eine andere Art anzeigen.
- Guardrail: Tabellen einmal beim Start, Nachschlagen über ein `Dictionary`, keine Kosten pro Frame.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [x] **Kriterium 7 [EditMode + PlayMode]:** GIVEN das holzscheit-Prefab hat Rigidbody-Masse 1 WHEN die Prüfung läuft THEN gibt es 1 Hinweis und keinen Fehler; WHEN der Host es erscheinen lässt THEN ist `Rigidbody.mass` = 3,0.
- [ ] **Kriterium 8 [Manuell]:** Solo: GIVEN ein Objekt in der Szene ohne Art WHEN Play startet THEN steht genau 1 Fehler mit dem Objektnamen in der Konsole, und ein Aufheben-Versuch lässt es liegen. *(Stand 2026-11-10: Die Fehlermeldung mit Objektnamen ist durch PlayMode-Tests belegt, die Sperre steht in `PlayerCarry` und `PlayerHeavyCarry`. Den Aufheben-Versuch im Spiel hat noch niemand von Hand geprüft.)*
- [x] **Kriterium 27 [EditMode]:** GIVEN der Laufzeit-Katalog WHEN nach `gibt_es_nicht` gesucht wird THEN gibt es keinen Treffer, genau 1 Fehler, keine Exception, und der Erscheinen-Pfad beim Client zeigt nichts an.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- `Item : MonoBehaviour` mit `Kind` (nur lesend). Kein Netzwert nötig: NGO erzeugt beim Client dasselbe Prefab.
- `CatalogService : MonoBehaviour` am `NetworkManager`-Objekt, mit `Catalog`, `TryGetKind`, `TryGetWare`, `TryGetNeed` (Parameter `in FixedString32Bytes`). Die Tabellen sind normale Felder des Dienstes, nicht des Assets.
- Der Hinweis bei abweichender Prefab-Masse kommt in die Katalogprüfung (Edge Case: Der Katalog gilt, die Masse wird beim Erscheinen überschrieben).
- Das Editor-Skript aus Story 005 hängt `Item` an alle 13 Prefabs und `CatalogService` an das `NetworkManager`-Objekt der Szene. Die Szene wird nicht von Hand bearbeitet.
- Für Kriterium 8: Das Aufheben verlangt eine Art; ein Objekt ohne `Item` oder ohne `Kind` wird nicht als Gegenstand behandelt. Wo die Prüfung beim Start sitzt, legt der Bau fest.
- PlayMode-Physik in festen Schritten (`Physics.simulationMode = SimulationMode.Script`, `Physics.Simulate(0.02f)`), wie im GDD beschrieben.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 007: `ItemSpawning` und die Ware am Vorratspaket
- Story 008: Tragen und Ofen lesen die Werte aus dem Katalog
- Story 009: Sperre beim Host-Start und im Build

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/Item.cs`
- `Assets/_Project/Scripts/Items/CatalogService.cs`
- `Assets/_Project/Scripts/Items/CatalogValidator.cs`
- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/_Project/Scripts/Carrying/PlayerCarry.cs`
- `Assets/Tests/EditMode/Items/CatalogServiceTests.cs`
- `Assets/Tests/PlayMode/Items/ItemMassTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/Tests/PlayMode/Items/ItemMassTests.cs` und `Assets/Tests/EditMode/Items/CatalogServiceTests.cs`; Kriterium 8 nach Testanleitung im Editor (Entwickler bestätigt)

**Status**: [x] Erstellt (2026-11-10). EditMode 67 von 67 (neu: `CatalogLookupTests` 8, vier Tests in `CatalogValidatorTests` zu Etikett und Masse), PlayMode 4 von 4 (`Assets/Tests/PlayMode/Items/ItemTests.cs`, die ersten PlayMode-Tests des Projekts). Gegenprobe: Ohne die Zeile, die die Masse übernimmt, schlägt `ItemWithKind_RigidbodyTakesMassFromCatalog` fehl. Kriterium 8 im Spiel (Aufheben-Versuch) prüft der Entwickler nach Testanleitung.

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-005-startkatalog.md`
- Unlocks: `production/epics/gegenstandsarten/story-007-erscheinen-und-entfernen.md`, `production/epics/gegenstandsarten/story-008-tragen-und-ofen-lesen-den-katalog.md`
