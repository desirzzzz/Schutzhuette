# Story 010: Aufprall-Messpunkt, Zustand „Fällt“ und Zerbrechen

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Der Host misst ab jetzt jeden Aufprall an einer einzigen Stelle und entscheidet damit, ob etwas zerbricht. Eine
Tasse, die gegen die Wand geworfen wird, verschwindet; eine abgelegte bleibt heil. Schnell geworfene
Gegenstände fliegen nicht mehr durch dünne Wände. Das ist der erste spürbare Zuwachs an Physik-Chaos.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-003`, `TR-gegenstandsarten-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Netz-Autorität und Physik im Netz
**ADR Decision Summary**: Jeder Gegenstand hat beim Host einen `ImpactReporter`, die einzige Stelle, die `OnCollisionEnter` für Spiel-Logik auswertet; er meldet einmal an `ImpactEvents`. Beim Host gibt es die Zustände Liegt, Getragen und Fällt; `ContinuousDynamic` gilt nur, solange ein Gegenstand fällt und nicht kinematisch ist.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Zu prüfen (ADR-0001, Verification 1): Beim Umschalten der Kollisionsprüfung gibt es keine Konsolen-Warnung bei Host und Client, auch nicht beim Bruch, wenn NGO den Körper beim `Despawn` kinematisch setzt. Dieselben Fälle einmal mit dauerhaft `ContinuousSpeculative` wiederholen; zeigt `ContinuousDynamic` Warnungen oder Durchflüge und der Dauerwert nicht, gilt der Dauerwert, und die Abgleichstelle entfällt. NOT SOURCEABLE: Änderungen an PhysX in Unity 6.4–6.6; in welcher Reihenfolge zwei Körper ihren `OnCollisionEnter` bekommen. Aus dem NGO-Quelltext belegt: `Despawn(true)` aus einem Physik-Callback ist unbedenklich; NGO setzt `collisionDetectionMode` nirgends.

**Control Manifest Rules (this layer)**:
- Required: Drei Zustände beim Host (Liegt, Getragen, Fällt); „Fällt“ geht nicht ins Netz.
- Required: Kollisionsprüfung an einer einzigen Stelle je Gegenstand abgleichen: `ContinuousDynamic` nur, wenn er fällt und nicht kinematisch ist, sonst `Discrete`. Die Vorhersage-Kopie bekommt beim Erzeugen `ContinuousDynamic`.
- Required: Trifft ein nicht kinematischer Körper einen liegenden Gegenstand, schaltet dessen `ImpactReporter` ihn zuerst auf „Fällt“ und prüft erst danach die Bruchregel.
- Required: Kontaktpunkte ohne Allokation lesen (`contactCount`, `GetContact(i)`). Zerbrechen genau einmal melden und entfernen, in der Reihenfolge Melden → `ItemSpawning.Remove`. `ImpactEvents.Raise` sichert jeden Zuhörer einzeln ab.
- Forbidden: Nie `OnCollisionEnter` für Spiel-Logik außerhalb von `ImpactReporter`. Nie `collision.contacts` lesen, nie den `Collision`-Parameter speichern. Nie lösen Vorhersage-Kopien Bruchregel oder Meldedienst aus. Nie einen Zustand „zerbrochen“ als NetworkVariable.
- Guardrail: Durchgehende Kollisionsprüfung nur für fallende Gegenstände; `OnCollisionEnter` beim Host ohne Allokation; bei Clients rechnet keine Physik für Gegenstände.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 14 [EditMode]:** GIVEN eine fallende Tasse WHEN im selben Physikschritt zwei Kontakte mit 8 und 9 m/s kommen THEN wird Entfernen genau 1-mal ausgelöst und genau 1 Scherben-Geräusch angefordert, ohne Exception.
- [ ] **Kriterium 15 [PlayMode]:** Nur Host: GIVEN eine Tasse Tee liegt eingefroren auf dem Boden WHEN eine Vorratskiste mit 8 m/s waagrecht dagegenprallt THEN ist die Tasse nach höchstens 10 Physikschritten entfernt; WHEN die Kiste stattdessen aus Haltehöhe (5,01 m/s) senkrecht darauf fällt THEN existiert die Tasse nach 50 Schritten noch.
- [ ] **Würfe bis 15 m/s (TR-gegenstandsarten-005; ADR-0001, Verification 1) [PlayMode oder Manuell]:** Eine Tasse mit 15 m/s fliegt nicht durch eine 0,2 m dicke Innenwand, und beim Umschalten der Kollisionsprüfung erscheint keine Konsolen-Warnung bei Host und Client, auch nicht beim Bruch.

