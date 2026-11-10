# Story 002: Prüfregeln für Masse und Hacken

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Die Katalogprüfung bekommt die Regeln mit Rechnung: die Höchstmasse für einen Träger, Hinweise bei zu leichten
Arten mit Trageart „zu zweit“, Kreise beim Hacken und die Massenbilanz beim Hacken.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-007`, `TR-gegenstandsarten-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: Die Prüfung ist die reine Funktion `CatalogValidator.Validate`; die Katalog-Einstellungen (`max_solo_mass`, Toleranz beim Hacken) stehen im `ItemCatalog`, nicht im Code.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Keine engine-spezifische API; reine C#-Rechnung. Toleranz ±0,01 bei Massen, Vergleich der Hack-Bilanz mit Toleranz 0,0001 (GDD).

**Control Manifest Rules (this layer)**:
- Required: Prüfung als reine Funktion; Prüfregeln und Tests bekommen den Katalog als Parameter.
- Required: Jeder Spielwert einer Art steht im `ItemKind`; Katalog-Einstellungen im `ItemCatalog`.
- Forbidden: Nie feste Zahlen im Code (die Grenze 25 kg steht im Datenobjekt). Nie `CatalogService.Current` in Prüfregeln oder Tests.
- Guardrail: keine Kosten pro Frame.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 3 [EditMode]:** GIVEN `max_solo_mass` = 25 WHEN geprüft werden: allein 25,0 / 25,01 / 30 kg, zu zweit 80 kg THEN gültig / Fehler / Fehler / gültig. WHEN `max_solo_mass` im Datenobjekt auf 30 steht THEN ist allein mit 28 kg gültig (die Grenze steht nicht im Code).
- [ ] **Kriterium 4 [EditMode]:** GIVEN Trageart zu zweit WHEN geprüft werden: 20 kg Vorratspaket / 25 kg Rohstoff / 15 kg Sperrgut / 26 kg Vorratspaket THEN Hinweis / Hinweis / nichts / nichts; nie ein Fehler.
- [ ] **Kriterium 6 [EditMode]:** GIVEN hackbar A → A oder A → B → A WHEN geprüft THEN gibt es mindestens 1 Fehler, der den Kreis nennt; holzklotz → holzscheit ergibt keinen.
- [ ] **Kriterium 22 [EditMode]:** GIVEN ein Klotz mit 12 kg WHEN das Ergebnis 4 × 3 / 4 × 3,25 / 4 × 2,7 / 5 × 3 / 3 × 3 kg ist THEN Bilanz 0 / +1 / −1,2 / +3 / −3 kg und Meldung nichts / nichts / nichts / Hinweis / Hinweis (−1,2 sind genau 10 %, kein Hinweis; Vergleich mit Toleranz 0,0001); nie ein Fehler.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- Die Regeln stehen in GDD „Formulas“, Formel 1 (Höchstmasse für einen Träger) und Formel 4 (Masse beim Hacken). Vor dem Bau dort die genauen Grenzen nachlesen.
- Alle Regeln sind reine Funktionen im `CatalogValidator` (oder einer statischen Hilfsklasse daneben), ohne Szene testbar.
- Die Kreis-Suche folgt dem Verweis `Chop.Result` von Art zu Art und meldet den Kreis mit den Kennungen.
- Hinweise sperren den Start nie; nur Fehler setzen `StartBlocked`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: Datentypen und Grundprüfung
- Das Hacken selbst (Kriterium 23) kommt mit dem System „Holz hacken“ (Plan-Schritt 7)

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/CatalogValidator.cs`
- `Assets/Tests/EditMode/Items/CatalogMassRulesTests.cs`
- `Assets/Tests/EditMode/Items/CatalogChopRulesTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/Tests/EditMode/Items/CatalogMassRulesTests.cs` und `Assets/Tests/EditMode/Items/CatalogChopRulesTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-001-katalog-datentypen-und-grundpruefung.md`
- Unlocks: `production/epics/gegenstandsarten/story-005-startkatalog.md`
