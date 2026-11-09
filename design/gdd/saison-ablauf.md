# Saison-Ablauf und Spielphasen

> **Status**: Approved (erneutes Design-Review 2026-11-08; siehe `design/gdd/reviews/saison-ablauf-review-log.md`)
> **Author**: Entwickler + Claude
> **Last Updated**: 2026-11-08
> **Last Verified**: 2026-11-08
> **Implements Pillar**: Teamwork unter Druck (die Uhr erzeugt den Druck), Gemütlichkeit (ruhiger Anfang)

## Summary

Saison-Ablauf und Spielphasen ist die gemeinsame Uhr jeder Runde: Vorbereitung, dann 30–40 Minuten
Spielzeit aus Nachmittag (10 min), Abend (12 min) und Nacht (8–18 min, endet mit dem Nachteinsatz), danach
Bewertung, Upgrades und die nächste Saison. Er legt die Startaufstellung, meldet Phasenwechsel und liefert
einen gemeinsamen Druckwert, aus dem Gäste, Wetter und Ereignisse ihre Eskalation ableiten. Er existiert,
damit jeder Abend mit Freunden einen ruhigen Anfang, einen Höhepunkt und ein Ende hat und alle Systeme
derselben Uhr folgen.

> **Quick reference** — Layer: `Core` · Priority: `Vertical Slice` · Key deps: `Netzwerk-Sitzung, Gegenstandsarten`

## Overview

**Saison-Ablauf und Spielphasen** gibt jeder Runde ihren Bogen. Eine Saison dauert 30 bis 40 Minuten und
läuft in drei Phasen:

- Am **Nachmittag** ist es ruhig. Die ersten Gäste kommen, man hackt Holz und holt Vorräte.
- Am **Abend** zieht der Sturm auf, und der Andrang wächst.
- In der **Nacht** fällt der Strom aus, Notfälle häufen sich, und ein Gast wird vermisst. Das ist der
  Höhepunkt mit dem Nachteinsatz.

Danach folgen die Bewertung mit Edelweiß, die Upgrades aus der Hüttenkasse und auf Wunsch die nächste
Saison. Die Spieler spüren das direkt: Das warme Nachmittagslicht wird zum kalten Abendblau und
schließlich zur dunklen Sturmnacht, und die Aufgaben werden dichter. Erst gemütlich, dann Chaos. So hat
jeder Abend mit Freunden einen Anfang, einen Höhepunkt und ein Ende.

Für die anderen Systeme ist der Saison-Ablauf die gemeinsame Uhr:

- Er sagt, welche Phase gerade läuft und wie weit sie ist.
- Er meldet jeden Phasenwechsel.
- Er legt beim Start fest, was wo liegt (die **Startaufstellung**).

Gäste, Wetter, Ereignisse, Nachteinsatz, Bewertung und HUD richten sich nach dieser Uhr, statt jeweils
eine eigene zu führen.

Die Uhr gehört dem Host (Architektur-Regel 2). Alle Spieler sehen dieselbe Phase zur selben Zeit. Wie das
technisch umgesetzt wird, legt eine spätere Architektur-Entscheidung fest (ADR, noch nicht angelegt).

## Detailed Design

### Core Rules

1. **Ablauf:** Vorbereitung → Nachmittag → Abend → Nacht → Bewertung → Upgrades → wieder Vorbereitung für
   die nächste Saison oder Ende. Die Saisons werden innerhalb einer Spielsitzung durchgezählt (Saison 1,
   2, …).
2. **Start:** In der Vorbereitung stehen alle in der Hütte, die Uhr läuft noch nicht. Jeder Spieler kann
   sich „Bereit“ melden, das sehen alle. Gestartet wird vom **Host** mit „Saison starten“, die
   Entscheidung liegt bei ihm.
3. **Startaufstellung:** Beim Start legt der Host die Hütte neu auf.
   - Gegenstände kommen an feste Plätze, aus einer Liste „Platz → Art, bei Paketen mit Ware“. Ein
     **Platz** ist ein Marker in der Szene mit einer festen Platz-Kennung (zum Beispiel
     `lager_kiste_1`); das Datenobjekt nennt nur die Kennung. Ein Datenobjekt kann keine Szenenobjekte
     verlinken, und Marker wandern mit, wenn die Hütte umgebaut wird. Jede Hütte (auch später weitere
     Hütten) hat ihre eigenen Marker. Eine Kennung ohne passenden Marker ist ein Fehler im Editor.
   - Die Spieler stehen an ihren Startpunkten.
   - Die anderen Systeme setzen sich zurück: leeres Lager, kalter Ofen, keine Gäste. Wie das jeweils
     aussieht, legen deren Dokumente fest.
   - Die Liste ist ein Datenobjekt und ersetzt den heutigen `CrateSpawner`.
4. **Uhr:** Nur der Host zählt die Zeit der Saison. Alle Spieler erhalten die aktuelle Phase, deren
   Startzeitpunkt (in Spielzeit, Formel 3a) und die Pausensumme und rechnen den Fortschritt der Phase
   selbst. So sehen alle dasselbe, ohne
   dass jedes Bild übertragen werden muss.
5. **Phasen nach Zeit:** Nachmittag und Abend haben feste Längen (Datenobjekt, Werte unter „Formulas“).
6. **Die Nacht endet mit dem Nachteinsatz:**
   - Sie dauert, bis der Nachteinsatz „beendet“ meldet, mindestens aber `night_min` (8 min). Meldet der
     Einsatz früher, läuft die Nacht bis dahin weiter. „Beendet“ heißt: Der Gast ist zurück, oder die
     Frist des Einsatzes ist abgelaufen.
   - Eine Höchstdauer der Nacht verhindert, dass eine Saison nie endet.
   - Wann in der Nacht der Einsatz beginnt, legt das Dokument zum Nachteinsatz fest.
7. **Meldungen an andere Systeme:**
   - **„Saison beginnt“** kommt beim Start, nachdem der Host die Gegenstände der letzten Saison entfernt
     hat und bevor er die Startaufstellung legt. Darauf setzen sich die Systeme zurück. Danach folgt
     „Nachmittag beginnt“.
   - Bei jedem weiteren Phasenwechsel meldet der Saison-Ablauf „Phase X beginnt“.
   - Am Ende der Nacht meldet er „Nacht vorbei“ mit einem Grund: `mission` (Einsatz beendet), `timeout`
     (Höchstdauer erreicht) oder `abort` (Host bricht ab).
   - Bricht der Host ab (Regel 12), meldet er **„Saison abgebrochen“**: nach „Nacht vorbei“ mit `abort`
     (falls es in der Nacht war) und vor „Vorbereitung beginnt“. Darauf setzt die Hüttenkasse ihren Stand
     zurück. Code-Form und Reihenfolgen: ADR-0003.
   - Jederzeit lesbar sind die aktuelle Phase, der **Fortschritt der Phase** (0 bis 1, zum Beispiel für
     Licht und Sturmstärke), der Wert **„Spielzeit läuft“** (falsch während der Pause solo) und die
     **Spielzeit `game_time`** (Formel 3a). `game_time` ist die **einzige Zeitbasis** für Spielregeln mit
     Zeit: Gespeicherte Zeitpunkte (Kochzeitpunkt eines Gerichts, Brennzeit des Ofens, Fortschritt einer
     Handlung) rechnen mit ihr, nie mit der Serverzeit. So bleibt in der Pause solo alles stehen, auch
     das Abkühlen.
8. **Bewertung und Upgrades:** Nach der Nacht berechnet das System „Bewertung“ die Edelweiß, der
   Bildschirm zeigt sie allen. Weiter zu „Upgrades“ geht es, wenn alle verbundenen Spieler „Weiter“
   gedrückt haben (wer während der Bewertung beitritt, zählt ab dann mit) oder der Host weiterklickt.
   In „Upgrades“ kaufen die Spieler mit der Hüttenkasse ein. Am Ende wählt der Host „Nächste Saison“,
   dann geht es zurück zur Vorbereitung, oder **„Beenden“: Das beendet die Sitzung** wie das Verlassen
   des Hosts. Alle landen im Hauptmenü, die Clients sehen „Der Host hat das Spiel beendet“, Kasse und
   Upgrades sind weg (Regel 9).
