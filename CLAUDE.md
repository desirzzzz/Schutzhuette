# Schutzhütte – Projektkontext für Claude Code

## Worum es geht

Schutzhütte ist ein Koop-Spiel für 1–4 Spieler (Unity, Steam). Die Spieler führen eine Alpenhütte durch eine Sturmnacht: kochen, heizen, Gäste versorgen, Vorräte den Berg hinaufschleppen. Je später es wird, desto chaotischer wird es. Höhepunkt jeder Runde ist ein Nachteinsatz im Schneesturm.

Das vollständige Konzept steht in `docs/KONZEPT.md`. Lies es, bevor du an Gameplay-Systemen arbeitest. Die einseitige Kurzfassung für das Framework steht in `design/game-brief.md`.

Die drei Säulen, an denen jede Entscheidung gemessen wird:
1. **Gemütlichkeit** – warme Stube, Ofen, zufriedene Gäste.
2. **Physik-Chaos** – alles ist schwer, wackelig und kann umfallen oder wegrutschen.
3. **Teamwork unter Druck** – vieles geht nur zu zweit.

## Über mich (den Entwickler)

- Ich habe **etwas Unity-Erfahrung** (Tutorials, kleine Projekte), aber noch kein Multiplayer-Spiel fertiggestellt.
- Erkläre kurz, *warum* du etwas so löst, besonders bei Netcode. Ich will es verstehen, nicht nur kopieren.
- Arbeite in kleinen Schritten: ein Feature pro Schritt, das ich im Editor testen kann.
- Antworte auf Deutsch. Code (Klassen, Methoden, Variablen) auf Englisch, Kommentare auf Deutsch.

## Tech-Stack

| Bereich | Entscheidung |
| --- | --- |
| Engine | Unity 6 (6000.6.6f1), Universal Render Pipeline (URP). Wechsel auf 6.7 LTS, sobald sie erscheint |
| Netzwerk | Netcode for GameObjects (NGO), Host-Client-Modell |
| Lokaler Multiplayer-Test | Multiplayer Play Mode (Unity-Paket) |
| Steam | Facepunch.Steamworks (später, ab Vertical Slice) |
| Sprachchat | Vivox oder Dissonance (später) |
| Input | Neues Input System |
| Graubox | ProBuilder |
| Versionskontrolle | Git mit Git LFS |

## Architektur-Regeln

1. **Multiplayer ab Tag 1.** Jedes Gameplay-Feature wird von Anfang an netzwerkfähig gebaut. Nichts erst für Singleplayer schreiben und später umbauen.
2. **Der Host entscheidet.** Spielzustand (Gegenstände, Ofen, Gäste, Wetter) ändert nur der Server/Host. Clients schicken Wünsche per RPC (`[Rpc(SendTo.Server)]`), der Host prüft und führt aus.
3. **Physik im Netz:**
   - Ruhende Gegenstände werden eingefroren (Rigidbody schlafen lassen oder kinematisch).
   - Getragene Gegenstände folgen dem Träger (Haltepunkt), nicht der freien Physik.
   - Nur fallende oder rutschende Dinge simuliert der Host voll; Clients bekommen die Position über `NetworkTransform`/`NetworkRigidbody`.
4. **Daten in ScriptableObjects:** Gerichte, Gästetypen, Ereignisse, Upgrades sind Datenobjekte, keine fest verdrahteten Werte im Code.
5. **Kleine, klare Komponenten** statt großer Manager-Klassen. Singletons nur für echte globale Dienste (z. B. `GameSessionManager`).
6. **Solo = Host ohne Clients.** Solo braucht keinen eigenen Code-Pfad.

## Ordnerstruktur

```
Assets/_Project/
  Scripts/
    Core/          Session, Spielphasen, Zeit
    Player/        Bewegung, Kamera, Interaktion
    Carrying/      Aufheben, Tragen, Zu-zweit-Tragen
    Hut/           Ofen, Lager, Betten, Seilbahn
    Guests/        Gäste und ihre Bedürfnisse
    Weather/       Sturm, Ereignisse
    Net/           Netzwerk-Hilfsklassen, Verbindung, Lobby
    UI/
  Data/            ScriptableObjects (Gerichte, Gäste, Ereignisse)
  Prefabs/
  Scenes/
  Art/
Assets/Tests/      EditMode/ und PlayMode/ (nur hier kompiliert Unity Tests)
docs/              Konzept und Notizen (KONZEPT.md, IDEEN.md) plus Framework-Referenzen
design/            game-brief.md (Framework)
production/        Sitzungsstand des Frameworks (session-state/ ist gitignored)
```

