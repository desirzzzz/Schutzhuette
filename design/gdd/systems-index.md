# Systems Index: Schutzhütte

> **Status**: Approved (vom Entwickler am 2026-11-08 freigegeben)
> **Created**: 2026-11-08
> **Last Updated**: 2026-11-09 (Design-Review Gäste und Bedürfnisse: Approved)
> **Source Concept**: docs/KONZEPT.md (Vollfassung) und design/game-brief.md (Kurzfassung)
> **Grundlage für den Umfang**: production/milestones/vertical-slice.md

---

## Overview

Schutzhütte ist eine Koop-Simulation für 1–4 Spieler. Eine Saison dauert 30–40 Minuten und läuft in
drei Phasen: Nachmittag, Abend und Nacht mit dem Nachteinsatz. Danach folgen die Bewertung und die
Upgrades. Jedes System wird an den drei Säulen gemessen: **Gemütlichkeit**, **Physik-Chaos** und
**Teamwork unter Druck**. Der Prototyp hat die sechs MVP-Systeme gebaut, rund um die Frage, ob Tragen
zu zweit Spaß macht. Der Vertical Slice baut darauf eine komplette Saison: Die Uhr erzeugt Druck, die
Gäste geben dem Tragen einen Zweck, und der Nachteinsatz ist der Höhepunkt. Alles läuft über den Host
(Architektur-Regel 2), Spielwerte stehen in Datenobjekten (Regel 4).

### Die Systeme im Einzelnen

**MVP (Prototyp, gebaut).** Die sechs Systeme sind ohne eigenes GDD entstanden: Auf Stufe `minimal`
galt der Game Brief. Wo der Vertical Slice eines davon erweitert, beschreibt das GDD des neuen Systems
die Erweiterung. Ein nachträgliches GDD ist mit `/reverse-document` möglich.

- **Netzwerk-Sitzung:** Host starten, beitreten, Spieler spawnen, Versionsprüfung beim Beitreten
  (NGO, Host-Client). Alle anderen Systeme bauen darauf auf.
- **Bewegung und Kamera:** First Person, synchronisiert. Die Kamera wippt mit Last.
- **Tragen und Werfen:** Aufheben, tragen, ablegen, werfen. Mit Client-Vorhersage, damit es auch bei
  Ping flüssig bleibt.
- **Tragen zu zweit:** Die Truhe bewegt sich nur, wenn zwei sie halten, schaukelt und bleibt an
  Türrahmen hängen. Die Trage im Nachteinsatz baut darauf auf.
- **Holzofen und Wärme:** Verbraucht Holz und geht aus. Die Wärme ist ein einfacher Wert. Im Vertical
  Slice wärmt der Ofen die ganze Hütte, und die Gäste reagieren auf Kälte.
- **Geräusche:** Aufprall nach Gewicht, Schritte auf Holz und Schnee, Ofen-Knistern, Wind. Wächst mit
  jedem neuen System mit.

**Vertical Slice (neu).**

- **Gegenstandsarten** *(abgeleitet)*: Holz, Zutaten, Teller, Wasser, Werkzeug und so weiter als
  Datenobjekte mit Gewicht und Eigenschaften. Fast jedes System liest sie, deshalb kommt dieses System
  zuerst.
- **Interaktion** *(abgeleitet)*: Ein gemeinsames „Benutzen“ für Herd, Bett, Kurbel, Hackstock,
  Erste Hilfe und Fenster. Heute kann die Taste E nur aufheben. Engpass: Fünf Systeme hängen daran.
- **Saison-Ablauf und Spielphasen:** Die Uhr des Hosts, die Phasen Nachmittag → Abend → Nacht, das Ende
  mit der Bewertung und der Start der nächsten Saison. Sie erzeugt den Druck der Säule „Teamwork unter
  Druck“. Engpass: Fünf Systeme hängen daran.
- **Steam-Lobby und Einladen:** Beitreten über Steam-Freunde, anfangs mit Valves kostenloser Test-App
  480. Das Beitreten über IP bleibt als Ausweg.
