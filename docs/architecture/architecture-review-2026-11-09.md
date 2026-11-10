## Architecture Review Report
Date: 2026-11-09
Mode: full
Workflow: standard
Engine: Unity 6000.6.6f1 (URP, PhysX), Netcode for GameObjects 2.13.3
GDDs Reviewed: 5 (gegenstandsarten, interaktion, saison-ablauf, steam-lobby, gaeste; dazu systems-index.md)
ADRs Reviewed: 3 (ADR-0001, ADR-0002, ADR-0003, alle Proposed)

Reviewed-Content-Hash: docs/architecture/adr-0001-netz-autoritaet-und-physik.md 5e1802946887faa10f800f1a85fd2d2b70e48c38
Reviewed-Content-Hash: docs/architecture/adr-0002-gegenstandsarten-katalog.md 6726dd8da44b06cbcd14a7e3e44985351f5d3d42
Reviewed-Content-Hash: docs/architecture/adr-0003-saison-uhr-und-meldungen.md 373ef131605b9178a8460fc065c9861e3dc5976a
Reviewed-Content-Hash: design/gdd/gaeste.md 7cdd894c5b12a3a1e39fc03f33de69c626eaf371
Reviewed-Content-Hash: design/gdd/gegenstandsarten.md c50b337841bb0d3203321f12c946919a4161739d
Reviewed-Content-Hash: design/gdd/interaktion.md 21098903d62d50f383cb62325bf9981a61eee317
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 74a3f38e95c93f343405cb43dc1ae054d71a79b1
Reviewed-Content-Hash: design/gdd/steam-lobby.md 402a1b9120d2c3b9a8238cc5e279de8acfcaa5e3
Reviewed-Content-Hash: design/gdd/systems-index.md af380d99cbcea1ee53a47fc5e87c9d2f6f811e84
Reviewed-Content-Hash: docs/architecture/tr-registry.yaml 814bc22d31fabcaa3101101bb4622da538260652
Reviewed-Content-Hash: docs/architecture/architecture.md fc7cc5b13b64fb0f76de2eb05f939a8539eceecb
Reviewed-Content-Hash: docs/engine-reference/unity/PLUGINS.md 8d99063a63269d629be11e9e00e9f460188b689a
Reviewed-Content-Hash: docs/engine-reference/unity/VERSION.md 29723cb31681a39c4edca2510d49769ad266e819
Reviewed-Content-Hash: docs/engine-reference/unity/breaking-changes.md a0f69922939714905709abb0ffdc2509a7e4a98e
Reviewed-Content-Hash: docs/engine-reference/unity/current-best-practices.md 8bb18e0ad0fa1ab330da77f8b63da329832444cd
Reviewed-Content-Hash: docs/engine-reference/unity/deprecated-apis.md fcc5485e9db6073dffd3ee2f17a025bd0e49ffd6
Reviewed-Content-Hash: docs/engine-reference/unity/modules/animation.md e0601eeba72565d71e1dd2a8bb267eb966d10a5f
Reviewed-Content-Hash: docs/engine-reference/unity/modules/audio.md 56c04c15cb9a356b87a27df5d0fcf67f18db9302
Reviewed-Content-Hash: docs/engine-reference/unity/modules/input.md 1b64a2381167d17c1b796d4d5f93a7e1fcc651e6
Reviewed-Content-Hash: docs/engine-reference/unity/modules/navigation.md 484a867a2fe1acf86b2071f7950b10afaab8ffab
Reviewed-Content-Hash: docs/engine-reference/unity/modules/networking.md 180299400a45b256071a32de93915629d303da2b
Reviewed-Content-Hash: docs/engine-reference/unity/modules/physics.md 191e8d8d502363a99e80670942d251ef819a3976
Reviewed-Content-Hash: docs/engine-reference/unity/modules/rendering.md 292081320361b18c12715f7ed11dae2a38ba5f2d
Reviewed-Content-Hash: docs/engine-reference/unity/modules/ui.md 87de2ff30ce1c8d9499b6bcf084947201852e6ab
Reviewed-Content-Hash: project.yaml 0a326aa4306eaf92fce8d58acf9495309095d83e
Reviewed-Content-Hash: .claude/docs/technical-preferences.md c26df2a574325f22ae2ade7f1d06809c35d2cbeb
Reviewed-Content-Hash: .claude/skills/architecture-review/SKILL.md 1a9d28aadbcbd26b5fc6b8ed1c64d73ae82b7b73
Reviewed-Content-Hash: .claude/skills/architecture-review/references/1-load.md 9a65759ebdbb2cb1f9744775bfed18cb25e76347
Reviewed-Content-Hash: .claude/skills/architecture-review/references/2-extract-requirements.md d08ea0b0e9ab9760b0d8bfe0cd9115f0bc40f8e2
Reviewed-Content-Hash: .claude/skills/architecture-review/references/3-traceability.md bb7b6b65c5e5e5b8869fa692413aa831615dede7
Reviewed-Content-Hash: .claude/skills/architecture-review/references/4-conflicts.md 0d30f8173b23248a32523a365b606dd1ce9642ab
Reviewed-Content-Hash: .claude/skills/architecture-review/references/5-engine.md 318ecbc1361d430badaf5dd8ce4cfc6ab0dff0ca
Reviewed-Content-Hash: .claude/skills/architecture-review/references/6-document-coverage.md dcce78482ec2ce60048989b6f9d1f797dd0a9a0e
Reviewed-Content-Hash: .claude/skills/architecture-review/references/7-report.md a007fd66bdac87812dc272e4b14cd231174e979d
Reviewed-Content-Hash: .claude/skills/architecture-review/references/8-write.md bf6d8946fc7b9e19eda7f65503a5c8f593a0d2b1
Reviewed-Content-Hash: .claude/skills/architecture-review/references/9-handoff.md fd5f681f4c3517c6a2e388c6d78075d82dded22e

