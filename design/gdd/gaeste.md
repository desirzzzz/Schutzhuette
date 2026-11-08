# Gäste und Bedürfnisse

> **Status**: Approved (erneutes Design-Review 2026-11-09 nach Einarbeitung aller Empfehlungen, siehe `design/gdd/reviews/gaeste-review-log.md`)
> **Author**: Entwickler + Claude
> **Last Updated**: 2026-11-09
> **Last Verified**: 2026-11-08
> **Implements Pillar**: Gemütlichkeit (Gäste versorgen, warme Stube), Teamwork unter Druck (Andrang am Abend)

## Summary

Gäste und Bedürfnisse steuert die Gäste der Hütte. Wanderer, Familien und Bergsteiger kommen je nach Phase
und `season_pressure`, gehen selbst an ihren Platz und zeigen nacheinander ein bis zwei Wünsche: Tee,
Speckbrot, Kaiserschmarrn, Betten oder Erste Hilfe. Die Spieler erfüllen sie durch Servieren oder
Versorgung. Wartezeit, Kälte, Lärm und Chaos bestimmen die Zufriedenheit, und daraus entstehen Trinkgeld
und Bewertung. Das System gibt dem Tragen einen Zweck und bringt den Andrang am Abend.

> **Quick reference** — Layer: `Feature` · Priority: `Vertical Slice` · Key deps: `Netzwerk-Sitzung, Saison-Ablauf und Spielphasen, Holzofen und Wärme, Gegenstandsarten, Interaktion`

## Overview

**Gäste und Bedürfnisse** bringt Leben in die Hütte. Im Lauf der Saison steigen Gäste den Hang herauf,
jeder mit einem klaren Wunsch: Der Wanderer hat Hunger und Durst, die Familie braucht Betten, der
Bergsteiger kommt verletzt an. Die Gäste gehen selbst zu einem freien Tisch oder Bett und warten dort, die
Spieler bringen ihnen, was sie brauchen. Wie zufrieden ein Gast ist, hängt davon ab, wie lange er warten
muss und wie es ihm in der Hütte geht: Kälte, Lärm und Chaos in der Stube drücken die Laune. Zufriedene
Gäste geben Trinkgeld, und aus ihrer Zufriedenheit berechnet die Bewertung am Ende die Edelweiß. Am Abend
kommen mehr Gäste, als man bequem versorgen kann. So wird aus der gemütlichen Stube Teamwork unter Druck.

Für die anderen Systeme legt dieses Dokument fest, welche Bedürfnisse es gibt, und bietet die
Schnittstelle „Bedürfnis erfüllt“ an. Versorgung (Betten, Erste Hilfe) und das Servieren rufen sie auf,
die Gäste selbst kennen keine Station. Das System liefert:

- Zufriedenheit und Trinkgeld an Bewertung und Hüttenkasse
- dem Nachteinsatz den Gast, der in der Nacht fehlt
- dem HUD die Wünsche der Gäste

Wie viele Gäste wann kommen, richtet sich nach der Phase und nach `season_pressure`. Alles entscheidet der
Host. Er berechnet auch die Wege, die Clients sehen nur, wo die Gäste gerade sind.

Ohne dieses System wäre das Tragen Selbstzweck. Erst der wartende Gast macht aus der Kiste einen Tee und
aus dem Holz eine warme Stube.

## Detailed Design

### Core Rules

1. **Gästetypen** sind Datenobjekte. Jeder Typ hat:
   - Kennung und Anzeigename (`wanderer`, `familie`, `bergsteiger`)
   - **Figurenzahl** 1 oder 2. Die Familie besteht aus zwei Figuren (Elternteil und Kind), die zusammen
     **ein** Gast sind: eine Zufriedenheit, ein Symbol, gemeinsame Bedürfnisse. Beide Figuren sind dieselbe
     Station: Servieren und Erste Hilfe gehen an jeder der beiden. Für den Lärmabstand zählt die nähere
     Figur.
   - eine **Bedürfniskette** mit 1–2 Bedürfnissen, die nacheinander kommen
   - Zahlenwerte wie Geduld und Trinkgeld (stehen unter „Formulas“)
2. **Bedürfnisse** sind ebenfalls Datenobjekte. Jedes hat eine Kennung (immer mit `beduerfnis_` davor), ein
   Symbol, eine Menge und eine Art der Erfüllung:

   | Bedürfnis | Erfüllt durch | Menge |
   |---|---|---|
   | `beduerfnis_tee` | Servieren eines Gerichts mit diesem Bedürfnis (Tee) | 1 |
   | `beduerfnis_speckbrot` | Servieren (Speckbrot) | 1 |
   | `beduerfnis_kaiserschmarrn` | Servieren (Kaiserschmarrn); die Familie teilt sich eine Portion | 1 |
   | `beduerfnis_bett` | Versorgung meldet jedes gemachte Bett, das für den Gast reserviert ist | 1 je Figur |
   | `beduerfnis_erste_hilfe` | Versorgung meldet „Erste Hilfe“ am Gast | 1 |

   Die Eigenschaft „Gericht“ aus den Gegenstandsarten nennt genau eine dieser Kennungen.
3. **Ketten (Startwerte im Datenobjekt):**
   - Wanderer: erst Tee, dann Speckbrot
   - Familie: erst Kaiserschmarrn, dann Betten
   - Bergsteiger: erst Erste Hilfe, dann Tee
4. **Plätze:**
   - Sitzplätze an den Tischen und die Betten sind Marker in der Szene, jeder mit fester Platz-Kennung (wie
     bei der Startaufstellung im Saison-Ablauf).
   - Ein Gast **reserviert bei der Ankunft alles, was er brauchen wird**: einen Sitzplatz (die Familie zwei
     an einem Tisch) und, wenn seine Kette Betten enthält, so viele freie Betten, wie er Figuren hat.
   - Die Reservierung gilt, bis der Gast geht oder die Saison endet.
5. **Ankunft:**
   - Gäste kommen nur am Nachmittag, am Abend und in der Nacht.
   - Wie dicht sie kommen, folgt aus `season_pressure` und der Spielerzahl (Formel).
   - **Typ:** Ist ein Gast fällig, zieht der Host den Typ zufällig nach Gewichten aus der Liste der aktuellen
     Phase. Typen, für die keine passenden Plätze frei sind, fallen aus der Ziehung.
   - **Kein Platz frei:** Passt kein Typ, wartet diese eine Ankunft, bis ein Platz frei wird. Weitere
     fällige Ankünfte stauen sich nicht dahinter auf, sie verfallen.
   - **Nachteinsatz:** Auch während des Nachteinsatzes kommen Gäste an, der Rest hält die Hütte.
   - **Startlisten** (Gewichte im Datenobjekt):

     | Phase | Wanderer | Familie | Bergsteiger |
     |---|---|---|---|
     | Nachmittag | 3 | 1 | – |
     | Abend | 2 | 2 | 1 |
     | Nacht | – | – | 1 (Notfall im Sturm) |

   - **Weg zum Platz:** Ein Gast erscheint an einem Ankunftspunkt unten am Hang (Marker). Von dort geht er
     selbst hinauf, durch den Vorraum zu seinem Platz.
6. **Ein Bedürfnis von Anfang bis Ende:**
   - **Anzeigen:** Am Platz zeigt der Gast sein aktuelles Bedürfnis (Symbol über dem Kopf, Geduldsring).
     Ab jetzt läuft seine Geduld, gerechnet in `game_time`.
   - **Erfüllt:** Der Gast **genießt** kurz (trinkt, isst, wird verbunden). Danach zeigt er das nächste
     Bedürfnis.
   - **Geduld abgelaufen:** Der Gast **mault**. Man hört ihn murren, und seine Zufriedenheit fällt auf 0.
     Das Bedürfnis bleibt offen. Wird es später doch erfüllt, steigt die Zufriedenheit wieder etwas
     (Formel).
   - **Nur das aktuelle Bedürfnis zählt:** Ein anderes Gericht nimmt der Gast nicht an. Der Hinweis am
     Fadenkreuz nennt den Grund, zum Beispiel „Möchte einen Tee“.
   - **Servieren:**
     - Das ist die Handlung „Servieren“ aus der Interaktion: das Gericht in der Hand, sofort.
     - Der Host prüft die Bedürfnis-Kennung des Gerichts, entfernt das Gericht und meldet das Bedürfnis als
       erfüllt.
     - Ob das Gericht warm war (`is_warm`), geht in die Zufriedenheit ein.
   - **Nur am Platz:** Ein Gast ist nur an seinem Platz eine Station. Solange er unterwegs ist, kann man ihm
     nichts geben.
   - **Vorgemachte Betten:** Ein Bett, das schon gemacht ist, wenn das Bedürfnis „Bett“ dran ist, zählt
     sofort. Vorarbeit lohnt sich also.
7. **Bleiben und Gehen:**
   - **Fertig am Nachmittag:** Ist das letzte Bedürfnis erfüllt und es ist noch Nachmittag, geht der Gast
     zurück zum Ankunftspunkt und verschwindet. Sein Platz wird frei.
   - **Fertig ab dem Abend:** Ab dem Abend geht niemand mehr (Sturm). Wer fertig ist, bleibt sitzen und
     plaudert bis zum Saisonende.
   - **Wer ein Bett hatte,** geht danach schlafen und bleibt bis zum Saisonende, egal in welcher Phase.
8. **Stimmung:** Solange ein Gast in der Hütte ist, drücken drei Dinge auf seine Zufriedenheit (Werte unter
   „Formulas“):
   - **Kälte:**
     - Die Wärme des Ofens liegt unter einer Schwelle.
     - Der Ofen wärmt die ganze Hütte, auch den Schlafraum.
   - **Lärm:**
     - Ein lauter Aufprall oder zerbrechendes Geschirr im Umkreis kostet einen kleinen Abzug je Ereignis.
     - Danach gilt für diesen Gast eine kurze Sperrzeit. So kostet ein umfallender Stapel nicht alles auf
       einmal.
   - **Chaos:** Ein fliegender oder fallender Gegenstand trifft den Gast. Das kostet einen großen Abzug, und
     der Gast ruft „Aua“.
   - Draußen auf dem Weg wirkt nichts davon.
9. **Wege und Körper:**
   - Der Host berechnet alle Wege der Gäste.
   - **Hindernisse:** Ruhende Gegenstände sind Hindernisse, um die die Gäste herumgehen.
   - **Blockiert:**
     - Gibt es keinen Weg zum Ziel, bleibt der Gast stehen und mault, bis der Weg frei ist.
     - Solange er blockiert ist, sinkt seine Zufriedenheit langsam (Formel).
   - **Spieler:** Die Gäste weichen Spielern aus. Spieler können Gäste nicht schieben, nur fliegende oder
     fallende Gegenstände treffen sie (Regel 8).
   - **Tempo:** Gäste gehen mit `walk_speed` (1,4 m/s, Bereich 0,8–2,5), deutlich langsamer als Spieler
     (4 m/s). Vom Ankunftspunkt bis in die Stube sind es etwa 20 m, also etwa 15 s.
10. **Vermisster Gast (Nacht):**
    - **Wann:** Den Zeitpunkt legt das Dokument zum Nachteinsatz fest.
    - **Wer:**
      - Der Host wählt einen anwesenden, wachen Gast: zuerst einen Wanderer, sonst einen anderen Gast, der
        keine Familie ist. Kinder gehen nicht verloren.
      - „Anwesend und wach“ heißt: am eigenen Platz, im Zustand Wartet, Mault, Genießt oder Bleibt. Wer
        unterwegs ist, schläft, vermisst oder weg ist, kommt nicht in Frage. Passen mehrere Gäste, nimmt der
        Host den, der am längsten da ist.
      - Ist keiner da, entsteht ein eigener vermisster Wanderer, der nie in der Hütte war.
    - **Was passiert:**
      - Der gewählte Gast verschwindet aus der Hütte, und sein Platz bleibt reserviert.
      - Ein offenes Bedürfnis verfällt.
    - **Zurück:** Bringt der Nachteinsatz ihn zurück, setzt der Host ihn an seinen Platz. Was er danach
      braucht, legt das Dokument zum Nachteinsatz fest. Vorläufig: Erste Hilfe, dann Tee.
11. **Spielerzahl:** Sie steht für eine ganze Saison fest, weil man nur zwischen den Saisons beitreten kann.
    Ein Faktor je Spielerzahl aus dem Datenobjekt verdünnt die Ankünfte (Formel).