9. **Was bleibt:** Von Saison zu Saison bleiben nur Hüttenkasse und gekaufte Upgrades erhalten, alles
   andere stellt die Startaufstellung neu auf. Beendet man das Spiel, ist auch das weg, gespeichert wird
   erst ab der Alpha.
10. **Beitreten nur zwischen den Saisons**, also in Vorbereitung, Bewertung oder Upgrades. Während einer
    laufenden Saison lehnt der Host einen Beitritt mit einer Meldung ab: „Gerade läuft eine Saison. Bitte
    nach der Bewertung erneut beitreten.“ Das geht über denselben Weg wie die Versionsprüfung.
11. **Pause nur solo:** Spielt der Host allein, hält das Menü (Esc) die Spielzeit an: Uhr, Physik und alle
    zeitgesteuerten Systeme. Sobald ein Client da ist, läuft die Zeit im Menü weiter. Die Pause gilt ab
    dem Moment, in dem beides zutrifft (Menü offen und Host allein), also auch, wenn der letzte Client
    geht, während das Menü schon offen ist. In der Pause steht `game_time`; die anderen Systeme rechnen
    damit und erfahren den Zustand zusätzlich über „Spielzeit läuft“. Physik und Animation hält
    `Time.timeScale = 0` an (ADR-0003, `docs/architecture/adr-0003-saison-uhr-und-meldungen.md`).
12. **Abbrechen:** Der Host kann eine laufende Saison über das Menü abbrechen. Dann gibt es keine
    Bewertung („Nacht vorbei“ mit Grund `abort`, falls es in der Nacht war), die Hüttenkasse fällt auf
    den Stand beim Druck auf „Saison starten“ zurück, und es geht zurück zur Vorbereitung. Die
    abgebrochene Saison zählt nicht: Der Zähler zählt nur Saisons mit Bewertung, ein Neustart behält
    also seine Nummer. So bringt Abbrechen keinen Vorteil.

### States and Transitions

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Vorbereitung | Uhr steht, Beitreten möglich, Spieler melden „Bereit“ | → Nachmittag (Host: „Saison starten“, Startaufstellung wird gelegt) |
| Nachmittag | Ruhig, Uhr läuft, kein Beitritt | → Abend (Länge erreicht), → Vorbereitung (Host bricht ab) |
| Abend | Sturm zieht auf, Andrang | → Nacht (Länge erreicht), → Vorbereitung (Abbruch) |
| Nacht | Stromausfall, Notfälle, Nachteinsatz | → Bewertung (Nachteinsatz beendet oder Höchstdauer erreicht), → Vorbereitung (Abbruch) |
| Bewertung | Edelweiß werden gezeigt, Beitreten möglich | → Upgrades (alle verbundenen Spieler haben „Weiter“ gedrückt, oder der Host klickt weiter) |
| Upgrades | Einkaufen mit der Hüttenkasse, Beitreten möglich | → Vorbereitung (Host: „Nächste Saison“), → Ende (Host: „Beenden“: Sitzung endet, alle ins Hauptmenü) |

„Pausiert“ ist kein eigener Zustand. Solo hält das Menü die Spielzeit im jeweiligen Zustand an.

### Interactions with Other Systems

| System | Was es vom Saison-Ablauf bekommt oder ihm liefert |
| --- | --- |
| Netzwerk-Sitzung | Beitritt nur zwischen den Saisons; die Ablehnung kommt mit Grund (wie die Versionsprüfung) |
| Gegenstandsarten | Startaufstellung: welche Art an welchem Platz, bei Paketen mit Ware |
| Gäste und Bedürfnisse | Phase und Fortschritt der Phase. Wann wie viele Gäste ankommen, legt das Gäste-Dokument fest. |
| Wetter und Eskalation | Phase und Fortschritt der Phase. Daraus macht das Wetter-Dokument die Sturmstärke. |
| Ereignisse (mit Strom und Licht) | Phase. Was die Nacht auslöst (Stromausfall), legt das Ereignis-Dokument fest. |
| Nachteinsatz | „Nacht beginnt“. Meldet zurück, wenn er beendet ist, und beendet damit die Nacht. Bekommt „Nacht vorbei“ mit Grund (bei `timeout` und `abort` bricht er ab). |
| Bewertung | „Nacht vorbei“ mit Grund. Rechnet nur bei `mission` und `timeout`, liefert die Edelweiß für den Bildschirm. |
| Hüttenkasse und Upgrades | Phase „Upgrades“. Kasse und Upgrades bleiben über die Saisons erhalten. |
| Holzofen, Lager und andere | Setzen sich bei der Startaufstellung zurück. |
| HUD | Uhr, Phase und Fortschritt der Phase |
| Alle Systeme mit Zeitregeln (Gegenstandsarten: Abkühlen; Interaktion: `dt` des Fortschritts; Holzofen: Brennzeit) | Spielzeit `game_time` (Formel 3a) als einzige Zeitbasis; steht in der Pause solo |
| Menüs | Vorbereitung („Bereit“, „Saison starten“), Bewertungs- und Upgrade-Bildschirm, Abbrechen, Pause solo |

Die Schnittstelle gehört dem Saison-Ablauf. Was eine Phase **bewirkt**, legt das jeweilige System fest.

> `systems-designer`, `gameplay-programmer` und `engine-programmer` wurden nicht befragt (Lean-Modus). Vor
> dem Bau selbst prüfen.

## Formulas

Werte vorgeschlagen von `systems-designer` (2026-11-08), gegen `interaktion.md` und die Ofenwerte
nachgerechnet, vom Entwickler freigegeben.

**Phasenlängen (Startwerte im Datenobjekt):**

| Phase | Wert | Anteil (typisch) | Begründung |
|---|---|---|---|
| Nachmittag | 600 s (10 min) | 30 % | Zeit für Vorarbeit, die sich später lohnt (etwa 13 Scheite bis zum Abend, Vorräte, erste Gäste). Kürzer wäre der Anfang nicht mehr gemütlich. |
| Abend | 720 s (12 min) | 36 % | Längste feste Phase; die Gruppe lernt den Andrang, bevor die Nacht ihn verdichtet. |
| Nacht, Mindestdauer | 480 s (8 min) | – | Sichert 30 min (600 + 720 + 480 = 1800) und Zeit für Stromausfall und Notfälle. |
| Nacht, Höchstdauer | 1080 s (18 min) | – | Obergrenze 40 min (600 + 720 + 1080 = 2400); nur Sicherheitsnetz. |
| Nacht, typisch | etwa 600–780 s | etwa 34 % | Ergibt sich aus dem Nachteinsatz; Saison dann 32–35 min. |

Die Bereiche in den Variablentabellen unten sind **Empfehlungen für das Spielprofil**. Harte Grenzen setzt
nur `phase_data_valid` (Längen > 0, `night_max` > `night_min`); im Inspector also `[Min]`, nicht
`[Range]`. So bleibt ein **Testprofil** mit 60/60/60/120 s (Nachmittag / Abend / `night_min` /
`night_max`) möglich, mit dem eine Saison zum Testen nur 3–4 Minuten dauert.

Die Nacht ist der Höhepunkt, weil dort am meisten passiert, nicht weil sie am längsten dauert. Die
**30–40 Minuten zählen nur die Spielzeit** (Nachmittag, Abend, Nacht). Vorbereitung (etwa 1 min),
Bewertung (0,5–1 min) und Upgrades (1–3 min) haben keine Uhr und kommen dazu; ein Durchgang dauert also
33–45 min, ein Abend mit Freunden fasst 3–4 Saisons.

### 1. Länge der Nacht

Die Formel `night_length` ist definiert als:

