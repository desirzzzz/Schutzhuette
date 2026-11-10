# Interaction Pattern Library

> **Status**: Draft (alle 17 Muster vom Entwickler freigegeben, noch ohne `/ux-review`)
> **Author**: Entwickler + ux-designer (`/ux-design patterns`)
> **Last Updated**: 2026-11-10
> **Template**: Interaction Pattern Library

---

## Overview

Diese Bibliothek legt fest, wie sich jede Art von Bedienung im ganzen Spiel verhält. UX-Dokumente (z. B.
`design/ux/hud.md`) nennen die Muster beim Namen, statt sie neu zu beschreiben. Braucht ein Bildschirm ein
neues Muster, kommt es zuerst hierher.

Es gibt noch keine UX-Dokumente für einzelne Bildschirme. Die Muster stammen aus den UI Requirements der
GDDs (Interaktion, Gäste, Gegenstandsarten, Saison-Ablauf, Steam-Lobby), aus
`design/accessibility-requirements.md` und aus dem Prototyp.

**Für alle Muster gilt:**
- **Der Host entscheidet** (Architektur-Regel 2). Der eigene Client zeigt sofort, was er selbst weiß, das
  Ergebnis kommt vom Host.
- **Nie nur Farbe**, zu jeder Taste ein Text, mindestens Stufe Standard aus
  `design/accessibility-requirements.md`.
- **Texte aus den Daten**, nie fest im Code (Architektur-Regel 4).
- Die Muster beschreiben das Verhalten, nicht die Technik. UI Toolkit oder UGUI entscheidet ADR-0007
  (`docs/architecture/architecture.md`, QQ-04).

---

## Pattern Catalog

| Pattern | Category | Used In | Status |
|---|---|---|---|
| Hinweis am Fadenkreuz | Input / Feedback | HUD, alles Anvisierbare | Draft |
| Halten mit Fortschrittsbalken | Input / Feedback | Stationen mit Dauer | Draft |
| Tastenhinweis passt sich dem Gerät an | Input | Hinweise, Menüs, Hilfe | Draft |
| Schild in der Welt | Data Display | Gäste, Namensschilder | Draft |
| Kurze Rückmeldung | Feedback | Trinkgeld | Draft |
| Status-Symbol | Data Display | Ausrüstung im HUD | Draft |
| Liste im HUD | Data Display | offene Wünsche | Draft |
| Phase mit Vorwarnung | Data Display / Feedback | Saison-Uhr | Draft |
| Markieren | Input / Absprache | Hütte, Nachteinsatz | Draft |
| Rufe-Rad | Input / Absprache | Hütte, Nachteinsatz | Draft |
| Richtungspfeil am Bildschirmrand | Feedback / Data Display | Hilferuf, Markierungen | Draft |
| Meldung, die stehen bleibt | Modal / Feedback | Verbindung, Beitritt, Abbruch | Draft |
| Rückfrage | Modal | Abbrechen, Verlassen, Beenden, Umbelegen | Draft |
| Nur für den Host | Navigation / Netz | Vorbereitung, Bewertung, Menü | Draft |
| Bereit-Liste | Data Display / Netz | Vorbereitung | Draft |
| Menü mit Maus und Gamepad | Navigation | alle Menüs | Draft |
| Einstellungs-Zeilen | Input | Einstellungen | Draft |

---

## Patterns

### A. In der Welt handeln

### Hinweis am Fadenkreuz

**Category**: Input / Feedback
**Used In**: HUD; alles, was man anvisiert (Gegenstände, Stationen, Gäste, Ausrüstung)

**Description**: Sagt, was die Tasten bei dem bewirken, worauf man gerade schaut, oder warum es nicht geht.
Er erscheint, sobald der Strahl der Interaktion (2,5 m, `interaktion.md` Regel 2) ein Ziel trifft, und
verschwindet beim Wegschauen. Quellen: `interaktion.md`, `gaeste.md` und `gegenstandsarten.md`, jeweils
UI Requirements.