---

**Unabhängigkeit:** Das Review lief in einer eigenen Sitzung, nicht in der Sitzung, die die ADRs geschrieben hat.
Die Engine-Zweitmeinung kam von einem frisch gestarteten `unity-specialist`.

**Geprüft gegen:** die fünf GDDs in `design/gdd/` (Abschnitte Detailed Design, Formulas, Edge Cases,
Dependencies, Acceptance Criteria), `docs/architecture/architecture.md` (29 vorläufige TR-IDs, Abschnitt
Required ADRs), `docs/registry/architecture.yaml` (37 Einträge: 7 Zustände, 7 Schnittstellen, 13
API-Entscheidungen, 10 verbotene Muster) und die Engine-Referenz `docs/engine-reference/unity/`. Es gab keinen
früheren Bericht (`RECEIPT: NONE`); `docs/consistency-failures.md` existiert nicht.

### Traceability Matrix

Legende: ✅ von einer `Accepted`-ADR gedeckt · 🟡 von einer `Proposed`-ADR gedeckt · ⚠️ teilweise · ❌ keine ADR.
E1, E2 … = Entscheidung 1, 2 … in der ADR.

| TR-ID | GDD | Anforderung (kurz) | ADR | Status |
|---|---|---|---|---|
| TR-gegenstandsarten-001 | gegenstandsarten.md | Katalog aller Arten als Datenobjekte (Masse, Trageart, Eigenschaften) | ADR-0002 (E1, E2) | 🟡 |
| TR-gegenstandsarten-002 | gegenstandsarten.md | Übers Netz nur die Kennung, nie das Objekt | ADR-0002 (E3, E4), ADR-0001 (E1) | 🟡 |
| TR-gegenstandsarten-003 | gegenstandsarten.md | Bruchregel beim Host | ADR-0001 (E4, E5) | ⚠️ Teilweise: Reihenfolge „Liegt → Fällt“ beim Treffer auf ein liegendes Gericht fehlt (GDD Edge Case, Kriterium 15) |
| TR-gegenstandsarten-004 | gegenstandsarten.md | Gerichte kühlen mit `game_time` ab | ADR-0002 (E5) + ADR-0003 (E3) | 🟡 |
| TR-gegenstandsarten-005 | gegenstandsarten.md | Würfe bis 15 m/s durchfliegen keine dünnen Wände | ADR-0001 (E3) | 🟡 (Prüfung in Plan-Schritt „Gegenstandsarten“ offen) |
| TR-gegenstandsarten-006 | gegenstandsarten.md | Waren als schlanker Typ ohne Körper | ADR-0002 (E1) | 🟡 |
| TR-gegenstandsarten-007 *(neu)* | gegenstandsarten.md | Katalogprüfung sperrt Build und Host-Start | ADR-0002 (E7) | 🟡 |
| TR-interaktion-001 | interaktion.md | Gemeinsamer Strahl, Stationen melden Handlungen | — (ADR-0004 geplant) | ❌ |
| TR-interaktion-002 | interaktion.md | Jede Handlung als RPC-Wunsch an den Host | ADR-0001 (E1, nur RPC-Form) | ⚠️ Teilweise |
| TR-interaktion-003 | interaktion.md | Fortschritt am Bezugsobjekt, `dt` aus `game_time` | ADR-0003 (E3, nur `dt`) | ⚠️ Teilweise |
| TR-interaktion-004 | interaktion.md | Hinweis und Fortschritt beim Client vorhergesagt | — (ADR-0004 geplant) | ❌ |
| TR-interaktion-005 | interaktion.md | Ausrüstung am Körper | ADR-0002 (E2, nur Daten `EquipmentSlot`) | ⚠️ Teilweise: Zustand „Am Körper“ fehlt in ADR-0001 und im Register |
| TR-interaktion-006 *(neu)* | interaktion.md | Handlungen als Datenobjekte mit `action_data_valid` | — (ADR-0004 geplant) | ❌ |
| TR-saison-001 | saison-ablauf.md | Uhr gehört dem Host, alle sehen dieselbe Phase | ADR-0003 (E1–E3) | 🟡 |
| TR-saison-002 | saison-ablauf.md | `game_time`, steht in der Solo-Pause | ADR-0003 (E3, E4) | 🟡 (Widerspruch W4 zur Rücksprung-Regel) |
| TR-saison-003 | saison-ablauf.md | Meldungen, jedes System setzt sich selbst zurück | ADR-0003 (E5) | 🟡 |
| TR-saison-004 | saison-ablauf.md | Gemeinsamer Druck `season_pressure` | ADR-0003 (E2) | 🟡 |
| TR-saison-005 | saison-ablauf.md | Startaufstellung | ADR-0003 (E7) | 🟡 (Widerspruch W3 zum Entfernen) |
| TR-saison-006 | saison-ablauf.md | Beitritt nur zwischen den Saisons | ADR-0003 (E7) | 🟡 (Widerspruch W1 zur Schichtenregel) |
| TR-saison-007 | saison-ablauf.md | Abbruch ohne Bewertung, Kasse fällt zurück | ADR-0003 (E5, E7) | 🟡 |
| TR-saison-008 *(neu)* | saison-ablauf.md | Sitzungsende, neuer Host-Start ohne Reste | ADR-0003 (Uhr-Reset in `OnNetworkSpawn`) | ⚠️ Teilweise: Rückkehr ins Hauptmenü und Meldung → ADR-0006/0007 |
| TR-steam-lobby-001 | steam-lobby.md | Transport wählbar: Steam oder IP | — (ADR-0006 geplant) | ❌ |
| TR-steam-lobby-002 | steam-lobby.md | Steam-Anbindung, App-ID aus Einstellung | — (ADR-0006 geplant) | ❌ |
| TR-steam-lobby-003 | steam-lobby.md | Eine Freigabe für beide Wege | ADR-0003 (E7, `JoinRules`) | ⚠️ Teilweise: Steam-Weg offen |
| TR-steam-lobby-004 | steam-lobby.md | 20 s Zeitgrenze, Steam-Namen | — (ADR-0006 geplant) | ❌ |
| TR-gaeste-001 | gaeste.md | Gäste vom Host gesteuert, Clients bekommen Position, Zustand … | ADR-0001 (Grundsatz) | ⚠️ Teilweise: Übertragung je Gast → ADR-0005 |
| TR-gaeste-002 | gaeste.md | Wegfindung beim Host (NavMesh) | — (ADR-0005 geplant, Paket-OK nötig) | ❌ |
| TR-gaeste-003 | gaeste.md | Ankunft aus Phase, Druck und `n` | ADR-0003 (Phase, Druck) | ⚠️ Teilweise: Quelle von `n` offen |
| TR-gaeste-004 | gaeste.md | Reaktion auf Kälte, Lärm, Chaos | ADR-0001 (E4, `ImpactEvents`) | 🟡 (Treffer auf Gäste: Prüfung in Plan-Schritt 5) |
| TR-gaeste-005 | gaeste.md | Schnittstelle „Bedürfnis erfüllt“ | — (ADR-0005 geplant) | ❌ |
| TR-gaeste-006 | gaeste.md | Gästetypen und Bedürfnisse als Datenobjekte | ADR-0002 (E1, E7) | 🟡 |
| TR-gaeste-007 | gaeste.md | Gäste eingefroren außerhalb der Spielphasen | ADR-0003 (E5) | 🟡 |
| TR-gaeste-008 *(neu)* | gaeste.md | Leistungsbudget ≤ 1,0 ms Host, ≤ 0,2 ms Client, 0 B GC | — (kein Budget im Register) | ❌ |

