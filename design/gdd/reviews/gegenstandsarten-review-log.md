# Review-Protokoll: Gegenstandsarten

## Review — 2026-11-08 — Verdict: NEEDS REVISION
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 2 | Recommended: 9
Summary: Das Dokument ist vollständig (alle 6 bei `standard` geforderten Abschnitte), sauber gegliedert, und alle 27 Registry-Einträge stimmen mit dem GDD überein. Blockierend sind zwei sachliche Fehler: Die Haltehöhe in der Bruchformel ist mit 1,9 m angenommen, im Code liegt der Haltepunkt aber 0,2 m über der Kapselmitte, also bei etwa 1,2 m. Außerdem verspricht die Overview ein Feld „Platzbedarf“, das in den Core Rules fehlt. Beides ist schnell behoben, die Bruchgeschwindigkeit 7,0 m/s bleibt dabei richtig.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Formulas (2. Zerbrechen): `hold_height` 1,8–1,9 m („Kamera + 0,2 m“) stimmt nicht mit dem Code überein. `_holdLocalPosition` (0; 0,2; 0,9) zählt ab Kapselmitte (Player.prefab, Tooltip in PlayerCarry.cs:36), die Kapselmitte liegt 1,0 m über dem Boden, die Kamera bei 1,7 m. Die Haltehöhe ist also etwa 1,2 m, `release_fall_speed` am Boden etwa 4,85 statt 5,94–6,11 m/s, am Tisch etwa 2,4 statt 4,2 m/s. Betroffen: Variablentabelle, Beispieltabelle, Prüfwert „Haltehöhe 1,9 m“, Prüffenster 6,72–7,2 (wird etwa 5,3–7,2), Edge Case „Kiste aus Haltehöhe 6 m/s“, Game Feel „enges Fenster“, Cross-Reference „0,2 m über der Kamera“, Open Questions 1 und 2 (Grenzwinkel etwa 39° statt 28°), AC 13 und AC 15, Registry `release_fall_speed` (output_range [0, 6.11], Notiz).
- [BLOCKING] Overview: „Das Lager nimmt den Platzbedarf“, aber die Core Rules definieren kein Feld Platzbedarf, und das Lager liest laut Interaktionstabelle nur Ware und Portionen. Entweder streichen oder als Feld aufnehmen.
- [RECOMMENDED] Formulas (1. und 2.): Beispiele rechnen mit 4,5 m/s Laufgeschwindigkeit, im Prefab sind es 4 m/s (12,5 m in 2,8 s; Wurf aus dem Laufen 12,5 statt etwa 12,0 m/s). Die Ergebnisse ändern sich nicht.
- [RECOMMENDED] Formulas (2. Output Range): Begründung „Wer im Gehen ablegt, käme auf 7,45 m/s“ stimmt nicht. Ablegen setzt die Geschwindigkeit auf 0 (PlayerCarry.cs:211). Die Regel mit dem senkrechten Anteil bleibt richtig, sie begründet sich über schräge Würfe.
- [RECOMMENDED] Core Rules / Visual-Audio: „Holz oder Weich, das sind die Geräuschgruppen von heute“ stimmt nicht. Heute gibt es eine Aufprallgruppe (Holz) plus eine dumpfe Schicht nach Gewicht, die die `impactSoft`-Clips nutzt. Die Klanggruppe pro Art ist neue Arbeit in den Geräuschen.
- [RECOMMENDED] Dependencies: Es fehlen Materialseilbahn (erzeugt Vorratspakete mit Ware), Versorgung: Betten und Erste Hilfe (Erste-Hilfe-Kasten), Solo-Hüttenhund (trägt kleine Dinge, liest Masse) und der Besitzer der „Startaufstellung“. Im Systemverzeichnis fehlt Gegenstandsarten bei Materialseilbahn und Versorgung unter „Depends On“.
- [RECOMMENDED] Acceptance Criteria (AC 25): Der Ofen nimmt höchstens 240 s Brennzeit (`StoveSettings._maxFuelSeconds`). „R + 45 s“ gilt nur bei R ≤ 195 s; das Kriterium braucht eine feste Restzeit, z. B. R = 60 s.
- [RECOMMENDED] Acceptance Criteria (AC 30): „höchstens 0,5 s auseinander“ lässt sich im Freundestest mit bloßem Auge nicht messen. Messweg nennen (Debug-Anzeige oder Log mit Serverzeit des Wechsels).
- [RECOMMENDED] Acceptance Criteria (AC 5): Den Host-Start in einem EditMode-Test zu verweigern, braucht eine prüfbare Funktion (z. B. ein Prüfergebnis, das der Host-Start liest). Die Build-Sperre aus Core Rule 1 deckt kein Kriterium ab.
- [RECOMMENDED] Tuning Knobs (bei `standard` nur empfohlen): Die Stellschrauben stehen verstreut in den Formeln (max_solo_mass, break_speed, cool_time, floor_factor, Prüfwerte Haltehöhe, Wurftempo, max_path, walk_speed_min, Toleranz 10 %). Eine Tabelle mit sicheren Bereichen und Ablageort (Katalog-Einstellungen) fehlt; bei max_path und walk_speed_min ist der Ablageort offen.
- [RECOMMENDED] Edge Cases: AC 2 prüft „leere Kennung“ und „leerer Name“ als Fehler, die Edge Cases nennen beides nicht.
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 71bfe804746f47d221d2fd0f720678387a8d222f
Reviewed-Content-Hash: design/registry/entities.yaml cd9e86e41a7f62ccad75e656bda146a81837cb6c
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/SoundSettings.cs 332cd8ed7783445496dd2d140dda1d783d199ef8
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 6
Summary: Erneutes Review nach der Überarbeitung, Dokument vollständig neu gelesen. Beide Blocker und alle 9 Empfehlungen des ersten Reviews sind eingearbeitet. Die neuen Werte zur Haltehöhe (1,28 m, 5,01 m/s, Fenster 5,51–7,2 m/s, 29° an der Wand, 38° auf den Boden, AC 10/12/13) sind nachgerechnet und stimmen mit Player.prefab und der Registry überein. Die Registry-Suche über das ganze Dokument (157 Zeilen) fand keinen Widerspruch. Übrig sind nur Kleinigkeiten, die den Bau nicht behindern.
Prior verdict resolved: Yes (2 von 2 Blockern, 9 von 9 Empfehlungen)
Findings:
- [NICE-TO-HAVE] Formulas (2.) / Tuning Knobs: Fenster-Untergrenze steht als 5,51, genau sind es 5,5125. Ein break_speed von 5,51 löst also einen Hinweis aus. AC 13 prüft richtig mit 5,52; im Text besser „5,52 (aufgerundet)“.
- [NICE-TO-HAVE] Formulas (2.): `hold_height` hat als Source „constant“, ist laut Tuning Knobs aber ein Prüfwert in den Katalog-Einstellungen (data file).
- [NICE-TO-HAVE] Acceptance Criteria (AC 25): Am Ende steht noch „R ändert sich nicht“, R ist nicht mehr definiert. Gemeint ist „die Restzeit bleibt 60 s“.
- [NICE-TO-HAVE] States and Transitions: Die Tabelle nennt keine Übergänge nach „Verbraucht“ (Ofen aus „Fällt“, Servieren und Auspacken aus „Getragen“, Hacken aus „Liegt“).
- [NICE-TO-HAVE] Core Rules: Die Kategorie liest außer dem Sperrgut-Hinweis kein System. Zweck nennen oder als reine Ordnung im Editor kennzeichnen.
- [NICE-TO-HAVE] Systemverzeichnis / Registry: Nachteinsatz, Ereignisse und Sprachchat nennen Gegenstandsarten nicht unter „Depends On“, obwohl das GDD sie als weiche Nutzer führt. Registry `chop_mass_balance` hat output_range [-80, 80], das GDD sagt „beliebig“.
- [ADVISORY] Player Fantasy fehlt weiterhin; bei `standard` nur empfohlen, Summary und Game Feel decken das Gefühl ab.
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 75afe7eac7903bf83515bdfd5c75cc65f4d46b36
Reviewed-Content-Hash: design/registry/entities.yaml 6858652dd9f8221e909409e49b7a1df94f53e34c
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/SoundSettings.cs 332cd8ed7783445496dd2d140dda1d783d199ef8
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888

