# Gegenstandsarten

> **Status**: Approved (erneutes Design-Review 2026-11-08 nach dem Nachtrag Spielzeit `game_time`; siehe `design/gdd/reviews/gegenstandsarten-review-log.md`)
> **Author**: Entwickler + Claude
> **Last Updated**: 2026-11-08
> **Last Verified**: 2026-11-08
> **Implements Pillar**: Physik-Chaos (alles hat Gewicht), Teamwork unter Druck (schwere Dinge nur zu zweit)

## Summary

Gegenstandsarten ist der Katalog aller Dinge zum Anfassen in Schutzhütte, vom Holzscheit bis zur Truhe:
Gewicht, Trageart (allein oder zu zweit) und Eigenschaften wie brennbar, Vorratspaket, Gericht oder
zerbrechlich, dazu die Waren im Lager. Spieler spüren ihn als Gewicht beim Tragen, als Teller, der beim
Wurf gegen die Wand zerbricht, und als Tee, der kalt wird. Er existiert, damit jeder Wert genau einmal
steht und Tragen, Ofen, Lager, Kochen und Gäste dieselben Daten lesen.

> **Quick reference** — Layer: `Foundation` · Priority: `Vertical Slice` · Key deps: `None`

## Overview

**Gegenstandsarten** ist der Katalog aller Dinge, die man in Schutzhütte anfassen kann: Holz, Kisten,
Zutaten, Gerichte, Werkzeug, die Truhe und die Trage. Jede Art ist ein Datenobjekt mit Namen, Gewicht
und Eigenschaften, zum Beispiel ob sie brennt, ob sie eine Zutat ist und ob man sie allein tragen kann
oder nur zu zweit. Die Spieler sehen das System nicht direkt. Sie spüren, was es festlegt: wie schwer
etwas ist, ob es eine zweite Person braucht und wofür es taugt.

Andere Systeme lesen den Katalog, statt eigene Werte zu führen:

- Das Tragen nimmt das Gewicht.
- Der Ofen nimmt die Brenndauer.
- Das Lager nimmt Ware und Portionen der Pakete.
- Das Kochen nimmt die Waren.
- Die Gäste nehmen die Gerichte.

Ohne den Katalog stünde jeder Wert mehrfach im Code. Eine Gewichtsänderung müsste dann an mehreren
Stellen passieren, das verbietet Architektur-Regel 4 („Daten in ScriptableObjects“).

Über das Netz wird eine Art nur über ihre feste Kennung verschickt, nie als Objekt. Wie Katalog und
Kennung technisch umgesetzt werden, legt ADR-0002 fest
(`docs/architecture/adr-0002-gegenstandsarten-katalog.md`).

## Detailed Design

### Core Rules

1. **Jede Gegenstandsart mit Körper ist ein Datenobjekt** mit diesen Pflichtfeldern:
   - **Kennung:** fest und eindeutig, zum Beispiel `holzscheit`. Sie ändert sich nie und wird übers
     Netz verschickt. Ist eine Kennung doppelt vergeben (auch zwischen Arten und Waren), verweigern
     Build und Host den Start, bis es behoben ist.
   - **Anzeigename** auf Deutsch.
   - **Masse in kg**, größer als 0. Sie ist die einzige Quelle für das Gewicht: Beim Erscheinen in der
     Welt übernimmt der Rigidbody diese Masse.
   - **Trageart:** `allein` oder `zu zweit`. Eine Art mit `zu zweit` bewegt sich nur, wenn zwei
     Spieler sie halten, so wie heute die Truhe.
   - **Kategorie**, genau eine: Brennstoff, Rohstoff, Vorratspaket, Gericht, Werkzeug oder Sperrgut.
   - **Aufprallklang:** Holz oder Weich. Heute gibt es nur eine Aufprallgruppe (Holz) plus eine dumpfe
     Schicht nach Gewicht, die die `impactSoft`-Clips nutzt. Die Klanggruppe je Art ist neue Arbeit in
     den Geräuschen.
   - **Prefab:** das Netzobjekt, das in der Welt erscheint.
2. **Dazu kommen optionale Eigenschaften**, jede mit eigenen Werten:
   - **Brennbar:** Brenndauer in Sekunden. Der Ofen liest sie.
   - **Hackbar:** Welche Art und wie viele Stück beim Hacken entstehen.
   - **Vorratspaket:** Wie viele Portionen das Paket enthält, dazu optional eine feste Ware (der
     Kanister enthält immer `ware_wasser`). Ohne feste Ware legt fest, wer das Paket erzeugt
     (Startaufstellung, Seilbahn), welche Ware drin ist. Die Ware ist dann eine Eigenschaft des
     einzelnen Gegenstands, die der Host als Kennung übers Netz mitschickt.
   - **Gericht:** Welches Gästebedürfnis es erfüllt (vorläufig, das legt das Gäste-Dokument fest) und
     eine Abkühlzeit in Sekunden. Ohne Abkühlzeit kühlt es nie ab.
   - **Zerbrechlich:** Bruchgeschwindigkeit in m/s. Prallt die Art schneller auf, zerbricht sie.
   - **Werkzeug:** Welche Handlung es ermöglicht. Das legt das Interaktions-Dokument fest.
   - **Ausrüstung:** Platz am Körper, `Kopf` oder `Gürtel`. Eine Art mit dieser Eigenschaft wird beim
     Aufheben angelegt statt in der Hand getragen (Zustand „Am Körper“). Daran und nur daran erkennt der
     Code Ausrüstung, nie am Namen. Die Regeln zum Anlegen und Aufhängen stehen in
     `design/gdd/interaktion.md`.
3. **Waren** wie Speck, Mehl oder Tee sind ein eigener, schlanker Datentyp ohne Körper: nur Kennung
   (immer mit `ware_` davor, zum Beispiel `ware_tee`, damit sie nie mit einer Art wie der Tasse `tee`
   zusammenfällt) und Anzeigename. Es gibt sie nur als Bestand im Lager und als Inhalt eines Pakets.
   Man trägt nie eine einzelne Portion, sondern das Paket.
4. **Gilt allein**, darf die Masse die Höchstmasse für einen Träger nicht überschreiten. Die genaue
   Grenze steht in „Formulas“. Der Katalog prüft das im Editor und meldet einen Verstoß als Fehler.
5. **Ein Gericht ist der fertige Teller oder die Tasse.** Geschirr ist keine eigene Art, ein
   zerbrochenes Gericht ist ganz weg.
6. **Was Spieler nicht können:** Arten umwandeln, außer über die zuständigen Systeme (Hacken, Kochen,
   Auspacken im Lager). Es gibt keine Taschen und kein Inventar. Jeder Gegenstand wird in der Hand
   getragen, die Kraxe kommt erst in der Alpha. **Ausnahme Ausrüstung** (Nachtrag 2026-11-08 aus dem
   GDD Interaktion): Arten mit der Eigenschaft „Ausrüstung“ (im Vertical Slice Stirnlampe und
   Funkgerät) werden beim Aufheben am Körper angelegt, je ein Platz pro Spieler für Kopf und Gürtel,
   damit die Hände für Trage und Kisten frei bleiben. Abnehmen geht nur am Ausrüstungshaken. Die Regeln
   dazu stehen in `design/gdd/interaktion.md`.

