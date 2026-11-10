# Gate-Check: Technical Setup → Pre-Production (3. Lauf)

- **Datum:** 2026-11-10
- **Geprüft von:** gate-check (`review_mode: lean`, `workflow: standard`, `qa.level: standard`,
  `team.size: individual`, alles abgeleitet aus `modes.rigor: standard`; `performance.enforce: warn`)
- **Vorgänger:** `production/gate-checks/gate-2026-11-09-prototyp.md` (FAIL; Blocker: keine Architektur,
  keine ADRs, kein Review, kein Test-Gerüst, kein CI)
- **Grundlage:** `artifact-check.sh --phase technical-setup`, `adr-dep-graph.sh`, Testlauf im Batchmode
  (heute 19:50), `docs/architecture/`, `production/milestones/vertical-slice.md`,
  `design/accessibility-requirements.md`, `design/ux/interaction-patterns.md`

## Einordnung

Geprüft wird das Framework-Gate Technical Setup → Pre-Production, also ob der Bau des Vertical Slice ab
Plan-Schritt 2 beginnen darf (`vertical-slice.md`: „Gebaut wird ab Schritt 2, wenn Schritt 1 erledigt und der
Gate-Check bestanden ist.“). Die Spaßfrage aus CLAUDE.md wurde am 2026-11-09 mit Bedenken beantwortet und
wird hier nicht neu geprüft.

## Pflicht-Artefakte (`standard`): 9/9 vorhanden

