# Vertical Slice: Plan

- **Stand:** 2026-11-08
- **Status:** geplant, **nicht begonnen**. Gebaut wird erst, wenn der Prototyp-Prüfpunkt bestanden
  ist: Freundestest 2 mit 0.1.4, danach `/playtest-report` und `/gate-check`
  (siehe `production/gate-checks/gate-2026-11-08-prototyp.md`).
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
| Steam-Lobby wann? | früh (Schritt 2), mit Valves kostenloser Test-App „Spacewar“ (App-ID 480). Die eigene App-ID (100 USD) kommt erst mit der Steam-Seite (Nachtrag am selben Abend) |

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
- **Erste echte Grafik:** stilisierte Low-Poly-Alpenwelt mit Hütte, Hang, Figuren und Gegenständen,
  warmes Licht drinnen gegen kaltes Blau draußen.
- **Steam-Seite:** Kapselbilder, Screenshots, Beschreibung und ein Trailer, der in den ersten
  5 Sekunden Chaos zeigt.

## Reihenfolge

Jeder Schritt ist für sich im Editor testbar. Der Kern einer Saison kommt zuerst, weil er auch die
offene Frage aus dem Prototyp beantwortet: Macht Tragen mit Ziel und Zeitdruck mehr Spaß als „ganz
okay“?

1. **Vorbereitung:** `/settings modes.rigor=standard` (CLAUDE.md). Wichtige Architektur-Entscheidungen
   als ADRs festhalten, vor allem Saison-Ablauf und Datenobjekte. Stories mit `/create-stories`.
2. **Steam-Lobby und Einladen, mit der Test-App 480:** Beitreten über Steam-Freunde statt über
   IP-Adresse und Portfreigabe. Kostenlos (Facepunch.Steamworks und Valves Test-App „Spacewar“).
   - Beide Spieler brauchen ein laufendes Steam. In der Freundesliste steht dann „Spacewar“ statt
     „Schutzhütte“.
   - Die Test-App teilen sich viele Entwickler. Lobbys deshalb nur über Einladen beitreten oder mit
     einer eigenen Kennung filtern, sonst sieht man fremde Lobbys.
   - Beitreten über IP bleibt als Ausweg, zum Beispiel für Tests mit zwei Fenstern auf einem PC
     (`127.0.0.1`). Steam selbst erlaubt nur ein Konto pro PC, deshalb lässt sich die Lobby zu zweit
     nur mit dem Freund testen.
3. **Saison-Uhr und Phasen:** Der Host steuert die Uhr, alle sehen dieselbe Phase. Licht und Wetter
   wechseln mit der Phase.
4. **Erster Gast mit erstem Gericht:** Ein Wanderer kommt an, hat Hunger und wartet in der Stube. Tee
   in zwei Schritten. Danach ein **früher Freundestest**: Macht es mit einem Ziel mehr Spaß?
5. **Holz hacken** für den Ofen.
6. **Lager und Materialseilbahn.**
7. **Weitere Gäste und Gerichte:** Familie mit Betten, Bergsteiger mit Erster Hilfe, Speckbrot und
   Kaiserschmarrn. Gäste reagieren auf Kälte.
8. **Wetter-Eskalation und die 5 Ereignisse.**
9. **Nachteinsatz:** Vermisster Gast, Stirnlampe, Trage zu zweit.
10. **Bewertung, Hüttenkasse, Upgrades, nächste Saison.**
11. **Proximity-Sprachchat und Funkgerät.**
12. **Erste echte Grafik:** Asset-Pack einsetzen, Figuren.
13. **Steam-Seite und eigene App-ID:** Steamworks-Konto anlegen (100 USD), das Spiel von App-ID 480
    auf die eigene umstellen, Bilder, Trailer und Text, live schalten. Danach Freundestest mit einer
    ganzen Saison, `/playtest-report` und `/gate-check`.

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
  1.000 USD Umsatz zurückgezahlt): Das legt der Entwickler selbst an, vor Schritt 13. Bis dahin
  genügt die kostenlose Test-App 480.
- **Solo:** Trage und Truhe gehen nur zu zweit. KONZEPT plant für Solo einen Hüttenhund. Kommt der in
  den Vertical Slice, oder ist Solo dort nur zum Testen gedacht?
- **Figuren:** Ragdoll beim Hinfallen (KONZEPT) im Vertical Slice oder später?
- **Musik** (Zither, Akkordeon, dichter mit dem Sturm) im Vertical Slice oder später?

## Neue Pakete

Nur nach Rückfrage, jeweils mit Anleitung für den Package Manager: Facepunch.Steamworks,
Vivox oder Dissonance, das Grafik-Asset-Pack.