### Traceability Summary

Gesamt: 33 Anforderungen (29 aus der Baseline in `architecture.md`, 4 neu in diesem Review)

| Status | Anzahl |
|---|---|
| ✅ Gedeckt (Accepted) | 0 |
| 🟡 Gedeckt (Proposed) | 16 |
| ⚠️ Teilweise | 8 |
| ❌ Lücke | 9 |

Alle Deckung ruht auf `Proposed`-ADRs. Deshalb ist kein Ergebnis besser als CONCERNS möglich. Ausweg: die drei
ADRs nach Einarbeitung der Befunde vom Entwickler auf `Accepted` setzen lassen.

**Foundation-Schicht** (Gegenstandsarten, 7 Anforderungen): 6 🟡, 1 ⚠️ (TR-gegenstandsarten-003), keine Lücke.
**Core-Schicht** (Interaktion, Saison, Steam-Lobby): Saison 7 🟡 + 1 ⚠️; Interaktion und Steam-Lobby warten auf
ADR-0004 und ADR-0006. **Feature** (Gäste): 3 🟡, 3 ⚠️, 3 ❌.

Bei `workflow: standard` sind nur kritische ADRs (Foundation) Pflicht
(`.claude/docs/workflow-modes.md`); die Lücken in Core und Feature sind in `architecture.md` mit festem
Zeitpunkt eingeplant (ADR-0004 vor Plan-Schritt 4, ADR-0005 vor Schritt 5, ADR-0006 vor Schritt 6). Sie sind
deshalb Hinweise, kein FAIL.