12. **Trinkgeld:**
    - Bei jedem erfüllten Bedürfnis gibt der Gast sofort Trinkgeld an die Hüttenkasse. Wie viel, hängt von
      seiner Zufriedenheit in diesem Moment ab (Formel).
    - Ein maulender Gast gibt nichts.
13. **Saisonwechsel:**
    - **„Saison beginnt“:** Der Host entfernt alle Gäste, löscht alle Reservierungen und setzt die Ankunft
      zurück.
    - **„Nacht vorbei“:**
      - Ab da kommen keine Gäste mehr.
      - Die Zufriedenheit aller Gäste wird eingefroren und an die Bewertung übergeben. Wer vorher gegangen
        ist, zählt mit dem Wert, den er beim Gehen hatte.
    - **Eingefroren außerhalb der Spielphasen:** Die Gäste-Logik läuft nur am Nachmittag, am Abend und in
      der Nacht. In jeder anderen Phase (Bewertung, Upgrades, Vorbereitung, also auch nach einem Abbruch)
      ist sie eingefroren:
      - Gäste sind keine Stationen.
      - Uhren, Zufriedenheit und Figuren stehen.
      - Es gibt keine Ankünfte und kein Trinkgeld.
14. **Was Spieler nicht können:**
    - Gäste tragen (Ausnahme: der Vermisste auf der Trage, das regelt der Nachteinsatz)
    - Gäste wegschicken oder Plätze zuweisen
    - mehrere Bedürfnisse auf einmal erfüllen
15. **Netz:**
    - Alles entscheidet der Host (Architektur-Regel 2), die Wegfindung läuft nur bei ihm.
    - Die Clients erhalten für jede Figur Position und Blickrichtung, außerdem Zustand, aktuelles Bedürfnis,
      Geduldsstand und eine grobe Stimmung (zufrieden, neutral, mault) für Gesicht und Geräusche.
    - Einmalige Ereignisse schickt der Host an alle: „Aua“, Murren und Trinkgeld (Münzen, Klimpern, „+6“).
    - Wie das technisch läuft, klärt eine spätere Architektur-Entscheidung (ADR: Wegfindung im Netz).

> `game-designer` und `ai-programmer` für diesen Abschnitt nicht befragt (Lean-Modus). Beim Review genau
> prüfen.

### States and Transitions

| Zustand | Bedeutung | Station? | Weiter zu |
|---|---|---|---|
| Kommt an | geht vom Ankunftspunkt zum Platz | nein | Wartet (am Platz), Blockiert |
| Blockiert | kein Weg zum Ziel, mault | nein | zurück in den Gehzustand, sobald der Weg frei ist |
| Wartet | Bedürfnis offen, Geduld läuft | ja | Genießt, Mault, Geht zum Bett (Betten komplett), Vermisst |
| Mault | Bedürfnis offen, Geduld abgelaufen | ja | Genießt, Geht zum Bett, Vermisst |
| Genießt | kurz nach der Erfüllung | nein | Wartet (nächstes Bedürfnis), Geht (Nachmittag), Bleibt (ab Abend), Vermisst |
| Bleibt | alles erfüllt, plaudert bis zum Saisonende | nein | Vermisst |
| Geht zum Bett | unterwegs zum reservierten Bett | nein | Schläft, Blockiert |
| Schläft | im Bett bis zum Saisonende | nein | – |
| Geht | verlässt die Hütte (nur am Nachmittag) | nein | Weg, Blockiert |
| Weg | verschwunden, Platz frei, Zufriedenheit festgehalten | – | – |
| Vermisst | draußen, der Nachteinsatz führt ihn | nein | Wartet (zurück, mit neuem Bedürfnis); sonst bleibt er vermisst bis zum Saisonende |

Bei „Saison beginnt“ verschwinden alle Gäste, egal in welchem Zustand. Außerhalb von Nachmittag, Abend und
Nacht wechselt kein Gast den Zustand (Regel 13).

### Interactions with Other Systems

| System | Was kommt herein | Was geht hinaus |
|---|---|---|
| Saison-Ablauf (Approved) | Phase, `season_pressure`, `game_time`, „Spielzeit läuft“, Meldungen „Saison beginnt“, „Phase X beginnt“, „Nacht vorbei“ | – |
| Holzofen und Wärme (gebaut, kein GDD) | Wärme 0–100 % | – |
| Gegenstandsarten (Approved) | Eigenschaft „Gericht“ (Bedürfnis-Kennung, `is_warm`), Zustand „Fällt“ (geworfen oder fallen gelassen) und Aufprall-Ereignisse beim Host (Lärm, Chaos) | Legt die Bedürfnis-Kennungen und den Wert kalter Gerichte fest |
| Interaktion (Approved) | Handlung „Servieren“ (Kategorie Gericht, sofort); der Gast ist Bezugsobjekt der Ersten Hilfe | Der Gast ist eine Station mit der Bedingung „Möchte gerade X“ |
| Kochen und Gerichte *(kein GDD)* | Gerichte, deren Eigenschaft eine Bedürfnis-Kennung nennt (indirekt über die Gegenstandsarten) | – |
| Versorgung *(kein GDD)* | ruft „Bedürfnis erfüllt“ auf (Bett, Erste Hilfe) | Welche Betten für welchen Gast reserviert sind |
| Geräusche (gebaut) | – | Murren, „Aua“, Klimpern beim Trinkgeld, Schritte |
| Nachteinsatz *(kein GDD)* | Zeitpunkt, „Gast ist zurück“ samt neuen Bedürfnissen | der vermisste Gast |
| Bewertung *(kein GDD)* | – | eingefrorene Zufriedenheit aller Gäste, Zustand des Vermissten |
| Hüttenkasse *(kein GDD)* | – | Trinkgeld je erfülltem Bedürfnis |
| HUD *(kein GDD)* | – | offene Bedürfnisse, Zahl der Gäste |

*Vorläufig* sind alle Annahmen über Systeme ohne GDD, also Versorgung, Kochen, Nachteinsatz, Bewertung,
Hüttenkasse und HUD. Deren Dokumente müssen diese Schnittstellen bestätigen.

## Formulas

Die Werte stammen von `systems-designer` (Ankunft, Geduld, Zufriedenheit, Lärm und Chaos, Stimmung,
Genießen) und `economy-designer` (Trinkgeld). Beide haben sie am 2026-11-08 vorgeschlagen, der Entwickler hat
sie freigegeben.

- Alle Zeiten laufen in `game_time`.
- Alle Werte liegen in Datenobjekten (Gästetyp, Bedürfnis, Gäste-Einstellungen).
- Die Bereiche sind Empfehlungen für das Spielprofil. Harte Grenzen setzen `[Min]` im Inspector und
  Formel 9.

### 1. Ankunft

Die Formel `arrival_rate` ist definiert als:

`arrival_rate = player_factor[n] * phase_rate_scale[phase] * (rate_floor + rate_gain * season_pressure)`
(Gäste pro Minute)

Damit füllt der Host ein Ankunfts-Budget, aber nur am Nachmittag, am Abend und in der Nacht:

`arrival_budget = min(arrival_budget + arrival_rate / 60 * dt, next_cost)`

- **Fällig:** Ein Gast ist fällig, sobald `arrival_budget >= next_cost` gilt.
- **Nach der Ankunft:** Es gilt `arrival_budget -= next_cost` und
  `next_cost = 1 + arrival_jitter * random(-1, 1)`.
- **Kappung:** Die Obergrenze bei `next_cost` setzt Regel 5 um. Wartet ein fälliger Gast auf einen Platz,
  sammelt sich kein weiteres Budget an.
- **„Saison beginnt“:** Es gilt `arrival_budget = initial_budget` und `next_cost = 1`.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| player_factor[n] | float | 0,5–1,0 | data file | 0,50 / 0,75 / 0,90 / 1,00 für 1–4 Spieler |
| n | int | 1–4 | calculated | Spielerzahl der Saison (fest, Regel 11) |
| phase_rate_scale | float | 0–1 | data file | Nachmittag 1,0, Abend 1,0, Nacht 0,2. Nachts kommen nur Notfälle. Ein fester Faktor je Phase, keine eigene Kurve. |
| rate_floor | float | 0–0,5 /min | data file | 0,10: Auch ohne Druck kommt jemand. |
| rate_gain | float | 0–1,5 /min | data file | 0,70: Am Abendende kommen 2,2-mal so viele Gäste wie am Abendanfang. |
| season_pressure | float | 0–1 | calculated | Formel 5 im Saison-Ablauf |
| dt | float | ≥ 0 s | calculated | Spielzeit seit dem letzten Schritt |
| initial_budget | float | 0–1 | data file | 0,6: Der erste Gast kommt nach etwa 3,3 min (4 Spieler) bzw. 5,7 min (solo), weil der Druck schon am Nachmittag steigt. |
| next_cost | float | 0,6–1,4 | calculated | im Mittel 1; die Streuung erzeugt Schübe statt Gleichtakt |
| arrival_jitter | float | 0–0,5 | data file | 0,4 |

**Output Range:** 0,05/min (1 Spieler, Druck 0) bis theoretisch 0,80/min. Im Spiel liegt sie höchstens bei
0,52/min am Abendende (4 Spieler) und bei 0,16/min in der Nacht.

**Example:**

- 4 Spieler, Abend nach 360 s: Der Druck ist 0,4, also 1,0 × 1,0 × (0,10 + 0,70 × 0,4) = 0,38/min, im Mittel
  alle 158 s.
- Bei 2 Spielern sind es 0,285/min, also alle 211 s.
- Nacht bei Druck 1: 0,2 × 0,8 = 0,16/min.

### 2. Geduld

Die Formel `patience_total` ist definiert als:

`patience_total = patience_base[type][step] * (1 - patience_pressure_cut * pressure_at_start)`

Danach gilt `patience_left = patience_total - (game_time - need_start_time)`.

- Der Ring zeigt `clamp(patience_left / patience_total, 0, 1)`.
- Bei `patience_left <= 0` mault der Gast.

| Typ | 1. Bedürfnis | 2. Bedürfnis |
|---|---|---|
| Wanderer | Tee 150 s | Speckbrot 150 s |
| Familie | Kaiserschmarrn 210 s | Betten 120 s |
| Bergsteiger | Erste Hilfe 100 s | Tee 120 s |

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| patience_base | float | 60–300 s | data file | Tabelle oben. Puffer: etwa das 2- bis 3-Fache von Kochen und Weg (45–60 s). |
| patience_pressure_cut | float | 0–0,5 | data file | 0,30. Bewusst mild, weil der Druck schon die Ankünfte verdichtet. |
| pressure_at_start | float | 0–1 | calculated | `season_pressure` beim Anzeigen des Bedürfnisses, danach eingefroren (der Ring springt nicht) |
| need_start_time | float | ≥ 0 s | calculated | `game_time` beim Anzeigen |

**Output Range:** 70 s (Erste Hilfe bei Druck 1) bis 210 s (Kaiserschmarrn bei Druck 0).

**Example:** Kaiserschmarrn am Abend bei Druck 0,4 ergibt 210 × 0,88 = 184,8 s. Der Tee des Bergsteigers
nachts bei Druck 1 ergibt 120 × 0,7 = 84 s.

### 3. Zufriedenheit beim Erfüllen

Die Formel `fulfill_gain` ist definiert als:

`fulfill_gain = quality * (state_before == Mault ? late_gain : base_gain * (speed_floor + (1 - speed_floor) * patience_fraction))`
mit `quality = (Gericht AND NOT is_warm) ? cold_factor : 1`

- **Start:** Bei der Ankunft gilt S = `start_satisfaction`.
- **Maulen:** Beim Ablauf der Geduld gilt einmalig S = 0.
- **Erfüllen:** Beim Erfüllen steigt S um `fulfill_gain`.
- **Betten:** Der Gewinn kommt einmal, sobald alle Betten gezählt sind.
- **Grenzen:** Jede Änderung wird auf 0–100 begrenzt.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| start_satisfaction | float | 0–100 | data file | 60: neutral. Schon der erste Erfolg macht den Gast zufrieden. |
| base_gain | float | 5–40 | data file | 20: Zwei Erfolge heben S von 60 auf etwa 92. |
| speed_floor | float | 0–1 | data file | 0,5: Auf den letzten Drücker gibt es die Hälfte. |
| patience_fraction | float | 0–1 | calculated | Rest des Rings beim Erfüllen. Vorgemachte Betten zählen mit 1. |
| cold_factor | float | 0–1 | data file | 0,5: kalt weniger wert, aber nicht wertlos (Gegenstandsarten) |
| late_gain | float | 5–40 | data file | 25: Nach dem Maulen steigt S „etwas“. |
| is_warm | bool | – | calculated | aus `gegenstandsarten.md` |

