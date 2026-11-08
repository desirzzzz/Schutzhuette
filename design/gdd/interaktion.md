# Interaktion

> **Status**: Approved (erneutes Design-Review 2026-11-08 nach dem Nachtrag Spielzeit `game_time`; siehe `design/gdd/reviews/interaktion-review-log.md`)
> **Author**: Entwickler + Claude
> **Last Updated**: 2026-11-08
> **Last Verified**: 2026-11-08
> **Implements Pillar**: Teamwork unter Druck (hacken, kurbeln, Erste Hilfe im Sturm), Gemütlichkeit (kochen, Betten machen)

## Summary

Interaktion ist das gemeinsame „Benutzen“ in Schutzhütte: anvisieren, Hinweis sehen, mit der linken
Maustaste benutzen (längere Handlungen gedrückt halten), mit E aufheben und ablegen, mit der rechten
Maustaste werfen. Stationen wie Hackstock, Kurbel oder Bett melden nur ihre Handlungen und Bedingungen;
Werkzeug-Prüfung, Fortschritt am Bezugsobjekt, Teamzuschlag zu zweit, Ausrüstung am Körper und der Weg
zum Host liegen hier. Es existiert, damit jede Station gleich funktioniert und sich jeder Tastendruck
auch mit Ping sofort anfühlt.

> **Quick reference** — Layer: `Core` · Priority: `Vertical Slice` · Key deps: `Bewegung und Kamera, Tragen und Werfen, Gegenstandsarten`

## Overview

**Interaktion** ist das gemeinsame „Benutzen“ in Schutzhütte. Schaut ein Spieler auf etwas Benutzbares in
seiner Nähe, zeigt ein Hinweis, was passieren wird, und ein Tastendruck löst es aus. Benutzbar sind etwa
der Herd, der Hackstock, die Kurbel der Seilbahn, ein Bett, ein verletzter Gast oder ein aufgeflogenes
Fenster. Manche Handlungen gehen nur mit dem passenden Werkzeug in der Hand, etwa Hacken nur mit der Axt
und Erste Hilfe nur mit dem Kasten. Längere Handlungen wie Hacken dauern einen Moment. Heute kann die
Taste E nur aufheben und ablegen. Ohne dieses System bräuchte jede Station ihre eigene Taste und ihren
eigenen Code.

Für die anderen Systeme ist die Interaktion die Grundlage. Eine Station meldet nur, welche Handlungen sie
anbietet und unter welchen Bedingungen. Um alles andere kümmert sich die Interaktion:

- das Anvisieren
- den Hinweis
- die Taste
- die Prüfung, ob das richtige Werkzeug in der Hand ist
- den Weg zum Host

Der Spieler soll dabei nie raten müssen: Ein Blick zeigt, was geht, ein Tastendruck tut es, und man
sieht sofort, dass etwas passiert. Das gilt auch für den Client mit Ping.

Ein Client schickt jede Handlung als Wunsch an den Host, der Host prüft sie und führt sie aus
(Architektur-Regel 2). Wie das technisch umgesetzt wird, legt eine spätere Architektur-Entscheidung fest
(ADR, noch nicht angelegt).

## Detailed Design

### Core Rules

1. **Tasten**, jede mit genau einer Aufgabe:
   - **E:** aufheben, ablegen, ein Truhenende anpacken oder loslassen (wie heute)
   - **Linke Maustaste:** benutzen. Bei längeren Handlungen gedrückt halten.
   - **Rechte Maustaste:** werfen. Das ist neu, heute wirft die linke.

   Die Tasten laufen über die Input Actions des Projekts und sind später frei belegbar.
2. **Anvisieren:** Ein gemeinsamer Strahl von der Kamera mit 2,5 m Reichweite (wie heute) findet das
   erste Ziel. Ein Ziel ist ein **Gegenstand** (aufheben mit E) oder eine **Station** (benutzen mit der
   linken Maustaste). Der Strahl ersetzt die beiden getrennten Strahlen in `PlayerCarry` und
   `PlayerHeavyCarry`. Er ignoriert Spielerfiguren und alles, was Spieler in der Hand oder am Körper
   tragen; sonst träfe man beim Blick nach unten den eigenen Teller oder die eigene Axt (der Haltepunkt
   liegt 0,5 m unter den Augen und 0,9 m vor der Figur). Liegt ein Gegenstand auf einer Station (Klotz auf dem Hackstock), gilt er für die
   linke Maustaste als diese Station; für E bleibt er ein Gegenstand.
3. **Handlung:** Eine Station bietet eine oder mehrere Handlungen an. Jede Handlung ist ein Datenobjekt
   mit:
   - **Name** für den Hinweis, zum Beispiel „Holz hacken“
   - **Was man dafür in der Hand halten muss**, einer von vier Werten: `freie Hände` (Kurbel, Bett),
     `egal` (Fenster schließen), `Werkzeug` (Werkzeug-Handlung aus den Gegenstandsarten, zum Beispiel
     `hacken`) oder `Kategorie` (zum Beispiel ein Gericht zum Servieren). Bei mehreren Nutzern muss jeder
     die Bedingung erfüllen.
   - **Dauer** in Sekunden. 0 heißt sofort.
   - **Höchstzahl gleichzeitiger Nutzer** und **Tempo-Zuschlag je weiterem Nutzer**
   - **Bedingungen**, die das System der Station meldet, zum Beispiel „Kein Klotz auf dem Hackstock“.
     Fehlt eine, nennt der Hinweis den Grund.
4. **Sofortige Handlung:** Klick, dann geht der Wunsch an den Host. Der Host prüft Reichweite, Werkzeug
   und Bedingungen und führt die Handlung aus. Alle sehen das Ergebnis. Der eigene Client gibt sofort
   eine Rückmeldung (Geräusch, kleine Bewegung), das Ergebnis selbst kommt vom Host.
5. **Handlung mit Dauer:**
   - Taste gedrückt halten: Der Fortschritt wächst, und alle Nutzer sehen einen Balken.
   - Pause: Loslassen, Wegschauen, aus der Reichweite gehen oder **Laufen** unterbricht die Handlung. Der
     Fortschritt bleibt **am Bezugsobjekt** der Handlung gespeichert (der Klotz auf dem Hackstock, der
     Gast, das Ereignis), ein anderer Spieler kann weitermachen. Bei Kurbel und Bett ist das Bezugsobjekt
     die Station selbst. Der Fortschritt gehört dem Objekt: Nimmt man einen halb gehackten Klotz weg und
     legt ihn wieder auf, geht es dort weiter. Ein anderer Klotz beginnt bei 0, so erbt nichts den
     Fortschritt eines anderen Objekts. Nur wenn das Objekt zerstört wird oder ein Ereignis endet,
     verfällt der Fortschritt.
   - Bleibt man stehen, hält die Taste weiter gedrückt und hat den Blick auf derselben Station, geht es
     von selbst weiter. Eine **neue** Handlung startet nur mit einem neuen Druck: Schwenkt man mit
     gedrückter Taste auf eine andere Station, passiert dort nichts. Wird an einer besetzten Station ein
     Platz frei, rückt niemand automatisch nach.
   - Bei 100 % führt der Host die Handlung aus und setzt den Fortschritt zurück. Nur der Host zählt.
6. **Mehrere Nutzer:** Bis zur Höchstzahl der Handlung arbeiten mehrere gleichzeitig, jeder weitere macht
   es schneller (Formel unter „Formulas“). Als Nutzer zählt nur, wer gerade gültig hält: Taste gedrückt,
   Blick auf der Station, in Reichweite, nicht am Laufen. Ist die Station voll, sagt der Hinweis
   „Besetzt“; um den letzten Platz gewinnt der Wunsch, der zuerst beim Host ankommt. Jede Handlung geht
   auch allein (mindestens 1 Nutzer), denn Solo ist ein Host ohne Clients.
7. **Hände voll:** Braucht eine Handlung freie Hände oder etwas anderes, als man hält, geht sie nicht. Der
   Hinweis sagt dann zum Beispiel „Erst die Kiste ablegen“. Wer ein Truhenende hält, kann nicht benutzen.