### Neue TR-IDs

Die 29 vorläufigen IDs aus `architecture.md` werden unverändert fest (das Register war leer, keine
Umbenennung). Neu:

- **TR-gegenstandsarten-007:** Katalogprüfung sperrt Build und Host-Start (Edge Cases Katalog, Kriterien 2, 5, 33).
  Stand in der Baseline nur indirekt unter -001.
- **TR-interaktion-006:** Handlungen als Datenobjekte mit `action_data_valid` (Regel 3, Formel 4).
- **TR-saison-008:** Sitzungsende und sauberer Neustart (Regel 8, Edge Case „Host verlässt“, Kriterium 45).
- **TR-gaeste-008:** Leistungsbudget der Gäste (Kriterium 49). Es ist die einzige Zahl für Rechenzeit in allen
  GDDs; `performance_budgets` im Register ist leer.

### Coverage Gaps (no ADR exists)

| TR-ID | System | Anforderung | Vorgeschlagene ADR | Bereich | Engine-Risiko |
|---|---|---|---|---|---|
| TR-interaktion-001 | Interaktion | Gemeinsamer Strahl | ADR-0004 Interaktion übers Netz | Core | NIEDRIG |
| TR-interaktion-004 | Interaktion | Vorhersage von Hinweis und Fortschritt | ADR-0004 | Networking | HOCH (NGO 2.13) |
| TR-interaktion-006 | Interaktion | Handlungen als Datenobjekte | ADR-0004 (Muster aus ADR-0002 übernehmen) | Core | NIEDRIG |
| TR-steam-lobby-001 | Steam-Lobby | Transport Steam oder IP | ADR-0006 Steam-Transport | Networking | HOCH (Paket unbekannt, QQ-03) |
| TR-steam-lobby-002 | Steam-Lobby | App-ID aus Einstellung | ADR-0006 | Networking | MITTEL |
| TR-steam-lobby-004 | Steam-Lobby | 20 s Zeitgrenze, Steam-Namen | ADR-0006 | Networking | MITTEL |
| TR-gaeste-002 | Gäste | Wegfindung beim Host | ADR-0005 Gäste-Wegfindung im Netz | Navigation | HOCH (Paket nicht installiert) |
| TR-gaeste-005 | Gäste | „Bedürfnis erfüllt“ | ADR-0005 | Core | NIEDRIG |
| TR-gaeste-008 | Gäste | Leistungsbudget | ADR-0005 oder eigener Budget-Eintrag (QQ-01) | Performance | MITTEL |

Teilweise gedeckt (Rest gehört in die genannte ADR):

| TR-ID | Was gedeckt ist | Was fehlt | Wohin |
|---|---|---|---|
| TR-gegenstandsarten-003 | Bruchregel, Messung, Despawn (ADR-0001) | Ein liegendes Gericht, das getroffen wird, muss im selben Kontakt **vor** der Bruchprüfung auf „Fällt“ wechseln, sonst bleibt es heil (GDD Edge Case, Kriterium 15) | ADR-0001, ein Satz in Entscheidung 4 |
| TR-interaktion-002 | RPC-Form und „Host prüft“ (ADR-0001) | Wem der Fortschritt gehört, Log `[Interact]` | ADR-0004 |
| TR-interaktion-003 | `dt` aus `game_time` (ADR-0003) | Fortschritt am Bezugsobjekt, Teamzuschlag | ADR-0004 |
| TR-interaktion-005 | Datenfeld `EquipmentSlot` (ADR-0002) | Zustand „Am Körper“ im Netz; ADR-0001 kennt nur Liegt, Getragen, Fällt (auch Register `item_motion_state`) | ADR-0004, Register-Eintrag erweitern |
| TR-saison-008 | Uhr beginnt bei jedem Host-Start neu (ADR-0003) | Rückkehr ins Hauptmenü, Meldung beim Client | ADR-0006 / ADR-0007 |
| TR-steam-lobby-003 | `JoinRules.Decide` im `ConnectionApprovalCallback` (ADR-0003) | Steam-Weg | ADR-0006 |
| TR-gaeste-001 | Host entscheidet (ADR-0001) | Übertragung je Gast (Position, Zustand, Bedürfnis, Geduld, Stimmung) | ADR-0005 |
| TR-gaeste-003 | Phase und Druck (ADR-0003) | Woher die Spielerzahl `n` beim Saisonstart kommt | ADR-0005 |