**Output Range:**

- rechtzeitig: +10 bis +20 (warm), +5 bis +10 (kalt)
- nach dem Maulen: +25 (warm), +12,5 (kalt)

**Example:**

- Tee mit 132 s Geduld, nach 60 s warm serviert: Rest 72/132 = 0,545, Faktor 0,5 + 0,5 × 0,545 = 0,77. Das
  ergibt +15,5, also S von 60 auf 75,5.
- Kalt serviert: +7,7, also S = 67,7.
- Nach dem Maulen warm serviert: S = 25.

### 4. Laufende Zufriedenheit

Die Formel `satisfaction_rate` ist definiert als:

`satisfaction_rate = warm_bonus - cold_drain - blocked_drain` (Punkte pro Sekunde)

- `warm_bonus = (inside AND warmth >= comfort_warmth AND S < warm_cap AND state in {Wartet, Genießt, Bleibt, Schläft}) ? warm_rate : 0`
- `cold_drain = inside ? cold_rate_max * clamp((cold_threshold - warmth) / cold_threshold, 0, 1) : 0`
- `blocked_drain = (state == Blockiert AND blocked_time > blocked_grace) ? blocked_rate : 0` (drinnen wie
  draußen)

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| inside | bool | – | calculated | Der Gast ist in der Hütte (nicht unterwegs draußen, nicht Vermisst, nicht Weg). |
| warmth | float | 0–100 | calculated | Ofenwärme für die ganze Hütte |
| cold_threshold | float | 20–60 | data file | 40: Der Ofen verliert 1,5 pro Sekunde, nach dem Ausgehen bleiben also 40 s Gnadenfrist. |
| cold_rate_max | float | 0–1 /s | data file | 0,4 bei Wärme 0. Der Abzug wächst mit der Kälte, ein kurzer Aussetzer kostet wenig. |
| comfort_warmth | float | 40–100 | data file | 60: Zwischen 40 und 60 ist es „neutral“. |
| warm_rate | float | 0–0,1 /s | data file | 0,03: Gemütlichkeit ist sichtbar, ersetzt aber keinen Service. |
| warm_cap | float | 60–100 | data file | 85: Die letzten 15 Punkte verdient man sich durch Service. |
| blocked_rate | float | 0–0,5 /s | data file | 0,2 (12 Punkte pro Minute): „sinkt langsam“ |
| blocked_grace | float | 0–15 s | data file | 5 s: Wer kurz im Weg steht, kostet nichts. |
| blocked_time | float | ≥ 0 s | calculated | Wie lange der Gast schon blockiert ist |
| blocked_max | float | 30–180 s | data file | 90: Danach zwängt sich der Gast am Hindernis vorbei (Edge Cases). |

**Output Range:** −0,6 (Kälte und blockiert zugleich) bis +0,03 Punkte pro Sekunde.

**Example:** Der Ofen geht bei Wärme 100 aus.

- Die ersten 40 s kosten nichts.
- Bis Wärme 0 vergehen weitere 26,7 s, die zusammen 5,33 Punkte kosten.
- Nach 2 min ohne Feuer hat der Gast etwa 27 Punkte verloren.

### 5. Lärm und Chaos

Die Formeln `noise_penalty` und `chaos_penalty` sind definiert als:

- `is_loud = broke OR (impact_speed >= noise_speed_min AND mass_kg >= noise_mass_min)`
- `noise_penalty = (is_loud AND inside AND game_time - last_noise >= noise_lockout) ? noise_base * clamp(1 - distance / noise_radius, 0, 1) : 0`
- `chaos_penalty = (item_state == Fällt AND inside AND hit_speed >= chaos_speed_min AND game_time - last_chaos >= chaos_lockout) ? chaos_base : 0`

Ergänzende Regeln:

- Die Sperrzeit beginnt nur, wenn wirklich ein Abzug angewendet wurde.
- Trifft der Gegenstand den Gast selbst, zählt nur Chaos und nicht zusätzlich Lärm. Das gilt auch, wenn der
  Treffer kein Chaos ergibt (zu langsam oder in der Sperrzeit): Dieser Gast bekommt aus dem Aufprall nie Lärm.
- `impact_speed` ist wie in `breaks_on_impact` die Normalkomponente.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| broke | bool | – | calculated | Der Aufprall hat ein Gericht zerbrochen (`breaks_on_impact`); zählt ohne Massegrenze. |
| noise_speed_min | float | 5,5–7,0 m/s | data file | 5,5: knapp über dem Ablegen aus Haltehöhe (5,01) und unter dem Bruch (7,0). Würfe (etwa 8) zählen, Ablegen nie. |
| noise_mass_min | float | 0,5–5 kg | data file | 2: Scheit, Axt und Kiste poltern. |
| noise_base | float | 0–10 | data file | 3: klein. Fünf Poltereien kosten etwa einen Erfolg. |
| noise_radius | float | 4–12 m | data file | 8: fast die ganze Stube (6,6 × 9,4 m), Luftlinie, linear abfallend |
| noise_lockout | float | 1–10 s | data file | 4: Ein umfallender Stapel kostet einmal. |
| chaos_base | float | 5–40 | data file | 15: groß, etwa ein Erfolg |
| chaos_speed_min | float | 1–5 m/s | data file | 3: Was rollt oder rutscht, tut nicht weh. |
| chaos_lockout | float | 1–10 s | data file | 3: Eine Kiste trifft nicht in jedem Physikschritt neu. |
| distance, hit_speed, mass_kg | float | – | calculated | Abstand zum Aufprall (bei der Familie zur näheren Figur), Geschwindigkeit beim Treffer, Masse der Art |

**Output Range:** Lärm 0–3 je Ereignis, Chaos 0 oder 15.

**Example:**

- Eine geworfene Kiste (10 kg, 8 m/s) prallt gegen die Wand, der Gast ist 3 m entfernt: 3 × (1 − 3/8) = −1,9.
  Ist er 7 m entfernt, sind es −0,4.
- Ein Klotz, der mit 5,01 m/s abgelegt wird, ist kein Lärm.
- Eine Kiste, die den Gast mit 4 m/s trifft, kostet −15 und löst „Aua“ aus.

### 6. Stimmung

Die Formel `mood` ist definiert als:

`mood = (state == Mault OR (state == Blockiert AND blocked_time > blocked_grace) OR S < mault_in) ? mault : (S >= happy_in ? zufrieden : neutral)`

Gegen Flackern gilt eine Hysterese: Die Stimmung „mault“ endet erst ab `mault_out`, die Stimmung „zufrieden“
erst unter `happy_out`.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| mault_in / mault_out | float | 0–40 | data file | 20 / 25: Nach spätem warmem Servieren (S = 25) ist der Gast wieder neutral, nach kaltem (12,5) murrt er weiter. |
| happy_in / happy_out | float | 50–90 | data file | 65 / 60: Der erste Erfolg macht zufrieden. |
| mood | enum | mault, neutral, zufrieden | calculated | geht als grobe Stimmung über das Netz |

**Output Range:** einer der drei Werte.

**Example:**

- Start bei 60: neutral.
- Nach dem Tee 75,5: zufrieden.
- Fällt S auf 61, bleibt er zufrieden. Erst bei 59 wird er wieder neutral.

### 7. Trinkgeld

Die Formel `tip_coins` ist definiert als:

`tip_coins = (state_before == Mault OR S_before <= 0) ? 0 : max(1, round(tip_base[need] * amount * clamp(S_before / tip_full, 0, 1) * w * tip_factor[n]))`
mit `w = (Gericht AND NOT is_warm) ? tip_cold_factor : 1`

Gezahlt wird sofort beim Erfüllen, bei Betten einmal, wenn alle gezählt sind. `round` rundet kaufmännisch (0,5
aufwärts).

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| tip_base[need] | int | 1–20 | data file | Tee 4, Speckbrot 6, Erste Hilfe 8, Kaiserschmarrn 12, Bett 3, nach Aufwand |
| amount | int | 1–2 | calculated | 1; bei Betten die Zahl der Betten (Figuren) |
| S_before | float | 0–100 | calculated | Zufriedenheit unmittelbar vor dem Gewinn aus Formel 3. So zählt kalt nicht doppelt. |
| tip_full | float | 50–100 | data file | 80: ab hier voller Betrag. Das verzeiht kleine Abzüge. |
| tip_cold_factor | float | 0–1 | data file | 0,5: Ein kaltes Gericht zahlt die Hälfte, auch bei S = 100. |
| tip_factor[n] | float | 1,0–1,5 | data file | 1,5 / 1,25 / 1,1 / 1,0 für 1–4 Spieler. Gleicht die geringere Gästezahl aus, eine Preisliste für alle. |

**Output Range:** rechnerisch 0–18 Münzen je Erfüllung, im Spiel höchstens 15 (Kaiserschmarrn solo).

- Beim ersten Bedürfnis liegt `S_before` höchstens bei `start_satisfaction` + `warm_rate` × Geduld, also bei
  63–66. Dort gibt es höchstens etwa 80 % des Betrags.
- Eine ganze Kette (warm, schnell, 4 Spieler) bringt beim Wanderer höchstens 9, bei der Familie 16 und beim
  Bergsteiger 10.
- *Korrigiert 2026-11-09 nach Prüfung durch `qa-lead`; vorher 0–18 je Erfüllung und 10 / 18 / 12 je Kette.*

**Example:**

- Kaiserschmarrn kalt, S_before = 60, 2 Spieler: 12 × 0,75 × 0,5 × 1,25 = 5,6, also 6 Münzen.
- Tee warm, S_before = 80, solo: 4 × 1 × 1 × 1,5 = 6.
- Nach dem Maulen serviert: 0.

### 8. Genießen

Die Formel `enjoy_total` ist definiert als:

`enjoy_total = enjoy_duration[need] + (has_next_need ? next_need_gap : 0)`

So lange ist der Gast im Zustand „Genießt“ und keine Station. Die Geduld des nächsten Bedürfnisses startet erst
danach.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| enjoy_duration | float | 0–10 s | data file | Tee 4, Speckbrot 6, Kaiserschmarrn 8, Erste Hilfe 5, Bett 0 (der Gast geht direkt ins Bett). So lange, dass man die Reaktion sieht. |
| next_need_gap | float | 0–10 s | data file | 4: kurz durchatmen |
| has_next_need | bool | – | calculated | Die Kette hat noch ein Bedürfnis. |

**Output Range:** 0–12 s.

**Example:** Wanderer nach dem Tee: 4 + 4 = 8 s, dann erscheinen das Speckbrot-Symbol und ein neuer Ring.

### 9. Prüfung der Gäste-Daten (Editor, Fehler)

Die Formel `guest_data_valid` ist definiert als:

`guest_data_valid = 0 <= arrival_jitter < 1 AND 0 <= patience_pressure_cut < 1 AND min(patience_base) > 0 AND tip_full > 0 AND noise_radius > 0 AND cold_threshold > 0 AND comfort_warmth >= cold_threshold AND mault_in <= mault_out < happy_out <= happy_in AND weight_sum[Nachmittag] > 0 AND weight_sum[Abend] > 0 AND weight_sum[Nacht] > 0`

Dazu kommen die Prüfungen aus den Grenzfällen (Kette 1–2, Figuren 1–2, Vorsilbe `beduerfnis_`, bekannte
Kennung im Gericht). `[Min]` im Inspector reicht dafür nicht, weil es „< 1“ nicht ausdrücken kann.

**Variables:**

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| arrival_jitter, patience_pressure_cut | float | 0 bis unter 1 | data file | Bei 1 würde `next_cost` 0 (in jedem Schritt ein Gast) bzw. `patience_total` 0 (der Ring teilt durch null). |
| patience_base, tip_full, noise_radius, cold_threshold | float | > 0 | data file | Teiler in den Formeln 2, 4, 5 und 7 |
| comfort_warmth | float | ≥ `cold_threshold` | data file | Sonst gäbe es Wärmebonus und Kälteabzug zugleich. |
| mault_in, mault_out, happy_out, happy_in | float | aufsteigend | data file | Sonst überlappen die Stimmungen, und die Hysterese kippt. |
| weight_sum[phase] | float | > 0 | calculated | Summe der Gewichte aller Typen in der Phase; bei 0 gibt es nichts zu ziehen. |
| guest_data_valid | bool | true/false | calculated | false = Fehler im Editor mit dem Namen des verletzten Werts |