- **Gäste und Bedürfnisse** (mit Wegfindung): 3 Typen (Wanderer will essen, Familie braucht Betten,
  Bergsteiger kommt verletzt). Dazu Zufriedenheit, Trinkgeld und Reaktion auf Kälte, Lärm und Chaos.
  Die Gäste laufen selbst zu Tisch und Bett, der Host berechnet die Wege. Das ist der Zweck hinter dem
  Tragen (Säule „Gemütlichkeit“).
- **Kochen und Gerichte:** 3 Rezepte mit je 2–4 Schritten (Vorschlag: Tee, Speckbrot,
  Kaiserschmarrn). Ein fertiges Gericht ist ein Gegenstand, den man dem Gast bringt.
- **Lager und Vorräte:** Ein kleines Lager, dessen Bestand zur Neige geht. Nachschub kommt über die
  Seilbahn.
- **Holz hacken:** Holz kommt nicht mehr fertig, es muss am Hackstock gehackt werden. Beliefert den
  Ofen.
- **Versorgung: Betten und Erste Hilfe** *(abgeleitet)*: Die Stationen für die Familie und den
  verletzten Bergsteiger. Sie melden dem Gast nur „Bedürfnis erfüllt“.
- **Materialseilbahn:** Einer belädt unten, einer kurbelt oben, der Wind lässt die Kiste schaukeln.
  Physik-Chaos und Teamwork in einem.
- **Wetter und Eskalation:** Der Sturm wird über die Phasen stärker: Wind, Sicht, Kälte.
- **Ereignisse** (mit Strom und Licht): 5 Ereignisse als Datenobjekte. Ein Fenster fliegt auf, der
  Generator fällt aus, die Wasserleitung friert ein, eine Dachlawine verschüttet den Eingang, die
  Seilbahn klemmt (Vorschlag). Dazu gehören Generator, Stromausfall und Stirnlampen.
- **Nachteinsatz:** Ein Gast fehlt. Suche draußen im Sturm mit Stirnlampe, Seil und Funkgerät, dann zu
  zweit auf der Trage zurück. Der Höhepunkt jeder Saison.
- **Solo-Hüttenhund:** Hilft solo beim Suchen und beim Tragen kleiner Dinge, damit eine Saison auch
  allein zu schaffen ist. Offene Designfrage: Wie gehen Truhe und Trage solo?
- **Figuren-Animation und Ragdoll:** Die Figuren wackeln leicht und fallen beim Hinfallen als Ragdoll
  um. Bringt Clips und Trailer-Momente (Säule „Physik-Chaos“).
- **Bewertung:** 1 bis 3 Edelweiß pro Saison, aus der Zufriedenheit der Gäste und dem Ausgang des
  Nachteinsatzes.
- **Hüttenkasse und Upgrades:** Trinkgeld kauft Upgrades zwischen den Saisons (Vorschlag: 5). Upgrades
  ändern nur Werte in den Datenobjekten der anderen Systeme. Ohne Speichern gilt der Fortschritt nur
  innerhalb einer Spielsitzung.
- **Sprachchat und Funkgerät:** In der Nähe hört man sich, mit Abstand wird es leiser. Draußen im
  Nachteinsatz überträgt nur das Funkgerät, und es rauscht. Braucht ein neues Paket (Vivox oder
  Dissonance).
- **HUD** *(abgeleitet)*: Uhr und Phase, was die Gäste wollen, Ofen, Hüttenkasse.
- **Menüs** *(abgeleitet)*: Hauptmenü, Lobby, Bewertungs- und Upgrade-Bildschirm, Einstellungen.
  Ersetzen die heutigen Test-Knöpfe.

**Alpha.** Kraxe (Rückentrage, stapelt physikalisch), Ausdauer und Rutschen auf Eis, Stolpern und
Mitreißen, Musik (wird mit dem Sturm dichter), Spielstand speichern *(abgeleitet)*, Tutorial
*(abgeleitet)*.