### Cross-ADR Conflicts

Keine 🔴 Konflikte: Nichts macht zwei ADRs zusammen unbaubar. Sieben ⚠️ Unstimmigkeiten, die vor `Accepted`
behoben werden sollten (jeweils ein bis drei Sätze in der genannten ADR).

**W1 — ADR-0003 vs. Schichtenregel in `architecture.md`** (Typ: Dependency)
- ADR-0003 E7: `PlayerSpawnApproval` (Netzwerk-Sitzung, **Foundation**) ruft `JoinRules.Decide` (Core) auf und
  braucht dafür die Phase aus `SeasonClock` (Core).
- `architecture.md`: „Eine Schicht nutzt nur Schichten darunter.“ Der Compiler merkt es nicht (eine Assembly),
  also nur das Code-Review.
- Auswirkung: Foundation hängt am Saison-Code; ADR-0006 (Steam-Transport, Foundation) erbt die Abhängigkeit.
- Lösungen: (1) `JoinRules` und ein Wert „Saison läuft“ liegen in Foundation (`Net/`), `SeasonClock` setzt ihn
  von oben; (2) `PlayerSpawnApproval` bekommt eine Prüffunktion übergeben (Core trägt sie beim Start ein).
  Empfehlung: (1), ist einfacher zu erklären.

**W2 — ADR-0001 vs. ADR-0003: Abmelden vom Meldedienst** (Typ: Pattern)
- ADR-0001: Abonnenten melden sich in `OnDestroy` oder `OnNetworkDespawn` ab.
- ADR-0003: Zuhörer melden sich in `OnDisable` ab, „übernimmt das Muster aus ADR-0001“.
- Auswirkung: Wer in `OnNetworkSpawn` anmeldet und in `OnDisable` abmeldet, ist nach Aus- und Einschalten
  still abgemeldet; wer nach ADR-0001 erst in `OnDestroy` abmeldet, bekommt Meldungen auch ausgeschaltet.
- Lösung: eine Regel für beide Meldedienste (Engine-Empfehlung 6): NetworkBehaviours melden sich in
  `OnNetworkSpawn` (`IsServer`) an und in `OnNetworkDespawn` ab, reine MonoBehaviours in `OnEnable`/`OnDisable`;
  dazu ein Register-Eintrag.

**W3 — ADR-0003 vs. ADR-0001: Entfernen bei der Startaufstellung** (Typ: Integration)
- ADR-0001: Eigener Code, der einen Körper entfernt, stellt vorher auf „Discrete“ (sonst Warnung, wenn NGO ihn
  beim Despawn kinematisch macht).
- ADR-0003 E7: Der Host entfernt alle Gegenstände mit `Despawn(true)`, ohne diese Regel.
- Lösung: ADR-0002 bekommt zur Host-Funktion „Erscheinen“ die Gegenstück-Funktion „Entfernen“
  (Discrete, dann Despawn); Bruch und Startaufstellung nutzen beide sie.

**W4 — ADR-0003 in sich und gegenüber GDD Saison-Ablauf Formel 3a: Rücksprung beim Client** (Typ: State)
- ADR-0003 E3 klemmt Rücksprünge der NGO-Zeit nur bis 0,5 s; größere übernimmt der Client. Dann läuft
  `game_time` beim Client rückwärts, `dt` wird einmal negativ.
- Dieselbe ADR schreibt „So ist `dt` nie negativ“, und die GDD-Formel 3a verlangt „läuft nie rückwärts“.
- Auswirkung: nur Anzeige und Vorhersage beim Client (der Host zählt), aber eine Regel, auf die sich
  ADR-0004 verlassen wird, stimmt nicht.
- Lösung (vom `unity-specialist` im Quelltext belegt, siehe Engine-Befund 2): Den Fall „großer Rücksprung vor
  dem ersten Zeitabgleich“ gibt es nach `OnNetworkSpawn` nicht. Also immer `max` ohne Ausnahme, den gemerkten
  Wert nur in `OnNetworkSpawn` zurücksetzen, große Sprünge nur loggen.

**W5 — ADR-0003: „alle verbundenen Spieler haben Weiter gedrückt“** (Typ: Integration)
- GDD Regel 8: weiter zu Upgrades, wenn alle verbundenen Spieler „Weiter“ gedrückt haben; wer in der
  Bewertung beitritt, zählt ab dann mit.
