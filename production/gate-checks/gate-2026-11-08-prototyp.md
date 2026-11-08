# Gate-Check: Prototyp-Prüfpunkt → Vertical Slice

- **Datum:** 2026-11-08
- **Geprüft von:** gate-check (Solo-Modus, `workflow: minimal`, `qa.level: minimal`)
- **Grundlage:** `production/qa/playtests/playtest-2026-11-08-freund.md`, CLAUDE.md (Meilenstein und
  Prüfpunkt), `artifact-check.sh --phase technical-setup` und `--path minimal`

## Einordnung

Das Framework hat keinen eigenen Schritt „Prototyp → Vertical Slice“. In seinen Stufen liegen Prototyp
und Vertical Slice beide in „Pre-Production“. Geprüft wurden deshalb zwei Dinge:

- **A.** Das Framework-Gate Technical Setup → Pre-Production auf Stufe `minimal`.
- **B.** Der Prüfpunkt aus CLAUDE.md: „Erst wenn das Tragen Spaß macht, geht es zum Vertical Slice.“
  Bei Widersprüchen gilt CLAUDE.md, deshalb entscheidet B.

## A. Technical Setup → Pre-Production (`minimal`)

### Pflicht-Artefakte: 2/3 vorhanden

- [x] Engine eingerichtet (`project.yaml`: Unity 6000.6.6f1, C#, URP)
- [x] Game Brief (`design/game-brief.md`)
- [ ] Story-Dateien (`production/epics/*/story-*.md`): fehlen. Das Projekt plant über die
  Meilenstein-Liste in CLAUDE.md statt über Stories. Für den Vertical Slice lassen sie sich mit
  `/create-stories` anlegen.

Bei `minimal` nicht verlangt und alle nicht vorhanden: Architektur-Dokument, ADRs, Architektur-Review,
Control Manifest, Barrierefreiheit, UX-Muster, CI-Testlauf. `implement` und `story-done` sind nicht
vom Datenträger prüfbar (NO_CHECK), ohne Stories aber ohnehin leer.

### Qualität

- Leistung: Es gibt keine Budgets in `project.yaml` und keine Profiling-Daten. Bei
  `performance.enforce: warn` ist das nur ein Hinweis. Das Spiel zeigt die FPS oben links an.
- Tests: Bei `qa.level: minimal` nicht verlangt.
- Director-Panel: übersprungen (Solo-Modus).

## B. Prüfpunkt aus CLAUDE.md

| Punkt | Stand |
| --- | --- |
| Alle 7 Meilenstein-Punkte | erledigt (alle Häkchen in CLAUDE.md gesetzt) |
| Mit simulierter Verzögerung (rund 100 ms) getestet | erledigt (Network Simulator im Editor) |
| Freundestest mit Bericht | erledigt (Freundestest 1, Build vermutlich 0.1.0) |
| Tragen macht Spaß | **nur „ganz okay“**: funktioniert, hat aber noch nicht richtig gezündet |
| Spaß mit dem aktuellen Build 0.1.4 zu zweit | **NOT ASSESSED**: nicht zu zweit gespielt |
| Geräusche zu zweit | **NOT ASSESSED**: nicht zu zweit gespielt |

## Blocker

1. **NOT ASSESSED: Zweiertest mit 0.1.4 fehlt.** „Gewicht spüren“, Spielfiguren, Farben und Ton
   kamen nach Freundestest 1 dazu und sollen genau den Spaß heben. Ob sie das tun, ist unbekannt.
   Schließen lässt sich das mit einer Runde mit dem Freund (Aufgaben aus `docs/FREUNDESTEST.md`).

## Bedenken

1. **Spaß nur „ganz okay“** statt eines klaren Ja. Die Daten stammen vom alten Build 0.1.0.

## Empfehlungen

- Den Vertical Slice jetzt nur **planen** (Umfang, Reihenfolge), nichts davon bauen.
- Freundestest 2 mit 0.1.4, diesmal mit Notizen (lustige Momente, Host, Ping, Dauer). Danach
  `/playtest-report` und diesen Gate-Check erneut.
- Bleibt der Spaß bei „ganz okay“: erst den Kern verbessern, zum Beispiel mit der Idee in
  `docs/IDEEN.md` (lässt einer los, fällt nur sein Ende der Truhe).
- Vor dem Bau des Vertical Slice auf `standard` hochstellen (`/settings modes.rigor=standard`), so
  steht es in CLAUDE.md.

## Ergebnis: CONCERNS

Das Ergebnis ist CONCERNS mit einem offenen NOT-ASSESSED-Punkt. Solange der offen ist, gibt der
Gate-Check den Wechsel zum Vertical Slice nicht frei, auch nicht gegen Annahme der Risiken.
`project.stage` bleibt unverändert (nicht gesetzt).

## Gegencheck

5 Fragen geprüft, das Ergebnis bleibt unverändert.

1. *Ist ein Bedenken eigentlich ein Blocker?* CLAUDE.md macht Spaß zur Bedingung für den Vertical
   Slice (nachgelesen). Deshalb wird der Bau nicht freigegeben. Planen ist kein Bau.
2. *Lässt es sich in der nächsten Phase lösen, oder wächst es?* Eine Runde mit dem Freund klärt es.
   Wer den Vertical Slice auf einem Kern baut, der nicht zündet, wird es später teuer bezahlen.
3. *Wurde ein FAIL zum CONCERNS geschönt?* „Ganz okay“ stammt laut Bericht von Build 0.1.0
   (nachgelesen). Ein FAIL auf diesen alten Daten würde den Befund überziehen, ein PASS wäre falsch.
4. *Gibt es ungeprüfte Artefakte?* Architektur, ADRs und Tests fehlen, sind bei `minimal` aber
   nicht verlangt. Ab `standard` werden sie wichtig, siehe Empfehlung.
5. *Blockieren die Punkte zusammen?* Ja, bis zum Zweiertest mit 0.1.4. Das deckt sich mit dem Plan
   des Entwicklers: heute planen, später bauen.