`night_length = clamp(t_mission_done, night_min, night_max)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| t_mission_done | float | 0–∞ s | calculated | Sekunden seit Nachtbeginn, bis der Nachteinsatz „beendet“ meldet; ∞, solange keine Meldung kam |
| night_min | float | 240–900 s (Start 480) | data file | Mindestdauer der Nacht |
| night_max | float | 600–1500 s (Start 1080) | data file | Höchstdauer der Nacht |
| night_length | float | night_min–night_max | calculated | Tatsächliche Länge der Nacht |

**Regel für den Host (jeden Schritt):** Die Nacht endet, wenn `phase_elapsed >= night_max` gilt oder wenn
der Einsatz beendet ist und `phase_elapsed >= night_min` gilt. Meldet der Einsatz früher, läuft die Nacht
bis `night_min` weiter (Ereignisse und Sturm gehen weiter, das HUD zeigt „Morgengrauen in …“). Der Grund
in „Nacht vorbei“ ist `mission` (Einsatz beendet) oder `timeout` (`night_max` erreicht). Ein „Einsatz
beendet“ nach der Nacht wird ignoriert und protokolliert.

**Output Range:** Mit den Startwerten 480–1080 s.

**Example:** Meldung nach 690 s ergibt 690 s. Meldung nach 300 s ergibt 480 s, die Nacht läuft noch
180 s weiter. Ohne Meldung ergibt sich 1080 s.

### 2. Spielzeit der Saison

Die Formel `season_length` ist definiert als:

`season_length = afternoon_length + evening_length + night_length`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| afternoon_length | float | 300–900 s (Start 600) | data file | Länge des Nachmittags |
| evening_length | float | 480–1080 s (Start 720) | data file | Länge des Abends |
| night_length | float | night_min–night_max | calculated | Aus Formel 1 |
| season_length | float | 1800–2400 s mit Startwerten | calculated | Spielzeit ohne Pause und ohne Zwischenphasen |

**Output Range:** Mit den Startwerten genau 1800–2400 s (30–40 min). Die Datenbereiche erlauben
1020–3480 s; dann gibt Formel 7 einen Hinweis. Passt zu `season_length` in `interaktion.md`.

**Example:** 600 + 720 + 690 = 2010 s (33:30 min). Gegenproben: `solo_hold_share` = 320 / 2010 = 0,16
(Ziel ≤ 0,25 eingehalten); Ofen: 2010 / 45 ≈ 45 Scheite ≈ 11 Klötze (Interaktion schätzt 12).

### 3a. Spielzeit

Die Formel `game_time` ist definiert als:

`game_time = (is_paused ? pause_time : server_time) - paused_total`

Beim Weiterspielen nach einer Pause rechnet der Host `paused_total += server_time - pause_time`.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| server_time | double | ≥ 0 s | calculated | Serverzeit; beim Client seine Schätzung der Serverzeit |
| is_paused | bool | true/false | calculated | Menü offen und Host allein (Regel 11) |
| pause_time | double | ≥ 0 s | calculated | Serverzeit, ab der `is_paused` wahr wird (Menü offen und Host allein, egal in welcher Reihenfolge) |
| paused_total | double | ≥ 0 s | calculated | Summe aller abgeschlossenen Pausen der Sitzung; der Host führt sie, sie wird mit Phase und Phasenstart synchronisiert |
| game_time | double | ≥ 0 s | calculated | Spielzeit: läuft wie die Serverzeit, steht in der Pause |

**Output Range:** Wächst wie die Serverzeit und steht in der Pause still; springt nie und läuft nie
rückwärts. `game_time` ist die **einzige Zeitbasis** für Spielregeln mit Zeit (Regel 7): Kochzeitpunkt
und Abkühlen (`gegenstandsarten.md`), Fortschritt und `dt` von Handlungen (`interaktion.md`), Brennzeit
des Ofens. Da Pause nur ohne Clients geht und während der Saison niemand beitritt (Regeln 10 und 11),
sehen Clients `paused_total` nur zwischen den Saisons ändern.

**Example:** Ohne Pause ist `game_time` = Serverzeit. Solo, Pause ab Serverzeit 1500,0: `game_time`
steht bei 1500,0, auch bei Serverzeit 1600,0. Weiter bei 1620,0: `paused_total` = 120,0; bei Serverzeit
1630,0 ist `game_time` = 1510,0. Ein Tee, gekocht bei `game_time` 1450, ist bei 1510 genau 60 s alt,
nicht 180 s.

### 3. Verstrichene Zeit der Phase

Die Formel `phase_elapsed` ist definiert als:

`phase_elapsed = game_time - phase_start_time`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| game_time | double | ≥ 0 s | calculated | Aus Formel 3a |
| phase_start_time | double | ≥ 0 s | calculated | Beginn der Phase in Spielzeit; der Host setzt ihn, alle bekommen ihn mit der Phase |
| phase_elapsed | double | ≥ 0 s (beim Client kurz leicht negativ möglich) | calculated | Verstrichene Spielzeit der Phase |

**Regeln:**
- **Pause:** Weil `game_time` in der Pause steht, steht auch `phase_elapsed`. Es muss nichts verschoben
  werden.
- **Wechsel nach Zeit:** Der neue Beginn ist `alter phase_start_time + Länge der Phase`, nicht die Zeit
  des Bildes, in dem der Host den Wechsel bemerkt. So sammelt sich kein Versatz an.
- **Ende der Nacht durch den Einsatz:** Die Bewertung beginnt zur Spielzeit der Meldung (frühestens bei
  `night_min`).
- Zeiten werden als `double` gespeichert.

**Output Range:** Nach oben offen (die Vorbereitung hat keine Länge). Beim Client kurz leicht negativ
möglich, wenn die neue Phase vor seiner geschätzten Serverzeit ankommt; Formel 4 klemmt das ab.

**Example:** Der Abend beginnt bei Spielzeit 1234,5 s; bei `game_time` 1594,5 s ist `phase_elapsed` =
360 s. Solo, Pause ab Serverzeit 1500,0 (Spielzeit 1500,0): die Phase steht bei 265,5 s. Weiter bei
Serverzeit 1620,0: bei Serverzeit 1630,0 ist `game_time` 1510,0 und `phase_elapsed` = 275,5 s.

### 4. Fortschritt der Phase

Die Formel `phase_progress` ist definiert als:

`phase_progress = (reference_length > 0) ? clamp(phase_elapsed / reference_length, 0, 1) : 0`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| phase_elapsed | double | s | calculated | Aus Formel 3 |
| reference_length | float | 0 oder 240–1080 s | calculated | Nachmittag: `afternoon_length`; Abend: `evening_length`; Nacht: `night_min`; Vorbereitung, Bewertung, Upgrades: 0 |
| phase_progress | float | 0–1 | calculated | Fortschritt der Phase |

**Output Range:** [0, 1], geklemmt. In der Nacht ist der Wert ab `night_min` gleich 1 und bleibt bis zum
Ende bei 1, der Höhepunkt hält also an. Wer in der Nacht absolute Zeit braucht (etwa für den Start des
Einsatzes), liest `phase_elapsed`. Phasen ohne Länge liefern 0, ohne Division durch 0.

**Example:** Abend bei 360 s: 360 / 720 = 0,5. Ein Client 50 ms hinterher: 359,95 / 720 = 0,49993. Nacht
nach 600 s: 600 / 480 = 1,25, geklemmt auf 1.

### 5. Gemeinsamer Druck

Wetter, Gäste, Ereignisse und später die Musik lesen diesen einen Wert, statt jeweils eine eigene Kurve
zu bauen. Wie schnell es eskaliert, wird damit an einer Stelle eingestellt; jedes System legt nur fest,
was ein bestimmter Druck bei ihm bewirkt. Er ersetzt die Phase nicht (der Stromausfall hängt weiter an
„Nacht“).

Die Formel `season_pressure` ist definiert als:

`season_pressure = (phase_index in {0,1,2}) ? p[phase_index] + (p[phase_index+1] - p[phase_index]) * phase_progress : 0`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| phase_index | int | 0–2 | calculated | 0 = Nachmittag, 1 = Abend, 2 = Nacht |
| p[0..3] | float | 0–1, aufsteigend (Start 0; 0,2; 0,6; 1,0) | data file | Druck am Beginn jeder Phase und am Ende der Nacht |
| phase_progress | float | 0–1 | calculated | Aus Formel 4 |
| season_pressure | float | 0–1 | calculated | Druck über die ganze Saison |

**Output Range:** [0, 1], solange die Stützwerte gültig sind (Formel 6). An den Phasengrenzen springt der
Wert nicht. Außerhalb der drei Phasen ist er 0. Jeder Spieler rechnet ihn selbst aus, ohne zusätzlichen
Netzverkehr.

**Example:** Nachmittag bei 300 s: 0 + 0,2 × 0,5 = 0,1. Abend bei 360 s: 0,2 + 0,4 × 0,5 = 0,4. Nacht bei
240 s: 0,6 + 0,4 × 0,5 = 0,8; ab 480 s: 1,0. Anstieg pro Minute 0,02, dann 0,033, dann 0,05 – erst
gemütlich, dann Chaos.

### 6. Prüfung der Phasendaten (Editor, Fehler)

Die Formel `phase_data_valid` ist definiert als:

`phase_data_valid = afternoon_length > 0 AND evening_length > 0 AND night_min > 0 AND night_max > night_min AND 0 <= p[0] <= p[1] <= p[2] <= p[3] <= 1`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| afternoon_length, evening_length, night_min, night_max | float | s | data file | Wie in den Formeln 1 und 2 |
| p[0..3] | float | 0–1 | data file | Stützwerte aus Formel 5 |
| phase_data_valid | bool | true/false | calculated | false = Fehler im Editor |

**Output Range:** bool. Die drei Phasen sind feste Felder im Datenobjekt, eine Reihenfolge muss nicht
geprüft werden.

**Example:** Startwerte: true. `night_max` = 400 bei `night_min` = 480: false, Meldung „Höchstdauer der
Nacht (400 s) muss größer sein als die Mindestdauer (480 s)“. p = [0; 0,6; 0,2; 1]: false.

### 7. Prüfung der Gesamtlänge (Editor, Hinweis)

Die Formel `season_length_in_target` ist definiert als:

`season_length_in_target = (afternoon_length + evening_length + night_min >= season_target_min) AND (afternoon_length + evening_length + night_max <= season_target_max)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| season_target_min | float | 1800 s | constant | 30 min, Entscheidung des Entwicklers |
| season_target_max | float | 2400 s | constant | 40 min |
| season_length_in_target | bool | true/false | calculated | false = Hinweis, nie ein Fehler |

