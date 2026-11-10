# Epic: Gegenstandsarten

> **Layer**: Foundation
> **GDD**: design/gdd/gegenstandsarten.md
> **Architecture Module**: Gegenstandsarten (Katalog und Kennung), `Assets/_Project/Scripts/Items/`, Namespace `Schutzhuette.Items`
> **Status**: Ready
> **Stories**: 11 stories (2026-11-10), siehe Tabelle unten

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [Katalog-Datentypen und Grundprüfung](story-001-katalog-datentypen-und-grundpruefung.md) | Logic | Complete | ADR-0002 |
| 002 | [Prüfregeln für Masse und Hacken](story-002-pruefregeln-masse-und-hacken.md) | Logic | Ready | ADR-0002 |
| 003 | [Abkühl-Regeln für Gerichte](story-003-abkuehl-regeln.md) | Logic | Ready | ADR-0002 |
| 004 | [Bruchregel und Aufprall-Formeln](story-004-bruchregel-und-aufprall-formeln.md) | Logic | Complete | ADR-0001 |
| 005 | [Startkatalog mit 13 Arten und 7 Waren](story-005-startkatalog.md) | Config/Data | Ready | ADR-0002 |
| 006 | [Laufzeit-Katalog, Komponente Item und Masse aus dem Katalog](story-006-laufzeit-katalog-und-masse.md) | Integration | Ready | ADR-0002 |
| 007 | [Erscheinen und Entfernen über ItemSpawning, Ware am Vorratspaket](story-007-erscheinen-und-entfernen.md) | Integration | Ready | ADR-0002 |
| 008 | [Tragen und Ofen lesen den Katalog](story-008-tragen-und-ofen-lesen-den-katalog.md) | Integration | Ready | ADR-0002 |
| 009 | [Build und Host-Start sperren bei Katalogfehlern](story-009-build-und-host-start-sperren.md) | Integration | Ready | ADR-0002 |
| 010 | [Aufprall-Messpunkt, Zustand „Fällt“ und Zerbrechen](story-010-aufprall-messpunkt-und-zerbrechen.md) | Integration | Ready | ADR-0001 |
| 011 | [Scherben und Aufprall-Klang über ImpactEffects](story-011-scherben-und-aufprall-klang.md) | Integration | Ready | ADR-0001 |

**Reihenfolge (Entscheidung des Entwicklers, 2026-11-10: „Schnell zu den Tassen“).** Zuerst alles, was das
Zerbrechen braucht, damit das erste sichtbare Ergebnis früh kommt:

1. 001 (erledigt)
2. 004: Bruchregel (erledigt)
3. 005, erster Teil: Steckbriefe und Prefabs für `vorratskiste`, `holzscheit`, `truhe` und `tee`. Das Kriterium
   „genau 13 Arten und 7 Waren“ bleibt offen, bis der zweite Teil gebaut ist.
4. 006 und 007: Das Spiel liest den Katalog und lässt Gegenstände darüber erscheinen
5. 010: Tassen zerbrechen

Danach in dieser Folge: 011 (Scherben-Klang), 002, 003, 005 zweiter Teil, 008, 009.

**Abnahmekriterien des GDD, die noch keine Story haben** (28 von 34 sind abgedeckt). Sie bekommen eine Story,
sobald ihr System existiert (`/create-stories gegenstandsarten` erneut):

| Kriterium | Inhalt | Wartet auf |
|-----------|--------|-----------|
| 17 | Tasse fällt beim Hinfallen des Trägers und bleibt aus Haltehöhe heil | Hinfallen und Ragdoll (Plan-Schritt 17) |
| 21, 30 | Tee kühlt im laufenden Spiel ab, bei Host und Client höchstens 0,5 s auseinander | Saison-Uhr (Epic Saison-Ablauf, Plan-Schritt 3) und Gerichte |
| 23 | Klotz hacken ergibt 4 Holzscheite mit je 3,0 kg | Holz hacken (Plan-Schritt 7) |
| 31 | Servieren schlägt fehl, wenn die Tasse vorher zerbrochen ist | Gäste (Plan-Schritt 5) |
| 34 | Ausrüstung geht in den Zustand „Am Körper“ und zerbricht dort nie | Interaktion und ADR-0004 (Plan-Schritt 4) |