**Output Range:** bool. Mit den Startwerten ist sie wahr.

### Erwartete Werte (Prüfung, keine Regel)

**Gäste pro Saison** (Nacht 720 s, ohne Platzgrenze):

| Spieler | Nachmittag | Abend | Nacht | gesamt |
|---|---|---|---|---|
| 1 | 1,5 | 2,3 | 0,9 | 4,7 |
| 2 | 1,9 | 3,4 | 1,3 | 6,6 |
| 3 | 2,1 | 4,1 | 1,5 | 7,7 |
| 4 | 2,3 | 4,6 | 1,7 | 8,6 |

**Belegung:**

- Mit 4 Spielern sind am Ende des Abends etwa 7–8 von 10 Sitzen besetzt, am Ende der Nacht etwa 9.
- Mit 2 Spielern sind es etwa 6 und 7.
- Die zweite Familie füllt die Betten.

**Arbeitslast am Abend:**

- **Zu viert:** 22–28 % Auslastung, Holz für den Ofen eingerechnet. Die Gäste allein sind also eher
  entspannt. Den Rest des Drucks bringen Ofen, Ereignisse, Seilbahn und Nachteinsatz.
- **Zu zweit:** 38–46 %. In Spitzen (drei Gäste in 2 min) mault etwa ein Gast.
- Der Entwickler hat am 2026-11-08 entschieden, das so zu lassen und im Freundestest zu messen.
- Der natürliche Engpass sind die Kochstellen aus dem Kochen-GDD.

**Trinkgeld pro Saison** (Rechnung `economy-designer`, mit den Gästezahlen oben):

| Leistung | 1 Spieler | 2 Spieler | 4 Spieler |
|---|---|---|---|
| schwach | etwa 13 | etwa 15 | etwa 16 |
| mittel | etwa 42 | etwa 49 | etwa 51 |
| sehr gut | etwa 69 | etwa 81 | etwa 84 |

Hinweis für die Hüttenkasse:

- Upgrade-Preise in der Größenordnung von 20–90 Münzen.
- Nach der Korrektur von Formel 7 liegt das rechnerische Maximum bei etwa 80 Münzen (solo) bis 94 (zu viert).
  „Sehr gut“ ist damit knapp bemessen. Die Hüttenkasse prüft das beim Festlegen der Preise.

## Edge Cases

> `systems-designer` für diesen Abschnitt nicht befragt (Lean-Modus).

**Ankunft und Plätze**

- **Wenn eine Familie fällig ist, aber zwei Sitze an einem Tisch oder zwei Betten fehlen:** Die Familie fällt
  aus der Ziehung (Regel 5). Passt dann kein anderer Typ, wartet die Ankunft.
- **Wenn nachts alle Sitze belegt sind:** Der Bergsteiger-Notfall wartet auf einen Platz. Ab dem Abend geht
  aber niemand mehr, also kommt er in dieser Nacht nicht. Bei „Nacht vorbei“ verfällt er. Einen Sonderplatz
  gibt es nicht.
- **Wenn auf einem reservierten Platz ein Gegenstand liegt:** Der Platz ist ein Ziel, das man nicht
  erreicht. Der Gast ist blockiert (siehe unten), bis jemand den Platz frei räumt.
  - Nach `blocked_max` setzt bzw. legt er sich trotzdem: Der Gegenstand ist für ihn durchlässig, solange er
    den Platz belegt, und bewegt sich nicht.
  - Danach gilt alles wie gewohnt, er ist am Platz eine Station.
  - So hängt solo nichts fest, auch wenn die Truhe (nur zu zweit tragbar) auf dem Sitz steht.
- **Wenn die Szene keine Sitzplätze oder keinen Ankunftspunkt hat:** Das ist ein Fehler im Editor. Im Spiel
  kommen keine Gäste, und der Host schreibt eine Fehlermeldung ins Log.
- **Wenn ein Gästetyp oder ein Bedürfnis falsch angelegt ist:** Das ist ein Fehler im Editor. Die Prüfung
  nennt die Kennung oder den Asset-Namen. Falsch heißt:
  - Die Kette hat nicht 1–2 Bedürfnisse.
  - Die Figurenzahl ist nicht 1 oder 2.
  - Eine Bedürfnis-Kennung beginnt nicht mit `beduerfnis_`.
  - Ein Gericht nennt eine Kennung, die es nicht gibt.

**Wege und Blockaden**

- **Wenn ein Gegenstand auf dem Weg eines gehenden Gastes zur Ruhe kommt:** Der Host plant den Weg neu. Gibt
  es keinen, ist der Gast blockiert.
- **Wenn ein Gast länger als `blocked_max` (90 s) blockiert ist:**
  - Er zwängt sich am Hindernis vorbei: Für diesen Gast ist der Gegenstand kurz durchlässig, bis er vorbei
    ist.
  - Der Gegenstand bewegt sich dabei nicht.
  - Die 90 s Abzug bleiben. Solo hängt so nichts fest, etwa wenn die Truhe in der Tür liegt.
- **Wenn Spieler im Weg stehen (zum Beispiel mit der Truhe in der Tür):** Das zählt nicht als „Blockiert“.
  Der Gast wartet oder weicht aus und kostet keine Zufriedenheit. Als Blockade zählen nur ruhende
  Gegenstände.
- **Wenn ein Gast auf dem Rückweg oder auf dem Weg zum Bett blockiert ist:** Es gelten dieselben Regeln.

**Servieren und Erfüllen**

- **Wenn zwei Spieler demselben Gast gleichzeitig das richtige Gericht geben:**
  - Der Host arbeitet die Wünsche der Reihe nach ab. Der erste erfüllt das Bedürfnis.
  - Der zweite schlägt fehl, weil der Gast jetzt genießt und keine Station mehr ist. Das zweite Gericht
    bleibt in der Hand.
- **Wenn das Servieren im selben Host-Schritt ankommt, in dem die Geduld abläuft:** Das Servieren zählt als
  rechtzeitig. Im Zweifel entscheidet der Host für die Spieler.
- **Wenn das Gericht zerbricht, während es serviert werden soll:** Wie in den Gegenstandsarten schlägt das
  Servieren fehl, und der Gast wartet weiter.
- **Wenn ein maulender Gast sein Bedürfnis bekommt:** Er erhält `late_gain` und kein Trinkgeld. Die Kette
  geht normal weiter, das nächste Bedürfnis beginnt mit voller Geduld.
- **Wenn Betten schon gemacht sind, bevor die Familie reserviert:** Sie zählen sofort bei der Reservierung,
  mit `patience_fraction` = 1, sobald das Bedürfnis „Bett“ dran ist.
- **Wenn ein gezähltes Bett später wieder ungemacht wird** (falls die Versorgung das vorsieht): Die Zählung
  bleibt. Einmal erfüllt ist erfüllt.

**Stimmung**

- **Wenn ein Gegenstand eine Familie trifft:** Die Familie ist ein Gast. Ein Treffer an einer der beiden
  Figuren kostet einmal `chaos_base`. Treffer an beiden Figuren innerhalb von `chaos_lockout` zählen nur
  einmal.
- **Wenn ein geworfener Gegenstand mehrere Gäste trifft:** Jeder Gast zahlt seinen eigenen Abzug, jeder mit
  eigener Sperrzeit.
- **Wenn ein Spieler mit einer getragenen Kiste in einen Gast läuft:** Getragenes ist nicht im Zustand
  „Fällt“, also gibt es kein Chaos. Der Gast weicht aus.
- **Wenn es draußen nahe der Hütte poltert** (Holzplatz): Lärm zählt per Luftlinie, Wände dämpfen nicht.
  Das Hacken selbst ist kein Aufprall.
- **Wenn Schlafende Lärm, Kälte oder einen Treffer abbekommen:** Es gilt dasselbe wie für alle Gäste in der
  Hütte. Aufwachen gibt es nicht.
- **Wenn S schon 0 ist:** Abzüge bleiben bei 0. Ein Gast mit S = 0, der nicht mault (zum Beispiel nach
  Chaos), gibt kein Trinkgeld, weil `S_before` ≤ 0 ist.
- **Wenn S genau auf einer Stimmungsgrenze liegt:** Bei 65 ist der Gast zufrieden (≥), bei 20 neutral
  (< zählt).

**Phasen und Saison**

- **Wenn ein Gast genau beim Wechsel vom Nachmittag zum Abend fertig wird:** Es zählt die Phase im Moment,
  in dem das Genießen endet. Wer schon geht, geht weiter.
- **Wenn die Spielzeit steht (Pause solo):**
  - Alle Uhren der Gäste stehen: Geduld, Genießen, Sperrzeiten, Ankunft.
  - Die Figuren bleiben stehen.
  - Kälte und Wärme wirken nicht, weil `dt` = 0 ist.
- **Wenn ein Client während der Saison die Verbindung verliert:** `n` bleibt die Spielerzahl beim
  Saisonstart. Ankunft und Trinkgeld ändern sich nicht.
- **Wenn „Nacht vorbei“ kommt, während Bedürfnisse offen sind:**
  - Ab diesem Moment sind Gäste keine Stationen mehr.
  - Uhren und Zufriedenheit stehen, es gibt kein Trinkgeld mehr.
  - Die Gäste bleiben sichtbar in der Hütte bis „Saison beginnt“.
- **Wenn der Host die Saison abbricht:** Die Kasse setzt der Saison-Ablauf zurück. Die Gäste verschwinden
  bei der nächsten „Saison beginnt“. Bis dahin sind sie eingefroren (Regel 13), weil die Saison in der
  Vorbereitung steht: keine Stationen, Uhren und Zufriedenheit stehen. Das gilt auch beim Abbruch am
  Nachmittag oder Abend, wenn der Saison-Ablauf kein „Nacht vorbei“ schickt.

**Vermisster Gast**

- **Während er vermisst ist:**
  - Seine Zufriedenheit steht, denn draußen wirkt nichts.
  - Sein Platz bleibt reserviert, ein offenes Bedürfnis verfällt.
- **Wenn er zurückkommt:**
  - Er setzt sich an seinen Platz, und S läuft vom eingefrorenen Wert weiter.
  - Die neuen Bedürfnisse vergibt der Nachteinsatz.
- **Wenn kein wacher Gast in Frage kommt:** Ein Ersatz-Wanderer entsteht draußen. Er reserviert keinen
  Sitzplatz. Kommt er zurück, setzt er sich auf den **Platz am Ofen** (eigener Marker, nur für den
  Zurückgebrachten) und startet mit `start_satisfaction`.
- **Wenn die Frist des Einsatzes abläuft, ohne dass er zurück ist:**
  - Er bleibt bis zum Saisonende im Zustand „Vermisst“.
  - An die Bewertung gehen sein eingefrorener Wert und der Zustand „Vermisst“. Was das für die Edelweiß
    bedeutet, legt die Bewertung fest.

## Dependencies

**Hängt ab von:**

| System | Art | Schnittstelle |
|---|---|---|
| Netzwerk-Sitzung (gebaut) | hart | Host-Autorität, Spielerzahl beim Saisonstart (`n`), Übertragung von Position, Zustand, Bedürfnis, Geduld und Stimmung an die Clients |
| Saison-Ablauf und Spielphasen | hart | Phase, `season_pressure`, `game_time`, „Spielzeit läuft“, Meldungen „Saison beginnt“, „Phase X beginnt“, „Nacht vorbei“ (`design/gdd/saison-ablauf.md`) |
| Gegenstandsarten | hart | Eigenschaft „Gericht“ (Bedürfnis-Kennung, `is_warm`), Zustand „Fällt“, Masse, `breaks_on_impact` (`design/gdd/gegenstandsarten.md`) |
| Interaktion | hart | Handlung „Servieren“ (Kategorie Gericht, sofort), Station und Bedingung „Möchte gerade X“, Reichweite (`design/gdd/interaktion.md`) |
| Holzofen und Wärme (gebaut, kein Dokument) | hart | Wärme 0–100 für die ganze Hütte |
| Wegfindung (Teil dieses Systems) | hart | NavMesh beim Host. Dafür braucht es vermutlich das Paket AI Navigation, das nur nach OK des Entwicklers kommt. Eine ADR „Wegfindung im Netz“ entscheidet. |

**Wird gebraucht von:**