**Output Range:** bool. Bewusst nur ein Hinweis, damit kurze Testprofile (zum Beispiel 60/60/60/120 s)
möglich bleiben. Zusätzlicher Hinweis zum Spannungsbogen: Ist
`(p[1]-p[0])/afternoon_length <= (p[2]-p[1])/evening_length <= (p[3]-p[2])/night_min` verletzt, meldet
der Editor „Der Druck steigt nicht in jeder Phase schneller“ (Startwerte: 0,00033 ≤ 0,00056 ≤ 0,00083).

**Example:** 600 + 720 + 480 = 1800 und 600 + 720 + 1080 = 2400: true. Abend 840 s: 2520 > 2400,
Hinweis „Saison kann bis 42:00 min dauern (Ziel 30–40)“.

## Edge Cases

**Verbindung**

- **Wenn der Host das Spiel verlässt:** Die Sitzung endet für alle, es gibt keinen Host-Wechsel. Die
  Clients sehen „Der Host hat das Spiel beendet“ und landen im Hauptmenü. Die Saison ist weg (gespeichert
  wird erst ab der Alpha).
- **Wenn ein Client mitten in der Saison die Verbindung verliert:** Die Saison läuft weiter. Seine
  Gegenstände fallen zu Boden, seine Ausrüstung erscheint an seiner letzten Position (Regeln aus
  `interaktion.md`). Wieder beitreten kann er erst nach der Nacht (Bewertung, Upgrades, Vorbereitung),
  wie Regel 10 es vorsieht.
- **Wenn alle Clients gehen:** Der Host spielt solo weiter, ab dann ist Pause möglich.
- **Wenn jemand in Bewertung oder Upgrades beitritt:** Er sieht den laufenden Bildschirm und steht in der
  nächsten Saison an einem Startpunkt.
- **Wenn ein Beitritt gerade läuft, während der Host „Saison starten“ drückt:** Der Knopf ist gesperrt,
  bis der neue Spieler erschienen ist. Die Saison beginnt mit allen, die da sind.
- **Wenn die Version falsch ist und zugleich eine Saison läuft:** Die Meldung zur Version kommt zuerst.

**Uhr und Phasen**

- **Wenn die geschätzte Serverzeit eines Clients knapp zurückliegt:** `phase_elapsed` ist kurz leicht
  negativ, `phase_progress` klemmt auf 0. Niemand sieht einen Unterschied.
- **Wenn der Nachteinsatz „beendet“ vor der Nacht oder zweimal meldet:** Es zählt nur die erste Meldung in
  der Nacht; frühere werden ignoriert und protokolliert.
- **Wenn der Nachteinsatz nie „beendet“ meldet** (Fehler): `night_max` (18 min) beendet die Nacht mit dem
  Grund `timeout`; der Einsatz bricht ab.
- **Wenn „Einsatz beendet“ nach der Nacht ankommt:** Es wird ignoriert und protokolliert.
- **Wenn der Host das Menü öffnet, solange ein Client da ist, und der Client dann geht:** Ab dem Gehen des
  Clients gilt die Pause (`pause_time` = dieser Zeitpunkt), die Uhr springt nicht.
- **Wenn ein Spieler beim Phasenwechsel gerade eine Handlung hält:** Er hält weiter. Ein Phasenwechsel
  unterbricht keine Handlung; nur der Start einer neuen Saison setzt alles zurück.
- **Pause:** Sie gilt nur in den drei Phasen mit Uhr (Nachmittag, Abend, Nacht). In Vorbereitung,
  Bewertung und Upgrades öffnet das Menü ohne Pause.

**Start, Abbrechen, Zurücksetzen**

- **Beim Start einer Saison:** Der Host entfernt zuerst alle Gegenstände der letzten Saison, auch was
  Spieler in der Hand halten oder am Körper tragen. Erst dann legt er die Startaufstellung. So liegt nie
  etwas doppelt.
- **Wenn die Startaufstellung eine unbekannte Art oder Ware nennt:** Dieser Platz bleibt leer, der Host
  protokolliert einen Fehler, die Saison startet trotzdem. Die Prüfung im Editor fängt das vorher ab
  (Regeln aus `gegenstandsarten.md`).
- **Wenn der Host in der Nacht während des Nachteinsatzes abbricht:** Der Einsatz wird abgebrochen (das
  Aufräumen legt dessen Dokument fest), es gibt keine Bewertung, es geht zurück zur Vorbereitung.
- **Wenn noch jemand einkauft, während der Host „Nächste Saison“ drückt:** Was mit dem Einkauf passiert,
  legt das Dokument zu Hüttenkasse und Upgrades fest. Der Saison-Ablauf wartet nicht.
- **Wenn ein Client die Bewertung nicht bestätigt:** Der Host kann jederzeit weiterklicken.

## Dependencies

**Hängt ab von:**

| System | Art | Schnittstelle |
| --- | --- | --- |
| Netzwerk-Sitzung | hart | Serverzeit, NetworkVariables für Phase und Phasenstart, Freigabe der Verbindung (Beitritt nur zwischen den Saisons) |
| Gegenstandsarten | hart | Startaufstellung: Arten und Waren an festen Plätzen (`design/gdd/gegenstandsarten.md`) |

**Wird gebraucht von:**