## Wichtig bei der Arbeit mit Unity

- Du kannst den Unity-Editor nicht bedienen und nicht auf Play drücken. Wenn ein Schritt im Editor nötig ist (Komponente hinzufügen, Prefab anlegen, Referenz zuweisen, Paket installieren), schreib mir eine **kurze, nummerierte Anleitung**, was ich wo klicken muss.
- **Bearbeite keine `.unity`-, `.prefab`- oder `.asset`-Dateien direkt** (YAML). Das zerstört leicht Szenen. Beschreib mir stattdessen die Änderung im Editor.
- **Lege keine `.meta`-Dateien an und lösche keine.** Unity erzeugt sie selbst.
- Neue Pakete nur nach Rückfrage hinzufügen; sag mir, wie ich sie über den Package Manager installiere.
- Nach jedem Schritt: Sag mir, wie ich es teste (Solo und mit zwei Spielern in Multiplayer Play Mode).
- **Domain Reload ist aus** (Voreinstellung neuer Unity-6.6-Projekte, Project Settings → Editor → Enter Play Mode Settings). Statische Felder behalten beim nächsten Play ihren alten Wert. Jedes statische Feld (z. B. eine Singleton-Instanz) wird deshalb in einer Methode mit `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` zurückgesetzt.
- Leere Ordner unter `Assets/` bekommen eine `.gitkeep`, sonst verschwinden sie beim Klonen und Unity löscht ihre `.meta`.

## Aktueller Meilenstein: Prototyp (Dez 2026 – Jan 2027)

Ziel: Eine Frage beantworten – **macht es Spaß, zu zweit eine schwere Kiste durch eine Hütte zu tragen?**

- [x] Projektgrundlage: Ordnerstruktur, `.gitignore` und `.gitattributes` (LFS) für Unity
- [x] NGO einrichten: Host starten / beitreten (einfache Buttons reichen), Spieler-Prefab spawnt
- [x] Spielerbewegung (First Person), synchronisiert
- [x] Graubox-Hütte (Rohbau per Editor-Skript nach `docs/GRAUBOX.md`; Nachformen mit ProBuilder bei Bedarf)
- [x] Aufheben, Tragen, Ablegen und Werfen von Kisten, synchron für alle (mit 100 ms Ping getestet; Client-Vorhersage macht es für den Client flüssig)
- [ ] Schwerer Gegenstand, der sich nur bewegt, wenn zwei Spieler ihn halten
- [ ] Ofen, der Holz verbraucht und ausgeht (Wärme als einfacher Wert)

Am Prüfpunkt testen wir mit Freunden. Erst wenn das Tragen Spaß macht, geht es zum Vertical Slice.

**Online-Gefühl mittesten:** Im Multiplayer Play Mode gibt es keine Verzögerung. Das Tragen muss auch für den Client Spaß machen, also ab dem Tragen-Schritt mit simulierter Verzögerung testen (Network Simulator von Unity Transport, etwa 100 ms).

## Bewusst NICHT bauen

- Keine Konsolen, kein Crossplay, kein PvP, kein Kampfsystem
- Keine offene Welt, keine prozedural erzeugten Karten
- Keine Story-Kampagne mit Zwischensequenzen
- Keine Mikrotransaktionen
- Nicht mehr als 4 Spieler

Wenn eine Idee in diese Liste fällt oder nicht zum aktuellen Meilenstein gehört: kurz darauf hinweisen und auf eine Notizliste in `docs/IDEEN.md` setzen, statt sie zu bauen.

## Git

- Nach jedem funktionierenden Schritt committen, mit einer kurzen Nachricht auf Deutsch. „Funktionierend“ heißt: Ich habe den Schritt im Editor getestet und bestätigt.
- Große Binärdateien (Modelle, Texturen, Audio) laufen über Git LFS.

## Zusammenspiel mit dem Game-Studios-Framework

Unten folgt das Framework „Claude Code Game Studios“ (Version 1.2.0). Es ist **schlank** installiert:

- **Stufe `minimal`** (Voreinstellung, deshalb steht in `project.yaml` kein `modes:`-Block). Statt Design-Dokumenten pro System gilt `design/game-brief.md`. Ab dem Vertical Slice auf `standard` hochstellen (`/settings`).
- **Hooks sind aus:** Es gibt absichtlich keine `.claude/settings.json`. `.claude/hooks/yaml-helper.sh` wird trotzdem von den Skills benutzt, nicht löschen.
- **`/setup-engine` ist gelaufen** (2026-11-07): Unity 6000.6.6f1, C#. Das Ergebnis steht in `project.yaml`, `.claude/docs/technical-preferences.md` und `docs/engine-reference/unity/`.
- Skills nutze ich, wenn sie passen, zum Beispiel `/playtest-report` nach Tests mit Freunden, `/scope-check`, wenn Ideen dazukommen, und `/gate-check` am Prüfpunkt.

**Bei Widersprüchen gelten die Regeln oben, nicht die des Frameworks.** Konkret:

- **Commits:** nach jedem funktionierenden Schritt, wie oben unter „Git“. Die Framework-Regel „No commits without user instruction“ gilt hier nicht.
- **Freigaben:** Pro Schritt zeige ich einmal, was ich vorhabe und welche Dateien ich anfasse. Deine Freigabe gilt für alle Dateien dieses Schritts. Ich frage nicht vor jeder einzelnen Datei.
- **Unity-Dateien:** Die Regeln unter „Wichtig bei der Arbeit mit Unity“ gelten auch für alle Agenten und Skills.
- **Nachweis:** Im Prototyp ist dein Test im Editor nach meiner Testanleitung der Nachweis. Die Screenshot-Pflicht aus den Coding Standards (`production/qa/evidence/`) gilt ab dem Vertical Slice.
- **Singletons:** erlaubt für echte globale Dienste (Architektur-Regel 5), sonst Abhängigkeiten übergeben.
- **Sprache:** Deutsch auch für Antworten von Agenten und Skills und für Dokumente. Code auf Englisch, Kommentare auf Deutsch.

---

# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through 49 coordinated Claude Code subagents.
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 6.6 (6000.6.6f1)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline
- **Asset Pipeline**: Unity Asset Import Pipeline

> **Note**: Engine-specialist agents exist for Godot, Unity, and Unreal with
> dedicated sub-specialists. Use the set matching your engine.

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

<!-- ENGINE-REFERENCE-IMPORT: the line below is engine-specific. /setup-engine
     rewrites it to @docs/engine-reference/<engine>/VERSION.md for the chosen
     engine, so a Unity or Unreal project stops loading the Godot reference every
     session. It defaults to Godot (the template's example engine); skills that
     need the pinned version read docs/engine-reference/<engine>/VERSION.md on
     demand regardless of this import. -->
@docs/engine-reference/unity/VERSION.md


## Technical Preferences

`project.yaml` at the repo root is the primary config store — engine, specialists,
naming, platform, performance, modes. Skills resolve it via `resolve_config`
(see `.claude/docs/config-resolution.md`).

`.claude/docs/technical-preferences.md` is the **legacy fallback**, read on demand
when a key is absent from `project.yaml`. It is not imported here: before
`/setup-engine` runs it is almost entirely `[TO BE CONFIGURED]` placeholders, and
after it runs `project.yaml` holds the real values.

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
  (exempt: `active.md` appends, agent memory, a subagent's new file at the path
  its orchestrating skill named; see `.claude/rules/agent-memory.md`)
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction
- A skipped or dismissed question is not an answer: act on nothing, re-ask in
  plain text and wait
- Started outside this file's folder? Warn first: hooks, deny rules and the
  ask-first mode load only when Claude Code starts here

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

Read `.claude/docs/context-management.md` on demand — it is a reference, not
session context. Two of its conventions are load-bearing and cited by name
elsewhere in the repo, so they are restated here rather than lost:

- **`production/session-state/active.md` is the session checkpoint.** The file is
  the memory, not the conversation. Read it first after any compaction, crash, or
  `/clear`.
- **Helpers in `.claude/scripts/` emit observations, never verdicts.** A script
  that scores or judges will eventually contradict a mode or override it cannot
  see. (Cited by `artifact-check.sh` and `adr-dep-graph.sh`.)
