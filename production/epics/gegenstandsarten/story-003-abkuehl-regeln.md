# Story 003: Abkühl-Regeln für Gerichte

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Die Rechenregel für warme und kalte Gerichte entsteht als reine Funktion, dazu der Hinweis der Katalogprüfung
bei einer zu kurzen Abkühlzeit. Angeschlossen wird die Regel erst, wenn die Saison-Uhr (Plan-Schritt 3) und die
Gerichte (Plan-Schritt 5) existieren.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung (Zeitquelle: ADR-0003)
**ADR Decision Summary**: Der Host speichert je Gericht nur den Kochzeitpunkt (`cooked_time`, `double` in Spielzeit). `is_warm` und `warm_fraction` rechnet jeder Rechner selbst mit `DishRules` (rein statisch); die Spielzeit kommt als Parameter.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Keine engine-spezifische API; reine C#-Rechnung mit `double`. Die Uhr des Clients kann knapp hinter dem Kochzeitpunkt liegen (Kriterium 19: `game_time` 99 bei `cooked_time` 100 ergibt 1,0, kein negativer Wert).

**Control Manifest Rules (this layer)**:
- Required: Zeitregeln für Gerichte stehen rein statisch in `DishRules`; Zeit kommt als Parameter.
- Required: Gespeicherte Zeitpunkte sind `double` in `game_time` (Core-Regel aus ADR-0003).
- Forbidden: Nie rechnen Spielregeln mit `Time.time`, `Time.deltaTime` oder `NetworkManager.ServerTime` direkt.
- Guardrail: keine Kosten pro Frame beim Host; Clients rechnen nur für die Anzeige.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 18 [EditMode]:** GIVEN cooked_time 100, cool_time 90 WHEN game_time 150 / 189,9 / 190 / 10 000 THEN warm / warm / kalt / kalt. GIVEN cool_time 0, −5 oder fehlend WHEN game_time 0 und 100 000 THEN immer warm, `warm_fraction` = 0.
- [ ] **Kriterium 19 [EditMode]:** GIVEN cooked_time 0, cool_time 150 WHEN game_time 0 / 100 / 150 / 300 THEN `warm_fraction` 1,0 / 0,333 / 0 / 0. GIVEN cooked_time 100 und game_time 99 (Uhr des Clients knapp zurück) THEN 1,0.
- [ ] **Kriterium 20 [EditMode]:** GIVEN floor_factor 6, max_path 15 m, walk_speed_min 4 m/s, carry_speed_factor 0,992 WHEN berechnet THEN ergibt sich 22,68 s; cool_time 20 gibt einen Hinweis, 90 nichts, ein Gericht ohne Abkühlzeit nichts; nie ein Fehler.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- `DishRules.IsWarm(double gameTime, double cookedTime, float coolTime)` und `DishRules.WarmFraction(...)` in `Schutzhuette.Items`. Die Formel steht in GDD „Formulas“, Formel 3.
- `coolTime ≤ 0` heißt: kühlt nie ab (`is_warm` bleibt wahr, `warm_fraction` = 0).
- Die Untergrenze für die Abkühlzeit (Kriterium 20) rechnet die Katalogprüfung aus den Katalog-Einstellungen (`floor_factor`, `max_path`, `walk_speed_min`) und gibt nur einen Hinweis.
- `DishState` (NetworkVariable mit dem Kochzeitpunkt) gehört nicht hierher.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- `DishState` und das Abkühlen im laufenden Spiel (Kriterien 21 und 30): kommen als eigene Story, sobald die Saison-Uhr steht (Epic Saison-Ablauf, Plan-Schritt 3)
- Was ein kaltes Gericht dem Gast wert ist: Gäste-Dokument

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/DishRules.cs`
- `Assets/_Project/Scripts/Items/CatalogValidator.cs`
- `Assets/Tests/EditMode/Items/DishRulesTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/Tests/EditMode/Items/DishRulesTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-001-katalog-datentypen-und-grundpruefung.md`
- Unlocks: `production/epics/gegenstandsarten/story-005-startkatalog.md`
