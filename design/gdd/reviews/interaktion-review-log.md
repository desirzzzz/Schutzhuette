# Review-Protokoll: Interaktion

## Review — 2026-11-08 — Verdict: NEEDS REVISION
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 1 | Recommended: 5 | Nice-to-have: 5
Summary: Ein starkes Dokument. Die Regeln sind genau, die 5 Formeln und alle Beispiele sind nachgerechnet und stimmen (Kurbel 11,4 s, Spielraum 1,4 m, Hysterese, Datenprüfung, 320 s / 2100 s = 0,15). Die 50 Abnahmekriterien sind alle einzeln prüfbar, die Schrittzahlen vermeiden Grenzwerte. Registry-Werte (interact_range 2,5, max_server_distance 4,0, drei Formeln) stimmen mit GDD und Code überein. Blockierend ist nur die Ausrüstung am Körper: Ihr fehlt ein Daten- und Zustandsmodell. Das betrifft auch den Nachtrag „Ausnahme Ausrüstung“ in Gegenstandsarten.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Core Rules (Regel 8) / Gegenstandsarten: Die Ausrüstung hat kein Daten- und Zustandsmodell. (a) Woran der Code erkennt, dass Stirnlampe und Funkgerät Ausrüstung sind und welchen Platz (Kopf, Gürtel) sie belegen, steht nirgends. Gegenstandsarten hat dafür kein Feld, die Registry auch nicht; ohne Feld bliebe nur eine Prüfung über den Namen (gegen Architektur-Regel 4 und den Geist von AC 18). (b) Die Zustandstabelle in Gegenstandsarten („jeder Gegenstand hat einen dieser Zustände“) kennt weder „am Körper“ noch „am Haken“. (c) Wie man Ausrüstung vom Haken wieder nimmt, ist nicht beschrieben. Vorschlag: optionale Eigenschaft „Ausrüstung: Platz Kopf oder Gürtel“ in Gegenstandsarten, Zustand „Am Körper“, und Haken-Plätze als liegende Gegenstände, die E wie jede Lampe aufnimmt.
- [RECOMMENDED] Core Rules (Regel 2) / Edge Cases: Der Strahl ignoriert laut Text nur Spielerfiguren. Der eigene gehaltene Gegenstand hängt 0,5 m unter den Augen und 0,9 m vor der Figur; wer mit Teller zum Gast oder mit Axt zum Hackstock nach unten schaut (etwa 25–35°), trifft sonst sein eigenes Gericht oder die Axt. Festlegen: Der Strahl ignoriert auch alles, was Spieler in der Hand oder am Körper tragen, und von anderen getragene Gegenstände sind kein Ziel.
- [RECOMMENDED] Formulas (2.): `target_distance` misst „zum nächsten Punkt der Station“. AC 29 und „ein gemeinsamer Wert für Tragen und Benutzen“ setzen voraus, dass das auch für Gegenstände und Truhenenden gilt (heute misst `PlayerCarry` Wurzel zu Gegenstandsmitte). Im Text „des Ziels“ schreiben.
- [RECOMMENDED] Gegenstandsarten: Offene Frage 5 („Hand oder Körper?“) ist durch Regel 8 beantwortet und steht noch offen. Die Zeile „Interaktion“ in den Tabellen „Interactions“ und „Dependencies“ nennt nur „Werkzeug (Handlung)“, die Interaktion liest aber auch Kategorie und Ausrüstung.
- [RECOMMENDED] Systemverzeichnis, Abschnitt Dependency Map: Der Text ist hinter der Tabelle zurück. „Holzofen ... braucht Tragen und Werfen und Interaktion“ widerspricht dem GDD (Ofen hängt nicht an der Interaktion), HUD nennt die Interaktion nicht, Interaktion nennt Netzwerk-Sitzung und Gegenstandsarten nicht.
- [RECOMMENDED] Tuning Knobs (bei `standard` nur empfohlen): Fehlt. Über ein Dutzend Stellschrauben (interact_range, max_server_distance, rtt_budget, interp_delay, look_grace, pause_speed, resume_speed, walk_grace, min_duration, max_solo_duration, max_solo_penalty, extra_user_bonus, Hakenplätze, Zielgröße 0,4 m) stehen verstreut in den Formeln, ohne Ablageort.
- [NICE-TO-HAVE] Formulas (1.): `crew_multiplier` 0–2 und `progress_rate` 0–4 gelten nur bei max_solo_penalty 2,0; der erlaubte Bereich bis 3 ergibt bis 3 bzw. 6.
- [NICE-TO-HAVE] Formulas (1.), Example: „43 % schneller“ meint 43 % weniger Zeit; die Rate ist 75 % höher.
- [NICE-TO-HAVE] States and Transitions: Die Spielertabelle führt „→ Trägt (E)“, „Trägt“ ist dort kein Zustand.
- [NICE-TO-HAVE] Formulas (Kopf) / Open Question 2: Die Kamerahöhe ist bekannt, 0,7 m über der Mitte der Spielfigur (`CameraMount`, PrototypeSceneBuilder), also etwa 1,78 m über dem Boden.
- [NICE-TO-HAVE] Barrierefreiheit: Halten als Umschalten (Toggle) als Option für die Menüs vormerken (offene Frage 3).
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen, Summary und Game Feel decken das Gefühl ab.
Reviewed-Content-Hash: design/gdd/interaktion.md f312c76e5204a5518f0b4b9eb6598c1420a1eb53
Reviewed-Content-Hash: design/registry/entities.yaml 6e3850ed26d3a06a4aa2cd31caedc3938eb716d1
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md ef9e8ed782c0947e5ea5e476be4248e85548d794
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerHeavyCarry.cs 288da7d15378638677c850cde5d59f642e807f6b
Reviewed-Content-Hash: Assets/_Project/Scripts/Player/PlayerLook.cs 886ad62ff8c92503fbf601aff3e82ce088c400de
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/InputSystem_Actions.inputactions 1a12cb91b67132fda366ae04ba2662092ed8781d

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 3
Summary: Erneutes Review, Dokument vollständig neu gelesen. Der Blocker (Ausrüstung ohne Daten- und Zustandsmodell) ist gelöst: Eigenschaft „Ausrüstung: Kopf|Gürtel“ in Gegenstandsarten, Zustand „Am Körper“, Hakenplatz = „Liegt“, Nehmen mit E, AC 52. Alle 5 Empfehlungen und alle 5 Kleinigkeiten des ersten Reviews sind eingearbeitet (Strahl ignoriert Getragenes, AC 51; target_distance „des Ziels“; Tuning Knobs; Bereiche an max_solo_penalty gebunden; Kamerahöhe; Toggle-Option). Formeln und neue Kriterien nachgerechnet (AC 51: 30° nach unten trifft den Haltepunkt bei 29°, guter Testfall). Registry (equipment_slot kopf/guertel, interact_range 2,5, max_server_distance 4,0) und Systemverzeichnis stimmen mit dem GDD überein.
Prior verdict resolved: Yes (1 von 1 Blocker, 5 von 5 Empfehlungen)
Findings:
- [NICE-TO-HAVE] Edge Cases (Ausrüstung) / Gegenstandsarten Zustände: Beim Verbindungsabbruch erscheint die Ausrüstung „auf Hüfthöhe“, geht also über „Fällt“ nach „Liegt“; die Zustandstabelle in Gegenstandsarten nennt direkt „→ Liegt“. Folgenlos, weil Lampe und Funkgerät nicht zerbrechlich sind.
- [NICE-TO-HAVE] Formulas (4.), Output Range: „Bei 3 Nutzern höchstens 0,5, bei 4 höchstens 0,33“ gilt nur bei max_solo_penalty 2,0.
- [NICE-TO-HAVE] Interactions / Dependencies: „Stirnlampe und Funkgerät gelten als Ausrüstung“ könnte direkt auf die Eigenschaft „Ausrüstung“ verweisen, wie Regel 8 es tut.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/interaktion.md 02f5c1cddd97b2425ca4c7f31c252eacd2fc8d70
Reviewed-Content-Hash: design/registry/entities.yaml b21de501b14fde61b25ff79f0e0114bcad6d6d54
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md 24df514642e755780a6943bf2ed493879089675f
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerHeavyCarry.cs 288da7d15378638677c850cde5d59f642e807f6b
Reviewed-Content-Hash: Assets/_Project/Scripts/Player/PlayerLook.cs 886ad62ff8c92503fbf601aff3e82ce088c400de
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/InputSystem_Actions.inputactions 1a12cb91b67132fda366ae04ba2662092ed8781d

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: L
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 0 | Nice-to-have: 1 (neu)
Summary: Erneutes Review nach dem Nachtrag aus dem Review von Saison-Ablauf. Geändert seit dem letzten APPROVED-Stand (+442 Byte): `dt` ist vergangene Spielzeit `game_time` (in der Pause solo 0), `season_length` hat Source „calculated“ aus dem Saison-Ablauf, Saison-Ablauf steht als weiche Abhängigkeit unter „Hängt ab von“. Geprüft wurden die geänderten Abschnitte (Formeln 1 und 5, Dependencies) und das ganze Dokument per Suche auf Zeitbezüge; passt zu Formel 3a in `saison-ablauf.md`, Registry und Systemverzeichnis (Zeile 8) stimmen.
Prior verdict resolved: Yes (Querbezug zum Blocker aus dem Review von Saison-Ablauf behoben)
Findings:
- [NICE-TO-HAVE] Acceptance Criteria: Kein Kriterium prüft, dass der Fortschritt einer Handlung in der Solo-Pause steht (`dt` = 0); AC 28 in `saison-ablauf.md` deckt nur Uhr, Physik und Ofen ab.
- [NICE-TO-HAVE] Weiter offen aus dem letzten APPROVED-Review: „Am Körper → Liegt“ über „Fällt“, Grenzen für 3 und 4 Nutzer nur bei max_solo_penalty 2,0, Tabellen nennen Ausrüstung über die Namen statt über die Eigenschaft.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen.
Reviewed-Content-Hash: design/gdd/interaktion.md c5cbb5690a63041f3c986600296ffd29693a909a
Reviewed-Content-Hash: design/registry/entities.yaml daa3912f8c99ada490bd01a6e2af9a5482058a34
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md b4250424e75f11cba86b5fda76cc1fe3f04e6918
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 5fe8fcb37e6904b2eca7cb60b9e09a717ca15643
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerCarry.cs 8d385a458c8d53cc376ab954f80bf24d0c70ef70
Reviewed-Content-Hash: Assets/_Project/Scripts/Carrying/PlayerHeavyCarry.cs 288da7d15378638677c850cde5d59f642e807f6b
Reviewed-Content-Hash: Assets/_Project/Scripts/Player/PlayerLook.cs 886ad62ff8c92503fbf601aff3e82ce088c400de
Reviewed-Content-Hash: Assets/_Project/Prefabs/Player.prefab 38213839be69ccff2746d64fb2e34d19eaabb290
Reviewed-Content-Hash: Assets/InputSystem_Actions.inputactions 1a12cb91b67132fda366ae04ba2662092ed8781d
