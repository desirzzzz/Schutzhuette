# ADR-0002: Gegenstandsarten-Katalog und Netz-Kennung

## Status

Proposed

> **Wer auf `Accepted` setzen darf: der Entwickler, oder `technical-director` nach ausdrücklicher Bestätigung
> des Entwicklers.** Kein anderer Agent und kein Skill allein. Stories, die auf eine `Proposed`-ADR
> verweisen, sind `Blocked`.

## Date

2026-11-09

## Last Verified

2026-11-09

## Decision Makers

Entwickler (Projektleitung) mit Claude über `/architecture-decision`. Engine-Prüfung: `unity-specialist`
(2026-11-09: ein Blocker zum Zeitpunkt der Startwerte, behoben in Entscheidung 5; Hinweise eingearbeitet).
TD-ADR übersprungen (Review-Modus lean).

## Summary

Tragen, Ofen, Lager, Kochen und Gäste brauchen dieselben Daten über Gegenstände, Waren, Bedürfnisse und
Gästetypen, und Clients müssen wissen, um welche Art es sich handelt. Entschieden ist: ein ScriptableObject
je Eintrag in einem Katalog-Objekt, optionale Eigenschaften als feste Blöcke, das Prefab verrät die Art
(nichts geht dafür übers Netz), wo eine Kennung doch übers Netz muss, geht sie als Text
(`FixedString32Bytes`), und eine reine Prüffunktion sperrt Build und Host-Start bei Fehlern.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.6f1, Netcode for GameObjects 2.13.3 (mit `com.unity.collections` für `FixedString32Bytes`) |
| **Domain** | Core (Daten) mit Networking |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH (Unity 6.6 und NGO 2.13 liegen nach dem Wissensstand des Modells, `VERSION.md`) |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md` (Block „NGO 2.13 for this project“), `breaking-changes.md` (6.6: `Dictionary`-Felder werden serialisiert, YAML-Format geändert), `deprecated-apis.md` (`Resources.Load`), `current-best-practices.md`; NGO 2.13.3 Quelltext `Runtime/NetworkVariable/Serialization/TypedILPPInitializers.cs` (`InitializeSerializer_FixedString<T>`); `Assets/_Project/Scripts/Net/GameVersion.cs` (Versionsprüfung beim Beitritt); `docs/registry/architecture.yaml` (Regeln aus ADR-0001) |
| **Post-Cutoff APIs Used** | `NetworkVariable<FixedString32Bytes>` und `FixedString32Bytes` als RPC-Parameter (NGO 2.13.3, Serialisierer im Quelltext belegt); `NetworkVariable<double>` |
| **Verification Required** | (1) Log-Prüfung: Der Client sieht die Ware-Kennung eines Vorratspakets schon in seinem `OnNetworkSpawn`, ohne Warnung „NetworkVariable is written to, but doesn't know its NetworkBehaviour yet“ beim Host. (2) Ein absichtlich kaputter Katalog lässt den Kommandozeilen-Build (`-buildWindows64Player`) mit „Result: Failure“ enden. (3) Nach einer Spielrunde zeigt `git status` keine Änderung an den Katalog-Dateien (GDD Gegenstandsarten, Abnahmekriterium 9). (4) Eine Kennung mit 29 Zeichen passt in `FixedString32Bytes`, eine mit 30 meldet die Prüfung als Fehler. |

**Aus dem NGO-2.13.3-Quelltext belegt** (Spezialisten-Prüfung 2026-11-09, Stichproben nachgelesen):
- `NetworkVariable<FixedString32Bytes>` und `NetworkVariable<double>` brauchen keinen eigenen Serialisierer.
  `FixedString32Bytes` geht auch als RPC-Parameter. Übertragen werden Länge und belegte Bytes, nicht feste 32.
- `OnNetworkSpawn` des Hosts läuft, bevor NGO die Spawn-Nachricht baut (`Spawning/NetworkSpawnManager.cs`); dort
  gesetzte Werte stehen in der Spawn-Nachricht, und NGO schickt keine doppelte Änderung hinterher
  (`NetworkVariable/NetworkVariableBase.cs`).
- Eine NetworkVariable vor `Spawn()` zu schreiben, loggt eine Warnung (`NetworkVariableBase.cs`); nach `Spawn()`
  geschriebene Werte kommen erst mit dem nächsten Tick.

**NOT SOURCEABLE** (nicht in `docs/engine-reference/` oder im Quelltext belegt, deshalb in „Verification
Required“):
- Der Build-Hook `IPreprocessBuildWithReport` mit `BuildFailedException` (aus älteren Unity-Versionen
  bekannt). Die Engine-Referenz nennt keinen Weg, einen Build aus Code abzubrechen.
- Die Zeichengrenze von `FixedString32Bytes` (29 Byte, Trainingswissen).
- Dass Unity den Kreisverweis Art → Prefab → Art ohne Probleme speichert (Trainingswissen: ja; der
  `GlobalObjectIdHash` hängt nur am Prefab).

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Netz-Autorität und Physik im Netz; muss vor der Umsetzung `Accepted` sein) |
| **Enables** | ADR-0003 (die Saison-Startaufstellung erzeugt Gegenstände über diese Arten), ADR-0004 (Interaktion liest Werkzeug, Kategorie und Ausrüstung aus dem Katalog), ADR-0005 (Gäste nutzen Bedürfnisse und Gästetypen als Datenobjekte) |
| **Blocks** | Plan-Schritt „Gegenstandsarten“ in `production/milestones/vertical-slice.md`. Epics und Stories gibt es noch keine. |
| **Ordering Note** | Das Abkühlen (TR-gegenstandsarten-004) rechnet mit `game_time` aus ADR-0003. Speicherort und Formel legt diese ADR fest; den Bau des Abkühlens erst nach ADR-0003 beginnen. Die Art-Kennung in `ImpactEffects.PlayBreakRpc` (ADR-0001) ist ein `FixedString32Bytes`. |

## Context

### Problem Statement

Heute stehen Spielwerte verstreut in Komponenten und Prefabs: die Brenndauer im `FuelItem` (45 s), die Masse
im Rigidbody jedes Prefabs, die Trageart „zu zweit“ nur dadurch, dass die Truhe ein `HeavyCarryable` hat. Das
GDD Gegenstandsarten verlangt einen Katalog, in dem jeder Wert genau einmal steht, und eine feste Kennung, die
übers Netz geht. Das GDD Gäste verlangt dasselbe für Gästetypen und Bedürfnisse. Plan-Schritt
„Gegenstandsarten“ baut den Katalog; vorher muss klar sein, wie er technisch aussieht.

### Current State

- Drei Prefabs mit Gegenständen: `Crate`, `Firewood`, `HeavyChest`. `CrateSpawner` erzeugt sie beim Host.
- `FuelItem._burnSeconds` (45 s), Masse aus `Rigidbody.mass`, `Carryable.Mass` liest den Rigidbody.
- Netz: NGO erzeugt beim Client dasselbe Prefab (über `GlobalObjectIdHash` und die Netzwerk-Prefab-Liste
  `Assets/DefaultNetworkPrefabs.asset`). Eine Art-Kennung gibt es nicht.
- `GameVersion` lässt nur Clients mit genau derselben Versionsnummer beitreten.
- Datenobjekte gibt es schon für Einstellungen (`StoveSettings`, `SoundSettings`), nicht für Arten.

### Constraints

- CLAUDE.md Architektur-Regel 4 (Daten in ScriptableObjects) und 5 (Singletons nur für echte globale
  Dienste). Domain Reload ist aus.
- Register-Regeln aus ADR-0001: Objekt-Identität im Netz über `NetworkObjectReference`/`NetworkObjectId`,
  nie über `EntityId`; `ImpactReport.Mass` kommt aus dem Katalog.
- `.asset`-Dateien werden nie von Hand (YAML) bearbeitet; Katalog-Assets entstehen per Editor-Skript oder im
  Inspector.
- Addressables sind nicht installiert; `Resources.Load` gilt als veraltet.
- Entwickler mit etwas Unity-Erfahrung: Der Katalog muss im Inspector ohne eigenen Editor-Code pflegbar sein.

### Requirements

- Alle Pflichtfelder und optionalen Eigenschaften aus GDD Gegenstandsarten, Regeln 1–3.
- Doppelte oder kaputte Einträge sperren Build und Host-Start (GDD Edge Cases).
- Über das Netz nur Kennungen, nie Datenobjekte (TR-gegenstandsarten-002).
- Waren als schlanker Typ ohne Körper (TR-gegenstandsarten-006).
- Gerichte kühlen mit `game_time` ab; der Host speichert nur den Kochzeitpunkt (TR-gegenstandsarten-004).
- Gästetypen und Bedürfnisse als Datenobjekte mit Prüfung `guest_data_valid` (TR-gaeste-006).
- Prüfregeln als reine Funktionen, testbar in EditMode mit `ScriptableObject.CreateInstance`.

## Decision

**1. Ein Datenobjekt je Eintrag, ein Katalog.** Jede Gegenstandsart ist ein ScriptableObject `ItemKind`,
jede Ware ein `WareKind`, jedes Bedürfnis ein `NeedKind`. Ein `ItemCatalog` listet alle drei und trägt die
Katalog-Einstellungen (`max_solo_mass`, Prüfwerte Haltehöhe und Wurftempo, `floor_factor`, `max_path`,
`walk_speed_min`, Toleranz beim Hacken). Gästetypen (`GuestType`) und die Gäste-Einstellungen folgen demselben
Muster im Gäste-Code und verweisen nach unten auf `NeedKind`. Ablage der Assets: `Assets/_Project/Data/Items/`,
`Data/Wares/`, `Data/Needs/`, `Data/Guests/`.

**2. Optionale Eigenschaften als feste Blöcke.** Jede Eigenschaft (Brennbar, Hackbar, Vorratspaket, Gericht,
Zerbrechlich, Werkzeug, Ausrüstung) ist ein `[Serializable]`-Block mit Schalter `Enabled` im `ItemKind`. Eine
neue Eigenschaft heißt: ein neuer Block. Werkzeug-Handlung und Ausrüstungsplatz sind Enums in Foundation
(`ToolAction`, `EquipmentSlot`), damit Interaktion (Core) nach unten liest und nicht umgekehrt.

**3. Das Prefab verrät die Art.** Jede Art hat ihr eigenes Prefab, und jedes Gegenstands-Prefab trägt die
Komponente `Item` mit einem Verweis auf seine `ItemKind`. NGO erzeugt beim Client dasselbe Prefab, also kennt
jeder Rechner die Art ohne zusätzliche Netzdaten. In `Awake` übernimmt der Rigidbody auf jedem Rechner die
Masse aus dem Katalog; so stimmt sie auch für die Vorhersage-Kopie. Zwei Arten mit demselben Prefab sind ein
Prüffehler.

**4. Kennungen als Text.** Wo eine Kennung doch übers Netz muss, geht sie als `FixedString32Bytes`: die Ware
eines Vorratspakets ohne feste Ware, das Bedürfnis eines Gastes, die Art in der Scherben-RPC (ADR-0001).
Kennungen bestehen nur aus `a–z`, `0–9` und `_`, höchstens 29 Zeichen (Grenze von `FixedString32Bytes`).
Waren beginnen mit `ware_`, Bedürfnisse mit `beduerfnis_`, Arten mit keinem der beiden. Eine unbekannte
Kennung beim Client ist ein Fehler im Log, und der Gegenstand wird nicht angezeigt; es wird nie still eine
andere Art gezeigt.

**5. Werte je Gegenstand als NetworkVariables beim Host.** Was nicht in der Art steht, sondern zum einzelnen
Gegenstand gehört, ist eine NetworkVariable, die nur der Host schreibt. Der Startwert kommt vor `Spawn()` in ein
normales Feld (zum Beispiel `SetInitialWare`), und der Host überträgt ihn in `OnNetworkSpawn` in die
NetworkVariable. Grund (NGO-Quelltext): `OnNetworkSpawn` läuft, bevor NGO die Spawn-Nachricht baut. So steht der
Wert schon in der Spawn-Nachricht, und der Client sieht ihn in seinem eigenen `OnNetworkSpawn`. Schreiben vor
`Spawn()` loggt eine Warnung, Schreiben nach `Spawn()` schickt den Wert erst einen Tick später hinterher.
Spätere Änderungen sehen Clients über `OnValueChanged`.
- Vorratspaket ohne feste Ware: `NetworkVariable<FixedString32Bytes>` mit der Ware-Kennung.
- Gericht: `NetworkVariable<double>` mit `cooked_time` in Spielzeit. Clients berechnen `is_warm` und
  `warm_fraction` selbst (`DishRules`, rein statisch). Die Spielzeit liefert ADR-0003.

**6. Zugriff zur Laufzeit über einen globalen Dienst.** Die Komponente `CatalogService` sitzt am Objekt des
`NetworkManager` (bleibt beim Szenenwechsel bestehen), hält den `ItemCatalog` und stellt sich als
`CatalogService.Current` bereit. Sie baut die Nachschlagetabellen (Kennung → Eintrag) bei jedem Start neu und
bietet `TryGetKind`, `TryGetWare` und `TryGetNeed` an. Der `ItemCatalog` selbst enthält nur Listen und
Einstellungen, keine Laufzeit-Tabellen: Ohne Domain Reload könnten nicht gespeicherte Felder eines
ScriptableObjects zwischen zwei Play-Durchläufen alt bleiben. Das statische Feld wird bei
`SubsystemRegistration` zurückgesetzt. Prüfregeln und Tests bekommen den Katalog als Parameter und nutzen
`Current` nie.

**7. Prüfung als reine Funktion, dreifach angewendet.** `CatalogValidator.Validate(ItemCatalog, NetworkPrefabsList)`
liefert einen `CatalogReport` mit Fehlern, Hinweisen und `StartBlocked`. Die Netzwerk-Prefab-Liste ist Parameter,
damit die Regel „jedes Prefab steht in der Liste“ ohne Szene prüfbar bleibt. Eine zweite reine Funktion prüft
die Gäste-Daten (`guest_data_valid`, im Gäste-Code, zum Beispiel in `GuestMath` wie im GDD Gäste) und den
Querverweis Gericht → Bedürfnis. Angewendet wird die Prüfung:
- im Editor über einen Menüpunkt und in den EditMode-Tests,
- beim Build über einen Build-Hook, der bei `StartBlocked` abbricht,
- beim Host-Start: Bei `StartBlocked` startet der Host nicht, und die Fehler stehen auf dem Bildschirm.

### Architecture Diagram

```
 Editor / Build                         Laufzeit (jeder Rechner)                    Netz
 ──────────────                         ────────────────────────                    ────
 Data/Items/*.asset (ItemKind) ─┐
 Data/Wares/*.asset (WareKind) ─┼─► ItemCatalog ──► CatalogService.Current
 Data/Needs/*.asset (NeedKind) ─┘        │              (Kennung → Eintrag)
                                         │
 CatalogValidator.Validate ◄─────────────┤ Menü, EditMode-Test, Build-Hook, Host-Start
   → CatalogReport (StartBlocked)        │
                                         ▼
 Prefab (NetworkObject) ── Item ──► ItemKind ── Masse, Trageart, Blöcke
   │                                                                     NGO erzeugt dasselbe Prefab
   └── Host: Instantiate → SetInitialWare / SetInitialCookedTime → Spawn
         OnNetworkSpawn (Host): Startwert → NetworkVariable
         dann baut NGO die Spawn-Nachricht ───────────────────────────────► beim Client: Art bekannt (Prefab)
           SupplyPackState.WareId (FixedString32Bytes) ───────────────────► Client: Kennung → WareKind
           DishState.CookedTime (double, Spielzeit) ──────────────────────► Client: DishRules.IsWarm
```

### Key Interfaces

Vorschlag; die genauen Namen legt der Bau fest. Ort: Foundation, `Assets/_Project/Scripts/Items/`, Namespace
`Schutzhuette.Items` (wie in ADR-0001). Gäste-Datentypen in `Assets/_Project/Scripts/Guests/`.

```csharp
namespace Schutzhuette.Items
{
    public enum CarryMode { Solo, Pair }
    public enum ItemCategory { Fuel, RawMaterial, SupplyPack, Dish, Tool, Bulky }
    public enum ImpactSoundGroup { Wood, Soft }
    public enum ToolAction { Chop, FirstAid, Light, Radio }
    public enum EquipmentSlot { Head, Belt }

    /// <summary>Eine Gegenstandsart. Zur Laufzeit schreibgeschützt.</summary>
    [CreateAssetMenu(menuName = "Schutzhütte/Gegenstandsart")]
    public class ItemKind : ScriptableObject
    {
        // [SerializeField] private Felder, nach außen nur lesend:
        public string Id { get; }                 // z. B. "holzscheit"
        public string DisplayName { get; }
        public float MassKg { get; }
        public CarryMode CarryMode { get; }
        public ItemCategory Category { get; }
        public ImpactSoundGroup ImpactSound { get; }
        public NetworkObject Prefab { get; }

        public FuelBlock Fuel { get; }            // Enabled, BurnSeconds
        public ChopBlock Chop { get; }            // Enabled, ItemKind Result, int Count
        public SupplyPackBlock SupplyPack { get; }// Enabled, int Portions, WareKind FixedWare (optional)
        public DishBlock Dish { get; }            // Enabled, NeedKind Need, float CoolTimeSeconds (≤ 0 = kühlt nie ab)
        public FragileBlock Fragile { get; }      // Enabled, float BreakSpeed
        public ToolBlock Tool { get; }            // Enabled, ToolAction Action
        public EquipmentBlock Equipment { get; }  // Enabled, EquipmentSlot Slot
    }

    public class WareKind : ScriptableObject { public string Id { get; } public string DisplayName { get; } }  // "ware_…"
    public class NeedKind : ScriptableObject { public string Id { get; } public string DisplayName { get; } }  // "beduerfnis_…"

    /// <summary>Nur Listen und Einstellungen, keine Laufzeit-Tabellen.</summary>
    public class ItemCatalog : ScriptableObject
    {
        public IReadOnlyList<ItemKind> Kinds { get; }
        public IReadOnlyList<WareKind> Wares { get; }
        public IReadOnlyList<NeedKind> Needs { get; }
        public CatalogSettings Settings { get; }   // max_solo_mass, Prüfwerte, floor_factor, …
    }

    /// <summary>Hält den Katalog zur Laufzeit. Sitzt am NetworkManager-Objekt.
    /// Baut die Nachschlagetabellen bei jedem Start neu.</summary>
    public class CatalogService : MonoBehaviour
    {
        public static CatalogService Current { get; private set; }
        public ItemCatalog Catalog { get; }
        public bool TryGetKind(in FixedString32Bytes id, out ItemKind kind);
        public bool TryGetWare(in FixedString32Bytes id, out WareKind ware);
        public bool TryGetNeed(in FixedString32Bytes id, out NeedKind need);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;
    }

    /// <summary>Reine Prüfregeln aus GDD Gegenstandsarten (Edge Cases, Formeln 1, 2, 3a, 4).</summary>
    public static class CatalogValidator
    {
        public static CatalogReport Validate(ItemCatalog catalog, NetworkPrefabsList networkPrefabs);
    }

    public sealed class CatalogReport
    {
        public IReadOnlyList<string> Errors { get; }   // nennt Kennung oder Asset-Namen
        public IReadOnlyList<string> Hints { get; }
        public bool StartBlocked => Errors.Count > 0;
    }

    /// <summary>An jedem Gegenstands-Prefab: welche Art es ist.</summary>
    public class Item : MonoBehaviour { public ItemKind Kind { get; } }   // kein Netzwert nötig

    /// <summary>Nur an Vorratspaketen ohne feste Ware.</summary>
    public class SupplyPackState : NetworkBehaviour
    {
        public NetworkVariable<FixedString32Bytes> WareId;      // schreibt nur der Host
        public void SetInitialWare(FixedString32Bytes wareId);  // Host, vor Spawn(); OnNetworkSpawn überträgt
    }

    /// <summary>Nur an Gerichten mit Abkühlzeit.</summary>
    public class DishState : NetworkBehaviour
    {
        public NetworkVariable<double> CookedTime;               // Spielzeit, schreibt nur der Host
        public void SetInitialCookedTime(double gameTime);       // Host, vor Spawn(); OnNetworkSpawn überträgt
    }

    /// <summary>GDD Gegenstandsarten, Formel 3. Rein, ohne Szene testbar.</summary>
    public static class DishRules
    {
        public static bool IsWarm(double gameTime, double cookedTime, float coolTime);
        public static float WarmFraction(double gameTime, double cookedTime, float coolTime);
    }
}
```

### Implementation Guidelines

- Jeder Spielwert einer Art **muss** im `ItemKind` stehen. Komponenten und Prefabs **dürfen** keine eigenen
  Kopien führen (zum Beispiel keine Brenndauer im `FuelItem`, keine Masse nur im Rigidbody).
- Datenobjekte **dürfen** zur Laufzeit nie geschrieben werden. Zustand eines einzelnen Gegenstands gehört
  an den Gegenstand (Feld oder NetworkVariable), nie in die Art.
- Code **muss** eine Art an ihrer Eigenschaft erkennen (`Kind.Fuel.Enabled`, `Kind.Equipment.Enabled`), nie an
  Kennung oder Namen.
- Über das Netz **dürfen** nur Kennungen gehen (`FixedString32Bytes`), nie ScriptableObjects, Asset-Pfade oder
  Listen-Positionen.
- Kennungen **müssen** dem Muster `^[a-z0-9_]{1,29}$` folgen; Waren beginnen mit `ware_`, Bedürfnisse mit
  `beduerfnis_`, Arten mit keinem der beiden. Kennungen **dürfen** sich nach dem ersten Release nie ändern.
- Jede Art **muss** ein eigenes Prefab haben, und jedes Prefab **muss** in der Netzwerk-Prefab-Liste stehen.
  Die Prüfung meldet beides.
- NetworkVariables je Gegenstand **darf nur** der Host schreiben. Startwerte **müssen** vor `Spawn()` in ein
  normales Feld und in `OnNetworkSpawn` (Host) in die NetworkVariable; nie vor `Spawn()` direkt in die
  NetworkVariable, nie erst danach. Clients **müssen** spätere Änderungen über `OnValueChanged` verarbeiten.
- Prefabs, die ein Editor-Skript anlegt, **müssen** danach ihren `GlobalObjectIdHash` neu erzeugen lassen
  (wie `PrototypeSceneBuilder.RefreshPrefabHashFromCommandLine`), sonst ist er 0 und NGO spawnt sie nicht.
- Wer ein Vorratspaket ohne feste Ware erzeugt, **muss** die Ware nennen; sonst erscheint es nicht (Fehler).
- Gegenstände **müssen** über eine Host-Funktion erscheinen, die die Art prüft, bevor sie `Instantiate` und
  `Spawn` aufruft; eine ungültige Art erscheint nicht.
- `CatalogService.Current` **darf nur** Laufzeit-Code nutzen. Prüfregeln, Editor-Code und Tests **müssen** den
  Katalog als Parameter bekommen.
- Nachschlagetabellen **müssen** zur Laufzeit gebaut werden (im `CatalogService`), nicht als serialisiertes
  `Dictionary` im Asset.
- Katalog-Assets **müssen** per Editor-Skript oder im Inspector entstehen, nie durch Bearbeiten der `.asset`-
  Datei.

## Alternatives Considered

### Alternative 1: Ein großes ScriptableObject mit einer Liste aller Einträge
- **Description**: Ein einziges Asset `Katalog` mit einer Liste von Einträgen; jede Art ist ein Listenelement.
- **Pros**: Alles auf einen Blick, nur eine Datei.
- **Cons**: Ein Prefab kann nicht sauber auf ein Listenelement verweisen; Git-Konflikte in einer großen Datei;
  ein Fehler in einem Eintrag betrifft die ganze Datei.
- **Estimated Effort**: etwas geringer.
- **Rejection Reason**: Das Prefab soll die Art kennen (Entscheidung 3); das geht nur mit einem Asset je Art.

### Alternative 2: Externe Datei (JSON), zur Laufzeit geladen
- **Description**: Arten in einer JSON-Datei, beim Start eingelesen.
- **Pros**: Ohne Unity bearbeitbar, später modbar.
- **Cons**: Keine Verweise auf Prefabs und andere Assets, kein Inspector, eigener Lade- und Prüfcode; Mods sind
  kein Ziel.
- **Estimated Effort**: höher.
- **Rejection Reason**: Widerspricht Architektur-Regel 4 und macht die Pflege für den Entwickler schwerer.

### Teilentscheidungen (je kurz)
- **Eigenschaften als `[SerializeReference]`-Liste:** sehr erweiterbar, aber die Anzeige im Inspector ist für
  6.6 nicht belegt und braucht vermutlich eigenen Editor-Code. Verworfen.
- **Eigenschaften als Komponenten am Prefab:** Werte stünden wieder im Prefab. Verworfen.
- **Art-Kennung als NetworkVariable je Gegenstand:** erlaubt ein Prefab für mehrere Arten, kostet aber Netzdaten
  und neue Fehlerfälle. Verworfen.
- **Kennung als Nummer (Listenplatz):** 2 Byte, aber bei abweichendem Katalogstand (gleiche Versionsnummer)
  zeigt der Client still die falsche Art. Verworfen.
- **Kennung als Prüfsumme:** klein und reihenfolgeunabhängig, aber in Logs unlesbar, Doppelungen müssen
  abgefangen werden. Verworfen.
- **Katalog-Verweis in jeder Komponente:** kein statischer Zustand, aber viele vergessliche Verweise. Verworfen.
- **`Resources.Load`:** laut Engine-Referenz veraltet. Verworfen.

## Consequences

### Positive
- Jeder Wert steht genau einmal; Tragen, Ofen, Lager, Kochen und Gäste lesen dieselben Daten.
- Für die Art eines Gegenstands geht nichts übers Netz; Clients kennen sie sofort beim Erscheinen.
- Kennungen sind in Logs lesbar; eine unbekannte Kennung ist ein sichtbarer Fehler statt einer falschen Art.
- Die Prüfung ist eine reine Funktion, testbar in EditMode, und dieselbe Funktion sperrt Build und Host-Start.
- Der Katalog ist im Inspector pflegbar, ohne eigenen Editor-Code.

### Negative
- Jede Art braucht ein eigenes Prefab (im Vertical Slice 13).
- Ein statischer Zugang (`CatalogService.Current`) ist globaler Zustand.
- Eine neue Werkzeug-Handlung oder Eigenschaft braucht eine Code-Änderung (Enum oder Block).
- Text-Kennungen sind größer als Nummern (bei wenigen Sendungen unerheblich).

### Neutral
- Die Masse wird auf jedem Rechner aus dem Katalog gesetzt, nicht übertragen.

## Risks
- **Startwerte kommen beim Client zu spät an**, wenn jemand sie nach `Spawn()` setzt: Der Client sieht dann in
  `OnNetworkSpawn` eine leere Kennung und meldet „unbekannt“. Gegenmittel: Regel „Startwert in
  `OnNetworkSpawn`“ (Entscheidung 5), Verification 1.
- **Prefabs aus Editor-Skripten haben `GlobalObjectIdHash` 0** und erscheinen beim Client nicht (im Projekt
  beobachtet, `modules/networking.md`). Gegenmittel: Hash nach dem Anlegen neu erzeugen (Guidelines).
- **Build-Hook bricht den Kommandozeilen-Build nicht ab.** Gegenmittel: Verification 2; Ausweg: Prüfung als
  `-executeMethod` vor dem Build in `commands.build`.
- **Ein Prefab fehlt in der Netzwerk-Prefab-Liste** und erscheint beim Client nicht. Gegenmittel: Prüfregel.
- **Datenobjekte werden im Editor zur Laufzeit verändert** und die Änderung bleibt gespeichert. Gegenmittel:
  Regel „schreibgeschützt“ und Abnahmekriterium 9 (`git status`).
- **Unity 6.6 schreibt Assets im neuen YAML-Format.** Ältere Assets zeigen fremde Diffs. Gegenmittel: neue
  Katalog-Assets entstehen gleich in 6.6; ein Umschreiben alter Assets in einem eigenen Commit.
- **Gleiche Versionsnummer, anderer Katalogstand** (Testbuilds). Gegenmittel: Text-Kennungen machen daraus einen
  Fehler statt einer falschen Art; die Versionsnummer bei jedem Build hochzählen.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| gegenstandsarten.md | TR-gegenstandsarten-001 (vorläufig): Katalog aller Arten als Datenobjekte mit Masse, Trageart und Eigenschaften (Regeln 1, 2) | `ItemKind` je Art mit Pflichtfeldern und festen Eigenschafts-Blöcken, `ItemCatalog` (Entscheidung 1, 2) |
| gegenstandsarten.md | TR-gegenstandsarten-002 (vorläufig): Über das Netz nur die feste Kennung, nie das Objekt | Art über das Prefab; sonst Kennung als `FixedString32Bytes` (Entscheidung 3, 4) |
| gegenstandsarten.md | TR-gegenstandsarten-004 (vorläufig): Gerichte kühlen mit `game_time` ab; der Host speichert nur den Kochzeitpunkt (Formel 3) | `DishState.CookedTime` als NetworkVariable, `DishRules` rein statisch (Entscheidung 5; Zeitquelle ADR-0003) |
| gegenstandsarten.md | TR-gegenstandsarten-006 (vorläufig): Waren als schlanker Typ ohne Körper (Regel 3) | `WareKind` mit Kennung und Anzeigename, ohne Prefab-Feld (Entscheidung 1) |
| gegenstandsarten.md | Edge Cases Katalog und Netz: doppelte Kennung sperrt Build und Host-Start; Prefab-Masse weicht ab → Katalog gilt; unbekannte Kennung beim Client → Fehler, nicht anzeigen; Gegenstand ohne Art → Fehler | `CatalogValidator` mit `StartBlocked` an drei Stellen; Masse in `Awake` aus dem Katalog; Fehler bei unbekannter Kennung (Entscheidung 3, 4, 7) |
| gegenstandsarten.md | Tuning Knobs: Katalog-Einstellungen (`max_solo_mass`, Prüfwerte, `floor_factor` …) liegen in Datenobjekten | `CatalogSettings` im `ItemCatalog` (Entscheidung 1) |
| gaeste.md | TR-gaeste-006 (vorläufig): Gästetypen und Bedürfnisse als Datenobjekte, mit Datenprüfung `guest_data_valid` | `GuestType` und `NeedKind` als ScriptableObjects, eigene reine Prüffunktion samt Querverweis Gericht → Bedürfnis (Entscheidung 1, 7) |

Die TR-IDs sind vorläufig; `/architecture-review` vergibt die festen.

## Performance Implications
- **CPU**: Nachschlagetabellen einmal beim Start; Nachschlagen per Kennung über ein `Dictionary`. Keine Kosten
  pro Frame.
- **Memory**: einige Dutzend kleine ScriptableObjects, vernachlässigbar.
- **Load Time**: Prüfung und Tabellenaufbau beim Host-Start, im Millisekundenbereich (geschätzt, nicht gemessen).
- **Network**: keine Daten für die Art eines Gegenstands. Je Vorratspaket ohne feste Ware einmal die Länge plus
  die Zeichen der Kennung (etwa 10–15 Byte), je Gericht einmal 8 Byte, jeweils in der Spawn-Nachricht.

## Migration Plan

Im Plan-Schritt „Gegenstandsarten“, nach `/test-setup` (Assemblies und Test-Framework):
1. Typen, `CatalogValidator`, `DishRules` und `CatalogService` anlegen; EditMode-Tests nach GDD
   Abnahmekriterien 1–6 und 18–20.
2. Editor-Skript legt die Assets an: 13 Arten, 7 Waren, 5 Bedürfnisse (Startwerte aus den GDDs) und den
   Katalog. Prefabs für neue Arten entstehen, wenn ihre Systeme gebaut werden.
3. `Item` an `Crate` (→ Art `vorratskiste`), `Firewood` (→ `holzscheit`) und `HeavyChest` (→ `truhe`);
   `Carryable.Mass` und der Ofen lesen aus der Art; `FuelItem` entfällt.
4. Build-Hook und Prüfung beim Host-Start einbauen; Test mit absichtlich kaputtem Katalog.
5. Gäste-Datentypen (`GuestType`, Gäste-Einstellungen) im Plan-Schritt „Erster Gast und Tee“.

**Rollback plan**: Die Datenobjekte sind ein Zusatz. Bis Schritt 3 laufen die alten Werte weiter; danach lassen
sich `FuelItem` und die Prefab-Massen aus dem Git-Stand zurückholen.

## Validation Criteria
- **EditMode:** GDD Gegenstandsarten, Abnahmekriterien 1–6 (Katalog und Prüfung), 18–20 (Abkühlen); GDD Gäste,
  Datenprüfung (ausgelieferte Daten fehlerfrei, je kaputtem Objekt genau 1 Fehler).
- **EditMode + PlayMode:** Abnahmekriterium 7 (Prefab-Masse 1 → nach Erscheinen 3,0).
- **Manuell:** Abnahmekriterien 8 (Objekt ohne Art) und 9 (`git status` sauber nach einer Runde); kaputter
  Katalog sperrt Build und Host-Start.
- **Editor, MPPM mit 100 ms Ping:** Der Client sieht bei Kiste, Scheit und Truhe die richtige Art (Log-Zeile mit
  Kennung) und bei einem Vorratspaket die richtige Ware.

## Related
- ADR-0001 (Netz-Autorität und Physik; Regeln `network_object_identity`, `impact_report`)
- ADR-0003 (Spielzeit für das Abkühlen), ADR-0004 (Interaktion liest Werkzeug und Ausrüstung), ADR-0005 (Gäste)
- `design/gdd/gegenstandsarten.md` (Regeln 1–3, Formeln, Edge Cases, Tuning Knobs, Abnahmekriterien A und C)
- `design/gdd/gaeste.md` (Regeln 1–2, Formel `guest_data_valid`, Edge Cases zu falschen Daten)
- Code: `Assets/_Project/Scripts/Hut/FuelItem.cs`, `Carrying/Carryable.cs`, `Carrying/CrateSpawner.cs`,
  `Net/GameVersion.cs`