**Full Vision.** Weitere Hütten (Gletscherhütte, Felsbiwak) und Kosmetik (Hüte, Trachten, Hundefelle).

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Netzwerk-Sitzung | Core | MVP | Implemented | — | — |
| 2 | Bewegung und Kamera | Core | MVP | Implemented | — | Netzwerk-Sitzung |
| 3 | Tragen und Werfen | Gameplay | MVP | Implemented | — | Bewegung und Kamera, Gegenstandsarten |
| 4 | Tragen zu zweit | Gameplay | MVP | Implemented | — | Tragen und Werfen, Gegenstandsarten |
| 5 | Holzofen und Wärme | Gameplay | MVP | Implemented | — | Tragen und Werfen, Gegenstandsarten, Saison-Ablauf und Spielphasen |
| 6 | Geräusche | Audio | MVP | Implemented | — | Netzwerk-Sitzung, Tragen und Werfen, Gegenstandsarten |
| 7 | Gegenstandsarten (inferred) | Core | Vertical Slice | Approved | design/gdd/gegenstandsarten.md | — |
| 8 | Interaktion (inferred) | Core | Vertical Slice | Approved | design/gdd/interaktion.md | Netzwerk-Sitzung, Bewegung und Kamera, Tragen und Werfen, Gegenstandsarten, Saison-Ablauf und Spielphasen |
| 9 | Saison-Ablauf und Spielphasen | Core | Vertical Slice | Approved | design/gdd/saison-ablauf.md | Netzwerk-Sitzung, Gegenstandsarten |
| 10 | Steam-Lobby und Einladen | Core | Vertical Slice | Approved | design/gdd/steam-lobby.md | Netzwerk-Sitzung, Saison-Ablauf und Spielphasen |
| 11 | Gäste und Bedürfnisse | Gameplay | Vertical Slice | Approved | design/gdd/gaeste.md | Netzwerk-Sitzung, Saison-Ablauf und Spielphasen, Holzofen und Wärme, Gegenstandsarten, Interaktion |
| 12 | Kochen und Gerichte | Gameplay | Vertical Slice | Not Started | — | Interaktion, Gegenstandsarten, Lager und Vorräte |
| 13 | Lager und Vorräte | Economy | Vertical Slice | Not Started | — | Gegenstandsarten, Tragen und Werfen, Saison-Ablauf und Spielphasen |
| 14 | Holz hacken | Gameplay | Vertical Slice | Not Started | — | Interaktion, Gegenstandsarten, Holzofen und Wärme |
| 15 | Versorgung: Betten und Erste Hilfe (inferred) | Gameplay | Vertical Slice | Not Started | — | Gäste und Bedürfnisse, Interaktion, Gegenstandsarten |
| 16 | Materialseilbahn | Gameplay | Vertical Slice | Not Started | — | Interaktion, Tragen und Werfen, Lager und Vorräte, Wetter und Eskalation, Gegenstandsarten |
| 17 | Wetter und Eskalation | Gameplay | Vertical Slice | Not Started | — | Saison-Ablauf und Spielphasen |
| 18 | Ereignisse (mit Strom und Licht) | Gameplay | Vertical Slice | Not Started | — | Saison-Ablauf und Spielphasen, Wetter und Eskalation, Interaktion, Holzofen und Wärme, Lager und Vorräte, Materialseilbahn |
| 19 | Nachteinsatz | Gameplay | Vertical Slice | Not Started | — | Saison-Ablauf und Spielphasen, Wetter und Eskalation, Gäste und Bedürfnisse, Tragen zu zweit, Ereignisse (mit Strom und Licht), Sprachchat und Funkgerät, Interaktion |
| 20 | Solo-Hüttenhund | Gameplay | Vertical Slice | Not Started | — | Gäste und Bedürfnisse, Tragen und Werfen, Nachteinsatz, Gegenstandsarten |
| 21 | Figuren-Animation und Ragdoll | Gameplay | Vertical Slice | Not Started | — | Netzwerk-Sitzung, Bewegung und Kamera, Tragen und Werfen |
| 22 | Bewertung | Progression | Vertical Slice | Not Started | — | Gäste und Bedürfnisse, Saison-Ablauf und Spielphasen, Nachteinsatz |
| 23 | Hüttenkasse und Upgrades | Progression | Vertical Slice | Not Started | — | Bewertung, Gäste und Bedürfnisse, Saison-Ablauf und Spielphasen |
| 24 | Sprachchat und Funkgerät | Audio | Vertical Slice | Not Started | — | Netzwerk-Sitzung, Interaktion |
| 25 | HUD (inferred) | UI | Vertical Slice | Not Started | — | Saison-Ablauf und Spielphasen, Gäste und Bedürfnisse, Holzofen und Wärme, Hüttenkasse und Upgrades, Interaktion, Steam-Lobby und Einladen |
| 26 | Menüs (inferred) | UI | Vertical Slice | Not Started | — | Netzwerk-Sitzung, Steam-Lobby und Einladen, Bewertung, Hüttenkasse und Upgrades, Saison-Ablauf und Spielphasen |
| 27 | Spielstand speichern (inferred) | Persistence | Alpha | Not Started | — | Hüttenkasse und Upgrades |
| 28 | Kraxe | Gameplay | Alpha | Not Started | — | Tragen und Werfen, Gegenstandsarten |
| 29 | Ausdauer und Rutschen auf Eis | Gameplay | Alpha | Not Started | — | Bewegung und Kamera, Wetter und Eskalation |
| 30 | Stolpern und Mitreißen | Gameplay | Alpha | Not Started | — | Tragen zu zweit, Figuren-Animation und Ragdoll |
| 31 | Musik | Audio | Alpha | Not Started | — | Saison-Ablauf und Spielphasen, Wetter und Eskalation |
| 32 | Tutorial (inferred) | Meta | Alpha | Not Started | — | Interaktion, Gäste und Bedürfnisse, Saison-Ablauf und Spielphasen |
| 33 | Weitere Hütten | Gameplay | Full Vision | Not Started | — | Saison-Ablauf und Spielphasen, Wetter und Eskalation, Ereignisse (mit Strom und Licht) |
| 34 | Kosmetik | Meta | Full Vision | Not Started | — | Figuren-Animation und Ragdoll, Hüttenkasse und Upgrades |

