# Playtest-Bericht: Freundestest 1

## Sitzung

- **Datum:** 2026-11-08, vormittags
- **Build:** Stand vom Vormittag, vermutlich Prototyp 0.1.0 (Release v0.1.0, um `db887fe`).
  Laut Git-Zeiten lag der Test vor „Spielfiguren statt Kapseln“ (`ebdc0f2`, 12:33).
- **Dauer:** nicht erfasst
- **Tester:** Entwickler und ein Freund (2 Spieler)
- **Plattform / Eingabe:** PC (Windows), Maus und Tastatur
- **Art:** Gezielter Test der Prüfpunkt-Frage
- **Quelle:** Mündliche Rückmeldung des Entwicklers am Abend (Fragebogen). Es gibt keine Notizen
  und keine Logs: Die `Player.log` vom Vormittag ist inzwischen überschrieben.

## Testfokus

Die Prüfpunkt-Frage des Prototyps: **Macht es Spaß, zu zweit eine schwere Kiste durch eine Hütte zu
tragen?** Im Build enthalten waren: Kisten aufheben, tragen, ablegen und werfen; die Truhe, die sich nur
zu zweit bewegt; Hängenbleiben an Türrahmen; Client-Vorhersage; der Holzofen.

**Nicht im Build, deshalb nicht zu zweit getestet:** Spielfiguren, „Gewicht spüren“ (Kamera wippt,
schwere Kisten bremsen, Truhe schaukelt), Farben und Licht, Geräusche, Versionsprüfung, Update-Knopf.
All das kam ab 0.1.1 dazu.

## Erste Eindrücke

Nicht erhoben.

## Spielablauf

### Was gut funktioniert hat

- Tragen und die Truhe zu zweit: „das hat gut funktioniert“ (Entwickler).
- Die Ecke im Vorraum mit der Truhe ist **genau richtig**: Man muss sich absprechen, kommt aber durch.

### Schwachstellen

- Der Spaß war nur **„ganz okay“**: Es funktioniert, hat aber noch nicht richtig gezündet.
  Schweregrad: Mittel. Das ist die Kernfrage des Prototyps.

### Unklarheiten

Keine gemeldet.

### Schöne Momente

Nicht erhoben.

## Fehler

Keine gemeldet (keine Abstürze, keine Hänger, keine störende Verzögerung).

| # | Beschreibung | Schweregrad | Reproduzierbar |
| --- | --- | --- | --- |
| – | – | – | – |

## Rückmeldung zu einzelnen Funktionen

### Truhe zu zweit

- **Zweck verstanden?** Ja
- **Hat es gepackt?** Teilweise („ganz okay“)
- **Ecke im Vorraum:** genau richtig

### Geräusche

Zu zweit noch nicht getestet. Der Freund kommt heute nicht mehr.

## Messwerte

Nicht erhoben (wer Host war, Ping, Dauer, Zeit für die Truhenstrecke).

## Gesamteindruck

- **Nochmal spielen?** Nicht erhoben
- **Schwierigkeit (Ecke):** Genau richtig
- **Spaß an der Kernfrage:** Ganz okay, noch kein klares Ja

## Abgleich mit der Absicht (Säulen aus CLAUDE.md)

- **Teamwork unter Druck:** Die Absprache an der Ecke funktioniert wie gewollt.
- **Physik-Chaos:** Der Spaß blieb bei „ganz okay“. Das passt dazu, dass Gewicht und Chaos in diesem
  Build kaum spürbar waren. „Gewicht spüren“ kam erst danach und ist zu zweit ungetestet.
- **Gemütlichkeit:** Im getesteten Build noch nicht vorhanden (keine Farben, kein Licht, kein Ton).

## Einordnung der Befunde

- **Design:** Spaß nur „ganz okay“ statt klar ja. Ob „Gewicht spüren“ und die Geräusche den Funken
  bringen, klärt erst ein Zweiertest mit 0.1.4. Es gibt noch kein GDD (Stufe `minimal`), maßgeblich
  ist `design/game-brief.md`.
- **Balance:** Keine Befunde.
- **Fehler:** Keine.
- **Feinschliff:** Die Idee in `docs/IDEEN.md` (lässt einer los, fällt nur sein Ende der Truhe).

## Top 3 aus diesem Test

1. **Freundestest mit 0.1.4:** Heben „Gewicht spüren“ und die Geräusche den Spaß von „ganz okay“
   auf „ja“?
2. **Geräusche zu zweit prüfen:** Der Client hört Aufpralle, Schritte des Partners, Ofen und Wind;
   der eigene Wurf klingt ohne Verzögerung.
3. **Nächstes Mal mitschreiben:** lustige Momente, wer Host war, Ping, Dauer. So enthält der Bericht
   mehr als die Kernfrage.

## Creative-Director-Prüfung

CD-PLAYTEST übersprungen (Solo-Modus).