- [x] Engine gewählt (Unity 6000.6.6f1, C#, URP, NGO 2.13.3)
- [x] Projekt-Config: Engine und Namensregeln in `project.yaml`; Budgets 60 fps und 16,6 ms in
  `.claude/docs/technical-preferences.md`. Draw Calls und Speicher fehlen (Bedenken 7).
- [x] Foundation-ADRs: ADR-0001, ADR-0002, ADR-0003, alle **Accepted** (2026-11-09)
- [x] Engine-Referenz `docs/engine-reference/unity/` (VERSION.md für 6.6)
- [x] Test-Gerüst `Assets/Tests/EditMode/` und `Assets/Tests/PlayMode/` mit 4 asmdefs
- [x] CI-Workflow `.github/workflows/tests.yml`: existiert, **noch nie gelaufen** (Bedenken 4)
- [x] Beispieltest `Assets/Tests/EditMode/StoveTests.cs` (5 Tests)
- [x] `docs/architecture/architecture.md`
- [x] `/architecture-review`-Bericht `docs/architecture/architecture-review-2026-11-09.md` (Mode: full,
  Verdict CONCERNS, älter als die Annahme der ADRs, Bedenken 5)

Bei `standard` nicht verlangt: Art Bible (nur Pflicht, wenn Stories mit visuellen Assets existieren;
`production/epics/` gibt es nicht).

**Empfohlen: 3/4 vorhanden**
- [x] `design/accessibility-requirements.md`, Stufe Standard festgelegt (heute, 8015ad1)
- [x] `design/ux/interaction-patterns.md`, 17 Muster (heute, d741502)
- [x] `docs/architecture/requirements-traceability.md` (vorhanden, aber veraltet: zählt „0 Accepted“)
- [ ] UX-Spec für einen Bildschirm begonnen: fehlt (Bedenken 8)

Hinweis: Der Workflow-Katalog führt `docs/architecture/control-manifest.md` unter Technical Setup (ABSENT).
Die Gate-Datei verlangt es nicht; es ist der erste Schritt nach diesem Gate.

## Qualität

- [x] Architektur deckt die Kernbereiche; alle HIGH-Domänen (Netzwerk, Physik, Navigation, Scripting) in
  `architecture.md` behandelt oder als offene Frage geführt
- [~] Namensregeln gesetzt, Performance-Budgets nur teilweise (Bedenken 7)
- [x] Barrierefreiheits-Stufe festgelegt: Standard
- [ ] UX-Spec begonnen (empfohlen): fehlt
- [x] Alle ADRs haben „Engine Compatibility“ und „GDD Requirements Addressed“
- [x] Keine veralteten APIs in den ADRs (Review, Mode full)
- [x] Alle ADRs mit Knowledge Risk HIGH markiert, alle mit derselben Engine-Version
- [x] Null Foundation-Lücken: Das Review führte TR-gegenstandsarten-003 als „teilweise“ (keine Lücke). Die
  fehlende Regel steht inzwischen in ADR-0001 (nachgelesen); ein neues Review hat das noch nicht bestätigt.
- [x] ADR-Abhängigkeiten: 0002→0001, 0003→0001, 0003→0002, keine Zyklen, keine leeren Depends-On
- **Tests (heute 19:50, Batchmode):** EditMode 5/5 bestanden (`testcasecount=5`). PlayMode lief, **0 Tests**.
  Der Runner ist eingerichtet und startet. Damit greift die NOT-ASSESSED-Regel für einen fehlenden oder nicht
  startenden Runner nicht. Ein Integrationstest ist aber nicht belegt, deshalb steht der Punkt unter Bedenken 6
  und wird nicht als bestanden gezählt.
- **Leistung:** Es gibt keine Profiling-Daten. Dieses Gate verlangt keinen Leistungsnachweis; bei
  `performance.enforce: warn` ist es nur ein Hinweis. Messung ist für Plan-Schritt 5 eingeplant (QQ-01).
- **Freund zu „mehr Physik“** (Plan-Schritt 1): laut Entwickler noch nicht gefragt (Bedenken 2).

## Director-Panel

Panel: 2 von 4 (`workflow: standard`, abgeleitet aus `modes.rigor: standard`). Creative Director und Art
Director wurden nicht befragt. Für das volle Panel `modes.rigor` auf `full` stellen.

**Technical Director: CONCERNS.** Bereit, ab Schritt 2 zu bauen, kein Blocker. Stichprobe in den ADRs:
Befund 1 (ADR-0001 Z. 182 und 298), Befund 2 und W4 (ADR-0003 Z. 193–199), W1, W2, W3, W5, W6 sind
eingearbeitet; W7 bewusst auf ADR-0004 verschoben (vertretbar, „Am Körper“ entsteht erst in Schritt 4).
Keine Zyklen, keine veralteten APIs. Für READY fehlen zwei Nachweise: ein grüner CI-Lauf und ein Review des
angenommenen Stands. Größtes offenes Engine-Risiko: QQ-03 (Steam-Transport für NGO 2.13 und Unity 6.6),
blockiert Schritt 2 nicht, weil der IP-Weg funktioniert.

**Producer: CONCERNS.** Bau ab Schritt 2 vertretbar: Für die Schritte 2–4 gibt es freigegebene GDDs und
angenommene ADRs. Alle 6 Einwände vom 2026-11-09 zur Reihenfolge sind eingearbeitet (Mindest-Lager in
Schritt 5, Gegenstandsarten und Interaktion eigene Schritte, Nachteinsatz ohne Funkgerät, HUD ab Schritt 3,
Chaos vor dem frühen Freundestest, Steam-Lobby hinter dem Go/No-Go). Zeitplan bis April 2027 optimistisch,
nicht unrealistisch; Neuschätzung nach Schritt 5 ist eingeplant.

## Blocker

Keine.

## Bedenken

Jeweils mit dem kleinsten Schritt, der es schließt.

1. **Barrierefreiheit ist Umfang, der nicht im VS-Plan steht** (Producer). Rund 17 Zusagen in
   `accessibility-requirements.md`, keine davon in `vertical-slice.md`; grob +10–15 % Umfang. Markieren,
   Rufe-Rad und Richtungspfeil sind ein neues Netz-System; Tastenbelegung und Einstellungspaket machen
   Schritt 15 zum größten Schritt. Kopplung: Draußen laufen Rufe über das Funkgerät, das am Sprachchat hängt
   (Streichkandidat Nr. 1).
   → Jeder Zusage einen Meilenstein geben (VS, Demo, EA). Empfehlung des Producers: in den VS nur, was später
   teuer nachzurüsten ist (umbelegbare Input-Aktionen und Halten als Umschalten in Schritt 4, Audio-Kanäle in
   Schritt 5, Faktoren für „Gemütlich“ als Datenfelder in den Schritten 5 und 10), der Rest in die
   Demo-Phase. Wird der Sprachchat gestrichen, rückt das Rufe-Rad als Ersatz in den VS. Dabei die
   Querverweise in `interaktion.md` (offene Frage 3), `saison-ablauf.md` (Wahl „Gemütlich“) und `gaeste.md`
   (Geduldsfaktor) nachziehen.
2. **„Mehr Physik“ nicht gefragt** (Producer). Schritte 2–4 hängen nicht davon ab, das Chaos-Element in
   Schritt 5 aber schon, und das Go/No-Go misst genau dieses.
   → Dem Freund heute eine Nachricht mit vier Auswahlmöglichkeiten schicken (Umfallen/Zerbrechen,
   Stolpern/Ragdoll, Rutschen, Wackeln beim Tragen). Die Antwort in `playtest-2026-11-09-freund.md`
   nachtragen, spätestens vor dem GDD Kochen. Bei „Stolpern/Ragdoll“ das Chaos-Element in Schritt 5 neu
   entscheiden.
3. **VS-Plan weicht von freigegebenen Quellen ab** (Producer). Schritt 4 sagt „Benutzen mit der Taste E“;
   `interaktion.md` und `interaction-patterns.md` legen Benutzen auf die linke Maustaste (E aufheben, Werfen
   rechte Maustaste; `docs/FREUNDESTEST.md` muss dann angepasst werden). ADR-0004 vor Schritt 4 und ADR-0006
   vor Schritt 6 fehlen im Plan.
   → Drei Zeilen in `vertical-slice.md` korrigieren, bevor `/create-stories` läuft.
4. **CI nie gelaufen, 26 Commits nur lokal** (beide). Alles aus Schritt 1 liegt auf einer Festplatte.
   `tests.yml` reicht nur `UNITY_LICENSE` weiter (nachgelesen, Z. 19, 34, 42). Laut game.ci braucht eine
   Personal-Lizenz eventuell auch `UNITY_EMAIL` und `UNITY_PASSWORD`, und Unity unterstützt die manuelle
   Aktivierung von Personal-Lizenzen nicht mehr (Quellen des Technical Directors:
   https://game.ci/docs/github/activation,
   https://discussions.unity.com/t/unity-no-longer-supports-manual-activation-of-personal-licenses/926760).
   Ob es ein game-ci-Image für 6000.6.6f1 gibt: NOT SOURCEABLE.
   → Secrets nach der aktuellen game.ci-Anleitung eintragen (der Entwickler selbst), falls verlangt die
   env-Zeilen in beiden Test-Schritten ergänzen, pushen, ersten grünen Lauf vor dem Abschluss der ersten
   Story in Schritt 2. Bis dahin gilt der lokale Batchmode-Lauf.
5. **Architektur-Review älter als der bewertete Stand** (Technical Director). Das Review sah die ADRs noch als
   Proposed. `requirements-traceability.md` zählt „0 Accepted“, `architecture.md` Z. 45–49 nennt das
   TR-Register leer (es hat 33 Einträge).
   → `/architecture-review` in einer frischen Sitzung (über die Content-Hashes nur ein Delta: 3 ADRs und
   `architecture.md`), danach Z. 45–49 nachziehen, vor den ersten Stories.
6. **PlayMode-Gerüst nicht belegt** (Technical Director). 0 Tests sieht aus wie bestanden, zeigt aber nur, dass
   die Assembly kompiliert.
   → Ein `[UnityTest]` in `Assets/Tests/PlayMode/`: Rigidbody erzeugen, einige FixedUpdates warten, prüfen,
   dass er gefallen ist. Fällig vor der ersten Integration-Story.
7. **Performance-Budgets unvollständig** (beide). Nur 60 fps und 16,6 ms, und nur in der Ersatzdatei;
   `project.yaml` hat keinen `performance:`-Block. Offen: Draw Calls, Speicher (QQ-01), Bandbreite (QQ-02),
   TR-gaeste-008 (höchstens 1,0 ms beim Host) fehlt im Register. Für die Schritte 2–4 (Graubox) unkritisch.
   → Wie geplant in Schritt 5 messen, dann `performance:` in `project.yaml` eintragen, TR-gaeste-008 mit.
8. **Kein UX-Spec für einen Bildschirm** (empfohlen; Technical Director). Erster Bildschirm ist das HUD in
   Schritt 3 (Uhr und Phase). Die UI-Technik entscheidet erst ADR-0007 vor Schritt 15.
   → `design/ux/hud.md` vor Schritt 3 beginnen. Technische Vorgabe: Das HUD liest nur aus `SeasonSnapshot`
   und Meldungen, damit später nur das Zeichnen ausgetauscht wird.
9. **Engine-Referenz teilweise alt** (Technical Director). `modules/` hat den Stand 6.3;
   `modules/networking.md` (um Z. 400) nennt die Signatur von `OnConnectionEvent` noch NOT SOURCEABLE,
   obwohl ADR-0001 sie belegt. Physik in 6.4–6.6 unbelegt, betrifft Schritt 2 (Würfe mit 15 m/s,
   `ContinuousSpeculative`).
   → Signatur in `networking.md` nachtragen; ADR-0001 Verification 1 (15 m/s gegen eine 0,2-m-Wand) als
   Abnahmekriterium der ersten Story in Schritt 2.
10. **Schritt 5 vor dem Go/No-Go überladen** (Producer). Wegfindung im Netz (höchstes technisches Risiko,
    neues Paket, ADR-0005), zwei fehlende GDDs (Kochen, Mindest-Lager), Tee, Chaos, Messungen QQ-01 und QQ-02,
    dazu die Freundestest-Kriterien aus `interaktion.md`.
    → Bei `/create-stories` in 5a (Gast mit Wegfindung, Tee, Mindest-Lager) und 5b (Chaos) teilen; Termin für
    den frühen Freundestest jetzt mit dem Freund festlegen; Ausweg bei gescheiterter Wegfindung in ADR-0005.
11. **Zweiertests sind der Engpass** (Producer). 2 Freundestests in 17 Bauschritten; die Schritte 6, 9, 12 und
    16 lassen sich solo nicht abnehmen.
    → Nach diesen Schritten je einen kurzen Freundestest in den Plan eintragen und einen festen Wochentag mit
    dem Freund vereinbaren.
12. **Spaß nur bedingt bestätigt** (aus dem Gate vom 2026-11-09). Der frühe Freundestest nach Schritt 5 bleibt
    das harte Go/No-Go.

## Empfehlungen

- Vor dem ersten Bauschritt (klein): Bedenken 2 (Freund fragen) und 3 (drei Zeilen im Plan).
- Vor `/create-stories`: `/create-control-manifest`, Bedenken 1 (Meilensteine für die Barrierefreiheit) und 5
  (Delta-Review).
- Während Schritt 2: Bedenken 4 (CI grün), 6 (erster PlayMode-Test), 9 (Wurf-Test als Abnahmekriterium).
- Vor Schritt 3: `design/ux/hud.md`.

## Ergebnis: CONCERNS

Keine Blocker, alle 9 Pflicht-Artefakte vorhanden, beide Directors CONCERNS. Kein Prüfpunkt ist NOT ASSESSED
(PlayMode ohne Tests ist Bedenken 6; Leistung verlangt dieses Gate nicht). `project.stage` bleibt unverändert
(nicht gesetzt), solange der Entwickler die Bedenken nicht ausdrücklich annimmt.

## Gegencheck

5 Fragen geprüft, das Ergebnis bleibt CONCERNS.

1. *Müsste ein Bedenken ein Blocker sein?* Die CI-Datei verlangt das Gate, einen Lauf nicht. [TOOL] In
   `tests.yml` nachgelesen: Die Datei ist vollständig, nur die Lizenz-Weitergabe ist unsicher. Bleibt Bedenken.
2. *Habe ich ein FAIL weichgezeichnet?* „Null Foundation-Lücken“: [TOOL] ADR-0001 Z. 181–185 nachgelesen; die
   Regel für TR-gegenstandsarten-003 steht drin. Kein Blocker.
3. *Gibt es ungeprüfte Artefakte mit weiteren Blockern?* Control Manifest und Art Bible verlangt dieses Gate bei
   `standard` nicht; `production/epics/` existiert nicht (nachgesehen).
4. *Ist PlayMode mit 0 Tests eigentlich NOT ASSESSED?* Die Regel gilt für einen fehlenden oder nicht startenden
   Runner. Der Runner lief (Exit 0, `testcasecount=0`). Als nicht belegt gewertet und nicht als bestanden
   (Bedenken 6).
5. *Ergeben die Bedenken zusammen einen Blocker?* Sie betreffen Umfang und Zeitplan ab Schritt 5 und Nachweise,
   die während Schritt 2 entstehen. Den Einstieg in Schritt 2 blockieren sie nicht, aber Bedenken 1, 10 und 11
   zusammen machen die Neuschätzung nach Schritt 5 zur Pflicht.

Chain-of-Verification: 5 questions checked — verdict unchanged.