| System | Art | Schnittstelle | Stand |
| --- | --- | --- | --- |
| Gäste und Bedürfnisse | hart | Phase, Fortschritt der Phase, `season_pressure` | `design/gdd/gaeste.md` |
| Wetter und Eskalation | hart | Phase, `season_pressure` (statt einer eigenen Kurve) | kein Dokument |
| Ereignisse (mit Strom und Licht) | hart | Phase („Nacht“ löst den Stromausfall aus), `season_pressure` | kein Dokument |
| Nachteinsatz | hart | „Nacht beginnt“, `phase_elapsed`; ruft „Einsatz beendet“ auf; bekommt „Nacht vorbei“ mit Grund (`mission`, `timeout`, `abort`) | kein Dokument |
| Bewertung | hart | „Nacht vorbei“ mit Grund; rechnet nur bei `mission` und `timeout`; ihr Ergebnis zeigen die Menüs | kein Dokument |
| HUD | hart | Uhr, Phase, Fortschritt, „Morgengrauen in …“ | kein Dokument |
| Menüs | hart | Vorbereitung („Bereit“, „Saison starten“), Bildschirme für Bewertung und Upgrades, Abbrechen, Pause solo | kein Dokument |
| Holzofen und Wärme, Lager und Vorräte | weich | Setzen sich bei „Saison beginnt“ zurück; der Ofen rechnet seine Brennzeit mit `game_time` (steht in der Pause) | Ofen gebaut, Lager kein Dokument |
| Gegenstandsarten | weich | Kochzeitpunkt und Abkühlen (`is_warm`, `warm_fraction`) rechnen mit `game_time` | `design/gdd/gegenstandsarten.md` |
| Hüttenkasse und Upgrades | weich | Phase „Upgrades“. Die Kasse setzt sich **nicht** zurück; beim Abbruch fällt sie auf den Stand beim Saisonstart zurück (sie merkt ihn sich bei „Saison beginnt“ und setzt ihn bei „Saison abgebrochen“ zurück). | kein Dokument |
| Interaktion | weich | `dt` des Fortschritts ist vergangene `game_time` (in der Pause 0); rechnet mit `season_length` in der Faustregel `solo_hold_share` | `design/gdd/interaktion.md` |
| Musik, Tutorial (Alpha), weitere Hütten (Full Vision) | weich | Phase, `season_pressure` | kein Dokument |

**Zurücksetzen:** Bei „Saison beginnt“ setzt sich jedes System selbst zurück. Der Saison-Ablauf kennt
deren Inhalte nicht, er entfernt nur die Gegenstände und legt die Startaufstellung.

**Kreis Saison ↔ Nachteinsatz:** Der Saison-Ablauf meldet „Nacht beginnt“ und stellt die Schnittstelle
„Einsatz beendet“ bereit. Der Nachteinsatz ruft nur diese auf und kennt den Rest der Uhr nicht; der
Saison-Ablauf kennt die Abläufe des Einsatzes nicht.

**Beidseitig:** Bis auf die Interaktion hat noch keins dieser Systeme ein Design-Dokument. Jedes muss
beim Schreiben „Saison-Ablauf“ unter seinen Abhängigkeiten nennen.

## Tuning Knobs

Bei Stufe `standard` nur empfohlen; ergänzt, weil das Review es bei den letzten Dokumenten vermisst hat.
Alle Werte liegen im Datenobjekt **Saison-Profil**; davon gibt es zwei: das Spielprofil (Startwerte) und
das Testprofil.

| Stellschraube | Spielprofil | Testprofil | Empfohlener Bereich (Spielprofil) | Wirkung |
| --- | --- | --- | --- | --- |
| `afternoon_length` | 600 s | 60 s | 300–900 s | Länge des ruhigen Anfangs. Länger = gemütlicher, aber ab der 2. Saison eventuell zäh. |
| `evening_length` | 720 s | 60 s | 480–1080 s | Länge des Andrangs vor der Nacht. |
| `night_min` | 480 s | 60 s | 240–900 s | Mindestdauer der Nacht; Bezugslänge ihres Fortschritts. Höher = Leerlauf nach frühem Einsatzende. |
| `night_max` | 1080 s | 120 s | 600–1500 s | Sicherheitsnetz; muss größer als `night_min` sein (sonst Fehler). Muss zu Start und Frist des Nachteinsatzes passen. |
| `p[0..3]` | 0; 0,2; 0,6; 1,0 | wie Spielprofil | 0–1, aufsteigend | Form des Spannungsbogens (`season_pressure`). |
| `season_target_min` / `max` | 1800 / 2400 s | – | fest (Entscheidung des Entwicklers) | Nur für den Hinweis zur Gesamtlänge. |

Wechselwirkungen: `afternoon_length`, `evening_length` und `night_min`/`night_max` bestimmen zusammen die
Gesamtlänge (Hinweis bei Verlassen von 30–40 min) und über `p` die Steigung des Drucks je Phase (Hinweis,
wenn sie nicht von Phase zu Phase wächst). Gäste, Wetter und Ereignisse lesen `season_pressure`; ändert man
`p`, ändert sich die Eskalation in allen dreien zugleich.

## Visual/Audio Requirements

- **Tageslicht je Phase:** am Nachmittag warmes Licht, am Abend kaltes Blau (wie das Abendlicht heute), in
  der Nacht dunkel. Der Übergang folgt dem Fortschritt der Phase, ohne Sprung. Gegen Ende der Nacht hellt
  der Himmel zum Morgengrauen auf. Das Tageslicht gehört zu diesem Dokument, die Lampen in der Hütte
  gehören zu „Strom und Licht“.
- **Phasenwechsel hört man:** am Nachmittag Kuhglocken (KONZEPT), zum Abend ein Windstoß, zur Nacht ein
  Donner oder Sturmheulen. Lauter wird der Wind über das Wetter.
- Den Bewertungsbildschirm mit den Edelweiß gestalten die Dokumente zu Bewertung und Menüs.

`art-director` und `audio-director` nicht befragt (Kategorie Core, Lean-Modus).

## Game Feel

- Erst gemütlich, dann Chaos: Der Druck steigt immer schneller. Der Nachmittag soll sich nach Ruhe
  anfühlen, nicht nach Warten.
- Ein Wechsel **nach Zeit** kommt nie überraschend: 30 s vorher zeigt das HUD eine Vorwarnung, zum
  Beispiel „Es wird Abend“ (Nachmittag → Abend, Abend → Nacht, Ende der Nacht durch `night_max`). Endet
  die Nacht durch den Einsatz, kommt das Ende ohne Vorwarnung; dort zeigt das HUD ab `night_min`
  nichts mehr an, weil die Rettung selbst der Abschluss ist.
- Die Nacht ist der Höhepunkt. Gegen Leerlauf nach einem frühen Einsatzende hilft „Morgengrauen in …“.
- Zwischen den Saisons soll es zügig gehen: Die Bewertung ist kurz, und die nächste Saison lässt sich in
  unter einer Minute starten.

## UI Requirements

- **HUD:** Phasenname, Tageszeit oder Balken, die Vorwarnung 30 s vor einem Wechsel nach Zeit,
  „Morgengrauen in …“
- **Vorbereitung:** Saisonnummer, Bereit-Liste, „Bereit“ für alle, „Saison starten“ nur für den Host
- **Bewertung:** Edelweiß und „Weiter“. Die Upgrades zeigen die Kasse, dazu „Nächste Saison“ und
  „Beenden“ nur für den Host.
- **Menü:** Pause (nur solo) und „Saison abbrechen“ (nur Host, mit Rückfrage)
- **Meldung beim Beitritt:** „Gerade läuft eine Saison. Bitte nach der Bewertung erneut beitreten.“
- **Debug-Anzeige** (siehe Abnahmekriterien)

> **UX-Hinweis:** HUD (Phase, Vorwarnung, Morgengrauen), Vorbereitung, Bewertungs- und Upgrade-Bildschirm
> und das Menü brauchen vor den Epics ein UX-Dokument mit `/ux-design` (`design/ux/hud.md` und die
> Menü-Bildschirme).

## Cross-References

