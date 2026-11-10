# Story 005: Startkatalog mit 13 Arten und 7 Waren

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Der echte Katalog entsteht: 13 Arten und 7 Waren als Datenobjekte, dazu je Art ein Prefab. Kiste, Holzscheit
und Truhe gibt es schon; die 10 neuen Arten (Holzklotz, Wasserkanister, Trage, Tee, Speckbrot, Kaiserschmarrn,
Axt, Erste-Hilfe-Kasten, Stirnlampe, Funkgerät) bekommen graue Graubox-Prefabs. Ein Editor-Skript legt alles
an, damit keine Asset-Datei von Hand bearbeitet wird.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-001`, `TR-gegenstandsarten-006`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: Jede Art hat ihr eigenes Prefab, und jedes Prefab steht in der Netzwerk-Prefab-Liste. Ablage der Assets: `Assets/_Project/Data/Items/`, `Data/Wares/`, `Data/Needs/`. Katalog-Assets entstehen per Editor-Skript oder im Inspector.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Prefabs, die ein Editor-Skript anlegt, brauchen danach einen neu erzeugten `GlobalObjectIdHash` (wie `PrototypeSceneBuilder.RefreshPrefabHashFromCommandLine`), sonst ist er 0 und NGO spawnt sie nicht. Batchmode läuft nur bei geschlossenem Editor. In 6.6 ist das YAML-Format geändert; neue Assets entstehen direkt im neuen Format.

**Control Manifest Rules (this layer)**:
- Required: Das Prefab verrät die Art: jede Art ein eigenes Prefab, jedes in der Netzwerk-Prefab-Liste.
- Required: Kennungen nach `^[a-z0-9_]{1,29}$`, Waren mit `ware_`.
- Required: Gegenstände haben einen server-autoritativen `NetworkTransform` mit `NetworkRigidbody` (`AutoUpdateKinematicState` an, `UseRigidBodyForMotion` aus, `SwitchTransformSpaceWhenParented` an).
- Forbidden: Nie eine `.asset`-, `.prefab`- oder `.unity`-Datei von Hand bearbeiten, nie `.meta`-Dateien anlegen oder löschen. Nie zwei Arten mit demselben Prefab.
- Guardrail: Mesh Collider brauchen „Read/Write“ am Mesh; Graubox-Prefabs nutzen einfache Collider (Box, Capsule).

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 1 [EditMode]:** GIVEN der ausgelieferte Katalog WHEN die Prüfung läuft THEN gibt es 0 Fehler, genau die 13 Arten mit den Werten aus dem Startkatalog (Masse, Trageart, Kategorie, Aufprallklang, Eigenschaften) und genau die 7 Waren mit `ware_`-Kennung. *Schnappschuss: Ändert ein anderes Dokument einen Wert, werden Tabelle und Test zusammen geändert.*
- [ ] **Prefab-Liste (ADR-0002) [EditMode]:** Jedes der 13 Prefabs steht in der Netzwerk-Prefab-Liste, und keine zwei Arten teilen sich ein Prefab; die Prüfung meldet dazu 0 Fehler.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- Die Werte stehen in der Tabelle „Startkatalog für den Vertical Slice“ im GDD (Detailed Design). Bestehende Prefabs: `Crate.prefab` ist `vorratskiste` (10 kg), `Firewood.prefab` ist `holzscheit` (3 kg, brennbar 45 s), `HeavyChest.prefab` ist `truhe` (80 kg, zu zweit).
- Die 7 Waren: `ware_speck`, `ware_brot`, `ware_mehl`, `ware_eier`, `ware_milch`, `ware_tee`, `ware_wasser`.
- Ein Editor-Skript (wie die bestehenden Builder, aufrufbar per `-executeMethod`) legt Arten, Waren, Katalog und die 10 neuen Graubox-Prefabs an, trägt sie in die Netzwerk-Prefab-Liste ein und lässt den `GlobalObjectIdHash` neu erzeugen. Es ist wiederholbar: Ein zweiter Lauf ändert nichts.
- Die neuen Prefabs sind einfache Formen in passender Größe mit `NetworkObject`, `NetworkTransform`, `NetworkRigidbody`, Rigidbody und Collider wie `Crate.prefab`. In die Szene kommen sie in dieser Story noch nicht.
- Das Feld „Bedürfnis“ der Gerichte bleibt leer, bis das Gäste-Dokument die Bedürfnisse festlegt (GDD: „vorläufig“). `Data/Needs/` bleibt bis dahin leer und bekommt eine `.gitkeep`.
- Der Schnappschuss-Test lädt den ausgelieferten Katalog im Editor (`AssetDatabase`), nicht über `CatalogService.Current`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: Komponente `Item` an den Prefabs, `CatalogService`, Masse aus dem Katalog
- Story 007: Erscheinen der Gegenstände in der Welt
- Echte Modelle statt Graubox: Plan-Schritt 17

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/_Project/Data/Items/` (13 Arten und der Katalog, vom Skript erzeugt)
- `Assets/_Project/Data/Wares/` (7 Waren, vom Skript erzeugt)
- `Assets/_Project/Prefabs/` (10 neue Prefabs, vom Skript erzeugt)
- `Assets/Tests/EditMode/Items/StartCatalogTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Config/Data
**Required evidence**:
- Config/Data: smoke check pass (`production/qa/smoke-*.md`); zusätzlich der Schnappschuss-Test `Assets/Tests/EditMode/Items/StartCatalogTests.cs` aus Kriterium 1

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-001-katalog-datentypen-und-grundpruefung.md`, `production/epics/gegenstandsarten/story-002-pruefregeln-masse-und-hacken.md`, `production/epics/gegenstandsarten/story-003-abkuehl-regeln.md`, `production/epics/gegenstandsarten/story-004-bruchregel-und-aufprall-formeln.md`
- Unlocks: `production/epics/gegenstandsarten/story-006-laufzeit-katalog-und-masse.md`, `production/epics/gegenstandsarten/story-009-build-und-host-start-sperren.md`
