# Story 011: Scherben und Aufprall-Klang über ImpactEffects

> **Epic**: Gegenstandsarten
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-11-10
> **Last Updated**: —

## Context

Alle Spieler hören, wenn etwas zerbricht oder aufprallt. Ein dauerhaftes Objekt hört am Meldedienst zu und
schickt Scherben und Geräusche an alle, weil der zerbrochene Gegenstand selbst gleich weg ist. Dazu bekommt
jede Art ihre Klanggruppe (Holz oder Weich) aus dem Katalog.

**GDD**: `design/gdd/gegenstandsarten.md`
**Requirement**: `TR-gegenstandsarten-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: Netz-Autorität und Physik im Netz
**ADR Decision Summary**: Scherben-Effekt, Scherben-Klang und die normalen Aufprall-Geräusche schickt das dauerhafte Objekt `ImpactEffects` (Presentation), das an `ImpactEvents` zuhört und `PlayBreakRpc` und `PlayImpactRpc` an alle sendet. Der eigene `OnCollisionEnter` in `ImpactSound` entfällt.
**ADR Version**: 2026-11-09

**Engine**: Unity 6000.6.6f1 | **Risk**: HIGH
**Engine Notes**: Aus dem NGO-2.13.3-Quelltext belegt: RPC und Despawn im selben Frame kommen in Sendereihenfolge an; `NetworkObjectReference` wirft eine `ArgumentException`, wenn das Objekt nicht mehr gespawnt ist. `FixedString32Bytes` geht als RPC-Parameter (Art-Kennung in `PlayBreakRpc`). `[Rpc(SendTo.ClientsAndHost)]` ist die Form für dieses Projekt, nicht `[ClientRpc]`.

**Control Manifest Rules (this layer)**:
- Required: `ImpactEffects` (dauerhaftes NetworkObject, vom Host beim Start erzeugt) hört an `ImpactEvents` und sendet `PlayBreakRpc` und `PlayImpactRpc`. Bei zwei zusammenstoßenden Gegenständen zählt der schwerere. `PlayImpactRpc` nur, wenn der Gegenstand noch gespawnt ist.
- Required: Ein Client überspringt die Geräusch-RPC, wenn der Gegenstand bei ihm gerade ausgeblendet ist, weil seine Kopie fliegt.
- Required: An- und Abmelden am Meldedienst in `OnNetworkSpawn` (nur bei `IsServer`) und `OnNetworkDespawn`; `OnDestroy` nur als `override` mit `base.OnDestroy()`.
- Forbidden: Nie sendet ein Gegenstand, der gleich entfernt wird, seine Effekte selbst. Nie ein eigener `OnCollisionEnter` in `ImpactSound`. Nie lösen Vorhersage-Kopien Spiel-Logik aus.
- Guardrail: Je Bruch eine RPC plus Despawn; Aufprall-Geräusche mit Sperrzeit je Gegenstand; Ruhendes sendet nichts.

---

## Acceptance Criteria

*From GDD `design/gdd/gegenstandsarten.md`, scoped to this story:*

- [ ] **Kriterium 16 [Manuell]:** Solo, je 10 Versuche mit Tee und Kaiserschmarrn: WHEN auf Boden oder Tisch abgelegt, auch im Gehen THEN 10/10 heil; WHEN aus dem Laufen gerade gegen die Wand geworfen THEN 10/10 verschwinden mit genau einem Scherben-Geräusch, ohne Reste. Speckbrot, genauso geworfen: 10/10 heil.
- [ ] **Kriterium 26 [Manuell]:** Solo: GIVEN je eine Art der Klanggruppen Holz und Weich WHEN sie aus Haltehöhe fallen THEN spielt jede den Klang ihrer Gruppe; eine zerbrechende Tasse spielt genau ein Scherben-Geräusch.
- [ ] **Kriterium 28 [Freundestest]:** Host und Client, Client mit etwa 100 ms Ping: GIVEN der Client hält eine Tasse Tee WHEN er sie gerade gegen die Wand wirft THEN verschwindet sie bei beiden mit je genau 1 Scherben-Geräusch; die Kopie beim Client zerbricht nicht selbst. WHEN er sie 10-mal ablegt THEN bleibt sie 10/10 bei beiden heil.

---

## Implementation Notes

*Derived from ADR-0001 Implementation Guidelines:*

- `ImpactEffects : NetworkBehaviour` mit `[Rpc(SendTo.ClientsAndHost)] PlayImpactRpc(NetworkObjectReference item, Vector3 point, float speed)` und `PlayBreakRpc(Vector3 point, FixedString32Bytes kindId)`. Der Client verträgt, dass `TryGet` fehlschlägt.
- Die Reihenfolge Melden → Entfernen (Story 010) sorgt dafür, dass die Scherben-RPC vor dem Despawn ankommt.
- Verschwindet das Original, entfernt der Client auch seine Vorhersage-Kopie (heute schon in `PlayerCarry`); die Kopie zerbricht nie selbst.
- Die Klanggruppe kommt aus `Kind.ImpactSound` (Holz, Weich). Die `impactSoft`-Clips in `SoundSettings` gibt es schon; die Zuordnung je Art ist neu.
- Für das Scherben-Geräusch braucht `SoundSettings` einen neuen Clip. Woher die Audiodatei kommt, klärt der Bau vorher mit dem Entwickler (Download nur nach Rückfrage mit Datei, Quelle und Größe; Audio läuft über Git LFS).
- Ein sichtbarer Scherben-Effekt bleibt in der Graubox einfach (zum Beispiel kurz aufblitzende Splitter). Sein Aussehen legt die Art Bible fest, wenn es sie gibt.
- `ImpactEffects` entsteht beim Host-Start per Code oder über das Editor-Skript als Prefab in der Netzwerk-Prefab-Liste; die Szene wird nicht von Hand bearbeitet.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 010: Messen, Bruchregel anwenden, Entfernen
- Gäste hören den Lärm (`noise_lockout`): Epic Gäste
- Musik und weitere Klangschichten: Polish

---

## Files

*The code and test files this story creates or changes. `/create-stories` lists the
planned ones; `/dev-story` replaces the list with the files it changed; `/story-done`
checks scope against it.*

- `Assets/_Project/Scripts/Audio/ImpactEffects.cs`
- `Assets/_Project/Scripts/Audio/ImpactSound.cs`
- `Assets/_Project/Scripts/Audio/GhostImpactSound.cs`
- `Assets/_Project/Scripts/Audio/ImpactAudio.cs`
- `Assets/_Project/Scripts/Audio/SoundSettings.cs`
- `Assets/_Project/Scripts/Editor/ItemCatalogBuilder.cs`
- `Assets/_Project/Scripts/Editor/SoundSetupBuilder.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**:
- Integration: playtest doc. Kriterien 16 und 26 nach Testanleitung (Entwickler bestätigt die Zählung 10/10), Kriterium 28 im Freundestest-Bericht

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/gegenstandsarten/story-010-aufprall-messpunkt-und-zerbrechen.md`
- Unlocks: None