---

## Categories

| Category | Description | Typical Systems |
|----------|-------------|-----------------|
| **Core** | Grundlage, auf der alles aufbaut | Netzwerk-Sitzung, Bewegung, Gegenstandsarten, Interaktion, Saison-Ablauf, Steam-Lobby |
| **Gameplay** | Die Systeme, die den Spaß machen | Tragen, Gäste, Kochen, Seilbahn, Wetter, Ereignisse, Nachteinsatz, Ragdoll |
| **Progression** | Fortschritt über die Saisons | Bewertung, Hüttenkasse und Upgrades |
| **Economy** | Bestände, Verbrauch, Nachschub | Lager und Vorräte |
| **Persistence** | Stand über das Spielende hinaus | Spielstand speichern |
| **UI** | Anzeigen für die Spieler | HUD, Menüs |
| **Audio** | Klang, Musik, Stimme | Geräusche, Sprachchat und Funkgerät, Musik |
| **Meta** | Außerhalb der Kernschleife | Tutorial, Kosmetik |

Narrative entfällt: Es gibt keine Story-Kampagne (KONZEPT, „Bewusst nicht dabei“).

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Für die Kernfrage nötig: Macht Tragen zu zweit Spaß? | Prototyp (gebaut) | Gebaut, GDD nur bei Bedarf |
| **Vertical Slice** | Eine komplette Saison in einer Hütte, erste echte Grafik, Steam-Seite | Vertical Slice | Design ZUERST |
| **Alpha** | Alle Mechaniken des Konzepts in grober Form | Demo und Playtests | Design danach |
| **Full Vision** | Inhalte für Version 1.0 | Version 1.0 | Design bei Bedarf |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. Netzwerk-Sitzung *(gebaut)*: Der Host entscheidet über alles, jedes System läuft darüber.
2. Gegenstandsarten: Datenobjekte, die alle Systeme mit Gegenständen lesen.

### Core Layer (depends on foundation)

