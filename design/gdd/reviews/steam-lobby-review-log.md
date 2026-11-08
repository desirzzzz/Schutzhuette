# Review-Protokoll: Steam-Lobby und Einladen

## Review — 2026-11-08 — Verdict: APPROVED
Review mode: lean · Workflow: standard
Scope signal: M
Specialists: keine (Lean-Modus)
Blocking items: 0 | Recommended: 1 | Nice-to-have: 5
Summary: Klares, gut prüfbares Dokument. Die 3 Formeln und ihre Beispiele stimmen (20 s = 10 + 7 + 10 × 0,3; typisch 7 s; Begrenzung 11–60 s; Plätze 4 / 3 bei leerem Punkt; „Sepp der Hütten…“, 6 Elemente + „…“ = 57 Byte, 15 × „山“ + „…“ = 48 Byte). Die Code-Bezüge sind nachgeprüft: `SlotCount` zählt leere Einträge mit, `FindFreeSlot` überspringt sie; Versionsdaten höchstens 64 Byte; `MismatchReason`-Texte; „Verbindung getrennt.“ und Port 7777 in `ConnectionHud`; `ClientConnectionBufferTimeout` 10, `ConnectTimeoutMS` 1000, `MaxConnectAttempts` 60 in `Prototype.unity`. Die Beitrittssperre und ihr Text stimmen wörtlich mit `saison-ablauf.md` überein, die Zeitgrenze rechnet richtigerweise mit Echtzeit statt `game_time`. Die 38 Abnahmekriterien sind einzeln prüfbar, Steam steckt hinter einer Schnittstelle mit Fake. Die offene Wahl von Steam-Paket und Transport ist bewusst an die ADR gegeben.
Prior verdict resolved: First review
Findings:
- [RECOMMENDED] Systemverzeichnis: Zeile 10 nennt unter „Depends On“ nur die Netzwerk-Sitzung; das GDD führt Saison-Ablauf als weiche Abhängigkeit (Beitrittssperre).
- [NICE-TO-HAVE] Formulas (1.), Output Range: Die Obergrenze 60 s wird mit dem Aufgeben des Unity Transport (60 × 1 s) begründet; das gilt für den IP-Weg, nicht für den Steam-Transport. Als Wert harmlos, die Begründung gehört zur ADR.
- [NICE-TO-HAVE] Formulas (2.): Gezählt wird nur in den ersten `max_players` Einträgen. Ist dort einer leer, gibt es weniger Plätze, auch wenn weiter hinten Punkte gesetzt sind (AC 4: 7 gesetzte Punkte → 3 Plätze). Gewollt und wie heute, aber als Konfigurationsfalle im Editor-Hinweis erwähnenswert.
- [NICE-TO-HAVE] Formulas (3.): Unitys Laufzeit fasst ZWJ-Folgen (👨‍👩‍👧) nicht sicher zu einem Textelement zusammen; die Regel verlangt eine eigene Zerlegung. Beim Bau einplanen.
- [NICE-TO-HAVE] Core Rules (Regel 5): Die Prüfungen stehen in der Reihenfolge Version, Platz, Saison; `saison-ablauf.md` AC 26 legt Version → Saison → voll fest. Nur die Aufzählung, aber besser angleichen.
- [NICE-TO-HAVE] Edge Cases (Steam selbst): „Steam-Verbindung reißt ab“ beschreibt den Abriss beim Host; der Abriss bei einem einzelnen Client (AC 37) steht nur in den Kriterien.
- [ADVISORY] Player Fantasy fehlt; bei `standard` nur empfohlen (Overview und Game Feel tragen das Gefühl „Hey, komm rein“).
Reviewed-Content-Hash: design/gdd/steam-lobby.md 3cfbdf6af122c35c892f3567a277d76b5a607fed
Reviewed-Content-Hash: design/registry/entities.yaml 8d21069e41a509283078b7d040850c88af96336b
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/saison-ablauf.md 91dc3b79ad2085daa8f5b2fc57cfae37287989ed
Reviewed-Content-Hash: design/game-brief.md c532ebc1df2645bf9cfaa33cef546086aa209ad3
Reviewed-Content-Hash: Assets/_Project/Scripts/Net/PlayerSpawnApproval.cs 334119bf6873ac2a063134352ababd6c8f286467
Reviewed-Content-Hash: Assets/_Project/Scripts/Net/GameVersion.cs d8272e7d75cfd2d10ab6adbabacab5fbb0bbb8f5
Reviewed-Content-Hash: Assets/_Project/Scripts/Net/ConnectionHud.cs dd6b691b25ef0d7e0763da699e410a37672eb0dd
Reviewed-Content-Hash: Assets/_Project/Scenes/Prototype.unity 2113bffeb9762903560742fac6a304c920dd5e45