**Specification**:
- **Aufbau:** unter dem Fadenkreuz, eine Zeile je Taste, feste Reihenfolge: E, linke Maustaste, rechte
  Maustaste. Tasten ohne Wirkung fehlen. Beispiel: „E: Axt ablegen“ und darunter „Linke Maustaste halten:
  Holz hacken“.
- **Inhalt aus den Daten:** Anzeigename („E: Holzscheit aufheben“), Paketinhalt („Vorratskiste (Speck,
  6 Portionen)“), Wunsch eines Gastes („Möchte einen Tee“). Kein Text fest im Code (Architektur-Regel 4).
- **Wenn es nicht geht:** Statt der Handlung steht der Grund, z. B. „braucht eine Axt“, „Besetzt“, „Erst die
  Kiste ablegen“, „Haken voll“. Der Grund steht immer als Text da, nie nur als ausgegraute Zeile.
- **Tempo:** erscheint und verschwindet sofort, ohne Einblenden, damit die Steuerung ohne Verzögerung wirkt.
- **Netz:** Jeder Spieler berechnet seinen Hinweis selbst aus seinem Blick und dem Zustand, den der Host
  verteilt. Ob die Handlung klappt, entscheidet der Host (Architektur-Regel 2).
- **Barrierefreiheit:** mindestens 20 px bei 1080p; Kasten oder Schatten dahinter, lesbar über Schnee und
  bei Nacht (Kontrast 4,5:1); Taste immer als Text (Muster „Tastenhinweis passt sich dem Gerät an“);
  verdeckt weder das Fadenkreuz noch den Fortschrittsbalken.

**When to Use**: Jede Handlung und jede Information, die am anvisierten Ding hängt.
**When NOT to Use**: Warnungen und Meldungen (→ Meldung, die stehen bleibt), Informationen ohne Bezug zum
Anvisierten (→ Liste im HUD).

**Reference**:
```
                    +
   ┌─────────────────────────────────────┐
   │ E: Axt ablegen                      │
   │ Linke Maustaste halten: Holz hacken │
   └─────────────────────────────────────┘
```

### Halten mit Fortschrittsbalken

**Category**: Input / Feedback
**Used In**: Holz hacken, Kurbel der Seilbahn, Bett machen, Erste Hilfe, Generator reparieren (alle
Handlungen mit Dauer > 0, `interaktion.md` Regeln 5 und 6)

**Description**: Zeigt den Fortschritt einer Handlung, für die man die linke Maustaste hält. Mehrere Spieler
können gemeinsam halten, dann geht es schneller. Der Fortschritt bleibt beim Loslassen am Objekt erhalten.

**Specification**:

| Zustand | Was man sieht und hört |
|---|---|
| Bereit | Hinweis „Linke Maustaste halten: Holz hacken“, kein Balken |
| In Arbeit | Balken unter dem Hinweis füllt sich gleichmäßig, dazu „2 Spieler“, wenn mehrere helfen. Arbeitsgeräusch, die Kamera wippt leicht (`interaktion.md`, Game Feel). |
| Pausiert (losgelassen, gelaufen, weggeschaut) | Balken bleibt stehen, daneben der Text „Pausiert“. Fortschritt bleibt am Bezugsobjekt. |
| Fertig | Balken kurz voll, Abschlussgeräusch, dann weg |
| Gesperrt | kein Balken; der Grund steht im Hinweis am Fadenkreuz |

- **Wer den Balken sieht:** jeder, der die Station anvisiert, auch Zuschauer. So sieht man einen halb
  gehackten Klotz.
- **Netz:** Der Host zählt den Fortschritt. Der eigene Balken läuft zwischen den Meldungen des Hosts weich
  weiter, damit er auch bei 100 ms Ping nicht ruckelt.