## Overview

Gegenstandsarten ist der Katalog aller Dinge, die man in Schutzhütte anfassen kann: Holz, Kisten, Zutaten,
Gerichte, Werkzeug, die Truhe und die Trage. Dieses Epic macht aus den heutigen Kisten, Holzscheiten und der
Truhe Datenobjekte mit Masse, Trageart und Eigenschaften (brennbar, zerbrechlich, Vorratspaket und weitere),
dazu die Waren als schlanken Datentyp. Tragen, Ofen, Lager, Kochen und Gäste lesen danach dieselben Daten,
statt eigene Werte zu führen. Zum Epic gehören auch die Katalogprüfung, die Build und Host-Start bei Fehlern
sperrt, die Bruchregel mit dem Messpunkt für Aufpralle beim Host und die durchgehende Kollisionsprüfung für
schnelle Würfe. Das ist Plan-Schritt 2 des Vertical Slice (`production/milestones/vertical-slice.md`).

Die Spielzeit für das Abkühlen der Gerichte (TR-gegenstandsarten-004) liefert die Saison-Uhr aus dem Epic
Saison-Ablauf (ADR-0003, Plan-Schritt 3). In diesem Epic entsteht die reine Rechenregel `DishRules` mit ihren
Tests; angeschlossen wird sie, sobald Uhr und Gerichte existieren.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|------------------|-------------|
| ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung | Ein ScriptableObject je Art, Ware und Bedürfnis; das Prefab verrät die Art; Kennungen als `FixedString32Bytes`; Erscheinen und Entfernen nur über `ItemSpawning`; Prüfung als reine Funktion | HIGH |
| ADR-0001: Netz-Autorität und Physik im Netz | Der Host entscheidet; `ImpactReporter` misst Aufpralle und meldet an `ImpactEvents`; Bruchregel als reine Funktion; `ContinuousDynamic` nur im Zustand „Fällt“ | HIGH |
| ADR-0003: Saison-Uhr, Spielzeit und Meldungen zwischen Systemen | Liefert `game_time` für das Abkühlen der Gerichte; reine Funktionen bekommen die Zeit als Parameter | HIGH |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-gegenstandsarten-001 | Katalog aller Arten als Datenobjekte: Masse, Trageart, Eigenschaften (brennbar, Vorratspaket, Gericht, zerbrechlich, Werkzeug, Ausrüstung, hackbar) | ADR-0002 ✅ |
| TR-gegenstandsarten-002 | Über das Netz wird eine Art nur über ihre feste Kennung verschickt, nie als Objekt | ADR-0002 ✅ |
| TR-gegenstandsarten-003 | Bruchregel: Zerbrechliches zerbricht ab einer Aufprall-Geschwindigkeit, der Host entscheidet (Formel 2) | ADR-0001 ✅ |
| TR-gegenstandsarten-004 | Gerichte kühlen mit game_time ab (is_warm, warm_fraction); der Host speichert nur den Kochzeitpunkt (Formel 3) | ADR-0002 ✅, Zeitquelle ADR-0003 ✅ |
| TR-gegenstandsarten-005 | Würfe bis 15 m/s dürfen dünne Wände nicht durchfliegen (Open Question 3, durchgehende Kollisionsprüfung) | ADR-0001 ✅ |
| TR-gegenstandsarten-006 | Waren (ware_*) sind ein schlanker Datentyp ohne Körper, nur Bestand im Lager und Inhalt eines Pakets | ADR-0002 ✅ |
| TR-gegenstandsarten-007 | Die Katalogprüfung ist eine reine Funktion; doppelte Kennung, fehlendes Prefab, Masse <= 0, unbekannte Ware oder ein Hack-Kreis sperren Build und Host-Start (Edge Cases Katalog, Kriterien 2, 5, 33) | ADR-0002 ✅ |

Untraced Requirements: None.

## Definition of Done

- Every story implemented, reviewed and closed with `/story-done`
- Every acceptance criterion of design/gdd/gegenstandsarten.md verified
- Logic and Integration stories have passing tests in the test root (`Assets/Tests/EditMode/`,
  `Assets/Tests/PlayMode/`)
- Visual/Feel and UI stories have retained screenshots in `production/qa/evidence/`
  (a lead sign-off too for Visual/Feel)
