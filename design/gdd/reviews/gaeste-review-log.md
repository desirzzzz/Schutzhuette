# Review-Protokoll: Gäste und Bedürfnisse

## Review — 2026-11-09 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 4 | Nice-to-have: 7
Summary: Ein dichtes, rechnerisch sauberes Dokument. Alle 8 Formeln, ihre Beispiele und die Zahlen in den 49 Abnahmekriterien sind nachgerechnet und stimmen. Geprüft wurden unter anderem:
- Ankunft: 0,38 / 0,285 / 0,52 / 0,16 pro Minute; erster Gast nach 3,26 min (zu viert) bzw. 5,71 min (solo); Gästetabelle je Phase; Testprofil-Budget 1,02; Kriterium 8 (102. Schritt).
- Geduld: 184,8 / 84 / 70 s.
- Zufriedenheit, Kälte und Trinkgeld: Gewinn 15,45; Kälte 84,67 / 63,33; alle Trinkgeld-Fälle in Kriterium 20; Kettenwerte 9 / 16 / 10; Saison-Maximum etwa 80 solo.
- Lärm, Chaos und Stimmung: Sperrzeiten und Hysterese.
Die Werte stimmen mit der Registry, `gegenstandsarten.md` (5,01 und 7,0 m/s, Massen, „Fällt“, `is_warm`), `saison-ablauf.md` (Druck-Stützwerte, `night_min` als Bezug, Testprofil 60/60/60/120, Meldungen) und dem Code des Ofens (1,5/s) überein. Offen bleiben vier Klarstellungen ohne Widerspruch zu anderen Dokumenten: Gegenstand auf dem reservierten Platz nach `blocked_max`, Auslöser des Einfrierens beim Abbruch vor der Nacht, fehlende Wärme in zwei Kriterien und Editor-Prüfungen für Teiler und Streuung.
Prior verdict resolved: First review
Findings:
- [RECOMMENDED] Edge Cases (Wege und Blockaden): „Gegenstand auf reserviertem Platz: blockiert, bis jemand den Platz frei räumt“ widerspricht dem Vorbeizwängen nach `blocked_max`. Bei einem Sitz oder Bett gibt es kein „vorbei“. Solo mit der Truhe (80 kg, nur zu zweit) auf dem Sitz hängt der Gast fest und verliert dauerhaft 0,2/s. Festlegen, ob er sich trotzdem setzt (Gegenstand für ihn durchlässig, bis er den Platz verlässt) oder was sonst passiert. Ein Kriterium dazu fehlt.
- [RECOMMENDED] Core Rules (Regel 13) und Edge Cases (Abbruch): Der Auslöser für „eingefroren wie nach Nacht vorbei“ fehlt, wenn am Nachmittag oder Abend abgebrochen wird. Der Saison-Ablauf schickt „Nacht vorbei“ (`abort`) nur in der Nacht, sonst nur den Wechsel zur Vorbereitung. In der Vorbereitung läuft `game_time` weiter, die Uhren der Gäste liefen also weiter. Den Auslöser nennen (zum Beispiel jede Phase außer Nachmittag, Abend und Nacht). Kriterium 25 braucht ihn für den Stub.
- [RECOMMENDED] Acceptance Criteria 19 und 33: Die Wärme fehlt, dabei ändert sich S über 60 s bzw. 3,5 s je nach Stub. Bei Wärme 0 hätte Kriterium 19 S_before 56 und 5 statt 8 Münzen, Kriterium 33 nicht 65/50. „Wärme 50“ ergänzen oder einen Standardwert in den Testvorgaben festlegen.
- [RECOMMENDED] Formulas (Datenprüfung): Es wird durch Stellwerte geteilt (`patience_total`, `tip_full`, `noise_radius`, `cold_threshold`). Mit `arrival_jitter` ≥ 1 wird `next_cost` ≤ 0, dann ist in jedem Schritt ein Gast fällig. Mit `patience_pressure_cut` 1 bei Druck 1 wird `patience_total` 0, dann teilt der Ring durch null. `[Min]` kann „< 1“ nicht ausdrücken. Eine Prüfung wie `phase_data_valid` im Saison-Ablauf ergänzen: `arrival_jitter` < 1, `patience_pressure_cut` < 1, Teiler > 0; ein Verstoß ist ein Fehler im Editor.
- [NICE-TO-HAVE] Formulas (4.), Example: „etwa 5,4 Punkte“ sind genau 5,33. Kriterium 29 rechnet schon richtig mit 84,67.
- [NICE-TO-HAVE] Core Rules (Regel 1) und Formulas (5.): Bei der Familie offenlassen, welche Figur für den Lärmabstand zählt (vermutlich die nähere). Klarstellen, dass beide Figuren dieselbe Station für Servieren und Erste Hilfe sind.
- [NICE-TO-HAVE] Core Rules (Regel 10) und States: Unklar ist, ob ein Gast gewählt werden kann, der drinnen unterwegs ist (Kommt an im Vorraum, Blockiert, Geht zum Bett). Die Zustandstabelle führt nur Wartet, Mault und Bleibt nach Vermisst, Kriterium 40 schließt nur „Kommt an, noch draußen“ aus.
- [NICE-TO-HAVE] Core Rules (Regel 15): Einmalige Ereignisse an die Clients fehlen in der Liste: „Aua“, Trinkgeld „+6“ mit Klimpern. Kriterium 47 und die UI-Anforderungen brauchen sie. Für die ADR „Wegfindung im Netz“ mitnehmen.
- [NICE-TO-HAVE] Acceptance Criteria (Messweg): Die Log-Zeile hat nur `t=<ServerTime>`. Der Saison-Ablauf loggt inzwischen auch `gt=<game_time>`; mit Pause lassen sich Geduldswerte so leichter prüfen.
- [NICE-TO-HAVE] Dependencies: Systemverzeichnis Zeile 12 (Kochen) nennt die Gäste nicht, das GDD führt Kochen als harte Abhängigkeit. Weil die Bedürfnis-Kennung in den Gegenstandsarten steht, ist die Abhängigkeit indirekt. Als indirekt kennzeichnen oder beim Kochen-GDD nachtragen.
- [NICE-TO-HAVE] Formulas (2., 3.), Beobachtung für den Freundestest:
  - Maulen setzt S unabhängig vom bisherigen Erfolg auf 0. Ein Gast mit perfektem ersten Bedürfnis endet bei höchstens 25 plus Wärmebonus. Für die Bewertung prüfen, ob sich das fair anfühlt.
  - Der Tee des Bergsteigers hat nachts 84 s, das ist nur das 1,4- bis 1,9-Fache von Kochen und Weg statt des 2- bis 3-Fachen.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen. Overview und Game Feel tragen das Gefühl.