1. Bewegung und Kamera *(gebaut)*: braucht die Netzwerk-Sitzung.
2. Tragen und Werfen *(gebaut)*: braucht Bewegung und Kamera und die Gegenstandsarten.
3. Interaktion: braucht Netzwerk-Sitzung, Bewegung und Kamera, Tragen und Werfen sowie die
   Gegenstandsarten (Werkzeug, Kategorie, Ausrüstung); die Spielzeit `game_time` kommt aus dem
   Saison-Ablauf. **Engpass.**
4. Saison-Ablauf und Spielphasen: braucht die Netzwerk-Sitzung. **Engpass.**
5. Steam-Lobby und Einladen: braucht die Netzwerk-Sitzung und (weich) den Saison-Ablauf, dessen
   Beitrittssperre auch für Steam gilt.

### Feature Layer (depends on core)

1. Tragen zu zweit *(gebaut)*: braucht Tragen und Werfen.
2. Holzofen und Wärme *(gebaut, wird erweitert)*: braucht Tragen und Werfen und die Gegenstandsarten
   (Brenndauer), setzt sich bei „Saison beginnt“ zurück (Saison-Ablauf). Holz kommt ohne Taste hinein,
   deshalb keine Interaktion.
3. Holz hacken: braucht Interaktion und Gegenstandsarten, beliefert den Ofen.
4. Lager und Vorräte: braucht Gegenstandsarten sowie Tragen und Werfen; setzt sich bei „Saison beginnt“
   zurück (Saison-Ablauf).
5. Kochen und Gerichte: braucht Interaktion, Gegenstandsarten und das Lager.
6. Materialseilbahn: braucht Interaktion, Tragen, das Lager und das Wetter (Schaukeln).
7. Gäste und Bedürfnisse: braucht Saison, Wärme, Gegenstandsarten und Interaktion. **Engpass.**
8. Versorgung (Betten, Erste Hilfe): braucht Gäste und Interaktion.
9. Wetter und Eskalation: braucht die Saison.
10. Ereignisse (mit Strom und Licht): braucht Saison, Wetter, Interaktion, Wärme, Lager und Seilbahn.
11. Nachteinsatz: braucht Saison, Wetter, Gäste, Tragen zu zweit, Ereignisse (Stirnlampe), Interaktion
    (Erste Hilfe am Vermissten) und das Funkgerät. Das Funkgerät ist eine weiche Abhängigkeit: Ohne es
    geht der Einsatz auch, nur ohne Funk.
12. Solo-Hüttenhund: braucht Gäste (dieselbe Wegfindung), Tragen und den Nachteinsatz.
13. Figuren-Animation und Ragdoll: braucht Netzwerk, Bewegung und Tragen (beim Hinfallen fällt der
    Gegenstand aus der Hand).
14. Bewertung: braucht Gäste, Saison und Nachteinsatz.
15. Hüttenkasse und Upgrades: braucht Bewertung, Gäste (Trinkgeld) und den Saison-Ablauf (Phase
    „Upgrades“, Rückfall der Kasse beim Abbruch).
16. Sprachchat und Funkgerät: braucht die Netzwerk-Sitzung und die Interaktion (Funkgerät am Gürtel).

### Presentation Layer (depends on features)

1. HUD: braucht Saison, Gäste, Ofen, Hüttenkasse, Interaktion (Hinweis am Fadenkreuz,
   Fortschrittsbalken, Ausrüstungs-Symbole) und Steam-Lobby (Namensschilder).
2. Menüs: braucht Netzwerk, Steam-Lobby, Bewertung, Upgrades und den Saison-Ablauf (Vorbereitung,
   Bewertungs- und Upgrade-Bildschirm, Abbrechen, Pause).

### Polish Layer (depends on everything)