8. **Ausrüstung am Körper:**
   - Welche Arten Ausrüstung sind und welchen Platz sie belegen, steht in der Eigenschaft „Ausrüstung“
     der Gegenstandsarten (Stirnlampe `Kopf`, Funkgerät `Gürtel`). Der Code erkennt Ausrüstung nur an
     diesem Feld, nie am Namen.
   - Ausrüstung wird beim Aufheben mit E sofort angelegt (Zustand „Am Körper“ in den
     Gegenstandsarten). Die Hände bleiben frei. Dafür braucht man freie Hände: E mit etwas in der Hand
     heißt immer „ablegen“.
   - Jeder Spieler hat genau einen Platz für eine Lampe und einen für ein Funkgerät. Wer schon eine Lampe
     trägt, kann keine zweite aufheben, der Hinweis sagt das.
   - Ausrüstung am Körper erfüllt die Werkzeug-Bedingung ihrer Handlung (`licht`, `funk`), als hielte
     man sie in der Hand.
   - Abnehmen geht nur an einem **Ausrüstungshaken** im Vorraum (Handlung „Aufhängen“). Aufhängen hängt
     alles auf, was man trägt. Der Haken hat je 4 Plätze für Lampen und für Funkgeräte. Ein Hakenplatz
     ist ein Ablageplatz: Ausrüstung am Haken hat den Zustand „Liegt“ und wird mit E wie jede andere
     Lampe genommen und dabei angelegt.
   - Wann die Lampe leuchtet, legt „Strom und Licht“ fest. Wie man funkt, legt der Sprachchat fest.
   - Das ist eine **Ausnahme zur Regel „keine Taschen, alles in der Hand“** im Dokument
     Gegenstandsarten (dort als Ausnahme vermerkt).
9. **Was Spieler nicht können:** zwei Handlungen gleichzeitig ausführen, Stationen durch Wände benutzen
   (der Strahl muss sie sehen) und Handlungen ohne Blick auf die Station ausführen.

### States and Transitions

Spieler:

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Frei | Kein Ziel im Blick | → Zielt (Ziel in Reichweite) |
| Zielt | Hinweis sichtbar | → Frei (wegschauen), → Benutzt (linke Maustaste). E (aufheben, ablegen) gehört dem Tragen und ändert diesen Zustand nicht. |
| Benutzt | Hält eine Handlung mit Dauer | → Zielt (loslassen, laufen, wegschauen), → Frei (Handlung fertig) |

Station (je Handlung):

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Bereit | Bedingungen erfüllt, Fortschritt 0 | → In Arbeit (erster Nutzer hält) |
| In Arbeit | 1 bis Höchstzahl Nutzer, Fortschritt wächst | → Pausiert (alle lassen los), → Fertig (100 %) |
| Pausiert | Fortschritt am Bezugsobjekt gespeichert, niemand arbeitet | → In Arbeit (jemand drückt neu, oder wer weiter gedrückt hält, steht wieder still und blickt auf die Station), → Bereit (anderes Bezugsobjekt mit Fortschritt 0) |
| Gesperrt | Eine Bedingung fehlt | → Pausiert (Bedingung erfüllt, Fortschritt des Bezugsobjekts > 0), → Bereit (Bedingung erfüllt, Fortschritt 0) |
| Fertig | Host führt die Handlung aus | → Bereit (Fortschritt 0) |

### Interactions with Other Systems

| System | Was es liefert oder nutzt |
| --- | --- |
| Bewegung und Kamera | Blickrichtung für den Strahl. Laufen pausiert eine Handlung mit Dauer. |
| Tragen und Werfen | Benutzt denselben Strahl. E und rechte Maustaste gehören dem Tragen. Was in der Hand ist, prüft die Handlung. |
| Tragen zu zweit | E packt ein Truhenende an. Wer eins hält, kann nicht benutzen. |
| Gegenstandsarten | Werkzeug-Handlung (`hacken`, `erste_hilfe`, `licht`, `funk`) und Kategorie des Gehaltenen. Stirnlampe und Funkgerät gelten als Ausrüstung. |
| Holzofen und Wärme | Holz kommt weiter ohne Taste hinein: vor die Ofentür legen oder werfen. Es gibt keine Handlung. |
| Holz hacken | Hackstock: „Holz hacken“, Axt in der Hand, halten |
| Kochen und Gerichte | Herd und Arbeitsfläche: eine Handlung je Rezeptschritt |
| Materialseilbahn | Kurbel: halten, zu zweit schneller |
| Versorgung: Betten und Erste Hilfe | Bett machen (halten), Erste Hilfe (Kasten in der Hand, halten) |
| Ereignisse (mit Strom und Licht) | Zum Beispiel Fenster schließen (sofort), Generator reparieren (halten) |
| Gäste und Bedürfnisse | „Servieren“: Gericht in der Hand, sofort |
| Sprachchat und Funkgerät | Das Funkgerät hängt am Gürtel. Die Sprechtaste legt der Sprachchat fest. |
| Netzwerk-Sitzung | Jede Handlung geht als Wunsch per RPC an den Host. |

Die Schnittstelle gehört der Interaktion: Stationen melden Handlungen und Bedingungen, die Interaktion
erledigt den Rest. Was eine Handlung **bewirkt**, legt das jeweilige System fest.

> `game-designer`, `ux-designer` und `gameplay-programmer` wurden nicht befragt (Lean-Modus). Vor dem
> Bau selbst prüfen.

## Formulas

Werte vorgeschlagen von `systems-designer` (2026-11-08, Reichweiten und Tempo im Code nachgeprüft), vom
Entwickler freigegeben. Bekannt: Die Kamera sitzt 0,7 m über der Mitte der Spielfigur, also etwa
1,78 m über dem Boden (`CameraMount` im `PrototypeSceneBuilder`). Nicht gemessen: Interpolationsverzögerung des
`NetworkTransform`, Tickrate des Hosts.

### 1. Zu zweit schneller

Die Formel `crew_multiplier` ist definiert als:

`crew_multiplier = (users >= 1) ? 1 + extra_user_bonus * (min(users, max_users) - 1) : 0`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| users | int | 0–4 | calculated | Wer gerade gültig hält: Taste gedrückt, Blick auf der Station, in Reichweite, nicht am Laufen. Der Host zählt. |
| max_users | int | 1–4 | data file | Höchstzahl der Handlung |
| extra_user_bonus | float | 0–1 (Start 0,5; Kurbel und Generator 0,75) | data file | Zuschlag je weiterem Nutzer |
| crew_multiplier | float | 0 bis `max_solo_penalty` (Start 2) | calculated | Faktor auf das Grundtempo |

**Output Range:** 0 ohne Nutzer, 1 allein, höchstens `max_solo_penalty` (Start 2,0, erlaubt bis 3; siehe
`action_data_valid`). Der Zuschlag bleibt
unter 1: Zwei Spieler an einer Aufgabe schaffen weniger als zwei an getrennten Aufgaben. Unter Zeitdruck
lohnt sich das Zusammenarbeiten, bei vielen offenen Aufgaben das Aufteilen. Niemand wird gezwungen.

**Example:** Kurbel mit 2 Nutzern und Zuschlag 0,75 ergibt 1,75. Mit 3 Nutzern bleibt es bei 1,75, weil
`min(3, 2)` = 2.

Die Formel `progress_rate` ist definiert als:

`progress_rate = crew_multiplier / duration`

Der Host rechnet pro Schritt `progress = min(1, progress + progress_rate * dt)`. Restzeit:
`time_to_finish = (1 - progress) / progress_rate`.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| crew_multiplier | float | 0 bis `max_solo_penalty` | calculated | siehe oben |
| duration | float | 0 oder 0,5–20 s | data file | Dauer allein. 0 heißt sofort, ohne Fortschritt. |
| dt | float | ≥ 0 s | calculated | Vergangene Spielzeit (`game_time` aus `design/gdd/saison-ablauf.md`) seit dem letzten Schritt des Hosts; in der Pause solo 0, der Fortschritt steht dann (Nachtrag 2026-11-08) |
| progress | float | 0–1 | calculated | Fortschritt, gespeichert am Bezugsobjekt |
| progress_rate | float | 0 bis `max_solo_penalty` / 0,5 pro s (Start: 0–4) | calculated | Anteil des Fortschritts pro Sekunde |
| time_to_finish | float | 0–20 s | calculated | Restzeit bei gleicher Besetzung |

