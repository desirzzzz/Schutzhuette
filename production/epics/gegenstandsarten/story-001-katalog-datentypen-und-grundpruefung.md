# Story 001: Katalog-Datentypen und Grundprüfung

> **Epic**: Gegenstandsarten
> **Status**: Complete (2026-11-10: 23 Tests grün, Entwickler hat die Exe getestet, „wie vorher“)
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: 2026-11-10

## Context

Die Datentypen des Katalogs entstehen: eine Art je Datenobjekt, Waren und Bedürfnisse als schlanke Typen, ein
Katalog, der alle listet, und eine reine Prüffunktion, die Fehler mit Namen meldet. Noch nichts davon ist im
Spiel sichtbar; die folgenden Stories bauen darauf auf.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-001`, `TR-gegenstandsarten-006`, `TR-gegenstandsarten-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: Jede Art ist ein ScriptableObject `ItemKind`, jede Ware ein `WareKind`, jedes Bedürfnis ein `NeedKind`; ein `ItemCatalog` listet alle. Optionale Eigenschaften sind feste `[Serializable]`-Blöcke mit Schalter `Enabled`. Die Prüfung ist die reine Funktion `CatalogValidator.Validate`.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: `FixedString32Bytes` fasst 29 Byte Text (ADR-0002, Verification 4): Eine Kennung mit 29 Zeichen ist gültig, eine mit 30 meldet die Prüfung als Fehler. In 6.6 serialisiert Unity `Dictionary`-Felder; der Katalog enthält trotzdem nur Listen.

