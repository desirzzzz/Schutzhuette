# Story 008: Tragen und Ofen lesen den Katalog

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Die gebauten Systeme aus dem Prototyp stellen auf den Katalog um: Das Tragen nimmt Masse und Trageart aus der
Art, der Ofen die Brenndauer. Eigene Kopien dieser Werte in Komponenten und Prefabs entfallen. Das Spiel fühlt
sich danach gleich an; neu ist, dass eine Änderung im Datenobjekt überall wirkt.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: Jeder Spielwert einer Art steht im `ItemKind`; Komponenten und Prefabs führen keine eigenen Kopien. Code erkennt eine Art an ihrer Eigenschaft (`Kind.Fuel.Enabled`), nie an Kennung oder Namen.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Zu prüfen (ADR-0002, Verification 3): Nach einer Spielrunde zeigt `git status` keine Änderung an den Katalog-Dateien. Der Ofen entfernt Holz heute aus `OnTriggerEnter`; `Despawn` aus einem Physik-Callback ist laut NGO-Quelltext unbedenklich, läuft aber ab jetzt über `ItemSpawning.Remove`.

**Control Manifest Rules (this layer)**:
- Required: Jeder Spielwert einer Art steht im `ItemKind` (keine Brenndauer im `FuelItem`, keine Masse nur im Rigidbody).
- Required: Verschwinden nur über `ItemSpawning.Remove`.
- Forbidden: Nie eine Art an Kennung, Anzeigename oder Prefab-Name erkennen. Nie Datenobjekte zur Laufzeit schreiben. Nie eigene Kopien von Spielwerten einer Art in Feature-Komponenten.
- Guardrail: Nachschlagen ohne Kosten pro Frame; die Art hängt am Gegenstand (`Item.Kind`).

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 24 [PlayMode]:** GIVEN ein Test-Katalog, in dem die Vorratskiste 20 kg wiegt WHEN der Host sie erscheinen lässt und ein Spieler sie trägt THEN ist `Rigidbody.mass` = 20 und der Tempofaktor derselbe wie beim Wasserkanister (0,8 statt 0,9).
- [ ] **Kriterium 25 [PlayMode]:** GIVEN ein brennender Ofen mit Restzeit 60 s (unter der Obergrenze von 240 s des Ofens) WHEN ein Holzscheit hineinfällt THEN ist es entfernt und die Restzeit 105 s (steht im Katalog 60 s Brenndauer, dann 120 s); WHEN eine Axt hineinfällt THEN bleibt sie bestehen und R ändert sich nicht.
- [ ] **Kriterium 29 [Freundestest]:** GIVEN nur der Client hält die Truhe WHEN er 3 s zieht THEN bewegt sie sich weniger als 0,1 m; halten beide sie, bewegt sie sich. Dasselbe gilt für die Trage, sobald sie gebaut ist.
- [ ] **Kriterium 9 [Manuell]:** GIVEN eine Spielrunde mit Tragen, Werfen und Verbrennen WHEN Play beendet ist THEN zeigt `git status` keine Änderung an den Katalog-Dateien in `Assets/_Project/Data/`.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- Tragen: Tempo und Kamera-Wippen rechnen mit `Item.Kind.MassKg`; ob ein Gegenstand zwei Träger braucht, sagt `Kind.CarryMode`, nicht die Komponente am Prefab.
- Ofen: Brennbar ist, was `Kind.Fuel.Enabled` hat; die Brenndauer ist `Kind.Fuel.BurnSeconds`. Ein Gegenstand ohne „brennbar“ wird nicht verbraucht (Edge Case).
- `FuelItem` verliert seine eigene Brenndauer. Wird die Komponente dadurch überflüssig, entfernt der Entwickler die Datei im Unity-Editor (keine `.meta`-Datei von Hand löschen); das Editor-Skript nimmt sie vorher von den Prefabs.
- Die Tests für Tragen und Ofen nutzen einen Test-Katalog als Parameter, nicht den ausgelieferten.
- Kriterium 29 wird beim nächsten Freundestest geprüft; allein mit zwei Fenstern zählt nicht (GDD).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 010 und 011: Aufprall, Zerbrechen, Klang nach Klanggruppe
- Der Zustand „Am Körper“ für Ausrüstung (Kriterium 34): Epic Interaktion, ADR-0004
- Änderungen an den Ofen-Regeln selbst (Obergrenze, Wärme): Ofen-Dokument

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Carrying/PlayerCarry.cs`
- `Assets/_Project/Scripts/Carrying/Carryable.cs`
- `Assets/_Project/Scripts/Carrying/HeavyCarryable.cs`
- `Assets/_Project/Scripts/Carrying/PlayerHeavyCarry.cs`
- `Assets/_Project/Scripts/Hut/Stove.cs`
- `Assets/_Project/Scripts/Hut/FuelItem.cs`
- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/Tests/PlayMode/Items/CatalogCarryTests.cs`
- `Assets/Tests/PlayMode/Items/CatalogStoveTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: `Assets/Tests/PlayMode/Items/CatalogCarryTests.cs` und `Assets/Tests/PlayMode/Items/CatalogStoveTests.cs`; Kriterium 9 nach Testanleitung (Entwickler bestätigt), Kriterium 29 im Freundestest-Bericht

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-006-laufzeit-katalog-und-masse.md`, `production/epics/gegenstandsarten/story-007-erscheinen-und-entfernen.md`
- Unlocks: None