## Review — 2026-11-08 — Verdict: NEEDS REVISION
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 1 | Recommended: 2
Summary: Prüfung des Nachtrags „Ausnahme Ausrüstung“ in Regel 6 (aus dem GDD Interaktion) im Rahmen des Reviews von Interaktion. Laut startender Sitzung und Größenvergleich (+291 Byte gegenüber dem freigegebenen Stand) ist das die einzige Änderung seit dem APPROVED-Review; der Rest wurde nicht erneut vollständig gelesen. Die Ausnahme passt inhaltlich zu Interaktion Regel 8, reißt aber eine Lücke ins Daten- und Zustandsmodell dieses Dokuments.
Prior verdict resolved: Approved-Stand wurde durch den Nachtrag geändert
Findings:
- [BLOCKING] Core Rules / States and Transitions: Für Ausrüstung fehlt ein Feld (Platz Kopf oder Gürtel), sonst erkennt der Code Stirnlampe und Funkgerät nur am Namen. Die Zustandstabelle („jeder Gegenstand hat einen dieser Zustände“) kennt weder „Am Körper“ noch „Am Haken“. Siehe Blocker im Review von Interaktion.
- [RECOMMENDED] Open Questions: Frage 5 („Hand oder Körper?“) ist durch den Nachtrag beantwortet und steht noch offen.
- [RECOMMENDED] Interactions with Other Systems / Dependencies: Die Zeile „Interaktion“ nennt nur „Werkzeug (Handlung)“; gelesen werden auch Kategorie und Ausrüstung.
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md ef9e8ed782c0947e5ea5e476be4248e85548d794
Reviewed-Content-Hash: design/registry/entities.yaml 6e3850ed26d3a06a4aa2cd31caedc3938eb716d1
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/gdd/interaktion.md f312c76e5204a5518f0b4b9eb6598c1420a1eb53
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/SoundSettings.cs 332cd8ed7783445496dd2d140dda1d783d199ef8
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 3 (neu) + 6 (aus dem APPROVED-Review davor, weiter offen)
Summary: Erneutes Review nach dem Nachtrag Ausrüstung, Dokument vollständig neu gelesen. Der Blocker ist gelöst: optionale Eigenschaft „Ausrüstung: Kopf oder Gürtel“ (Code erkennt Ausrüstung nur am Feld), Zustand „Am Körper“ (fällt und zerbricht nie), Hakenplatz als „Liegt“, Startkatalog Stirnlampe Kopf / Funkgerät Gürtel, AC 34. Beide Empfehlungen sind eingearbeitet (offene Frage 5 beantwortet; Zeile Interaktion nennt Werkzeug, Kategorie, Ausrüstung). Registry stimmt (equipment_slot kopf/guertel), die übrigen Werte sind unverändert gegenüber dem letzten APPROVED-Stand.
Prior verdict resolved: Yes (1 von 1 Blocker, 2 von 2 Empfehlungen)
Findings:
- [NICE-TO-HAVE] Dependencies: „Beidseitig: Noch hat keins dieser Systeme ein Design-Dokument“ stimmt nicht mehr, Interaktion hat eins.
- [NICE-TO-HAVE] States and Transitions: „Am Körper → Liegt“ beim Verbindungsabbruch; die Ausrüstung erscheint auf Hüfthöhe und geht damit über „Fällt“.
- [NICE-TO-HAVE] Edge Cases / Datenprüfung: Keine Editor-Prüfung für unsinnige Kombinationen, etwa Ausrüstung mit Trageart „zu zweit“.
- [NICE-TO-HAVE] Weiter offen aus dem vorigen APPROVED-Review: Fenstergrenze 5,51 (genau 5,5125), `hold_height` mit Source „constant“, „R“ in AC 25, Übergänge nach „Verbraucht“, Registry-Bereich von `chop_mass_balance`.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 24df514642e755780a6943bf2ed493879089675f
Reviewed-Content-Hash: design/registry/entities.yaml b21de501b14fde61b25ff79f0e0114bcad6d6d54
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/gdd/interaktion.md 02f5c1cddd97b2425ca4c7f31c252eacd2fc8d70
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/SoundSettings.cs 332cd8ed7783445496dd2d140dda1d783d199ef8
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 1 (neu)
Summary: Erneutes Review nach dem Nachtrag aus dem Review von Saison-Ablauf. Geändert seit dem letzten APPROVED-Stand (+419 Byte): `is_warm` und `warm_fraction` rechnen mit `game_time` statt der Serverzeit (Variablentabelle, AC 18 und 19), dazu ein Satz unter „Hängt ab von“ (die Formel bekommt `game_time` als Wert, keine Abhängigkeit). Vollständig neu gelesen wurden Core Rules, Zustände, Formeln, Edge Cases und Dependencies; der Rest wurde per Suche gegen den letzten Stand geprüft. Im Dokument steht keine `server_time` mehr; Registry stimmt.
Prior verdict resolved: Yes (Querbezug zum Blocker aus dem Review von Saison-Ablauf behoben)
Findings:
- [NICE-TO-HAVE] Dependencies: Die Zeile „Saison-Ablauf und Spielphasen“ steht noch auf „kein Dokument“, und „Beidseitig: Noch hat keins dieser Systeme ein Design-Dokument“ stimmt nicht mehr (Interaktion und Saison-Ablauf haben eins).
- [NICE-TO-HAVE] Weiter offen aus früheren Reviews: Fenstergrenze 5,51 (genau 5,5125), `hold_height` mit Source „constant“, „R“ in AC 25, Übergänge nach „Verbraucht“, „Am Körper → Liegt“ beim Verbindungsabbruch läuft über „Fällt“, keine Prüfung unsinniger Kombinationen (Ausrüstung zu zweit), Registry-Bereich von `chop_mass_balance`.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md b4250424e75f11cba86b5fda76cc1fe3f04e6918
Reviewed-Content-Hash: design/registry/entities.yaml daa3912f8c99ada490bd01a6e2af9a5482058a34
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/gdd/interaktion.md c5cbb5690a63041f3c986600296ffd29693a909a
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 5fe8fcb37e6904b2eca7cb60b9e09a717ca15643
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/_Project/Scripts/Audio/SoundSettings.cs 332cd8ed7783445496dd2d140dda1d783d199ef8
Reviewed-Content-Hash: Assets/_Project/Scripts/Hut/StoveSettings.cs c76e57a138295b89180200d94d392b4fefbd7888