**Startkatalog für den Vertical Slice** (Startwerte; die Dokumente der lesenden Systeme dürfen sie
ändern, die Änderung wird dann hier nachgetragen):

| Kennung | Name | Kategorie | Masse | Trageart | Aufprallklang | Eigenschaften |
| --- | --- | --- | --- | --- | --- | --- |
| `holzscheit` | Holzscheit | Brennstoff | 3 kg | allein | Holz | brennbar 45 s *(wie heute)* |
| `holzklotz` | Holzklotz | Rohstoff | 12 kg | allein | Holz | hackbar → 4 Holzscheite |
| `vorratskiste` | Vorratskiste | Vorratspaket | 10 kg *(wie heute)* | allein | Holz | 6 Portionen, Ware beim Erscheinen |
| `wasserkanister` | Wasserkanister | Vorratspaket | 20 kg | allein | Weich | 10 Portionen, fest `ware_wasser` |
| `truhe` | Vorratstruhe | Vorratspaket | 80 kg *(wie heute)* | zu zweit | Holz | 20 Portionen, Ware beim Erscheinen |
| `trage` | Trage | Sperrgut | 15 kg leer | zu zweit | Holz | *vorläufig*, Nachteinsatz |
| `tee` | Tasse Tee | Gericht | 0,5 kg | allein | Weich | zerbrechlich 7,0 m/s, kühlt in 90 s ab |
| `speckbrot` | Speckbrot (Holzbrett) | Gericht | 0,8 kg | allein | Holz | kühlt nicht ab, zerbricht nicht |
| `kaiserschmarrn` | Kaiserschmarrn (Teller) | Gericht | 0,8 kg | allein | Weich | zerbrechlich 7,0 m/s, kühlt in 150 s ab |
| `axt` | Axt | Werkzeug | 2 kg | allein | Holz | Hacken |
| `erste_hilfe` | Erste-Hilfe-Kasten | Werkzeug | 3 kg | allein | Weich | Erste Hilfe |
| `stirnlampe` | Stirnlampe | Werkzeug | 0,3 kg | allein | Weich | Werkzeug `licht`, Ausrüstung Kopf |
| `funkgeraet` | Funkgerät | Werkzeug | 0,5 kg | allein | Weich | Werkzeug `funk`, Ausrüstung Gürtel |

**Waren** (vorläufig, das Kochen-Dokument legt sie fest): `ware_speck`, `ware_brot`, `ware_mehl`,
`ware_eier`, `ware_milch`, `ware_tee`, `ware_wasser`.

### States and Transitions

Jeder Gegenstand in der Welt hat einen dieser Zustände:

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Liegt | Ruht und ist eingefroren (Architektur-Regel 3). Ausrüstung an einem Hakenplatz liegt ebenfalls. | → Getragen (aufheben), → Am Körper (aufheben, nur Ausrüstung, Platz frei), → Fällt (angestoßen) |
| Getragen | Folgt dem Haltepunkt des Trägers | → Fällt (ablegen, werfen, Träger lässt los) |
| Am Körper | Nur Ausrüstung: sitzt auf ihrem Platz (Kopf, Gürtel) an der Figur und folgt ihr; fällt und zerbricht nie, auch nicht beim Sturz des Trägers | → Liegt (am Haken aufgehängt, oder beim Verbindungsabbruch des Trägers an seiner letzten Position) |
| Fällt | Der Host simuliert die Physik | → Liegt (kommt zur Ruhe), → Zerbrochen (nur zerbrechlich, Aufprall schneller als die Bruchgeschwindigkeit) |
| Zerbrochen | Wird entfernt, mit Scherben-Geräusch | Ende |
| Verbraucht | Verbrannt, ausgepackt, gehackt oder serviert | Ende |

Gerichte haben zusätzlich eine Temperatur: **Warm → Kalt**, sobald die Abkühlzeit seit dem Kochen
abgelaufen ist. Die Zeit läuft in jedem der obigen Zustände weiter. Nur der Host zählt sie und
entscheidet über alle Übergänge.

### Interactions with Other Systems

| System | Liest aus dem Katalog | Was dort passiert |
| --- | --- | --- |
| Tragen und Werfen | Masse, Trageart | Tempo und Kamera-Wippen nach Masse. Setzt beim Loslassen den Zustand „Fällt“; den Aufprall misst `ImpactReporter` am Gegenstand, der Host prüft damit das Zerbrechen (ADR-0001). |
| Tragen zu zweit | Trageart `zu zweit` | Truhe und Trage brauchen zwei Träger. |
| Holzofen und Wärme | Brennbar (Brenndauer) | Verbraucht den Gegenstand und verlängert die Brennzeit. |
| Holz hacken | Hackbar (Ergebnis, Anzahl) | Verbraucht den Klotz und erzeugt die Scheite. |
| Lager und Vorräte | Vorratspaket (Ware, Portionen) | Verbraucht das Paket und erhöht den Bestand der Ware. |
| Kochen und Gerichte | Waren, Gericht | Nimmt Waren aus dem Bestand und erzeugt ein warmes Gericht. |
| Gäste und Bedürfnisse | Gericht (Bedürfnis, warm oder kalt) | Verbraucht das Gericht. Die Freude daran legt das Gäste-Dokument fest. |
| Interaktion | Werkzeug (Handlung), Kategorie, Ausrüstung (Platz) | Erlaubt die Handlung nur mit dem passenden Werkzeug oder der passenden Kategorie in der Hand; legt Ausrüstung am Körper an. |
| Geräusche | Masse, Aufprallklang | Aufprall nach Gewicht und Klanggruppe, dazu ein Scherben-Geräusch beim Zerbrechen. |
| Netzwerk-Sitzung | Kennung | Übers Netz geht nur die Kennung, nie das Datenobjekt. |

Die Schnittstelle gehört den Gegenstandsarten: Sie liefern die Daten. Was mit den Daten passiert, legt
das jeweilige System fest.

> `systems-designer`, `gameplay-programmer` und `engine-programmer` wurden nicht befragt (Lean-Modus).
> Vor dem Bau selbst prüfen.

## Formulas

Werte vorgeschlagen von `systems-designer` (2026-11-08), Schwerkraft −9,81, Wurfcode und Physik-
Einstellungen im Projekt nachgeprüft. Vom Entwickler freigegeben.

### 1. Höchstmasse für einen Träger (Prüfung im Editor)

Die Formel `solo_mass_valid` ist definiert als:

`solo_mass_valid = (carry_mode == two_person) OR (mass_kg <= max_solo_mass)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| carry_mode | enum | alone, two_person | data file | Trageart der Art |
| mass_kg | float | > 0 (Katalog 0,3–80) | data file | Masse der Art |
| max_solo_mass | float | 20–40 (Start 25) | data file | Ein Wert für den ganzen Katalog |
| solo_mass_valid | bool | true/false | calculated | false = Fehler im Editor |

**Output Range:** `false` nur bei `alone` und mehr als 25 kg (25,0 ist erlaubt). Zusätzlich gibt es
einen Hinweis (keinen Fehler), wenn eine Art `two_person` hat, höchstens 25 kg wiegt und nicht in der
Kategorie Sperrgut ist. Die Trage (15 kg, Sperrgut) ist die gewollte Ausnahme.

**Example:** Kanister 20 kg ≤ 25, also gültig. Das Tempo sinkt auf 80 % (3,2 statt 4 m/s), für 12,5 m
braucht man 3,9 s statt 3,1 s. Eine 30-kg-Art mit `alone` ist ungültig. Herleitung des Werts:
(1 − 0,75) / 0,01 = 25. Der Faktor 0,01 ist der Tempoverlust pro kg aus dem Tragen, 0,75 das
Mindesttempo, das einem Träger bleibt. Ab 40 kg klemmt das Tempo im Tragen bei 0,6, darüber wäre
nichts mehr spürbar schwerer.

### 2. Zerbrechen

Die Formel `breaks_on_impact` ist definiert als:

`breaks_on_impact = is_fragile AND state == falling AND impact_speed > break_speed`, mit
`impact_speed = abs(dot(v_relative, contact_normal))`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| is_fragile | bool | true/false | data file | Eigenschaft „Zerbrechlich“ |
| state | enum | liegt, getragen, fällt, … | calculated | Zustand beim Host. Nur „fällt“ zählt, Getragenes zerbricht nie. |
| v_relative | Vector3 | beliebig | calculated | Relative Geschwindigkeit der Kontaktpartner |
| contact_normal | Vector3 | Länge 1 | calculated | Kontaktnormale |
| impact_speed | float | 0–ca. 16 m/s | calculated | Senkrechter Anteil der Aufprallgeschwindigkeit, derselbe Wert wie bei den Geräuschen |
| break_speed | float | 5,51–7,2 (Start 7,0) | data file | Bruchgeschwindigkeit der Art |
| breaks_on_impact | bool | true/false | calculated | true = Host entfernt den Gegenstand, Scherben-Geräusch |

**Was ein Kontakt ist:** ein Kollisionsereignis der Physik. `impact_speed` ist der größte senkrechte
Anteil über alle Kontaktpunkte dieses Ereignisses. Die Geräusche rechnen künftig genauso (heute werten
sie nur den ersten Kontaktpunkt aus).

**Output Range:** Jeder Kontakt wird einzeln geprüft, es gibt kein Schadenskonto. Die Gesamt-
geschwindigkeit wäre der falsche Messwert: Ein Wurf, der die Wand nur streift, hätte die vollen 8 m/s
und würde zerbrechen, obwohl er kaum aufprallt. Beim Ablegen setzt das Tragen die Geschwindigkeit auf 0,
dort zählt nur der Fall.

**Example (break_speed 7,0):**

| Fall | impact_speed | Ergebnis |
|---|---|---|
| Ablegen auf den Boden | 5,01 | heil |
| Ablegen auf den Tisch | 2,73 | heil |
| Wurf (8 m/s) gerade gegen die Wand | 8,0 | zerbricht |
| Wurf aus dem Laufen gegen die Wand (8 + 4 m/s) | 12,0 | zerbricht |
| Wurf 40° schräg zur Wand | 6,1 | heil |

Der Wert 7,0 entspricht einer Fallhöhe von 2,5 m, also gut 1,2 m mehr als die Haltehöhe. Schräg gegen
eine Wand zerbricht ein Wurf bis etwa 29° Abweichung (8 × cos 29° = 7,0). Auf den Boden zerbricht ein
Wurf ab etwa 38° Neigung nach oben oder unten: `sqrt((8 × sin p)² + 2 × 9,81 × 1,28) > 7` gilt ab
`sin p > 0,61`.

**Fallgeschwindigkeit beim Loslassen:** Die Formel `release_fall_speed` ist definiert als:

`release_fall_speed = sqrt(2 * gravity * max(0, hold_height - surface_height))`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| gravity | float | 9,81 | constant | Physik-Schwerkraft des Projekts |
| hold_height | float | 1,28 m | constant | Haltepunkt aus dem Tragen: 0,2 m über der Mitte der Spielfigur. Die Mitte liegt 1,08 m über dem Boden (Kapsel 2 m hoch, dazu 0,08 m Hautdicke des Character Controllers); 0,9 m vor der Figur |
| surface_height | float | 0–ca. 1,0 m | calculated | Boden 0, Tisch etwa 0,9 |
| release_fall_speed | float | 0–5,01 m/s | calculated | Aufprall nach dem Loslassen |

**Output Range:** 0 bis 5,01 m/s (Boden). Beim Ablegen setzt das Tragen die Geschwindigkeit auf 0, es
zählt also nur der Fall.

**Example:** Boden: sqrt(2 × 9,81 × 1,28) = 5,01 m/s. Tisch: sqrt(2 × 9,81 × 0,38) = 2,73 m/s.

**Prüffenster im Editor (Hinweis, kein Fehler):**
`1,1 × release_fall_speed_max ≤ break_speed ≤ 0,9 × throw_speed`, also 5,51 bis 7,2 m/s. Die
Prüfung liest dafür zwei Prüfwerte aus den Katalog-Einstellungen: Haltehöhe 1,28 m (ergibt
`release_fall_speed_max` = 5,01 m/s) und Wurftempo 8,0 m/s. Beide müssen zu den Werten im Tragen
passen; ändert sich dort etwas, werden die Prüfwerte nachgezogen. Ein Wert darunter heißt, Ablegen
zerbricht das Geschirr. Ein Wert darüber heißt, gerade Würfe gegen die Wand zerbrechen nichts mehr. Wird
das Fenster zu eng, ist die Wurfgeschwindigkeit im Tragen der Hebel, nicht die Bruchgeschwindigkeit.

### 3. Abkühlen

Die Formel `is_warm` ist definiert als:

`is_warm = (game_time - cooked_time) < cool_time`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| game_time | double | ≥ 0 s | calculated | Spielzeit aus dem Saison-Ablauf (`design/gdd/saison-ablauf.md`, Formel 3a): läuft wie die Serverzeit, steht in der Pause solo. Nachtrag 2026-11-08 (vorher Serverzeit), damit Gerichte in der Pause nicht abkühlen. |
| cooked_time | double | ≥ 0 s | calculated | Spielzeit (`game_time`), zu der das Gericht entstand. Den speichert der Host. |
| cool_time | float | 25–300 s (Tee 90, Schmarrn 150) | data file | Abkühlzeit. Fehlt sie oder ist sie ≤ 0, kühlt das Gericht nie ab. |
| is_warm | bool | true/false | calculated | Warm oder kalt |

**Output Range:** Der Wechsel warm → kalt ist endgültig. Aufwärmen gibt es in diesem Dokument nicht.
Der Host speichert nur den Kochzeitpunkt, Clients berechnen den Zustand daraus selbst. Ist
`cool_time` ≤ 0 oder fehlt sie (Speckbrot), ist `is_warm` immer wahr: Das Gericht gilt als frisch.

**Example:** Tee, gekocht bei t = 100 s: Bei t = 150 s ist er warm, bei t = 190 s kalt.

**Dampf-Anteil (nur für die Anzeige, keine Spielregel):**
`warm_fraction = clamp(1 - (game_time - cooked_time) / cool_time, 0, 1)`. Er sinkt von 1 auf 0 und
kündigt an, dass ein Gericht kalt wird. Beispiel: Kaiserschmarrn nach 100 s ergibt
1 − 100/150 = 0,33, also nur noch schwacher Dampf. Ist `cool_time` ≤ 0 oder fehlt sie, ist
`warm_fraction` = 0: Das Gericht dampft nie (es wird nicht durch 0 geteilt).

**Untergrenze im Editor (Hinweis):**
`cool_time_floor = floor_factor * max_path / (walk_speed_min * carry_speed_factor)` mit
`floor_factor` = 6 (data file, 4–10), `max_path` = 15 m, `walk_speed_min` = 4 m/s und
`carry_speed_factor` = 0,992 (Gericht 0,8 kg). Das ergibt 6 × 15 / (4 × 0,992) ≈ 23 s. Empfohlen ist
das 20- bis 40-Fache des Wegs, also 75 bis 150 s. Gerichte ohne Abkühlzeit bekommen keinen Hinweis.

Begründung der Werte: Das Spiel läuft im 45-Sekunden-Takt eines Scheits. Ein normaler Weg dauert
4 s, mit Chaos 10 bis 20 s. Wer trödelt, merkt es, wer sich beeilt, hat keinen Stress. Tee (90 s)
ist schnell gemacht, Kaiserschmarrn (150 s) macht mehr Arbeit und bekommt mehr Zeit.

### 4. Masse beim Hacken

Die Formel `chop_mass_balance` ist definiert als:

`chop_mass_balance = result_count * result_mass_kg - source_mass_kg`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| result_count | int | 1–10 | data file | Stückzahl beim Hacken |
| result_mass_kg | float | > 0 | data file | Masse der Ergebnis-Art |
| source_mass_kg | float | > 0 | data file | Masse der hackbaren Art |
| chop_mass_balance | float | beliebig | calculated | Überschuss (+) oder Verlust (−) |

**Output Range:** Der Sollwert ist 0. Weicht er um mehr als 10 % der Ausgangsmasse ab, gibt der Editor
einen Hinweis. Ohne die Regel könnte Hacken aus wenig Masse viel Brennstoff machen.

**Example:** Klotz: 4 × 3 − 12 = 0, also gültig. Er brennt insgesamt 180 s, das sind 15 s pro kg,
genau wie beim einzelnen Scheit.

## Edge Cases

**Katalog und Daten (Prüfung im Editor)**

- **Wenn zwei Arten oder Waren dieselbe Kennung haben:** Der Editor meldet einen Fehler, der beide
  nennt. Build und Host-Start werden verweigert, bis es behoben ist.
- **Wenn die Masse ≤ 0 ist oder ein Vorratspaket 0 Portionen hat:** Fehler im Editor. Die Art gilt als
  ungültig und erscheint nicht in der Welt.
- **Wenn die Kennung oder der Anzeigename leer ist** (bei Arten und Waren): Fehler im Editor. Der
  Eintrag gilt als ungültig.
- **Wenn eine Art kein Prefab hat:** Fehler im Editor. Waren haben als eigener Datentyp gar kein
  Prefab-Feld.
- **Wenn ein Vorratspaket auf eine unbekannte Ware verweist** (fest im Katalog oder beim Erscheinen
  gesetzt): Fehler. Im Editor wird die Art abgelehnt, zur Laufzeit erscheint das Paket nicht.
- **Wenn ein Paket ohne feste Ware ohne Angabe erscheinen soll:** Fehler beim Host, das Paket
  erscheint nicht. Wer ein Paket erzeugt, muss die Ware nennen.
- **Wenn die Rigidbody-Masse im Prefab vom Katalog abweicht:** Der Katalog gilt, die Masse wird beim
  Erscheinen überschrieben. Der Editor gibt einen Hinweis.
- **Wenn eine hackbare Art sich selbst ergibt**, ob direkt oder über eine Kette (Klotz → Klotz):
  Fehler im Editor.
- **Wenn ein Gegenstand in der Szene keine Art hat:** Fehler beim Start. Das Objekt wird nicht als
  Gegenstand behandelt, man kann es also nicht aufheben.

**Netz**

- **Wenn ein Client eine unbekannte Kennung bekommt:** Das verhindert schon die Versionsprüfung beim
  Beitreten. Passiert es trotzdem, meldet der Client einen Fehler und zeigt den Gegenstand nicht an.
  Beim Host ändert sich nichts.
- **Wenn die Vorhersage-Kopie beim Client gegen eine Wand fliegt:** Sie zerbricht nie selbst, nur der
  Host entscheidet. Hat der Host das Original entfernt, verschwindet auch die Kopie.

**Zerbrechen und Abkühlen**

- **Wenn ein liegendes zerbrechliches Gericht getroffen wird:** Der Stoß versetzt es in den Zustand
  „Fällt“ und zählt selbst als Aufprall. Eine geworfene Kiste (8 m/s) zerbricht die Tasse. Eine Kiste,
  die aus Haltehöhe darauf fällt (5,01 m/s), lässt sie heil.
- **Wenn mehrere Kontakte im selben Physikschritt kommen:** Jeder wird geprüft, der erste über der
  Grenze zerbricht das Gericht. Der Host entfernt es einmal, und es gibt genau ein Scherben-Geräusch.
- **Wenn der Träger hinfällt (Ragdoll):** Der Gegenstand wechselt von „Getragen“ zu „Fällt“, danach
  gilt die normale Bruchregel.
- **Wenn ein Gericht während des Tragens kalt wird:** Es bleibt in der Hand und ist ab dann kalt. Was
  ein kaltes Gericht dem Gast wert ist, legt das Gäste-Dokument fest. Empfehlung: weniger wert, aber
  nicht wertlos.
- **Wenn ein Gericht keine Abkühlzeit hat** (Speckbrot): Es gilt immer als frisch, `is_warm` bleibt
  wahr. Es dampft nie (`warm_fraction` = 0), und der Editor gibt keinen Hinweis zur Untergrenze.
- **Wenn ein Gericht zerbricht, während ein Gast es gerade annehmen soll:** Der Host arbeitet die
  Ereignisse der Reihe nach ab. Ist das Gericht schon entfernt, schlägt das Servieren fehl, und der Gast
  wartet weiter.

**Tragen und Verbrauch**

- **Wenn eine Art mit „zu zweit“ nur einen Träger hat:** Sie bewegt sich nicht, so wie heute die Truhe.
- **Wenn ein Gegenstand ohne „brennbar“ in den Ofen geworfen wird:** Der Ofen verbraucht ihn nicht, er
  bleibt liegen oder prallt ab. Die Einzelheiten legt das Ofen-Dokument fest.
- **Wenn Personen auf der Trage liegen:** Ihre Masse ist dynamisch und gehört zum Nachteinsatz. Der
  Katalog prüft nur die Leermasse (15 kg).
- **Wenn eine Art genau 25 kg wiegt und „allein“ hat:** Das ist erlaubt.
- **Wenn ein Upgrade Werte ändern soll:** Upgrades ändern im Vertical Slice keine Werte im Katalog. Die
  hellere Stirnlampe gehört zu „Strom und Licht“.

## Dependencies

**Hängt ab von:** keinem anderen System. Gegenstandsarten sind die Grundlage. Die Netzwerk-Sitzung wird
nur als Weg für die Kennung genutzt, sie ist keine Abhängigkeit. Die Abkühl-Formeln bekommen die
Spielzeit `game_time` aus dem Saison-Ablauf (`design/gdd/saison-ablauf.md`) als Wert übergeben; der
Katalog selbst hängt deshalb nicht am Saison-Ablauf.

**Wird gebraucht von:**

| System | Art | Schnittstelle (was gelesen wird) | Stand |
| --- | --- | --- | --- |
| Tragen und Werfen | hart | Masse, Trageart; setzt beim Loslassen „Fällt“ (den Aufprall für die Bruchregel misst `ImpactReporter`, ADR-0001) | gebaut, liest heute die Rigidbody-Masse |
| Tragen zu zweit | hart | Trageart `zu zweit` | gebaut |
| Holzofen und Wärme | hart | Brennbar (Brenndauer) | gebaut, Brenndauer heute im Holz selbst (`FuelItem`) |
| Holz hacken | hart | Hackbar (Ergebnis, Anzahl), Masse für die Bilanz | kein Dokument |
| Lager und Vorräte | hart | Vorratspaket (Ware, Portionen), Liste der Waren | kein Dokument |
| Kochen und Gerichte | hart | Waren, Gericht (Abkühlzeit, Kochzeitpunkt) | kein Dokument |
| Gäste und Bedürfnisse | hart | Gericht (Bedürfnis, warm/kalt) | `design/gdd/gaeste.md` |
| Interaktion | hart | Werkzeug (Handlung), Kategorie, Ausrüstung (Platz) | `design/gdd/interaktion.md` |
| Geräusche | weich | Masse, Aufprallklang, Scherben beim Zerbrechen | gebaut |
| Nachteinsatz | weich | Trage (Leermasse, zu zweit) | kein Dokument |
| Ereignisse (mit Strom und Licht) | weich | Stirnlampe (Werkzeug) | kein Dokument |
| Sprachchat und Funkgerät | weich | Funkgerät (Werkzeug) | kein Dokument |
| Kraxe (Alpha) | weich | Masse | kein Dokument |
| Materialseilbahn | hart | Vorratspaket; erzeugt Pakete und setzt dabei die Ware | kein Dokument |
| Versorgung: Betten und Erste Hilfe | weich | Erste-Hilfe-Kasten (Werkzeug, über die Interaktion) | kein Dokument |
| Solo-Hüttenhund | weich | Masse (trägt nur kleine Dinge) | kein Dokument |
| Saison-Ablauf und Spielphasen | weich | Startaufstellung: legt beim Saisonstart fest, welche Gegenstände wo liegen und welche Ware in den Paketen steckt (vorläufig; heute macht das `CrateSpawner`) | kein Dokument |

„Hart“ heißt: Das System funktioniert ohne den Katalog nicht. „Weich“ heißt: Es nutzt nur einzelne
Einträge daraus.

**Beidseitig:** Noch hat keins dieser Systeme ein Design-Dokument. Jedes muss beim Schreiben
„Gegenstandsarten“ unter seinen Abhängigkeiten nennen.

## Tuning Knobs

Bei Stufe `standard` nur empfohlen, auf Hinweis des Design-Reviews ergänzt. Alle Werte liegen in
Datenobjekten, nie im Code.

| Stellschraube | Start | Sicherer Bereich | Ablage | Wirkung |
| --- | --- | --- | --- | --- |
| `max_solo_mass` | 25 kg | 20–40 kg | Katalog-Einstellungen | Ab wann eine Art nur zu zweit tragbar sein muss. Höher = schwere Dinge allein tragbar, Teamwork schwächer. |
| `break_speed` | 7,0 m/s | 5,51–7,2 m/s | je Art | Ab wann Geschirr zerbricht. Niedriger = zerbricht beim Ablegen; höher = gerade Würfe zerbrechen nicht mehr. |
| `cool_time` | Tee 90 s, Schmarrn 150 s | 25–300 s | je Art | Zeit bis kalt. Kürzer = mehr Zeitdruck, mehr Frust. |
| `floor_factor` | 6 | 4–10 | Katalog-Einstellungen | Puffer der Untergrenze für `cool_time` (nur Editor-Hinweis). |
| Prüfwert Haltehöhe | 1,28 m | muss dem Tragen entsprechen | Katalog-Einstellungen | Grundlage des Prüffensters für `break_speed`. |
| Prüfwert Wurftempo | 8,0 m/s | muss dem Tragen entsprechen | Katalog-Einstellungen | Obere Grenze des Prüffensters. |
| `max_path` | 15 m | 10–15 m | Katalog-Einstellungen | Längster Weg Herd → Stube für die Untergrenze. |
| `walk_speed_min` | 4 m/s | muss der Bewegung entsprechen | Katalog-Einstellungen | Lauftempo für die Untergrenze. |
| Toleranz beim Hacken | 10 % | 5–20 % | Katalog-Einstellungen | Ab welcher Abweichung der Massenbilanz der Editor warnt. |

Wechselwirkungen: Ändert das Tragen Haltepunkt oder Wurftempo, müssen die beiden Prüfwerte mit; sonst
prüft der Editor gegen alte Zahlen. Das Wurftempo verschiebt die obere Grenze für `break_speed`.

## Visual/Audio Requirements

- **Gewicht sieht man:** Die Größe passt grob zur Masse. Die Truhe (80 kg) ist groß und mit Eisen
  beschlagen, Kiste und Kanister sind mittelgroß, Tasse und Stirnlampe klein. Alles im Low-Poly-Stil des
  Konzepts.
- **Dampf über warmen Gerichten:** Seine Stärke folgt dem Dampf-Anteil (1 → 0). Kalte Gerichte und
  Speckbrot dampfen nicht.
- **Zerbrechen:** ein kurzer Scherben-Effekt aus Partikeln, ohne dass etwas liegen bleibt, dazu ein
  Scherben-Geräusch. Das ist ein neuer Klang, er gilt nur für den Bruch.
- **Aufprall:** Neu ist eine Klanggruppe je Art (Holz oder Weich). Heute gibt es nur die Holzgruppe plus
  eine dumpfe Schicht nach Gewicht (`impactSoft`-Clips). Lautstärke und Tonhöhe richten sich weiter nach
  der Masse.
- **Inhalt der Pakete:** Eine Ware bekommt optional eine Farbe oder ein Symbol für den Aufkleber auf
  Kiste und Truhe, damit man den Inhalt von außen erkennt.

`art-director` und `audio-director` nicht befragt (Kategorie Core, Lean-Modus).

## Game Feel

- Das Gewicht spürt man über das Tragen: Tempo und Kamera-Wippen. Die Massen sind in drei Stufen
  gewählt: unter 3 kg leicht, 10–20 kg merklich, 80 kg nur zu zweit.
- Zerbrechen muss fair wirken. Ablegen zerbricht nie etwas, ein Wurf gegen die Wand immer. Das Fenster
  (5,51 bis 7,2 m/s) lässt dafür Spielraum; der Playtest bestätigt es.
- Der Dampf kündigt an, dass ein Gericht kalt wird, es soll nie plötzlich passieren.
- Beim Client erscheint der Bruch mit der Verzögerung seines Pings (etwa 100 ms), seine Kopie prallt
  kurz ab. Das nehmen wir hin.

## UI Requirements

- Das System hat kein eigenes Element auf dem Bildschirm. Der Anzeigename erscheint im Hinweis beim
  Anschauen, zum Beispiel „E: Holzscheit aufheben“. Diesen Hinweis gestalten Interaktion und HUD.
- Bei einem Paket nennt der Hinweis den Inhalt: „Vorratskiste (Speck, 6 Portionen)“.
- **Debug-Anzeige**, nur während der Entwicklung: Kennung, Zustand und warm/kalt samt Dampf-Anteil des
  Gegenstands, den man ansieht. Sie wird für die manuellen Tests 16 und 30 gebraucht.

> **UX-Hinweis:** Der Anschau-Hinweis mit Name und Paketinhalt gehört ins HUD. Vor den Epics mit
> `/ux-design` ein UX-Dokument dafür anlegen (`design/ux/hud.md`).

## Cross-References

| Verweis | Quelle | Genutzt in |
| --- | --- | --- |
| 1 % Tempoverlust pro kg, Mindesttempo 0,6 | Tragen und Werfen (`PlayerCarry`, gebaut, kein Dokument) | Formel 1 |
| Wurftempo 8 m/s (höchstens 15); Haltepunkt 0,2 m über der Mitte der Spielfigur (1,28 m über dem Boden, mit 0,08 m Hautdicke), 0,9 m davor; Ablegen ohne Geschwindigkeit | Tragen und Werfen (`PlayerCarry`) | Formel 2, Prüfwerte |
| Lauftempo 4 m/s | Bewegung und Kamera (`PlayerMovement`) | Formel 1, Untergrenze Abkühlzeit |
| Ofen nimmt höchstens 240 s Brennzeit auf | Holzofen (`StoveSettings`) | Abnahmekriterium 25 |
| Aufprall-Messwert | `BreakRule.ImpactSpeed` und `ImpactReporter` (ADR-0001; heute noch `ImpactAudio`) | Bruchregel |
| Klanggruppen, 3 kg leicht / 80 kg schwer | Geräusche (`ImpactAudio`, `SoundSettings`) | Aufprallklang |
| Truhe bewegt sich nur zu zweit | Tragen zu zweit (`HeavyCarryable`) | Trageart |
| Brenndauer 45 s | Holzofen (heute `FuelItem`, künftig Katalog nach ADR-0002) | Holzscheit |
| Bedürfnis, Wert kalter Gerichte | Gäste (`design/gdd/gaeste.md`) | Eigenschaft Gericht |
| Rezepte, endgültige Waren | Kochen (kein Dokument) | Warenliste |
| Architektur-Regeln 2, 3 und 4 | CLAUDE.md | Host entscheidet, Einfrieren, Datenobjekte |

## Acceptance Criteria

Vorschlag von `qa-lead` (2026-11-08), an die Entscheidungen des Entwicklers angepasst und freigegeben.

**Damit die Tests verlässlich bleiben:** Alle Formeln und die Katalogprüfung sind reine Funktionen.
Zeit, Geschwindigkeit und Normale werden als Werte übergeben, ohne echte Uhr und ohne Zufall. In
EditMode-Tests entstehen Arten über `ScriptableObject.CreateInstance`. PlayMode-Physik läuft in festen
Schritten (`Physics.simulationMode = SimulationMode.Script`, `Physics.Simulate(0.02f)`). Toleranz
±0,01 bei m/s und s. Kriterien für zwei Spieler werden beim Freundestest geprüft, nicht allein mit zwei
Fenstern.

Testarten: **[EditMode]** automatischer Unit-Test unter `Assets/Tests/EditMode/`, **[PlayMode]**
automatischer Test mit Physik, **[Manuell]** im Editor nach Anleitung, **[Freundestest]** zu zweit
übers Netz.

### A. Katalog und Daten

1. **[EditMode]** **GIVEN** der ausgelieferte Katalog **WHEN** die Prüfung läuft **THEN** gibt es
   0 Fehler, genau die 13 Arten mit den Werten aus dem Startkatalog (Masse, Trageart, Kategorie,
   Aufprallklang, Eigenschaften) und genau die 7 Waren mit `ware_`-Kennung. *Schnappschuss: Ändert ein
   anderes Dokument einen Wert, werden Tabelle und Test zusammen geändert.*
2. **[EditMode]** **GIVEN** je eine Test-Art mit einem Fehler (Masse 0, Masse −1, leere Kennung, leerer
   Name, kein Prefab, Paket mit 0 Portionen, Paket mit unbekannter fester Ware) **WHEN** die Prüfung
   läuft **THEN** gibt es pro Test-Art genau 1 Fehler, der ihre Kennung oder ihren Asset-Namen nennt.
3. **[EditMode]** **GIVEN** `max_solo_mass` = 25 **WHEN** geprüft werden: allein 25,0 / 25,01 / 30 kg,
   zu zweit 80 kg **THEN** gültig / Fehler / Fehler / gültig. **WHEN** `max_solo_mass` im Datenobjekt
   auf 30 steht **THEN** ist allein mit 28 kg gültig (die Grenze steht nicht im Code).
4. **[EditMode]** **GIVEN** Trageart zu zweit **WHEN** geprüft werden: 20 kg Vorratspaket / 25 kg
   Rohstoff / 15 kg Sperrgut / 26 kg Vorratspaket **THEN** Hinweis / Hinweis / nichts / nichts; nie ein
   Fehler.
5. **[EditMode]** **GIVEN** zwei Einträge (Arten oder Waren) mit derselben Kennung **WHEN** die Prüfung
   läuft **THEN** gibt es genau 1 Fehler, der beide Assets nennt, **AND** das Prüfergebnis meldet
   „Start gesperrt“ (das Ergebnis, das der Host-Start liest, ist eine prüfbare Funktion).
6. **[EditMode]** **GIVEN** hackbar A → A oder A → B → A **WHEN** geprüft **THEN** gibt es mindestens
   1 Fehler, der den Kreis nennt; holzklotz → holzscheit ergibt keinen.
7. **[EditMode + PlayMode]** **GIVEN** das holzscheit-Prefab hat Rigidbody-Masse 1 **WHEN** die Prüfung
   läuft **THEN** gibt es 1 Hinweis und keinen Fehler; **WHEN** der Host es erscheinen lässt **THEN**
   ist `Rigidbody.mass` = 3,0.
8. **[Manuell]** Solo: **GIVEN** ein Objekt in der Szene ohne Art **WHEN** Play startet **THEN** steht
   genau 1 Fehler mit dem Objektnamen in der Konsole, und ein Aufheben-Versuch lässt es liegen.
9. **[Manuell]** **GIVEN** eine Spielrunde mit Tragen, Werfen und Verbrennen **WHEN** Play beendet ist
   **THEN** zeigt `git status` keine Änderung an den Katalog-Dateien in `Assets/_Project/Data/`.

### B. Zerbrechen

10. **[EditMode]** **GIVEN** ein flacher Wurf auf den Boden mit v_relative (6,0; −5,01; 0), Normale
    (0; 1; 0), break_speed 7,0, Tasse „fällt“ **WHEN** berechnet **THEN** ist `impact_speed` = 5,01 und
    die Tasse bleibt heil (der Gesamtbetrag 7,82 zählt nicht).
11. **[EditMode]** **GIVEN** zerbrechlich, break_speed 7,0, „fällt“ **WHEN** impact_speed 4,2 / 6,1 /
    7,0 / 7,01 / 8,0 / 12,5 **THEN** heil / heil / heil / bricht / bricht / bricht. Der schräge Wurf
    (5,14; 0; 6,13) gegen die Normale (0; 0; 1) ergibt 6,13 und bleibt heil. Speckbrot und eine Tasse
    im Zustand „getragen“ bleiben auch bei 12,5 heil.
12. **[EditMode]** **GIVEN** (Haltehöhe; Fläche) = (1,28; 0) / (1,28; 0,9) / (1,8; 0) / (1,28; 2,0)
    **WHEN** `release_fall_speed` berechnet wird **THEN** 5,01 / 2,73 / 5,94 / 0, ohne NaN.
13. **[EditMode]** **GIVEN** Prüfwerte Haltehöhe 1,28 m und Wurftempo 8,0 m/s (Fenster 5,51 bis 7,20)
    **WHEN** break_speed 7,0 / 5,52 / 7,20 / 5,45 / 7,25 geprüft wird **THEN** nichts / nichts /
    nichts / Hinweis „Ablegen zerbricht“ / Hinweis „gerade Würfe zerbrechen nicht“; nie ein Fehler.
14. **[EditMode]** **GIVEN** eine fallende Tasse **WHEN** im selben Physikschritt zwei Kontakte mit 8
    und 9 m/s kommen **THEN** wird Entfernen genau 1-mal ausgelöst und genau 1 Scherben-Geräusch
    angefordert, ohne Exception.
15. **[PlayMode]** Nur Host: **GIVEN** eine Tasse Tee liegt eingefroren auf dem Boden **WHEN** eine
    Vorratskiste mit 8 m/s waagrecht dagegenprallt **THEN** ist die Tasse nach höchstens 10
    Physikschritten entfernt; **WHEN** die Kiste stattdessen aus Haltehöhe (5,01 m/s) senkrecht darauf
    fällt **THEN** existiert die Tasse nach 50 Schritten noch.
16. **[Manuell]** Solo, je 10 Versuche mit Tee und Kaiserschmarrn: **WHEN** auf Boden oder Tisch
    abgelegt, auch im Gehen **THEN** 10/10 heil; **WHEN** aus dem Laufen gerade gegen die Wand geworfen
    **THEN** 10/10 verschwinden mit genau einem Scherben-Geräusch, ohne Reste. Speckbrot, genauso
    geworfen: 10/10 heil.
17. **[Manuell]** *(sobald Hinfallen gebaut ist)* **GIVEN** ein Spieler trägt eine Tasse **WHEN** er
    hinfällt **THEN** fällt die Tasse mit Physik und bleibt beim Sturz aus Haltehöhe auf den Boden heil.

### C. Abkühlen

18. **[EditMode]** **GIVEN** cooked_time 100, cool_time 90 **WHEN** game_time 150 / 189,9 / 190 /
    10 000 **THEN** warm / warm / kalt / kalt. **GIVEN** cool_time 0, −5 oder fehlend **WHEN**
    game_time 0 und 100 000 **THEN** immer warm, `warm_fraction` = 0.
19. **[EditMode]** **GIVEN** cooked_time 0, cool_time 150 **WHEN** game_time 0 / 100 / 150 / 300
    **THEN** `warm_fraction` 1,0 / 0,333 / 0 / 0. **GIVEN** cooked_time 100 und game_time 99 (Uhr
    des Clients knapp zurück) **THEN** 1,0.
20. **[EditMode]** **GIVEN** floor_factor 6, max_path 15 m, walk_speed_min 4 m/s, carry_speed_factor
    0,992 **WHEN** berechnet **THEN** ergibt sich 22,68 s; cool_time 20 gibt einen Hinweis, 90 nichts,
    ein Gericht ohne Abkühlzeit nichts; nie ein Fehler.
21. **[PlayMode]** Host, austauschbare Uhr: **GIVEN** ein Tee mit cooked_time T **WHEN** er bis T + 89
    abwechselnd liegt, getragen, geworfen und wieder getragen wird und die Uhr auf T + 90 springt
    **THEN** ist er kalt und bleibt in der Hand; bei T + 1000 ist er weiter kalt, auch neben dem
    brennenden Ofen.

### D. Hacken

22. **[EditMode]** **GIVEN** ein Klotz mit 12 kg **WHEN** das Ergebnis 4 × 3 / 4 × 3,25 / 4 × 2,7 /
    5 × 3 / 3 × 3 kg ist **THEN** Bilanz 0 / +1 / −1,2 / +3 / −3 kg und Meldung nichts / nichts /
    nichts / Hinweis / Hinweis (−1,2 sind genau 10 %, kein Hinweis; Vergleich mit Toleranz 0,0001);
    nie ein Fehler.
23. **[PlayMode]** *(sobald Hacken gebaut ist)* **GIVEN** ein Holzklotz liegt und der Spieler hält die
    Axt **WHEN** er hackt **THEN** ist der Klotz entfernt und es liegen genau 4 neue Holzscheite mit je
    `Rigidbody.mass` 3,0; **WHEN** er mit leerer Hand oder einem Scheit hackt **THEN** passiert nichts.

### E. Netz und Zusammenspiel

24. **[PlayMode]** **GIVEN** ein Test-Katalog, in dem die Vorratskiste 20 kg wiegt **WHEN** der Host
    sie erscheinen lässt und ein Spieler sie trägt **THEN** ist `Rigidbody.mass` = 20 und der
    Tempofaktor derselbe wie beim Wasserkanister (0,8 statt 0,9).
25. **[PlayMode]** **GIVEN** ein brennender Ofen mit Restzeit 60 s (unter der Obergrenze von 240 s des
    Ofens) **WHEN** ein Holzscheit hineinfällt **THEN** ist es entfernt und die Restzeit 105 s (steht im
    Katalog 60 s Brenndauer, dann 120 s); **WHEN**
    eine Axt hineinfällt **THEN** bleibt sie bestehen und R ändert sich nicht.
26. **[Manuell]** Solo: **GIVEN** je eine Art der Klanggruppen Holz und Weich **WHEN** sie aus
    Haltehöhe fallen **THEN** spielt jede den Klang ihrer Gruppe; eine zerbrechende Tasse spielt genau
    ein Scherben-Geräusch.
27. **[EditMode]** **GIVEN** der Laufzeit-Katalog **WHEN** nach `gibt_es_nicht` gesucht wird **THEN**
    gibt es keinen Treffer, genau 1 Fehler, keine Exception, und der Erscheinen-Pfad beim Client zeigt
    nichts an.
28. **[Freundestest]** Host und Client, Client mit etwa 100 ms Ping: **GIVEN** der Client hält eine
    Tasse Tee **WHEN** er sie gerade gegen die Wand wirft **THEN** verschwindet sie bei beiden mit je
    genau 1 Scherben-Geräusch; die Kopie beim Client zerbricht nicht selbst. **WHEN** er sie 10-mal
    ablegt **THEN** bleibt sie 10/10 bei beiden heil.
29. **[Freundestest]** **GIVEN** nur der Client hält die Truhe **WHEN** er 3 s zieht **THEN** bewegt
    sie sich weniger als 0,1 m; halten beide sie, bewegt sie sich. Dasselbe gilt für die Trage, sobald
    sie gebaut ist.
30. **[Freundestest]** **GIVEN** der Host erzeugt zur Zeit T einen Tee **WHEN** beide ihn beobachten
    **THEN** wird er bei Host und Client höchstens 0,5 s auseinander kalt, etwa bei T + 90 s. Gemessen
    wird über je eine Log-Zeile mit der Serverzeit des Wechsels bei Host und Client (oder die
    Debug-Anzeige), nicht mit bloßem Auge.
31. **[EditMode]** *(sobald Gäste gebaut sind)* **GIVEN** beim Host kommen die Ereignisse [Tasse
    zerbricht, Tasse an Gast G servieren] **WHEN** er sie abarbeitet **THEN** schlägt das Servieren
    fehl, G wartet weiter, keine Exception.
32. **[PlayMode]** **GIVEN** der Host lässt eine Vorratskiste mit `ware_speck` erscheinen **WHEN** ein
    Client sie sieht **THEN** lesen Host und Client am Gegenstand dieselbe Ware `ware_speck`.
33. **[Manuell]** **GIVEN** zwei Einträge mit derselben Kennung im Projekt **WHEN** der Build gestartet
    wird (Batchmode wie beim Freundestest-Build) **THEN** bricht er mit einem Fehler ab, der beide
    Assets nennt, und es entsteht keine Exe.
34. **[EditMode + PlayMode]** **GIVEN** eine Test-Art „Testlampe“ mit der Eigenschaft Ausrüstung `Kopf`
    und eine gleich schwere Test-Art ohne diese Eigenschaft **WHEN** beide aufgehoben werden **THEN**
    geht die Testlampe in den Zustand „Am Körper“ (der Code erkennt sie am Feld, nicht am Namen), die
    andere in „Getragen“. **WHEN** der Träger der Testlampe stürzt (Testaufruf) **THEN** bleibt sie
    „Am Körper“ und zerbricht nicht, auch wenn sie zusätzlich zerbrechlich ist.

## Open Questions

| # | Frage | Wer klärt | Bis wann |
| --- | --- | --- | --- |
| 1 | Passt das Bruchfenster (5,51–7,2 m/s) im Spiel? Hebel wäre das Wurftempo. | Entwickler im Playtest | wenn das Zerbrechen gebaut ist |
| 2 | Sollen steile Würfe auf den Boden (ab etwa 38° Neigung) zerbrechen? | Entwickler im Playtest | wenn das Zerbrechen gebaut ist |
| 3 | Bei 15 m/s können Gegenstände dünne Wände durchfliegen. Braucht es für Würfe eine durchgehende Kollisionsprüfung? | Programmierung (ADR) | vor dem Bau |
| 4 | Lässt man ein Gericht in einer Wand los, drückt die Physik es mit bis zu 10 m/s heraus. Zerbricht es dabei? | Test, notfalls kurze Schonzeit nach dem Loslassen | beim Bau |
| 5 | ~~Trägt man Stirnlampe und Funkgerät in der Hand oder am Körper?~~ **Beantwortet 2026-11-08:** am Körper (Eigenschaft „Ausrüstung“, Zustand „Am Körper“, Regeln in `design/gdd/interaktion.md` Regel 8) | Dokument Interaktion | erledigt |
| 6 | Wie schwer ist die Trage mit einer Person darauf? | Dokument zum Nachteinsatz | bei dessen Design |
| 7 | Welche Waren gibt es endgültig, und welche Rezepte? | Dokument zum Kochen | bei dessen Design |
| 8 | ~~Was ist ein kaltes Gericht wert?~~ **Beantwortet 2026-11-08:** halb so viel, bei der Zufriedenheit (`cold_factor` 0,5) und beim Trinkgeld (`tip_cold_factor` 0,5), `design/gdd/gaeste.md` Formeln 3 und 7 | Dokument zu den Gästen | erledigt |
| 9 | Für automatische Tests fehlen noch `.asmdef`-Dateien und `Assets/Tests/`. | `/test-setup` | vor dem ersten Test |
| 10 | Wie gehen Katalog und Kennung übers Netz (Index oder Text)? | `/architecture-decision` | vor dem Bau |