- **Umschalt-Modus** (`design/accessibility-requirements.md`, Motor): Ein Druck gilt als gehalten, bis man
  nochmal drückt, E oder die rechte Maustaste drückt, die Handlung fertig ist oder die Station aus Blick
  oder Reichweite verliert. Laufen pausiert wie beim Halten; wer wieder stehen bleibt und auf die Station
  schaut, macht weiter. Der Hinweis lautet dann „Linke Maustaste: Holz hacken (an/aus)“.
- **Barrierefreiheit:** Den Stand zeigt die Länge des Balkens, nicht seine Farbe. „Kamerabewegung
  reduzieren“ schaltet das Wippen ab. Kein schnelles Drücken.

**When to Use**: Jede Handlung mit Dauer > 0.
**When NOT to Use**: Handlungen mit Dauer 0, die passieren beim Klick (`interaktion.md`, Regel 4).

**Reference**:
```
                    +
   ┌─────────────────────────────────────┐
   │ Linke Maustaste halten: Holz hacken │
   │ ███████████████░░░░░░░░   2 Spieler │
   └─────────────────────────────────────┘
```

### Tastenhinweis passt sich dem Gerät an

**Category**: Input
**Used In**: Hinweis am Fadenkreuz, Rufe-Rad, Menüs, Hilfeseite, Tutorial

**Description**: Jede Taste, die das Spiel nennt, erscheint so, wie sie auf dem gerade benutzten Gerät heißt,
und mit der Belegung, die der Spieler gewählt hat.

**Specification**:
- Zeigt immer die **aktuelle Belegung** des Spielers (nach dem Umbelegen die neue Taste), nie die
  Voreinstellung.
- Wechselt, sobald das andere Gerät benutzt wird: Tastendruck oder deutliche Mausbewegung für Maus und
  Tastatur, Knopf oder Stick über der Totzone für das Gamepad. Leichtes Zittern der Maus schaltet nicht um.
- Im Vertical Slice zeigen alle Gamepads die Xbox-Beschriftung (A, B, X, Y). PlayStation-Symbole: Open
  Questions.
- Nicht belegte Aktion: „nicht belegt“ statt einer fehlenden Zeile.
- **Barrierefreiheit:** Symbol und Text immer zusammen, nie nur das Symbol.

**When to Use**: Überall, wo das Spiel eine Taste nennt.
**When NOT to Use**: —

### B. Anzeigen

### Schild in der Welt

**Category**: Data Display
**Used In**: Gäste (Bedürfnis-Symbol, Geduldsring, Gesicht, „Zzz“), Namensschilder der Spieler

**Description**: Ein kleines Schild über einer Figur, das ihren Zustand auf einen Blick zeigt. Quellen:
`gaeste.md` (Visual/Audio und UI Requirements), `steam-lobby.md` (UI Requirements).

**Specification**:
- Immer zur Kamera gedreht. Lesbar aus 10 m; weiter weg wird das Schild nicht kleiner als eine Mindestgröße
  am Bildschirm (Pixelzahl in `design/ux/hud.md`).
- Wände verdecken das Schild (man sieht es nur, wenn man die Figur sieht). Möbel und Figuren verdecken es
  nicht, sonst würde ein Spieler mit Kiste die ganze Stube verdecken.
- **Netz:** Bedürfnis und Geduld kommen vom Host; der Ring läuft beim Client weich weiter.
- **Barrierefreiheit:** Geduldsring zeigt den Stand über den Füllstand, die Stimmung über das Gesicht;
  Spieler sind am Namen erkennbar, nicht nur an der Farbe (`design/accessibility-requirements.md`,
  Color-as-Only-Indicator Audit).
- Wie weit man Namensschilder sieht, legt `design/ux/hud.md` fest; draußen entscheidet der Nachteinsatz.

**When to Use**: Zustand einer Figur, den alle sehen sollen.
**When NOT to Use**: Längere Texte. Ein Schild trägt ein Symbol und höchstens ein Wort.

