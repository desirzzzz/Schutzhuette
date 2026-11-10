# Story 004: Bruchregel und Aufprall-Formeln

> **Epic**: Gegenstandsarten
> **Status**: Complete (2026-11-10: 20 Tests grün, Entwickler hat die Exe getestet und den Commit freigegeben)
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-11-10
> **Last Updated**: 2026-11-10

## Context

Die Rechenregeln für das Zerbrechen entstehen als reine Funktionen: wie schnell ein Aufprall zählt, wann etwas
bricht und wie schnell ein Gegenstand nach dem Loslassen aus Haltehöhe aufkommt. Dazu kommt der Hinweis der
Katalogprüfung, wenn eine Bruchgeschwindigkeit so liegt, dass schon das Ablegen zerbricht oder gerade Würfe
nicht zerbrechen.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Netz-Autorität und Physik im Netz
**ADR Decision Summary**: Die Bruchregel ist eine reine statische Funktion (`BreakRule.BreaksOnImpact`). `impact_speed` ist der größte senkrechte Anteil der Relativgeschwindigkeit; nur was beim Host „fällt“, kann zerbrechen.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Keine engine-spezifische API außer `Vector3`; reine Rechnung. Toleranz ±0,01 bei m/s. Kein NaN bei Haltehöhe unter der Fläche (Kriterium 12).

**Control Manifest Rules (this layer)**:
- Required: Rechenregeln sind reine statische Funktionen, damit EditMode-Tests sie ohne Szene prüfen.
- Required: Jeder Spielwert einer Art steht im `ItemKind` (`Fragile.BreakSpeed`); Prüfwerte Haltehöhe und Wurftempo stehen in den Katalog-Einstellungen.
- Forbidden: Nie feste Zahlen im Code. Nie `CatalogService.Current` in Prüfregeln oder Tests.
- Guardrail: keine Allokation in der Rechnung, sie läuft später im Physik-Callback.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [x] **Kriterium 10 [EditMode]:** GIVEN ein flacher Wurf auf den Boden mit v_relative (6,0; −5,01; 0), Normale (0; 1; 0), break_speed 7,0, Tasse „fällt“ WHEN berechnet THEN ist `impact_speed` = 5,01 und die Tasse bleibt heil (der Gesamtbetrag 7,82 zählt nicht).
- [x] **Kriterium 11 [EditMode]:** GIVEN zerbrechlich, break_speed 7,0, „fällt“ WHEN impact_speed 4,2 / 6,1 / 7,0 / 7,01 / 8,0 / 12,5 THEN heil / heil / heil / bricht / bricht / bricht. Der schräge Wurf (5,14; 0; 6,13) gegen die Normale (0; 0; 1) ergibt 6,13 und bleibt heil. Speckbrot und eine Tasse im Zustand „getragen“ bleiben auch bei 12,5 heil.
- [x] **Kriterium 12 [EditMode]:** GIVEN (Haltehöhe; Fläche) = (1,28; 0) / (1,28; 0,9) / (1,8; 0) / (1,28; 2,0) WHEN `release_fall_speed` berechnet wird THEN 5,01 / 2,73 / 5,94 / 0, ohne NaN.
- [x] **Kriterium 13 [EditMode]:** GIVEN Prüfwerte Haltehöhe 1,28 m und Wurftempo 8,0 m/s (Fenster 5,51 bis 7,20) WHEN break_speed 7,0 / 5,52 / 7,20 / 5,45 / 7,25 geprüft wird THEN nichts / nichts / nichts / Hinweis „Ablegen zerbricht“ / Hinweis „gerade Würfe zerbrechen nicht“; nie ein Fehler.

---

## Implementation Notes

*Derived from ADR-0001 Implementation Guidelines:*

- `BreakRule.ImpactSpeed(Vector3 relativeVelocity, Vector3 contactNormal)` liefert `|dot(relativeVelocity, normal)|`; `BreakRule.BreaksOnImpact(bool isFragile, bool isFalling, float impactSpeed, float breakSpeed)` in `Schutzhuette.Items`. Die Formeln stehen in GDD „Formulas“, Formel 2.
- Gebrochen wird nur bei `impactSpeed > breakSpeed` (7,0 bleibt heil, 7,01 bricht), nur zerbrechlich und nur im Zustand „Fällt“.
- `release_fall_speed` und das Fenster für die Bruchgeschwindigkeit (Kriterium 13) sind reine Funktionen; die Katalogprüfung nutzt sie für Hinweise.
- Über mehrere Kontaktpunkte zählt später der größte Wert; das Lesen der Kontakte gehört zu Story 010.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 010: `ImpactReporter`, Zustand „Fällt“, Entfernen des zerbrochenen Gegenstands
- Story 011: Scherben-Effekt und Klang

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/BreakRule.cs`
- `Assets/_Project/Scripts/Items/CatalogValidator.cs`
- `Assets/Tests/EditMode/Items/BreakRuleTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/Tests/EditMode/Items/BreakRuleTests.cs` — must exist and pass

**Status**: [x] Erstellt, 20 Tests bestanden im Batchmode (2026-11-10, `test-results/editmode.xml`: 48 von 48). Gegenprobe: Mit `>=` statt `>` in `BreaksOnImpact` schlägt `FragileAndFalling_BreaksOnlyAboveBreakSpeed(7.0f,False)` fehl.

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-001-katalog-datentypen-und-grundpruefung.md`
- Unlocks: `production/epics/gegenstandsarten/story-005-startkatalog.md`, `production/epics/gegenstandsarten/story-010-aufprall-messpunkt-und-zerbrechen.md`