- `SeasonDirector` kennt Bestätigungen (`Confirm`) und Abgänge (`ClientLeft`), aber nicht, wer verbunden ist
  (kein `ClientJoined`, keine Liste in `Step`).
- Lösung: `ClientJoined(ulong)` ergänzen oder die verbundenen Kennungen an `Step` übergeben.

**W6 — ADR-0002: wer prüft die Gäste-Daten beim Host-Start?** (Typ: Dependency)
- ADR-0002 E7: Die Gäste-Prüfung liegt im Gäste-Code (Feature) und läuft auch beim Host-Start.
- Wer sie aufruft, steht nicht da. Ruft der Katalog-Dienst (Foundation) sie auf, zeigt der Aufruf nach oben.
- Lösung: Die Gäste melden ihr Prüfergebnis selbst an eine Sammelstelle in Foundation, oder der Host-Start
  (Menü, Presentation) ruft beide Prüfungen auf.

**W7 — Register `item_motion_state` vs. GDD Gegenstandsarten** (Typ: State)
- Register und ADR-0001 kennen drei Zustände; das GDD hat zusätzlich „Am Körper“ (fällt nie, auch nicht beim
  Sturz des Trägers, Kriterium 34).
- Lösung: in ADR-0004 festlegen und den Register-Eintrag dann erweitern. Bis dahin gilt: Ausrüstung am Körper
  hat kein `ImpactReporter`-Verhalten „Fällt“.

### ADR Dependency Order

`adr-dep-graph.sh`: 3 ADRs, Kanten 0002 → 0001, 0003 → 0001, 0003 → 0002, kein Kreis, keine fehlenden
Abschnitte.

1. Foundation: **ADR-0001** Netz-Autorität und Physik im Netz
2. Foundation: **ADR-0002** Gegenstandsarten-Katalog (braucht ADR-0001)
3. Core: **ADR-0003** Saison-Uhr, Spielzeit und Meldungen (braucht ADR-0001 und ADR-0002)

⚠️ ADR-0002 hängt an ADR-0001, das noch `Proposed` ist. ⚠️ ADR-0003 hängt an ADR-0001 und ADR-0002, beide
`Proposed`. Reihenfolge beim Annehmen also 0001 → 0002 → 0003.

Das Abkühlen (ADR-0002) braucht `game_time` aus ADR-0003. Das ist kein Kreis: `DishRules` bekommt die Spielzeit
als Parameter, die Ordering Note in ADR-0002 regelt die Bau-Reihenfolge.

### GDD Revision Flags

Keine. Kein GDD trifft eine Annahme, der das geprüfte Engine-Verhalten widerspricht: Die Lücken aus Q2 und Q7
(unten) liegen in den ADRs, nicht in den GDDs. Das Systemverzeichnis bleibt unverändert.

**Hinweis für ein künftiges GDD (keine Status-Änderung):** `systems-index.md` beschreibt „Hüttenkasse und
Upgrades“ mit „Upgrades ändern nur Werte in den Datenobjekten der anderen Systeme“. ADR-0002 verbietet das
Schreiben von Datenobjekten zur Laufzeit (Register `runtime_data_object_write`), und das GDD Gegenstandsarten
sagt für den Katalog schon „Upgrades ändern keine Werte“. Das GDD Hüttenkasse muss Upgrades als
Laufzeit-Zuschläge an den Systemen beschreiben, nicht als Änderung der Assets.

### Engine Compatibility Issues

**Engine:** Unity 6000.6.6f1, NGO 2.13.3 · ADRs mit Engine-Compatibility-Abschnitt: 3 / 3

- **Versionen:** alle drei ADRs nennen dieselbe Version. Keine veraltete Angabe.
- **Veraltete APIs:** keine im Einsatz. `InstanceID`, `Resources.Load`, `OnClientConnectedCallback`,
  `RequireOwnership` stehen nur als „nicht verwenden“ oder Migration. `Rigidbody.linearVelocity` ist auf
  Loslassen und Aufheben begrenzt (ADR-0001).
- **Post-Cutoff-APIs:** keine widersprüchlichen Annahmen zwischen den ADRs. ADR-0002 und ADR-0003 nutzen
  dieselbe Regel für Startwerte (`OnNetworkSpawn`), ADR-0003 dasselbe RPC-Muster wie ADR-0001.
- **Veraltete Referenz:** `docs/engine-reference/unity/modules/networking.md` Zeile 400 nennt die Signatur von
  `OnConnectionEvent` noch „NOT SOURCEABLE“; ADR-0001 hat sie aus dem Quelltext gelesen. Die Referenz sollte
  nachgezogen werden, nicht die ADR.