**Output Range:** Bei der Dauer 0 gibt es keine Rate, es wird also nicht durch 0 geteilt. Ist die Rate 0
(niemand hält), zeigt die Anzeige keine Restzeit an. Ein Fortschritt über 1 verfällt.

**Example:** Eine Kurbelfahrt dauert allein 20 s (Rate 0,05 pro s), zu zweit 11,4 s (Rate 0,0875 pro s:
75 % höhere Rate, 43 % weniger Zeit). A kurbelt bis 0,4 und geht weg. B allein braucht dann noch 0,6 / 0,05 = 12 s, mit A
zusammen 0,6 / 0,0875 = 6,9 s.

### 2. Reichweite beim Host

Die Formel `host_range_ok` ist definiert als:

`host_range_ok = target_distance <= max_server_distance`

Einstellregel: `max_server_distance >= interact_range + range_slack` mit
`range_slack = max_player_speed * (rtt_budget + interp_delay)`. Ist sie verletzt, meldet der Editor einen
**Fehler**: Sonst lehnt der Host gültige Klicks von Clients mit Ping ab.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| target_distance | float | ≥ 0 m | calculated | Von den **Augen** der Figur (Sicht des Hosts) zum **nächsten Punkt des Ziels**: Station, Gegenstand oder Truhenende. Heute misst `PlayerCarry` noch von der Wurzel zur Gegenstandsmitte; das wird beim Bau umgestellt. |
| max_server_distance | float | 2,5–6 m (Start 4,0) | data file | Ein gemeinsamer Wert für Tragen und Benutzen |
| interact_range | float | 1–4 m (Start 2,5) | data file | Reichweite des Strahls beim Spieler |
| max_player_speed | float | 0–8 m/s (Start 4) | data file | Höchstes Lauftempo |
| rtt_budget | float | 0,1–0,5 s (Start 0,25) | data file | Hin- und Rückweg zum Host mit Reserve |
| interp_delay | float | 0–0,3 s (Start 0,1, geschätzt) | data file | Wie verspätet der Host die Figur des Clients sieht |
| range_slack | float | ≥ 0 m | calculated | Spielraum über `interact_range` hinaus |
| host_range_ok | bool | true/false | calculated | Host nimmt den Wunsch oder das weitere Halten an |

**Output Range:** true oder false. Der Host prüft beim Start und bei jedem Schritt, solange gehalten wird.
Wer die Grenze überschreitet, zählt nicht mehr als Nutzer, der Fortschritt bleibt erhalten. Loslassen
lehnt der Host nie ab.

**Example:** Spielraum 4 × (0,25 + 0,1) = 1,4 m; 2,5 + 1,4 = 3,9 ≤ 4,0, die Einstellregel ist erfüllt.
Ein Client mit 100 ms Ping drückt bei 2,4 m. Der Host sieht ihn bei etwa 3,2 m, der Wunsch wird
angenommen. Bei 4,5 m wird er abgelehnt.

### 3. Wegschauen und Laufen

Die Formel `look_away_pause` ist definiert als:

`look_away_pause = time_since_target_seen > look_grace`

Die Formel `walk_pause` ist definiert als:

`walk_pause = walk_time >= walk_grace`, wobei `walk_time` die zusammenhängende Zeit zählt, in der
`horizontal_speed > (walk_pause ? resume_speed : pause_speed)` gilt (Hysterese gegen Flackern beim
Abbremsen); danach springt der Zähler auf 0.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| time_since_target_seen | float | ≥ 0 s | calculated | Zeit seit der Strahl die Station zuletzt traf |
| look_grace | float | 0–0,5 s (Start 0,2) | data file | Kulanz gegen Flackern des Strahls |
| horizontal_speed | float | 0–8 m/s | calculated | Waagrechtes Tempo der eigenen Figur; Kameradrehen zählt nicht |
| pause_speed | float | 0,2–1,5 m/s (Start 0,5) | data file | Ab hier zählt es als Laufen |
| resume_speed | float | 0,1 bis `pause_speed` (Start 0,25) | data file | Ab hier gilt man wieder als stehend |
| walk_grace | float | 0–0,3 s (Start 0,1) | data file | Mindestdauer des Laufens |
| walk_time | float | ≥ 0 s | calculated | siehe oben |
| look_away_pause, walk_pause | bool | true/false | calculated | Client meldet Pause |

**Output Range:** Wegschauen und Laufen erkennt der Client selbst, ohne Verzögerung, und meldet beides per
RPC. „Reichweite verlassen“ ist dasselbe wie Wegschauen (der Strahl reicht nur 2,5 m). Loslassen wirkt
sofort, ohne Kulanz. Während der Kulanz läuft der Fortschritt weiter, das sind höchstens etwa 0,25 s,
rund 2 % einer Kurbelfahrt zu zweit.

**Example:** Ein Kamerastoß von 0,12 s pausiert nicht; ein bewusstes Wegdrehen von 0,35 s pausiert nach
0,2 s. Ein Rempler mit 0,7 m/s für 0,05 s pausiert nicht; ein Schritt mit 3,2 m/s pausiert nach 0,1 s.

### 4. Prüfung der Handlungsdaten (Editor)

Die Formel `action_data_valid` ist definiert als:

`action_data_valid = (duration == 0 OR duration >= min_duration) AND duration <= max_solo_duration AND
max_users >= 1 AND max_users <= 4 AND extra_user_bonus >= 0 AND extra_user_bonus <= 1 AND
(duration == 0 OR crew_multiplier(max_users) <= max_solo_penalty)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| duration | float | 0–20 s | data file | Dauer allein |
| min_duration | float | 0,25–1 s (Start 0,5) | data file | Kleinste Dauer über 0 |
| max_solo_duration | float | 10–30 s (Start 20) | data file | Längste Dauer allein |
| max_users | int | 1–4 | data file | Höchstzahl Nutzer |
| extra_user_bonus | float | 0–1 | data file | Zuschlag |
| max_solo_penalty | float | 1,5–3 (Start 2,0) | data file | Wie viel langsamer allein als voll besetzt höchstens |
| action_data_valid | bool | true/false | calculated | false = Fehler im Editor |

**Output Range:** true oder false. Bei 3 Nutzern ist der Zuschlag damit höchstens 0,5, bei 4 Nutzern
höchstens 0,33.

**Example:** Kurbel mit 20 s, 2 Nutzern und Zuschlag 0,75 ist gültig (1,75 ≤ 2). Ein Generator mit 25 s
ist ungültig, ebenso eine Handlung mit 3 Nutzern und Zuschlag 0,75 (2,5 > 2).

### 5. Anteil der Saison an Stationen (Hinweis)

Die Formel `solo_hold_share` ist definiert als:

`solo_hold_share = SUM(count_i * duration_i) / season_length`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| count_i | int | 0–50 | calculated | Geschätzte Ausführungen der Handlung i je Saison (aus den Dokumenten von Ofen, Gästen, Ereignissen, Seilbahn) |
| duration_i | float | 0–20 s | data file | Dauer allein der Handlung i |
| season_length | float | 1800–2400 s | calculated | Spielzeit der Saison, kommt aus `design/gdd/saison-ablauf.md` (Formel 2) |
| solo_hold_share | float | ≥ 0 | calculated | Anteil der Saison, den ein Einzelspieler an Stationen hält (ohne Wege) |

**Output Range:** Ziel ≤ 0,25. Über 0,35 fühlt sich das Spiel nach Knopfhalten an, dann sinken Dauern
oder Anzahlen. Das ist eine Faustregel für dieses Dokument und die Dokumente der Stationen, **keine
Editor-Prüfung**: Die Anzahlen `count_i` stehen in keiner Datendatei. Prüfen lässt sich der Wert mit
`/balance-check`, sobald die Stationen ihre Anzahlen festlegen.

**Example:** Mit den Startdauern unten und geschätzten Anzahlen (12 Klötze, 8 Betten, 3 Erste Hilfe,
2 Generator, 6 Fahrten): 320 s / 2100 s = 0,15.

### Startdauern (vorläufig, die Dokumente der Stationen legen sie fest)

| Handlung | Allein | Max. Nutzer | Zuschlag | Zu zweit | Begründung |
|---|---|---|---|---|---|
| Holz hacken | 6 s | 1 | – | – | Routine, kurz; die Axt gibt es nur einmal in der Hand |
| Bett machen | 8 s | 2 | 0,5 | 5,3 s | Ruhige Gemütlichkeits-Handlung, eine Person je Bettseite |
| Erste Hilfe | 8 s | 1 | – | – | Notfall im Sturm, kurz; der Kasten nur einmal in der Hand |
| Generator reparieren | 20 s | 2 | 0,75 | 11,4 s | Ereignis; die Frist im Ereignis-Dokument sollte mindestens 40 s, besser 60 s betragen |
| Fenster schließen | sofort | 1 | – | – | So im Detailed Design |
| Kurbeln (eine Fahrt) | 20 s | 2 | 0,75 | 11,4 s | Transport, Teamwork; ob der Fortschritt die Position des Wagens ist, legt die Seilbahn fest |

**Zielgröße:** Jede Station soll mindestens **0,4 m** groß sein, damit man sie sicher anvisiert (bei 2,5 m
Abstand etwa 9° Sichtwinkel). Gemessen wird die kleinste Kante der Bounds ihres Interaktions-Colliders;
darunter gibt der Editor einen Hinweis.

## Edge Cases

**Ziel und Anvisieren**

- **Wenn ein Gegenstand auf einer Station liegt** (Klotz auf dem Hackstock): Für die linke Maustaste gilt
  er als die Station, für E als Gegenstand. Der Hinweis zeigt für jede Taste, was sie wirklich tut: mit
  der Axt in der Hand „E: Axt ablegen · Linke Maustaste halten: Holz hacken“, mit leeren Händen
  „E: Holzklotz aufheben · Linke Maustaste: braucht eine Axt“.
- **Wenn der Klick mit der linken Maustaste den Mauszeiger einfängt** (nach Esc): Er fängt nur den
  Zeiger ein und löst keine Handlung aus.
- **Wenn ein Mitspieler zwischen Spieler und Station steht:** Der Strahl ignoriert Spielerfiguren. In der
  engen Hütte würde man sich sonst ständig gegenseitig die Station verdecken.
- **Wenn man mit Teller oder Axt in der Hand nach unten schaut:** Der Strahl ignoriert alles, was Spieler
  in der Hand oder am Körper tragen, und trifft den Gast oder den Hackstock dahinter. Ein Gegenstand, den
  ein **anderer** Spieler gerade trägt, ist ebenfalls kein Ziel.
- **Wenn die Station kleiner als 0,4 m ist:** Der Editor gibt einen Hinweis, weil man sie schwer trifft.

**Gleichzeitig und Netz**

- **Wenn zwei Spieler den letzten Platz wollen:** Der Wunsch, der zuerst beim Host ankommt, gewinnt. Der
  andere bekommt „Besetzt“, sein Balken verschwindet.
- **Wenn ein Client die Verbindung verliert, während er hält:** Der Host nimmt ihn aus den Nutzern, der
  Fortschritt bleibt am Bezugsobjekt.
- **Wenn der Host selbst spielt:** Er hat keinen Ping, sonst gelten dieselben Regeln.

**Während des Haltens**

- **Wenn man während des Haltens E oder die rechte Maustaste drückt:** Das Benutzen endet wie beim
  Loslassen, danach passiert das Aufheben, Ablegen oder Werfen.
- **Wenn das nötige Werkzeug aus der Hand fällt** (abgelegt, geworfen, Sturz): Die Bedingung fehlt, die
  Handlung pausiert, und der Hinweis nennt das Werkzeug.
- **Wenn eine Bedingung wegfällt** (der Gast geht weg): Die Station wird „Gesperrt“, alle pausieren, der
  Hinweis nennt den Grund.
- **Wenn das Bezugsobjekt weggenommen wird** (Klotz vom Hackstock gehoben): Alle Nutzer werden frei, die
  Station hat kein Bezugsobjekt mehr. Der Fortschritt bleibt am Klotz und gilt wieder, sobald er
  zurückliegt.
- **Wenn das Bezugsobjekt zerstört wird oder ein Ereignis endet:** Der Fortschritt verfällt, alle Nutzer
  werden frei.
- **Wenn eine Handlung fertig wird, während noch jemand hält:** Der Host führt sie genau einmal aus, alle
  Nutzer werden frei. Für die nächste Handlung muss man die Taste neu drücken. Weiterhalten startet
  nichts Neues, damit man nicht aus Versehen den nächsten Klotz hackt.
- **Wenn eine Handlung die Dauer 0 hat:** Sie passiert beim Klick, gedrückt halten bewirkt nichts.

**Ausrüstung**

- **Wenn man schon eine Lampe trägt und eine zweite aufheben will:** Das geht nicht, der Hinweis sagt
  „Du trägst schon eine Stirnlampe“. Dasselbe gilt für das Funkgerät.
- **Wenn ein Spieler mit Ausrüstung hinfällt (Ragdoll):** Die Ausrüstung bleibt am Körper.
- **Wenn ein Spieler mit Ausrüstung die Verbindung verliert:** Der Host lässt Lampe und Funkgerät an
  seiner letzten Position auf Hüfthöhe als Gegenstände erscheinen, verschoben auf den nächsten freien
  Punkt, nie in einer Wand (wie beim Ablegen).
- **Wenn am Ausrüstungshaken kein Platz für eine Art frei ist** (je 4 für Lampen und für Funkgeräte):
  Dieses Stück bleibt am Körper, der Hinweis sagt „Haken voll“. Was Platz hat, wird aufgehängt.
- **Wenn man mit etwas in der Hand E auf eine Stirnlampe drückt:** E legt das Gehaltene ab, die Lampe
  bleibt liegen. Für Ausrüstung braucht man freie Hände.

## Dependencies

**Hängt ab von:**

| System | Art | Schnittstelle |
| --- | --- | --- |
| Netzwerk-Sitzung | hart | Wünsche per RPC an den Host, Ergebnisse an alle |
| Bewegung und Kamera | hart | Blickrichtung und Augenposition für den Strahl, waagrechtes Tempo für „Laufen“ |
| Tragen und Werfen | hart | Liefert, was man gerade in der Hand hat (nur lesen). E und rechte Maustaste gehören dem Tragen. |
| Gegenstandsarten | hart | Werkzeug-Handlung und Kategorie des Gehaltenen. Stirnlampe und Funkgerät gelten als Ausrüstung. |
| Saison-Ablauf und Spielphasen | weich | Spielzeit `game_time`: `dt` des Fortschritts ist vergangene Spielzeit, in der Pause solo 0 (`design/gdd/saison-ablauf.md`) |

**Wird gebraucht von:**

| System | Art | Schnittstelle | Stand |
| --- | --- | --- | --- |
| Tragen und Werfen, Tragen zu zweit | hart | Nutzen den gemeinsamen Strahl (das aktuelle Ziel, nur lesen). Beim Bau werden die beiden eigenen Strahlen ersetzt. | gebaut, wird umgestellt |
| Holz hacken | hart | Handlung „Holz hacken“ am Hackstock, Bezugsobjekt Klotz | kein Dokument |
| Kochen und Gerichte | hart | Handlungen je Rezeptschritt | kein Dokument |
| Materialseilbahn | hart | Handlung „Kurbeln“, zu zweit schneller | kein Dokument |
| Versorgung: Betten und Erste Hilfe | hart | „Bett machen“, „Erste Hilfe“ (Bezugsobjekt Gast) | kein Dokument |
| Ereignisse (mit Strom und Licht) | hart | „Fenster schließen“, „Generator reparieren“; Stirnlampe am Körper | kein Dokument |
| Gäste und Bedürfnisse | hart | Handlung „Servieren“ am Gast, mit Gericht in der Hand | kein Dokument |
| HUD | hart | Hinweis (Taste, Handlung, Grund) und Fortschrittsbalken | kein Dokument |
| Nachteinsatz | weich | Erste Hilfe am Vermissten, Stirnlampe | kein Dokument |
| Sprachchat und Funkgerät | weich | Funkgerät am Gürtel | kein Dokument |
| Tutorial (Alpha) | weich | Erklärt die drei Tasten | kein Dokument |

Der **Holzofen** hängt nicht an der Interaktion: Holz kommt ohne Taste hinein (vor die Ofentür legen
oder werfen).

**Kreis Tragen ↔ Interaktion:** Die Interaktion besitzt den Strahl und stellt das aktuelle Ziel bereit,
das Tragen stellt den gehaltenen Gegenstand bereit. Beide lesen nur beim anderen. So muss keines der
beiden die Abläufe des anderen kennen.

**Beidseitig:** Bis auf das Tragen hat noch keins dieser Systeme ein Design-Dokument. Jedes muss beim
Schreiben „Interaktion“ unter seinen Abhängigkeiten nennen.

## Tuning Knobs

Bei Stufe `standard` nur empfohlen, auf Hinweis des Design-Reviews ergänzt. Alle Werte liegen in
Datenobjekten, nie im Code: in den **Interaktions-Einstellungen** (ein Datenobjekt für das ganze Spiel)
oder **je Handlung** bzw. **je Station**.

| Stellschraube | Start | Sicherer Bereich | Ablage | Wirkung |
| --- | --- | --- | --- | --- |
| `interact_range` | 2,5 m | 1–4 m | Interaktions-Einstellungen | Wie weit der Strahl reicht. Größer = bequemer, aber man aktiviert Dinge aus Versehen durch den Raum. |
| `max_server_distance` | 4,0 m | 2,5–6 m | Interaktions-Einstellungen | Grenze des Hosts für Tragen und Benutzen. Muss die Einstellregel erfüllen (sonst Fehler). |
| `max_player_speed` | 4 m/s | muss der Bewegung entsprechen | Interaktions-Einstellungen | Grundlage des Spielraums für Ping. |
| `rtt_budget` | 0,25 s | 0,1–0,5 s | Interaktions-Einstellungen | Eingeplanter Ping mit Reserve. Größer = toleranter bei schlechtem Netz, leichter zu schummeln. |
| `interp_delay` | 0,1 s (geschätzt) | 0–0,3 s | Interaktions-Einstellungen | Verzögerung der Figur beim Host; beim Bau messen. |
| `look_grace` | 0,2 s | 0–0,5 s | Interaktions-Einstellungen | Kulanz beim Wegschauen. Kürzer = Balken flackert; länger = Fortschritt ohne Blick. |
| `pause_speed` / `resume_speed` | 0,5 / 0,25 m/s | 0,2–1,5 / 0,1 bis `pause_speed` | Interaktions-Einstellungen | Ab wann Laufen pausiert bzw. wieder als Stehen gilt. |
| `walk_grace` | 0,1 s | 0–0,3 s | Interaktions-Einstellungen | Mindestdauer des Laufens vor der Pause. |
| `min_duration` / `max_solo_duration` | 0,5 s / 20 s | 0,25–1 s / 10–30 s | Interaktions-Einstellungen | Erlaubte Dauern einer Handlung (Editor-Prüfung). |
| `max_solo_penalty` | 2,0 | 1,5–3 | Interaktions-Einstellungen | Wie viel schneller voll besetzt höchstens ist als allein. |
| Zielgröße | 0,4 m | 0,3–0,6 m | Interaktions-Einstellungen | Kleinste Kante des Interaktions-Colliders, darunter Editor-Hinweis. |
| `duration` | je Handlung (Tabelle „Startdauern“) | 0 oder 0,5–20 s | je Handlung | Dauer allein. |
| `max_users` / `extra_user_bonus` | je Handlung | 1–4 / 0–1 | je Handlung | Teamwork an dieser Station. |
| Hakenplätze | je 4 für Lampen und Funkgeräte | 1–4 | je Station (Ausrüstungshaken) | Wie viel Ausrüstung der Haken aufnimmt. |

Wechselwirkungen: `interact_range`, `max_player_speed`, `rtt_budget` und `interp_delay` bestimmen
zusammen, wie groß `max_server_distance` mindestens sein muss. `max_solo_penalty` begrenzt, welche
Kombinationen aus `max_users` und `extra_user_bonus` erlaubt sind.

## Visual/Audio Requirements

- Das Ziel, auf das man schaut, wird dezent hervorgehoben. Stationen und Gegenstände lassen sich dabei
  unterscheiden.
- Ein Fortschrittsbalken über der Station ist für alle sichtbar. Zu zweit sieht man, dass er schneller
  wächst.
- Jede Handlung hat ihr Geräusch, zum Beispiel Axtschläge im Takt des Fortschritts oder das Ratschen der
  Kurbel. Welches genau, legt die jeweilige Station fest.
- Beim Klick gibt es sofort ein kurzes Griff-Geräusch beim eigenen Client. Bei „Besetzt“ oder einer
  Ablehnung kommt ein dumpfer Ton.
- Die Ausrüstung sieht man an der Figur: die Lampe am Kopf, das Funkgerät am Gürtel.

`art-director` und `audio-director` nicht befragt (Kategorie Core, Lean-Modus).

## Game Feel

- Ein Blick, ein Druck: Der Hinweis erscheint sofort beim Anvisieren.
- Ein Klick fühlt sich trotz Ping sofort an, weil die Rückmeldung beim Client im selben Frame kommt.
- Das Halten soll körperlich wirken: Die Kamera wippt leicht im Takt, der Balken wächst gleichmäßig.
- Zu zweit soll man das Tempo spüren, der Balken ist schneller und das Geräusch dichter.
- Nichts flackert und nichts frustriert, dafür sorgen die 0,2 s Kulanz beim Wegschauen und die
  Laufschwelle.

## UI Requirements

- **Hinweis am Fadenkreuz**, eine Zeile je Taste: „Taste: Handlung“ oder der Grund, warum es nicht geht,
  zum Beispiel „braucht eine Axt“, „Besetzt“, „Erst die Kiste ablegen“ oder „Haken voll“.
- **Fortschrittsbalken** mit der Zahl der Nutzer.
- **Kleine Symbole** für die getragene Ausrüstung (Lampe, Funkgerät).
- **Debug-Anzeige** an der Station (Nutzer, Faktor, Fortschritt), nur während der Entwicklung.

> **UX-Hinweis:** Hinweis am Fadenkreuz, Fortschrittsbalken und Ausrüstungs-Symbole gehören ins HUD. Vor
> den Epics mit `/ux-design` ein UX-Dokument dafür anlegen (`design/ux/hud.md`).

## Cross-References

| Verweis | Quelle | Genutzt in |
|---|---|---|
| Werkzeug-Handlungen `hacken`, `erste_hilfe`, `licht`, `funk`; Kategorien; Ausnahme für Ausrüstung in Regel 6 | `design/gdd/gegenstandsarten.md` (Approved) | Regeln 3 und 8 |
| Reichweite 2,5 m (doppelt im Code), Grenze des Hosts 4 m, Ablegen auf einen freien Punkt | `PlayerCarry`, `PlayerHeavyCarry` | Regel 2, Formel 2, Ausrüstung beim Verbindungsabbruch |
| Lauftempo 4 m/s | `PlayerMovement` | Formeln 2 und 3 |
| Mauszeiger einfangen mit der linken Maustaste | `PlayerLook` | Grenzfall, Kriterium 8 |
| Aktionen `Player/Interact` und `Player/Attack` | `Assets/InputSystem_Actions.inputactions` | Regel 1 |
| Holz kommt per Trigger in den Ofen | `Stove` | Tabelle der Interaktionen |
| Architektur-Regel 2 | CLAUDE.md | Regel 4 |

## Acceptance Criteria

Vorschlag von `qa-lead` (2026-11-08), an die Entscheidungen des Entwicklers angepasst und freigegeben.

**Damit die Tests verlässlich bleiben:** Alle Formeln sind reine Funktionen, Werte kommen als Parameter,
ohne echte Uhr und ohne Zufall; der Schritt des Hosts bekommt `dt` übergeben. PlayMode-Tests nutzen
`dt = 0,02`, `Physics.simulationMode = SimulationMode.Script` und `Physics.Simulate(0.02f)`; Test-Stubs
schicken die Wünsche der Nutzer direkt an den Host, ohne Netz. Grenzwerte werden an den reinen Funktionen
geprüft, Schrittfolgen enden nie genau auf einer Grenze. Toleranz ±0,01 bei m, s und Fortschritt.

**Messweg im Netz:** Log-Zeilen
`[Interact] t=<ServerTime> <accept|reject|pause|resume|done|remove> station= action= client= users= progress= dist= reason=`
und eine Debug-Anzeige an der Station (users, crew_multiplier, progress). Beides gehört in den Bau.
Zweispielige Kriterien werden beim Freundestest geprüft, nicht mit zwei Fenstern.

Testarten: **[EditMode]** automatischer Unit-Test unter `Assets/Tests/EditMode/`, **[PlayMode]**
automatischer Test mit Physik, **[Manuell]** solo im Editor nach Anleitung, **[Freundestest]** zu zweit
übers Netz.

### A. Tasten und Ziel (Regeln 1, 2, 9)

1. **[EditMode]** **GIVEN** das Input-Actions-Asset **WHEN** die Aktionen Aufheben, Benutzen und Werfen
   geladen werden **THEN** gibt es jede genau einmal mit der Standardbelegung E, linke Maustaste, rechte
   Maustaste und keine Belegung doppelt. *Codeprüfung:* In `Carrying/` und im Interaktionsordner gibt es
   0 Treffer für `Keyboard.current` und `Mouse.current`.
2. **[Manuell]** Solo: **GIVEN** eine Kiste in der Hand **WHEN** linke Maustaste, rechte Maustaste bzw. E
   **THEN** bleibt sie in der Hand, fliegt bzw. liegt abgelegt, jeweils 5/5.
3. **[PlayMode]** **GIVEN** Augen bei (0; 1,6; 0), Blick +z, eine Station als Würfel mit 0,5 m Kante
   **WHEN** ihre Vorderfläche bei 2,49 m bzw. 2,51 m liegt **THEN** ist das Ziel die Station bzw. es gibt
   kein Ziel. **GIVEN** statt der Station ein Gegenstand bei 2,0 m **THEN** ist das Ziel der Gegenstand.
4. **[PlayMode]** **GIVEN** eine Station bei 2,0 m **WHEN** eine fremde Spielerfigur bei 1,0 m
   dazwischensteht **THEN** ist das Ziel die Station. **WHEN** stattdessen eine Wand dazwischensteht
   **THEN** gibt es kein Ziel, und die linke Maustaste schickt 0 Wünsche.
5. **[PlayMode + Codeprüfung]** **GIVEN** Kiste A bei 1,5 m im Blick, Kiste B bei 2,0 m dahinter **WHEN** E
   **THEN** wird A getragen; genauso packt E das angeschaute Truhenende. `Physics.Raycast` zum Anvisieren
   gibt es nur noch in der Interaktion (0 Treffer in `PlayerCarry` und `PlayerHeavyCarry`).
6. **[PlayMode]** **GIVEN** ein Spieler ist am Host Nutzer an Station A **WHEN** ein Wunsch für Station B
   ankommt **THEN** ist er nach dem nächsten Schritt des Hosts an höchstens 1 Station Nutzer.
7. **[Manuell]** **GIVEN** ein Klotz liegt auf dem Hackstock **WHEN** man ihn ansieht, einmal mit der Axt
   und einmal mit leeren Händen **THEN** zeigt der Hinweis „E: Axt ablegen · Linke Maustaste halten: Holz
   hacken“ bzw. „E: Holzklotz aufheben · Linke Maustaste: braucht eine Axt“; E wirkt nur auf das Tragen,
   die linke Maustaste nur auf das Hacken.
8. **[Manuell]** **GIVEN** der Mauszeiger ist frei (nach Esc) und der Blick liegt auf einer Station
   **WHEN** man mit der linken Maustaste den Zeiger einfängt **THEN** wird nur der Zeiger gefangen, und das
   Log zeigt keinen Wunsch.

### B. Handlungen und Fortschritt (Regeln 3, 4, 5, 7; `progress_rate`)

9. **[EditMode]** `crew_multiplier` **GIVEN** (users; max_users; extra_user_bonus) = (0;2;0,75) /
   (1;2;0,75) / (2;2;0,75) / (3;2;0,75) / (2;2;0,5) / (4;4;0,33) / (3;1;0,5) **THEN** 0 / 1 / 1,75 /
   1,75 / 1,5 / 1,99 / 1.
10. **[EditMode]** **GIVEN** duration 20 **WHEN** crew 1 bzw. 1,75 **THEN** Rate 0,05 bzw. 0,0875;
    Restzeit ab 0 ist 20,0 bzw. 11,43, ab 0,4 ist sie 12,0 bzw. 6,86. **GIVEN** crew 0 **THEN** Rate 0 und
    „keine Restzeit“ (kein Infinity). **GIVEN** duration 0 **THEN** „sofort“, ohne Rate, ohne NaN, ohne
    Exception.
11. **[EditMode]** **GIVEN** progress 0,99 und Rate 0,0875 **WHEN** dt 0,2 **THEN** progress = 1,0 (der
    Überschuss verfällt) und „fertig“ kommt genau 1-mal.
12. **[EditMode]** Zustände: **GIVEN** eine Handlung **WHEN** „Nutzer hält“, „alle lassen los“, „drückt
    neu“, „100 %“ **THEN** Bereit → In Arbeit → Pausiert → In Arbeit → Fertig → Bereit (Fortschritt 0).
    **WHEN** bei Fortschritt 0,3 eine Bedingung fehlt und mit demselben Objekt zurückkommt **THEN**
    Gesperrt → Pausiert (0,3). **WHEN** ein anderes Bezugsobjekt mit Fortschritt 0 kommt **THEN** Bereit.
13. **[PlayMode]** **GIVEN** eine Testhandlung mit 6 s und 1 haltendem Nutzer **WHEN** 295 Schritte
    **THEN** progress 0,983, nicht fertig. **WHEN** 305 Schritte **THEN** genau 1 Ausführung,
    Fortschritt 0, der Nutzer ist frei. **WHEN** er 100 weitere Schritte hält **THEN** bleibt es bei
    1 Ausführung und Fortschritt 0. **WHEN** er neu drückt **THEN** wächst der Fortschritt wieder.
14. **[PlayMode]** **GIVEN** eine Handlung mit Dauer 0 **WHEN** Klick und 150 Schritte (3 s) gehalten
    **THEN** genau 1 Ausführung, kein Balken, Fortschritt bleibt 0.
15. **[PlayMode]** **GIVEN** A hackt Klotz K1 bis 0,40 und lässt los **WHEN** B hält **THEN** startet B bei
    0,40. **WHEN** K1 weggenommen und wieder aufgelegt wird **THEN** geht es bei 0,40 weiter. **WHEN**
    stattdessen K2 aufgelegt wird **THEN** Fortschritt 0, Zustand Bereit.
16. **[PlayMode]** **GIVEN** A hält bei 0,3 und meldet `walk_pause` **WHEN** er stehen bleibt, die Taste
    weiter drückt und auf die Station schaut **THEN** zählt er im nächsten Schritt wieder als Nutzer, und
    der Fortschritt wächst ab 0,3. **WHEN** er mit gedrückter Taste auf eine andere Station schwenkt
    **THEN** passiert dort nichts.
17. **[PlayMode]** **GIVEN** 2 Nutzer halten bei 0,5 **WHEN** die Station „Bedingung fehlt“ meldet **THEN**
    Gesperrt, users 0, nach 50 Schritten Fortschritt 0,5, der Hinweis enthält den Grund-Text. **WHEN** die
    Bedingung mit demselben Objekt zurückkommt und beide neu drücken **THEN** geht es ab 0,5 weiter.
18. **[PlayMode]** **GIVEN** die Handlung „Holz hacken“ (Werkzeug `hacken`) **WHEN** mit Axt, Holzscheit
    bzw. leerer Hand **THEN** angenommen / abgelehnt / abgelehnt, der Hinweis nennt „Axt“. **WHEN** man die
    Test-Art „Testbeil“ mit `hacken` hält **THEN** angenommen (der Code prüft keinen Namen). **GIVEN** eine
    Testhandlung mit Kategorie Gericht **WHEN** man Tee bzw. ein Holzscheit hält **THEN** angenommen bzw.
    abgelehnt. **GIVEN** `egal` **WHEN** man eine Kiste hält **THEN** angenommen.
19. **[PlayMode]** **GIVEN** Hacken läuft bei 0,5 **WHEN** die Axt die Hand verlässt (Ablegen, Werfen,
    Sturz als Testaufruf) **THEN** zählt der Nutzer nicht mehr, der Fortschritt bleibt 0,5, der Hinweis
    nennt „Axt“.
20. **[Manuell]** **GIVEN** eine Kiste in der Hand vor einer Station mit `freie Hände` **WHEN** linke
    Maustaste **THEN** zeigt der Hinweis „Erst die Kiste ablegen“, das Log zeigt keinen Wunsch, die Kiste
    bleibt in der Hand. **GIVEN** man hält ein Truhenende **WHEN** linke Maustaste auf eine Station
    **THEN** zeigt das Log keinen Wunsch.
21. **[Manuell]** **GIVEN** man hackt mit der Axt **WHEN** rechte Maustaste **THEN** stoppt der Balken und
    die Axt fliegt; **WHEN** stattdessen E **THEN** stoppt der Balken und die Axt liegt. In beiden Fällen
    bleibt der Fortschritt am Klotz.

### C. Mehrere Nutzer (Regel 6)

22. **[PlayMode]** **GIVEN** die Kurbel (20 s, max 2, Zuschlag 0,75) und 2 haltende Nutzer **WHEN**
    560 Schritte **THEN** progress 0,98, nicht fertig; **WHEN** 575 Schritte **THEN** genau 1 Ausführung.
    Allein ist sie nach 990 Schritten nicht fertig, nach 1010 Schritten fertig.
23. **[PlayMode]** **GIVEN** die Kurbel ist voll **WHEN** ein dritter Wunsch kommt **THEN** „Besetzt“,
    users bleibt 2, crew 1,75. **WHEN** einer der beiden `walk_pause` meldet **THEN** im nächsten Schritt
    users 1 und crew 1,0; der Dritte rückt **nicht** von selbst nach, erst sein neuer Druck wird
    angenommen; danach bekommt der Pausierte beim Weiterhalten „Besetzt“.
24. **[EditMode]** **GIVEN** 1 freier Platz **WHEN** im selben Schritt die Wünsche in der Reihenfolge
    [P2, P3] ankommen **THEN** wird P2 angenommen und P3 bekommt „Besetzt“; bei [P3, P2] umgekehrt.
25. **[PlayMode]** **GIVEN** eine Handlung mit Werkzeug `hacken` und max 2 (Test) **WHEN** zwei Nutzer
    halten, nur einer mit Axt **THEN** zählt nur der mit Axt, crew 1,0.
26. **[Manuell]** Solo: **GIVEN** die Kurbel **WHEN** man sie allein hält **THEN** ist sie nach 20 s ±0,5
    fertig (Debug-Anzeige), und das Log zeigt `accept` und `done` über denselben Wunsch-Pfad wie bei einem
    Client.

### D. Reichweite und Kulanz (`host_range_ok`, Einstellregel, `look_away_pause`, `walk_pause`)

27. **[EditMode]** **GIVEN** max_server_distance 4,0 **WHEN** target_distance 3,99 / 4,0 / 4,01 / 4,5
    **THEN** true / true / false / false.
28. **[EditMode]** Einstellregel: **GIVEN** die Startwerte (2,5; 4; 0,25; 0,1) **THEN** range_slack 1,40,
    Summe 3,90 ≤ 4,0, keine Meldung. **GIVEN** max_player_speed 6 **THEN** Summe 4,60 > 4,0 und genau
    1 **Fehler**. **GIVEN** zusätzlich max_server_distance 5,0 **THEN** keine Meldung.
29. **[PlayMode]** Messweg: **GIVEN** ein Würfel mit 1 m Kante, die Figurwurzel 4,4 m vom Würfelmittelpunkt,
    die Augen 3,9 m vom nächsten Punkt **THEN** angenommen; bei 4,1 m von den Augen zum nächsten Punkt
    abgelehnt mit `reason=range`. **GIVEN** max_server_distance 3,0 **WHEN** Aufheben, Truhenende anpacken
    und Benutzen bei 3,5 m **THEN** alle drei abgelehnt (ein gemeinsamer Wert). *Heute misst `PlayerCarry`
    von der Wurzel zur Gegenstandsmitte und `PlayerHeavyCarry` mit 2,5 m; der Test wird erst nach dem
    Umbau grün.*
30. **[PlayMode]** **GIVEN** ein Nutzer hält bei 0,5 **WHEN** seine Augen auf 4,1 m rücken **THEN** users 0,
    Fortschritt 0,5. **WHEN** aus 10 m „Loslassen“ kommt **THEN** wird es angenommen.
31. **[EditMode]** **GIVEN** look_grace 0,2 **WHEN** time_since_target_seen 0,12 / 0,2 / 0,21 / 0,35
    **THEN** false / false / true / true. **GIVEN** look_grace 0 **WHEN** 0 / 0,01 **THEN** false / true.
32. **[EditMode]** `walk_pause` mit dt 0,01, pause 0,5, resume 0,25, grace 0,1: (a) 5 Schritte mit
    0,7 m/s, dann 0: nie eine Pause. (b) 3,2 m/s: nach 9 Schritten keine Pause, nach 11 Schritten Pause.
    (c) danach 100 Schritte mit 0,4 m/s: die Pause bleibt; dann 1 Schritt mit 0,2: keine Pause mehr,
    walk_time 0. (d) aus dem Stand 200 Schritte mit 0,4 m/s: nie eine Pause. (e) Geschwindigkeit
    (0; −5; 0) und (0,3; 0; 0,3): keine Pause. Grenzfall der reinen Funktion: walk_time 0,1 bei grace 0,1
    ergibt Pause.
33. **[Manuell]** Solo, Kurbel halten, je 5/5: kurzer Mausruck (unter 0,2 s weg) → Balken läuft weiter;
    bewusst wegdrehen → Balken stoppt; ein Schritt → Balken stoppt; stehen bleiben, Taste gedrückt,
    zurückschauen → Balken läuft weiter; über 2,5 m zurückweichen → Balken stoppt.

### E. Ausrüstung (Regel 8)

34. **[PlayMode]** **GIVEN** leere Hände **WHEN** E auf die Stirnlampe **THEN** sitzt sie auf dem Kopfplatz,
    die Hand ist leer, sie ist kein freier Gegenstand mehr; danach trägt E auf einer Kiste die Kiste. Das
    Funkgerät verhält sich genauso am Gürtel, beides geht gleichzeitig. **GIVEN** eine Kiste in der Hand
    **WHEN** E auf die Lampe **THEN** wird die Kiste abgelegt, die Lampe bleibt liegen.
35. **[PlayMode]** **GIVEN** der Kopfplatz ist belegt **WHEN** E auf eine zweite Lampe **THEN** passiert
    nichts, der Hinweis sagt „Du trägst schon eine Stirnlampe“; beim Funkgerät genauso.
36. **[PlayMode]** **GIVEN** eine Handlung mit Werkzeug `licht` (Test) **WHEN** man die Lampe am Kopf
    trägt und die Hände leer sind **THEN** wird sie angenommen.
37. **[PlayMode]** **GIVEN** Lampe und Funkgerät am Körper **WHEN** „Aufhängen“ am Haken mit freien Plätzen
    **THEN** sind beide Plätze am Körper leer und beides hängt am Haken. **GIVEN** alle 4 Lampenplätze
    belegt **THEN** wird nur das Funkgerät aufgehängt, die Lampe bleibt am Kopf, der Hinweis sagt „Haken
    voll“. E und rechte Maustaste mit leerer Hand lösen die Ausrüstung nie.
38. **[PlayMode]** **GIVEN** ein Spieler mit Lampe und Funkgerät **WHEN** der Host seinen
    Verbindungsabbruch verarbeitet (Testaufruf) **THEN** erscheinen genau 1 Lampe und 1 Funkgerät als
    Gegenstände, höchstens 1 m von seiner letzten Position, auf Hüfthöhe und nicht in einer Wand; die
    Gesamtzahl der Lampen bleibt gleich.
39. **[Manuell]** *(sobald Hinfallen gebaut ist)* Bei einem Sturz (Ragdoll) bleiben Lampe und Funkgerät am
    Körper.

### F. Datenprüfung (`action_data_valid`, Zielgröße, `solo_hold_share`)

40. **[EditMode]** **GIVEN** min 0,5, max 20, penalty 2,0, Tupel (Dauer; Nutzer; Zuschlag): **gültig**
    (20;2;0,75), (20;1;0), (0;4;1,0), (0,5;1;0), (6;3;0,5), (8;4;0,33); **ungültig** (25;2;0,75),
    (20,01;1;0), (0,25;1;0), (−1;1;0), (8;3;0,75), (8;4;0,34), (6;0;0), (6;5;0), (6;2;−0,1), (6;2;1,1).
    Jede ungültige ergibt genau 1 Fehler mit ihrem Asset-Namen (Vergleich mit Toleranz 0,0001). Steht
    max_solo_duration im Datenobjekt auf 30, ist (25;2;0,75) gültig.
41. **[EditMode]** *(sobald die Stationen gebaut sind)* Die ausgelieferten Handlungen ergeben 0 Fehler und
    entsprechen der Tabelle „Startdauern“. *Schnappschuss: Tabelle und Test werden zusammen geändert.*
42. **[EditMode]** Eine Station, deren Interaktions-Collider eine kleinste Kante von 0,39 m hat, ergibt
    1 Hinweis, mit 0,40 m nichts; nie ein Fehler.
43. **[EditMode]** `solo_hold_share` als reine Rechnung: **GIVEN** 12×6, 8×8, 3×8, 2×20 und 6×20 Sekunden
    **WHEN** die Saison 2100 s bzw. 1800 s dauert **THEN** 0,152 bzw. 0,178. Es gibt keine Meldung im
    Editor (Faustregel, siehe Formulas).

### G. Netz und Zusammenspiel (Freundestest, Client mit etwa 100 ms)

44. **[Freundestest]** Tragen nach dem Umbau: Kiste aufheben, ablegen, mit der rechten Maustaste werfen,
    die Truhe zu zweit tragen. Die linke Maustaste mit einer Kiste in der Hand wirft nicht. 10 von 10
    Aufheben-Versuchen bei höchstens 2,5 m stehen im Host-Log als `accept`.
45. **[Freundestest]** **GIVEN** der Client steht vor einer Sofort-Handlung **WHEN** er klickt **THEN**
    stehen im Client-Log `click` und `local-feedback` im selben Frame; `result` kommt beim Client höchstens
    gemessene RTT + 0,1 s nach `click` an.
46. **[Freundestest]** Kurbel zu zweit: Das Host-Log zeigt users=2 und crew=1.75; zwischen dem ersten
    `accept` und `done` liegen 11,4 ±0,3 s. Kurbelt der Client allein, sind es 20,0 ±0,3 s. Beim Client
    zeigt das Log den ersten Fortschritt über 0 höchstens 0,3 s nach dem `accept` des Hosts.
47. **[Freundestest]** **GIVEN** der Client hält die Kurbel allein **WHEN** er sich wegdreht **THEN** steht
    `pause reason=look` beim Host höchstens 0,35 s nach `target-lost` beim Client, und der Fortschritt des
    Hosts liegt höchstens 0,02 über dem des Clients. Bei einem Schritt gilt das mit `reason=walk` und
    höchstens 0,25 s.
48. **[Freundestest]** **GIVEN** der Client läuft mit vollem Tempo an einem Fenster vorbei **WHEN** er
    klickt, solange der Strahl es trifft **THEN** werden 10 von 10 Wünschen angenommen, jeder `dist`-Wert
    im Host-Log ist höchstens 4,0.
49. **[Freundestest]** **GIVEN** eine Debug-Station mit Höchstzahl 1 und ohne Werkzeug **WHEN** beide auf
    Zuruf drücken, 5 Durchgänge **THEN** je Durchgang genau 1 `accept` und 1 `reject reason=full`; der
    Abgelehnte sieht „Besetzt“ und keinen Balken.
50. **[Freundestest]** **GIVEN** der Client hält die Kurbel allein bis etwa 0,5 **WHEN** er das Spiel
    beendet **THEN** zeigt das Host-Log `remove reason=disconnect` und users 0, der Fortschritt bleibt 5 s
    lang unverändert; hält danach der Host, geht es ab diesem Wert weiter. Trug der Client Lampe und
    Funkgerät, erscheinen beide beim Host, die Zählung im Log ist vorher und nachher gleich.

### H. Nachträge aus dem Design-Review

51. **[PlayMode]** **GIVEN** ein Spieler hält einen Teller (Haltepunkt 0,5 m unter den Augen, 0,9 m davor)
    und blickt 30° nach unten auf eine Station bei 1,5 m **WHEN** der Strahl auswertet **THEN** ist das
    Ziel die Station, nicht der Teller. **GIVEN** ein anderer Spieler trägt eine Kiste zwischen beiden
    **THEN** ist auch diese Kiste kein Ziel.
52. **[PlayMode]** **GIVEN** eine Lampe hängt an einem Hakenplatz (Zustand „Liegt“) **WHEN** ein Spieler
    mit leeren Händen und freiem Kopfplatz E drückt **THEN** ist der Hakenplatz leer und die Lampe „Am
    Körper“ dieses Spielers.

## Open Questions

| # | Frage | Wer klärt | Bis wann |
|---|---|---|---|
| 1 | Dauern, Anzahlen und Höchstzahl der Nutzer endgültig festlegen, danach `solo_hold_share` mit `/balance-check` prüfen | Dokumente der Stationen | bei deren Design |
| 2 | Verzögerung der Interpolation (geschätzt 0,1 s) und Tickrate des Hosts messen (die Kamerahöhe ist bekannt: 0,7 m über der Mitte der Spielfigur) | Programmierung | beim Bau |
| 3 | Tasten frei belegbar machen; dazu als Option für Barrierefreiheit „Halten als Umschalten“ (einmal drücken startet, nochmal drücken stoppt) | Dokument zu den Menüs | bei dessen Design |
| 4 | Gamepad und Steam Deck: Belegung festlegen (das Konzept nennt Steam Deck) | Dokumente zu Menüs und HUD | vor der Demo |
| 5 | Log-Format `[Interact]` und Debug-Anzeige | ADR oder Bau | beim Bau |
| 6 | Interaktion übers Netz: wie die RPCs aussehen und wer den Fortschritt besitzt | `/architecture-decision` | vor dem Bau |
| 7 | Werfen auf die rechte Maustaste ändert die Steuerung für den Freund, `docs/FREUNDESTEST.md` muss angepasst werden | Bau | beim Bau |
