# Playtest-Bericht: Freundestest 2

## Sitzung

- **Datum:** 2026-11-09
- **Build:** Prototyp 0.1.4 (Release v0.1.4). Der Spielcode hat sich seit v0.1.4 nicht geändert.
- **Dauer:** nicht erfasst
- **Tester:** Entwickler und ein Freund (2 Spieler)
- **Plattform / Eingabe:** PC (Windows), Maus und Tastatur
- **Art:** Gezielter Test. Er schließt den offenen Punkt aus dem Gate-Check vom 2026-11-08
  (`production/gate-checks/gate-2026-11-08-prototyp.md`): Zweiertest mit 0.1.4.
- **Quelle:** Kurze Rückmeldung des Entwicklers nach dem Test. Es gibt keine Notizen. Auf dem Entwickler-PC
  liegt keine `Player.log` von diesem Test, die letzte stammt vom Update-Test am 2026-11-08 um 19:28.

## Testfokus

Weiter die Prüfpunkt-Frage des Prototyps: **Macht es Spaß, zu zweit eine schwere Kiste durch eine Hütte
zu tragen?** Neu gegenüber Freundestest 1 (Build 0.1.0) waren: Spielfiguren statt Kapseln, „Gewicht
spüren“ (Kamera wippt, schwere Kisten bremsen, die Truhe schaukelt), Farben und Licht, Geräusche, die
Versionsprüfung und der Update-Knopf.

Aus den Top 3 von Freundestest 1 sollten geprüft werden:

1. Heben „Gewicht spüren“ und die Geräusche den Spaß von „ganz okay“ auf „ja“?
2. Hört man zu zweit alles (Aufpralle, Schritte des Partners, Ofen, Wind)?
3. Mitschreiben: lustige Momente, wer Host war, Ping, Dauer.

## Erste Eindrücke

Nicht erhoben.

## Spielablauf

### Was gut funktioniert hat

- **Alles funktioniert:** „es funktioniert alles“ (Entwickler). Es gab keine Abstürze, keine Hänger und
  keine Verbindungsprobleme.
- **Geräusche zu zweit:** „er hört alles“. Top-3-Punkt 2 aus Freundestest 1 ist damit erledigt.
- **Netz:** „es läuft flüssig“. Im echten Online-Spiel war keine störende Verzögerung zu merken.
- **Versionen:** Die Verbindung kam zustande, also passten die Versionen beider Spieler zusammen.

### Schwachstellen

- **Spaß nur unter Bedingungen.** Der Freund sagt selbst: Wenn das Spiel Texturen hat, mehr Physik und
  etwas zu tun, „kann es richtig Spaß machen“. Er sieht das Potenzial. Ein klares Ja zum Tragen, so wie
  es heute ist, ist es aber nicht. Schweregrad: Mittel. Das ist die Kernfrage des Prototyps.
- **„Physik“ fehlt ihm.** Der Prototyp hat Physik: Kisten fallen, lassen sich werfen und rutschen. Dass
  der Freund trotzdem „Physik“ vermisst, deutet darauf hin, dass das **Physik-Chaos noch nicht spürbar**
  ist: Es fällt zu wenig um, es geht zu wenig schief. Das ist eine Deutung, nachgefragt wurde es nicht.
  Schweregrad: Mittel.
- **Es gibt nichts zu tun.** Im Prototyp gibt es kein Ziel außer Tragen und Heizen. Das war Absicht: Der
  Prototyp prüft nur das Tragen. Schweregrad: Niedrig für den Prototyp, hoch für den Vertical Slice.
- **Keine Texturen.** Die Hütte ist eine Graubox mit Farben. Das war ebenfalls Absicht. Schweregrad:
  Niedrig für den Prototyp.

### Unklarheiten

Keine gemeldet.

### Schöne Momente

Nicht erhoben.

## Fehler

Keine gemeldet.

| # | Beschreibung | Schweregrad | Reproduzierbar |
| --- | --- | --- | --- |
| – | – | – | – |

## Rückmeldung zu einzelnen Funktionen

