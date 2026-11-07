# Schutzhütte – Konzeptdokument

Stand: 7. November 2026

## Pitch & Überblick

In Schutzhütte führen 1 bis 4 Spieler eine Alpenhütte durch eine Sturmnacht. Ihr kocht, heizt, versorgt Gäste und schleppt Vorräte den Berg hinauf, und je später es wird, desto mehr gerät alles außer Kontrolle.

| Eckdatum | Plan |
| --- | --- |
| Genre | Koop-Simulation mit Physik-Chaos |
| Spieler | 1–4 online, solo spielbar |
| Länge einer Runde | 30–40 Minuten (eine Saison) |
| Zielpreis | 8,99 € |
| Plattform | PC (Steam), Steam Deck tauglich |
| Engine | Unity 6 LTS |
| Ziel | Early Access in etwa 15 Monaten |

**Das Gefühl:** Erst gemütlich, dann Chaos. Drei Säulen tragen jede Entscheidung:

1. **Gemütlichkeit** – warme Stube, knisternder Ofen, zufriedene Gäste.
2. **Physik-Chaos** – alles ist schwer, wackelig und kann den Hang hinunterrutschen.
3. **Teamwork unter Druck** – vieles geht nur zu zweit, und der Sturm lässt keine Pause.

## Zielgruppe & Markt

Die Zielgruppe sind Freundesgruppen, die abends über Discord zusammen spielen, und die Streamer, denen sie zuschauen. Gekauft wird meist von der ganzen Gruppe auf einmal, deshalb ist der niedrige Preis Teil des Designs.

| Vergleichsspiel | Was wir übernehmen | Was wir anders machen |
| --- | --- | --- |
| PEAK, R.E.P.O., Lethal Company | Proximity-Voice, Physik-Pannen, kurze Runden, günstiger Preis | Kein Horror: Spannung kommt vom Wetter, nicht von Monstern |
| Overcooked | Koop-Management unter Zeitdruck | 3D-Physik und Erkundung draußen statt fester Küche |
| How to Fish | Gemütliche Koop-Simulation mit Freunden | Steigende Eskalation pro Runde und ein klarer Höhepunkt |

