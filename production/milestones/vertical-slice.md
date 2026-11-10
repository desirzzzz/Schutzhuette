# Vertical Slice: Plan

- **Stand:** 2026-11-10 (Schritt 4 und 6 nach dem Gate-Check ergänzt)
- **Status:** Schritt 1 läuft, gebaut wird noch nicht. Die Spaßfrage des Prototyps ist mit Bedenken
  bestanden (Freundestest 2, `production/qa/playtests/playtest-2026-11-09-freund.md`). Der Gate-Check
  vom 2026-11-10 ist CONCERNS: keine Blocker, 12 Bedenken
  (`production/gate-checks/gate-2026-11-10-pre-production.md`). Aus Schritt 1 fehlen noch das Control
  Manifest, die Stories für Schritt 2 und die Frage an den Freund. Gebaut wird ab Schritt 2, wenn das
  erledigt ist.
- **Grundlage:** `docs/KONZEPT.md` („Kern-Spielschleife“, „Mechaniken im Detail“, „Umfang“,
  „Fahrplan“) und `design/game-brief.md`. Bei Widersprüchen gilt KONZEPT.md.

## Ziel

Eine komplette Saison in einer Hütte (Almhütte), erste echte Grafik, Steam-Seite.

**Prüfpunkt am Ende** (KONZEPT, „Fahrplan“): Die Steam-Seite ist live. Dazu die Frage aus dem Game
Brief: Fühlt sich eine Saison vollständig an, also mit ruhigem Anfang, chaotischem Höhepunkt und einer
Bewertung? Das klärt ein Freundestest mit einer ganzen Saison.

## Entscheidungen des Entwicklers (2026-11-08)

| Frage | Entscheidung |
| --- | --- |
| Länge einer Saison | voll, 30–40 Minuten |
| Inhalt | Mittel: 3 Gästetypen, 3 Gerichte, 5 Ereignisse |
| Systeme | Nachteinsatz mit Trage, Holz hacken, Materialseilbahn, Hüttenkasse und Upgrades |
| Steam und Sprache | Steam-Lobby mit Einladen, Proximity-Sprachchat mit Funkgerät |
| Steam-Lobby wann? | mit Valves kostenloser Test-App „Spacewar“ (App-ID 480). Die eigene App-ID (100 USD) kommt erst mit der Steam-Seite (Nachtrag am selben Abend). Ursprünglich früh (Schritt 2), am 2026-11-09 hinter den frühen Freundestest verschoben: Das Hosting über IP funktioniert, und die Lobby klärt die Spaßfrage nicht |
| Ragdoll und Solo-Hüttenhund | beide im Vertical Slice, Musik erst in der Alpha (Nachtrag bei `/map-systems`, siehe `design/gdd/systems-index.md`) |
| Chaos vor dem frühen Freundestest (2026-11-09) | Geschirr zerbricht bei hartem Aufprall, der Gast reagiert auf Lärm und Treffer (beides aus den freigegebenen GDDs Gegenstandsarten und Gäste). Antwort auf „Physik-Chaos ist die schwächste Säule“ aus Freundestest 2 |
| Nachteinsatz und Funkgerät (2026-11-09) | Der Nachteinsatz funktioniert zuerst ohne Funkgerät. Das Funkgerät kommt mit dem Sprachchat dazu, der Sprachchat bleibt Streichkandidat Nr. 1 |

## Inhalt im Einzelnen

Was mit „Vorschlag“ markiert ist, ist noch nicht entschieden.

- **Ablauf einer Saison:** Nachmittag (ruhig) → Abend (Sturm zieht auf, Andrang) → Nacht
  (Stromausfall, Notfälle, Nachteinsatz) → Bewertung → Upgrades → nächste Saison.
- **Gäste (3):** Wanderer wollen essen, Familien brauchen Betten, Bergsteiger kommen verletzt
  (Erste Hilfe). Gäste reagieren auf Kälte, Lärm und Chaos in der Stube.
- **Gerichte (3, je 2–4 Schritte):** Tee, Speckbrot, Kaiserschmarrn (Vorschlag).
- **Ereignisse (5):** Ein Fenster fliegt auf, der Generator fällt aus, die Wasserleitung friert ein,
  eine Dachlawine verschüttet den Eingang (alle vier aus KONZEPT), dazu die Seilbahn klemmt im Sturm
  (Vorschlag).
- **Holz hacken:** Der Ofen braucht ständig gehacktes Holz, es kommt nicht mehr fertig vom Holzplatz.
- **Lager und Materialseilbahn:** Das Lager ist klein. Einer belädt unten, einer kurbelt oben, Wind
  lässt die Kiste schaukeln.
- **Nachteinsatz:** Ein Gast fehlt. Ein oder zwei Spieler gehen mit Stirnlampe, Seil und Funkgerät
  hinaus und bringen ihn zu zweit auf der Trage zurück. Die Trage kann auf der Truhe aus dem Prototyp
  aufbauen.
- **Bewertung und Hüttenkasse:** 1 bis 3 Edelweiß, das Trinkgeld geht in die Hüttenkasse.
- **Upgrades (5, Vorschlag):** größerer Ofen, größeres Lager, Notstromaggregat, schnellere
  Seilbahnkurbel, hellere Stirnlampen. Der Early Access hat 10.