**Control Manifest Rules (this layer)**:
- Required: Ein Datenobjekt je Eintrag (`ItemKind`, `WareKind`, `NeedKind`), ein `ItemCatalog` mit den Katalog-Einstellungen. Optionale Eigenschaften sind feste `[Serializable]`-Blöcke mit `Enabled`; `ToolAction` und `EquipmentSlot` sind Enums in Foundation.
- Required: Kennungen folgen `^[a-z0-9_]{1,29}$`; Waren beginnen mit `ware_`, Bedürfnisse mit `beduerfnis_`, Arten mit keinem der beiden.
- Required: Prüfung als reine Funktion `CatalogValidator.Validate(ItemCatalog, NetworkPrefabsList)` mit `CatalogReport`.
- Forbidden: Nie Datenobjekte zur Laufzeit schreiben. Nie Nachschlagetabellen als serialisiertes `Dictionary` im Asset. Nie `CatalogService.Current` in Prüfregeln oder Tests. Nie ein großes ScriptableObject mit einer Liste aller Einträge.
- Guardrail: keine Kosten pro Frame; die Prüfung läuft nur im Editor, im Build und beim Host-Start.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [x] **Kriterium 2 [EditMode]:** GIVEN je eine Test-Art mit einem Fehler (Masse 0, Masse −1, leere Kennung, leerer Name, kein Prefab, Paket mit 0 Portionen, Paket mit unbekannter fester Ware) WHEN die Prüfung läuft THEN gibt es pro Test-Art genau 1 Fehler, der ihre Kennung oder ihren Asset-Namen nennt.
- [x] **Kriterium 5 [EditMode]:** GIVEN zwei Einträge (Arten oder Waren) mit derselben Kennung WHEN die Prüfung läuft THEN gibt es genau 1 Fehler, der beide Assets nennt, AND das Prüfergebnis meldet „Start gesperrt“ (das Ergebnis, das der Host-Start liest, ist eine prüfbare Funktion).
- [x] **Edge Case Waren [EditMode]:** Eine Ware mit leerer Kennung oder leerem Anzeigenamen ergibt einen Fehler; Waren haben kein Prefab-Feld.
- [x] **Kennung (ADR-0002, Verification 4) [EditMode]:** Eine Kennung mit 29 Zeichen ist gültig, eine mit 30 Zeichen, mit Großbuchstaben oder mit falschem Präfix (`ware_` an einer Art, fehlendes `ware_` an einer Ware) ergibt einen Fehler.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- Ort: `Assets/_Project/Scripts/Items/`, Namespace `Schutzhuette.Items`. Felder `[SerializeField] private`, nach außen nur lesend.
- `ItemKind`: `Id`, `DisplayName`, `MassKg`, `CarryMode` (Solo, Pair), `Category` (Fuel, RawMaterial, SupplyPack, Dish, Tool, Bulky), `ImpactSound` (Wood, Soft), `Prefab` (`NetworkObject`) und die Blöcke `Fuel`, `Chop`, `SupplyPack`, `Dish`, `Fragile`, `Tool`, `Equipment`.
- `WareKind` und `NeedKind`: nur `Id` und `DisplayName`.
- `ItemCatalog`: `Kinds`, `Wares`, `Needs` als `IReadOnlyList` und `Settings` (`max_solo_mass`, Prüfwerte Haltehöhe und Wurftempo, `floor_factor`, `max_path`, `walk_speed_min`, Toleranz beim Hacken). Nur Listen und Einstellungen, keine Laufzeit-Tabellen.
- `CatalogReport`: `Errors`, `Hints`, `StartBlocked => Errors.Count > 0`. Fehler nennen Kennung oder Asset-Namen.
- `CatalogValidator.Validate(ItemCatalog, NetworkPrefabsList)`: Die Prefab-Liste ist Parameter, damit die Regel „jedes Prefab steht in der Liste“ ohne Szene prüfbar bleibt. Zwei Arten mit demselben Prefab sind ein Prüffehler.
- In Tests entstehen Arten über `ScriptableObject.CreateInstance`. Dafür braucht es einen Weg, Testwerte zu setzen, ohne die Felder öffentlich schreibbar zu machen (zum Beispiel eine `internal`-Methode mit `InternalsVisibleTo` für die Test-Assembly). Das legt der Bau fest.
- Katalog-Assets entstehen per Editor-Skript oder im Inspector, nie durch Bearbeiten der `.asset`-Datei.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Höchstmasse für einen Träger, Hinweise bei Trageart zu zweit, Hack-Kreise, Massenbilanz
- Story 003: Hinweis zur Abkühlzeit; Story 004: Hinweis zur Bruchgeschwindigkeit
- Story 005: die echten Katalog-Assets und Prefabs
- Story 006: `CatalogService`, Komponente `Item`, Hinweis bei abweichender Prefab-Masse
- Story 009: Build-Skript und Sperre beim Host-Start

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/ItemKind.cs`
- `Assets/_Project/Scripts/Items/ItemKindBlocks.cs`
- `Assets/_Project/Scripts/Items/ItemEnums.cs`
- `Assets/_Project/Scripts/Items/WareKind.cs`
- `Assets/_Project/Scripts/Items/NeedKind.cs`
- `Assets/_Project/Scripts/Items/ItemCatalog.cs`
- `Assets/_Project/Scripts/Items/CatalogReport.cs`
- `Assets/_Project/Scripts/Items/CatalogValidator.cs`
- `Assets/Tests/EditMode/Items/CatalogValidatorTests.cs`
- `Assets/Tests/EditMode/Items/CatalogTestFactory.cs` (Helfer für Test-Kataloge, auch für Story 002 bis 004)
- `Assets/_Project/Scripts/AssemblyInfo.cs` (Zugriff der Tests und Editor-Skripte auf die Befüll-Methoden)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/Tests/EditMode/Items/CatalogValidatorTests.cs` — must exist and pass

**Status**: [x] Erstellt, 23 Tests bestanden im Batchmode (2026-11-10, `test-results/editmode.xml`: 28 von 28 mit den 5 Ofen-Tests). Gegenprobe: Mit absichtlich falscher Massen-Regel schlägt `KindWithZeroMass_OneErrorNamingId` fehl.

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/gegenstandsarten/story-002-pruefregeln-masse-und-hacken.md`, `production/epics/gegenstandsarten/story-003-abkuehl-regeln.md`, `production/epics/gegenstandsarten/story-004-bruchregel-und-aufprall-formeln.md`, `production/epics/gegenstandsarten/story-005-startkatalog.md`