### Kurze Rückmeldung

**Category**: Feedback
**Used In**: Trinkgeld („+6“), später weitere Belohnungen

**Description**: Eine kleine Zahl oder ein Wort, das kurz bestätigt, was gerade passiert ist.

**Specification**:
- Steigt über dem Auslöser auf und verblasst nach etwa 1,5 s.
- Erscheint erst nach der Bestätigung durch den Host, zusammen mit dem Geräusch (z. B. Münzklimpern).
- **Barrierefreiheit:** Mit „Kamerabewegung reduzieren“ nur verblassen, nicht aufsteigen.

**When to Use**: Bestätigung, die man nicht lesen muss, um weiterzuspielen.
**When NOT to Use**: Alles, worauf man reagieren muss (→ Meldung, die stehen bleibt).

### Status-Symbol

**Category**: Data Display
**Used In**: HUD: getragene Stirnlampe und Funkgerät (`interaktion.md`, UI Requirements)

**Description**: Ein kleines Symbol für Ausrüstung am Körper und ihren Zustand.

**Specification**:
- Fester Platz im HUD (welcher, legt `design/ux/hud.md` fest).
- Zustand über die Form, nicht über die Farbe, z. B. ausgeschaltete Lampe als durchgestrichenes Symbol.

**When to Use**: Ausrüstung am Körper, die man selbst nicht sieht.
**When NOT to Use**: Dinge in der Hand, die sieht man.

### Liste im HUD

**Category**: Data Display
**Used In**: HUD: offene Wünsche der Gäste, z. B. „2× Tee, 1× Erste Hilfe“ (`gaeste.md`, UI Requirements)

**Description**: Eine kurze Übersicht, die ohne Tastendruck zeigt, was gerade zu tun ist.

**Specification**:
- Je Zeile Symbol, Anzahl und Name.
- Feste Reihenfolge nach Art, nicht nach Dringlichkeit: Die Liste springt nicht, „Tee“ steht immer an
  derselben Stelle. Wie dringend etwas ist, zeigen die Ringe über den Gästen.
- Neue Einträge 1 s hervorgehoben, durch Umrandung oder Helligkeit, nicht nur durch Farbe.
- Leer: „Alle versorgt“.

**When to Use**: Zusammenfassung über alle Gäste oder Stationen.
**When NOT to Use**: Details einzelner Gäste (→ Schild in der Welt).

### Phase mit Vorwarnung

**Category**: Data Display / Feedback
**Used In**: HUD: Saison-Uhr (`saison-ablauf.md`, UI Requirements)

**Description**: Zeigt, in welcher Phase die Saison ist, und kündigt den nächsten Wechsel an.

**Specification**:
- Immer sichtbar: Name der Phase (Nachmittag, Abend, Nacht) und die Zeit als Balken oder Uhr (Form in
  `design/ux/hud.md`).
- 30 s vor einem Wechsel: „Abend in 0:30“, zählt herunter, dazu ein Geräusch (z. B. Windstoß). In der
  Nacht „Morgengrauen in …“.
- **Barrierefreiheit:** Die Vorwarnung bleibt bis zum Wechsel stehen und blinkt nicht; sie ist sichtbar und
  hörbar.
- **Netz:** Zeit vom Host aus der Spielzeit (ADR-0003); in der Pause solo steht auch die Anzeige still.

**When to Use**: Zustand und Wechsel der Saison.
**When NOT to Use**: Einzelne Ereignisse (→ Meldung oder eigenes sichtbares Zeichen nach den Ereignis-Regeln).

### C. Absprache

> Neu aus `design/accessibility-requirements.md` (Auditory: Markieren und Rufe): Absprache ohne Stimme, vor
> allem im Nachteinsatz. Tasten sind Voreinstellungen und frei umbelegbar. Reichweite und Funk regeln im
> Detail die GDDs Nachteinsatz und Sprachchat.