### Engine Specialist Findings

`unity-specialist`, 2026-11-09, unabhängige Zweitmeinung; Belege im NGO-2.13.3-Quelltext, die mit Zeilennummer
genannt sind, wurden stichprobenartig nachgelesen (`ConnectionApprovedMessage.cs` 294–296,
`NetworkTimeSystem.cs` 155, `NetworkTransform.cs` 4254, `NetworkObject.cs` 1827–1832,
`NetworkRigidBodyBase.cs` 46).

**Vor `Accepted` beheben (vom Gutachter als Blocker eingestuft):**

1. **ADR-0001, Bruch eines liegenden Gerichts (Q2).** Die Bruchregel verlangt „Fällt“. Eine liegende Tasse, die
   eine Kiste trifft, ist im Callback noch „Liegt“, also zerbricht sie nie. GDD Gegenstandsarten, Kriterium 15
   wäre unerfüllbar. In welcher Reihenfolge die beiden Körper ihren Callback bekommen, ist nicht festgelegt.
   Korrektur: Der `ImpactReporter` der Tasse erkennt selbst, dass ein bewegter, nicht kinematischer Körper sie
   trifft, wechselt auf „Fällt“ und prüft erst danach im selben Callback die Bruchregel.
2. **ADR-0003, Rücksprung der Spielzeit beim Client (Q7, = W4).** Der Fall „großer Rücksprung vor dem ersten
   Zeitabgleich“ kommt nach `OnNetworkSpawn` nicht vor: Die Client-Zeit läuft erst nach dem Verbinden
   (`NetworkTimeSystem.cs` 155), und `ConnectionApproved` setzt sie zurück, bevor Objekte erscheinen
   (`ConnectionApprovedMessage.cs` 294–296). Korrektur: immer `max` ohne Ausnahme, den gemerkten Wert nur in
   `OnNetworkSpawn` zurücksetzen, große Sprünge nur loggen. Damit stimmt „läuft nie rückwärts“ (Formel 3a).

**Empfehlungen:**

3. **ADR-0003, Pause hält keine Eingaben an.** Mausdrehung läuft weiter, und die eigenen RPCs des Hosts laufen
   sofort lokal: Aufheben und Werfen gingen in der Pause. Die Gameplay-Action-Maps müssen bei offenem Menü aus.
4. **ADR-0001, `ContinuousSpeculative` als Dauerwert prüfen (Q1).** Kinematische Körper unterstützen nur
   „Speculative“ (Trainingswissen); NGO setzt beim Despawn selbst `isKinematic`
   (`AutoSetKinematicOnDespawn`, Standard an). Ein Dauerwert spart die Abgleichstelle und das Umschalten. Mit
   15 m/s gegen eine 0,2-m-Wand testen; die bestehende Verification 1 deckt beide Varianten.
5. **ADR-0002, Build-Abbruch über `-executeMethod` (Q4).** Ob `BuildFailedException` aus einem Build-Hook den
   Batchmode-Build in 6.6 abbricht, ist nicht belegt. Hauptweg: eigenes Build-Skript mit
   `BuildPipeline.BuildPlayer`, Prüfung vorher und Auswertung von `report.summary.result`; passt zu den
   bestehenden Batchmode-Skripten. Die ADR nennt das bisher nur als Ausweg.
6. **ADR-0001 + ADR-0003, eine Abmelde-Regel (Q9, = W2).** NetworkBehaviours: in `OnNetworkSpawn` anmelden
   (nur `IsServer`), in `OnNetworkDespawn` abmelden; auch ein direktes `Destroy` löst das Despawn aus
   (`NetworkObject.cs` 1827–1832). Reine MonoBehaviours: `OnEnable`/`OnDisable`. `OnDestroy` in
   NetworkBehaviours meiden: ein `private void OnDestroy()` verdeckt `NetworkBehaviour.OnDestroy`, und
   NetworkVariables werden nicht freigegeben.
7. **ADR-0003, Teleport (Q6).** `NetworkTransform.Teleport(pos, rot, transform.localScale)` statt
   `SetState(…, teleportDisabled: false)`: Beide landen in derselben Methode, aber `Teleport` wirft auf der
   falschen Seite, statt still nichts zu tun.

**Hinweise:**

8. ADR-0003, Constraint „Der Host kann einen Spieler nicht selbst versetzen“ stimmt so nicht: `SetState` beim
   Host leitet per Owner-RPC weiter. Die eigene RPC bleibt trotzdem nötig, weil ein aktiver
   `CharacterController` die Position überschreibt.
9. ADR-0003, der NOT-SOURCEABLE-Punkt „Interpolation mit skalierter Zeit“ ist jetzt belegt
   (`NetworkTransform.cs` 4254, `RealTimeProvider.DeltaTime`). Die Begründung „Pause nur ohne Clients“ steht
   damit fest.