| System | Art | Schnittstelle | Stand |
|---|---|---|---|
| Kochen und Gerichte | hart, indirekt | Gerichte nennen eine Bedürfnis-Kennung (die Eigenschaft steht in den Gegenstandsarten, das Kochen setzt sie); die Geduld rechnet mit 45–60 s Kochen inklusive Weg | kein Dokument |
| Versorgung: Betten und Erste Hilfe | hart | ruft „Bedürfnis erfüllt“ auf (Bett je Bett, Erste Hilfe), liest die Bett-Reservierungen | kein Dokument |
| Nachteinsatz | hart | bekommt den vermissten Gast, gibt „Gast ist zurück“ samt neuen Bedürfnissen | kein Dokument |
| Bewertung | hart | eingefrorene Zufriedenheit aller Gäste und den Zustand des Vermissten bei „Nacht vorbei“ | kein Dokument |
| Hüttenkasse und Upgrades | hart | Trinkgeld je Erfüllung; Preise in der Größenordnung von 20–90 Münzen | kein Dokument |
| HUD | hart | offene Bedürfnisse, Zahl der Gäste | kein Dokument |
| Solo-Hüttenhund | weich | Hindernis-Regeln und Wegfindung (der Hund nutzt dieselbe Technik) | kein Dokument |
| Geräusche (gebaut) | weich | Murren, „Aua“, Münzklimpern, Schritte der Gäste | kein Dokument |
| Ereignisse, Wetter | weich | Kälte über den Ofen (der Generator- und der Fenster-Ausfall wirken nur über die Wärme) | kein Dokument |
| Tutorial (Alpha) | weich | erster Gast als Lehrbeispiel | kein Dokument |

**Kreis Gäste ↔ Versorgung und Kochen:**

- Die Gäste legen die Bedürfnisse fest und bieten die Schnittstelle „Bedürfnis erfüllt“ an.
- Stationen und Gerichte rufen nur diese Schnittstelle auf. Die Gäste kennen keine Station
  (`systems-index.md`).

**Beidseitig:**

- Die Einträge „kein Dokument“ in Saison-Ablauf, Gegenstandsarten und Interaktion werden auf
  `design/gdd/gaeste.md` gesetzt.
- Jedes System ohne Dokument muss die Gäste beim Schreiben unter seinen Abhängigkeiten nennen.

## Tuning Knobs

Bei Stufe `standard` nur empfohlen; ergänzt wie im Saison-Ablauf. Die empfohlenen Bereiche stehen in den
Variablentabellen unter „Formulas“. Hier steht, wo die Werte liegen und was zusammenhängt.

| Datenobjekt | Stellschrauben | Wirkung |
|---|---|---|
| Gästetyp (je Typ) | Figurenzahl, Bedürfniskette, `patience_base` je Schritt, Gewicht je Phase | Wer wann kommt und wie viel Zeit man hat |
| Bedürfnis (je Bedürfnis) | Symbol, Menge, `tip_base`, `enjoy_duration` | Wert und Rhythmus eines einzelnen Wunschs |
| Gäste-Einstellungen (eins) | Ankunft, Geduld, Zufriedenheit, Raumstimmung, Lärm und Chaos, Stimmung, Trinkgeld (Formeln 1–8), `next_need_gap`, `walk_speed` (Regel 9) | alles Übrige |

**Die wichtigsten Hebel für den Freundestest:**

- `rate_gain` und `player_factor`: der Andrang am Abend (zu viert 22–28 % Last, zu zweit 38–46 %).
- `patience_base`: wie viel das Spiel verzeiht. Soll etwa das 2- bis 3-Fache von Kochzeit plus Weg bleiben.
- `cold_threshold` und `cold_rate_max`: wie hart ein ausgehender Ofen bestraft wird.
- `noise_base` und `chaos_base`: was Chaos an Laune kostet. Zu hoch, und niemand wirft mehr etwas. Das wäre
  gegen die Säule Physik-Chaos.

**Wechselwirkungen:**

- `cold_threshold` hängt an der Abklingrate des Ofens (1,5/s ergibt 40 s Gnadenfrist). Ändert man den Ofen,
  ändert sich die Gnadenfrist.
- `noise_speed_min` muss zwischen dem Ablegen (5,01 m/s) und dem Bruch (7,0 m/s, `breaks_on_impact`) bleiben.
- `tip_base`, `tip_factor` und die Gästezahl bestimmen zusammen das Einkommen. Die Upgrade-Preise der
  Hüttenkasse müssen mitziehen.
- `mault_out` 25 passt zu `late_gain` 25: Wer nach dem Maulen warm versorgt wird, ist wieder neutral.

## Visual/Audio Requirements

- **Figuren:** Die drei Typen sind auf einen Blick zu unterscheiden: der Wanderer mit Rucksack, die Familie
  als großes und kleines Paar, der Bergsteiger mit Helm und Seil. Im Vertical Slice reichen einfache Formen
  und eine Farbe je Typ.
- **Symbol über dem Kopf:**
  - Je Bedürfnis ein eigenes Symbol: Tasse, Brot, Pfanne, Bett, rotes Kreuz.
  - Aus der ganzen Stube lesbar (mindestens 10 m) und immer zur Kamera gedreht.
  - Der Geduldsring liegt um das Symbol.
- **Stimmung:**
  - Ein Gesicht in drei Stufen (zufrieden, neutral, mault).
  - Wer mault, verschränkt die Arme. Wer blockiert ist, schaut zum Hindernis.
- **Genießen und Schlafen:**
  - Der Gast trinkt, isst oder wird verbunden. Über warmen Gerichten steigt Dampf.
  - Wer schläft, liegt im Bett, darüber „Zzz“.
- **Ton:**
  - Murren beim Wechsel zu „Mault“, danach höchstens alle 20 s leise
  - „Aua“ beim Treffer
  - Klimpern beim Trinkgeld
  - Schritte (vorhandene Clips für Holz und Schnee)
  - Gemurmel der bleibenden Gäste als leiser Raumklang (Gemütlichkeit)
  - leises Schnarchen im Schlafraum

`art-director` und `audio-director` nicht befragt (Lean-Modus). Symbol, Ring und Gesicht verfeinert das
UX-Dokument (siehe UI Requirements).

## Game Feel

- **Ein Blick genügt:** Von der Tür aus sieht man, wer was will und wie dringend. Dafür braucht es keine
  Liste.
- **Dringlichkeit ohne Panik:** Der Ring wird erst im letzten Drittel rot. Das Murren ist hörbar, nervt aber
  nicht.
- **Sofortige Belohnung:** Beim Servieren gibt es sofort Geräusch und Bewegung (Interaktion, Regel 4).
  Genießen und Klimpern folgen nach der Bestätigung durch den Host, bei 100 ms Ping also kurz danach.
- **Die Stube klingt nach ihrem Zustand:** Warm und voll heißt Gemurmel und Knistern, kalt und chaotisch
  heißt Murren. Dieser Unterschied ist das eigentliche Feedback zum Ofen.
- **Chaos soll lustig sein, nicht strafen:** Ein Treffer kostet viel, aber das „Aua“ soll zum Lachen sein.
- **Gäste stehen nie lange im Weg:** Sie weichen Spielern aus. Nach `blocked_max` zwängen sie sich vorbei.

## UI Requirements

- **Über dem Gast:** Symbol, Geduldsring, Stimmung und das Trinkgeld als „+6“. Alle Spieler sehen dasselbe
  (kommt vom Host).
- **Hinweis am Fadenkreuz** (Interaktion): „Tee servieren“, sonst der Grund, warum es nicht geht, zum
  Beispiel „Möchte einen Tee“.
- **HUD (vorläufig):** Zahl der Gäste und offene Bedürfnisse je Art, zum Beispiel „2× Tee, 1× Erste Hilfe“.
- **Debug-Anzeige (Host):** je Gast Zustand, S, Geduld und Stimmung, dazu Ankunfts-Budget und `next_cost`.
- **Farbschwäche:** Ring und Stimmung zeigen ihren Zustand nicht nur über Farbe, sondern auch über
  Füllstand und Gesicht.

> **UX-Hinweis:** Die Anzeige über den Gästen, die Hinweise am Fadenkreuz und die HUD-Liste brauchen vor den
> Epics ein UX-Dokument mit `/ux-design` (`design/ux/hud.md`).

## Cross-References

| Verweis | Quelle | Genutzt in |
|---|---|---|
| Phasen, `season_pressure`, `game_time`, Meldungen, Testprofil, Beitritt nur zwischen Saisons | `design/gdd/saison-ablauf.md` | Regeln 5, 11, 13; Formeln 1, 2 |
| Eigenschaft „Gericht“, `is_warm`, `breaks_on_impact`, Zustand „Fällt“, Massen | `design/gdd/gegenstandsarten.md` | Regeln 2, 6, 8; Formeln 3, 5, 7 |
| Handlung „Servieren“, Station mit Bedingung, Hinweis am Fadenkreuz, Erste Hilfe | `design/gdd/interaktion.md` | Regel 6, UI Requirements |
| Ofenwärme 0–100, Abklingrate 1,5/s | `Stove`, `StoveSettings` | Formel 4 |
| „Drinnen?“ per Strahl zum Dach; kann `inside` liefern | `Shelter` | Formeln 4, 5 |
| Aufprall beim Host, Masse | `ImpactSound` | Formel 5 |
| Tische und Maße der Stube | `docs/GRAUBOX.md` | Erwartete Werte, `noise_radius` |
| Schlafraum mit 4 Betten, Weg vom Hang | `design/levels/huette-skizze.md` | Regeln 4, 5 |
| Gästetypen, Trinkgeld und Bewertungen | `docs/KONZEPT.md` | Overview |
| Architektur-Regeln 2 und 4 | CLAUDE.md | Regeln 1, 15 |

## Acceptance Criteria

Vorschlag von `qa-lead` (2026-11-08), vom Entwickler am 2026-11-09 freigegeben. Die Lücken, die er dabei
gefunden hat, sind eingearbeitet (Formeln 1, 5, 7, Regeln 9, 10, Grenzfälle).

**Damit die Tests verlässlich bleiben:**

- Alle Formeln sind reine Funktionen, zum Beispiel in einer statischen Klasse `GuestMath`.
- Die Gäste-Logik des Hosts läuft ohne Szene. `Step(double gameTime, …)` bekommt Phase, `season_pressure`,
  Wärme, `n` und die Ereignisse des Schritts übergeben (Servieren, „Bedürfnis erfüllt“, Aufprall, Weg
  blockiert oder frei).
- Der Zufall kommt aus einer austauschbaren Quelle: entweder eine feste Wertefolge (Ziehung `u` ∈ [0; 1),
  Streuung `r` ∈ [−1; 1]) oder `System.Random` mit festem Seed.
- Saison-Ablauf, Ofen, Versorgung, Nachteinsatz, Bewertung und Hüttenkasse sind Stubs.
- **Test-Hütte** für EditMode: Tisch A mit 2 Sitzen, Tisch B mit 3 Sitzen, 2 Betten, 1 Ofenplatz.
- Der Wärme-Stub steht auf 50 (neutral: kein Kälteabzug, kein Wärmebonus), wenn ein Kriterium nichts anderes
  sagt.
- PlayMode läuft wie in `interaktion.md` (`SimulationMode.Script`, `Physics.Simulate(0.02f)`, austauschbare
  Uhr). Die Wegfindung wird in kleinen Testszenen geprüft.
- Zeiten sind `double`. Schrittfolgen laufen in 0,37 s und landen nie genau auf einer Grenze.
- Toleranz ±0,01. Werte, die über viele Schritte aufsummiert werden: ±0,05. Summen über die Schwelle
  `blocked_grace`: ±0,1. Münzen müssen exakt stimmen.

**Messweg (gehört in den Bau):**

- Log-Zeilen:
  `[Guest] t=<ServerTime> <arrive|seat|need|need-received|serve|reject|mault|leave|gone|blocked|free|squeeze|noise|chaos|tip|missing|returned|freeze|clear> gt=<game_time> id= type= state= need= S= left= mood= n= coins= client= reason=`
- Eine Debug-Anzeige je Gast (Typ, Zustand, Bedürfnis, `patience_left`, S, Stimmung), dazu `arrival_budget`,
  `next_cost`, `n` und die freien Sitze und Betten.
- Debug-Befehl „Gast <typ>“ (nur Editor und Development Build): Er macht sofort eine Ankunft dieses Typs
  fällig, die Platzregeln gelten weiter. Ohne ihn käme mit dem Testprofil (60/60/60/120 s) solo höchstens
  ein Gast, und zwar ganz am Ende der Nacht (Budget am Saisonende etwa 1,02).

**Testarten:**