Reviewed-Content-Hash: design/gdd/gaeste.md 787d8f148aac8fe5b1ff19d7984f11f9c3e5e0fd
Reviewed-Content-Hash: design/registry/entities.yaml fb26dc4b0eb65fe96fd333f10ff541699ab78386
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 3867aaafba45ca9355efba37f670d0ba9edc123b
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 236810092679a234ea857ea7714119299aa9a794
Reviewed-Content-Hash: design/gdd/interaktion.md 21098903d62d50f383cb62325bf9981a61eee317
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/levels/huette-skizze.md 6d576a81a483763794a5a6c45c2d2c297a4932e7
Reviewed-Content-Hash: docs/KONZEPT.md 61b8e6849eea854b6e0f56192ab726ab20048b99
Reviewed-Content-Hash: docs/GRAUBOX.md cb7f69e759c4331fb5f4e45ab9e2f8160f86635c
Reviewed-Content-Hash: production/milestones/vertical-slice.md f87133252712d26611b2f4b3eb2dc0acc497ac05
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/Stove.cs 15eb380be026aaa6bc1debe15186dad788f69a21
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888
Reviewed-Content-Hash: Assets/_Project/Data/StoveSettings.asset 1e643672ddeb4bbe260a73bfb6ab528631396eef
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/Shelter.cs 9a96657b007cb106379830e28f20ba59d18ccf71