| Verweis | Quelle | Genutzt in |
|---|---|---|
| Arten, Waren, Prüfung der Startaufstellung | `design/gdd/gegenstandsarten.md` | Regel 3, Grenzfälle |
| `season_length` 1800–2400 s; Handlungen laufen über Phasenwechsel weiter | `design/gdd/interaktion.md` | Formel 2, Grenzfall, Kriterium 35 |
| Versionsprüfung beim Beitreten (`response.Reason`) | `PlayerSpawnApproval` | Regel 10, Kriterium 26 |
| Heutiges Auslegen der Kisten, wird ersetzt | `CrateSpawner` | Regel 3, Kriterium 7 |
| NetworkVariables, die nur der Host ändert; Zurücksetzen | `Stove` | Regel 4, Kriterium 34 |
| Kaltes Abendlicht | `HutLookBuilder` | Visual/Audio |
| Phasen und Saisonlänge | `docs/KONZEPT.md`, `production/milestones/vertical-slice.md` | Overview, Formulas |
| Architektur-Regel 2 | CLAUDE.md | Regel 4 |

## Acceptance Criteria

Vorschlag von `qa-lead` (2026-11-08), an die Entscheidungen des Entwicklers angepasst und freigegeben.

**Damit die Tests verlässlich bleiben:** Alle Formeln sind reine Funktionen (zum Beispiel eine statische
Klasse `SeasonMath`). Der Schritt des Hosts bekommt die Serverzeit übergeben (`Step(double serverTime)`),
ebenso „Einsatz beendet“ (`MissionFinished(double serverTime)`); keine echte Uhr, kein Zufall.
Nachteinsatz, Bewertung, Kasse, Upgrades und die zurückzusetzenden Systeme sind in Tests Stubs. PlayMode
wie in `interaktion.md` (`Physics.simulationMode = SimulationMode.Script`, `Physics.Simulate(0.02f)`).
Schrittfolgen laufen in Schritten von 0,37 s und landen nie genau auf einer Phasengrenze; Grenzen werden
an den reinen Funktionen geprüft. Zeiten sind `double`, Toleranz ±0,01.

**Testprofil:** ein zweites Saison-Profil mit 60/60/60/120 s, p wie im Spielprofil; eine Saison dauert
3–4 min. Solange es den Nachteinsatz noch nicht gibt: ein Debug-Befehl „Einsatz beendet“ (nur Editor und
Development Build) und ein Bewertungs-Stub, der immer 3 Edelweiß liefert.

**Messweg (gehört in den Bau):** Log-Zeilen
`[Season] t=<ServerTime> <start|phase-begin|phase-received|tick|pause|resume|mission-done|mission-ignored|night-over|abort|join-reject|start-blocked|spawn-check> gt=<game_time> season= phase= start= elapsed= progress= pressure= client= reason=`
(Zahlen mit Punkt, drei Nachkommastellen; eine `tick`-Zeile bei jeder vollen 10-s-Marke der
Serverzeit) und eine Debug-Anzeige mit Saison-Nr., Phase, ServerTime, `phase_elapsed`, `phase_progress`,
`season_pressure`, Bereit-Liste und Anzahl der Gegenstände. Freundestests mit Development Build und
Testprofil, nicht mit zwei Fenstern.

Testarten: **[EditMode]** unter `Assets/Tests/EditMode/`, **[PlayMode]**, **[Manuell]** (solo im
Editor), **[Freundestest]** (zu zweit übers Netz).

### A. Ablauf und Start (Regeln 1, 2, 3, 8, 9)

1. **[EditMode]** **GIVEN** Spielprofil, Stubs und 2 Client-Stubs **WHEN** der Host startet, der Stub in
   der Nacht bei 690,0 s „beendet“ meldet, der Host in der Bewertung „Weiter“ wählt (kein Client hat
   bestätigt) und in Upgrades „Nächste Saison“ **THEN** laufen die Zustände genau Vorbereitung →
   Nachmittag → Abend → Nacht → Bewertung → Upgrades → Vorbereitung, und der Saisonzähler steht auf 2.
2. **[EditMode]** **GIVEN** Bewertung mit 2 verbundenen Spielern **WHEN** beide „Weiter“ drücken **THEN**
   ist im nächsten Schritt Upgrades. **WHEN** während der Bewertung ein dritter beitritt **THEN** braucht
   es auch sein „Weiter“ (oder den Klick des Hosts).
3. **[EditMode]** **GIVEN** Vorbereitung, alle 4 Spieler „Bereit“ **WHEN** 1000 s Serverzeit vergehen
   **THEN** bleibt es Vorbereitung mit `phase_progress` = 0 und `season_pressure` = 0. **GIVEN** 0 von 3
   Clients bereit **WHEN** der Host „Saison starten“ wählt **THEN** beginnt der Nachmittag.
4. **[PlayMode]** **GIVEN** eine Test-Startaufstellung mit 3 Plätzen (Kiste, Paket mit einer Ware, Truhe)
   und 5 Gegenstände der Vorsaison, davon einer in der Hand und eine Ausrüstung am Körper des
   Host-Spielers **WHEN** der Host startet **THEN** gibt es genau 3 Gegenstände, jeder mit der Art seines
   Platzes auf dessen Position ±0,01 m vor dem ersten Physikschritt, das Paket enthält die Ware, Hand und
   Körper sind leer.
5. **[PlayMode]** **GIVEN** dieselbe Aufstellung, der Host-Spieler steht 10 m neben seinem Startpunkt
   **WHEN** man dreimal startet und abbricht **THEN** sind nach jedem Start genau 3 Gegenstände da, nie
   doppelt, und der Host-Spieler steht auf Startpunkt 1 ±0,01 m.
6. **[PlayMode]** **GIVEN** eine Aufstellung, deren Platz 2 eine unbekannte Art nennt **WHEN** der Host
   startet **THEN** bleibt Platz 2 leer, Platz 1 und 3 sind belegt, das Log hat genau 1 Fehler
   (`LogAssert.Expect`), und der Zustand ist Nachmittag.
7. **[EditMode + Codeprüfung]** **GIVEN** das echte Startaufstellungs-Asset **WHEN** die Prüfung aus
   `gegenstandsarten.md` läuft **THEN** 0 Fehler. In `Assets/_Project/Scripts/` gibt es 0 Treffer für
   `CrateSpawner`, auch in den Editor-Buildern.
8. **[PlayMode]** **GIVEN** Saison 1 in Upgrades, Kassen-Stub mit 50, ein gekauftes Upgrade **WHEN**
   „Nächste Saison“ und Start **THEN** läuft Saison 2, Kasse 50, Upgrade vorhanden, Gegenstände wie in
   Kriterium 4.

### B. Uhr, Nacht und Formeln (Regeln 4, 5, 6; `night_length`, `season_length`, `phase_elapsed`, `phase_progress`)

9. **[EditMode]** `night_length` mit 480/1080 (Zeitpunkt der Meldung → Ergebnis): 690 → 690; 300 → 480;
   480 → 480; 1080 → 1080; 5000 → 1080; keine Meldung (+∞) → 1080.
10. **[EditMode]** **GIVEN** Spielprofil, Nachmittag beginnt bei A = 100,0, Schritte 0,37 s **WHEN** der
    Stub bei Nacht + 690,0 meldet / bei Nacht + 300,0 meldet / nie meldet **THEN** beginnt die Bewertung
    bei A + 2010,0 / A + 1800,0 / A + 2400,0, und „Nacht vorbei“ kommt mit Grund `mission` / `mission` /
    `timeout`. Bei der Meldung nach 300 s bleibt es Nacht bis zum ersten Schritt ab Nacht + 480, das Log
    zeigt `mission-done` zum Zeitpunkt der Meldung.
11. **[EditMode]** **GIVEN** Abend **WHEN** „Einsatz beendet“ kommt **THEN** bleibt es Abend, 1 Zeile
    `mission-ignored reason=not-night`. **GIVEN** Nacht **WHEN** Meldungen bei 300,0 und 310,0 s kommen
    **THEN** zählt die erste, die zweite ergibt `mission-ignored`, Bewertung bei Nacht + 480,0. **GIVEN**
    Bewertung **WHEN** „Einsatz beendet“ kommt **THEN** `mission-ignored reason=not-night`, keine
    Änderung.