10. ADR-0001, `ImpactEvents.Raise` mit try/catch je Zuhörer braucht `GetInvocationList()` und allokiert je
    Aufprall. Vertretbar, oder Zuhörer in einer eigenen Liste halten. Den `Collision`-Parameter nie
    speichern (Unity verwendet ihn wieder).
11. Bestätigt: `NetworkVariable<SeasonSnapshot>` funktioniert ohne `[GenerateSerializationForType]`, solange
    der Wert ein Feld des NetworkBehaviours ist (`NetworkBehaviourILPP.cs` 1406–1429); `FixedString32Bytes`
    fasst 29 Byte; der Kreisverweis Art → Prefab → Art ist unproblematisch.
12. Q3, Treffer auf Gäste: `OnCollisionEnter` feuert, sobald ein Partner dynamisch ist. Durchfliegen ist bei
    0,3 m pro Schritt gegen einen etwa 0,5 m dicken Gast unwahrscheinlich. Empfehlung für ADR-0005: Gäste mit
    kinematischem Rigidbody.

Nicht geprüft: Signatur von `OnConnectionEvent` (vom Gutachter nicht nachgelesen; ADR-0001 hat sie gelesen).

### Architecture Document Coverage

- **Systeme in der Schichtenkarte:** alle 26 Systeme aus MVP und Vertical Slice (`systems-index.md` Zeilen
  1–26) stehen in einer Schicht. Alpha und Full Vision (Zeilen 27–34) fehlen, das ist bei einer Karte für den
  Vertical Slice richtig. Keine verwaisten Einträge.
- **Schichten passen zum Systemverzeichnis:** Foundation = Netzwerk-Sitzung und Gegenstandsarten wie in der
  Dependency Map.
- **Datenfluss, API-Grenzen, Modul-Besitz:** bei `workflow: standard` nicht ausgeführt, die ADRs und das
  Register tragen sie. Erlaubt.
- **Veraltete Stellen in `architecture.md`:** „ADRs Referenced: keine“, „ADR Audit: Es gibt keine ADRs“,
  „0 von 29 gedeckt“; QQ-05 und QQ-06 sind durch ADR-0003 und ADR-0001 beantwortet. Nachziehen, sobald die
  ADRs `Accepted` sind (gehört nicht zu den Dateien dieses Reviews).
- **Schichtenregel:** siehe W1 und W6.

---

### Verdict: CONCERNS

- Alle Deckung ruht auf `Proposed`-ADRs (0 von 33 durch eine `Accepted`-ADR gedeckt). Das allein begrenzt das
  Ergebnis auf CONCERNS.
- Keine 🔴 Konflikte zwischen den ADRs, die Engine-Angaben sind einheitlich, die Abhängigkeiten kreisfrei.
- Die Foundation-Anforderungen sind gedeckt, eine davon nur teilweise (TR-gegenstandsarten-003).
- Zwei Befunde in einzelnen ADRs (Gutachter-Befunde 1 und 2) müssen vor `Accepted` behoben werden. Beide sind
  je ein bis drei Sätze.
- Die 9 Lücken liegen in Core und Feature und sind in `architecture.md` mit ADR und Plan-Schritt eingeplant;
  bei `workflow: standard` sind sie nicht blockierend.

### Blocking Issues

Keine im Sinne eines FAIL. **Vor `Accepted` beheben:** Gutachter-Befunde 1 (ADR-0001) und 2 (ADR-0003). Danach
die übrigen Punkte W1–W7 und Empfehlungen 3–7 einarbeiten oder bewusst verwerfen.

### Required ADRs

1. Die drei ADRs überarbeiten und annehmen, in der Reihenfolge **ADR-0001 → ADR-0002 → ADR-0003**
   (`/architecture-decision accept ADR-NNNN`; annehmen darf nur der Entwickler).
2. **ADR-0004 Interaktion übers Netz** (Core, vor Plan-Schritt 4): TR-interaktion-001 bis -006; legt auch den
   Zustand „Am Körper“ fest (W7).
3. **ADR-0005 Gäste-Wegfindung im Netz** (Feature, vor Plan-Schritt 5): TR-gaeste-001, -002, -003, -005,
   -008; Paket AI Navigation nur nach OK; ersten Eintrag in `performance_budgets` anlegen.
4. **ADR-0006 Steam-Transport mit IP-Ausweg** (Core/Foundation, vor Plan-Schritt 6): TR-steam-lobby-001 bis
   -004, TR-saison-008 (Teil Sitzungsende).
5. Später: ADR-0007 UI und Szenenablauf (TR-saison-008, Teil Hauptmenü), ADR-0008 Sprachchat, ADR-0009
   Ragdoll.