- **Steam-Lobby:** Beitreten über Steam-Freunde statt über IP-Adresse und Portfreigabe
  (Facepunch.Steamworks).
- **Sprachchat:** In der Nähe hört man sich, mit der Entfernung wird es leiser. Draußen im
  Nachteinsatz überträgt nur das Funkgerät, und es rauscht.
- **Solo-Hüttenhund:** Hilft solo beim Suchen und beim Tragen kleiner Dinge (KONZEPT, „Solo“).
- **Ragdoll:** Figuren wackeln leicht und fallen beim Hinfallen als Ragdoll um. Bringt Momente für
  Clips und den Trailer.
- **Erste echte Grafik:** stilisierte Low-Poly-Alpenwelt mit Hütte, Hang, Figuren und Gegenständen,
  warmes Licht drinnen gegen kaltes Blau draußen.
- **Steam-Seite:** Kapselbilder, Screenshots, Beschreibung und ein Trailer, der in den ersten
  5 Sekunden Chaos zeigt.

## Reihenfolge

Jeder Schritt ist für sich im Editor testbar. Freundestest 2 sagte: Es kann richtig Spaß machen, wenn
es etwas zu tun gibt, mehr Physik und Texturen. Deshalb kommen ein Ziel und spürbares Chaos zuerst, und
der frühe Freundestest nach Schritt 5 entscheidet, ob es so weitergeht. Jeder Schritt baut nur auf
Systemen aus früheren Schritten auf (Abhängigkeiten laut `design/gdd/systems-index.md`).

1. **Vorbereitung:** Vor dem ersten Bauschritt, so verlangt es der Gate-Check vom 2026-11-09.
   - `/create-architecture`: den Ist-Stand des Prototyps festhalten, Budgets für Draw Calls und
     Speicher ergänzen.
   - `/architecture-decision` für 3–4 Foundation-ADRs: Netz-Autorität und Physik-Synchronisation,
     Datenobjekte und Gegenstandsarten mit Netz-ID, Saison-Uhr und Phasen-Synchronisation, Steam-Transport
     mit IP-Ausweg. Danach `/architecture-review`.
   - `/test-setup`: asmdefs, Test-Paket (nach Rückfrage), EditMode und PlayMode, ein Beispieltest, CI.
   - `/gate-check` erneut, danach `/create-control-manifest` und Stories mit `/create-stories`.
   - Den Freund fragen, was er mit „mehr Physik“ meint.
   - Nebenher: die Art Bible (`/art-bible`), fertig spätestens vor dem Kauf des Asset-Packs.
2. **Gegenstandsarten:** Die heutigen Kisten, Holzscheite und die Truhe werden Datenobjekte (Arten) mit
   Gewicht, Material und Eigenschaften. Alle folgenden Schritte lesen diese Daten.
3. **Saison-Uhr und Phasen:** Der Host steuert die Uhr, alle sehen dieselbe Phase. Licht und Stimmung
   wechseln mit der Phase, den Sturm selbst bringt Schritt 8. Das HUD zeigt Uhr und Phase.
4. **Interaktion:** Ein gemeinsames „Benutzen“ mit der linken Maustaste, zuerst an Herd und Ofen. E hebt
   nur noch auf und legt ab, geworfen wird neu mit der rechten Maustaste (`design/gdd/interaktion.md`).
   - Vorher ADR-0004 „Interaktion übers Netz“ (`docs/architecture/architecture.md`).
   - Die Tastentabelle in `docs/FREUNDESTEST.md` wird in diesem Schritt angepasst (heute wirft der
     Linksklick).
5. **Erster Gast mit erstem Gericht:** Ein Wanderer kommt an, hat Hunger und wartet in der Stube.
   - Vorher ein ADR für die Gäste-Wegfindung im Netz (AI-Navigation-Paket nur nach Rückfrage).
   - Tee in zwei Schritten am Herd. Die Zutaten kommen aus einem **Mindest-Lager**, dessen Bestand in
     diesem Schritt nicht ausgeht. Das echte Lager folgt in Schritt 9.
   - **Chaos von Anfang an:** Tassen zerbrechen bei hartem Aufprall, der Gast reagiert auf Lärm und auf
     Treffer.
   - Danach der **frühe Freundestest als Go/No-Go:** Macht es mit einem Ziel und Chaos richtig Spaß?
     Host, Ping, Dauer und lustige Momente notieren, dann `/playtest-report` und den Aufwand neu
     schätzen. Bei No-Go wird erst der Kern verbessert, bevor weitere Systeme dazukommen.
