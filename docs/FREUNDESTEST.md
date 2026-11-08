# Freundestest: Prototyp-Prüfpunkt

**Die eine Frage:** Macht es Spaß, zu zweit eine schwere Kiste durch eine Hütte zu tragen?

Build: `Builds/Schutzhuette-Prototyp-0.1.0.zip` (Windows, 64 Bit). Version 0.1.0.

## Vorbereitung (jeder Spieler)

1. Zip entpacken, `Schutzhuette.exe` starten. Das Spiel öffnet sich in einem Fenster.
2. Windows warnt eventuell vor einem „unbekannten Herausgeber“, weil der Build nicht signiert ist.
   Wer dem Build vertraut, klickt auf **Weitere Informationen → Trotzdem ausführen**.
3. Sprachchat nebenher (zum Beispiel Discord): Das Spiel hat noch keinen.

## Verbinden (1 Host, bis zu 3 weitere Spieler)

- **Host:** **„Host starten“**. Beim ersten Mal fragt die Windows-Firewall: Zugriff erlauben.
- **Alle anderen:** Unter **„Adresse des Hosts“** die Adresse eintragen, dann **„Beitreten“**.

Welche Adresse?

| Situation | Adresse | Was der Host vorher tun muss |
| --- | --- | --- |
| Alle im selben Netz (zu Hause, LAN) | Die lokale IPv4-Adresse des Host-PCs, zum Beispiel `192.168.1.23`. Zu finden mit `ipconfig` in der Eingabeaufforderung. | Nichts außer der Firewall-Freigabe. |
| Übers Internet, mit Portfreigabe | Die öffentliche IP des Hosts (zum Beispiel über eine „Wie ist meine IP?“-Webseite). | Im Router **UDP-Port 7777** an den Host-PC weiterleiten. |
| Übers Internet, ohne Router-Zugriff | Die virtuelle IP des Hosts im gemeinsamen virtuellen Netz. | Alle installieren dasselbe virtuelle LAN-Programm (zum Beispiel ZeroTier, Tailscale oder Radmin VPN) und treten demselben Netz bei. |

Der Port ist fest **7777**. Allein testen geht auch: Spiel zweimal starten, im einen „Host starten“,
im anderen mit `127.0.0.1` „Beitreten“.

## Steuerung

| Taste | Wirkung |
| --- | --- |
| W A S D | Laufen |
| Maus | Umsehen |
| E | Kiste oder Holz aufheben / ablegen, Truhenende anpacken / loslassen |
| Linksklick | Getragene Kiste oder Holz werfen |
| Esc | Maus freigeben (für die Knöpfe) |
| Linksklick ins Bild | Maus wieder einfangen |

Oben links: Ping und FPS. Oben rechts: Ofen (Brenndauer, Wärme).

## Aufgaben für eine Runde (15 bis 20 Minuten)

1. **Aufwärmen:** Jeder bringt eine Kiste vom Hangfuß ins Lager.
2. **Die Truhe:** Zu zweit die Truhe (rechts am Hangfuß) den Hang hinauf, durch den Eingang, um die
   Ecke im Vorraum in die Stube bis zum Ofen bringen.
3. **Der Ofen:** Während zwei die Truhe tragen, hält jemand den Ofen am Brennen: Holz vom Holzplatz
   (rechts am Hangfuß) holen und vor die Ofentür legen oder werfen.
4. **Frei spielen:** Werfen, Chaos, eigene Ideen.

Danach Host wechseln und noch eine Runde, damit beide Seiten (Host und Client) gespielt werden.

## Was ihr notiert (für `/playtest-report`)

- **Lustige Momente:** Was ist passiert, wer hat gelacht, gute Zitate.
- **Genervt:** Wo seid ihr hängen geblieben, was fühlte sich träge oder unfair an?
- **Truhe:** Klappt die Abstimmung zu zweit? Ist die Ecke im Vorraum zu leicht, genau richtig oder zu schwer?
- **Netz:** Wer war Host, welcher Ping stand oben links, wie hat sich das Tragen als Client angefühlt?
- **Fehler:** Was ist passiert, was habt ihr kurz davor gemacht? Die Logdatei liegt unter
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Schutzhütte\Player.log`, bitte mitschicken.

Mit den Notizen danach `/playtest-report` aufrufen. Am Prüfpunkt entscheidet `/gate-check`, ob es
zum Vertical Slice weitergeht.

## Bekannte Grenzen dieses Builds

- Kein Sprachchat, kein Steam, keine Lobby: Verbindung nur über die IP-Adresse.
- Keine künstliche Verzögerung: Der Network Simulator wirkt nur im Unity-Editor. Übers Internet gibt
  es echte Verzögerung.
- Lässt einer die Truhe los, fällt die ganze Truhe flach herunter (Idee in `docs/IDEEN.md`).
- Graubox-Grafik, keine Animationen.
- Holz als Client in den Ofen werfen wurde im Editor noch nicht getestet.