- **[EditMode]** unter `Assets/Tests/EditMode/`
- **[PlayMode]**, auch mit Host und Client im selben Testprozess, ohne zwei Fenster
- **[Manuell]** solo im Editor
- **[Freundestest]** zu zweit übers Netz, Development Build, Ping etwa 100 ms (echt oder mit dem Network
  Simulator aufgestockt)

### A. Daten und Plätze (Regeln 1–4)

1. **[EditMode]** **GIVEN** die ausgelieferten Datenobjekte **WHEN** die Prüfung läuft **THEN** gibt es
   0 Fehler und genau diese Werte:
   - `wanderer`: 1 Figur, Tee 150 s → Speckbrot 150 s
   - `familie`: 2 Figuren, Kaiserschmarrn 210 s → Bett 120 s
   - `bergsteiger`: 1 Figur, Erste Hilfe 100 s → Tee 120 s
   - 5 Bedürfnisse, alle mit `beduerfnis_` davor
   - Trinkgeld: Tee 4, Speckbrot 6, Kaiserschmarrn 12, Bett 3, Erste Hilfe 8
   - Genießen: 4 / 6 / 8 / 0 / 5 s (gleiche Reihenfolge)
   - Startlisten: 3:1:– / 2:2:1 / –:–:1
   - `walk_speed` 1,4 m/s
   - Jedes Gericht der Gegenstandsarten nennt genau eine dieser 5 Kennungen.

   *Schnappschuss: Tabelle und Test werden zusammen geändert.*
2. **[EditMode]** **GIVEN** je ein Test-Objekt mit genau einem Fehler (Kette mit 0 oder 3 Bedürfnissen,
   Figurenzahl 0 oder 3, Kennung `tee` ohne Vorsilbe, Gericht mit unbekannter Bedürfnis-Kennung) **WHEN** die
   Prüfung läuft **THEN** gibt es genau 1 Fehler je Objekt, der seine Kennung oder seinen Asset-Namen nennt.
   **GIVEN** die Gäste-Einstellungen mit je genau einem dieser Werte: `arrival_jitter` 1,0;
   `patience_pressure_cut` 1,0; `tip_full` 0; `noise_radius` 0; `cold_threshold` 0; ein `patience_base` 0;
   `comfort_warmth` 30 (unter `cold_threshold` 40); `mault_out` 15 (unter `mault_in` 20); `mault_out` 60 (gleich
   `happy_out` 60, nicht kleiner); `happy_out` 70 (über `happy_in` 65); alle Abend-Gewichte 0 **WHEN**
   `guest_data_valid` geprüft wird **THEN** ist sie false, mit genau 1 Fehler, der den verletzten Wert nennt.
   Damit ist jede Bedingung von Formel 9 einmal verletzt. Mit den Startwerten ist sie true.
3. **[EditMode]** **GIVEN** die Test-Hütte, alles frei **WHEN** nacheinander Familie, Wanderer und
   Bergsteiger ankommen **THEN** belegt die Familie 2 Sitze an einem Tisch und 2 Betten, die beiden anderen
   je 1 Sitz. Frei bleiben 1 Sitz und 0 Betten. **WHEN** der Wanderer am Nachmittag fertig ist **THEN** hält
   er seinen Sitz im Zustand Geht und gibt ihn erst mit Weg frei.
4. **[EditMode]** **GIVEN** Abend, frei sind je 1 Sitz an Tisch A und an Tisch B sowie 2 Betten **WHEN** mit
   u = 0,5 bzw. 0,7 gezogen wird **THEN** fällt die Familie aus der Ziehung (keine 2 Sitze an einem Tisch).
   Es kommt ein Wanderer bzw. ein Bergsteiger (Gewichte 2:1; 0,5 × 3 = 1,5 < 2; 0,7 × 3 = 2,1). **GIVEN**
   2 Sitze an einem Tisch, aber 0 Betten **THEN** fällt die Familie ebenso aus der Ziehung.

### B. Ankunft (Regeln 5, 11; Formel 1)

5. **[EditMode]** **GIVEN** (n; Phase; Druck) **WHEN** `arrival_rate` berechnet wird **THEN** ergibt sich
   (Gäste pro Minute):
   - (4; Abend; 0,4) → 0,38
   - (2; Abend; 0,4) → 0,285
   - (1; Abend; 0,4) → 0,19
   - (4; Nacht; 1) → 0,16
   - (1; Nachmittag; 0) → 0,05
   - (4; Abend; 0,6) → 0,52
   - (3; Nachmittag; 0,1) → 0,153

   **GIVEN** Vorbereitung, Bewertung oder Upgrades **WHEN** 1000 s vergehen **THEN** bleibt das Budget
   gleich, und es gibt 0 Ankünfte.
6. **[EditMode]** **GIVEN** Budget 0,6, `next_cost` 1, Rate 0,38/min **WHEN** ein Schritt mit dt = 30 s läuft
   **THEN** steht das Budget bei 0,79, kein Gast ist fällig. **WHEN** ein weiterer Schritt mit dt = 60 s
   läuft **THEN** steht es bei 1,0 (gekappt, nicht 1,17), und ein Gast ist fällig. Nach der Ankunft ist das
   Budget 0, und `next_cost` wird mit r = 0,5 / −1 / +1 zu 1,2 / 0,6 / 1,4. **WHEN** „Saison beginnt“ bei
   Budget 0,95 und `next_cost` 1,4 kommt **THEN** gelten 0,6 und 1.
7. **[EditMode]** **GIVEN** Nachmittag, n = 4, Druck fest 0,2 (also 0,24/min), alle Sitze belegt, eine
   Ankunft fällig **WHEN** 300 s vergehen **THEN** gibt es 0 Ankünfte, und das Budget bleibt bei 1,0 (nicht
   2,2). **WHEN** ein Wanderer den Zustand Weg erreicht **THEN** kommt im nächsten Schritt genau 1 Gast, das
   Budget ist 0, und in den folgenden 120 s kommt keiner mehr (0,48 < 0,6).
8. **[EditMode]** **GIVEN** Nacht, n = 4, Druck 1, Einsatz-Stub „läuft“, Budget 0,9, `next_cost` 1, 1 Sitz
   frei **WHEN** Schritte von 0,37 s laufen **THEN** kommt genau 1 Bergsteiger im 102. Schritt (bei 37,74 s;
   nach 101 Schritten steht das Budget erst bei 0,99965). **GIVEN** Nacht, alle Sitze belegt, eine Ankunft
   fällig **WHEN** „Nacht vorbei“ kommt **THEN** verfällt sie, danach gibt es 0 Ankünfte.
9. **[EditMode]** **GIVEN** alle Plätze frei **WHEN** gezogen wird **THEN** gilt:
   - Nachmittag: u = 0 / 0,7499 / 0,75 / 0,9999 → Wanderer / Wanderer / Familie / Familie
   - Abend: u = 0,39 / 0,41 / 0,81 → Wanderer / Familie / Bergsteiger
   - Nacht: immer Bergsteiger
   - Mit `System.Random(12345)` und 10 000 Ziehungen am Nachmittag: Anteil Wanderer 0,75 ±0,02, kein
     Bergsteiger.
10. **[PlayMode]** **GIVEN** eine Testszene mit Ankunftspunkt, Vorraum und 1 Sitz, Weg 15 m **WHEN** ein Gast
    fällig wird **THEN** erscheint er am Ankunftspunkt (±0,1 m) und steht nach höchstens 16 s am Sitz
    (15 m bei 1,4 m/s plus 5 s Puffer; ±0,3 m). Unterwegs ist er keine Station. `need_start_time` ist der
    Zeitpunkt, an dem er den Sitz erreicht (±0,37 s). **GIVEN** eine Testszene ohne Ankunftspunkt **THEN**
    kommen 0 Gäste, und im Log steht genau 1 Fehler (`LogAssert.Expect`).
11. **[EditMode]** *(Grenzfall Verbindungsabbruch)* **GIVEN** Saisonstart mit n = 2 **WHEN** im Abend nur noch
    1 Spieler verbunden ist **THEN** bleibt die Rate bei Druck 0,4 bei 0,285 (nicht 0,19), und ein warmer Tee
    bei S_before 80 bringt 5 Münzen (nicht 6). **WHEN** die nächste Saison solo startet **THEN** gelten 0,19
    und 6.

### C. Bedürfnis, Servieren, Trinkgeld (Regeln 6, 12, 14; Formeln 2, 3, 7, 8)

12. **[EditMode]** **GIVEN** (Bedürfnis; Druck beim Anzeigen) **WHEN** `patience_total` berechnet wird
    **THEN** ergibt sich:
    - (Wanderer Tee; 0) → 150
    - (Wanderer Tee; 0,4) → 132
    - (Kaiserschmarrn; 0,4) → 184,8
    - (Bett; 0,5) → 102
    - (Bergsteiger Tee; 1) → 84
    - (Erste Hilfe; 1) → 70

    Der Ring zeigt bei 72 von 132 s den Wert 0,545 und bei −5 s den Wert 0. **WHEN** der Druck danach auf 0,6
    steigt **THEN** bleibt `patience_total` bei 132.
13. **[EditMode]** **GIVEN** n = 4, Nachmittag bis 700,0, Druck 0, Wärme 50, ein Wanderer sitzt ab 100,0
    **WHEN** er den Tee warm bei 130,0 bekommt und das Speckbrot warm 30 s nach dessen Erscheinen **THEN**:
    - Tee: Gewinn 18 (S 60 → 78), 3 Münzen
    - Er genießt bis 138,0, dann erscheint das Speckbrot mit 150 s Geduld.
    - Speckbrot bei 168,0: Gewinn 18 (S 96), 6 Münzen (5,85 gerundet)
    - Nach 6 s Genießen wechselt er zu Geht.
    - Der Kassen-Stub bekommt 2 Aufrufe, zusammen 9 Münzen.
14. **[EditMode]** **GIVEN** n = 4, ein Wanderer, Tee mit 150 s ab 100,0, Wärme 50 **WHEN** Schritte bei 249,9
    und 250,2 laufen **THEN** wartet er erst mit S 60 und mault dann: S 0, Stimmung mault, das Bedürfnis
    bleibt Tee, genau 1 Murren. **WHEN** er bei 300,0 einen warmen Tee bekommt **THEN** ist S 25, es gibt
    0 Münzen, die Stimmung ist neutral, und nach 8 s erscheint das Speckbrot mit vollen 150 s. Mit einem
    kalten Tee stattdessen: S 12,5, die Stimmung bleibt mault.
15. **[EditMode]** *(Grenzfall Servieren im selben Schritt wie der Geduldsablauf)* **GIVEN** wie in
    Kriterium 14, letzter Schritt bei 249,9 **WHEN** der Schritt bei 250,2 zugleich einen warmen Tee bringt
    **THEN** zählt das Servieren als rechtzeitig: Gewinn 10 (Rest 0), S 70, 3 Münzen (S_before 60), Stimmung
    zufrieden. Der Gast ist nie im Zustand Mault, es gibt 0 Murren.
16. **[EditMode]** **GIVEN** ein Wanderer wartet auf Tee **WHEN** ihm ein Speckbrot serviert wird **THEN**
    lehnt er ab mit genau „Möchte einen Tee“, das Gericht bleibt in der Hand, S bleibt gleich.
    - **GIVEN** ein Bergsteiger wartet auf Erste Hilfe **WHEN** ihm ein Tee serviert wird **THEN** lehnt er
      ab.
    - **GIVEN** ein Gast in Kommt an, Blockiert, Genießt, Bleibt, Geht zum Bett oder Schläft **WHEN** ihm
      etwas serviert wird **THEN** wird abgelehnt.
    - Als Station bietet der Gast nur „Servieren“ und „Erste Hilfe“ an: kein Aufheben, kein Wegschicken,
      keine Platzwahl.
    - Pro Schritt wird höchstens 1 Bedürfnis je Gast erfüllt.
    - Gericht zerbricht beim Servieren: siehe `gegenstandsarten.md`, Kriterium 31.
17. **[EditMode]** *(Grenzfall gleichzeitiges Servieren)* **GIVEN** ein Wanderer wartet auf Tee **WHEN** in
    einem Host-Schritt [Spieler 1 mit warmem Tee, Spieler 2 mit warmem Tee] ankommen **THEN** erfüllt
    Spieler 1 das Bedürfnis, sein Gericht ist entfernt. Spieler 2 wird mit Grund `not-station` abgelehnt und
    behält seinen Tee. Es gibt genau 1 Gewinn und 1 Trinkgeld. In umgekehrter Reihenfolge erfüllt Spieler 2.
