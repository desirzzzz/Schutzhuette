# Skizze: Hütte und Umgebung (Vertical Slice)

> **Status**: Skizze, vom Entwickler am 2026-11-08 für gut befunden
> **Zweck**: Grundlage für das spätere Dokument „Hütten-Layout“ (vor dem Bau der Seilbahn, Plan-Schritt
> „Lager und Materialseilbahn“ in `production/milestones/vertical-slice.md`)
> **Gebaut**: ja, als Graubox ohne Funktion (2026-11-10, auf Wunsch des Entwicklers vor dem Gate-Check).
> Editor-Skript `HutExtensionBuilder` (Menü „Schutzhütte/Karte erweitern (Skizze)“), Maße in
> `docs/GRAUBOX.md`, Abschnitt „Erweiterung nach der Skizze“. Die Funktionen kommen mit den Plan-Schritten.
> **Berg**: Seit 2026-11-10 steht die Hütte auf einem Gelände (`HutTerrainBuilder`, `docs/GRAUBOX.md`,
> Abschnitt „Berg“). Abweichung von der Skizze: Die Talstation liegt tiefer und weiter vorn am Hang
> (z −20 … −16, Boden −6,5); das Seil läuft über eine Stütze an der Platzkante und ist rund 35 m lang.

![Skizze von oben](huette-skizze.svg)

Draufsicht, oben = bergauf, unten = Tal. Maßstab 15 px pro Meter. Grau gibt es schon in der Graubox
(`docs/GRAUBOX.md`), orange ist neu im Vertical Slice, gestrichelt ist Gelände.

## Orte

Koordinaten in Metern wie in der Graubox (x nach rechts, z nach hinten = bergauf). Neue Orte sind nur
ungefähr gesetzt; die echten Maße legt das Dokument „Hütten-Layout“ fest.

| Ort | Neu? | x | z | Notiz |
|---|---|---|---|---|
| Vorraum | nein | −6 … −1 | 12 … 15,5 | Haken für die Ausrüstung (Stirnlampe, Seil, Funkgerät) |
| Lager | nein | −6 … −1 | 15,5 … 18,5 | Neue Seitentür in der Westwand zur Bergstation |
| Küche | nein | −6 … −1 | 18,5 … 22 | Herd an der Rückwand |
| Stube | nein | −1 … 6 | 12 … 22 | Ofen bei (2,2 / 20,9), Tische wie heute, Hundekorb neben dem Ofen |
| Schlafraum | ja | −1 … 6 | 22 … 25,5 | 4 Betten, Tür aus der Stube |
| Generator-Schuppen | ja | 7,5 … 13 | 17 … 21 | Draußen neben der Stube |
| Seilbahn-Bergstation | ja | −14,5 … −8 | 15 … 19 | Kurbel, direkt neben dem Lager |
| Seilbahn-Talstation | ja | −14,5 … −8 | −8 … −4 (gebaut: −20 … −16, tiefer am Hang) | Hier wird beladen; Seil etwa 20 m (gebaut: rund 35 m über eine Stütze) |
| Terrasse | nein | −7 … 7 | 10 … 12 | |
| Hang | nein | −7 … 3 | 2 … 10 | Steigt von 0 auf 1,5 m |
| Startplätze | nein | −3, −1, 1, 3 | 0 | |
| Holzplatz | nein | 4,7 … 6,3 | 2,9 … 4,3 | Lage aus der Szene (Spawnpunkte `WoodPile`) |
| Hackstock | ja | 7,2 … 7,8 | 2 … 2,6 | Rechts neben dem Holz; vor dem Holz steht die Truhe |
| Truhe | nein | 4,1 … 5,9 | 1,2 … 1,8 | Lage aus der Szene (Spawnpunkt `HeavySpawnPoints`) |
| Suchgebiet Nachteinsatz | ja | 4 … 16 | −11 … −1 | Abseits vom Weg zur Talstation; gebaut am Hang (0 … −3,6 m) |

## Warum so

- **Schlafraum hinter der Stube:** Die Familie geht zum Schlafen durch die Stube und erlebt Kälte, Lärm und
  Chaos mit. Betten machen passiert mitten im Trubel (Säule Gemütlichkeit und Teamwork unter Druck).
- **Bergstation am Lager:** Was mit der Seilbahn ankommt, ist nach wenigen Schritten verstaut. Unten belädt
  einer, oben kurbelt einer; beide sind getrennt und müssen sich absprechen (`docs/KONZEPT.md`,
  Materialseilbahn).
- **Generator draußen:** Fällt in der Nacht der Strom aus, muss jemand in den Sturm hinaus.
- **Hackstock am Holzplatz:** Holz liegen, hacken, über die Terrasse zum Ofen tragen, etwa 15 m.
- **Suchgebiet abseits der Talstation:** Nachteinsatz und Seilbahn kreuzen sich nicht, die Gruppe teilt sich
  wirklich auf.
- **Herd → Stube:** Vom Herd zum hintersten Tisch sind es etwa 13 m. Das bleibt unter der vorläufigen
  Grenze von etwa 15 m fürs Servieren; den genauen Wert legt das GDD zur Versorgung fest.

## Verworfene Varianten

- **Matratzenlager unterm Dach mit Treppe:** sehr alpin, aber Tragen über eine Treppe ist für den Vertical
  Slice zu teuer. Bleibt als Idee für später.
- **Seilbahn auf der Ostseite:** dort stehen schon Holzplatz und Truhe; es würde draußen zu eng.

## Was die endgültigen Maße festlegt

Die Skizze ist absichtlich grob. Abstände und Größen hängen von noch offenen GDDs ab: Gäste und
Bedürfnisse (Betten), Versorgung und Kochen (Wege zum Servieren), Holz hacken, Lager und Seilbahn,
Wetter und Ereignisse (Generator) und Nachteinsatz (Größe des Suchgebiets). Wenn diese stehen, entsteht
daraus das Dokument „Hütten-Layout“ (`level-designer`, ggf. `/team-level`).