12. **[EditMode]** `game_time` und `phase_elapsed`: ohne Pause ist `game_time` = Serverzeit; Beginn 1234,5,
    `game_time` 1594,5 → `phase_elapsed` 360,0. Pause ab Serverzeit 1500,0 → bei Serverzeit 1600,0 ist
    `game_time` 1500,0 und `phase_elapsed` 265,5. Weiter bei 1620,0 → `paused_total` 120,0; bei
    Serverzeit 1630,0 ist `game_time` 1510,0 und `phase_elapsed` 275,5. Ein Tee mit Kochzeitpunkt
    `game_time` 1450,0 ist dann 60,0 s alt (nicht 180,0).
13. **[EditMode]** Wechsel ohne Versatz: **GIVEN** Testprofil, Nachmittag beginnt bei 100,0, Schritte
    0,37 s, keine Meldung **THEN** beginnen Abend, Nacht und Bewertung bei 160,0 / 220,0 / 340,0, obwohl
    der Host jeden Wechsel erst im Schritt danach bemerkt. Mit dem Spielprofil: Abend bei A + 600,0,
    Nacht bei A + 1320,0.
14. **[EditMode]** **GIVEN** Testprofil, Nachmittag ab 100,0, ein Schritt bei 105,0 **WHEN** der nächste
    Schritt wegen eines Hängers erst bei 230,0 kommt **THEN** ist es Nacht mit Beginn 220,0,
    `phase_elapsed` = 10,0, und es gab 2 Meldungen in der Reihenfolge Abend, Nacht.
15. **[EditMode]** `phase_progress`: (180; 720) → 0,25; Nacht bei 240 s mit `night_min` 480 → 0,5;
    (600; 480) → 1; (−0,05; 720) → 0; Länge 0 (Vorbereitung) → 0, ohne Ausnahme.
16. **[Manuell]** **GIVEN** Testprofil, Debug-Anzeige **WHEN** der Host solo startet und nichts tut
    **THEN** wechselt die Anzeige nach 60 s auf Abend, nach 120 s auf Nacht, nach 240 s auf Bewertung
    (Stoppuhr ±1 s); die `start=`-Werte der `phase-begin`-Zeilen liegen genau 60,000 / 60,000 / 120,000
    auseinander; in der Nacht steht der Fortschritt ab 60 s auf 1,00.
17. **[Manuell]** **GIVEN** Testprofil, Nacht bei 20 s **WHEN** jemand den Debug-Befehl „Einsatz beendet“
    auslöst **THEN** bleibt es Nacht, das HUD zeigt „Morgengrauen in 0:40“ (±1 s), die Bewertung beginnt
    bei 60 s Nacht.
18. **[Codeprüfung + Manuell]** Phasenlängen und p stehen nur im Saison-Profil: In den Saison-Skripten
    gibt es 0 Treffer für 600, 720, 480 oder 1080 als feste Zahlen. Wird im Inspector das Testprofil
    zugewiesen, ändern sich die Längen ohne Code-Änderung.

### C. Meldungen und Druck (Regel 7, `season_pressure`)

19. **[EditMode]** **GIVEN** ein Stub-Abonnent **WHEN** eine ganze Saison wie in Kriterium 1 läuft
    **THEN** bekommt er genau 1 „Saison beginnt“ (nach dem Entfernen der alten Gegenstände, vor dem
    Legen der Startaufstellung), genau 6 Meldungen „Phase X beginnt“ (Nachmittag, Abend, Nacht,
    Bewertung, Upgrades, Vorbereitung) und genau 1 „Nacht vorbei“. Phase, `phase_progress`, `game_time`
    und „Spielzeit läuft“ sind in jedem Schritt lesbar.
20. **[EditMode]** Mit p = [0; 0,2; 0,6; 1,0]: Nachmittag bei 0,5 → 0,1; Abend bei 0,5 → 0,4; Nacht bei
    0,5 → 0,8; Nacht bei 1 → 1,0; Vorbereitung, Bewertung, Upgrades → 0.
21. **[EditMode]** Kein Sprung an den Grenzen: Nachmittag bei 1 = Abend bei 0 = 0,2; Abend bei 1 = Nacht
    bei 0 = 0,6. Über die Saison aus Kriterium 10 ändert sich der Druck von Schritt zu Schritt um
    höchstens 0,01, bis er mit der Bewertung auf 0 fällt.

### D. Datenprüfung (`phase_data_valid`, `season_length_in_target`)

22. **[EditMode]** `phase_data_valid`: Spielprofil → true; Testprofil → true; `night_max` 400 bei
    `night_min` 480 → false mit genau „Höchstdauer der Nacht (400 s) muss größer sein als die
    Mindestdauer (480 s)“; `night_max` = `night_min` = 480 → false; `afternoon_length` 0 → false;
    p = [0; 0,6; 0,2; 1] → false; p = [0; 0,2; 0,2; 1] → true; p[0] = −0,01 → false; p[3] = 1,01 → false.
23. **[EditMode]** `season_length_in_target`: Spielprofil → true; Abend 840 → false mit „Saison kann bis
    42:00 min dauern (Ziel 30–40)“; Nachmittag 400 → false mit „Saison kann schon nach 26:40 min enden
    (Ziel 30–40)“; Testprofil → false (nur Hinweis).
24. **[EditMode]** Spannungsbogen: Spielprofil → kein Hinweis; Testprofil → kein Hinweis
    (0,0033 ≤ 0,0067 ≤ 0,0067); p = [0; 0,5; 0,6; 1,0] → Hinweis „Der Druck steigt nicht in jeder Phase
    schneller“, `phase_data_valid` bleibt true.
25. **[Manuell]** **GIVEN** das Saison-Profil im Inspector **WHEN** `night_max` auf 400 gesetzt wird
    **THEN** steht die Meldung aus Kriterium 22 als Fehler (rot) in der Konsole. **WHEN** das Testprofil
    zugewiesen wird **THEN** erscheinen nur Hinweise (gelb), und Play startet eine Saison mit
    60/60/60/120 s (kein `[Range]` blockiert die Werte).

### E. Beitreten, Pause, Abbrechen, Beenden (Regeln 8, 10, 11, 12)

26. **[EditMode + Codeprüfung]** Die Beitrittsentscheidung ist eine reine Funktion (Version, Zustand,
    freier Platz): Vorbereitung, Bewertung, Upgrades → angenommen; Nachmittag, Abend, Nacht → abgelehnt
    mit genau „Gerade läuft eine Saison. Bitte nach der Bewertung erneut beitreten.“; falsche Version in
    der Nacht → Versionsmeldung. Reihenfolge der Prüfungen: Version → Saison → voll. *Codeprüfung:* Die
    Ablehnung läuft im selben `ConnectionApprovalCallback` wie die Versionsprüfung (`response.Reason`).
27. **[EditMode]** **GIVEN** ein Beitritt ist angenommen, der Spieler aber noch nicht erschienen **WHEN**
    der Host „Saison starten“ wählt **THEN** startet nichts, 1 Zeile `start-blocked reason=join-pending`;
    nach dem Erscheinen startet die Saison, und die Startaufstellung zählt den neuen Spieler mit.
28. **[Manuell]** **GIVEN** solo, Testprofil, Nachmittag bei etwa 30 s, eine Kiste gerade geworfen, der Ofen
    brennt **WHEN** das Esc-Menü 20 s offen ist **THEN** sind `elapsed` in `pause` und `resume` gleich
    (±0,01), die Kiste hängt in der Luft und fliegt danach weiter, die Brennzeit des Ofens ändert sich
    nicht, und der Abend beginnt bei Nachmittag-Beginn + 60 + (t_resume − t_pause) ±0,01.
29. **[Manuell]** **GIVEN** solo **WHEN** der Host Esc in Vorbereitung, Bewertung oder Upgrades öffnet
    **THEN** gibt es keine `pause`-Zeile, und eine in der Vorbereitung geworfene Kiste fliegt weiter.