6. **Steam-Lobby und Einladen, mit der Test-App 480:** Beitreten über Steam-Freunde statt über
   IP-Adresse und Portfreigabe. Kostenlos (Facepunch.Steamworks und Valves Test-App „Spacewar“).
   - Vorher ADR-0006 „Steam-Transport für NGO mit IP-Ausweg“ (`docs/architecture/architecture.md`).
   - Beide Spieler brauchen ein laufendes Steam. In der Freundesliste steht dann „Spacewar“ statt
     „Schutzhütte“.
   - Die Test-App teilen sich viele Entwickler. Lobbys deshalb nur über Einladen beitreten oder mit
     einer eigenen Kennung filtern, sonst sieht man fremde Lobbys.
   - Beitreten über IP bleibt als Ausweg, zum Beispiel für Tests mit zwei Fenstern auf einem PC
     (`127.0.0.1`). Steam selbst erlaubt nur ein Konto pro PC, deshalb lässt sich die Lobby zu zweit
     nur mit dem Freund testen.
7. **Holz hacken** für den Ofen.
8. **Wetter-Eskalation:** Der Sturm wird über die Phasen stärker: Wind, Sicht, Kälte. Er kommt vor der
   Seilbahn, weil ihr Wind von hier stammt. Die Ereignisse folgen in Schritt 11.
9. **Lager und Materialseilbahn:** Das Lager wird echt. Der Bestand geht zur Neige, Nachschub kommt über
   die Seilbahn, und der Wind lässt die Kiste schaukeln.
10. **Weitere Gäste und Gerichte:** Familie mit Betten, Bergsteiger mit Erster Hilfe, Speckbrot und
    Kaiserschmarrn. Gäste reagieren auf Kälte.
11. **Die 5 Ereignisse** mit Strom und Licht.
12. **Nachteinsatz:** Vermisster Gast, Stirnlampe, Trage zu zweit. Zuerst ohne Funkgerät, das kommt in
    Schritt 16 dazu.
13. **Solo-Hüttenhund:** Hilft solo beim Suchen und Tragen, damit eine Saison auch allein zu schaffen
    ist.
14. **Bewertung, Hüttenkasse, Upgrades, nächste Saison.**
15. **Menüs und HUD:** Hauptmenü, Lobby, Bewertungs- und Upgrade-Bildschirm und Einstellungen ersetzen
    die Test-Knöpfe. Das HUD, das bis hierher Schritt für Schritt gewachsen ist (Uhr, Gäste, Ofen,
    Hüttenkasse), wird zusammengeführt. Vorher `/ux-design` für HUD und Menüs.
16. **Proximity-Sprachchat und Funkgerät,** auch im Nachteinsatz.
17. **Erste echte Grafik:** Asset-Pack einsetzen, Figuren mit leichtem Wackeln und Ragdoll beim
    Hinfallen.
18. **Steam-Seite und eigene App-ID:** Das Spiel von App-ID 480 auf die eigene umstellen, Bilder,
    Trailer und Text, live schalten. Das Steamworks-Konto wird schon früher angelegt (siehe offene
    Fragen). Danach Freundestest mit einer ganzen Saison, `/playtest-report` und `/gate-check`.

Die Systeme dahinter mit Abhängigkeiten und Design-Reihenfolge stehen in `design/gdd/systems-index.md`.

## Umfang und Zeit

KONZEPT sieht für den Vertical Slice drei Monate vor (Feb – Apr 2027). Bei allen vier Entscheidungen
liegt der gewählte Umfang über der schmalen Variante. Ob er in drei Monate passt, ist unsicher. Laut
KONZEPT ist das größte Risiko, „dass das Projekt zu groß wird“. Der Prototyp war geplant bis Januar
2027 und ist seit dem 8. November 2026 gebaut. Das ergibt rund zwei Monate Puffer.

Wird die Zeit knapp, entscheidet der Entwickler, was wegfällt. Die Reihenfolge oben ist so gelegt,
dass die späten Schritte am leichtesten in die Demo-Phase rutschen können: zuerst Sprachchat, dann
Upgrades, dann das dritte Gericht und der dritte Gast.

## Offene Fragen (vor dem jeweiligen Schritt klären)

- **Asset-Pack** für die Grafik: welches, was es kostet, welche Lizenz gilt.
- **Sprachchat:** Vivox oder Dissonance (Kosten, Aufwand, Funkgerät-Effekt).
- **Steamworks-Konto** und App-Gebühr (Steam Direct, 100 USD pro Spiel, nicht erstattbar, wird ab
  1.000 USD Umsatz zurückgezahlt): Das legt der Entwickler selbst an. Valve prüft Konto, Steuerdaten
  und später die Store-Seite, das braucht Vorlauf. Deshalb spätestens beim Start von Schritt 17
  anlegen, nicht erst in Schritt 18. Bis dahin genügt die kostenlose Test-App 480.
- **Solo-Hüttenhund:** Truhe und Trage gehen nur zu zweit. Wie schafft man sie solo, zum Beispiel
  mit einem Schlitten, den der Hund zieht? Das klärt das GDD des Hundes.
- **Ragdoll im Netz:** Nur Zustand und Wurzelposition übertragen oder mehr? Das klärt das GDD und ein
  früher Versuch.

Entschieden: Ragdoll und Solo-Hüttenhund kommen in den Vertical Slice, die Musik in die Alpha.

## Neue Pakete

Nur nach Rückfrage, jeweils mit Anleitung für den Package Manager: Facepunch.Steamworks,
Vivox oder Dissonance, das Grafik-Asset-Pack.