### Markieren

**Category**: Input / Absprache
**Used In**: Hütte, Nachteinsatz

**Description**: Markiert für alle die Stelle oder das Ding, auf das man schaut.

**Specification**:
- Taste: Q, Gamepad Steuerkreuz oben.
- Markiert, worauf man schaut, bis etwa 50 m weit. Trifft man ein Ding oder einen Gast, hängt die Markierung
  daran, wandert mit und nennt es („Tee“, „Vermisster“).
- Zeigt den Namen des Spielers und die Entfernung in Metern.
- Höchstens eine Markierung je Spieler; eine neue ersetzt die alte. Verschwindet nach etwa 10 s.
- **Wer sie sieht:** in der Hütte alle. Draußen gilt die Reichweite der Stimme: nah direkt, weiter weg nur,
  wenn Sender und Empfänger ein Funkgerät tragen.
- **Netz:** Wunsch per RPC an den Host; der Host prüft, verteilt und lässt höchstens eine Markierung pro
  Sekunde und Spieler zu (Architektur-Regel 2).
- **Barrierefreiheit:** an Form und Name erkennbar, nicht nur an der Farbe; außerhalb des Bilds zeigt der
  Richtungspfeil hin; Empfänger hören einen kurzen Ton.

**When to Use**: Auf etwas zeigen, ohne zu sprechen.
**When NOT to Use**: Dauerhafte Ziele oder Aufgaben (das Spiel setzt keine Wegpunkte, siehe
`accessibility-requirements.md`, Weg finden).

### Rufe-Rad

**Category**: Input / Absprache
**Used In**: Hütte, Nachteinsatz

**Description**: Ein Rad mit kurzen Rufen, die als Text mit Name und als Laut der Figur ankommen.

**Specification**:
- V halten öffnet das Rad; mit Maus oder Stick in eine Richtung zeigen, loslassen sendet. Esc oder rechte
  Maustaste bricht ab. Umschalt-Modus: ein Druck öffnet, ein Klick wählt.
- Während das Rad offen ist, steht die Blickrichtung still (die Maus wählt); Laufen geht weiter.
- Rufe (Datenobjekte, Architektur-Regel 4): „Hierher!“, „Hilfe!“, „Gefunden!“, „Holz!“, „Ja“, „Nein“. Die
  endgültige Liste kommt aus den GDDs Nachteinsatz und Sprachchat.
- „Hierher!“ setzt zusätzlich eine Markierung an die eigene Position.
- Die anderen sehen eine Textzeile mit Name („Anna: Hierher!“), die letzten 3 Rufe, jeden 8 s lang, und hören
  einen kurzen Laut der Figur von ihrer Position.
- Reichweite und Netz wie bei „Markieren“.

**When to Use**: Kurze Absprache ohne Stimme.
**When NOT to Use**: Freie Texte (Steam-Chat), Meldungen des Spiels (→ Meldung, die stehen bleibt).

### Richtungspfeil am Bildschirmrand

**Category**: Feedback / Data Display
**Used In**: Hilferuf des Vermissten, Markierungen außerhalb des Bilds

**Description**: Zeigt am Bildschirmrand, in welcher Richtung eine Quelle liegt, die man nicht sieht.

**Specification**:
- Liegt die Quelle außerhalb des Bilds: Pfeil am Rand in ihre Richtung, daneben „[Hilferuf]“ oder der Name
  des Spielers. Liegt sie im Bild: kein Pfeil, die Anzeige sitzt an der Quelle.
- Dauer: solange der Ruf zu hören ist, plus 3 s; bei Markierungen, solange es sie gibt.
- Einstellung „Einblendungen für Geräusche“, standardmäßig an, abschaltbar. Der Pfeil gibt nur die Richtung,
  die Hörende über den Stereoton bekommen.