30. **[PlayMode]** **GIVEN** Nachmittag, Abend oder Nacht mit laufendem Einsatz-Stub, Kasse beim Start 50,
    während der Saison +20 Trinkgeld (Kasse 70) **WHEN** der Host abbricht **THEN** ist der Zustand
    Vorbereitung, 1 Zeile `abort phase=…`, der Bewertungs-Stub hat 0 Aufrufe, die Kasse steht auf **50**,
    der Saisonzähler ist unverändert, und in der Nacht bekommt der Einsatz-Stub „Nacht vorbei“ mit Grund
    `abort`.
31. **[EditMode]** **GIVEN** Nacht mit Einsatz-Stub, der nie meldet **WHEN** `night_max` erreicht ist
    **THEN** bekommen Einsatz- und Bewertungs-Stub „Nacht vorbei“ mit Grund `timeout`, und die Bewertung
    rechnet.

### F. Zurücksetzen (Regel 3, Dependencies)

32. **[PlayMode]** **GIVEN** 2 Stub-Systeme hören auf „Saison beginnt“ **WHEN** Start, 3 Phasenwechsel,
    Abbruch, neuer Start **THEN** bekommt jedes genau 2 Aufrufe, keinen bei den Phasenwechseln.
33. **[Codeprüfung]** Die Saison-Skripte in `Scripts/Core/` verweisen auf keine Klasse aus `Hut/`,
    `Guests/` oder `Weather/`. Der Nachteinsatz erreicht die Uhr nur über „Einsatz beendet“.
34. **[Manuell]** **GIVEN** der Ofen wurde in Saison 1 angeheizt **WHEN** der Host abbricht und neu
    startet **THEN** ist der Ofen aus, ohne Holz, die Wärme auf ihrem Startwert.
35. **[Manuell]** **GIVEN** Testprofil **WHEN** man bei etwa 58 s Nachmittag mit dem Holzhacken beginnt und
    die Taste hält **THEN** hackt man nach dem Wechsel in den Abend weiter; das `[Interact]`-Log zeigt
    beim Wechsel kein `pause` und kein `remove`, `done` kommt nach der üblichen Dauer.

### G. Netz (Freundestest mit Testprofil)

36. **[Freundestest]** **GIVEN** Vorbereitung **WHEN** der Freund „Bereit“ drückt **THEN** zeigen beide
    Bereit-Listen ihn innerhalb von 1 s. Beim Freund gibt es 0 Knöpfe für Starten, Abbrechen, Nächste
    Saison und Beenden. Der Host kann starten, auch wenn der Freund nicht bereit ist.
37. **[Freundestest]** **GIVEN** eine ganze Saison **WHEN** man die Logs beider Rechner vergleicht
    **THEN** hat jede `phase-received`-Zeile des Freundes denselben `start=`-Wert wie die passende
    `phase-begin`-Zeile des Hosts, und beim Freund gilt |gt − start| ≤ 0,5 s (`start` und `gt` sind
    beide Spielzeit; auch nach einer früheren Solo-Pause in derselben Sitzung).
38. **[Freundestest]** In den `tick`-Zeilen zur selben 10-s-Marke unterscheiden sich `progress` und
    `pressure` zwischen Host und Freund um höchstens 0,01.
39. **[Freundestest + Codeprüfung]** Das Log des Freundes hat pro Saison genau 6 `phase-received`-Zeilen.
    Synchronisiert werden nur Phase, `phase_start_time`, `paused_total`, Saisonzähler und Bereit-Liste
    (in der Bewertung trägt sie „Weiter“, ADR-0003); für `elapsed`,
    `progress` und `pressure` gibt es keine NetworkVariable und kein RPC.
40. **[Freundestest]** **GIVEN** Saison 2 **WHEN** der Host startet **THEN** meldet die `spawn-check`-Zeile
    1 s nach dem Start für beide Spieler höchstens 0,1 m Abstand zum Startpunkt, und die Debug-Anzeige des
    Freundes zeigt so viele Gegenstände, wie die Aufstellung Plätze hat.
41. **[Freundestest]** **WHEN** der Freund im Abend beitreten will **THEN** sieht er genau den Text aus
    Kriterium 26, der Host loggt `join-reject reason=season-running`, der Zustand ändert sich nicht.
    **WHEN** er in der Bewertung beitritt **THEN** sieht er den Bewertungsbildschirm mit denselben
    Edelweiß wie der Host und steht in der nächsten Saison auf einem Startpunkt.
42. **[Freundestest]** **GIVEN** Abend, der Freund trägt eine Kiste **WHEN** er das Spiel hart beendet
    **THEN** läuft die Saison weiter: kein `abort`, die Nacht beginnt bei Abend-Beginn + 60,000, die Kiste
    liegt beim Host am Boden. Ein neuer Beitritt in der Nacht wird abgelehnt, in der Bewertung angenommen.
43. **[Freundestest]** **GIVEN** Abend, Freund verbunden **WHEN** der Host das Esc-Menü 20 s offen hat
    **THEN** gibt es keine `pause`-Zeile, die Nacht beginnt bei Abend-Beginn + 60,000. **WHEN** der Freund
    geht, während das Menü des Hosts offen ist **THEN** beginnt ab diesem Moment `pause`, und der nächste
    Phasenbeginn verschiebt sich um die Pausendauer (±0,01), ohne Sprung der Uhr.
44. **[Freundestest]** **GIVEN** Bewertung, beide sehen 3 Edelweiß **WHEN** der Freund nicht bestätigt und
    der Host „Weiter“ wählt **THEN** sind beide in Upgrades. **WHEN** der Freund noch einkauft und der Host
    „Nächste Saison“ wählt **THEN** sind beide in der Vorbereitung, beide Anzeigen zeigen Saison 2.
45. **[Freundestest]** **WHEN** der Host mitten in der Nacht das Spiel verlässt **THEN** sieht der Freund
    innerhalb von 2 s „Der Host hat das Spiel beendet“ und landet im Hauptmenü. **WHEN** der Host in
    Upgrades „Beenden“ wählt **THEN** passiert dasselbe, und ein neuer Host-Start beginnt mit Saison 1,
    Kasse auf ihrem Startwert und 0 Upgrades.

**Annahmen des `qa-lead`, beim Bau zu bestätigen:** Log-Format und 10-s-`tick`, Inhalt der
Debug-Anzeige, Netz-Toleranzen (0,5 s, 1 s, 2 s, 0,1 m), Reihenfolge der Beitrittsprüfungen, Startpunkt =
Platz aus `PlayerSpawnApproval`, mehrere Phasenwechsel in einem Schritt (Kriterium 14), Debug-Befehl
„Einsatz beendet“ und Bewertungs-Stub.

## Open Questions

| # | Frage | Wer klärt | Bis wann |
|---|---|---|---|
| 1 | Start und Frist des Nachteinsatzes müssen zu `night_max` passen: Start + Frist + 60 s ≤ 1080 s | Dokument zum Nachteinsatz | bei dessen Design |
| 2 | Gibt es nach einem frühen Einsatzende Leerlauf? | Playtest | erster Freundestest mit Saison |
| 3 | Wird der Nachmittag ab der 2. Saison zu lang? 480–720 s testen | Playtest | erster Freundestest mit Saison |
| 4 | Wie Pause und Uhr umgesetzt werden (`Time.timeScale` oder eigener Wert, Abgleich der Serverzeit) | `/architecture-decision` | vor dem Bau |
| 5 | Gäste, Wetter und Ereignisse müssen `season_pressure` nutzen und dürfen keine eigenen Kurven bauen. **Gäste erledigt 2026-11-08:** Ankunft und Geduld nutzen `season_pressure` (`design/gdd/gaeste.md` Formeln 1 und 2). | deren Dokumente | bei deren Design |
| 6 | Wiederbeitritt mitten in der Saison nach einem Verbindungsabbruch, später vielleicht über die Steam-Identität | nach der Steam-Lobby | nach dem Plan-Schritt „Steam-Lobby“ |
| 7 | Kasse und Upgrades speichern | System „Spielstand speichern“ | Alpha |