**Warum jetzt:** Günstige Online-Koop-Spiele („Friendslop“) lieferten laut Sensor Tower die zwei größten Indie-Launches und 46 % der Indie-Verkäufe im ersten Monat ([Sensor Tower](https://sensortower.com/blog/indie-and-new-ip-break-out-without-denting-aaa)). Die Koop-Simulation How to Fish erreichte 2026 einen Steam-Peak von 370.901 Spielern ([Übersicht](https://refurbo.in/blogs/best-indie-games-of-2026-whats-topping-steam-charts)). Ein Alpenhütten-Setting ist in diesem Feld noch frei.

## Kern-Spielschleife

Eine Saison steigert sich in drei Phasen bis zum Nachteinsatz. Danach wird die Hütte ausgebaut, und die nächste Saison beginnt.

**Während einer Saison (30–40 Minuten):**

1. **Nachmittag** – ruhig: Gäste kommen an, Holz hacken, Vorräte holen.
2. **Abend** – Sturm zieht auf, Andrang: kochen, Betten machen, Seilbahn bedienen.
3. **Nacht** (Höhepunkt) – Stromausfall, Notfälle, Nachteinsatz: einen vermissten Gast suchen.

**Zwischen den Saisons:**

4. **Bewertung** – 1 bis 3 Edelweiß, Trinkgeld geht in die Hüttenkasse.
5. **Hütte ausbauen** – Upgrades kaufen, neue Hütten freischalten.
6. **Nächste Saison** – neuer Mix aus Gästen, Wetter und Ereignissen. Weiter bei 1.

Der Druck steigt innerhalb einer Runde, nicht über viele Stunden. So hat jeder Abend mit Freunden einen Anfang, einen Höhepunkt und ein Ende.

## Mechaniken im Detail

Fünf Systeme bilden das Spiel. Das Tragen ist das wichtigste: Wenn es sich gut anfühlt, trägt es alles andere.

### Tragen & Physik

- Alles hat Gewicht: Holz, Wasserkanister, Käselaib, Bierfass, verletzte Gäste.
- Schwere Dinge gehen nur zu zweit. Wer stolpert, reißt den anderen mit.
- Die Kraxe (Rückentrage) stapelt physikalisch. Schlecht gepackt, kippt sie.
- Materialseilbahn: Einer belädt unten, einer kurbelt oben. Wind lässt die Kiste schaukeln.
- Ausdauer sinkt bergauf, Eis lässt Spieler rutschen.

### Versorgen & Kochen

- Wenige, klare Gerichte mit 2–4 Schritten: Kaspressknödelsuppe, Kaiserschmarrn, Tee, Speckbrot.
- Der Holzofen wärmt die ganze Hütte und braucht ständig Holz. Holz muss gehackt werden.
- Das Lager ist klein. Geht etwas aus, muss Nachschub über die Seilbahn kommen.

### Gäste

- Jeder Typ hat ein klares Bedürfnis: Wanderer wollen essen, Familien brauchen Betten, Bergsteiger kommen verletzt, die Influencerin sucht WLAN und beschwert sich.
- Zufriedene Gäste geben Trinkgeld und Bewertungen. Das ist die Währung für Upgrades.
- Gäste reagieren auf Kälte, Lärm und Chaos in der Stube.

### Wetter & Eskalation

- Jede Saison hat drei Phasen: Nachmittag (ruhig), Abend (Sturm zieht auf, Andrang), Nacht (Sturm, Notfälle).
- Ereignisse drücken auf die Gruppe: Ein Fenster fliegt auf, der Generator fällt aus, die Wasserleitung friert ein, eine Dachlawine verschüttet den Eingang.

### Nachteinsatz

- Der Höhepunkt jeder Runde: Ein Gast fehlt. Ein oder zwei Spieler gehen mit Stirnlampe, Seil und Funkgerät hinaus, der Rest hält die Hütte.
- Draußen bricht die Proximity-Voice mit der Entfernung ab. Nur das Funkgerät überträgt, und es rauscht.
- Der Gefundene muss auf der Trage zu zweit zurückgebracht werden.

### Solo

- Ein Hüttenhund hilft beim Suchen und Tragen kleiner Dinge. Solo gibt es weniger Gäste und eine kleinere Hütte.

## Progression & Wiederspielwert

Zwischen den Saisons wird die Hütte ausgebaut, und neue Hütten bringen neue Probleme. So kommt die Gruppe Abend für Abend wieder.

- **Hüttenkasse:** Trinkgeld und Bewertungen kaufen Upgrades, zum Beispiel einen größeren Ofen, einen Generator, ein größeres Lager, eine zweite Seilbahn oder ein Schneemobil.
- **Neue Hütten:** Almhütte (Einstieg), Gletscherhütte (Eis, Spalten, wenig Holz), Felsbiwak (winzig, extremes Wetter).
- **Bewertung pro Saison:** 1 bis 3 Edelweiß. Drei Edelweiß schalten die nächste Hütte schneller frei.
- **Zufall:** Jede Saison mischt Gäste, Wetter und Ereignisse neu.
- **Kosmetik:** Hüte, Trachten, Stirnlampen und Hundefelle, nur zum Spaß, kein Vorteil.

## Look, Sound & Stimmung

Das Bild lebt vom Kontrast: warmes Licht drinnen, kaltes Blau draußen. Der wichtigste Moment ist der Blick aus dem warmen Fenster in den Schneesturm.

- **Grafik:** stilisierte Low-Poly-3D-Welt, die auch auf dem Steam Deck läuft.
- **Figuren:** leicht wackelig, mit Ragdoll beim Hinfallen. Lustige Gesichter, keine realistischen Menschen.
- **Sound:** knisternder Ofen, heulender Wind, Kuhglocken am Nachmittag, rauschender Funk in der Nacht.
- **Musik:** ruhig mit Zither- und Akkordeon-Akzenten am Anfang, dichter und dunkler, je stärker der Sturm wird.

## Technik in Unity

Multiplayer kommt ab dem ersten Tag ins Projekt. Ein Einzelspieler-Spiel nachträglich netzwerkfähig zu machen, ist der häufigste Grund, warum Koop-Projekte scheitern.

| Bereich | Empfehlung | Warum |
| --- | --- | --- |
| Engine | Unity 6 LTS | Stabile Version mit langer Unterstützung |
| Netzwerk | Netcode for GameObjects (Alternative: FishNet) | Offizielle Lösung, viele Tutorials; Host-Client-Modell, kein eigener Server nötig |
| Steam-Anbindung | Facepunch.Steamworks oder Steamworks.NET | Lobbys, Freunde einladen, Achievements |
| Sprachchat | Vivox oder das Asset Dissonance, mit Nähe-Abschwächung | Proximity-Voice ist für das Genre Pflicht |
| Physik | PhysX, der Host entscheidet über Objekte | Verhindert, dass Gegenstände bei Spielern an verschiedenen Orten liegen |
| Prototyp-Grafik | ProBuilder für die Graubox, danach ein Low-Poly-Asset-Pack | Schnell spielbar, Grafik erst später |
| Versionskontrolle | Git mit Git LFS (oder Unity Version Control) | Sicherung und Rückgriff auf alte Stände |

**Faustregel für die Physik im Netz:** Liegt ein Gegenstand still, wird er eingefroren. Wird er getragen, hängt er am Träger. Nur fallende oder rutschende Dinge simuliert der Host voll. Das spart Bandbreite und vermeidet die meisten Synchronisationsfehler.

## Umfang

Früh in den Early Access gehen, und zwar mit einer Hütte, die richtig Spaß macht, statt drei halbfertigen.

| Inhalt | Early Access | Version 1.0 |
| --- | --- | --- |
| Hütten | 1 | 3 |
| Gästetypen | 5 | 10 |
| Gerichte | 5 | 10 |
| Ereignisse | 8 | 20 |
| Upgrades | 10 | 25 |
| Nachteinsatz | ja | ja, mit mehr Varianten |

**Bewusst nicht dabei:**

- keine Konsolen und kein Crossplay
- kein PvP und kein Kampfsystem
- keine offene Welt und keine prozedural erzeugten Karten
- keine Story-Kampagne mit Zwischensequenzen
- keine Mikrotransaktionen
- nicht mehr als 4 Spieler zum Start

## Fahrplan bis zum Steam-Release

Ziel ist der Early Access im Februar 2028, rund 15 Monate ab Dezember 2026. Jede Phase endet mit einem Prüfpunkt. Ist er nicht erreicht, wird nachgebessert, statt weiterzubauen.

| Phase | Zeitraum | Inhalt | Prüfpunkt am Ende |
| --- | --- | --- | --- |
| 1. Prototyp | Dez 2026 – Jan 2027 (2 Monate) | Tragen, Physik, Ofen, zwei Spieler online in einer Graubox-Hütte | Tragen zu zweit macht Spaß |
| 2. Vertical Slice | Feb – Apr 2027 (3 Monate) | Eine komplette Saison in einer Hütte, erste echte Grafik, Steam-Seite | Steam-Seite ist live |
| 3. Demo und Playtests | Mai – Sep 2027 (5 Monate) | Steam Playtest, Discord, Clips und Demo-Schlüssel für Streamer | Demo läuft stabil |
| 4. Steam Next Fest und Feinschliff | Okt 2027 – Jan 2028 (4 Monate) | Demo im Next Fest Oktober 2027, Inhalte für den Early Access fertigstellen | Rund 10.000 Wunschlisten |
| Early Access auf Steam | Feb 2028 (Monat 15) | Eine Hütte, 5 Gästetypen, Nachteinsatz; danach der Weg zu Version 1.0 | – |

Die Zeiten sind Schätzungen für eine Person mit etwas Unity-Erfahrung und gehen davon aus, dass das Steam Next Fest wie bisher im Oktober stattfindet. Wichtig ist die Reihenfolge, nicht das genaue Datum.

## Marketing & Steam-Strategie

In diesem Genre verkaufen Clips das Spiel, nicht Anzeigen. Jede Mechanik sollte deshalb lustige Momente erzeugen, die man in 15 Sekunden zeigen kann.

- **Steam-Seite früh live**, sobald der Vertical Slice steht. Der Trailer zeigt in den ersten 5 Sekunden Chaos.
- **Kurzvideos:** 2 bis 3 Clips pro Woche aus Playtests auf TikTok, YouTube Shorts und Reddit.
- **Streamer:** Demo-Schlüssel an kleine und mittlere Koop-Streamer schicken, nicht nur an die großen.
- **Discord ab Tag 1:** Playtester sammeln und Feedback holen.
- **Steam Playtest** für offene Tests vor der Demo nutzen.
- **Steam Next Fest im Oktober 2027** als größter Schub für Wunschlisten.
- **Presse-Geschichte:** ein Spiel aus Österreich über eine Alpenhütte. Das ist ein klarer Aufhänger für Medien im deutschsprachigen Raum.

## Risiken & Gegenmaßnahmen

Das größte Risiko ist nicht die Idee, sondern dass das Projekt zu groß wird oder niemand davon erfährt.

| Risiko | Gegenmaßnahme |
| --- | --- |
| Der Umfang wächst immer weiter | Liste „Bewusst nicht dabei“ einhalten, feste Meilensteine |
| Zu wenig Aufmerksamkeit zum Start | Clips ab Monat 2, Wunschlisten-Ziel vor dem Early Access |
| Netzwerkcode überfordert | Erst ein Tutorial-Projekt, dann Multiplayer ab Tag 1 im echten Spiel |
| Physik-Fehler im Multiplayer | Host entscheidet über Objekte, ruhende Dinge einfrieren |
| Solo macht weniger Spaß | Solo-Modus mit Hüttenhund, Marketing klar auf Koop ausrichten |
| Markt ist voll mit Koop-Chaos-Spielen | Eigenes Setting und die gemütliche Seite als Unterschied betonen |
| Motivation geht verloren | Kleine Ziele, alle 2 Wochen mit Freunden testen |

## Nächste Schritte: die ersten zwei Wochen

Nach zwei Wochen soll eine Frage beantwortet sein: Macht es Spaß, zu zweit eine schwere Kiste durch eine Hütte zu tragen?

**Woche 1**

- [ ] Unity 6 LTS installieren, Projekt mit Git und Git LFS anlegen
- [ ] Netcode-for-GameObjects-Tutorial durcharbeiten: zwei Spieler laufen online in einer Szene
- [ ] Graubox-Hütte mit ProBuilder bauen: Stube, Küche, Lager, Eingang, ein Hang davor

**Woche 2**

- [ ] Aufheben und Tragen von Kisten, für beide Spieler synchron
- [ ] Ein schwerer Gegenstand, der sich nur zu zweit bewegen lässt
- [ ] Ein Ofen, der Holz braucht und ausgeht
- [ ] Mit einem Freund testen und notieren, was lustig war und was genervt hat