- **Barrierefreiheit:** an Form und Text erkennbar, blinkt nicht, Mindestgröße.

**When to Use**: Spielwichtige Quellen außerhalb des Blickfelds (Gameplay-Critical SFX Audit mit „Ja“).
**When NOT to Use**: Atmosphäre-Geräusche, Ziele ohne Geräusch oder Markierung.

### D. Menüs und Meldungen

### Meldung, die stehen bleibt

**Category**: Modal / Feedback
**Used In**: „Verbindung getrennt.“, Beitritt abgelehnt (`saison-ablauf.md` Regel 10, `steam-lobby.md`),
„Host hat die Saison abgebrochen“

**Description**: Eine Nachricht des Spiels, auf die man reagieren muss. Sie verschwindet nie von selbst
(`accessibility-requirements.md`, Lesezeit).

**Specification**:
- **Im Menü:** Kasten mit Text und einem Knopf „OK“, der den Fokus hat. Enter, A, Esc und B schließen.
- **Im laufenden Spiel nie als blockierender Kasten**, denn im Koop läuft das Spiel weiter. Stattdessen eine
  Zeile oben in der Mitte, die bleibt, bis man sie mit einer Taste schließt oder ihr Grund vorbei ist.
- Text sagt, was passiert ist und was man tun kann, z. B. „Gerade läuft eine Saison. Bitte nach der Bewertung
  erneut beitreten.“ Texte aus den Daten.
- Ein neutraler Ton beim Erscheinen.

**When to Use**: Fehler, Ablehnungen, Ereignisse der Sitzung.
**When NOT to Use**: Bestätigungen ohne Handlungsbedarf (→ Kurze Rückmeldung), Fragen (→ Rückfrage).

### Rückfrage

**Category**: Modal
**Used In**: „Saison abbrechen?“ (`saison-ablauf.md`, UI Requirements), „Aktuelles Spiel verlassen und
beitreten?“ (`steam-lobby.md`), Beenden als Host („Alle Spieler werden getrennt.“), Doppelbelegung beim
Umbelegen

**Description**: Fragt nach, bevor etwas passiert, das sich nicht zurücknehmen lässt.

**Specification**:
- Frage und zwei Knöpfe: einer heißt wie die Handlung („Saison abbrechen“), nicht „Ja“; der andere „Zurück“.
- Fokus zuerst auf dem sicheren Knopf „Zurück“. Esc und B bedeuten „Zurück“.
- **Barrierefreiheit:** Ein gefährlicher Knopf ist am Wort erkennbar, nicht nur an roter Farbe.

**When to Use**: Handlungen, die etwas beenden oder für alle ändern.
**When NOT to Use**: Alles, was sich leicht rückgängig machen lässt.

### Nur für den Host

**Category**: Navigation / Netz
**Used In**: „Saison starten“, „Nächste Saison“, „Beenden“ auf der Bewertung, „Saison abbrechen“
(`saison-ablauf.md`), Wahl „Gemütlich/Normal“ (`accessibility-requirements.md`, Cognitive)

**Description**: Bedienelemente, die nur der Host benutzen darf, und was alle anderen an ihrer Stelle sehen.

**Specification**:
- Der Host sieht Knopf oder Auswahl.
- Alle anderen sehen an derselben Stelle Text statt eines ausgegrauten Knopfs, z. B. „Wartet auf den Host
  (Anna)“ oder „Schwierigkeit: Gemütlich“.
- **Netz:** Wunsch per RPC an den Host; der Host prüft auch, dass der Wunsch von ihm selbst kommt
  (Architektur-Regel 2).

**When to Use**: Entscheidungen für die ganze Sitzung.
**When NOT to Use**: Persönliche Einstellungen (→ Einstellungs-Zeilen).

### Bereit-Liste

**Category**: Data Display / Netz
**Used In**: Vorbereitung (`saison-ablauf.md`, Regel 2)