18. **[EditMode]** **GIVEN** (Gericht; Zustand vorher; Rest) **WHEN** `fulfill_gain` berechnet wird **THEN**
    ergibt sich:
    - (warm; Wartet; 72/132) → 15,45
    - (kalt; Wartet; 72/132) → 7,73
    - (warm; Wartet; 1) → 20
    - (warm; Wartet; 0) → 10
    - (kalt; Wartet; 0) → 5
    - (warm; Mault) → 25
    - (kalt; Mault) → 12,5
    - (Erste Hilfe, kein Gericht; 0,5) → 15
    - S 95 + 20 wird auf 100 begrenzt.
19. **[EditMode]** **GIVEN** eine Familie, Bett-Bedürfnis ab 500,0 (120 s), S 80, n = 2, Wärme 50 **WHEN** die
    Versorgung bei 530,0 Bett 1 meldet **THEN** gibt es noch keinen Gewinn, die Familie wartet. **WHEN** bei
    560,0 Bett 2 kommt **THEN** gibt es einmal Gewinn 15 (Rest 0,5) und 8 Münzen (3 × 2 × 1,25 = 7,5),
    0 s Genießen, dann Geht zum Bett und Schläft.
    - Ein nicht reserviertes Bett zählt nicht.
    - Ein gezähltes Bett, das wieder ungemacht wird, ändert nichts.
    - **GIVEN** beide Betten waren schon gemacht **WHEN** das Bett-Bedürfnis erscheint **THEN** ist es sofort
      erfüllt, mit Gewinn 20 (Rest 1).
20. **[EditMode]** **GIVEN** (Bedürfnis; warm oder kalt; S_before; n) **WHEN** `tip_coins` berechnet wird
    **THEN** ergibt sich:
    - (Kaiserschmarrn; kalt; 60; 2) → 6 (5,625)
    - (Tee; warm; 80; 1) → 6
    - (Tee; warm; 100; 4) → 4
    - (Tee; warm; 50; 4) → 3 (2,5 rundet auf; `Mathf.Round` ergäbe 2)
    - (Tee; kalt; 5; 4) → 1 (0,125, Mindestbetrag)
    - (Kaiserschmarrn; warm; 80; 1) → 18 (nur die Formel; im Spiel beim ersten Bedürfnis nicht erreichbar)
    - (2 Betten; 80; 3) → 7 (6,6)
    - (Erste Hilfe; 60; 4) → 6
    - S_before 0 → 0
    - Zustand vorher Mault → 0
21. **[EditMode]** **GIVEN** (Bedürfnis; folgt noch eins?) **WHEN** `enjoy_total` berechnet wird **THEN**
    ergibt sich: (Tee; ja) → 8, (Speckbrot; nein) → 6, (Kaiserschmarrn; ja) → 12, (Erste Hilfe; ja) → 9,
    (Tee; nein) → 4, (Bett; nein) → 0. **GIVEN** ein Bergsteiger bekommt bei 300,0 Erste Hilfe **THEN**
    genießt er bei 308,9 noch (Servieren wird abgelehnt), und bei 309,1 zeigt er den Tee mit
    `need_start_time` 309,0.

### D. Bleiben, Gehen, Saisonwechsel (Regeln 7, 13)

22. **[EditMode]** **GIVEN** der Abend beginnt bei 700,0 **WHEN** ein Wanderer sein Speckbrot (6 s Genießen)
    bei 690,0 bekommt **THEN** wechselt er ab 696,0 zu Geht und am Ankunftspunkt zu Weg. Sein Sitz ist frei,
    sein S ist festgehalten. **WHEN** er es stattdessen bei 697,0 bekommt **THEN** endet das Genießen bei
    703,0 im Abend: Er bleibt, und sein Sitz bleibt belegt. **GIVEN** ein Wanderer ist bei 700,0 schon in
    Geht **THEN** geht er weiter bis Weg.
23. **[EditMode]** **GIVEN** eine Familie bekommt ihre Betten am Nachmittag **WHEN** die Saison bis „Nacht
    vorbei“ läuft **THEN** bleibt sie im Zustand Schläft, ihre Sitze und Betten bleiben belegt. Bei Wärme 0
    verliert sie in 10 s 4,0 Punkte und wacht nicht auf.
24. **[EditMode]** *(Grenzfall Nacht vorbei)* **GIVEN** je ein Gast in Wartet (40 s Geduld übrig), Mault,
    Bleibt, Schläft und Vermisst (S 70), dazu ein Wanderer, der am Nachmittag mit S 88 gegangen ist **WHEN**
    „Nacht vorbei“ kommt **THEN** bekommt der Bewertungs-Stub genau 6 Einträge mit den aktuellen S-Werten, 88
    für den Gegangenen und 70 samt Zustand Vermisst. **WHEN** danach 600 s bei Wärme 0 mit Lärm und Treffern
    vergehen **THEN** bleiben S, Geduld und Zustände unverändert, Servieren wird abgelehnt, es gibt 0 Münzen
    und 0 Ankünfte, und alle Figuren sind noch da.
25. **[EditMode]** **GIVEN** 6 Gäste in verschiedenen Zuständen, 7 belegte Plätze, Budget 0,95 **WHEN**
    „Saison beginnt“ kommt (auch nach einem Abbruch) **THEN** gibt es 0 Gäste und 0 Reservierungen, Budget
    0,6, `next_cost` 1, und `n` ist neu gesetzt.
    - *(Grenzfall Abbruch vor der Nacht)* **GIVEN** Abend, Wärme 0, drei Gäste (Wartet mit 40 s Geduld, Mault,
      Genießt) **WHEN** die Phase durch einen Abbruch zur Vorbereitung wechselt, ohne „Nacht vorbei“, und
      600 s vergehen **THEN** bleiben S, Geduld und Zustände unverändert, Servieren wird abgelehnt, es gibt
      0 Münzen, 0 Ankünfte und 0 Aufrufe an den Bewertungs-Stub, und alle Figuren sind noch da.
    - Dasselbe gilt in Bewertung und Upgrades nach einer normalen Nacht.
26. **[Manuell]** *(Grenzfall Pause solo)* **GIVEN** solo, Spielprofil, per Debug-Befehl ein wartender
    Wanderer (Ring etwa halb leer) und ein zweiter auf dem Weg, Wärme unter 40 **WHEN** das Esc-Menü 20 s
    offen ist **THEN** zeigt die Debug-Anzeige danach dieselben Werte für `patience_left` und S wie davor
    (±0,01), beide Figuren stehen in dieser Zeit still, und danach läuft alles weiter.
27. **[Manuell]** **GIVEN** solo, Testprofil **WHEN** man am Nachmittag „Gast wanderer“ auslöst und nichts
    serviert **THEN**:
    - Der Gast erscheint unten am Ankunftspunkt und geht durch den Vorraum zu einem Sitz.
    - Tee-Symbol und Ring zeigt er erst am Sitz.
    - `left` in der `need`-Zeile ist 150 × (1 − 0,3 × Druck) ±0,01.
    - Nach dieser Zeit mault er hörbar.
    - Bei „Nacht vorbei“ steht eine `freeze`-Zeile im Log. Einen Tee lehnt er danach ab, er bleibt sichtbar.
    - Nach „Saison starten“ ist er weg.

### E. Stimmung (Regel 8; Formeln 4, 5, 6)

28. **[EditMode]** **GIVEN** (Ort; Wärme; Zustand; S; blocked_time) **WHEN** `satisfaction_rate` berechnet
    wird **THEN** ergibt sich (Punkte pro Sekunde):
    - (drinnen; 70; Wartet; 50) → +0,03
    - (drinnen; 70; Wartet; 85) → 0
    - (drinnen; 70; Mault) → 0
    - drinnen bei Wärme 50 / 40 / 20 / 0 → 0 / 0 / −0,2 / −0,4
    - (draußen am Hang; 0) → 0
    - (Vermisst; 0) → 0
    - (Blockiert drinnen; 50; 4,9) → 0
    - (Blockiert drinnen; 50; 5,1) → −0,2
    - (Blockiert drinnen; 0; 5,1) → −0,6
    - (Blockiert draußen; 0; 5,1) → −0,2
    - (Blockiert drinnen; 70; S 50; 5,1) → −0,2 (kein Wärmebonus)
29. **[EditMode]** **GIVEN** ein Gast in Bleibt, drinnen, S 90, ein Wärme-Stub mit max(0; 100 − 1,5 t),
    Schritte von 0,02 s **WHEN** 40 s / 66,68 s / 120 s vergangen sind **THEN** ist S 90,00 / 84,67 / 63,33
    (±0,05).
30. **[EditMode]** *(Grenzfall `blocked_max`)* **GIVEN** ein Gast in Kommt an, drinnen, Wärme 50, S 60
    **WHEN** er in 0,37-s-Schritten blockiert ist **THEN** mault er ab blocked_time > 5 s. Im ersten Schritt
    über 90 s kommt `squeeze`, und er ist wieder in Kommt an, mit S 43,0 ±0,1 (85 s × 0,2). Die 17 Punkte
    bleiben abgezogen, die Stimmung ist danach neutral.
31. **[EditMode]** **GIVEN** (Masse; Aufprall; Abstand) **WHEN** `noise_penalty` berechnet wird **THEN**
    ergibt sich:
    - (10 kg; 8 m/s; 3 m) → −1,875
    - (10 kg; 8 m/s; 7 m) → −0,375
    - (10 kg; 8 m/s; 8 m) → 0
    - (12 kg; 5,01 m/s; 1 m) → 0
    - (2 kg; 5,5 m/s; 0 m) → −3
    - (1,9 kg; 8 m/s; 1 m; heil) → 0
    - zerbrochene Tasse in 4 m → −1,5
    - Aufprall draußen, Gast drinnen in 5 m Luftlinie → −1,125
    - Gast draußen → 0
    - Sperrzeit: Abzug bei 10,0; bei 13,9 → 0; bei 14,1 → wieder Abzug.
    - Ein Ereignis in 9 m bei 20,0 ergibt 0 und startet keine Sperrzeit: bei 20,5 in 2 m → −2,25.
32. **[EditMode]** **GIVEN** ein Treffer **WHEN** `chaos_penalty` berechnet wird **THEN** ergibt sich:
    - (Fällt; 4 m/s; drinnen) → −15
    - (Fällt; 2,9 m/s) → 0
    - (Fällt; 3,0 m/s) → −15
    - (Getragen; 4 m/s) → 0
    - draußen → 0
    - Sperrzeit: −15 bei 10,0; bei 12,9 → 0; bei 13,1 → −15.
    - Eine Kiste (10 kg, 8 m/s) trifft Gast A, Gast B steht 3 m daneben: A −15 und kein Lärm, B −1,875.
    - Eine Kiste (10 kg) trifft Gast A mit 2,9 m/s oder in seiner Sperrzeit: A bekommt weder Chaos noch Lärm.
    - S 10 fällt auf 0, nicht darunter. Ein Tee danach bringt 0 Münzen (S_before 0), der Gewinn kommt normal.
33. **[EditMode]** *(Grenzfall Familie)* **GIVEN** eine Familie mit S 80, Wärme 50 **WHEN** das Elternteil
    bei 10,0 und das Kind bei 11,0 getroffen wird **THEN** ist S 65 (einmal −15). **WHEN** das Kind bei 13,5
    erneut getroffen wird **THEN** ist S 50. **GIVEN** ein Wurf trifft bei 20,0 die Familie und einen
    Wanderer **THEN** zahlen beide je −15.
    - Ein lauter Aufprall (10 kg, 8 m/s) liegt 3 m vom Kind und 6 m vom Elternteil entfernt: einmal −1,875
      (nähere Figur).
    - Servieren am Kind erfüllt das Bedürfnis genauso wie am Elternteil.
34. **[EditMode]** **GIVEN** ein wartender Gast mit S in dieser Folge **WHEN** die Stimmung berechnet wird
    **THEN** ergibt sich: 60 → neutral; 75,5 → zufrieden; 61 → zufrieden; 60 → zufrieden; 59,9 → neutral;
    65 → zufrieden; 19,9 → mault; 24,9 → mault; 25 → neutral; 20 → neutral. Im Zustand Mault (S 0) ist die
    Stimmung mault. Blockiert mit S 80 und blocked_time 5,1 ergibt mault, nach dem Freiwerden mit S 79
    zufrieden.