## Review — 2026-11-09 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 2
Summary: Erneutes Review nach Einarbeitung aller 4 Empfehlungen und 7 Kleinigkeiten vom selben Tag. Das ganze Dokument wurde neu gelesen, nicht nur die Änderungen.
- Gegenstand auf dem reservierten Sitz oder Bett: Nach `blocked_max` setzt bzw. legt sich der Gast; der Gegenstand ist für ihn durchlässig und bewegt sich nicht (Grenzfall und Kriterium 38).
- Regel 13 friert die Gäste-Logik in jeder Phase außer Nachmittag, Abend und Nacht ein, also auch beim Abbruch vor der Nacht (Kriterium 25).
- Der Wärme-Stub steht standardmäßig auf 50; die Kriterien 19 und 33 nennen ihn.
- Neue Formel 9 `guest_data_valid`: Die Startwerte erfüllen sie (Streuung 0,4 und Druckabzug 0,3 unter 1, Teiler > 0, 60 ≥ 40, 20 ≤ 25 < 60 ≤ 65, Gewichte je Phase 4 / 5 / 1). Die sechs Fehlerfälle in Kriterium 2 verletzen je genau eine Bedingung.
- Die Kleinigkeiten (5,33; nähere Figur der Familie; Vermisster nur am Platz, auch aus Genießt; einmalige Ereignisse ans Netz; `gt=` im Log; Kochen indirekt; Offene Frage 10) sind stimmig umgesetzt.
Zahlen, Registry und Nachbardokumente sind unverändert stimmig. Es gibt keinen neuen Widerspruch.
Prior verdict resolved: Yes (alle 4 Empfehlungen und 7 Kleinigkeiten des ersten Reviews umgesetzt)
Findings:
- [NICE-TO-HAVE] Formulas (Einleitung): „Harte Grenzen setzt nur der Editor (`[Min]`)“ passt nicht mehr ganz zu Formel 9, die sagt, dass `[Min]` nicht reicht. Besser: „Harte Grenzen setzen `[Min]` und Formel 9.“
- [NICE-TO-HAVE] Acceptance Criteria 2: Die Fehlerfälle prüfen 6 der Bedingungen von Formel 9. Ungeprüft bleiben `patience_base`, `noise_radius` und `cold_threshold` gleich 0 sowie `happy_out` > `happy_in` und `mault_out` ≥ `happy_out`. Ergänzen oder die Liste als Beispiele kennzeichnen.
- [ADVISORY] Player Fantasy fehlt weiterhin; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/gaeste.md c21b85902d8e56b80f66134061bb6e04932e1a95
Reviewed-Content-Hash: design/registry/entities.yaml fb26dc4b0eb65fe96fd333f10ff541699ab78386
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 3867aaafba45ca9355efba37f670d0ba9edc123b
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 236810092679a234ea857ea7714119299aa9a794
Reviewed-Content-Hash: design/gdd/interaktion.md 21098903d62d50f383cb62325bf9981a61eee317
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/levels/huette-skizze.md 6d576a81a483763794a5a6c45c2d2c297a4932e7
Reviewed-Content-Hash: docs/KONZEPT.md 61b8e6849eea854b6e0f56192ab726ab20048b99
Reviewed-Content-Hash: docs/GRAUBOX.md cb7f69e759c4331fb5f4e45ab9e2f8160f86635c
Reviewed-Content-Hash: production/milestones/vertical-slice.md f87133252712d26611b2f4b3eb2dc0acc497ac05
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/Stove.cs 15eb380be026aaa6bc1debe15186dad788f69a21
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888
Reviewed-Content-Hash: Assets/_Project/Data/StoveSettings.asset 1e643672ddeb4bbe260a73bfb6ab528631396eef
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/Shelter.cs 9a96657b007cb106379830e28f20ba59d18ccf71

> **Nachtrag der Autorensitzung (2026-11-09):** Die beiden Kleinigkeiten des zweiten Reviews sind umgesetzt.
> Die Formulas-Einleitung nennt jetzt `[Min]` und Formel 9 als harte Grenzen. Kriterium 2 verletzt jede
> Bedingung von Formel 9 genau einmal: zusätzlich `noise_radius` 0, `cold_threshold` 0, ein `patience_base` 0,
> `mault_out` 60 (gleich `happy_out`) und `happy_out` 70 (über `happy_in`). Kein neues Review nötig, da nur
> Text und Testfälle geschärft wurden.