1. Geräusche *(gebaut)*: wächst mit jedem System mit.
2. Musik *(Alpha)*: braucht Saison und Wetter.
3. Tutorial *(Alpha)*: braucht Interaktion, Gäste und Saison.

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Gegenstandsarten | Vertical Slice | Foundation | game-designer, systems-designer | S |
| 2 | Interaktion | Vertical Slice | Core | game-designer, gameplay-programmer | M |
| 3 | Saison-Ablauf und Spielphasen | Vertical Slice | Core | game-designer | M |
| 4 | Steam-Lobby und Einladen | Vertical Slice | Core | network-programmer | S |
| 5 | Gäste und Bedürfnisse | Vertical Slice | Feature | game-designer, ai-programmer | L |
| 6 | Kochen und Gerichte | Vertical Slice | Feature | game-designer | M |
| 7 | Lager und Vorräte | Vertical Slice | Feature | game-designer, economy-designer | S |
| 8 | Holzofen und Wärme (Erweiterung) | MVP | Feature | game-designer | S |
| 9 | Holz hacken | Vertical Slice | Feature | game-designer | S |
| 10 | Versorgung: Betten und Erste Hilfe | Vertical Slice | Feature | game-designer | S |
| 11 | Materialseilbahn | Vertical Slice | Feature | game-designer, gameplay-programmer | M |
| 12 | Wetter und Eskalation | Vertical Slice | Feature | game-designer | M |
| 13 | Ereignisse (mit Strom und Licht) | Vertical Slice | Feature | game-designer, systems-designer | M |
| 14 | Nachteinsatz | Vertical Slice | Feature | game-designer, level-designer | L |
| 15 | Solo-Hüttenhund | Vertical Slice | Feature | game-designer, ai-programmer | M |
| 16 | Figuren-Animation und Ragdoll | Vertical Slice | Feature | technical-artist, network-programmer | M |
| 17 | Bewertung | Vertical Slice | Feature | systems-designer | S |
| 18 | Hüttenkasse und Upgrades | Vertical Slice | Feature | economy-designer | M |
| 19 | Sprachchat und Funkgerät | Vertical Slice | Feature | network-programmer, sound-designer | M |
| 20 | HUD | Vertical Slice | Presentation | ux-designer | S |
| 21 | Menüs | Vertical Slice | Presentation | ux-designer | M |
| 22 | Spielstand speichern | Alpha | Feature | gameplay-programmer | S |
| 23 | Kraxe | Alpha | Feature | game-designer | M |
| 24 | Ausdauer und Rutschen auf Eis | Alpha | Feature | game-designer | S |
| 25 | Stolpern und Mitreißen | Alpha | Feature | game-designer | M |
| 26 | Musik | Alpha | Polish | audio-director | M |
| 27 | Tutorial | Alpha | Polish | ux-designer | M |
| 28 | Weitere Hütten | Full Vision | Feature | level-designer | L |
| 29 | Kosmetik | Full Vision | Polish | art-director | M |

Effort: S = 1 Sitzung, M = 2–3 Sitzungen, L = 4 oder mehr. Eine Sitzung ist ein Design-Gespräch, an
dessen Ende ein fertiges GDD steht. Die Reihenfolge folgt dem Bauplan in
`production/milestones/vertical-slice.md`: Lobby, Saison und der erste Gast mit Tee kommen früh, damit
der frühe Freundestest möglich ist.

---

## Circular Dependencies

- **Gäste ↔ Versorgung und Kochen:** Der Gast braucht Essen und ein Bett, die Stationen und Gerichte
  arbeiten am Gast. Lösung: Das System Gäste legt fest, welche Bedürfnisse es gibt, und bietet eine
  Schnittstelle „Bedürfnis erfüllt“. Stationen und Gerichte rufen nur diese auf. Die Gäste kennen keine
  Station.
- **Hüttenkasse und Upgrades ↔ Ofen, Lager, Seilbahn, Strom und Licht:** Upgrades ändern nur Werte in
  den Datenobjekten (Architektur-Regel 4). Die Systeme lesen ihre Werte und kennen keine Upgrades.
- **Nachteinsatz → Sprachchat und Funkgerät:** Das ist kein Kreis, aber das Funkgerät wird nach dem
  Nachteinsatz designt. Der Nachteinsatz plant das Funkgerät als Platzhalter ein (weiche Abhängigkeit).