35. **[PlayMode]** **GIVEN** Host, Testszene, ein Gast sitzt drinnen **WHEN** ihn eine Kiste mit etwa 8 m/s
    trifft **THEN** gibt es genau 1 `chaos` (−15) und 1 „Aua“, auch wenn der Kontakt mehrere Physikschritte
    dauert. **WHEN** eine Kiste aus Haltehöhe neben ihm abgelegt wird **THEN** gibt es 0 `noise`. **WHEN** ein
    Spieler mit einer getragenen Kiste in ihn läuft **THEN** gibt es 0 `chaos`. **GIVEN** ein Gast draußen am
    Hang wird getroffen **THEN** bleibt sein S gleich.

### F. Wege (Regel 9)

Die Kriterien sind technikneutral: Das Paket AI Navigation ist nicht installiert, die ADR „Wegfindung im
Netz“ steht noch aus.

36. **[PlayMode]** **GIVEN** ein Testraum von 6 × 6 m, der Sitz gegenüber der Tür, eine ruhende Kiste auf der
    geraden Linie **WHEN** ein Gast zum Sitz geht **THEN** erreicht er ihn, ohne blockiert zu sein und ohne
    die Kiste zu berühren (0 Kontakte). Die Kiste bewegt sich weniger als 0,01 m.
37. **[PlayMode]** **GIVEN** eine ruhende Truhe versperrt die einzige Tür (1 m) **WHEN** ein Gast zum Sitz
    will **THEN** ist er innerhalb von 1 s blockiert, und es gibt eine `blocked`-Zeile. **WHEN** die Truhe
    weggenommen wird **THEN** ist er innerhalb von 1 s wieder unterwegs und erreicht den Sitz. Dasselbe gilt
    in diesen Fällen:
    - Ein Gegenstand kommt erst während des Gehens auf dem Weg zur Ruhe: Der Weg wird neu geplant, ohne
      Ausweg ist der Gast blockiert.
    - Ein Gegenstand liegt auf seinem reservierten Sitz: blockiert, bis der Sitz geräumt ist.
    - Auf dem Weg zum Bett und auf dem Rückweg zum Ankunftspunkt.
38. **[PlayMode]** *(Grenzfall Vorbeizwängen)* **GIVEN** eine Test-Einstellung mit `blocked_max` 3 s, die Tür
    ist versperrt **WHEN** 3 s vergangen sind **THEN** geht der Gast durch die Truhe hindurch zum Sitz. Die
    Truhe bewegt sich weniger als 0,01 m und dreht sich weniger als 0,1°, und ein zweiter Gast wird an ihr
    wieder blockiert.
    - *(Grenzfall Gegenstand auf dem Platz)* **GIVEN** eine Kiste steht auf seinem reservierten Sitz,
      `blocked_max` 3 s **WHEN** 3 s vergangen sind **THEN** sitzt er am Sitz (±0,3 m), die Kiste bewegt sich
      weniger als 0,01 m, und er zeigt sein Bedürfnis als Station. Dasselbe gilt für ein reserviertes Bett.
39. **[PlayMode]** **GIVEN** eine Spielerfigur steht 10 s in der einzigen Tür **WHEN** ein Gast hindurch will
    **THEN** ist er nie im Zustand Blockiert, sein S bleibt gleich (Wärme 50), und danach geht er durch.
    **GIVEN** ein Gast sitzt **WHEN** eine Spielerfigur 2 s gegen ihn läuft **THEN** sitzt er weiter an
    seinem Platz (±0,05 m).

### G. Vermisster Gast (Regel 10)

40. **[EditMode]** **GIVEN** Nacht, anwesend sind eine Familie (Wartet), ein Bergsteiger (Bleibt) und ein
    Wanderer (Bleibt) **WHEN** der Einsatz-Stub „Gast vermisst“ meldet **THEN** wird der Wanderer gewählt.
    Ohne Wanderer wird der Bergsteiger gewählt, nie die Familie oder ihr Kind.
    - Bei zwei Wanderern wird der zuerst angekommene gewählt.
    - Ein Wanderer in Kommt an, Blockiert, Geht zum Bett, Geht oder Schläft wird nie gewählt, einer in
      Genießt schon.
41. **[EditMode]** **GIVEN** der gewählte Wanderer wartet auf sein Speckbrot, S 70, Sitz an Tisch B **WHEN**
    er vermisst wird **THEN**:
    - Er ist im Zustand Vermisst, die Figur ist aus der Hütte verschwunden, er hat kein Bedürfnis mehr, sein
      Sitz bleibt belegt.
    - Nach 300 s bei Wärme 0 und Lärm in 1 m ist S weiter 70.
    - **WHEN** bei Druck 0,8 „Gast ist zurück“ kommt **THEN** sitzt er sofort wieder an seinem Sitz
      (±0,1 m), braucht Erste Hilfe (76 s), danach Tee, und S läuft ab 70 weiter.
    - **WHEN** die Frist stattdessen abläuft **THEN** bleibt er vermisst, und die Bewertung bekommt 70 und den
      Zustand Vermisst.
42. **[EditMode]** *(Grenzfall Ersatzgast)* **GIVEN** nur eine Familie ist da, 0 Sitze sind frei **WHEN**
    „Gast vermisst“ kommt **THEN** entsteht ein Ersatz-Wanderer im Zustand Vermisst, ohne Sitz-Reservierung
    (die freien Sitze bleiben gleich). **WHEN** er zurück ist **THEN** sitzt er auf dem Ofenplatz, mit S 60,
    und braucht Erste Hilfe, dann Tee. Der Ofenplatz zählt nie als freier Sitz für Ankünfte.

### H. Netz und Leistung (Regel 15)

43. **[PlayMode + Codeprüfung]** **GIVEN** Host und 1 Client im selben Testprozess (wie in
    `gegenstandsarten.md`, Kriterium 32) **WHEN** ein Gast 10 s lang läuft und ein Bedürfnis zeigt **THEN**
    stimmen beim Client Position (±0,1 m nach dem Anhalten), Blickrichtung, Zustand, Bedürfnis, Geduldsring
    (±0,02) und Stimmung mit dem Host überein, und der Client berechnet 0 Wege. *Codeprüfung:* Die
    NetworkVariables der Gäste schreibt nur der Server. S, `arrival_budget` und die Reservierungen werden
    nicht übertragen.
44. **[PlayMode]** **GIVEN** Host und Client, der Client hält ein Speckbrot vor einem Wanderer, der Tee will
    **WHEN** der Client „Servieren“ schickt **THEN** lehnt der Host ab, der Client sieht „Möchte einen Tee“
    und behält das Speckbrot. Mit einem Tee erfüllt der Host das Bedürfnis, und das Gericht ist bei beiden
    weg.
45. **[Freundestest]** **GIVEN** Testprofil, Debug-Befehl „Gast familie“ **WHEN** beide zusehen **THEN** gehen
    beide Figuren auch beim Freund ohne Sprünge über 0,5 m (außer beim Erscheinen) zu denselben Sitzen.
    Symbol, Ring und Gesicht stimmen bei beiden überein. Jede `need-received`-Zeile des Freundes kommt
    höchstens 0,5 s (ServerTime) nach der passenden `need`-Zeile des Hosts.
46. **[Freundestest]** *(Grenzfall gleichzeitiges Servieren)* **GIVEN** beide halten einen warmen Tee vor
    demselben Wanderer **WHEN** sie auf Kommando gleichzeitig servieren (5 Versuche) **THEN** ist jedes Mal
    genau 1 Tee weg, der andere bleibt in der Hand. Im Host-Log stehen 1 `serve` und 1
    `reject reason=not-station`, und das Trinkgeld kommt einmal.
47. **[Freundestest]** **WHEN** der Freund einen Tee serviert **THEN** ist der Tee bei beiden weg, beide hören
    das Münzklimpern, und im Host-Log steht `serve client=1`. **WHEN** der Freund eine Kiste auf einen Gast
    wirft **THEN** hören beide genau 1 „Aua“, und im Host-Log steht 1 `chaos`.
48. **[Freundestest]** *(Grenzfall Spielerzahl)* **GIVEN** eine Saison zu zweit **WHEN** der Freund im Abend
    das Spiel hart beendet **THEN** zeigen alle `[Guest]`-Zeilen bis „Nacht vorbei“ `n=2`. Die nächste
    Saison solo zeigt `n=1`.
49. **[Manuell]** *(Leistung)* **GIVEN** ein Development Build auf dem Rechner des Entwicklers, Host solo,
    eine Testszene mit 12 Gast-Figuren (10 Gäste + 2 Familienfiguren), die gleichzeitig laufen, und eine
    Kiste wird in die Stube geworfen **WHEN** man 60 s misst (ProfilerMarker `Guests.Step` und `Guests.Path`
    oder eine `[Perf]`-Zeile alle 10 s) **THEN** braucht das Gäste-System (Logik, Wegfindung,
    Hindernis-Updates) im Mittel ≤ 1,0 ms pro Frame. Im Frame, in dem die Kiste zur Ruhe kommt und alle neu
    planen, sind es höchstens 2,0 ms. Nach dem Erscheinen erzeugt das Gäste-Update 0 B GC-Allokation pro
    Frame. Beim Client höchstens 0,2 ms (keine Wegfindung, nur Interpolation).

    *Begründung:* 60 fps lassen 16,6 ms pro Frame. Physik und Netz sind die großen Posten, die Gäste nehmen
    höchstens etwa 6 % davon. Ein Wert für Mindest-Hardware folgt mit den Systemanforderungen.

**Annahmen des `qa-lead`, beim Bau zu bestätigen:**

- Log-Format und Inhalt der Debug-Anzeige, Debug-Befehl „Gast <typ>“
- Test-Hütte (2 + 3 Sitze, 2 Betten, Ofenplatz)
- Die Ziehung läuft kumulativ in Listenreihenfolge.
- Ein neues Bedürfnis beginnt ohne Versatz: `need_start_time` ist das Ende des Genießens, nicht die Zeit des
  Schritts (wie die Phasenwechsel im Saison-Ablauf).
- Rundung mit `floor(x + 0,5)`
- Toleranzen: ±0,05, ±0,1, ±0,37 s; im Netz 0,5 s, 0,5 m, 0,1 m, Ring ±0,02
- Test-Einstellung `blocked_max` = 3 s
- NGO-Testhilfen (`NetcodeIntegrationTest`) für Host und Client in einem Prozess. Ob es sie in NGO 2.13.3
  gibt, wird beim Bau geprüft. Ein `testables`-Eintrag in `Packages/manifest.json` nur nach OK des
  Entwicklers.

## Open Questions

| # | Frage | Wer klärt | Bis wann |
|---|---|---|---|
| 1 | Wann geht der Gast verloren, und was braucht er nach der Rückkehr? Vorläufig: Erste Hilfe, dann Tee. | Nachteinsatz-GDD | bei dessen Design |
| 2 | Kochstellen und Kochzeiten: Die Geduld setzt etwa 45–60 s für Kochen und Weg voraus. | Kochen-GDD | bei dessen Design |
| 3 | Wie wird aus Zufriedenheit die Edelweiß-Wertung, und zählen Edelweiß als Geld? Empfehlung `economy-designer`: nein. KONZEPT nennt „Trinkgeld und Bewertungen“. | Bewertung-GDD, Hüttenkasse-GDD | bei deren Design |
| 4 | Upgrade-Preise in der Größenordnung von 20–90 Münzen | Hüttenkasse-GDD | bei dessen Design |
| 5 | Paket „AI Navigation“ (`com.unity.ai.navigation`), NavMesh mit bewegten Gegenständen, Abgleich im Netz | Entwickler (Paket-OK), `/architecture-decision` (ADR „Wegfindung im Netz“) | vor dem Bau |
| 6 | Die Navigations-Referenz in `docs/engine-reference/unity/` ist für 6.6 nicht geprüft. | `unity-specialist` | vor dem ADR |
| 7 | Die Interaktion schätzt im Beispiel 8 Betten pro Saison, hier sind es höchstens 4 (zwei Familien). | Versorgung-GDD | bei dessen Design |
| 8 | Ist der Andrang zu viert zu entspannt? Spürt man die Nacht mit `phase_rate_scale` 0,2? | Playtest | Freundestest mit Gästen |
| 9 | Genaue Zahl der Sitzplätze und Betten (angenommen: 10 und 4) | Dokument „Hütten-Layout“ | vor Schritt 6 im Plan |
| 10 | Fühlt sich Maulen fair an? Es setzt S auf 0, egal wie gut es vorher lief (danach höchstens 25 plus Wärmebonus). Reicht dem Bergsteiger nachts der Tee mit 84 s (nur das 1,4- bis 1,9-Fache von Kochen und Weg)? | Playtest, danach Bewertung-GDD | Freundestest mit Gästen |
