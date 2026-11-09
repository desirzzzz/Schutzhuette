# Gate-Check: Prototyp-Prüfpunkt → Vertical Slice (2. Lauf)

- **Datum:** 2026-11-09
- **Geprüft von:** gate-check (`review_mode: lean`, `workflow: standard`, `qa.level: standard`,
  `team.size: individual`, alles abgeleitet aus `modes.rigor: standard`)
- **Vorgänger:** `production/gate-checks/gate-2026-11-08-prototyp.md` (CONCERNS, damals `workflow: minimal`,
  offen: NOT ASSESSED „Zweiertest mit 0.1.4“)
- **Grundlage:** `production/qa/playtests/playtest-2026-11-08-freund.md`,
  `production/qa/playtests/playtest-2026-11-09-freund.md`, CLAUDE.md (Meilenstein und Prüfpunkt),
  `production/milestones/vertical-slice.md`, `design/gdd/systems-index.md`,
  `artifact-check.sh --phase technical-setup`

## Einordnung

Wie im ersten Lauf werden zwei Dinge geprüft:

- **A.** Das Framework-Gate Technical Setup → Pre-Production. Prototyp und Vertical Slice liegen im
  Framework beide in Pre-Production, `project.stage` ist nicht gesetzt.
- **B.** Der Prüfpunkt aus CLAUDE.md: „Erst wenn das Tragen Spaß macht, geht es zum Vertical Slice.“

**Was sich geändert hat:** `modes.rigor` steht seit dem ersten Lauf auf `standard`. CLAUDE.md sieht das
für den Vertical Slice so vor. Auf `minimal` verlangte A nur die Engine, deshalb entschied im ersten Lauf
B allein. Auf `standard` verlangt A vor dem Bau Architektur, ADRs und Tests. Das widerspricht CLAUDE.md
nicht, es kommt zur Spaßfrage dazu. Deshalb zählen jetzt A und B.

## A. Technical Setup → Pre-Production (`standard`)

### Pflicht-Artefakte: 3/9 vorhanden