- **Tragen und Werfen ↔ Interaktion:** Die Interaktion besitzt den gemeinsamen Strahl und stellt das
  aktuelle Ziel bereit, das Tragen stellt den gehaltenen Gegenstand bereit. Beide lesen nur beim anderen
  (siehe `design/gdd/interaktion.md`). Im Enumeration-Table steht deshalb nur „Interaktion → Tragen“.
- **Saison-Ablauf ↔ Nachteinsatz:** Der Saison-Ablauf meldet „Nacht beginnt“ und „Nacht vorbei“ (mit
  Grund) und stellt die Schnittstelle „Einsatz beendet“ bereit; der Nachteinsatz ruft nur diese auf und
  kennt den Rest der Uhr nicht (siehe `design/gdd/saison-ablauf.md`).

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Gäste und Bedürfnisse | Technical / Design | Wegfindung über das Netz synchron halten; Engpass für vier Systeme | Früh designen; der Host berechnet die Wege, Clients sehen nur die Position; früher Freundestest nach dem ersten Gast mit Tee |
| Nachteinsatz | Design / Technical | Suche im Sturm ohne offene Welt, Sicht, Trage, Funk | Kleines Suchgebiet am Hang; Trage baut auf der Truhe auf; Funk als Platzhalter |
| Solo-Hüttenhund | Design | Truhe und Trage brauchen zwei Spieler. Wie schafft man das solo? | Gleich zu Beginn des GDD klären (zum Beispiel zieht der Hund einen Schlitten) |
| Figuren-Animation und Ragdoll | Technical | Ein Ragdoll hat viele Körperteile; alle über das Netz zu schicken ist teuer | Nur Zustand und Wurzelposition übertragen, die Körperteile rechnet jeder selbst; früh ausprobieren |
| Sprachchat und Funkgerät | Technical / Scope | Neues Paket (Vivox oder Dissonance), Kosten und Lernaufwand | Paket früh entscheiden; für Tests reicht Discord |
| Steam-Lobby und Einladen | Technical | Anderer Transport für NGO über Steam (neues Paket) | Beitreten über IP bleibt als Ausweg |
| Gesamter Vertical Slice | Scope | 20 neue Systeme, geplant sind drei Monate (KONZEPT, „Fahrplan“) | Späte Systeme (Sprachchat, Upgrades) können in die Demo-Phase rutschen; der Entwickler entscheidet |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 34 |
| Design docs started | 5 (Gegenstandsarten, Interaktion, Saison-Ablauf, Steam-Lobby; 2026-11-08; Gäste und Bedürfnisse 2026-11-09) |
| Design docs reviewed | 5 (Gegenstandsarten, Interaktion, Saison-Ablauf nach Überarbeitung Approved; Steam-Lobby im ersten Review Approved; 2026-11-08; Gäste und Bedürfnisse im ersten Review Approved, 2026-11-09) |
| Design docs approved | 5 (Gegenstandsarten, Interaktion, Saison-Ablauf, Steam-Lobby; 2026-11-08; Gäste und Bedürfnisse 2026-11-09) |
| MVP systems designed | 0/6 (alle 6 gebaut, ohne GDD auf Stufe `minimal`) |
| Vertical Slice systems designed | 5/20 (Gegenstandsarten, Interaktion, Saison-Ablauf, Steam-Lobby, Gäste und Bedürfnisse, alle Approved) |

---

## Next Steps

- [x] Review and approve this systems enumeration (2026-11-08)
- [ ] Vertical-Slice-Systeme in der Reihenfolge oben designen (`/design-system [system-name]` oder `/map-systems next`)
- [ ] Nach jedem GDD `/design-review` laufen lassen
- [ ] Gebaut wird erst, wenn der Prototyp-Prüfpunkt bestanden ist (Freundestest 2 mit 0.1.4, dann `/gate-check`)
- [ ] Die riskantesten Systeme (Gäste, Ragdoll im Netz) früh ausprobieren

> **Prüfungen durch Director-Agenten:** TD-SYSTEM-BOUNDARY, PR-SCOPE und CD-SYSTEMS sind im Lean-Modus
> nicht vorgesehen (2026-11-08). Keine dieser Prüfungen ist gelaufen.
