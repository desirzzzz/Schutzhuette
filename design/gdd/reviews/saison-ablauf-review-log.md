# Review-Protokoll: Saison-Ablauf und Spielphasen

## Review — 2026-11-08 — Verdict: NEEDS REVISION
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 1 | Recommended: 4 | Nice-to-have: 1
Summary: Ein sehr sauberes Dokument. Alle 7 Formeln und ihre Beispiele sind nachgerechnet und stimmen (Nacht 480–1080 s, Saison 1800–2400 s, Pause-Beispiel 265,5 s und 1354,5, Druck 0,1 / 0,4 / 0,8, Steigungen 0,02 / 0,033 / 0,05 pro Minute, Hinweis bei 42:00 und 26:40 min). Die 45 Abnahmekriterien sind einzeln prüfbar; Testprofil und 0,37-s-Schritte vermeiden Grenzwerte. Registry (season_length mit referenced_by interaktion.md, phase_progress, season_pressure, season_target_min/max) stimmt mit dem GDD überein. Blockierend ist ein Widerspruch zwischen Dokumenten: Die Solo-Pause hält „alle zeitgesteuerten Systeme“ an, aber das freigegebene Abkühlen in Gegenstandsarten rechnet mit der Serverzeit, die in der Pause weiterläuft.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Core Rules (Regel 11) / Dependencies: Es fehlt eine pausierbare Zeitbasis. „Spielzeit läuft“ ist nur ein Wahrheitswert; Systeme mit gespeicherten Zeitpunkten helfen damit nicht. `is_warm` und `warm_fraction` in `gegenstandsarten.md` rechnen `server_time - cooked_time`; Formel 3 hier lässt die Serverzeit in der Pause weiterlaufen. Ergebnis: Tee wird während der Solo-Pause kalt. Vorschlag: Der Saison-Ablauf stellt eine Spielzeit bereit (zum Beispiel `game_time` = Serverzeit minus Summe der Pausen) und nennt sie als einzige Zeitbasis für zeitgesteuerte Spielregeln; Gegenstandsarten (Abkühlen) und Interaktion (Fortschritt, Kulanz) werden in den Dependencies genannt. Die Anpassung in `gegenstandsarten.md` (Zeitbasis von Formel 3) gehört zur Behebung.
- [RECOMMENDED] Core Rules (Regel 7): Die Meldung „Saison beginnt“ fehlt in der Liste der Meldungen, obwohl Regel 3, Dependencies und AC 32 darauf bauen. Festlegen, wann sie kommt (vor oder nach dem Legen der Startaufstellung, vor „Nachmittag beginnt“), und in AC 19 mitzählen.
- [RECOMMENDED] Core Rules (Regel 6): Der Text sagt, die Nacht dauert, bis der Einsatz „beendet“ meldet; die Mindestdauer `night_min` steht nur in den Formeln und Grenzfällen. Regel 6 sollte sie nennen.
- [RECOMMENDED] Core Rules (Regel 3): Was ein „Platz“ der Startaufstellung ist, bleibt offen. Ein ScriptableObject kann keine Szenenobjekte referenzieren; also entweder Platz-Kennungen, die zu Markern in der Szene passen, oder Koordinaten im Datenobjekt (wichtig auch für „Weitere Hütten“).
- [RECOMMENDED] Game Feel / UI: „Ein Phasenwechsel kommt nie überraschend: 30 s vorher eine Vorwarnung“ geht nicht beim Ende der Nacht durch den Einsatz nach `night_min`, das sofort kommt. Klarstellen, dass die Vorwarnung für Wechsel nach Zeit gilt (Nachmittag → Abend, Abend → Nacht, `night_max`).
- [NICE-TO-HAVE] interaktion.md, Formel 5: `season_length` steht dort mit Source „data file“; sie kommt jetzt aus dem Saison-Ablauf (Registry bereits richtig).
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 95004d0677aa4b699bd937157bea9568d59ff859
Reviewed-Content-Hash: design/registry/entities.yaml 242c6aa07cdcd21249f3b04853518763625140ac
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 24df514642e755780a6943bf2ed493879089675f
Reviewed-Content-Hash: design/gdd/interaktion.md 02f5c1cddd97b2425ca4c7f31c252eacd2fc8d70
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Net/PlayerSpawnApproval.cs 334119bf6873ac2a063134352ababd6c8f286467
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/Stove.cs 15eb380be026aaa6bc1debe15186dad788f69a21

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 1 | Nice-to-have: 2
Summary: Erneutes Review, Dokument vollständig neu gelesen. Der Blocker ist gelöst: Formel 3a `game_time` (Serverzeit minus Pausensumme) ist die einzige Zeitbasis für Spielregeln mit Zeit (Regeln 7 und 11), `phase_elapsed` rechnet mit ihr, Gegenstandsarten (Abkühlen) und Interaktion (`dt`) sind nachgezogen. Nachgerechnet: Pause ab 1500, weiter bei 1620, `paused_total` 120, bei 1630 `game_time` 1510, Tee von 1450 ist 60 s alt; `phase_elapsed` 265,5 / 275,5. Alle 4 Empfehlungen sind eingearbeitet („Saison beginnt“ in Regel 7 und AC 19, `night_min` in Regel 6, Platz = Szenen-Marker mit Kennung, Vorwarnung nur bei Wechseln nach Zeit). Registry stimmt (game_time mit referenced_by Gegenstandsarten und Interaktion, is_warm/warm_fraction auf game_time).
Prior verdict resolved: Yes (1 von 1 Blocker, 4 von 4 Empfehlungen)
Findings:
- [RECOMMENDED] Acceptance Criteria (AC 37, AC 39) und Messweg: Noch auf dem Stand vor `game_time`. AC 39 nennt als synchronisierte Werte nur Phase, Phasenstart, Saisonzähler und Bereit-Liste; `paused_total` fehlt. AC 37 prüft |t − start| ≤ 0,5 s mit t als Serverzeit, `start` ist jetzt Spielzeit; nach einer früheren Solo-Pause in derselben Sitzung liegt der Unterschied bei der Pausensumme und das Kriterium schlägt fälschlich fehl. Vorschlag: `gt=<game_time>` in die Log-Zeile aufnehmen und AC 37 dagegen prüfen, `paused_total` in AC 39 ergänzen.
- [NICE-TO-HAVE] Interactions with Other Systems: Die Tabelle nennt `game_time` nicht; nur Regel 7, Formel 3a und die Dependencies tun es.
- [NICE-TO-HAVE] Registry: `is_warm` und `warm_fraction` könnten `design/gdd/saison-ablauf.md` unter referenced_by führen (die Dependencies hier nennen beide).
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 5fe8fcb37e6904b2eca7cb60b9e09a717ca15643
Reviewed-Content-Hash: design/registry/entities.yaml daa3912f8c99ada490bd01a6e2af9a5482058a34
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md b4250424e75f11cba86b5fda76cc1fe3f04e6918
Reviewed-Content-Hash: design/gdd/interaktion.md c5cbb5690a63041f3c986600296ffd29693a909a
Reviewed-Content-Hash: Assets/_Project/Scripts/Net/PlayerSpawnApproval.cs 334119bf6873ac2a063134352ababd6c8f286467
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/Stove.cs 15eb380be026aaa6bc1debe15186dad788f69a21
