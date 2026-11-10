# Test-Gerüst

**Engine**: Unity 6.6 (6000.6.6f1)
**Test Framework**: Unity Test Framework 1.8.0 (NUnit)
**CI**: `.github/workflows/tests.yml`
**Eingerichtet**: 2026-11-10 (`/test-setup`)

## Ordner

Unity kompiliert nur Code unter `Assets/` und `Packages/`. Ein Test in diesem Ordner `tests/` würde nie
kompiliert, und der Lauf meldete dann „0 Tests, Passed“. Deshalb liegen die Tests selbst unter `Assets/Tests/`:

```
Assets/Tests/
  EditMode/   # Rechenregeln ohne Szene (Assembly Schutzhuette.Tests.EditMode), z. B. StoveTests.cs
  PlayMode/   # Tests in einer laufenden Szene (Assembly Schutzhuette.Tests.PlayMode)
tests/
  README.md   # diese Datei
  smoke/      # Checkliste für den Schnelltest (/smoke-check)
production/qa/
  evidence/   # Screenshots und Testnachweise von Hand
```

Überall, wo Framework-Dokumente `tests/unit/[system]/` schreiben, ist `Assets/Tests/EditMode/` gemeint, bei
`tests/integration/[system]/` entsprechend `Assets/Tests/PlayMode/`.

Der Spielcode liegt in der Assembly `Schutzhuette.Runtime` (`Assets/_Project/Scripts/`), die Bau-Skripte in
`Schutzhuette.Editor` (`Assets/_Project/Scripts/Editor/`). Die Test-Assemblies verweisen auf
`Schutzhuette.Runtime`. Aufteilung und Gründe: `docs/architecture/architecture.md`, Abschnitt
„Code-Aufteilung“.

## Tests starten

**Im Editor:** Window → General → Test Runner, Reiter „EditMode“ oder „PlayMode“, dann „Run All“.

**Im Batchmode** (Unity muss geschlossen sein; den Editor direkt aufrufen, nicht ein `Unity` aus dem PATH):

```
"C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults test-results/editmode.xml
"C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testResults test-results/playmode.xml
```

- Kein `-quit` anhängen, `-runTests` beendet Unity von selbst.
- Exit-Code **0**: alle Tests bestanden. **2**: mindestens ein Test fehlgeschlagen. **1**: Kompilierfehler,
  dann gibt es keine Ergebnisdatei. **3**: Lauf fehlgeschlagen. Jeder andere Wert ist ebenfalls ein Fehler.
- Die alte Ergebnisdatei vor jedem Lauf löschen, damit nie ein altes Ergebnis gelesen wird.
- Im Element `<test-run>` der Ergebnisdatei steht `testcasecount`. **0 heißt: nichts gefunden oder
  kompiliert.** Das ist kein Bestehen, sondern „nicht geprüft“.
- Ein EditMode-Lauf führt die PlayMode-Tests nicht aus. Liegen Tests in `PlayMode/`, beide Zeilen starten.
- `test-results/` gehört nicht ins Repository.

## Namen

Klasse `[System]Tests` in einer gleichnamigen Datei, Methode `[Szenario]_[Erwartung]` in PascalCase
(`.claude/rules/test-standards.md`). Beispiel: `StoveTests.cs` → `StoveOut_HeatDecays()`.
Jeder Test hat die Teile Arrange, Act und Assert.

Testbar ohne Szene sind reine statische Rechenregeln wie `Stove.Step` und `HeavyCarryable.ComputeCarryPose`
(Architektur-Regel 5). Neue Regeln deshalb als solche Funktionen schreiben.

## Welcher Nachweis für welche Story

| Story-Art | Nachweis | Ort |
|---|---|---|
| Logik | automatischer Test, muss bestehen | `Assets/Tests/EditMode/` |
| Zusammenspiel | Test oder dokumentierter Spieltest | `Assets/Tests/PlayMode/` |
| Aussehen/Gefühl | Screenshot + Freigabe | `production/qa/evidence/` |
| UI | Screenshot jedes geänderten Bildschirms | `production/qa/evidence/` |
| Werte/Daten | Schnelltest bestanden | `production/qa/smoke-*.md` |

Im Prototyp gilt laut `CLAUDE.md` noch dein Test im Editor oder in der exe als Nachweis; die
Screenshot-Pflicht beginnt mit dem Vertical Slice.

## CI

GitHub führt bei jedem Push auf `main` und bei jedem Pull Request die EditMode- und PlayMode-Tests aus
(`game-ci/unity-test-runner`). Schlägt ein Test fehl, ist der Lauf rot.

**Voraussetzung: die Unity-Lizenz als Secret.** GitHub braucht eine Unity-Lizenz, um den Editor zu starten.
Sie wird als Repository-Secret `UNITY_LICENSE` eingetragen (GitHub → Repository → Settings → Secrets and
variables → Actions). Das trägst du selbst ein; Lizenzdaten gehören nie in eine Datei im Repository. Wie
man die Lizenz für game-ci erzeugt, steht in der Anleitung von game-ci (https://game.ci/docs, Abschnitt
„Activation“); wir richten das gemeinsam ein, wenn es so weit ist.

Bis das Secret existiert, bricht der erste Schritt jedes Laufs mit der Meldung „UNITY_LICENSE fehlt“ ab.
Das ist Absicht: Ein Lauf, der still nichts testet, sähe sonst aus wie ein bestandener.