---

## Implementation Notes

*Derived from ADR-0001 Implementation Guidelines:*

- `ImpactReport` (struct), `ImpactEvents` (statisch, `Reported`, `Raise`, Reset bei `SubsystemRegistration`) und `ImpactReporter` in `Schutzhuette.Items`. Der Reporter wertet nur bei `IsServer` aus.
- `impact_speed` ist der größte Wert von `BreakRule.ImpactSpeed` über alle Kontaktpunkte (Story 004).
- Liegt der Gegenstand und trifft ihn ein Körper mit nicht kinematischem Rigidbody (`collision.rigidbody`), wechselt er im selben Callback zuerst auf „Fällt“. Schläft der Körper wieder ein, ist er wieder „Liegt“.
- Beim Bruch: Sperre gegen weitere Kontakte im selben Physikschritt, Melden mit `Broke = true`, danach `ItemSpawning.Remove` (stellt auf „Discrete“ und ruft `Despawn(true)`).
- Eigener Code, der einen Körper kinematisch macht (`AttachTo`) oder entfernt, stellt vorher auf „Discrete“. Das Loslassen und Werfen in `PlayerCarry` setzt „Fällt“.
- `Rigidbody.linearVelocity` nur beim Loslassen (Startgeschwindigkeit) und beim Aufheben (auf null) schreiben.
- Damit Kriterium 14 in EditMode läuft, steckt die Entscheidung „einmal melden, einmal entfernen“ in einer Klasse ohne Unity-Callback, die der Reporter aufruft; „Entfernen“ und „Scherben anfordern“ sind im Test Stubs.
- Für die Tests und den manuellen Wurf erscheinen `tee` und `vorratskiste` über `ItemSpawning.Spawn`. Bis die Startaufstellung existiert (Epic Saison-Ablauf), legt der Host beim Start eine kleine Testaufstellung mit Tee, Kaiserschmarrn und Speckbrot auf den Tisch der Stube.
- Das Editor-Skript hängt `ImpactReporter` an alle Gegenstands-Prefabs.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 011: Scherben-Effekt, Scherben-Klang und Aufprall-Geräusche über `ImpactEffects`
- Gäste reagieren auf Lärm und Treffer (ADR-0001, Verification 2): Epic Gäste, Plan-Schritt 5
- Tasse fällt beim Hinfallen des Trägers (Kriterium 17): sobald Hinfallen gebaut ist
- Ausrüstung „am Körper“: ADR-0004

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Items/ImpactReport.cs`
- `Assets/_Project/Scripts/Items/ImpactEvents.cs`
- `Assets/_Project/Scripts/Items/ImpactReporter.cs`
- `Assets/_Project/Scripts/Items/ItemPhysicsState.cs`
- `Assets/_Project/Scripts/Carrying/PlayerCarry.cs`
- `Assets/_Project/Scripts/Carrying/LocalGhost.cs`
- `Assets/_Project/Scripts/Carrying/CrateSpawner.cs`
- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/Tests/EditMode/Items/BreakOnceTests.cs`
- `Assets/Tests/PlayMode/Items/BreakOnImpactTests.cs`
- `Assets/Tests/PlayMode/Items/FastThrowWallTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/Tests/PlayMode/Items/BreakOnImpactTests.cs` und `Assets/Tests/EditMode/Items/BreakOnceTests.cs`; der Wand-Test als PlayMode-Test oder als Notiz nach Testanleitung (Entwickler bestätigt, mit dem Ergebnis des Vergleichs `ContinuousDynamic` gegen `ContinuousSpeculative`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-004-bruchregel-und-aufprall-formeln.md`, `production/epics/gegenstandsarten/story-007-erscheinen-und-entfernen.md`
- Unlocks: `production/epics/gegenstandsarten/story-011-scherben-und-aufprall-klang.md`
