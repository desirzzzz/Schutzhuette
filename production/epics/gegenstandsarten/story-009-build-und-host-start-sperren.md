# Story 009: Build und Host-Start sperren bei Katalogfehlern

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Die Katalogprüfung wird an drei Stellen angewendet: als Menüpunkt im Editor, im Build und vor dem Start des
Hosts. Ein fehlerhafter Katalog kann danach weder als Exe ausgeliefert noch als Host gestartet werden. Der
Build der Exe läuft ab dieser Story über ein eigenes Build-Skript.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-007`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung
**ADR Decision Summary**: Die Prüfung läuft im Editor (Menüpunkt, EditMode-Tests), im Build über ein eigenes Build-Skript (`-executeMethod`, Exit-Code 1 bei Fehlern) und vor `StartHost()` im Startmenü. Ein Build-Hook (`IPreprocessBuildWithReport`) ist nur Zusatzschutz.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: NOT SOURCEABLE für 6.6: `BuildPipeline.BuildPlayer` mit `BuildPlayerOptions`, die Auswertung von `BuildReport.summary.result` und ob `IPreprocessBuildWithReport` mit `BuildFailedException` einen Build aus „Build Profiles“ abbricht. Zu prüfen (ADR-0002, Verification 2): Ein absichtlich kaputter Katalog lässt das Build-Skript mit Exit-Code 1 und den Fehlern im Log enden, ohne dass eine `.exe` entsteht; ein heiler Katalog baut wie bisher. Im Build-Skript nicht `BuildOptions.ForceEnableAssertions` oder `CoreBuildData.developmentBuild` nutzen (veraltet).

**Control Manifest Rules (this layer)**:
- Required: Prüfung als reine Funktion, angewendet im Editor (Menüpunkt, EditMode-Tests), im Build-Skript (`-executeMethod`, Exit-Code 1 bei Fehlern) und vor `StartHost()`.
- Required (Presentation): Das Startmenü ruft vor `StartHost()` die Katalog- und die Gäste-Prüfung auf. Bei Fehlern startet der Host nicht, und die Fehler stehen auf dem Bildschirm.
- Forbidden: Nie die Gäste-Prüfung aus Foundation-Code aufrufen; Aufrufe gehen nur nach unten. Nie `CatalogService.Current` in Prüfregeln oder Editor-Code.
- Guardrail: Prüfung und Tabellenaufbau beim Host-Start im Millisekundenbereich (geschätzt, nicht gemessen).

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 33 [Manuell]:** GIVEN zwei Einträge mit derselben Kennung im Projekt WHEN der Build gestartet wird (Batchmode wie beim Freundestest-Build) THEN bricht er mit einem Fehler ab, der beide Assets nennt, und es entsteht keine Exe.
- [ ] **Edge Case Host-Start [Manuell]:** Wenn zwei Arten oder Waren dieselbe Kennung haben, wird der Host-Start verweigert, bis es behoben ist; der Fehler steht auf dem Bildschirm und nennt beide.
- [ ] **Heiler Katalog (ADR-0002, Verification 2) [Manuell]:** Mit dem ausgelieferten Katalog baut das Build-Skript die Exe wie bisher (Exit-Code 0), und der Host startet.

---

## Implementation Notes

*Derived from ADR-0002 Implementation Guidelines:*

- Build-Skript (`-executeMethod`, wie die bestehenden Batchmode-Skripte): ruft die Prüfung auf, baut nur ohne Fehler mit `BuildPipeline.BuildPlayer`, wertet `report.summary.result` aus und beendet Unity mit Exit-Code 1, wenn Prüfung oder Build fehlschlägt. `commands.build` in `project.yaml` wechselt auf dieses Skript.
- Der Build-Hook ruft dieselbe Prüfung bei Builds aus dem Fenster „Build Profiles“ auf. Bricht er dort nicht ab, loggt er nur die Fehler.
- Host-Start: Bis das Hauptmenü gebaut ist (Plan-Schritt 15), ruft die bestehende Start-Oberfläche (`ConnectionHud`) die Prüfung vor `StartHost()` auf und zeigt die Fehler an. Die Prüfung bekommt Katalog und Netzwerk-Prefab-Liste als Parameter.
- Die Gäste-Prüfung gibt es noch nicht (Epic Gäste). Das Build-Skript und die Start-Oberfläche rufen sie auf, sobald sie existiert.
- Für Kriterium 33 legt der Test die doppelte Kennung nur vorübergehend an und nimmt sie danach zurück; der kaputte Stand wird nie committet.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001 und 002: die Prüfregeln selbst
- Das Hauptmenü und die endgültige Fehleranzeige: Plan-Schritt 15, ADR-0007
- Prüfung der Gäste-Daten (`guest_data_valid`): Epic Gäste

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Editor/CatalogBuild.cs`
- `Assets/_Project/Scripts/Editor/CatalogBuildHook.cs`
- `Assets/_Project/Scripts/Editor/CatalogMenu.cs`
- `Assets/_Project/Scripts/Net/ConnectionHud.cs`
- `project.yaml`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: playtest doc mit den Log-Zeilen und Exit-Codes der zwei Build-Läufe (kaputter und heiler Katalog) und der Fehleranzeige beim Host-Start, in `production/qa/evidence/`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-005-startkatalog.md`
- Unlocks: None
