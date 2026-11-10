# Architecture Traceability Index
Last Updated: 2026-11-09
Engine: Unity 6000.6.6f1 (URP, PhysX), Netcode for GameObjects 2.13.3

Geschrieben von `/architecture-review` (Bericht: `docs/architecture/architecture-review-2026-11-09.md`).
Feste TR-IDs stehen in `docs/architecture/tr-registry.yaml`. Bei einem neuen Review wird diese Datei ersetzt.

## Coverage Summary
- Total requirements: 33
- Covered (Accepted): 0 (0 %)
- Covered (Proposed): 16 (48 %), kein Pass-Zustand, bis die ADRs `Accepted` sind
- Partial: 8
- Gaps: 9

Legende: ✅ von einer `Accepted`-ADR gedeckt · 🟡 von einer `Proposed`-ADR gedeckt · ⚠️ teilweise · ❌ keine ADR.
E1, E2 … = Entscheidung 1, 2 … in der ADR.

## Full Matrix

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

## Known Gaps

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

## Superseded Requirements

Keine. Alle drei ADRs und alle fünf GDDs stammen vom 2026-11-08/09; kein GDD wurde nach seiner ADR inhaltlich
geändert. Die vier neuen IDs (TR-gegenstandsarten-007, TR-interaktion-006, TR-saison-008, TR-gaeste-008)
ergänzen die Baseline aus `architecture.md`, sie ersetzen nichts.