### Truhe zu zweit und „Gewicht spüren“

Nicht einzeln erhoben. Die Fragen aus `docs/FREUNDESTEST.md` (Schaukeln, Drehen, Ruck beim Anheben)
blieben offen.

### Geräusche

- **Hört der Freund alles?** Ja.
- Ob er Host oder Client war, ist nicht erfasst. Vermutlich war er Client, weil der Entwickler das
  Hosting über die Portweiterleitung eingerichtet hat.

### Netz

Läuft flüssig. Der Ping ist nicht erfasst.

## Messwerte

Nicht erhoben (wer Host war, Ping, Dauer, Zeit für die Truhenstrecke). Top-3-Punkt 3 aus Freundestest 1
ist damit noch offen.

## Gesamteindruck

- **Nochmal spielen?** Nicht direkt gefragt. Der Freund will das Spiel sehen, wenn mehr drin ist.
- **Spaß an der Kernfrage:** Bedingtes Ja. Das Tragen funktioniert und trägt die Idee, richtig Spaß
  erwartet er erst mit Aussehen, mehr Physik-Chaos und einer Aufgabe.
- **Technik:** Ja. Netz, Ton und Stabilität halten im echten Online-Spiel.

## Abgleich mit der Absicht (Säulen aus CLAUDE.md)

- **Gemütlichkeit:** Fehlt noch sichtbar („Texturen“). Farben und Licht aus 0.1.2 reichen dafür nicht.
  Erwartet für eine Graubox.
- **Physik-Chaos:** Die schwächste Säule im heutigen Build. Der Freund vermisst sie, obwohl die Physik
  technisch da ist. Im Prototyp kippt und rutscht zu wenig von selbst.
- **Teamwork unter Druck:** Das Teamwork klappt online. Der Druck fehlt („was zu tun“), denn ohne Gäste,
  Uhr und Phasen gibt es keinen Grund zur Eile. Das liefern erst Saison-Ablauf und Gäste.

## Einordnung der Befunde

- **Design:**
  - *Etwas zu tun:* Das deckt der geplante Vertical Slice ab. Die GDDs für Saison-Ablauf, Gegenstandsarten,
    Interaktion und Gäste sind freigegeben, Kochen und Gerichte ist das nächste (Reihenfolge Nr. 6).
  - *Mehr Physik-Chaos:* Im Vertical Slice zahlen die Materialseilbahn (schaukelnde Kiste im Wind),
    Ereignisse (Fenster fliegt auf, Dachlawine) und Figuren-Animation mit Ragdoll darauf ein. Dazu kommt die
    Idee in `docs/IDEEN.md` (lässt einer los, fällt nur sein Ende der Truhe). Vorher sollte geklärt
    werden, was der Freund mit „Physik“ meint.
  - Kein Widerspruch zu einem GDD. Die Befunde bestätigen die Planung für den Vertical Slice.
- **Balance:** Keine Befunde.
- **Fehler:** Keine.
- **Feinschliff und Grafik:** Texturen und ein einheitlicher Look gehören zum Vertical Slice. Eine
  Art Bible gibt es noch nicht (`/art-bible`).

## Top 3 aus diesem Test

1. **Nachfragen, was „mehr Physik“ heißt:** Umfallen, Rutschen, Werfen, die Truhe kippt, man stolpert?
   Die Antwort zeigt, wo Säule 2 im Vertical Slice zuerst ansetzen muss.
2. **Etwas zu tun geben:** Der Vertical Slice mit Gästen, Kochen und Saison-Ablauf ist der direkte
   Weg dorthin. Den Zweck des Tragens („wofür schleppe ich das?“) liefern die Gäste.
3. **Look klären:** Texturen und Stimmung über eine Art Bible festlegen, bevor Assets entstehen.

Außerdem gilt weiter: Beim nächsten Test mitschreiben (Host, Ping, Dauer, lustige Momente).

## Creative-Director-Prüfung

CD-PLAYTEST übersprungen (Lean-Modus, kein Phasen-Gate).