- [x] Engine eingerichtet (`project.yaml`: Unity 6000.6.6f1, C#, URP, PhysX)
- [x] Projekt-Config: Engine und Namensregeln in `project.yaml`. Die Performance-Budgets stehen in der
  zugelassenen Ersatzquelle `.claude/docs/technical-preferences.md` (60 fps, 16,6 ms). Draw Calls und
  Speicher sind noch `[TO BE CONFIGURED]`, siehe Bedenken.
- [x] Engine-Referenz (`docs/engine-reference/unity/`, VERSION.md für 6.6)
- [ ] Architekturdokument `docs/architecture/architecture.md`: fehlt
- [ ] ADRs für die Foundation-Schicht (`docs/architecture/adr-*.md`): keine.
  `docs/architecture/tr-registry.yaml` ist leer (`requirements: []`).
- [ ] `/architecture-review`-Bericht: fehlt
- [ ] Test-Gerüst `Assets/Tests/EditMode/` und `Assets/Tests/PlayMode/`: fehlt. Zwei technische
  Hindernisse: Unter `Assets/` gibt es kein einziges `.asmdef` (der Code liegt in Assembly-CSharp, darauf
  kann ein Test-Assembly nicht verweisen), und `com.unity.test-framework` steht nicht in
  `Packages/manifest.json`.
- [ ] Mindestens eine Beispiel-Testdatei: fehlt
- [ ] CI-Workflow `.github/workflows/tests.yml`: fehlt

Bei `standard` nicht verlangt: Art Bible (nur Pflicht, wenn Stories mit visuellen Assets existieren; es
gibt keine Stories).

Empfohlen und fehlend (Bedenken): `design/accessibility-requirements.md`, `design/ux/interaction-patterns.md`,
ein begonnener UX-Spec, `docs/architecture/requirements-traceability.md`.

Hinweis: Der Workflow-Katalog führt für Technical Setup außerdem `docs/architecture/control-manifest.md`.
Die Gate-Datei verlangt es nicht, es ist der erste Schritt nach einem PASS.

### Qualität

- **Tests:** Bei `qa.level: standard` müssen Logic- und Integration-Tests laufen. Es gibt keinen
  Test-Runner und keine Tests. Das wäre NOT ASSESSED, geht aber im fehlenden Test-Gerüst (Blocker) auf.
- **Leistung:** Bei `performance.enforce: warn` nur ein Hinweis. Es gibt keine Profiling-Daten.
- **ADR-Abhängigkeiten:** Nicht prüfbar, weil es keine ADRs gibt.

## B. Prüfpunkt aus CLAUDE.md

| Punkt | Stand |
| --- | --- |
| Alle 7 Meilenstein-Punkte | erledigt |
| Mit simulierter Verzögerung (rund 100 ms) getestet | erledigt |
| Freundestest mit Bericht | erledigt (zwei Berichte) |
| Zweiertest mit dem aktuellen Build 0.1.4 | **erledigt** (gestern NOT ASSESSED): online zu zweit, alles funktioniert, läuft flüssig |
| Geräusche zu zweit | **erledigt**: „er hört alles“ |
| Tragen macht Spaß | **bedingtes Ja**: „wenn es Texturen, Physik und was zu tun hat, kann es richtig Spaß machen“. Der Entwickler wertet das als bestanden mit Bedenken (Antwort im Gate-Check, 2026-11-09). |

## Director-Panel

Panel: 2 von 4 (`workflow: standard`, abgeleitet aus `modes.rigor: standard`). Die Sicht von Creative
Director und Art Director wurde nicht eingeholt. Für das volle Panel `modes.rigor` auf `full` stellen.

**Technical Director: NOT READY.** Technisch steht der Prototyp gut da: host-autoritativ,
Client-Vorhersage beim Tragen, online zu zweit flüssig, Werte schon in ScriptableObjects. Die
Foundation-Entscheidungen stecken aber nur im Code und in den Regeln von CLAUDE.md. Es fehlen
Architekturdokument, Foundation-ADRs, Review, Test-Gerüst und CI. Vor dem Test-Gerüst braucht es
asmdefs (z. B. ein Runtime- und ein Editor-asmdef). Das ist ein Umbau des Prototyp-Codes, der im Editor
geprüft werden muss. Die 5 freigegebenen GDDs decken die Foundation- und Core-Schicht ab und reichen für
die ADRs. Die übrigen GDDs liegen in der Feature-Schicht und können parallel entstehen. Für die
Gäste-Wegfindung im Netz gehört vor Bauschritt 4 ein eigenes ADR geschrieben.

**Producer: NOT READY für den Bau, Planung geht sofort weiter.** Neben den fehlenden Artefakten passt die
Reihenfolge in `production/milestones/vertical-slice.md` nicht zum Systemindex:

- Kochen braucht Lager und Vorräte. Der Tee kommt aber in Schritt 4, das Lager erst in Schritt 6.
- Gegenstandsarten und Interaktion sind freigegeben, aber nicht gebaut, und haben keinen eigenen
  Schritt. Schritt 4 setzt beide voraus.
- Der Nachteinsatz (Schritt 9) braucht Sprachchat und Funkgerät (Schritt 12). Der Sprachchat steht
  zugleich als Erstes auf der Streichliste.
- HUD und Menüs fehlen in der Reihenfolge ganz.
- Der Vertical Slice hat laut Index 20 Systeme, 15 davon ohne GDD.
- Plan-Schritt 1 nennt ADRs und Stories, aber kein Test-Gerüst, kein CI und kein Review.

Zum Spaß: Ein bedingtes Ja ist als Einstieg vertretbar, dann muss der frühe Freundestest nach Schritt 4
aber ein hartes Go/No-Go sein. Physik-Chaos ist die schwächste Säule, und was sie trägt, kommt spät
(Ereignisse in Schritt 8, Ragdoll in Schritt 13). Ein billiges Chaos-Element sollte vor diesen Test
rücken. Die Steam-Lobby hilft bei der Spaßfrage nicht, weil das Hosting über IP schon funktioniert. Sie
kann hinter Schritt 4.

Umfang und Zeit: optimistisch, nicht unrealistisch. Der Engpass ist nicht das Programmiertempo. Engpässe
sind die Zweiertests (nur mit dem Freund), 15 GDD-Reviews bei `standard`, die fehlende Art Bible vor dem
Asset-Pack, der Vorlauf für das Steamworks-Konto (nicht erst in Schritt 14 beginnen) sowie die offenen
Fragen zu Vivox oder Dissonance und zu Ragdoll im Netz. Nach Schritt 4 neu schätzen.

## Blocker

1. **Kein Architekturdokument, keine Foundation-ADRs, kein Architektur-Review.** `/create-architecture`,
   dann `/architecture-decision` für 3–4 kritische Entscheidungen, dann `/architecture-review`.
   Vorschläge des Technical Directors für die ADRs: Netz-Autorität und Physik-Synchronisation (beschreibt
   den Ist-Stand), Datenobjekte und Gegenstandsarten mit Netz-ID, Saison-Uhr und Phasen-Synchronisation,
   Steam-Transport für NGO mit IP-Ausweg.
2. **Kein Test-Gerüst, kein Beispieltest, kein CI.** `/test-setup`: asmdefs anlegen, Test-Paket
   (nur nach Rückfrage, CLAUDE.md), EditMode und PlayMode, ein Beispieltest, CI-Workflow. Die
   Unity-Lizenz-Secrets für game-ci richtet der Entwickler selbst ein.

## Bedenken

1. **Spaß nur bedingt.** Bestanden mit Bedenken laut Entwickler. Der frühe Freundestest nach VS-Schritt 4
   wird zum harten Go/No-Go.
2. **Physik-Chaos nicht spürbar.** Der Freund vermisst „Physik“, obwohl sie da ist. Was er genau meint,
   ist noch nicht gefragt.
3. **Reihenfolge im VS-Plan** widerspricht dem Systemindex (siehe Producer).
4. **Performance-Budgets unvollständig:** Draw Calls und Speicher fehlen.
5. **Empfohlene UX- und Barrierefreiheits-Dokumente fehlen.**

## Empfehlungen

- Den kürzesten Weg zu PASS gehen: `/test-setup`, `/create-architecture`, 3–4 ADRs mit
  `/architecture-decision`, `/architecture-review`, danach diesen Gate-Check erneut.
- Den Freund fragen, was „mehr Physik“ heißt.
- Die Reihenfolge in `vertical-slice.md` korrigieren: Lager vor dem Tee oder ein Minimum davon,
  Gegenstandsarten und Interaktion als eigener Schritt, Sprachchat vor dem Nachteinsatz oder den
  Nachteinsatz ohne Funkgerät planen, HUD und Menüs einplanen, ein Chaos-Element vor den frühen
  Freundestest, die Steam-Lobby hinter Schritt 4.
- Die restlichen GDDs jeweils kurz vor ihrem Bauschritt schreiben, als Nächstes Kochen und Gerichte.
- Die Art Bible nebenher schreiben, spätestens vor dem Kauf des Asset-Packs.
- Beim nächsten Freundestest Host, Ping, Dauer und lustige Momente notieren.

## Ergebnis: FAIL

Beide Directors sagen NOT READY, und Pflicht-Artefakte fehlen. Das FAIL blockiert nur den **Bau** des
Vertical Slice und das Setzen von `project.stage`. Planen und GDDs schreiben gehen weiter. Die Spaßfrage
aus CLAUDE.md ist mit Bedenken bestanden. `project.stage` bleibt unverändert (nicht gesetzt).

## Gegencheck

5 Fragen geprüft, das Ergebnis bleibt FAIL. Ein Blocker wurde zum Bedenken abgestuft.

1. *Sind harte Blocker von Empfehlungen getrennt?* Die Performance-Budgets waren zuerst ein Blocker. Laut
   `.claude/docs/technical-preferences.md` stehen 60 fps und 16,6 ms dort (nachgelesen), und die Gate-Datei
   lässt diese Ersatzquelle zu. Deshalb sind sie jetzt nur ein Bedenken. Beiden Directors war „none“
   übergeben worden, das ist hiermit korrigiert.
2. *War ich bei bestandenen Punkten zu nachsichtig?* Die Engine-Referenz liegt in
   `docs/engine-reference/unity/` mit VERSION.md für 6.6 (nachgelesen).
3. *Fehlen weitere Blocker?* Unter `Assets/` gibt es 0 `.asmdef` (nachgezählt). Das ist kein eigener
   Blocker, aber ein Vorarbeit-Schritt für `/test-setup`. Das Control Manifest verlangt dieses Gate nicht.
4. *Was ist der kürzeste Weg zu PASS?* `/test-setup`, `/create-architecture`, ADRs mit Review.
5. *Lässt sich das FAIL lösen, oder steckt ein tieferes Problem dahinter?* Es lässt sich lösen: Es fehlt
   Dokumentation und Test-Gerüst, die Technik des Prototyps funktioniert. Die Designfrage (Spaß) ist
   getrennt davon mit Bedenken bestanden.