**Description**: Zeigt allen, wer bereit für die nächste Saison ist.

**Specification**:
- Eine Zeile je Spieler mit Name und Zustand als Text und Häkchen, nicht nur als Farbe.
- Eigener Knopf „Bereit“ schaltet um.
- „Saison starten“ (Nur für den Host) ist immer drückbar, der Host entscheidet. Daneben „2 von 3 bereit“.

**When to Use**: Abstimmung vor dem Start.
**When NOT to Use**: Während der Saison.

### Menü mit Maus und Gamepad

**Category**: Navigation
**Used In**: alle Menüs

**Description**: Wie man sich in jedem Menü bewegt.

**Specification**:
- Bedienbar mit Maus, Tastatur (Pfeile, Enter, Esc) und Gamepad (Stick oder Steuerkreuz, A, B).
- Sichtbarer Fokus-Rahmen, nicht nur eine Farbe. Beim Öffnen liegt der Fokus auf dem meistgenutzten Knopf.
- Esc oder B geht immer eine Ebene zurück. Im Spiel öffnet Esc das Pausenmenü; im Koop steht dort sichtbar
  „Das Spiel läuft weiter“ (`saison-ablauf.md`, Regel 11).
- Mauszeiger wird beim Öffnen freigegeben und beim Schließen wieder eingefangen (wie heute `PlayerLook`).

**When to Use**: Jedes Menü.
**When NOT to Use**: Rufe-Rad (eigenes Muster).

### Einstellungs-Zeilen

**Category**: Input
**Used In**: Einstellungen (Grafik, Ton, Steuerung, Barrierefreiheit)

**Description**: Die Bausteine der Einstellungen.

**Specification**:
- **Regler:** Wert als Zahl („Musik 80 %“), auch mit Pfeilen oder Stick in Schritten.
- **Schalter:** „An“ oder „Aus“ als Text.
- **Auswahl:** ◀ Wert ▶
- **Taste umbelegen:** Klick, dann „Taste drücken …“, Esc bricht ab. Bei Doppelbelegung Rückfrage „Q ist schon
  für Markieren belegt. Tauschen?“ mit „Tauschen“ und „Zurück“. Je Zeile „Zurücksetzen“, dazu „Alles
  zurücksetzen“.
- Änderungen wirken sofort und werden lokal gespeichert, ohne Knopf „Übernehmen“.

**When to Use**: Persönliche Einstellungen.
**When NOT to Use**: Entscheidungen für die ganze Sitzung (→ Nur für den Host).

---

## Gaps & Patterns Needed

| Fehlendes Muster | Gebraucht für | Wann |
|---|---|---|
| Laden und Verbinden | Beitreten dauert (Steam-Lobby, IP-Weg) | bei den Menü-Dokumenten |
| Texteingabe | IP-Adresse beim Weg „Über IP spielen“ (`steam-lobby.md`) | bei den Menü-Dokumenten |
| Bewertung und Upgrade-Kauf | Bildschirme Bewertung und Upgrades | bei deren UX-Dokument |
| Tutorial-Hinweis | Tutorial (Alpha) | beim Tutorial |
| Standards für Animation und Klang der UI | alle Muster | sobald es eine Art Bible gibt |

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|---|---|---|---|
| PlayStation-Symbole und Steam Input: Welche Beschriftung zeigen andere Gamepads? | ux-designer | vor dem Bau der Einstellungen | offen |
| Schilder in der Welt mit UI Toolkit (World Space UI) oder UGUI? | ADR-0007 | vor Plan-Schritt 15 | offen |
| Q (Markieren) und V (Rufe-Rad) endgültig? Im Spieltest prüfen, ob sie sich mit anderen Tasten beißen. | Entwickler | beim Bau | offen |
| Wo stehen die Zeilen der Rufe? Wie groß und wie weit sichtbar sind die Namensschilder? | `design/ux/hud.md` | bei dessen Design | offen |
