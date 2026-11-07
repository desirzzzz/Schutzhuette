# Game Brief: Schutzhütte

<!-- Kurzfassung von docs/KONZEPT.md für das Framework (Stufe minimal).
     Bei Widersprüchen gilt KONZEPT.md. Engine steht in project.yaml (/setup-engine). -->

**One-sentence pitch:** 1–4 Freunde führen eine Alpenhütte durch eine Sturmnacht: kochen, heizen, Gäste versorgen und schwere Vorräte den Berg hinaufschleppen, bis im Schneesturm ein Gast vermisst wird.

## Core loop
- Saison (30–40 Minuten) in drei Phasen: Nachmittag (ruhig) → Abend (Sturm zieht auf, Andrang) → Nacht (Stromausfall, Notfälle, Nachteinsatz).
- Gäste versorgen (Essen, Betten, Wärme, Erste Hilfe) → Trinkgeld und 1–3 Edelweiß.
- Hüttenkasse → Upgrades → nächste Saison mit neuem Mix aus Gästen, Wetter und Ereignissen.

## Player goal & fail state — what "working" looks like
- Ziel: die Hütte mit zufriedenen Gästen durch die Nacht bringen und den Vermissten lebend zurückholen. Eine Saison fühlt sich vollständig an, wenn sie einen ruhigen Anfang, einen chaotischen Höhepunkt und eine Bewertung hat.
- Scheitern ist graduell: unzufriedene Gäste, wenig Trinkgeld, 1 Edelweiß.

## MVP — what must exist to be the game
- Tragen mit Gewicht, schwere Dinge nur zu zweit, für alle Spieler synchron (Online-Koop 1–4, Host-Client)
- Holzofen als Wärmequelle der ganzen Hütte, braucht ständig gehacktes Holz
- 5 Gerichte mit 2–4 Schritten und ein kleines Lager mit Nachschub über die Materialseilbahn
- 5 Gästetypen mit je einem klaren Bedürfnis
- Wetter-Eskalation in drei Phasen mit 8 Ereignissen
- Nachteinsatz: Vermissten suchen und zu zweit auf der Trage zurückbringen
- Bewertung, Hüttenkasse und 10 Upgrades, eine Hütte (Almhütte)

## Out of scope — not building this
- Keine Konsolen, kein Crossplay, kein PvP, kein Kampfsystem
- Keine offene Welt, keine prozedural erzeugten Karten
- Keine Story-Kampagne mit Zwischensequenzen, keine Mikrotransaktionen
- Nicht mehr als 4 Spieler

## Build order
1. Prototyp: NGO mit Host/Beitreten, synchronisierte First-Person-Bewegung, Graubox-Hütte, Kisten tragen und werfen, schwerer Gegenstand nur zu zweit, Ofen mit Holz. Prüfpunkt: Tragen zu zweit macht Spaß, auch mit simulierter Verzögerung.
2. Vertical Slice: eine komplette Saison in einer Hütte, erste echte Grafik, Steam-Seite.
3. Demo und Playtests: Steam Playtest, Discord, Demo für Streamer.
4. Steam Next Fest und Feinschliff: restliche Early-Access-Inhalte.
5. Early Access.

---
**Who it's for / what they feel:** Freundesgruppen, die abends über Discord spielen, und Koop-Streamer: erst gemütlich, dann Chaos.

**Art & audio direction:** stilisierte Low-Poly-Alpenwelt, warmes Licht drinnen gegen kaltes Blau draußen; knisternder Ofen, heulender Wind, Zither und Akkordeon.

**Reference game:** Overcooked – Koop-Management unter Zeitdruck, aber in 3D-Physik mit Tragen zu zweit und einem Einsatz draußen statt fester Küche.
